using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QualityEverything
{
    internal static class QualityEquipmentCombat
    {
        private const string Owner = "rimworld.qualityframework";
        private sealed class GearStat
        {
            public StatDef Stat;
            public EffectId Effect;
            public bool LowerIsBetter;
        }
        private static readonly Dictionary<StatDef, GearStat> gearStats = new Dictionary<StatDef, GearStat>();
        private static readonly Dictionary<ThingDef, List<GearStat>> activeGear = new Dictionary<ThingDef, List<GearStat>>();
        private static readonly Dictionary<EffectId, HashSet<ThingDef>> activeTargets = new Dictionary<EffectId, HashSet<ThingDef>>();
        private static readonly HashSet<ThingDef> turretWeapons = new HashSet<ThingDef>();
        private static bool registered;
        private static readonly EffectId[] gearIds = { EffectId.EquipmentWork, EffectId.EquipmentMovement, EffectId.EquipmentMedicine, EffectId.EquipmentSocial, EffectId.EquipmentAbilities };

        internal static void Register()
        {
            if (registered) return;
            registered = true;
            var harmony = new Harmony(Owner);
            MethodInfo gearMethod = AccessTools.Method(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear));
            bool cacheReady = Patch(harmony, AccessTools.Method(typeof(CompQuality), nameof(CompQuality.SetQuality)), nameof(QualityChanged), nameof(BeforeQualityChanged));
            cacheReady &= Patch(harmony, AccessTools.Method(typeof(Thing), nameof(Thing.Notify_Equipped)), nameof(GearChanged));
            cacheReady &= Patch(harmony, AccessTools.Method(typeof(Thing), nameof(Thing.Notify_Unequipped)), nameof(GearChanged));
            bool gearReady = cacheReady && Patch(harmony, gearMethod, nameof(GearOffset));
            if (gearReady)
            {
                AddGearStats(EffectId.EquipmentWork, false, "WorkSpeedGlobal", "MiningSpeed", "DeepDrillingSpeed", "MiningYield", "SmoothingSpeed", "ResearchSpeed", "AnimalGatherSpeed", "AnimalGatherYield", "PlantWorkSpeed", "PlantHarvestYield", "DrugHarvestYield", "ConstructionSpeed", "ConstructSuccessChance", "FixBrokenDownBuildingSuccessChance", "GeneralLaborSpeed", "GlobalLearningFactor");
                AddGearStats(EffectId.EquipmentMovement, false, "MoveSpeed", "CarryingCapacity", "ComfyTemperatureMax", "ToxicResistance", "ToxicEnvironmentResistance");
                AddGearStats(EffectId.EquipmentMovement, true, "ComfyTemperatureMin");
                AddGearStats(EffectId.EquipmentMedicine, false, "MedicalTendSpeed", "MedicalTendQuality", "MedicalOperationSpeed", "MedicalSurgerySuccessChance");
                AddGearStats(EffectId.EquipmentSocial, false, "NegotiationAbility", "PawnBeauty", "ArrestSuccessChance", "TradePriceImprovement", "SocialImpact", "TameAnimalChance", "TrainAnimalChance");
                AddGearStats(EffectId.EquipmentAbilities, false, "PsychicEntropyMax", "PsychicEntropyMaxOffset", "PsychicEntropyRecoveryRate", "PsychicEntropyRecoveryRateOffset", "MeditationFocusGain");
            }
            foreach (var id in gearIds) RegisterGear(id, gearReady);

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                if (def.building?.turretGunDef != null) turretWeapons.Add(def.building.turretGunDef);
            AddCombatStat(EffectId.MeleeCooldown, cacheReady, StatDefOf.MeleeWeapon_CooldownMultiplier,
                d => d.IsWeapon && !turretWeapons.Contains(d) && d.tools?.Count > 0);
            AddCombatStat(EffectId.RangedCooldown, cacheReady, StatDefOf.RangedWeapon_Cooldown,
                d => d.IsRangedWeapon && !turretWeapons.Contains(d));
            bool turretReady = Patch(harmony, AccessTools.Method(typeof(Building_TurretGun), "BurstCooldownTime"), nameof(TurretCooldown));
            RegisterCombat(EffectId.TurretCooldown, turretReady, null,
                d => d.thingClass != null && typeof(Building_TurretGun).IsAssignableFrom(d.thingClass) && d.thingClass.Assembly == typeof(Building_TurretGun).Assembly);
            StatDef trap = StatDefOf.TrapMeleeDamage;
            bool trapReady = cacheReady;
            RegisterCombat(EffectId.TrapDamage, trapReady, trap,
                d => d.thingClass != null && typeof(Building_TrapDamager).IsAssignableFrom(d.thingClass) && d.thingClass.Assembly == typeof(Building_TrapDamager).Assembly);
            if (trapReady && !Protected(trap))
            {
                if (trap.parts == null) trap.parts = new List<StatPart>();
                trap.parts.Add(new StatPart_QualityTrapDamage { parentStat = trap });
            }
        }

        private static string Text(string suffix) => ("QEverything.EquipmentCombat." + suffix).Translate();
        private static bool Enabled(EffectId id) => QualitySettingsState.ActiveSnapshot.Effects.TryGetValue(id.ToString(), out var effect) && effect.Enabled;
        private static bool HasQuality(ThingDef def) => def.comps?.Any(c => c?.compClass != null && typeof(CompQuality).IsAssignableFrom(c.compClass)) == true;
        private static bool Protected(StatDef stat) => stat != null && (stat.Worker.GetType().Assembly != typeof(StatWorker).Assembly ||
            stat.parts?.Any(p => p is StatPart_Quality || p.GetType().Assembly != typeof(StatPart).Assembly || p.GetType().Name.IndexOf("Quality", StringComparison.OrdinalIgnoreCase) >= 0) == true);

        private static bool Patch(Harmony harmony, MethodInfo method, string postfix, string prefix = null)
        {
            if (method == null) return false;
            // Unknown interceptors may already supply quality scaling. Fail closed rather than advertise arbitrary stacking.
            if (Harmony.GetPatchInfo(method)?.Owners.Any(o => o != Owner) == true) return false;
            try
            {
                harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(QualityEquipmentCombat), prefix),
                    new HarmonyMethod(typeof(QualityEquipmentCombat), postfix));
                return true;
            }
            catch (Exception error)
            {
                Log.Warning("QEverything: equipment/combat adapter left native behavior intact for " + method.Name + ": " + error.Message);
                return false;
            }
        }

        private static void AddGearStats(EffectId effect, bool lower, params string[] names)
        {
            foreach (string name in names)
            {
                var stat = DefDatabase<StatDef>.GetNamedSilentFail(name);
                if (stat != null && !Protected(stat)) gearStats[stat] = new GearStat { Stat = stat, Effect = effect, LowerIsBetter = lower };
            }
        }
        private static void RegisterGear(EffectId id, bool ready)
        {
            int supported = 0;
            foreach (var entry in QualityCatalog.Entries)
            {
                var offsets = entry.Def.equippedStatOffsets;
                var stats = new List<GearStat>();
                if ((entry.Def.IsApparel || entry.Def.IsWeapon) && offsets != null)
                    foreach (var offset in offsets)
                        if (gearStats.TryGetValue(offset.stat, out var stat) && stat.Effect == id &&
                            (stat.LowerIsBetter ? offset.value < 0 : offset.value > 0)) stats.Add(stat);
                if (!ready || stats.Count == 0)
                {
                    QualityEffects.SetUnsupported(id, entry.Def, Text(ready ? "NoContribution" : "Conflict"));
                    continue;
                }
                supported++;
                if (Enabled(id) && HasQuality(entry.Def) && QualityCatalog.Participates(entry.Def))
                {
                    if (!activeGear.TryGetValue(entry.Def, out var list)) activeGear[entry.Def] = list = new List<GearStat>();
                    list.AddRange(stats);
                }
            }
            QualityEffects.SetCapability(id, ready && supported > 0, Text(!ready ? "Conflict" : supported > 0 ? "GearAvailable" : "NoContribution"));
        }
        private static void AddCombatStat(EffectId id, bool ready, StatDef stat, Func<ThingDef, bool> matches)
        {
            RegisterCombat(id, ready, stat, matches);
            if (!ready || Protected(stat)) return;
            if (stat.parts == null) stat.parts = new List<StatPart>();
            stat.parts.Add(new StatPart_QualityWeaponCooldown(id) { parentStat = stat });
        }
        private static void RegisterCombat(EffectId id, bool ready, StatDef stat, Func<ThingDef, bool> matches)
        {
            var active = activeTargets[id] = new HashSet<ThingDef>();
            bool protectedCurve = Protected(stat);
            int supported = 0;
            foreach (var entry in QualityCatalog.Entries)
            {
                string reason = !ready ? Text("Conflict") : protectedCurve ? Text("NativeCurve") : !matches(entry.Def) ? Text("NotApplicable") : null;
                if (reason != null) { QualityEffects.SetUnsupported(id, entry.Def, reason); continue; }
                supported++;
                if (Enabled(id) && HasQuality(entry.Def) && QualityCatalog.Participates(entry.Def)) active.Add(entry.Def);
            }
            QualityEffects.SetCapability(id, ready && !protectedCurve && supported > 0,
                Text(!ready ? "Conflict" : protectedCurve ? "NativeCurve" : "CombatAvailable"));
        }
        internal static float Factor(EffectId id, Thing thing) => thing != null && activeTargets.TryGetValue(id, out var defs) && defs.Contains(thing.def) ? QualityEffects.Factor(id, thing) : 1f;

        private static void GearOffset(Thing __0, StatDef __1, ref float __result)
        {
            var gear = __0;
            if (gear == null || !activeGear.TryGetValue(gear.def, out var list) || !gearStats.TryGetValue(__1, out var supported) || !list.Contains(supported)) return;
            __result = QualityGearContribution.Scale(__result, QualityEffects.Factor(supported.Effect, gear), supported.LowerIsBetter);
        }
        private static void ClearWearer(Thing gear, Pawn pawn)
        {
            if (gear == null || pawn == null || !activeGear.TryGetValue(gear.def, out var list)) return;
            foreach (var supported in list) supported.Stat.Worker.ClearCacheForThing(pawn);
        }
        private static void GearChanged(Thing __instance, Pawn __0) => ClearWearer(__instance, __0);
        private static void BeforeQualityChanged(CompQuality __instance, out QualityCategory __state) => __state = __instance.Quality;
        [HarmonyPriority(Priority.Last)]
        private static void QualityChanged(CompQuality __instance, QualityCategory __state)
        {
            var gear = __instance.parent;
            if (__state == __instance.Quality || gear == null) return;
            if (activeTargets.TryGetValue(EffectId.TrapDamage, out var traps) && traps.Contains(gear.def)) StatDefOf.TrapMeleeDamage.Worker.ClearCacheForThing(gear);
            if (activeTargets.TryGetValue(EffectId.MeleeCooldown, out var melee) && melee.Contains(gear.def)) StatDefOf.MeleeWeapon_CooldownMultiplier.Worker.ClearCacheForThing(gear);
            if (activeTargets.TryGetValue(EffectId.RangedCooldown, out var ranged) && ranged.Contains(gear.def)) StatDefOf.RangedWeapon_Cooldown.Worker.ClearCacheForThing(gear);
            if (!activeGear.ContainsKey(gear.def)) return;
            var holder = gear.ParentHolder;
            ClearWearer(gear, holder is Pawn_EquipmentTracker equipment ? equipment.pawn : holder is Pawn_ApparelTracker apparel ? apparel.pawn : null);
        }
        private static void TurretCooldown(Building_TurretGun __instance, ref float __result)
        {
            // Both explicit and fallback cooldowns are scaled here. Turret gun definitions are excluded from weapon effects, including older stat-based fallback consumers.
            __result *= Factor(EffectId.TurretCooldown, __instance);
        }
    }

    internal sealed class StatPart_QualityWeaponCooldown : StatPart
    {
        private readonly EffectId effect;
        internal StatPart_QualityWeaponCooldown(EffectId effect) { this.effect = effect; }
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing) val *= QualityEquipmentCombat.Factor(effect, req.Thing);
        }
        public override string ExplanationPart(StatRequest req)
        {
            float factor = req.HasThing ? QualityEquipmentCombat.Factor(effect, req.Thing) : 1f;
            return factor == 1f ? null : "QEverything.EquipmentCombat.CooldownExplanation".Translate(factor.ToStringPercent());
        }
    }

    internal sealed class StatPart_QualityTrapDamage : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing) val *= QualityEquipmentCombat.Factor(EffectId.TrapDamage, req.Thing);
        }
        public override string ExplanationPart(StatRequest req)
        {
            float factor = req.HasThing ? QualityEquipmentCombat.Factor(EffectId.TrapDamage, req.Thing) : 1f;
            return factor == 1f ? null : "QEverything.EquipmentCombat.TrapExplanation".Translate(factor.ToStringPercent());
        }
    }
}
