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
            return QualityCatalog.Assigned(def);
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
        // Ownership by reference preserves native/custom properties even after repeated synchronization.
        private static readonly Dictionary<ThingDef, CompProperties> owned = new Dictionary<ThingDef, CompProperties>();
        public static void SyncAllDefs()
        {
            QualityCatalog.Build();
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                bool assigned = QualityDefUtility.ShouldReceiveQuality(def);
                if (!assigned)
                {
                    if (owned.TryGetValue(def, out var property)) { def.comps?.Remove(property); owned.Remove(def); }
                    continue;
                }
                if (def.comps?.Exists(p => p?.compClass != null && typeof(CompQuality).IsAssignableFrom(p.compClass)) == true) continue;
                def.comps ??= new List<CompProperties>();
                var added = new CompProperties { compClass = typeof(CompQuality) };
                def.comps.Add(added); owned[def] = added;
            }
        }
    }
}
