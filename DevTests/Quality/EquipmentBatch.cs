using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ForeignCacheProbe;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (CHANGELOG 2026-11-12; Source/Quality_EquipmentCombat.cs): an unrelated mod patching the equipment hooks
// (CompQuality.SetQuality, Thing.Notify_Equipped/Notify_Unequipped, StatWorker.StatOffsetFromGear) no longer switches the
// equipment effects off. Mod set "equipment" loads the Progression settings (all equipment effects on, Mild factors
// .95/.98/1/1.02/1.05/1.10/1.20) plus the marker that makes ForeignPatchProbe patch those four methods before the mod's startup.
// - Equipment Social and Movement (the groups Core apparel contributes to) are available and not marked as handled elsewhere;
//   melee and ranged cooldown and trap damage, which share the cache hooks, are available.
// - A worn item's beneficial gear offset follows the Mild curve: Apparel_CowboyHat SocialImpact +0.10, Apparel_ClothMask
//   ToxicEnvironmentResistance +0.5.
// Mechanical except cq.gear-worn, which reads a spawned colonist's stat while wearing the hat.
// Not covered: Combat Extended's custom stat workers (CE is not installed), Work, Medicine and Abilities (no Core item adds
// those offsets), and the Harmony-ID note shown when a real quality provider blocks an effect.
public class EquipmentBatch : Batch
{
    public override string Id => "cq-equipment";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[]
    {
        "cq.foreign-hooks", "cq.gear-available", "cq.gear-curve-social", "cq.gear-curve-toxic", "cq.gear-worn",
    }.Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    private static readonly QualityCategory[] Levels7 = Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>().ToArray();
    private static readonly Type EffectId = AccessTools.TypeByName("ConfigurableQuality.EffectId");
    private static readonly Type Effects = AccessTools.TypeByName("ConfigurableQuality.QualityEffects");
    private static readonly Type Catalog = AccessTools.TypeByName("ConfigurableQuality.QualityCatalog");
    private static readonly Type Combat = AccessTools.TypeByName("ConfigurableQuality.QualityEquipmentCombat");

    private static object Effect(string id) => Enum.Parse(EffectId, id);
    private static T Call<T>(Type type, string method, object arg) => (T)AccessTools.Method(type, method).Invoke(null, new[] { arg });
    private static bool Available(string id) => Call<bool>(Effects, "Available", Effect(id));
    private static bool HandledElsewhere(string id) => Call<bool>(Effects, "HandledElsewhere", Effect(id));
    private static string Reason(string id) => Call<string>(Effects, "Reason", Effect(id));
    private static bool Participates(ThingDef def) => Call<bool>(Catalog, "Participates", def);
    private static bool CuratedGearStat(StatDef stat) => ((IDictionary)AccessTools.Field(Combat, "gearStats").GetValue(null)).Contains(stat);

    private static string Join(IEnumerable<float> values) => string.Join("/", values.Select(f => f.ToString("0.###")));

