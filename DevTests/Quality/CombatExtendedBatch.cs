using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;
using Verse.AI;
using static ConfigurableQuality.DevTests.MechBionicsBatch;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (Source/Quality_CombatExtended.cs; Steam report 2026-11-21 that reload quality does nothing). Mod set "ce"
// (Core, Combat Extended) loads the ebf settings plus "Aim time, reload, sway and recoil" (CeHandling) on with Mild factors
// .95/.98/1/1.02/1.05/1.10/1.20.
// - CeHandling is available and the mod's stat part is on CE_RangedWeapon_ReloadFactor.
// - An assault rifle's CE_RangedWeapon_ReloadFactor is 1/.95, 1 and 1/1.2 at Awful, Normal and Legendary.
// - Combat Extended's own reload job for that rifle, started on a colonist holding it with ammunition, waits the Normal duration
//   x1/.95 at Awful and x1/1.2 at Legendary (CE rounds each duration up to a whole tick).
// Mechanical: starts CE's ReloadWeapon job directly and reads the length of its wait toil. Not covered: the reload finishing,
// the pawn's Reload speed tooltip, and the other CE effects.
public class CombatExtendedBatch : Batch
{
    public override string Id => "cq-ce";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { "cq.ce-available", "cq.ce-reload-factor", "cq.ce-reload-job" }
        .Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    private static readonly QualityCategory[] Measured = { QualityCategory.Awful, QualityCategory.Normal, QualityCategory.Legendary };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var factorStat = DefDatabase<StatDef>.GetNamedSilentFail("CE_RangedWeapon_ReloadFactor");
        bool available = Call<bool>("Available", "CeHandling");
        bool owned = factorStat != null && CqKit.Parts(factorStat).Count > 0;
        ctx.Begin("cq.ce-available").Expect("CeHandling", "available").Expect("part on reload factor", true)
            .Observe("CeHandling", $"available={available} reason={Call<string>("Reason", "CeHandling")}")
            .Observe("part on reload factor", owned)
            .Check(available && owned, "the handling effect is unavailable or has no stat part on CE's reload factor");

        Kit.RemoveStartingColonists(ctx);
        var area = Kit.Area(ctx, 5, 5);
        var pawn = Kit.Worker(ctx, area.CenterCell, WorkTypeDefOf.Hauling);
        yield return Wait.Ticks(2);

        var rifle = (ThingWithComps)CqKit.Make("Gun_AssaultRifle", null, QualityCategory.Normal);
        var quality = rifle.TryGetComp<CompQuality>();
        var ammoUser = rifle.AllComps.FirstOrDefault(c => c.GetType().FullName == "CombatExtended.CompAmmoUser");
        if (!Kit.Require(ctx, "cq.ce-reload-factor", (factorStat != null, "Combat Extended defines CE_RangedWeapon_ReloadFactor"),
                (quality != null, "the assault rifle has quality"), (ammoUser != null, "the assault rifle has Combat Extended's ammo user"))) yield break;

        float Factor(QualityCategory q)
        {
            quality.SetQuality(q, ArtGenerationContext.Colony);
            return rifle.GetStatValue(factorStat, true);
        }
        var factors = Measured.Select(Factor).ToArray();
        var expected = Measured.Select(q => 1f / CqKit.MildBenefit[(int)q]).ToArray();
        ctx.Begin("cq.ce-reload-factor")
            .Expect("Awful / Normal / Legendary", string.Join(" / ", expected.Select(v => v.ToString("0.###"))))
            .Observe("Awful / Normal / Legendary", string.Join(" / ", factors.Select(v => v.ToString("0.###"))))
            .Check(Enumerable.Range(0, 3).All(i => Kit.Near(factors[i], expected[i], 0.001f)), "the rifle's reload factor does not follow its quality");

        pawn.equipment.AddEquipment(rifle);
        var ammoDef = AccessTools.Property(ammoUser.GetType(), "CurrentAmmo").GetValue(ammoUser) as ThingDef;
        if (!Kit.Require(ctx, "cq.ce-reload-job", (ammoDef != null, "the rifle names its current ammunition"))) yield break;
        var ammo = ThingMaker.MakeThing(ammoDef);
        ammo.stackCount = 200;
        pawn.inventory.innerContainer.TryAdd(ammo);
        ctx.Setup($"Developer equipped {pawn.LabelShort} with a {rifle.def.label} and gave 200 {ammoDef.label}");
        yield return Wait.Ticks(2);

        var magCount = AccessTools.Property(ammoUser.GetType(), "CurMagCount");
        var makeJob = AccessTools.Method(ammoUser.GetType(), "TryMakeReloadJob");
        var toilsField = AccessTools.Field(typeof(JobDriver), "toils");
        var durations = new int[Measured.Length];
        string driverName = null;
        for (int i = 0; i < Measured.Length; i++)
        {
            quality.SetQuality(Measured[i], ArtGenerationContext.Colony);
            magCount.SetValue(ammoUser, 0);
            var job = (Job)makeJob.Invoke(ammoUser, null);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            var driver = pawn.jobs.curDriver;
            driverName = driver?.GetType().FullName;
            var toils = toilsField.GetValue(driver) as List<Toil>;
            durations[i] = toils?.Where(t => t.defaultCompleteMode == ToilCompleteMode.Delay).Select(t => t.defaultDuration).DefaultIfEmpty(0).Max() ?? 0;
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            yield return Wait.Ticks(1);
        }
        int normal = durations[1];
        var expectedTicks = Measured.Select(q => normal / CqKit.MildBenefit[(int)q]).ToArray();
        ctx.Begin("cq.ce-reload-job").Subject(pawn)
            .Expect("reload ticks Awful / Normal / Legendary", string.Join(" / ", expectedTicks.Select(v => v.ToString("0.#"))))
            .Observe("reload ticks Awful / Normal / Legendary", string.Join(" / ", durations))
            .Observe("driver", driverName)
            .Check(normal > 0 && Enumerable.Range(0, 3).All(i => Kit.Near(durations[i], expectedTicks[i], 2f)) && durations[2] < normal && durations[0] > normal,
                "Combat Extended's reload job does not take the rifle's quality into account");
    }
}
