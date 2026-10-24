using HarmonyLib;
using RimWorld;
using Verse;

namespace ConfigurableQuality
{
    internal static class QualityMarketValue
    {
        internal static bool Suppress(StatPart_Quality part, StatRequest req)
        {
            // Building wealth uses MarketValueIgnoreHp rather than the displayed MarketValue.
            string stat = part.parentStat?.defName;
            if (stat != "MarketValue" && stat != "MarketValueIgnoreHp") return false;
            var def = req.Def as ThingDef;
            var entry = QualityCatalog.Get(def);
            return entry != null && (entry.RemovableVanillaQuality || (!entry.NativeQuality && QualityCatalog.Assigned(def)))
                && !QualityCatalog.ScalesMarketValue(def);
        }

        // Skip only the native quality part, retaining condition, materials and other price modifiers.
        [HarmonyPatch(typeof(StatPart_Quality), nameof(StatPart_Quality.TransformValue))]
        private static class ValuePatch
        {
            private static bool Prefix(StatPart_Quality __instance, StatRequest req) => !Suppress(__instance, req);
        }

        [HarmonyPatch(typeof(StatPart_Quality), nameof(StatPart_Quality.ExplanationPart))]
        private static class ExplanationPatch
        {
            private static bool Prefix(StatPart_Quality __instance, StatRequest req, ref string __result)
            {
                if (!Suppress(__instance, req)) return true;
                __result = null;
                return false;
            }
        }
    }
}
