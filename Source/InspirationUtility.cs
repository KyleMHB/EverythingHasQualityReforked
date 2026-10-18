using RimWorld;
using Verse;
using HarmonyLib;

namespace QualityEverything
{
    [DefOf]
    public static class DefOf_QFramework
    {
        static DefOf_QFramework()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOf_QFramework));
        }

        public static InspirationDef QF_Inspired_Butchering;

        public static InspirationDef QF_Inspired_Chemistry;

        public static InspirationDef QF_Inspired_Construction;

        public static InspirationDef QF_Inspired_Cooking;

        public static InspirationDef QF_Inspired_Gathering;

        public static InspirationDef QF_Inspired_Harvesting;

        public static InspirationDef QF_Inspired_Mining;

        public static InspirationDef QF_Inspired_Stonecutting;
    }

    [HarmonyPatch(typeof(InspirationWorker), "CommonalityFor")]
    class InspirationUtility
    {
        [HarmonyPostfix]
        public static float Postfix(float __result, InspirationWorker __instance)
        {
            InspirationDef inspiration = __instance.def;
            if (inspiration == DefOf_QFramework.QF_Inspired_Butchering)
            {
                if (QualityCatalog.AnyAssigned(QualityBucket.Stuff, QualityBucket.Ingredient, QualityBucket.TastyIngredient) && ModSettings_QEverything.inspiredButchering && ModSettings_QEverything.skilledButchering)
                {
                    return __result;
                }
                return 0f;
            }
            if (inspiration == DefOf_QFramework.QF_Inspired_Gathering)
            {
                if (QualityCatalog.AnyAssigned(QualityBucket.Stuff, QualityBucket.Ingredient, QualityBucket.TastyIngredient) && ModSettings_QEverything.inspiredGathering && ModSettings_QEverything.skilledAnimals)
                {
                    return __result;
                }
                return 0f;
            }
            if (inspiration == DefOf_QFramework.QF_Inspired_Harvesting)
            {
                if (QualityCatalog.AnyAssigned(QualityBucket.Stuff, QualityBucket.Ingredient, QualityBucket.TastyIngredient) && ModSettings_QEverything.inspiredHarvesting && ModSettings_QEverything.skilledHarvesting)
                {
                    return __result;
                }
                return 0f;
            }
            if (inspiration == DefOf_QFramework.QF_Inspired_Mining)
            {
                if (QualityCatalog.AnyAssigned(QualityBucket.Stuff, QualityBucket.Ingredient, QualityBucket.TastyIngredient) && ModSettings_QEverything.inspiredMining && ModSettings_QEverything.skilledMining)
                {
                    return __result;
                }
                return 0f;
            }
            if (inspiration == DefOf_QFramework.QF_Inspired_Stonecutting)
            { 
                if(QualityCatalog.AnyAssigned(QualityBucket.Stuff, QualityBucket.Ingredient, QualityBucket.TastyIngredient) && ModSettings_QEverything.inspiredStonecutting && ModSettings_QEverything.skilledStoneCutting)
                { 
                    return __result;
                }
                return 0f;
            }
            if (inspiration == DefOf_QFramework.QF_Inspired_Chemistry)
            {
                if (ModSettings_QEverything.inspiredChemistry && QualityCatalog.AnyAssigned(QualityBucket.Drug, QualityBucket.Medicine))
                {
                    return __result;
                }
                return 0f;
            }
            if (inspiration == DefOf_QFramework.QF_Inspired_Construction)
            {
                if (ModSettings_QEverything.inspiredConstruction && QualityCatalog.AnyAssigned(QualityBucket.Building, QualityBucket.WorkBuilding, QualityBucket.SecurityBuilding))
                {
                    return __result;
                }
                return 0f;
 
            }
            if (inspiration == DefOf_QFramework.QF_Inspired_Cooking)
            {
                if (ModSettings_QEverything.inspiredCooking && QualityCatalog.AnyAssigned(QualityBucket.Meal))
                {
                    return __result;
                }
                return 0f;
            }
            return __result;
        }

        public static InspirationDef CheckInspired(ThingDef thingDef, SkillDef relevantSkill)
        {
            //Log.Message("Setting inspiration type");
            if (relevantSkill == SkillDefOf.Construction)
            {
                if (thingDef.HasComp(typeof(CompArt))) return InspirationDefOf.Inspired_Creativity;
                else return DefOf_QFramework.QF_Inspired_Construction;
            }
            else if (relevantSkill == SkillDefOf.Artistic || thingDef.IsApparel || thingDef.IsWeapon)
            {
                return InspirationDefOf.Inspired_Creativity;
            }
            else if (relevantSkill == SkillDefOf.Crafting)
            {
                if (thingDef.IsDrug || thingDef.IsMedicine) return DefOf_QFramework.QF_Inspired_Chemistry;
                if (thingDef.thingCategories != null && thingDef.thingCategories.Contains(ThingCategoryDefOf.StoneBlocks)) return DefOf_QFramework.QF_Inspired_Stonecutting;
            }
            else if (relevantSkill == SkillDefOf.Cooking)
            {
                if (thingDef.thingCategories != null && thingDef.thingCategories.Contains(ThingCategoryDefOf.Foods)) return DefOf_QFramework.QF_Inspired_Cooking;
                else if (thingDef.IsMeat || thingDef.IsStuff) return DefOf_QFramework.QF_Inspired_Butchering;
            }
            else if (relevantSkill == SkillDefOf.Animals) return DefOf_QFramework.QF_Inspired_Gathering;
            else if (relevantSkill == SkillDefOf.Plants) return DefOf_QFramework.QF_Inspired_Harvesting;
            else if (relevantSkill == SkillDefOf.Mining) return DefOf_QFramework.QF_Inspired_Mining;
            return null;
        }
    }
}
