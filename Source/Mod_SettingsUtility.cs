using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace QualityEverything
{
    class Mod_SettingsUtility
    {
        public static void LabeledIntEntry(Rect rect, string label, ref int value, ref string editBuffer, int multiplier, int min, int max)
        {
            int num = (int)rect.width / 15;
            Widgets.Label(rect, label);
            if (Widgets.ButtonText(new Rect(rect.xMax - 90f, rect.yMin, 25f, rect.height), (-1 * multiplier).ToString(), true, true, true))
            {
                value -= GenUI.CurrentAdjustmentMultiplier();
                editBuffer = value.ToString();
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
            }
            if (Widgets.ButtonText(new Rect(rect.xMax - 30f, rect.yMin, 25f, rect.height), "+" + multiplier.ToString(), true, true, true))
            {
                value += multiplier * GenUI.CurrentAdjustmentMultiplier();
                editBuffer = value.ToString();
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
            }
            Widgets.TextFieldNumeric<int>(new Rect(rect.xMax - 60f, rect.yMin, 25f, rect.height), ref value, ref editBuffer, min, max);
        }
        public static void LabeledFloatEntry(Rect rect, string label, ref float value, ref string editBuffer, float multiplier, float largeMultiplier, float min, float max)
        {
            rect.width -= 8f;
            float num = rect.width / 9f;
            rect.x += 8f;
            Widgets.Label(rect, label);
            if (multiplier != largeMultiplier)
            {
                if (Widgets.ButtonText(new Rect(rect.xMax - num * 5f, rect.yMin, num, rect.height), "--", true, true, true))
                {
                    value -= largeMultiplier * GenUI.CurrentAdjustmentMultiplier();
                    value = (float)Mathf.Round(value * 100f) / 100f;
                    editBuffer = value.ToString();
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
                }
                if (Widgets.ButtonText(new Rect(rect.xMax - num, rect.yMin, num, rect.height), "++", true, true, true))
                {
                    value += largeMultiplier * GenUI.CurrentAdjustmentMultiplier();
                    value = (float)Mathf.Round(value * 100f) / 100f;
                    editBuffer = value.ToString();
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
                }
            }
            if (Widgets.ButtonText(new Rect(rect.xMax - num * 4f, rect.yMin, num - 3f, rect.height), "-", true, true, true))
            {
                value -= multiplier * GenUI.CurrentAdjustmentMultiplier();
                value = (float)Mathf.Round(value * 100f) / 100f;
                editBuffer = value.ToString();
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
            }
            if (Widgets.ButtonText(new Rect(rect.xMax - (num * 2f) + 3f, rect.yMin, num - 3f, rect.height), "+", true, true, true))
            {
                value += multiplier * GenUI.CurrentAdjustmentMultiplier();
                value = (float)Mathf.Round(value * 100f) / 100f;
                editBuffer = value.ToString();
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
            }
            Widgets.TextFieldNumeric<float>(new Rect(rect.xMax - (num * 3f) - 3f, rect.yMin, num + 5f, rect.height), ref value, ref editBuffer, min, max);
        }

        public static void PopulateStuff()
        {
            for (int i = 0; i < DefDatabase<ThingDef>.AllDefsListForReading.Count; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.AllDefsListForReading[i];
                if (QualityDefUtility.GetBucket(def) == QualityBucket.Stuff)
                {
                    ModSettings_QEverything.stuffDict[def.defName] = def.HasComp(typeof(CompQuality));
                }
            }
        }

        public static void PopulateBuildings()
        {
            for (int i = 0; i < DefDatabase<ThingDef>.AllDefsListForReading.Count; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.AllDefsListForReading[i];
                QualityBucket bucket = QualityDefUtility.GetBucket(def);
                if (bucket == QualityBucket.WorkBuilding
                    || bucket == QualityBucket.SecurityBuilding
                    || bucket == QualityBucket.Building)
                {
                    ModSettings_QEverything.bldgDict[def.defName] = def.HasComp(typeof(CompQuality));
                }
            }
        }

        public static void PopulateWeapons()
        {
            for (int i = 0; i < DefDatabase<ThingDef>.AllDefsListForReading.Count; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.AllDefsListForReading[i];
                QualityBucket bucket = QualityDefUtility.GetBucket(def);
                if (bucket == QualityBucket.Weapon || bucket == QualityBucket.Shell)
                {
                    ModSettings_QEverything.weapDict[def.defName] = def.HasComp(typeof(CompQuality));
                }
            }
        }

        public static void PopulateApparel()
        {
            for (int i = 0; i < DefDatabase<ThingDef>.AllDefsListForReading.Count; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.AllDefsListForReading[i];
                if (QualityDefUtility.GetBucket(def) == QualityBucket.Apparel)
                {
                    ModSettings_QEverything.appDict[def.defName] = def.HasComp(typeof(CompQuality));
                }
            }
        }

        public static void PopulateOther()
        {
            for (int i = 0; i < DefDatabase<ThingDef>.AllDefsListForReading.Count; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.AllDefsListForReading[i];
                if (QualityDefUtility.IsOtherCustomizationCandidate(def))
                {
                    ModSettings_QEverything.otherDict[def.defName] = def.HasComp(typeof(CompQuality));
                }
            }
        }

        public static void ApplySettingsChanges()
        {
            if (Startup.stuffPatchRan && !ModSettings_QEverything.stuffQuality && !ModSettings_QEverything.indivStuff)
            {
                Find.WindowStack.Add(new Window_RestartWarning("QEverything.RestartStuff".Translate()));
                return;
            }
            else if (!Startup.stuffPatchRan && (ModSettings_QEverything.stuffQuality || ModSettings_QEverything.indivStuff))
            {
                Find.WindowStack.Add(new Window_RestartWarning("QEverything.RestartStuff".Translate()));
                return;
            }
            QualityCompSync.SyncAllDefs();
            Quality_CompPatch.ApplyNewQuality();
            Find.WindowStack.Add(new Window_RestartWarning("QEverything.Restart".Translate()));
        }

        public static void RestoreDefaults()
        {
            ModSettings_QEverything.useMaterialQuality = true;
            ModSettings_QEverything.useTableQuality = true;
            ModSettings_QEverything.useSkillReq = true;
            ModSettings_QEverything.stdSupplyQuality = 2;
            ModSettings_QEverything.tableFactor = .4f;

            ModSettings_QEverything.inspiredButchering = true;
            ModSettings_QEverything.inspiredChemistry = true;
            ModSettings_QEverything.inspiredCooking = true;
            ModSettings_QEverything.inspiredConstruction = true;
            ModSettings_QEverything.inspiredGathering = true;
            ModSettings_QEverything.inspiredHarvesting = true;
            ModSettings_QEverything.inspiredMining = true;
            ModSettings_QEverything.inspiredStonecutting = true;

            ModSettings_QEverything.skilledAnimals = false;
            ModSettings_QEverything.skilledButchering = false;
            ModSettings_QEverything.skilledHarvesting = false;
            ModSettings_QEverything.skilledMining = false;
            ModSettings_QEverything.skilledStoneCutting = false;

            ModSettings_QEverything.edificeQuality = true;
            ModSettings_QEverything.minEdificeQuality = 0;
            ModSettings_QEverything.maxEdificeQuality = 4;

            ModSettings_QEverything.workQuality = true;
            ModSettings_QEverything.minWorkQuality = 0;
            ModSettings_QEverything.maxWorkQuality = 4;

            ModSettings_QEverything.securityQuality = true;
            ModSettings_QEverything.minSecurityQuality = 0;
            ModSettings_QEverything.maxSecurityQuality = 4;

            ModSettings_QEverything.stuffQuality = true;
            ModSettings_QEverything.minStuffQuality = 0;
            ModSettings_QEverything.maxStuffQuality = 4;

            ModSettings_QEverything.ingredientQuality = true;
            ModSettings_QEverything.minIngQuality = 2;
            ModSettings_QEverything.maxIngQuality = 4;
            ModSettings_QEverything.minTastyQuality = 0;
            ModSettings_QEverything.maxTastyQuality = 4;

            ModSettings_QEverything.mealQuality = false;
            ModSettings_QEverything.minMealQuality = 0;
            ModSettings_QEverything.maxMealQuality = 4;

            ModSettings_QEverything.drugQuality = false;
            ModSettings_QEverything.minDrugQuality = 0;
            ModSettings_QEverything.maxDrugQuality = 4;

            ModSettings_QEverything.medQuality = false;
            ModSettings_QEverything.minMedQuality = 0;
            ModSettings_QEverything.maxMedQuality = 4;

            ModSettings_QEverything.manufQuality = true;
            ModSettings_QEverything.minManufQuality = 0;
            ModSettings_QEverything.maxManufQuality = 4;

            ModSettings_QEverything.apparelQuality = false;
            ModSettings_QEverything.minApparelQuality = 0;
            ModSettings_QEverything.maxApparelQuality = 6;

            ModSettings_QEverything.weaponQuality = false;
            ModSettings_QEverything.minWeaponQuality = 0;
            ModSettings_QEverything.maxWeaponQuality = 6;

            ModSettings_QEverything.shellQuality = false;
            ModSettings_QEverything.minShellQuality = 0;
            ModSettings_QEverything.maxShellQuality = 4;

            ModSettings_QEverything.indivStuff = false;
            ModSettings_QEverything.indivBuildings = false;
            ModSettings_QEverything.indivWeapons = false;
            ModSettings_QEverything.indivApparel = false;
            ModSettings_QEverything.indivOther = false;
            ModSettings_QEverything.stuffDict.Clear();
            ModSettings_QEverything.bldgDict.Clear();
            ModSettings_QEverything.weapDict.Clear();
            ModSettings_QEverything.appDict.Clear();
            ModSettings_QEverything.otherDict.Clear();

            ModSettings_QEverything.multSupplyFactor = true;
            ModSettings_QEverything.awfulSupplyFactor = .8f;
            ModSettings_QEverything.poorSupplyFactor = .9f;
            ModSettings_QEverything.normalSupplyFactor = 1f;
            ModSettings_QEverything.goodSupplyFactor = 1.1f;
            ModSettings_QEverything.excSupplyFactor = 1.2f;
            ModSettings_QEverything.masterSupplyFactor = 1.3f;
            ModSettings_QEverything.legSupplyFactor = 1.4f;

        }
    }
}
