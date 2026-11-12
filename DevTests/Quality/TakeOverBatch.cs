using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ProviderProbe;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (CHANGELOG 2026-11-12 "Added"; Source/Quality_EquipmentCombat.cs, Quality_PatchOwnership.cs):
// an effect that another quality mod already handles steps aside by default, and replaces that mod's version when the
// player switches it on and ticks "Use this effect instead of the other mod's".
// Both mod sets load the Progression settings (all effects on, Mild factors) plus the marker that makes QualityProviderProbe
// double gear offsets, patch the turret cooldown and add a Legendary x0.5 quality part to RangedWeapon_Cooldown.
// - "provider": no take-over. The gear effects, ranged cooldown and turret cooldown are handled elsewhere; the probe's
//   scaling is untouched (hat offset 0.2 at every quality, revolver cooldown Legendary/Normal 0.5).
// - "takeover": takeOver on EquipmentSocial and RangedCooldown only. The probe's gear patch and quality part are gone and
//   the Mild curves apply (hat offset 0.1 x benefit curve, revolver cooldown x cost curve). TurretCooldown, not ticked, is
//   still handled elsewhere and the probe's turret patch stays.
// Mechanical: reads registration state, Harmony owners and stats on made things. Not covered: a real quality mod, the
// hit point, power, battery and consumable take-overs, and ticking the box in the editor.
internal static class TakeOverKit
{
    public static readonly QualityCategory[] Levels7 = Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>().ToArray();
    // Source/Quality_Configuration.cs QualityEffectCatalog.Curve: Mild cost factors for Awful through Legendary.
    public static readonly float[] MildCost = { 1.05f, 1.02f, 1f, .98f, .95f, .90f, .80f };
    private static readonly Type EffectId = AccessTools.TypeByName("ConfigurableQuality.EffectId");
    private static readonly Type Effects = AccessTools.TypeByName("ConfigurableQuality.QualityEffects");

    private static T Call<T>(string method, string id) => (T)AccessTools.Method(Effects, method).Invoke(null, new[] { Enum.Parse(EffectId, id) });
    public static bool Available(string id) => Call<bool>("Available", id);
    public static bool HandledElsewhere(string id) => Call<bool>("HandledElsewhere", id);
    public static bool TakenOver(string id) => Call<bool>("TakenOver", id);
    public static string Reason(string id) => Call<string>("Reason", id);
    public static string State(string id) => $"available={Available(id)} handledElsewhere={HandledElsewhere(id)} takenOver={TakenOver(id)} reason={Reason(id)}";

    public static List<string> Owners(System.Reflection.MethodBase method) => Harmony.GetPatchInfo(method)?.Owners.ToList() ?? new List<string>();
    public static string Join(IEnumerable<float> values) => string.Join("/", values.Select(f => f.ToString("0.###")));
    public static bool Matches(float[] observed, float[] expected) => observed.Zip(expected, (o, e) => Kit.Near(o, e, 0.001f)).All(x => x);

    public static float[] HatOffsets() => Levels7.Select(q =>
        StatWorker.StatOffsetFromGear(CqKit.Make("Apparel_CowboyHat", "Cloth", q), StatDefOf.SocialImpact)).ToArray();

    // Legendary etc. relative to Normal, so the revolver's base cooldown drops out.
    public static float[] RevolverCooldownRatios()
    {
        var values = Levels7.Select(q => CqKit.Make("Gun_Revolver", null, q).GetStatValue(StatDefOf.RangedWeapon_Cooldown)).ToArray();
        float normal = values[(int)QualityCategory.Normal];
        return values.Select(v => normal == 0f ? 0f : v / normal).ToArray();
    }

    public static bool VanillaQualityPart(StatDef stat) => stat.parts?.Any(p => p.GetType() == typeof(StatPart_Quality)) == true;

    public static bool Probe(BatchContext ctx, string id) => Kit.Require(ctx, id,
        (QualityProviderProbe.Active, "the quality provider probe is active"), (QualityProviderProbe.StatPartAdded, "the probe added its ranged cooldown quality part"));
}

