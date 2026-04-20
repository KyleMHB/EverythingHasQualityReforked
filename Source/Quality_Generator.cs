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
            ThingDef def = thing.def;
            if (ModSettings_QEverything.skilledStoneCutting && thing.HasThingCategory(ThingCategoryDefOf.StoneBlocks) && relevantSkill == null)
            {
                relevantSkill = SkillDefOf.Crafting;
            }
            if (relevantSkill == null || (!ModSettings_QEverything.skilledButchering && (def.IsMeat || def.IsLeather)))
            {
                return QualityUtility.GenerateQuality(QualityGenerator.BaseGen);
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
                        level -= recipeSkillReq[sk].minLevel;
                    }
                }
                else if (relevantSkill == SkillDefOf.Plants)
                {
                    int plantSkill = 0;
                    foreach (var plantDef in DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.plant?.sowMinSkill != null))
                    {
                        if (plantDef.plant.harvestedThingDef == def) plantSkill = Mathf.Max(plantSkill, plantDef.plant.sowMinSkill);
                    }
                    level -= plantSkill;
                }
                else if (def is BuildableDef)
                {
                    level -= def.constructionSkillPrerequisite;
                }
            }
            InspirationDef inspirationDef = InspirationUtility.CheckInspired(def, relevantSkill);
            bool inspired = (inspirationDef != null && pawn.InspirationDef == inspirationDef);
            
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
                        qualityCategory = (QualityCategory)Mathf.Min((int)(qualityCategory + (byte)((RoleEffect_ProductionQualityOffset)roleEffect).offset), 6);
                    }
                }
            }
            if (inspired && maxQuality > 4)
            {
                pawn.mindState.inspirationHandler.EndInspiration(inspirationDef);
            }
            return (QualityCategory)Mathf.Clamp((int)qualityCategory, minQuality, maxQuality);
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
