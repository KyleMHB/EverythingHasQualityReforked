using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;
using HarmonyLib;

namespace QualityEverything
{
    [HarmonyPatch]
    public static class Quality_CompPatch
    {
        [HarmonyPatch(typeof(CompQuality), "SetQuality")]
        [HarmonyPrefix]
        public static void ApplyQualityLimits(CompQuality __instance, ref QualityCategory q)
        {
            if (__instance?.parent == null)
            {
                return;
            }

            int minQuality = QualityDefUtility.GetMinQuality(__instance.parent.def);
            int maxQuality = QualityDefUtility.GetMaxQuality(__instance.parent.def);
            if ((int)q < minQuality) q = (QualityCategory)minQuality;
            if ((int)q > maxQuality) q = (QualityCategory)maxQuality;
        }

        public static void DefPatch()
        {
            QualityCompSync.SyncAllDefs();
        }

        public static void ApplyNewQuality()
        {
            if (Current.Game == null)
            {
                return;
            }

            MethodInfo initializeComps = AccessTools.Method(typeof(ThingWithComps), "InitializeComps");

            foreach (Map map in Find.Maps)
            {
                foreach (Thing thing in map.listerThings.AllThings)
                {
                    if (!(thing is ThingWithComps thingWithComps))
                    {
                        continue;
                    }

                    if (thing.TryGetComp<CompQuality>() != null || !thing.def.HasComp(typeof(CompQuality)))
                    {
                        continue;
                    }

                    initializeComps?.Invoke(thingWithComps, null);

                    CompQuality comp = thing.TryGetComp<CompQuality>();
                    if (comp == null)
                    {
                        continue;
                    }

                    int quality = Mathf.Clamp(
                        (int)QualityCategory.Normal,
                        QualityDefUtility.GetMinQuality(thing.def),
                        QualityDefUtility.GetMaxQuality(thing.def));
                    comp.SetQuality((QualityCategory)quality, ArtGenerationContext.Outsider);
                }
            }
        }
    }
}
