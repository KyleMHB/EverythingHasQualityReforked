using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;

namespace QualityEverything
{
    internal enum QualityBucket
    {
        None,
        Stuff,
        WorkBuilding,
        SecurityBuilding,
        Building,
        Weapon,
        Shell,
        Apparel,
        Drug,
        Medicine,
        Meal,
        TastyIngredient,
        Ingredient,
        Manufactured
    }

    internal static class QualityDefUtility
    {
        private static readonly ThingCategoryDef BuildingsSecurityCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BuildingsSecurity");
        private static readonly ThingCategoryDef BuildingsFurnitureCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BuildingsFurniture");
        private static readonly ThingCategoryDef BuildingsProductionCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BuildingsProduction");
        private static readonly ThingCategoryDef GrenadesCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Grenades");

        public static bool HasCategory(ThingDef def, ThingCategoryDef category)
        {
            return def != null && category != null && def.thingCategories != null && def.IsWithinCategory(category);
        }

        public static bool IsExcludedFromQuality(ThingDef def)
        {
            return def == null || def.plant != null || def.IsCorpse || def.race != null;
        }

        public static bool IsBuildingFurnitureOrProduction(ThingDef def)
        {
            return HasCategory(def, BuildingsFurnitureCategory)
                || HasCategory(def, BuildingsProductionCategory)
                || HasCategory(def, ThingCategoryDefOf.BuildingsArt);
        }

        public static bool IsFoodPartition(ThingDef def)
        {
            return def != null && !def.IsDrug && def.IsNutritionGivingIngestible;
        }

        public static bool IsWeaponLike(ThingDef def)
        {
            return def != null
                && !def.IsIngestible
                && (def.IsWeapon || def.IsShell || HasCategory(def, GrenadesCategory));
        }

        public static bool IsBuildingQualityCandidate(ThingDef def)
        {
            return def != null
                && def.building != null
                && def.Claimable
                && !def.IsBlueprint
                && !def.IsFrame;
        }

        public static bool IsOtherCustomizationCandidate(ThingDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (!(HasCategory(def, ThingCategoryDefOf.Manufactured) || def.IsDrug || def.IsMedicine || def.IsIngestible))
            {
                return false;
            }

            return !def.IsStuff
                && !def.IsCorpse
                && def.plant == null
                && !def.IsShell
                && !def.IsApparel
                && !IsWeaponLike(def)
                && def.building == null;
        }

        public static QualityBucket GetBucket(ThingDef def)
        {
            if (IsExcludedFromQuality(def))
            {
                return QualityBucket.None;
            }

            if (def.IsStuff)
            {
                return QualityBucket.Stuff;
            }

            if (IsBuildingQualityCandidate(def))
            {
                if (def.IsWorkTable)
                {
                    return QualityBucket.WorkBuilding;
                }

                if (HasCategory(def, BuildingsSecurityCategory) || def.building.IsTurret)
                {
                    return QualityBucket.SecurityBuilding;
                }

                return QualityBucket.Building;
            }

            if (IsWeaponLike(def))
            {
                return def.IsShell ? QualityBucket.Shell : QualityBucket.Weapon;
            }

            if (def.IsApparel)
            {
                return QualityBucket.Apparel;
            }

            if (def.IsDrug)
            {
                return QualityBucket.Drug;
            }

            if (def.IsMedicine)
            {
                return QualityBucket.Medicine;
            }

            if (def.ingestible != null)
            {
                if (HasCategory(def, ThingCategoryDefOf.Foods))
                {
                    return QualityBucket.Meal;
                }

                if (def.IsNutritionGivingIngestible || def.IsAnimalProduct)
                {
                    return def.ingestible.preferability == FoodPreferability.RawTasty
                        ? QualityBucket.TastyIngredient
                        : QualityBucket.Ingredient;
                }
            }

            if (HasCategory(def, ThingCategoryDefOf.Manufactured))
            {
                return QualityBucket.Manufactured;
            }

            return QualityBucket.None;
        }

