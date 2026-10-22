using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    public class Quality_Generator
    {
        public static QualityCategory GenerateQualityCreatedByPawn(Pawn pawn, SkillDef relevantSkill, Thing thing, int supplyQuality = -1, List<SkillRequirement> recipeSkillReq = null)
        {
            return GenerateQualityForDef(pawn, relevantSkill, thing.def, supplyQuality, recipeSkillReq);
        }

        public static QualityCategory GenerateQualityForDef(Pawn pawn, SkillDef relevantSkill, ThingDef def, int supplyQuality = -1, List<SkillRequirement> recipeSkillReq = null, bool consumeInspiration = true)
        {
            if (def == null || !QualityDefUtility.ShouldReceiveQuality(def))
                return QualityUtility.GenerateQualityCreatedByPawn(pawn, relevantSkill, consumeInspiration);
            if (ModSettings_QEverything.skilledStoneCutting && def.thingCategories?.Contains(ThingCategoryDefOf.StoneBlocks) == true && relevantSkill == null)
            {
                relevantSkill = SkillDefOf.Crafting;
            }
            if (relevantSkill == null || (!ModSettings_QEverything.skilledButchering && (def.IsMeat || def.IsLeather)))
            {
                return (QualityCategory)Mathf.Clamp((int)QualityUtility.GenerateQuality(QualityGenerator.BaseGen), QualityDefUtility.GetMinQuality(def), QualityDefUtility.GetMaxQuality(def));
            }
            QualityCategory qualityCategory;
            int minQuality = QualityDefUtility.GetMinQuality(def);
            int maxQuality = QualityDefUtility.GetMaxQuality(def);
            int level;
            if (pawn == null)
            {
                return (QualityCategory)Mathf.Clamp(
                    (int)QualityUtility.GenerateQuality(QualityGenerator.BaseGen),
                    minQuality,
                    maxQuality);
            }

            if (pawn.IsColonyMech)
                level = pawn.RaceProps.mechFixedSkillLevel;
            else
            {
                SkillRecord skill = pawn.skills?.GetSkill(relevantSkill);
                if (skill == null)
                {
                    return (QualityCategory)Mathf.Clamp(
                        (int)QualityUtility.GenerateQuality(QualityGenerator.BaseGen),
                        minQuality,
                        maxQuality);
                }

                level = skill.Level;
            }

            if (ModSettings_QEverything.useSkillReq)
            {
                if (recipeSkillReq != null)
                {
                    for (int sk = 0; sk < recipeSkillReq.Count; sk++)
                    {
                        if (recipeSkillReq[sk].skill == relevantSkill) level -= recipeSkillReq[sk].minLevel;
                    }
                }
                else if (relevantSkill == SkillDefOf.Plants)
                {
                    level -= GetHarvestRequirement(def);
                }
                else if (relevantSkill == SkillDefOf.Construction)
                {
                    level -= def.constructionSkillPrerequisite;
                }
            }
            InspirationDef inspirationDef = InspirationUtility.CheckInspired(def, relevantSkill);
            bool inspired = (consumeInspiration && inspirationDef != null && pawn.InspirationDef == inspirationDef);

            if ((ModSettings_QEverything.useMaterialQuality || ModSettings_QEverything.useTableQuality) && supplyQuality >= 0)
            {
                if (ModSettings_QEverything.multSupplyFactor)
                {
                    switch(supplyQuality)
					{
                        case 0:
                            level = (int)(level * ModSettings_QEverything.awfulSupplyFactor);
                            break;
                        case 1:
                            level = (int)(level * ModSettings_QEverything.poorSupplyFactor);
                            break;
                        case 2:
                            level = (int)(level * ModSettings_QEverything.normalSupplyFactor);
                            break;
                        case 3:
                            level = (int)(level * ModSettings_QEverything.goodSupplyFactor);
                            break;
                        case 4:
                            level = (int)(level * ModSettings_QEverything.excSupplyFactor);
                            break;
                        case 5:
                            level = (int)(level * ModSettings_QEverything.masterSupplyFactor);
                            break;
                        case 6:
                            level = (int)(level * ModSettings_QEverything.legSupplyFactor);
                            break;
                    }
                }
                else
                {
                    level += supplyQuality - Mathf.Min(maxQuality, ModSettings_QEverything.stdSupplyQuality);
                }
            }
            qualityCategory = QualityUtility.GenerateQualityCreatedByPawn(level, inspired);
            if (ModsConfig.IdeologyActive && pawn.Ideo != null)
            {
                Precept_Role role = pawn.Ideo.GetRole(pawn);
                if (role != null && role.def.roleEffects != null)
                {
                    RoleEffect roleEffect = role.def.roleEffects.FirstOrDefault((RoleEffect eff) => eff is RoleEffect_ProductionQualityOffset);
                    if (roleEffect != null)
                    {
                        qualityCategory = (QualityCategory)QualityTier.AddLevels((int)qualityCategory, ((RoleEffect_ProductionQualityOffset)roleEffect).offset);
                    }
                }
            }
            if (inspired && maxQuality > 4)
            {
                pawn.mindState.inspirationHandler.EndInspiration(inspirationDef);
            }
            return (QualityCategory)Mathf.Clamp((int)qualityCategory, minQuality, maxQuality);
        }

        private static Dictionary<ThingDef, int> harvestRequirements;
        private static int GetHarvestRequirement(ThingDef product)
        {
            if (harvestRequirements == null)
            {
                harvestRequirements = new Dictionary<ThingDef, int>();
                foreach (ThingDef plant in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    ThingDef harvested = plant.plant?.harvestedThingDef;
                    if (harvested == null) continue;
                    harvestRequirements.TryGetValue(harvested, out int previous);
                    harvestRequirements[harvested] = Math.Max(previous, plant.plant.sowMinSkill);
                }
            }
            return harvestRequirements.TryGetValue(product, out int required) ? required : 0;
        }
        public static int GetMinQuality(ThingDef def)
        {
            return QualityDefUtility.GetMinQuality(def);
        }

        public static int GetMaxQuality(ThingDef def)
        {
            return QualityDefUtility.GetMaxQuality(def);
        }
    }
}
