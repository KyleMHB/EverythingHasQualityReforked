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
            Widgets.Label(rect, label);
            if (Widgets.ButtonText(new Rect(rect.xMax - 90f, rect.yMin, 25f, rect.height), (-1 * multiplier).ToString(), true, true, true))
            {
                value -= multiplier * GenUI.CurrentAdjustmentMultiplier();
                value = Mathf.Clamp(value, min, max);
                editBuffer = value.ToString();
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
            }
            if (Widgets.ButtonText(new Rect(rect.xMax - 30f, rect.yMin, 25f, rect.height), "+" + multiplier.ToString(), true, true, true))
            {
                value += multiplier * GenUI.CurrentAdjustmentMultiplier();
                value = Mathf.Clamp(value, min, max);
                editBuffer = value.ToString();
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
            }
            Widgets.TextFieldNumeric<int>(new Rect(rect.xMax - 60f, rect.yMin, 25f, rect.height), ref value, ref editBuffer, min, max);
            value = Mathf.Clamp(value, min, max);
            editBuffer = value.ToString();
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
            Find.WindowStack.Add(new Window_RestartWarning("QEverything.Restart".Translate()));
        }

        public static void RestoreDefaults()
        {
            var defaults = new QualityConfiguration();
            var draft = QualitySettingsState.Draft;
            draft.Production = defaults.Production;
            draft.Assignment = defaults.Assignment;
            draft.Participation = defaults.Participation;
            draft.Effects = defaults.Effects;
            draft.LinkedFurnitureBonusCap = defaults.LinkedFurnitureBonusCap;
            draft.LinkedFurniturePenaltyCap = defaults.LinkedFurniturePenaltyCap;
            QualitySettingsState.Changed();
        }
    }
}