public class ProviderBlockedBatch : Batch
{
    public override string Id => "cq-provider";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { "cq.provider-gear", "cq.provider-stat", "cq.provider-turret" }
        .Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        if (TakeOverKit.Probe(ctx, "cq.provider-gear"))
        {
            var owners = TakeOverKit.Owners(QualityProviderProbe.GearMethod);
            var offsets = TakeOverKit.HatOffsets();
            var expected = TakeOverKit.Levels7.Select(_ => .1f * QualityProviderProbe.GearMultiplier).ToArray();
            ctx.Begin("cq.provider-gear").Expect("EquipmentSocial", "handled elsewhere, reason names " + QualityProviderProbe.Owner)
                .Expect("hat SocialImpact offsets", TakeOverKit.Join(expected))
                .Observe("EquipmentSocial", TakeOverKit.State("EquipmentSocial")).Observe("owners", string.Join(", ", owners))
                .Observe("hat SocialImpact offsets", TakeOverKit.Join(offsets))
                .Check(!TakeOverKit.Available("EquipmentSocial") && TakeOverKit.HandledElsewhere("EquipmentSocial") && !TakeOverKit.TakenOver("EquipmentSocial")
                    && TakeOverKit.Reason("EquipmentSocial").IndexOf(QualityProviderProbe.Owner, StringComparison.OrdinalIgnoreCase) >= 0
                    && owners.Contains(QualityProviderProbe.Owner) && !owners.Contains(CqKit.HarmonyId) && TakeOverKit.Matches(offsets, expected),
                    "without take-over the mod did not step aside for the quality provider on gear offsets");
        }
        if (TakeOverKit.Probe(ctx, "cq.provider-stat"))
        {
            var ratios = TakeOverKit.RevolverCooldownRatios();
            var expected = new[] { 1f, 1f, 1f, 1f, 1f, 1f, QualityProviderProbe.LegendaryCooldown };
            ctx.Begin("cq.provider-stat").Expect("RangedCooldown", "handled elsewhere").Expect("revolver cooldown ratio to Normal", TakeOverKit.Join(expected))
                .Observe("RangedCooldown", TakeOverKit.State("RangedCooldown"))
                .Observe("provider quality part", TakeOverKit.VanillaQualityPart(StatDefOf.RangedWeapon_Cooldown))
                .Observe("mod parts", CqKit.Parts(StatDefOf.RangedWeapon_Cooldown).Count)
                .Observe("revolver cooldown ratio to Normal", TakeOverKit.Join(ratios))
                .Check(!TakeOverKit.Available("RangedCooldown") && TakeOverKit.HandledElsewhere("RangedCooldown")
                    && TakeOverKit.VanillaQualityPart(StatDefOf.RangedWeapon_Cooldown) && CqKit.Parts(StatDefOf.RangedWeapon_Cooldown).Count == 0
                    && TakeOverKit.Matches(ratios, expected),
                    "without take-over the mod did not leave the provider's ranged cooldown curve alone");
        }
        if (TakeOverKit.Probe(ctx, "cq.provider-turret"))
        {
            var owners = TakeOverKit.Owners(QualityProviderProbe.TurretMethod);
            ctx.Begin("cq.provider-turret").Expect("TurretCooldown", "handled elsewhere; provider patch present, mod patch absent")
                .Observe("TurretCooldown", TakeOverKit.State("TurretCooldown")).Observe("owners", string.Join(", ", owners))
                .Check(!TakeOverKit.Available("TurretCooldown") && TakeOverKit.HandledElsewhere("TurretCooldown")
                    && owners.Contains(QualityProviderProbe.Owner) && !owners.Contains(CqKit.HarmonyId),
                    "without take-over the mod did not step aside for the quality provider on turret cooldown");
        }
        yield break;
    }
}

public class TakeOverBatch : Batch
{
    public override string Id => "cq-takeover";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { "cq.takeover-gear", "cq.takeover-stat", "cq.takeover-scoped" }
        .Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        if (TakeOverKit.Probe(ctx, "cq.takeover-gear"))
        {
            var owners = TakeOverKit.Owners(QualityProviderProbe.GearMethod);
            var offsets = TakeOverKit.HatOffsets();
            var expected = CqKit.MildBenefit.Select(f => .1f * f).ToArray();
            ctx.Begin("cq.takeover-gear").Expect("EquipmentSocial", "available and taken over; provider patch removed, mod patch present")
                .Expect("hat SocialImpact offsets", TakeOverKit.Join(expected))
                .Observe("EquipmentSocial", TakeOverKit.State("EquipmentSocial")).Observe("owners", string.Join(", ", owners))
                .Observe("hat SocialImpact offsets", TakeOverKit.Join(offsets))
                .Check(TakeOverKit.Available("EquipmentSocial") && TakeOverKit.TakenOver("EquipmentSocial") && !TakeOverKit.HandledElsewhere("EquipmentSocial")
                    && !owners.Contains(QualityProviderProbe.Owner) && owners.Contains(CqKit.HarmonyId) && TakeOverKit.Matches(offsets, expected),
                    "take-over did not replace the provider's gear offset scaling with the Mild curve");
        }
        if (TakeOverKit.Probe(ctx, "cq.takeover-stat"))
        {
            var ratios = TakeOverKit.RevolverCooldownRatios();
            ctx.Begin("cq.takeover-stat").Expect("RangedCooldown", "available and taken over; provider quality part removed")
                .Expect("revolver cooldown ratio to Normal", TakeOverKit.Join(TakeOverKit.MildCost))
                .Observe("RangedCooldown", TakeOverKit.State("RangedCooldown"))
                .Observe("provider quality part", TakeOverKit.VanillaQualityPart(StatDefOf.RangedWeapon_Cooldown))
                .Observe("mod parts", CqKit.Parts(StatDefOf.RangedWeapon_Cooldown).Count)
                .Observe("revolver cooldown ratio to Normal", TakeOverKit.Join(ratios))
                .Check(TakeOverKit.Available("RangedCooldown") && TakeOverKit.TakenOver("RangedCooldown")
                    && !TakeOverKit.VanillaQualityPart(StatDefOf.RangedWeapon_Cooldown) && CqKit.Parts(StatDefOf.RangedWeapon_Cooldown).Count == 1
                    && TakeOverKit.Matches(ratios, TakeOverKit.MildCost),
                    "take-over did not replace the provider's ranged cooldown curve with the Mild curve");
        }
        if (TakeOverKit.Probe(ctx, "cq.takeover-scoped"))
        {
            var owners = TakeOverKit.Owners(QualityProviderProbe.TurretMethod);
            ctx.Begin("cq.takeover-scoped").Expect("TurretCooldown (not ticked)", "handled elsewhere; provider patch present, mod patch absent")
                .Observe("TurretCooldown", TakeOverKit.State("TurretCooldown")).Observe("owners", string.Join(", ", owners))
                .Check(!TakeOverKit.Available("TurretCooldown") && TakeOverKit.HandledElsewhere("TurretCooldown") && !TakeOverKit.TakenOver("TurretCooldown")
                    && owners.Contains(QualityProviderProbe.Owner) && !owners.Contains(CqKit.HarmonyId),
                    "take-over reached an effect that was not ticked");
        }
        yield break;
    }
}
