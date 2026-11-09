using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (docs/quality-guide.md, README): "A fresh installation leaves gameplay unchanged. Quality assignment,
// effects, production changes and vanilla removal start disabled." Mod set "default" starts with no mod settings file.
// Mechanical: defs are inspected and unspawned items are made directly; no production or UI is exercised.
public class FreshBatch : Batch
{
    public override string Id => "cq-fresh";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { "cq.loaded", "cq.fresh-assignment", "cq.fresh-effects" }
        .Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        int patched = CqKit.PatchedMethods(), parts = CqKit.Parts(StatDefOf.MaxHitPoints).Count;
        ctx.Begin("cq.loaded").Expect("patchedMethods", "> 0").Expect("maxHitPointsParts", "> 0")
            .Observe("patchedMethods", patched).Observe("maxHitPointsParts", parts).Observe("activeMods", ModsConfig.ActiveModsInLoadOrder.Count())
            .Check(patched > 0 && parts > 0, "Configurable Quality's Harmony patches or stat parts are missing");

        var added = CqKit.AddableDefs.Where(CqKit.HasQuality).ToList();
        var lost = CqKit.NativeDefs.Where(d => !CqKit.HasQuality(d)).ToList();
        ctx.Begin("cq.fresh-assignment").Expect("addedQuality", "none").Expect("nativeQualityLost", "none")
            .Observe("addedQuality", added.Count == 0 ? "none" : string.Join(", ", added))
            .Observe("nativeQualityLost", lost.Count == 0 ? "none" : string.Join(", ", lost))
            .Observe("defs", CqKit.QualityDefs(CqKit.AddableDefs.Concat(CqKit.NativeDefs)))
            .Check(added.Count == 0 && lost.Count == 0, "a fresh installation added quality to a non-quality def or removed native quality");

        // Native-quality items at both ends of the scale: the mod's own stat parts must not change their hit points.
        var readings = new List<string>();
        bool neutral = true;
        foreach (var def in CqKit.NativeDefs)
            foreach (var q in new[] { QualityCategory.Awful, QualityCategory.Legendary })
            {
                float factor = CqKit.Contribution(CqKit.Make(def, def == "Apparel_Duster" ? "Cloth" : null, q), StatDefOf.MaxHitPoints);
                readings.Add($"{def} {q} x{factor:0.###}");
                neutral &= Kit.Near(factor, 1f, 0.0001f);
            }
        ctx.Begin("cq.fresh-effects").Expect("maxHitPointsFactor", "x1 for every reading").Observe("readings", string.Join("; ", readings))
            .Check(neutral, "a durability effect changed hit points on a fresh installation");
        yield break;
    }
}
