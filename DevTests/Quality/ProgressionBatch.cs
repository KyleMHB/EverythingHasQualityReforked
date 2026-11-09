using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (Presets/Progression settings; docs/quality-guide.md). Mod set "progression" loads
// DevTests/modsettings/progression, a copy of the preset's settings file, at startup; settings apply after a restart, so
// they cannot change mid-run and run-all cannot combine this batch with cq-fresh.
// - Assignment: Building and Stuff groups on; Meal and Medicine off; native quality kept.
// - Building group bounds: minEdificeQuality 0, maxEdificeQuality 4, so a wall's quality stays within Awful..Excellent.
// - Mild durability factors .95/.98/1/1.02/1.05/1.10/1.20 (Awful..Legendary) for BuildingDurability and ApparelDurability.
// Mechanical: quality is set directly on made or god-mode-placed things; construction and production are not exercised.
public class ProgressionBatch : Batch
{
    public override string Id => "cq-progression";
    public override int SegmentCount => 2;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[]
    {
        "cq.progression-assignment", "cq.edifice-bounds", "cq.building-durability", "cq.apparel-durability", "cq.wall-hitpoints", "cq.reload",
    }.Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    private static readonly QualityCategory[] Levels7 = Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>().ToArray();
    private const QualityCategory EdificeMax = QualityCategory.Excellent; // maxEdificeQuality 4

    private static string Join(IEnumerable<float> values) => string.Join("/", values.Select(f => f.ToString("0.###")));

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        if (index == 1)
        {
            Reloaded(ctx);
            yield break;
        }

        string[] expectQuality = { "Wall", "Steel" }, expectNone = { "MealSimple", "MedicineIndustrial" };
        bool assignmentOk = expectQuality.All(CqKit.HasQuality) && !expectNone.Any(CqKit.HasQuality) && CqKit.NativeDefs.All(CqKit.HasQuality);
        ctx.Begin("cq.progression-assignment").Expect("Wall,Steel", "quality").Expect("MealSimple,MedicineIndustrial", "no quality")
            .Expect("native", "quality kept")
            .Observe("defs", CqKit.QualityDefs(CqKit.AddableDefs.Concat(CqKit.NativeDefs)))
            .Check(assignmentOk, "the Progression preset did not give Building/Stuff defs quality, gave Meal/Medicine quality, or removed native quality");
        if (!CqKit.HasQuality("Wall"))
        {
            Kit.SkipRemaining(ctx, this, "steel walls have no quality, so bounds and durability cannot be measured", CoverageReasons.SetupFailed);
            yield break;
        }

        // Requesting each quality on a wall: levels above the Building bound are held at Excellent.
        var walls = Levels7.Select(q => CqKit.Make("Wall", "Steel", q)).ToArray();
        var stored = walls.Select(w => w.TryGetQuality(out var q) ? q.ToString() : "none").ToArray();
        var expectedStored = Levels7.Select(q => q > EdificeMax ? EdificeMax : q).ToArray();
        ctx.Begin("cq.edifice-bounds").Expect("stored", string.Join("/", expectedStored)).Observe("stored", string.Join("/", stored))
            .Check(stored.SequenceEqual(expectedStored.Select(q => q.ToString())), "a wall's quality was not held within the Building group's Awful..Excellent bounds");

        // The mod's own MaxHitPoints contribution follows the Mild curve for the wall's stored quality.
        var wallFactors = walls.Select(w => CqKit.Contribution(w, StatDefOf.MaxHitPoints)).ToArray();
        var wallExpected = expectedStored.Select(q => CqKit.MildBenefit[(int)q]).ToArray();
        ctx.Begin("cq.building-durability").Expect("factors", Join(wallExpected)).Observe("factors", Join(wallFactors))
            .Check(wallFactors.Zip(wallExpected, (f, e) => Kit.Near(f, e, 0.001f)).All(x => x), "BuildingDurability does not follow the Mild curve");

        // A native-quality apparel item is not bounded by the Building group, so it covers the whole curve up to x1.20.
        var dusterFactors = Levels7.Select(q => CqKit.Contribution(CqKit.Make("Apparel_Duster", "Cloth", q), StatDefOf.MaxHitPoints)).ToArray();
        ctx.Begin("cq.apparel-durability").Expect("factors", Join(CqKit.MildBenefit)).Observe("factors", Join(dusterFactors))
            .Check(dusterFactors.Zip(CqKit.MildBenefit, (f, e) => Kit.Near(f, e, 0.001f)).All(x => x), "ApparelDurability does not follow the Mild curve");

        // The whole stat as the game reads it on placed walls: Excellent (the bound) has 1.05 times Normal's maximum hit points.
        var area = Kit.Area(ctx, 6, 3);
        var normal = Kit.Place(ctx, "Wall", area.Min + new IntVec3(1, 0, 1), Rot4.North, Kit.Def("Steel"));
        var excellent = Kit.Place(ctx, "Wall", area.Min + new IntVec3(4, 0, 1), Rot4.North, Kit.Def("Steel"));
        normal.TryGetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        excellent.TryGetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
        ctx.Setup($"Developer set {normal.ThingID} to Normal and {excellent.ThingID} to Excellent");
        yield return Wait.Ticks(2);
        float ratio = (float)excellent.MaxHitPoints / normal.MaxHitPoints;
        ctx.Begin("cq.wall-hitpoints").Expect("excellentOverNormal", "1.05 (±1 HP)")
            .Observe("normalMaxHitPoints", normal.MaxHitPoints).Observe("excellentMaxHitPoints", excellent.MaxHitPoints).Observe("excellentOverNormal", ratio.ToString("0.###"))
            .Check(Math.Abs(excellent.MaxHitPoints - normal.MaxHitPoints * 1.05f) <= 1f, "an Excellent wall's maximum hit points are not 1.05 times a Normal wall's");

        ctx.Remember("excellent", excellent.ThingID);
        ctx.Remember("excellentMax", excellent.MaxHitPoints.ToString());
        yield return Wait.Reload();
    }

    private static void Reloaded(BatchContext ctx)
    {
        var wall = ctx.Map.listerThings.ThingsOfDef(Kit.Def("Wall")).FirstOrDefault(t => t.ThingID == ctx.Recall("excellent"));
        string quality = wall != null && wall.TryGetQuality(out var q) ? q.ToString() : "none";
        int expected = int.Parse(ctx.Recall("excellentMax"));
        ctx.Begin("cq.reload").Expect("found", true).Expect("quality", "Excellent").Expect("maxHitPoints", expected)
            .Observe("found", wall != null).Observe("quality", quality).Observe("maxHitPoints", wall?.MaxHitPoints ?? -1)
            .Check(quality == "Excellent" && wall.MaxHitPoints == expected, "the Excellent wall lost its quality or its scaled hit points across save and reload");
    }
}
