using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QualityEverything
{
    public sealed class CompProperties_FermentationQualityTracker : CompProperties
    {
        public CompProperties_FermentationQualityTracker()
        {
            compClass = typeof(CompFermentationQualityTracker);
        }
    }

    public sealed class CompFermentationQualityTracker : ThingComp
    {
        private int inputQualityTotal;
        private int inputCount;

        public void Record(QualityCategory quality, int count)
        {
            if (count <= 0)
            {
                return;
            }

            inputQualityTotal += (int)quality * count;
            inputCount += count;
        }

        public bool TryGetCompletedBatchQuality(out QualityCategory quality)
        {
            quality = QualityCategory.Normal;
            if (inputCount != Building_FermentingBarrel.MaxCapacity)
            {
                return false;
            }

            int roundedQuality = (inputQualityTotal + inputCount / 2) / inputCount;
            quality = (QualityCategory)roundedQuality;
            return true;
        }

        public void Clear()
        {
            inputQualityTotal = 0;
            inputCount = 0;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref inputQualityTotal, "inputQualityTotal", 0);
            Scribe_Values.Look(ref inputCount, "inputCount", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit
                && (inputCount < 0
                    || inputCount > Building_FermentingBarrel.MaxCapacity
                    || inputQualityTotal < 0
                    || inputQualityTotal > inputCount * (int)QualityCategory.Legendary))
            {
                Log.Warning("QEverything: discarded invalid saved fermentation quality data.");
                Clear();
            }
        }
    }

    [StaticConstructorOnStartup]
    internal static class FermentationQualityTrackerStartup
    {
        static FermentationQualityTrackerStartup()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def?.thingClass == null
                    || !typeof(Building_FermentingBarrel).IsAssignableFrom(def.thingClass)
                    || def.HasComp(typeof(CompFermentationQualityTracker)))
                {
                    continue;
                }

                def.comps ??= new List<CompProperties>();
                def.comps.Add(new CompProperties_FermentationQualityTracker());
            }
        }
    }

    [HarmonyPatch(typeof(Building_FermentingBarrel), nameof(Building_FermentingBarrel.AddWort), new Type[] { typeof(Thing) })]
    internal static class FermentingBarrelAddWortPatch
    {
        private struct AddWortState
        {
            public CompFermentationQualityTracker Tracker;
            public QualityCategory Quality;
            public int SpaceBefore;
        }

        private static void Prefix(Building_FermentingBarrel __instance, Thing wort, ref AddWortState __state)
        {
            if (!ModSettings_QEverything.useMaterialQuality || wort == null)
            {
                return;
            }

            CompQuality inputQuality = wort.TryGetComp<CompQuality>();
            CompFermentationQualityTracker tracker = __instance.TryGetComp<CompFermentationQualityTracker>();
            if (inputQuality == null || tracker == null)
            {
                return;
            }

            __state.Tracker = tracker;
            __state.Quality = inputQuality.Quality;
            __state.SpaceBefore = __instance.SpaceLeftForWort;
        }

        private static void Postfix(Building_FermentingBarrel __instance, AddWortState __state)
        {
            if (__state.Tracker == null)
            {
                return;
            }

            int acceptedCount = __state.SpaceBefore - __instance.SpaceLeftForWort;
            __state.Tracker.Record(__state.Quality, acceptedCount);
        }
    }

    [HarmonyPatch(typeof(Building_FermentingBarrel), nameof(Building_FermentingBarrel.TakeOutBeer))]
    internal static class FermentingBarrelTakeOutBeerPatch
    {
        private struct TakeOutBeerState
        {
            public bool ApplyQuality;
            public QualityCategory Quality;
        }

        private static void Prefix(Building_FermentingBarrel __instance, ref TakeOutBeerState __state)
        {
            if (!ModSettings_QEverything.useMaterialQuality)
            {
                return;
            }

            CompFermentationQualityTracker tracker = __instance.TryGetComp<CompFermentationQualityTracker>();
            __state.ApplyQuality = tracker != null && tracker.TryGetCompletedBatchQuality(out __state.Quality);
        }

        private static void Postfix(Thing __result, TakeOutBeerState __state)
        {
            if (!__state.ApplyQuality || __result == null)
            {
                return;
            }

            CompQuality outputQuality = __result.TryGetComp<CompQuality>();
            outputQuality?.SetQuality(__state.Quality, ArtGenerationContext.Colony);
        }
    }

    [HarmonyPatch(typeof(Building_FermentingBarrel), "Reset")]
    internal static class FermentingBarrelResetPatch
    {
        private static void Postfix(Building_FermentingBarrel __instance)
        {
            __instance.TryGetComp<CompFermentationQualityTracker>()?.Clear();
        }
    }
}
