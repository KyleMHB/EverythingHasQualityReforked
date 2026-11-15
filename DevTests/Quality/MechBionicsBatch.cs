using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (CHANGELOG 2026-11-15; Source/Quality_Mechanoids.cs, Source/Quality_Bionics.cs). Mod set "mechbionics" (Core,
// Biotech) loads the Progression settings plus the six mechanoid and installed-part effects on with Mild factors
// .95/.98/1/1.02/1.05/1.10/1.20, and the Prosthetics and bionics group on.
// - The four mechanoid effects and part efficiency are available. Part hit points says it needs Elite Bionics Framework (not loaded).
// - A generated mech gets a quality. Its own MoveSpeed follows the curve; MeleeCooldownFactor, where lower is better, is divided by it.
// - Installing a Legendary bionic arm (vanilla installer, no surgeon, so no failure roll) gives the installed part Legendary, and the
//   shoulder's efficiency is the arm's partEfficiency x1.2. Installing a Normal arm over it returns the old arm as a Legendary item
//   through the vanilla removal path, and the new part reads x1.
// - After a save and reload, a spawned Masterwork mech and a Good installed arm keep their quality, and the arm still reads x1.02.
// - The effect list names the missing mods: part hit points reads "Needs Elite Bionics Framework" and the Combat Extended effects
//   "Needs Combat Extended". The case opens the advanced editor on each for a few seconds so the window can be captured.
// Mechanical: calls the real installer and removal code on a spawned colonist. Not covered: gestation, Elite Bionics Framework,
// Quality Mechanoids and Quality Bionics Remastered.
public class MechBionicsBatch : Batch
{
    public override string Id => "cq-mechbionics";
    public override int SegmentCount => 2;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[]
    {
        "cq.mechbionics-available", "cq.mech-generated", "cq.mech-stat", "cq.bionic-install", "cq.bionic-reinstall", "cq.mechbionics-reload", "cq.mechbionics-labels",
    }.Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    private static readonly Type EffectId = AccessTools.TypeByName("ConfigurableQuality.EffectId");
    private static readonly Type Effects = AccessTools.TypeByName("ConfigurableQuality.QualityEffects");
    private static readonly Type MechComp = AccessTools.TypeByName("ConfigurableQuality.CompMechQuality");
    private static readonly Type PartComp = AccessTools.TypeByName("ConfigurableQuality.HediffComp_PartQuality");

    private static object Effect(string id) => Enum.Parse(EffectId, id);
    private static T Call<T>(string method, string id) => (T)AccessTools.Method(Effects, method).Invoke(null, new[] { Effect(id) });
    private static readonly float[] Mild = CqKit.MildBenefit;

    private static ThingComp MechQuality(Pawn pawn) => pawn.AllComps.FirstOrDefault(c => c.GetType() == MechComp);
    private static HediffComp PartQuality(Hediff hediff) => (hediff as HediffWithComps)?.comps?.FirstOrDefault(c => c.GetType() == PartComp);
    private static bool IsSet(object comp) => comp != null && (bool)AccessTools.Property(comp.GetType(), "IsSet").GetValue(comp);
    private static QualityCategory Quality(object comp) => (QualityCategory)AccessTools.Property(comp.GetType(), "Quality").GetValue(comp);
    private static void SetQuality(object comp, QualityCategory quality) => AccessTools.Property(comp.GetType(), "Quality").SetValue(comp, quality);

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        if (index == 1)
        {
            Reloaded(ctx);
            yield break;
        }
        Available(ctx);
        foreach (var wait in Labels(ctx)) yield return wait;
        Mech(ctx);
        Kit.RemoveStartingColonists(ctx);
        var area = Kit.Area(ctx, 5, 5);
        var pawn = Kit.Worker(ctx, area.CenterCell, WorkTypeDefOf.Hauling);
        yield return Wait.Ticks(2);
        if (!Bionics(ctx, pawn)) yield break;

        var mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Mech_Lancer"), Faction.OfPlayer));
        GenSpawn.Spawn(mech, area.Min + new IntVec3(1, 0, 1), ctx.Map);
        SetQuality(MechQuality(mech), QualityCategory.Masterwork);
        var arm = pawn.health.hediffSet.hediffs.First(h => h.def.defName == "BionicArm");
        SetQuality(PartQuality(arm), QualityCategory.Good);
        ctx.Setup($"Developer spawned a Masterwork {mech.LabelShort} and set {pawn.LabelShort}'s installed arm to Good");
        ctx.Remember("mech", mech.ThingID);
        ctx.Remember("colonist", pawn.ThingID);
        yield return Wait.Reload();
    }

    private static void Reloaded(BatchContext ctx)
    {
        var pawns = ctx.Map.mapPawns.AllPawns;
        var mech = pawns.FirstOrDefault(p => p.ThingID == ctx.Recall("mech"));
        var colonist = pawns.FirstOrDefault(p => p.ThingID == ctx.Recall("colonist"));
        var mechComp = mech == null ? null : MechQuality(mech);
        var arm = colonist?.health.hediffSet.hediffs.FirstOrDefault(h => h.def.defName == "BionicArm");
        var armComp = arm == null ? null : PartQuality(arm);
        float expected = arm == null ? 0f : arm.def.addedPartProps.partEfficiency * Mild[(int)QualityCategory.Good];
        float efficiency = arm == null ? -1f : PawnCapacityUtility.CalculatePartEfficiency(colonist.health.hediffSet, arm.Part);
        string Show(object comp) => comp == null ? "missing" : IsSet(comp) ? Quality(comp).ToString() : "unset";
        ctx.Begin("cq.mechbionics-reload").Expect("mech", "Masterwork").Expect("arm", "Good").Expect("arm efficiency", expected.ToString("0.###"))
            .Observe("mech", Show(mechComp)).Observe("arm", Show(armComp)).Observe("arm efficiency", efficiency.ToString("0.###"))
            .Check(IsSet(mechComp) && Quality(mechComp) == QualityCategory.Masterwork && IsSet(armComp) && Quality(armComp) == QualityCategory.Good && Kit.Near(efficiency, expected, 0.001f),
                "the mech or the installed arm lost its quality across save and reload");
    }

    private static void Available(BatchContext ctx)
    {
        string[] on = { "MechWork", "MechCombat", "MechArmor", "MechEnergy", "BionicEfficiency" };
        var c = ctx.Begin("cq.mechbionics-available").Expect("available", string.Join(",", on)).Expect("BionicHitPoints", "unavailable, nothing to act on");
        foreach (var id in on.Append("BionicHitPoints"))
            c.Observe(id, $"available={Call<bool>("Available", id)} noItems={Call<bool>("NoItems", id)} elsewhere={Call<bool>("HandledElsewhere", id)} reason={Call<string>("Reason", id)}");
        var off = on.Where(id => !Call<bool>("Available", id)).ToList();
        c.Check(off.Count == 0 && !Call<bool>("Available", "BionicHitPoints") && Call<bool>("NoItems", "BionicHitPoints"),
            $"mechanoid or part effects are not registered as expected (unavailable: {string.Join(",", off)})");
    }

    // Opens the advanced editor on each effect and holds it for a few seconds. The engine's own screenshot call writes nothing in 1.7,
    // so a "label-ready" marker in the run folder lets an outside window capture take the picture for a person to look at.
    private static IEnumerable<Wait> Labels(BatchContext ctx)
    {
        var editorType = AccessTools.TypeByName("ConfigurableQuality.Window_QualityOptions");
        string pointer = Path.Combine(TestRunner.EvidenceRoot, "active.txt");
        string runDir = File.Exists(pointer) ? File.ReadAllText(pointer).Trim() : TestRunner.EvidenceRoot;
        var c = ctx.Begin("cq.mechbionics-labels").Expect("BionicHitPoints", "Needs Elite Bionics Framework").Expect("CeRange", "Needs Combat Extended");
        var labels = new Dictionary<string, string>();
        foreach (var (id, search) in new[] { ("BionicHitPoints", "Installed bionics"), ("CeRange", "Combat Extended") })
        {
            labels[id] = Call<string>("StatusLabel", id);
            c.Observe(id, labels[id]);
            var editor = (Window)Activator.CreateInstance(editorType);
            Find.WindowStack.Add(editor);
            AccessTools.Method(editorType, "SelectEffect").Invoke(editor, new[] { Effect(id) });
            AccessTools.Field(editorType, "effectSearch").SetValue(editor, search);
            yield return Wait.Frames(5);
            File.WriteAllText(Path.Combine(runDir, "label-ready-" + id + ".txt"), id);
            yield return Wait.Seconds(5);
            Find.WindowStack.TryRemove(editor, false);
            yield return Wait.Frames(2);
        }
        c.Check(labels["BionicHitPoints"] == "Needs Elite Bionics Framework" && labels["CeRange"] == "Needs Combat Extended",
            "the effect list does not name the missing mod");
    }

    private static void Mech(BatchContext ctx)
    {
        var kind = DefDatabase<PawnKindDef>.GetNamed("Mech_Lancer");
        var mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, Faction.OfMechanoids));
        var comp = MechQuality(mech);
        ctx.Setup($"Developer generated an unspawned {mech.LabelShort}");
        ctx.Begin("cq.mech-generated").Subject(mech).Expect("comp", "present and set").Observe("comp", comp == null ? "missing" : IsSet(comp) ? "set " + Quality(comp) : "unset")
            .Check(IsSet(comp), "a generated mech got no quality while the mechanoid effects are on");
        if (!Kit.Require(ctx, "cq.mech-stat", (comp != null, "the mech has the quality comp"))) return;
        var levels = new[] { QualityCategory.Awful, QualityCategory.Normal, QualityCategory.Legendary };
        var move = new List<float>();
        var cooldown = new List<float>();
        foreach (var level in levels)
        {
            SetQuality(comp, level);
            move.Add(CqKit.Contribution(mech, StatDefOf.MoveSpeed));
            cooldown.Add(CqKit.Contribution(mech, DefDatabase<StatDef>.GetNamed("MeleeCooldownFactor")));
        }
        var expectMove = levels.Select(l => Mild[(int)l]).ToArray();
        var expectCooldown = expectMove.Select(f => 1f / f).ToArray();
        string Join(IEnumerable<float> v) => string.Join("/", v.Select(f => f.ToString("0.###")));
        ctx.Begin("cq.mech-stat").Subject(mech).Expect("MoveSpeed", Join(expectMove)).Expect("MeleeCooldownFactor", Join(expectCooldown))
            .Observe("MoveSpeed", Join(move)).Observe("MeleeCooldownFactor", Join(cooldown))
            .Check(move.Zip(expectMove, (a, b) => Kit.Near(a, b, 0.001f)).All(x => x) && cooldown.Zip(expectCooldown, (a, b) => Kit.Near(a, b, 0.001f)).All(x => x),
                "the mech's own stats do not follow its quality");
        mech.Destroy();
    }

    private static bool Bionics(BatchContext ctx, Pawn pawn)
    {
        var hediffDef = DefDatabase<HediffDef>.GetNamed("BionicArm");
        var item = hediffDef.spawnThingOnRemoved;
        var recipe = DefDatabase<RecipeDef>.AllDefsListForReading.FirstOrDefault(r => r.addsHediff == hediffDef && r.Worker is Recipe_InstallArtificialBodyPart);
        var part = recipe == null ? null : pawn.RaceProps.body.AllParts.FirstOrDefault(p => recipe.appliedOnFixedBodyParts.Contains(p.def));
        if (!Kit.Require(ctx, "cq.bionic-install", (item.HasComp(typeof(CompQuality)), item.defName + " has quality"), (recipe != null, "an install recipe for the bionic arm exists"),
                (part != null, "the colonist has a shoulder")))
        {
            Kit.Require(ctx, "cq.bionic-reinstall", (false, "the install case could not run"));
            Kit.Require(ctx, "cq.mechbionics-reload", (false, "the install case could not run"));
            return false;
        }
        float baseEfficiency = hediffDef.addedPartProps.partEfficiency;
        var legendary = CqKit.Make(item.defName, null, QualityCategory.Legendary);
        recipe.Worker.ApplyOnPawn(pawn, part, null, new List<Thing> { legendary }, null);
        ctx.Setup($"Developer installed a Legendary {item.label} on {pawn.LabelShort}'s {part.Label} with no surgeon");
        var installed = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == hediffDef && h.Part == part);
        var comp = installed == null ? null : PartQuality(installed);
        float efficiency = PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, part);
        float expected = baseEfficiency * Mild[(int)QualityCategory.Legendary];
        ctx.Begin("cq.bionic-install").Subject(pawn).Expect("part quality", "Legendary").Expect("efficiency", expected.ToString("0.###"))
            .Observe("part quality", comp == null ? "no comp" : IsSet(comp) ? Quality(comp).ToString() : "unset").Observe("efficiency", efficiency.ToString("0.###"))
            .Check(IsSet(comp) && Quality(comp) == QualityCategory.Legendary && Kit.Near(efficiency, expected, 0.001f), "the installed arm did not take the item's quality or its efficiency");

        var normal = CqKit.Make(item.defName, null, QualityCategory.Normal);
        var before = pawn.Map.listerThings.ThingsOfDef(item).ToList();
        recipe.Worker.ApplyOnPawn(pawn, part, null, new List<Thing> { normal }, null);
        ctx.Setup($"Developer installed a Normal {item.label} over the Legendary one");
        var returned = pawn.Map.listerThings.ThingsOfDef(item).Except(before).ToList();
        var replaced = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == hediffDef && h.Part == part);
        var newComp = replaced == null ? null : PartQuality(replaced);
        float after = PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, part);
        var quality = returned.Select(t => t.TryGetComp<CompQuality>()?.Quality.ToString() ?? "none").ToList();
        ctx.Begin("cq.bionic-reinstall").Subject(pawn).Expect("returned item", "one Legendary").Expect("new part", "Normal, efficiency " + baseEfficiency.ToString("0.###"))
            .Observe("returned item", string.Join(",", quality)).Observe("new part", (newComp == null ? "no comp" : Quality(newComp).ToString()) + ", efficiency " + after.ToString("0.###"))
            .Check(quality.Count == 1 && quality[0] == "Legendary" && IsSet(newComp) && Quality(newComp) == QualityCategory.Normal && Kit.Near(after, baseEfficiency, 0.001f),
                "removing the arm did not return its quality, or the new arm did not take its own");
        foreach (var thing in returned) thing.Destroy();
        return true;
    }
}
