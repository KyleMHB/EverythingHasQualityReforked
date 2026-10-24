using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;

namespace ConfigurableQuality
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
        private static readonly ThingCategoryDef GrenadesCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Grenades");

        public static bool HasCategory(ThingDef def, ThingCategoryDef category)
        {
            return def != null && category != null && def.thingCategories != null && def.IsWithinCategory(category);
        }

        public static bool IsExcludedFromQuality(ThingDef def)
        {
            return def == null || def.plant != null || def.IsCorpse || def.race != null;
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
                    return ModSettings_ConfigurableQuality.minStuffQuality;
                case QualityBucket.WorkBuilding:
                    return ModSettings_ConfigurableQuality.minWorkQuality;
                case QualityBucket.SecurityBuilding:
                    return ModSettings_ConfigurableQuality.minSecurityQuality;
                case QualityBucket.Building:
                    return ModSettings_ConfigurableQuality.minEdificeQuality;
                case QualityBucket.Weapon:
                    return ModSettings_ConfigurableQuality.minWeaponQuality;
                case QualityBucket.Shell:
                    return ModSettings_ConfigurableQuality.minShellQuality;
                case QualityBucket.Apparel:
                    return ModSettings_ConfigurableQuality.minApparelQuality;
                case QualityBucket.Drug:
                    return ModSettings_ConfigurableQuality.minDrugQuality;
                case QualityBucket.Medicine:
                    return ModSettings_ConfigurableQuality.minMedQuality;
                case QualityBucket.Meal:
                    return ModSettings_ConfigurableQuality.minMealQuality;
                case QualityBucket.TastyIngredient:
                    return ModSettings_ConfigurableQuality.minTastyQuality;
                case QualityBucket.Ingredient:
                    return ModSettings_ConfigurableQuality.minIngQuality;
                case QualityBucket.Manufactured:
                    return ModSettings_ConfigurableQuality.minManufQuality;
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
                    return ModSettings_ConfigurableQuality.maxStuffQuality;
                case QualityBucket.WorkBuilding:
                    return ModSettings_ConfigurableQuality.maxWorkQuality;
                case QualityBucket.SecurityBuilding:
                    return ModSettings_ConfigurableQuality.maxSecurityQuality;
                case QualityBucket.Building:
                    return ModSettings_ConfigurableQuality.maxEdificeQuality;
                case QualityBucket.Weapon:
                    return ModSettings_ConfigurableQuality.maxWeaponQuality;
                case QualityBucket.Shell:
                    return ModSettings_ConfigurableQuality.maxShellQuality;
                case QualityBucket.Apparel:
                    return ModSettings_ConfigurableQuality.maxApparelQuality;
                case QualityBucket.Drug:
                    return ModSettings_ConfigurableQuality.maxDrugQuality;
                case QualityBucket.Medicine:
                    return ModSettings_ConfigurableQuality.maxMedQuality;
                case QualityBucket.Meal:
                    return ModSettings_ConfigurableQuality.maxMealQuality;
                case QualityBucket.TastyIngredient:
                    return ModSettings_ConfigurableQuality.maxTastyQuality;
                case QualityBucket.Ingredient:
                    return ModSettings_ConfigurableQuality.maxIngQuality;
                case QualityBucket.Manufactured:
                    return ModSettings_ConfigurableQuality.maxManufQuality;
                default:
                    return (int)QualityCategory.Legendary;
            }
        }

    }

    internal static class QualityCompSync
    {
        // Ownership by reference preserves native/custom properties even after repeated synchronization.
        private static readonly Dictionary<ThingDef, CompProperties> owned = new Dictionary<ThingDef, CompProperties>();
        private static readonly Dictionary<ThingDef, List<KeyValuePair<int, CompProperties>>> removedVanilla = new Dictionary<ThingDef, List<KeyValuePair<int, CompProperties>>>();
        public static void SyncAllDefs()
        {
            QualityCatalog.Build();
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading) SyncDef(def);
        }
        internal static void SyncDef(ThingDef def)
        {
            if (QualityCatalog.RemovesVanillaQuality(def))
            {
                // Only standard quality on explicitly selected vanilla defs is removable.
                // Preserve every unrelated property and retain originals for repeated synchronization.
                if (!removedVanilla.ContainsKey(def))
                {
                    var removed = new List<KeyValuePair<int, CompProperties>>();
                    for (int i = 0; i < (def.comps?.Count ?? 0); i++)
                        if (def.comps[i]?.compClass == typeof(CompQuality)) removed.Add(new KeyValuePair<int, CompProperties>(i, def.comps[i]));
                    removedVanilla[def] = removed;
                }
                def.comps?.RemoveAll(p => p?.compClass == typeof(CompQuality));
                return;
            }
            if (removedVanilla.TryGetValue(def, out var originals))
            {
                def.comps ??= new List<CompProperties>();
                foreach (var original in originals)
                    if (!def.comps.Contains(original.Value)) def.comps.Insert(Math.Min(original.Key, def.comps.Count), original.Value);
                removedVanilla.Remove(def);
            }
            bool assigned = QualityDefUtility.ShouldReceiveQuality(def);
            if (!assigned)
            {
                if (owned.TryGetValue(def, out var property)) { def.comps?.Remove(property); owned.Remove(def); }
                return;
            }
            if (def.comps?.Exists(p => p?.compClass != null && typeof(CompQuality).IsAssignableFrom(p.compClass)) == true) return;
            def.comps ??= new List<CompProperties>();
            var added = new CompProperties { compClass = typeof(CompQuality) };
            def.comps.Add(added); owned[def] = added;
        }
    }
}
