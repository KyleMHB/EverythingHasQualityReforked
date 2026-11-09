using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

internal static class CqKit
{
    public const string HarmonyId = "kylemhb.configurablequality";
    public static readonly string[] AddableDefs = { "Wall", "Steel", "MealSimple", "MedicineIndustrial" };
    public static readonly string[] NativeDefs = { "Apparel_Duster", "Gun_Revolver" };
    // docs/quality-guide.md: Mild benefit factors for Awful through Legendary.
    public static readonly float[] MildBenefit = { .95f, .98f, 1f, 1.02f, 1.05f, 1.10f, 1.20f };

    private static System.Reflection.Assembly Assembly => Harness.Settings.ProductionType.Assembly;

    public static bool HasQuality(string def) => Kit.Def(def).HasComp(typeof(CompQuality));

    public static string QualityDefs(IEnumerable<string> defs) => string.Join(", ", defs.Select(d => $"{d}={HasQuality(d)}"));

    public static int PatchedMethods() =>
        Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m)?.Owners.Any(o => o.StartsWith(HarmonyId)) == true);

    public static List<StatPart> Parts(StatDef stat) => (stat.parts ?? new List<StatPart>()).Where(p => p.GetType().Assembly == Assembly).ToList();

    // Configurable Quality's own contribution to a stat: 100 transformed by only this mod's stat parts.
    public static float Contribution(Thing thing, StatDef stat)
    {
        float value = 100f;
        var req = StatRequest.For(thing);
        foreach (var part in Parts(stat)) part.TransformValue(req, ref value);
        return value / 100f;
    }

    public static Thing Make(string def, string stuff, QualityCategory quality)
    {
        var thing = ThingMaker.MakeThing(Kit.Def(def), stuff == null ? null : Kit.Def(stuff));
        thing.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Colony);
        return thing;
    }
}