    private static Thing Make(string def, QualityCategory quality)
    {
        var thingDef = Kit.Def(def);
        var thing = ThingMaker.MakeThing(thingDef, thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(thingDef) : null);
        thing.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Colony);
        return thing;
    }

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        ForeignHooks(ctx);
        GearAvailable(ctx);
        Curve(ctx, "cq.gear-curve-social", "Apparel_CowboyHat", StatDefOf.SocialImpact);
        Curve(ctx, "cq.gear-curve-toxic", "Apparel_ClothMask", StatDefOf.ToxicEnvironmentResistance);

        // A colonist wearing the hat: its SocialImpact rises by the hat's extra offset when the worn hat's quality changes.
        // The stat is read before SetQuality, so a cached reading would be stale unless the mod's SetQuality hook clears it.
        Kit.RemoveStartingColonists(ctx);
        var area = Kit.Area(ctx, 5, 5);
        var pawn = Kit.Worker(ctx, area.CenterCell, WorkTypeDefOf.Hauling);
        var hat = (Apparel)Make("Apparel_CowboyHat", QualityCategory.Normal);
        pawn.apparel.Wear(hat, dropReplacedApparel: true);
        ctx.Setup($"Developer put a Normal {hat.LabelShort} on {pawn.LabelShort}");
        yield return Wait.Ticks(2);
        var social = StatDefOf.SocialImpact;
        float before = pawn.GetStatValue(social);
        float gearNormal = StatWorker.StatOffsetFromGear(hat, social);
        hat.GetComp<CompQuality>().SetQuality(QualityCategory.Legendary, ArtGenerationContext.Colony);
        ctx.Setup($"Developer set the worn {hat.LabelShort} to Legendary");
        float after = pawn.GetStatValue(social);
        float expected = StatWorker.StatOffsetFromGear(hat, social) - gearNormal;
        if (Kit.Require(ctx, "cq.gear-worn", (pawn.apparel.Wearing(hat), "the colonist wears the hat"), (expected > 0.001f, $"the Legendary hat adds more than the Normal one ({expected:0.###})")))
            ctx.Begin("cq.gear-worn").Subject(pawn).Expect("socialImpactRise", expected.ToString("0.###"))
                .Observe("before", before.ToString("0.###")).Observe("after", after.ToString("0.###"))
                .Observe("socialImpactRise", (after - before).ToString("0.###")).Observe("cacheable", social.cacheable)
                .Check(Kit.Near(after - before, expected, 0.002f), "the worn hat's quality change did not reach the colonist's SocialImpact");
    }

    private static void ForeignHooks(BatchContext ctx)
    {
        var c = ctx.Begin("cq.foreign-hooks").Expect("probe", "active").Expect("each hook", "mod absent at probe time; probe and mod patches now");
        c.Observe("probe", ForeignPatchProbe.Active ? "active" : "inactive");
        bool ok = ForeignPatchProbe.Active;
        foreach (var (name, method) in ForeignPatchProbe.Targets())
        {
            var owners = Harmony.GetPatchInfo(method)?.Owners.ToList() ?? new List<string>();
            bool early = ForeignPatchProbe.ModOwnedAtProbe.TryGetValue(name, out var owned) && !owned;
            bool both = owners.Contains(ForeignPatchProbe.Owner) && owners.Contains(CqKit.HarmonyId);
            ok &= early && both;
            c.Observe(name, $"modAtProbe={(ForeignPatchProbe.ModOwnedAtProbe.TryGetValue(name, out var o) ? o.ToString() : "n/a")} owners=[{string.Join(", ", owners)}]");
        }
        c.Check(ok, "the foreign patches were not installed before the mod's startup, or the mod did not patch alongside them");
    }

    private static void GearAvailable(BatchContext ctx)
    {
        string conflict = "QEverything.EquipmentCombat.Conflict".Translate();
        string[] expectedOn = { "EquipmentSocial", "EquipmentMovement", "MeleeCooldown", "RangedCooldown", "TrapDamage" };
        string[] gear = { "EquipmentWork", "EquipmentMovement", "EquipmentMedicine", "EquipmentSocial", "EquipmentAbilities" };
        var c = ctx.Begin("cq.gear-available").Expect("available", string.Join(",", expectedOn)).Expect("gear blocked by a conflict", "none");
        foreach (var id in gear.Union(expectedOn))
            c.Observe(id, $"available={Available(id)} handledElsewhere={HandledElsewhere(id)} reason={Reason(id)}");
        var blocked = gear.Where(id => HandledElsewhere(id) || Reason(id) == conflict || Reason(id).Contains("Harmony ID")).ToList();
        var off = expectedOn.Where(id => !Available(id)).ToList();
        c.Check(off.Count == 0 && blocked.Count == 0,
            $"unrelated foreign patches still block equipment effects (unavailable: {string.Join(",", off)}; blocked: {string.Join(",", blocked)})");
    }

    private static void Curve(BatchContext ctx, string id, string def, StatDef stat)
    {
        var thingDef = Kit.Def(def);
        if (!Kit.Require(ctx, id, (thingDef.HasComp(typeof(CompQuality)), def + " has quality"), (Participates(thingDef), def + " participates in effects"),
                (CuratedGearStat(stat), stat.defName + " is a curated gear stat")))
            return;
        var offsets = Levels7.Select(q => StatWorker.StatOffsetFromGear(Make(def, q), stat)).ToArray();
        float normal = offsets[(int)QualityCategory.Normal];
        var ratios = offsets.Select(v => normal == 0f ? 0f : v / normal).ToArray();
        ctx.Begin(id).Expect("ratio to Normal", Join(CqKit.MildBenefit)).Observe("offsets", Join(offsets)).Observe("ratio to Normal", Join(ratios))
            .Check(normal > 0f && ratios.Zip(CqKit.MildBenefit, (r, e) => Kit.Near(r, e, 0.001f)).All(x => x),
                $"{def}'s {stat.defName} gear offset does not follow the Mild curve");
    }
}
