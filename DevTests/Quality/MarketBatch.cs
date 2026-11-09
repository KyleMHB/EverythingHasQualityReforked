using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (docs/quality-guide.md "Market value and colony wealth"): price scaling stays on by default. Turning it
// off for an item removes both quality premiums and discounts, for quality this mod adds and for standard quality on eligible
// vanilla items, while material and other price modifiers still apply. Building wealth uses MarketValueIgnoreHp.
// Mod set "market" loads DevTests/modsettings/market: the Progression preset with market-value item exceptions that turn
// scaling off for Apparel_Duster (vanilla quality) and Wall (quality added by this mod). Gun_Revolver, Steel and Door keep the
// default. Mechanical: things are made with each quality and their stats read; nothing is traded.
public class MarketBatch : Batch
{
    public override string Id => "cq-market";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[]
    {
        "cq.market-off-vanilla", "cq.market-off-added", "cq.market-default-vanilla", "cq.market-default-added", "cq.market-default-building",
    }.Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    private static readonly QualityCategory[] Levels3 = { QualityCategory.Awful, QualityCategory.Normal, QualityCategory.Legendary };

    private static float[] Values(string def, string stuff, StatDef stat) =>
        Levels3.Select(q => CqKit.Make(def, stuff, q).GetStatValue(stat, applyPostProcess: true)).ToArray();

    private static string Join(float[] v) => string.Join("/", v.Select(f => f.ToString("0.##")));

    private static void Flat(BatchContext ctx, string id, string def, string stuff, params StatDef[] stats)
    {
        if (!Kit.Require(ctx, id, (CqKit.HasQuality(def), def + " has quality"))) return;
        var c = ctx.Begin(id);
        bool flat = true;
        foreach (var stat in stats)
        {
            var v = Values(def, stuff, stat);
            flat &= v.All(x => System.Math.Abs(x - v[1]) < 0.01f);
            c.Expect(stat.defName + " Awful/Normal/Legendary", "all equal").Observe(stat.defName + " Awful/Normal/Legendary", Join(v));
        }
        c.Check(flat, $"{def}'s market value still changes with quality although its price scaling is off");
    }

    private static void Scaled(BatchContext ctx, string id, string def, string stuff, StatDef stat)
    {
        var v = Values(def, stuff, stat);
        if (!Kit.Require(ctx, id, (CqKit.HasQuality(def), def + " has quality"))) return;
        ctx.Begin(id).Expect(stat.defName + " Awful/Normal/Legendary", "increasing").Observe(stat.defName, Join(v))
            .Check(v[0] < v[1] && v[1] < v[2], $"{def}'s {stat.defName} does not scale with quality although its price scaling is on");
    }

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var ignoreHp = DefDatabase<StatDef>.GetNamed("MarketValueIgnoreHp");
        Flat(ctx, "cq.market-off-vanilla", "Apparel_Duster", "Cloth", StatDefOf.MarketValue);
        Flat(ctx, "cq.market-off-added", "Wall", "Steel", StatDefOf.MarketValue, ignoreHp);
        Scaled(ctx, "cq.market-default-vanilla", "Gun_Revolver", null, StatDefOf.MarketValue);
        Scaled(ctx, "cq.market-default-added", "Steel", null, StatDefOf.MarketValue);
        // Guard: the same quality parts scale an assigned building whose rule is left on.
        Scaled(ctx, "cq.market-default-building", "Door", "Steel", ignoreHp);
        yield break;
    }
}
