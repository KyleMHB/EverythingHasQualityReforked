using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
{
    // This is a conservative definition-level preview, not a second implementation of adapter eligibility.
    internal sealed class Window_QualitySupport : Window
    {
        private readonly QualityCatalogEntry entry;
        private readonly List<EffectId> candidates, otherExclusions;
        private EffectId? expanded;
        private bool showOther;
        private Vector2 scroll;
        public override Vector2 InitialSize => new Vector2(840, 660);
        private static string A(string key) => ("QEverything.Advanced." + key).Translate().ToString();
        private static string EffectName(EffectId id) => ("QEverything.Options.Effect." + id).Translate().ToString();
        public Window_QualitySupport(QualityCatalogEntry entry)
        {
            this.entry = entry;
            candidates = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Where(id => Candidate(id, entry.Def)).ToList();
            otherExclusions = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Where(id => !candidates.Contains(id) && QualityEffects.UnsupportedReason(id, entry.Def) != null).ToList();
            doCloseX = true; forcePause = true; absorbInputAroundWindow = true;
        }
        private static bool HasComp<T>(ThingDef def) where T : ThingComp => def.comps?.Any(p => p?.compClass != null && typeof(T).IsAssignableFrom(p.compClass)) == true;
        private static bool Is<T>(ThingDef def) => def.thingClass != null && typeof(T).IsAssignableFrom(def.thingClass);
        private static bool Gear(ThingDef def, params string[] names) => (def.IsWeapon || def.IsApparel) && def.equippedStatOffsets?.Any(s => s.stat != null && names.Contains(s.stat.defName) && (s.stat.defName == "ComfyTemperatureMin" ? s.value < 0 : s.value > 0)) == true;
        private static bool Candidate(EffectId id, ThingDef def)
        {
            if (def == null || def.IsBlueprint || def.IsFrame) return false;
            switch (id)
            {
                case EffectId.BuildingDurability: return def.useHitPoints && Is<Building>(def);
                case EffectId.WeaponDurability: return def.useHitPoints && def.IsWeapon && !Is<Building>(def);
                case EffectId.ApparelDurability: return def.useHitPoints && def.IsApparel && !def.IsWeapon && !Is<Building>(def);
                case EffectId.WorktableSpeed: return Is<Building_WorkTable>(def) && !Is<Building_ResearchBench>(def);
                case EffectId.ResearchSpeed: return Is<Building_ResearchBench>(def);
                case EffectId.DoorSpeed: return Is<Building_Door>(def);
                // Native PowerOutput chooses output/demand at runtime, so trader components are candidates for both.
                case EffectId.GeneratorOutput:
                case EffectId.ConsumerDemand: return HasComp<CompPowerTrader>(def);
                case EffectId.BatteryCapacity:
                case EffectId.BatteryEfficiency: return HasComp<CompPowerBattery>(def);
                case EffectId.FoodNutrition:
                case EffectId.FoodPoisoningRisk: return def.ingestible != null;
                case EffectId.MedicinePotency: return def.IsMedicine;
                case EffectId.DrugStrength:
                case EffectId.DrugDuration: return def.IsDrug;
                case EffectId.EquipmentWork: return Gear(def, "WorkSpeedGlobal", "MiningSpeed", "DeepDrillingSpeed", "MiningYield", "SmoothingSpeed", "ResearchSpeed", "AnimalGatherSpeed", "AnimalGatherYield", "PlantWorkSpeed", "PlantHarvestYield", "DrugHarvestYield", "ConstructionSpeed", "ConstructSuccessChance", "FixBrokenDownBuildingSuccessChance", "GeneralLaborSpeed", "GlobalLearningFactor");
                case EffectId.EquipmentMovement: return Gear(def, "MoveSpeed", "CarryingCapacity", "ComfyTemperatureMax", "ComfyTemperatureMin", "ToxicResistance", "ToxicEnvironmentResistance");
                case EffectId.EquipmentMedicine: return Gear(def, "MedicalTendSpeed", "MedicalTendQuality", "MedicalOperationSpeed", "MedicalSurgerySuccessChance");
                case EffectId.EquipmentSocial: return Gear(def, "NegotiationAbility", "PawnBeauty", "ArrestSuccessChance", "TradePriceImprovement", "SocialImpact", "TameAnimalChance", "TrainAnimalChance");
                case EffectId.EquipmentAbilities: return Gear(def, "PsychicEntropyMax", "PsychicEntropyMaxOffset", "PsychicEntropyRecoveryRate", "PsychicEntropyRecoveryRateOffset", "MeditationFocusGain");
                case EffectId.MeleeCooldown: return def.IsWeapon && def.tools?.Count > 0;
                case EffectId.RangedCooldown: return def.IsRangedWeapon;
                case EffectId.TurretCooldown: return Is<Building_TurretGun>(def);
                case EffectId.TrapDamage: return Is<Building_TrapDamager>(def);
                case EffectId.LinkedFurniture: return def.defName == "Dresser" || def.defName == "EndTable";
                default: return false;
            }
        }
        private string Reason(EffectId id) => QualityEffects.UnsupportedReason(id, entry.Def) ?? QualityEffects.Reason(id);
        private float RowHeight(EffectId id, float width) => 42 + (expanded == id ? Math.Max(36, Text.CalcHeight(Reason(id), width - 16) + 14) : 0);
        private void DrawRow(EffectId id, ref float y, float width)
        {
            bool unsupported = QualityEffects.UnsupportedReason(id, entry.Def) != null;
            string status = A(unsupported ? "SupportRecordedExclusion" : QualityEffects.Available(id) ? "AdapterAvailable" : "AdapterUnavailable");
            if (Widgets.ButtonText(new Rect(0, y, width * .53f, 34), EffectName(id))) expanded = expanded == id ? (EffectId?)null : id;
            Widgets.Label(new Rect(width * .55f, y + 3, width * .45f, 32), status);
            y += 42;
            if (expanded == id)
            {
                float h = Math.Max(36, Text.CalcHeight(Reason(id), width - 16) + 14);
                Widgets.Label(new Rect(8, y, width - 16, h), Reason(id)); y += h;
            }
        }
        public override void DoWindowContents(Rect rect)
        {
            Widgets.Label(new Rect(0, 0, rect.width, 30), entry.Label + " / " + entry.Source);
            Widgets.Label(new Rect(0, 38, rect.width, 70), A("SupportIntro"));
            float width = rect.width - 18;
            var rows = showOther ? candidates.Concat(otherExclusions).ToList() : candidates;
            float height = 126 + rows.Sum(id => RowHeight(id, width));
            var viewport = new Rect(0, 116, rect.width, rect.height - 164);
            Widgets.BeginScrollView(viewport, ref scroll, new Rect(0, 0, width, Math.Max(viewport.height, height)));
            float y = 0;
            Widgets.Label(new Rect(0, y, width, 28), A("SupportCandidates") + " (" + candidates.Count + ")"); y += 32;
            if (candidates.Count == 0) { Widgets.Label(new Rect(0, y, width, 38), A("SupportNone")); y += 42; }
            foreach (var id in candidates) DrawRow(id, ref y, width);
            if (otherExclusions.Count > 0)
            {
                if (Widgets.ButtonText(new Rect(0, y, width, 34), A(showOther ? "SupportHideOther" : "SupportShowOther") + " (" + otherExclusions.Count + ")")) showOther = !showOther;
                y += 42;
                if (showOther) foreach (var id in otherExclusions) DrawRow(id, ref y, width);
            }
            Widgets.EndScrollView();
            if (Widgets.ButtonText(new Rect(rect.width - 130, rect.height - 36, 130, 32), "QEverything.Options.UX.Done".Translate())) Close();
        }
    }
}