        public static bool ShouldReceiveQuality(ThingDef def)
        {
            string defName = def?.defName;
            if (def == null || string.IsNullOrEmpty(defName))
            {
                return false;
            }

            switch (GetBucket(def))
            {
                case QualityBucket.Stuff:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivStuff,
                        ModSettings_QEverything.stuffDict,
                        defName,
                        ModSettings_QEverything.stuffQuality);

                case QualityBucket.WorkBuilding:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivBuildings,
                        ModSettings_QEverything.bldgDict,
                        defName,
                        ModSettings_QEverything.workQuality);

                case QualityBucket.SecurityBuilding:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivBuildings,
                        ModSettings_QEverything.bldgDict,
                        defName,
                        ModSettings_QEverything.securityQuality);

                case QualityBucket.Building:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivBuildings,
                        ModSettings_QEverything.bldgDict,
                        defName,
                        ModSettings_QEverything.edificeQuality);

                case QualityBucket.Weapon:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivWeapons,
                        ModSettings_QEverything.weapDict,
                        defName,
                        ModSettings_QEverything.weaponQuality);

                case QualityBucket.Shell:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivWeapons,
                        ModSettings_QEverything.weapDict,
                        defName,
                        ModSettings_QEverything.shellQuality);

                case QualityBucket.Apparel:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivApparel,
                        ModSettings_QEverything.appDict,
                        defName,
                        ModSettings_QEverything.apparelQuality);

                case QualityBucket.Drug:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivOther,
                        ModSettings_QEverything.otherDict,
                        defName,
                        ModSettings_QEverything.drugQuality);

                case QualityBucket.Medicine:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivOther,
                        ModSettings_QEverything.otherDict,
                        defName,
                        ModSettings_QEverything.medQuality);

                case QualityBucket.Meal:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivOther,
                        ModSettings_QEverything.otherDict,
                        defName,
                        ModSettings_QEverything.mealQuality);

                case QualityBucket.TastyIngredient:
                case QualityBucket.Ingredient:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivOther,
                        ModSettings_QEverything.otherDict,
                        defName,
                        ModSettings_QEverything.ingredientQuality);

                case QualityBucket.Manufactured:
                    return GetIndividualOrCategorySetting(
                        ModSettings_QEverything.indivOther,
                        ModSettings_QEverything.otherDict,
                        defName,
                        ModSettings_QEverything.manufQuality);

                default:
                    return false;
            }
        }

        public static int GetMinQuality(ThingDef def)
        {
            switch (GetBucket(def))
            {
                case QualityBucket.Stuff:
                    return ModSettings_QEverything.minStuffQuality;
                case QualityBucket.WorkBuilding:
                    return ModSettings_QEverything.minWorkQuality;
                case QualityBucket.SecurityBuilding:
                    return ModSettings_QEverything.minSecurityQuality;
                case QualityBucket.Building:
                    return ModSettings_QEverything.minEdificeQuality;
                case QualityBucket.Weapon:
                    return ModSettings_QEverything.minWeaponQuality;
                case QualityBucket.Shell:
                    return ModSettings_QEverything.minShellQuality;
                case QualityBucket.Apparel:
                    return ModSettings_QEverything.minApparelQuality;
                case QualityBucket.Drug:
                    return ModSettings_QEverything.minDrugQuality;
                case QualityBucket.Medicine:
                    return ModSettings_QEverything.minMedQuality;
                case QualityBucket.Meal:
                    return ModSettings_QEverything.minMealQuality;
                case QualityBucket.TastyIngredient:
                    return ModSettings_QEverything.minTastyQuality;
                case QualityBucket.Ingredient:
                    return ModSettings_QEverything.minIngQuality;
                case QualityBucket.Manufactured:
                    return ModSettings_QEverything.minManufQuality;
                default:
                    return 0;
            }
        }

        public static int GetMaxQuality(ThingDef def)
        {
            if (def != null && def.HasComp(typeof(CompArt)))
            {
                return (int)QualityCategory.Legendary;
            }

            switch (GetBucket(def))
            {
                case QualityBucket.Stuff:
                    return ModSettings_QEverything.maxStuffQuality;
                case QualityBucket.WorkBuilding:
                    return ModSettings_QEverything.maxWorkQuality;
                case QualityBucket.SecurityBuilding:
                    return ModSettings_QEverything.maxSecurityQuality;
                case QualityBucket.Building:
                    return ModSettings_QEverything.maxEdificeQuality;
                case QualityBucket.Weapon:
                    return ModSettings_QEverything.maxWeaponQuality;
                case QualityBucket.Shell:
                    return ModSettings_QEverything.maxShellQuality;
                case QualityBucket.Apparel:
                    return ModSettings_QEverything.maxApparelQuality;
                case QualityBucket.Drug:
                    return ModSettings_QEverything.maxDrugQuality;
                case QualityBucket.Medicine:
                    return ModSettings_QEverything.maxMedQuality;
                case QualityBucket.Meal:
                    return ModSettings_QEverything.maxMealQuality;
                case QualityBucket.TastyIngredient:
                    return ModSettings_QEverything.maxTastyQuality;
                case QualityBucket.Ingredient:
                    return ModSettings_QEverything.maxIngQuality;
                case QualityBucket.Manufactured:
                    return ModSettings_QEverything.maxManufQuality;
                default:
                    return (int)QualityCategory.Legendary;
            }
        }

        private static bool GetIndividualOrCategorySetting(
            bool useIndividualSettings,
            Dictionary<string, bool> dict,
            string defName,
            bool categoryDefault)
        {
            if (!useIndividualSettings)
            {
                return categoryDefault;
            }

            return dict.TryGetValue(defName, out bool enabled) && enabled;
        }
    }

    internal static class QualityCompSync
    {
        private static readonly HashSet<string> OriginalQualityDefs = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> OriginalImproveThisDefs = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Type ImproveThisCompType = AccessTools.TypeByName("RimWorld___Improve_This.ImproveThisComp");

        private static bool initialized;

        public static void SyncAllDefs()
        {
            CaptureOriginalState();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                SyncDef(def);
            }
        }

        private static void CaptureOriginalState()
        {
            if (initialized)
            {
                return;
            }

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.HasComp(typeof(CompQuality)))
                {
                    OriginalQualityDefs.Add(def.defName);
                }

                if (ImproveThisCompType != null && def.HasComp(ImproveThisCompType))
                {
                    OriginalImproveThisDefs.Add(def.defName);
                }
            }

            initialized = true;
        }

        private static void SyncDef(ThingDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.defName))
            {
                return;
            }

            bool shouldHaveQuality = QualityDefUtility.ShouldReceiveQuality(def);
            bool keepQuality = OriginalQualityDefs.Contains(def.defName) || shouldHaveQuality;
            SyncComp(def, typeof(CompQuality), keepQuality);

            if (ImproveThisCompType != null)
            {
                bool keepImproveThis = OriginalImproveThisDefs.Contains(def.defName) || shouldHaveQuality;
                SyncComp(def, ImproveThisCompType, keepImproveThis);
            }
        }

        private static void SyncComp(ThingDef def, Type compType, bool keepComp)
        {
            if (compType == null)
            {
                return;
            }

            def.comps ??= new List<CompProperties>();

            int existingIndex = FindCompIndex(def.comps, compType);
            if (keepComp)
            {
                if (existingIndex < 0)
                {
                    def.comps.Add(new CompProperties { compClass = compType });
                }
                return;
            }

            for (int i = def.comps.Count - 1; i >= 0; i--)
            {
                if (def.comps[i]?.compClass == compType)
                {
                    def.comps.RemoveAt(i);
                }
            }
        }

        private static int FindCompIndex(List<CompProperties> comps, Type compType)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i]?.compClass == compType)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
