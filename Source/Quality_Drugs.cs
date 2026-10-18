using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QualityEverything
{
    // Deliberately limited to four ordinary single-stage native highs. Unknown schemas remain native.
    internal static class QualityDrugEffects
    {
        sealed class HighDose
        {
            public HighDose() { }
            public DoseSnapshot Snapshot = new DoseSnapshot();
            public HediffStage Source, View;
        }
        static readonly ConditionalWeakTable<Hediff, HighDose> doses = new ConditionalWeakTable<Hediff, HighDose>();
        static readonly HashSet<HediffDef> supported = new HashSet<HediffDef>();
        static readonly HashSet<string> highNames = new HashSet<string> { "GoJuiceHigh", "WakeUpHigh", "YayoHigh", "SmokeleafHigh" };
        static readonly HashSet<string> tolerances = new HashSet<string> { "GoJuiceTolerance", "WakeUpTolerance", "PsychiteTolerance", "SmokeleafTolerance" };
        static readonly HashSet<string> capacities = new HashSet<string> { "Consciousness", "Sight", "Moving" };
        static readonly HashSet<string> positiveStats = new HashSet<string> { "WorkSpeedGlobal", "MeditationFocusGain" };
        static StatDef[] affectedStats = new StatDef[0];
        static readonly MethodInfo clone = AccessTools.Method(typeof(object), "MemberwiseClone");
        static readonly MethodInfo nativeAdd = AccessTools.Method(typeof(Pawn_HealthTracker), "AddHediff", new[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) });
        public static void Register()
        {
            var owner = typeof(QualityDrugEffects);
            var affected = new HashSet<StatDef>(DefDatabase<StatDef>.AllDefsListForReading.Where(s =>
                positiveStats.Contains(s.defName) || s.defName == "RestFallRateFactor"
                || s.capacityFactors?.Any(c => c?.capacity != null && capacities.Contains(c.capacity.defName)) == true
                || s.capacityOffsets?.Any(c => c?.capacity != null && capacities.Contains(c.capacity.defName)) == true));
            bool changed;
            do
            {
                changed = false;
                foreach (var stat in DefDatabase<StatDef>.AllDefsListForReading)
                    if (stat.statFactors?.Any(affected.Contains) == true || stat.postProcessStatFactors?.Any(affected.Contains) == true) changed |= affected.Add(stat);
            } while (changed);
            affectedStats = affected.ToArray();
            bool ready = QualityConsumableEffects.Patch(AccessTools.Method(typeof(IngestionOutcomeDoer_GiveHediff), "DoIngestionOutcomeSpecial"), transpiler: nameof(GiveTranspiler), owner: owner)
                && QualityPatchUtility.Statuses.TryGetValue("drug dose capture", out bool matched) && matched
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(Hediff), "TryMergeWith"), prefix: nameof(BeforeMerge), postfix: nameof(AfterMerge), owner: owner)
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(Hediff), "ExposeData"), postfix: nameof(ExposeDose), owner: owner)
                && QualityConsumableEffects.Patch(AccessTools.PropertyGetter(typeof(Hediff), "CurStage"), postfix: nameof(Stage), owner: owner)
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(HediffComp_SeverityPerDay), "SeverityChangePerDay"), postfix: nameof(Decay), owner: owner)
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(Thought), "MoodOffset"), postfix: nameof(Mood), owner: owner)
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(IngestionOutcomeDoer_OffsetNeed), "DoIngestionOutcomeSpecial"), transpiler: nameof(NeedTranspiler), owner: owner)
                && QualityPatchUtility.Statuses.TryGetValue("drug rest benefit", out bool needMatched) && needMatched
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(IngestionOutcomeDoer_OffsetPsyfocus), "DoIngestionOutcomeSpecial"), transpiler: nameof(PsyfocusTranspiler), owner: owner)
                && QualityPatchUtility.Statuses.TryGetValue("drug psyfocus benefit", out bool psyMatched) && psyMatched;
            foreach (var def in DefDatabase<HediffDef>.AllDefsListForReading)
                if (ready && SchemaSupported(def)) supported.Add(def);
            foreach (var entry in QualityCatalog.Entries.Where(e => e.Def.IsDrug))
            {
                var doers = entry.Def.ingestible?.outcomeDoers;
                bool high = doers?.OfType<IngestionOutcomeDoer_GiveHediff>().Any(d => supported.Contains(d.hediffDef)) == true;
                bool unknown = doers == null || doers.Any(d => d.GetType() != typeof(IngestionOutcomeDoer_GiveHediff)
                    && d.GetType() != typeof(IngestionOutcomeDoer_OffsetNeed) && d.GetType() != typeof(IngestionOutcomeDoer_OffsetPsyfocus));
                // Tolerance givers are deliberately native. Other high givers or custom outcomes reject the whole drug.
                unknown |= doers?.OfType<IngestionOutcomeDoer_GiveHediff>().Any(d => !supported.Contains(d.hediffDef)
                    && (d.hediffDef == null || !tolerances.Contains(d.hediffDef.defName))) == true;
                unknown |= doers?.OfType<IngestionOutcomeDoer_OffsetNeed>().Any(d => d.need != NeedDefOf.Rest) == true;
                if (!high || unknown)
                    foreach (var effect in new[] { EffectId.DrugStrength, EffectId.DrugDuration })
                        QualityEffects.SetUnsupported(effect, entry.Def, "QEverything.Consumables.DrugSchema".Translate().ToString());
            }
            foreach (var effect in new[] { EffectId.DrugStrength, EffectId.DrugDuration })
                QualityEffects.SetCapability(effect, ready, (ready ? "QEverything.Consumables.DrugsSupported" : "QEverything.Consumables.DrugUnavailable").Translate().ToString());
        }
        static bool SchemaSupported(HediffDef def)
        {
            if (!highNames.Contains(def.defName) || def.hediffClass != typeof(Hediff_High) || def.stages?.Count != 1
                || def.comps?.Count != 1 || !(def.comps[0] is HediffCompProperties_SeverityPerDay decay)
                || decay.GetType() != typeof(HediffCompProperties_SeverityPerDay) || decay.compClass != typeof(HediffComp_SeverityPerDay)
                || decay.severityPerDay >= 0 || decay.reverseSeverityChangeChance != 0 || decay.minAge > 0
                || decay.severityPerDayRange.min != 0 || decay.severityPerDayRange.max != 0) return false;
            var stage = def.stages[0];
            if (stage.capMods?.Any(c => (c?.capacity == null || !capacities.Contains(c.capacity.defName)) || c.statFactorMod != null || c.setMaxCurveOverride != null || c.setMaxCurveEvaluateStat != null) == true
                || stage.statOffsets?.Any(s => s?.stat == null || !positiveStats.Contains(s.stat.defName)) == true
                || stage.statFactors?.Any(s => s?.stat == null || s.stat.defName != "RestFallRateFactor") == true) return false;
            return !stage.multiplyStatChangesBySeverity && stage.statOffsetEffectMultiplier == null && stage.statFactorEffectMultiplier == null
                && stage.capacityFactorEffectMultiplier == null && (stage.statOffsetsBySeverity == null || stage.statOffsetsBySeverity.Count == 0)
                && (stage.statFactorsBySeverity == null || stage.statFactorsBySeverity.Count == 0);
        }
        static IEnumerable<CodeInstruction> GiveTranspiler(IEnumerable<CodeInstruction> instructions) => QualityPatchUtility.ReplaceSingleCall(instructions,
            nativeAdd, AccessTools.Method(typeof(QualityDrugEffects), nameof(AddDose)), () => new[] { new CodeInstruction(OpCodes.Ldarg_2) }, "drug dose capture");
        static void AddDose(Pawn_HealthTracker health, Hediff hediff, BodyPartRecord part, DamageInfo? damage, DamageWorker.DamageResult result, Thing dose)
        {
            if (supported.Contains(hediff.def) && dose != null)
            {
                var state = doses.GetOrCreateValue(hediff);
                state.Snapshot = new DoseSnapshot(QualityEffects.Factor(EffectId.DrugStrength, dose), QualityEffects.Factor(EffectId.DrugDuration, dose));
            }
            health.AddHediff(hediff, part, damage, result);
            if (supported.Contains(hediff.def)) Invalidate(hediff.pawn);
        }
        static void BeforeMerge(Hediff __instance, out float __state) => __state = __instance.Severity;
        static void AfterMerge(Hediff __instance, Hediff other, bool __result, float __state)
        {
            if (!__result || !supported.Contains(__instance.def)) return;
            if (!doses.TryGetValue(__instance, out var old) && !doses.TryGetValue(other, out _)) return;
            var incoming = doses.TryGetValue(other, out var added) ? added.Snapshot : null;
            var snapshot = DoseSnapshot.Merge(old?.Snapshot, __state, incoming, other.Severity);
            var state = doses.GetOrCreateValue(__instance);
            state.Snapshot = snapshot; state.View = null;
            // Severity/stage may be unchanged at the native cap although the captured benefit changed.
            Invalidate(__instance.pawn);
        }
        static void Invalidate(Pawn pawn)
        {
            if (pawn == null) return;
            pawn.health?.hediffSet?.DirtyCache();
            foreach (var stat in affectedStats) stat.Worker.ClearCacheForThing(pawn);
        }
        static void ExposeDose(Hediff __instance)
        {
            if (!supported.Contains(__instance.def)) return;
            var state = doses.GetOrCreateValue(__instance);
            Scribe_Values.Look(ref state.Snapshot.Strength, "qEverythingDrugStrength", 1f);
            Scribe_Values.Look(ref state.Snapshot.Duration, "qEverythingDrugDuration", 1f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                state.Snapshot = new DoseSnapshot(state.Snapshot.Strength, state.Snapshot.Duration);
                state.View = null;
            }
        }
        static void Decay(HediffComp_SeverityPerDay __instance, ref float __result)
        {
            if (__result < 0 && supported.Contains(__instance.parent.def) && doses.TryGetValue(__instance.parent, out var dose))
                __result /= dose.Snapshot.Duration;
        }
        static T Copy<T>(T value) where T : class => (T)clone.Invoke(value, null);
        static void Stage(Hediff __instance, ref HediffStage __result)
        {
            if (__result == null || !supported.Contains(__instance.def) || !doses.TryGetValue(__instance, out var dose) || dose.Snapshot.Strength == 1) return;
            if (dose.View == null || dose.Source != __result)
            {
                float strength = dose.Snapshot.Strength;
                dose.Source = __result;
                var view = Copy(__result);
                if (view.painFactor < 1) view.painFactor = Math.Max(0, 1 + (view.painFactor - 1) * strength);
                if (view.painOffset < 0) view.painOffset *= strength;
                if (view.capMods != null) view.capMods = view.capMods.Select(mod =>
                {
                    if (!capacities.Contains(mod.capacity.defName) || mod.offset <= 0) return mod;
                    var copy = Copy(mod); copy.offset *= strength; return copy;
                }).ToList();
                if (view.statOffsets != null) view.statOffsets = view.statOffsets.Select(mod =>
                {
                    if (!positiveStats.Contains(mod.stat.defName) || mod.value <= 0) return mod;
                    var copy = Copy(mod); copy.value *= strength; return copy;
                }).ToList();
                if (view.statFactors != null) view.statFactors = view.statFactors.Select(mod =>
                {
                    if (mod.stat.defName != "RestFallRateFactor" || mod.value < 0 || mod.value >= 1) return mod;
                    var copy = Copy(mod); copy.value = Math.Max(0, 1 + (mod.value - 1) * strength); return copy;
                }).ToList();
                dose.View = view;
            }
            __result = dose.View;
        }
        static void Mood(Thought __instance, ref float __result)
        {
            var def = __instance.def;
            if (__result <= 0 || def.workerClass != typeof(ThoughtWorker_Hediff) || def.hediff == null || !supported.Contains(def.hediff)) return;
            var high = __instance.pawn?.health?.hediffSet?.GetFirstHediffOfDef(def.hediff);
            if (high != null && doses.TryGetValue(high, out var dose)) __result *= dose.Snapshot.Strength;
        }
        static IEnumerable<CodeInstruction> NeedTranspiler(IEnumerable<CodeInstruction> instructions) => QualityPatchUtility.ReplaceSingleCall(instructions,
            AccessTools.PropertySetter(typeof(Need), "CurLevel"), AccessTools.Method(typeof(QualityDrugEffects), nameof(SetNeed)),
            () => new[] { new CodeInstruction(OpCodes.Ldarg_2) }, "drug rest benefit");
        static void SetNeed(Need need, float target, Thing dose)
        {
            float delta = target - need.CurLevel;
            if (need.def == NeedDefOf.Rest && delta > 0) target = need.CurLevel + delta * QualityEffects.Factor(EffectId.DrugStrength, dose);
            need.CurLevel = target;
        }
        static IEnumerable<CodeInstruction> PsyfocusTranspiler(IEnumerable<CodeInstruction> instructions) => QualityPatchUtility.ReplaceSingleCall(instructions,
            AccessTools.Method(typeof(Pawn_PsychicEntropyTracker), "OffsetPsyfocusDirectly"), AccessTools.Method(typeof(QualityDrugEffects), nameof(Psyfocus)),
            () => new[] { new CodeInstruction(OpCodes.Ldarg_2) }, "drug psyfocus benefit");
        static void Psyfocus(Pawn_PsychicEntropyTracker tracker, float offset, Thing dose) => tracker.OffsetPsyfocusDirectly(offset > 0 ? offset * QualityEffects.Factor(EffectId.DrugStrength, dose) : offset);
    }
}
