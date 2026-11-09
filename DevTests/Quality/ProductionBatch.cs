using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (docs/quality-guide.md "Production"; the Progression preset's production settings): production quality
// is clamped to each group's minimum and maximum. Under Progression the Building group is Awful..Excellent (maxEdificeQuality
// 4), Manufactured (the first loaded def in that group) is Awful..Excellent (maxManufQuality 4) and Ingredient is Normal..Excellent (minIngQuality 2, maxIngQuality 4).
// Construction runs the real path: a skill-20 colonist completes wall frames through Frame.CompleteConstruction, which the mod
// redirects to its generator. Crafting and harvesting are sampled through Quality_Generator.GenerateQualityForDef, the method
// those patched paths call, with the vanilla distribution for the same skill as a guard that the bound matters.
public class ProductionBatch : Batch
{
    private const int Walls = 30, Draws = 200;

    public override string Id => "cq-production";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[]
    {
        "cq.construction-bound", "cq.manufactured-bound", "cq.ingredient-minimum",
    }.Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    private static readonly Type DefUtility = AccessTools.TypeByName("ConfigurableQuality.QualityDefUtility");
    private static int MinQuality(ThingDef def) => (int)AccessTools.Method(DefUtility, "GetMinQuality").Invoke(null, new object[] { def });
    private static int MaxQuality(ThingDef def) => (int)AccessTools.Method(DefUtility, "GetMaxQuality").Invoke(null, new object[] { def });
    private static string Bucket(ThingDef def) => AccessTools.Method(DefUtility, "GetBucket").Invoke(null, new object[] { def }).ToString();

    private static string Histogram(IEnumerable<QualityCategory> values) =>
        string.Join(" ", values.GroupBy(q => q).OrderBy(g => g.Key).Select(g => $"{g.Key}:{g.Count()}"));

    private static List<QualityCategory> Vanilla(int level) =>
        Enumerable.Range(0, Draws).Select(_ => QualityUtility.GenerateQualityCreatedByPawn(level, false)).ToList();

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        Kit.RemoveStartingColonists(ctx);
        var area = Kit.Area(ctx, 34, 10);
        IntVec3 At(int x, int z) => area.Min + new IntVec3(x, 0, z);
        var builder = Kit.Worker(ctx, At(1, 8), WorkTypeDefOf.Construction, SkillDefOf.Construction);
        builder.skills.GetSkill(SkillDefOf.Construction).Level = 20;
        builder.skills.GetSkill(SkillDefOf.Crafting).Level = 20;
        ctx.Setup($"Developer set {builder.LabelShort}'s Construction and Crafting skills to 20");

        // Construction: real frames completed by the skill-20 builder.
        var wall = Kit.Def("Wall");
        var vanilla20 = Vanilla(20);
        if (Kit.Require(ctx, "cq.construction-bound",
                (MaxQuality(wall) == (int)QualityCategory.Excellent, $"the Building bound is Excellent (is {(QualityCategory)MaxQuality(wall)})"),
                (vanilla20.Any(q => q > QualityCategory.Excellent), $"vanilla skill-20 quality can exceed Excellent ({Histogram(vanilla20)})")))
        {
            var built = new List<QualityCategory>();
            var missing = new List<string>();
            for (int i = 0; i < Walls; i++)
            {
                var cell = At(1 + i, 2);
                var frame = (Frame)ThingMaker.MakeThing(wall.frameDef, Kit.Def("Steel"));
                frame.SetFactionDirect(Faction.OfPlayer);
                var steel = ThingMaker.MakeThing(Kit.Def("Steel"));
                steel.stackCount = wall.CostStuffCount;
                frame.resourceContainer.TryAdd(steel);
                GenSpawn.Spawn(frame, cell, ctx.Map);
                frame.CompleteConstruction(builder);
                if (cell.GetEdifice(ctx.Map) is Building b && b.def == wall && b.TryGetQuality(out var q)) built.Add(q);
                else missing.Add(cell.ToString());
            }
            ctx.Setup($"Developer spawned {Walls} steel wall frames with their steel and called Frame.CompleteConstruction for {builder.LabelShort}");
            bool ok = missing.Count == 0 && built.All(q => q <= QualityCategory.Excellent);
            ctx.Begin("cq.construction-bound").Expect("walls", Walls).Expect("highest", "<= Excellent").Subject(builder)
                .Observe("walls", built.Count).Observe("highest", built.Count == 0 ? "none" : built.Max().ToString())
                .Observe("distribution", Histogram(built)).Observe("vanillaSkill20", Histogram(vanilla20))
                .Observe("missing", missing.Count == 0 ? "none" : string.Join(", ", missing))
                .Check(ok, "a skill-20 builder produced a wall above the Building group's Excellent bound, or a frame did not complete");
        }

        // Crafting: a Manufactured product from a skill-20 crafter stays within Awful..Excellent.
        var component = DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(d => d.HasComp(typeof(CompQuality)) && Bucket(d) == "Manufactured");
        ctx.Setup($"Manufactured def {component?.defName ?? "none"}");
        if (Kit.Require(ctx, "cq.manufactured-bound",
                (component != null, "a def in the Manufactured group has quality"),
                (component == null || MaxQuality(component) == (int)QualityCategory.Excellent, "the Manufactured bound is Excellent")))
        {
            var crafted = Enumerable.Range(0, Draws).Select(_ => Quality_Generator.GenerateQualityForDef(builder, SkillDefOf.Crafting, component, -1, null, false)).ToList();
            ctx.Begin("cq.manufactured-bound").Expect("highest", "<= Excellent").Expect("lowest", ">= Awful").Subject(builder)
                .Observe("def", component.defName).Observe("draws", Draws).Observe("distribution", Histogram(crafted)).Observe("vanillaSkill20", Histogram(vanilla20))
                .Check(crafted.All(q => q <= QualityCategory.Excellent), "a skill-20 crafter produced a component above the Manufactured group's Excellent bound");
        }

        // Harvesting: an Ingredient product from an unskilled grower is raised to at least Normal.
        var grower = Kit.Worker(ctx, At(3, 8), WorkTypeDefOf.Growing, SkillDefOf.Plants);
        grower.skills.GetSkill(SkillDefOf.Plants).Level = 0;
        var ingredient = DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(d => d.HasComp(typeof(CompQuality)) && Bucket(d) == "Ingredient");
        var vanilla0 = Vanilla(0);
        ctx.Setup($"Developer set {grower.LabelShort}'s Plants skill to 0; Ingredient def {ingredient?.defName ?? "none"}");
        if (Kit.Require(ctx, "cq.ingredient-minimum",
                (ingredient != null, "a def in the Ingredient group has quality"),
                (ingredient == null || MinQuality(ingredient) == (int)QualityCategory.Normal && MaxQuality(ingredient) == (int)QualityCategory.Excellent, "the Ingredient bounds are Normal..Excellent"),
                (vanilla0.Any(q => q < QualityCategory.Normal), $"vanilla skill-0 quality can fall below Normal ({Histogram(vanilla0)})")))
        {
            var harvested = Enumerable.Range(0, Draws).Select(_ => Quality_Generator.GenerateQualityForDef(grower, SkillDefOf.Plants, ingredient, -1, null, false)).ToList();
            ctx.Begin("cq.ingredient-minimum").Expect("lowest", ">= Normal").Expect("highest", "<= Excellent").Subject(grower)
                .Observe("def", ingredient.defName).Observe("draws", Draws).Observe("distribution", Histogram(harvested)).Observe("vanillaSkill0", Histogram(vanilla0))
                .Check(harvested.All(q => q >= QualityCategory.Normal && q <= QualityCategory.Excellent),
                    "an unskilled grower produced an Ingredient outside the group's Normal..Excellent bounds");
        }
        yield break;
    }
}
