using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;
using static ConfigurableQuality.DevTests.MechBionicsBatch;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (CHANGELOG 2026-11-15; Source/Quality_Bionics.cs RegisterHitPoints). Mod set "ebf" (Core, HugsLib, Elite Bionics
// Framework) loads the mechbionics settings: part hit points on with Mild factors .95/.98/1/1.02/1.05/1.10/1.20.
// - With Elite Bionics Framework loaded, the part hit points effect is available.
// - A colonist's shoulder carrying an installed bionic arm has EBF's maximum hit points scaled by the arm's quality: Legendary and
//   Awful give the Normal value x1.2 and x0.95, rounded to a whole number and at least 1.
// Mechanical: installs through the vanilla installer and reads EBF's EBFEndpoints.GetMaxHealthWithEBF uncached. Not covered: the
// health tab, damage taken against the new maximum, and EBF's older VanillaExtender route.
public class EbfBatch : Batch
{
    public override string Id => "cq-ebf";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { "cq.ebf-available", "cq.ebf-hitpoints" }
        .Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        bool available = Call<bool>("Available", "BionicHitPoints");
        ctx.Begin("cq.ebf-available").Expect("BionicHitPoints", "available")
            .Observe("BionicHitPoints", $"available={available} reason={Call<string>("Reason", "BionicHitPoints")}")
            .Check(available, "part hit points is not available with Elite Bionics Framework loaded");

        Kit.RemoveStartingColonists(ctx);
        var area = Kit.Area(ctx, 5, 5);
        var pawn = Kit.Worker(ctx, area.CenterCell, WorkTypeDefOf.Hauling);
        yield return Wait.Ticks(2);

        var hediffDef = DefDatabase<HediffDef>.GetNamed("BionicArm");
        var recipe = DefDatabase<RecipeDef>.AllDefsListForReading.FirstOrDefault(r => r.addsHediff == hediffDef && r.Worker is Recipe_InstallArtificialBodyPart);
        var part = recipe == null ? null : pawn.RaceProps.body.AllParts.FirstOrDefault(p => recipe.appliedOnFixedBodyParts.Contains(p.def));
        var endpoint = AccessTools.Method(AccessTools.TypeByName("EBF.EBFEndpoints"), "GetMaxHealthWithEBF", new[] { typeof(BodyPartRecord), typeof(Pawn), typeof(bool) });
        if (!Kit.Require(ctx, "cq.ebf-hitpoints", (recipe != null, "an install recipe for the bionic arm exists"), (part != null, "the colonist has a shoulder"),
                (endpoint != null, "EBF has EBFEndpoints.GetMaxHealthWithEBF"))) yield break;

        recipe.Worker.ApplyOnPawn(pawn, part, null, new List<Thing> { CqKit.Make(hediffDef.spawnThingOnRemoved.defName, null, QualityCategory.Normal) }, null);
        ctx.Setup($"Developer installed a {hediffDef.label} on {pawn.LabelShort}'s {part.Label} with no surgeon");
        var comp = PartQuality(pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == hediffDef && h.Part == part));
        if (!Kit.Require(ctx, "cq.ebf-hitpoints", (comp != null, "the installed arm has the part quality comp"))) yield break;
        float MaxHealth(QualityCategory quality)
        {
            SetQuality(comp, quality);
            return (float)endpoint.Invoke(null, new object[] { part, pawn, false });
        }
        float normal = MaxHealth(QualityCategory.Normal), legendary = MaxHealth(QualityCategory.Legendary), awful = MaxHealth(QualityCategory.Awful);
        float Expected(QualityCategory quality) => Math.Max(1f, (float)Math.Round(normal * CqKit.MildBenefit[(int)quality]));
        ctx.Begin("cq.ebf-hitpoints").Subject(pawn)
            .Expect("Awful / Normal / Legendary", $"{Expected(QualityCategory.Awful)} / {normal} / {Expected(QualityCategory.Legendary)}")
            .Observe("Awful / Normal / Legendary", $"{awful} / {normal} / {legendary}")
            .Check(Kit.Near(awful, Expected(QualityCategory.Awful), 0.51f) && Kit.Near(legendary, Expected(QualityCategory.Legendary), 0.51f) && legendary > normal && awful < normal,
                "EBF's maximum hit points for the shoulder do not follow the installed arm's quality");
    }
}
