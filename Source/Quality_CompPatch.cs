using RimWorld;
using Verse;
using HarmonyLib;

namespace ConfigurableQuality
{
    [HarmonyPatch]
    public static class Quality_CompPatch
    {
        [HarmonyPatch(typeof(CompQuality), "SetQuality")]
        [HarmonyPrefix]
        public static void ApplyQualityLimits(CompQuality __instance, ref QualityCategory q)
        {
            if (QualityNativeOutput.PreserveBounds || __instance?.parent == null || !QualityDefUtility.ShouldReceiveQuality(__instance.parent.def)) return;
            int minQuality = QualityDefUtility.GetMinQuality(__instance.parent.def);
            int maxQuality = QualityDefUtility.GetMaxQuality(__instance.parent.def);
            if ((int)q < minQuality) q = (QualityCategory)minQuality;
            if ((int)q > maxQuality) q = (QualityCategory)maxQuality;
        }

        public static void DefPatch() { QualityCompSync.SyncAllDefs(); }

        // Kept for callers of the previous API. Component changes now happen at startup;
        // InitializeComps would replace unrelated components and their saved state.
        public static void ApplyNewQuality() { }
    }
}
