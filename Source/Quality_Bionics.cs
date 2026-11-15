using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
{
    // An installed part is a hediff, so the item's quality is copied into a hediff comp at surgery and back onto the item when it comes out.
    // Efficiency and hit points are scaled per hediff; shared HediffDef values are never changed.
    internal static class QualityBionics
    {
        internal const string QualityBionicsId = "assassinsbro.qualitybionicsremastered";
        internal const string EbfId = "V1024.EBFramework";
        private static readonly Dictionary<HediffDef, ThingDef> parts = new Dictionary<HediffDef, ThingDef>();
        private static readonly HashSet<ThingDef> items = new HashSet<ThingDef>();
        private static bool discovered, registered;

        private static string Text(string suffix) => ("QEverything.Bionics." + suffix).Translate();
        private static bool Enabled(EffectId id) => QualitySettingsState.ActiveSnapshot.Effects.TryGetValue(id.ToString(), out var effect) && effect.Enabled;

        // Any replacement part that returns an item when removed: peg legs and hooks as well as bionics, from any mod.
        private static void Discover()
        {
            if (discovered || DefDatabase<HediffDef>.DefCount == 0) return;
            discovered = true;
            foreach (var hediff in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                var item = hediff.spawnThingOnRemoved;
                if (hediff.addedPartProps == null || item == null || hediff.hediffClass == null || !typeof(Hediff_AddedPart).IsAssignableFrom(hediff.hediffClass)) continue;
                if (item.thingClass == null || !typeof(ThingWithComps).IsAssignableFrom(item.thingClass) || item.IsNaturalOrgan) continue;
                parts[hediff] = item;
                items.Add(item);
            }
        }
        internal static bool IsPartItem(ThingDef def)
        {
            Discover();
            return def != null && items.Contains(def);
        }

        internal static void Register()
        {
            if (registered) return;
            registered = true;
            Discover();
            if (ModsConfig.IsActive(QualityBionicsId))
            {
                // Quality Bionics Remastered changes the shared part efficiency, so scaling here as well would apply quality twice.
                foreach (var id in new[] { EffectId.BionicEfficiency, EffectId.BionicHitPoints }) QualityEffects.SetCapability(id, false, Text("Foreign"), true);
                return;
            }
            // Always added, so an installed part keeps its quality while the effects are switched off.
            foreach (var hediff in parts.Keys)
            {
                if (hediff.comps == null) hediff.comps = new List<HediffCompProperties>();
                if (!hediff.comps.Any(c => c?.compClass == typeof(HediffComp_PartQuality))) hediff.comps.Add(new HediffCompProperties { compClass = typeof(HediffComp_PartQuality) });
            }
            var harmony = new Harmony("kylemhb.configurablequality.bionics");
            bool transfer = PatchInstallers(harmony)
                && Patch(harmony, AccessTools.Method(typeof(MedicalRecipesUtility), nameof(MedicalRecipesUtility.SpawnThingsFromHediffs)), prefix: nameof(BeginRemoval), finalizer: nameof(EndRemoval))
                && Patch(harmony, AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(WipeMode) }), postfix: nameof(Spawned));
            // Part efficiency is recalculated constantly, so it is patched only while its effect is on. The transfer hooks always run.
            bool efficiency = transfer && (!Enabled(EffectId.BionicEfficiency)
                || Patch(harmony, AccessTools.Method(typeof(PawnCapacityUtility), nameof(PawnCapacityUtility.CalculatePartEfficiency)), postfix: nameof(Efficiency)));
            string missing = Text(transfer ? "Conflict" : "TransferConflict");
            if (parts.Count == 0) QualityEffects.SetCapability(EffectId.BionicEfficiency, false, Text("NoParts"), noItems: true);
            else QualityEffects.SetCapability(EffectId.BionicEfficiency, efficiency, efficiency ? Text("EfficiencyAvailable") : missing);
            RegisterHitPoints(harmony, transfer, missing);
        }

        // Hit points need Elite Bionics Framework, which gives each body part its own maximum. It is found by name, so it stays optional.
        private static void RegisterHitPoints(Harmony harmony, bool transfer, string missing)
        {
            if (!ModsConfig.IsActive(EbfId)) { QualityEffects.SetCapability(EffectId.BionicHitPoints, false, Text("NeedsEbf"), neededMod: "Elite Bionics Framework"); return; }
            if (parts.Count == 0) { QualityEffects.SetCapability(EffectId.BionicHitPoints, false, Text("NoParts"), noItems: true); return; }
            if (!transfer) { QualityEffects.SetCapability(EffectId.BionicHitPoints, false, missing); return; }
            // Current EBF builds route every part maximum through EBFEndpoints; older ones only through VanillaExtender. Patching one avoids scaling twice.
            bool patch = Enabled(EffectId.BionicHitPoints);
            var endpoint = AccessTools.TypeByName("EBF.EBFEndpoints");
            var method = endpoint == null ? null : AccessTools.Method(endpoint, "GetMaxHealthWithEBF", new[] { typeof(BodyPartRecord), typeof(Pawn), typeof(bool) });
            bool ready = method != null && (!patch || Patch(harmony, method, postfix: nameof(EbfMaxHealth)));
            if (method == null)
            {
                var extender = AccessTools.TypeByName("EBF.VanillaExtender");
                method = extender == null ? null : AccessTools.Method(extender, "GetPartMaxHealth", new[] { typeof(HediffSet), typeof(BodyPartRecord) });
                ready = method != null && (!patch || Patch(harmony, method, postfix: nameof(EbfPartMaxHealth)));
            }
            QualityEffects.SetCapability(EffectId.BionicHitPoints, ready, Text(ready ? "HitPointsAvailable" : "EbfChanged"));
        }

        private static bool Patch(Harmony harmony, MethodBase method, string prefix = null, string postfix = null, string finalizer = null)
        {
            if (method == null) return false;
            try
            {
                harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(QualityBionics), prefix), postfix == null ? null : new HarmonyMethod(typeof(QualityBionics), postfix),
                    finalizer: finalizer == null ? null : new HarmonyMethod(typeof(QualityBionics), finalizer));
                return true;
            }
            catch (Exception error)
            {
                Log.Warning("Configurable Quality: bionics quality left " + method.Name + " unchanged: " + error.Message);
                return false;
            }
        }
        // Mods install parts through their own recipe workers too, so every ApplyOnPawn implementation gets the transfer.
        private static bool PatchInstallers(Harmony harmony)
        {
            var signature = new[] { typeof(Pawn), typeof(BodyPartRecord), typeof(Pawn), typeof(List<Thing>), typeof(Bill) };
            bool vanilla = false;
            foreach (var type in GenTypes.AllSubclasses(typeof(RecipeWorker)))
            {
                var method = AccessTools.DeclaredMethod(type, nameof(RecipeWorker.ApplyOnPawn), signature);
                if (method == null || method.IsAbstract) continue;
                bool patched = Patch(harmony, method, postfix: nameof(Installed));
                if (type == typeof(Recipe_InstallArtificialBodyPart)) vanilla = patched;
            }
            return vanilla;
        }

        internal static float Factor(EffectId id, Hediff hediff)
        {
            var comp = hediff?.TryGetComp<HediffComp_PartQuality>();
            if (comp == null || !comp.IsSet || !QualityEffects.Available(id) || !QualityCatalog.Participates(hediff.def.spawnThingOnRemoved)) return 1f;
            return QualityEffects.TierFactor(id, (int)comp.Quality);
        }
        internal static bool Shows => QualityEffects.Available(EffectId.BionicEfficiency) || QualityEffects.Available(EffectId.BionicHitPoints);

        // Postfixes receive arguments by position: modded overrides name them differently.
        private static void Installed(RecipeWorker __instance, Pawn __0, BodyPartRecord __1, List<Thing> __3)
        {
            var added = __instance.recipe?.addsHediff;
            if (added == null || __0?.health?.hediffSet == null || !parts.TryGetValue(added, out var item)) return;
            var source = __3?.FirstOrDefault(t => t?.def == item)?.TryGetComp<CompQuality>();
            if (source == null) return;
            // A failed surgery adds no part. Match the operated part first: a pawn can carry several parts of one kind.
            var hediffs = __0.health.hediffSet.hediffs;
            var target = hediffs.LastOrDefault(h => h.def == added && h.Part == __1 && h.TryGetComp<HediffComp_PartQuality>()?.IsSet == false)
                ?? hediffs.LastOrDefault(h => h.def == added && h.TryGetComp<HediffComp_PartQuality>()?.IsSet == false);
            var comp = target?.TryGetComp<HediffComp_PartQuality>();
            if (comp == null) return;
            comp.Quality = source.Quality;
            __0.health.capacities?.Notify_CapacityLevelsDirty();
            __0.health.summaryHealth?.Notify_HealthChanged();
        }

        // SpawnThingsFromHediffs recurses into child parts, so each call keeps its own list of parts it is about to drop.
        private static readonly List<List<KeyValuePair<ThingDef, QualityCategory?>>> removing = new List<List<KeyValuePair<ThingDef, QualityCategory?>>>();
        private static void BeginRemoval(Pawn __0, BodyPartRecord __1)
        {
            var dropping = new List<KeyValuePair<ThingDef, QualityCategory?>>();
            if (__0?.health?.hediffSet != null)
                foreach (var hediff in __0.health.hediffSet.hediffs)
                {
                    if (hediff.Part != __1 || hediff.def.spawnThingOnRemoved == null) continue;
                    var comp = hediff.TryGetComp<HediffComp_PartQuality>();
                    dropping.Add(new KeyValuePair<ThingDef, QualityCategory?>(hediff.def.spawnThingOnRemoved, comp != null && comp.IsSet ? comp.Quality : (QualityCategory?)null));
                }
            removing.Add(dropping);
        }
        private static Exception EndRemoval(Exception __exception)
        {
            if (removing.Count > 0) removing.RemoveAt(removing.Count - 1);
            return __exception;
        }
        private static void Spawned(Thing __result)
        {
            if (removing.Count == 0 || __result == null) return;
            var dropping = removing[removing.Count - 1];
            int index = dropping.FindIndex(p => p.Key == __result.def);
            if (index < 0) return;
            var quality = dropping[index].Value;
            dropping.RemoveAt(index);
            if (quality != null) __result.TryGetComp<CompQuality>()?.SetQuality(quality.Value, ArtGenerationContext.Colony);
        }

        // Matches vanilla: a part under an added parent part works as well as that parent; otherwise its own added part decides.
        private static Hediff GoverningPart(HediffSet set, BodyPartRecord part, bool ignoreAddedParts)
        {
            for (var record = part?.parent; record != null; record = record.parent)
                if (set.HasDirectlyAddedPartFor(record)) return set.GetFirstHediffMatchingPart<Hediff_AddedPart>(record);
            if (ignoreAddedParts || part == null) return null;
            foreach (var hediff in set.hediffs) if (hediff is Hediff_AddedPart && hediff.Part == part) return hediff;
            return null;
        }
        private static void Efficiency(HediffSet __0, BodyPartRecord __1, bool __2, ref float __result)
        {
            if (__result <= 0f || __0 == null) return;
            var hediff = GoverningPart(__0, __1, __2);
            if (hediff != null) __result *= Factor(EffectId.BionicEfficiency, hediff);
        }

        private static float HitPointFactor(HediffSet set, BodyPartRecord part)
        {
            if (set == null || part == null) return 1f;
            foreach (var hediff in set.hediffs)
                if (hediff is Hediff_AddedPart && hediff.Part == part) return Factor(EffectId.BionicHitPoints, hediff);
            return 1f;
        }
        private static void Scale(ref float result, float factor)
        {
            if (factor != 1f) result = Mathf.Max(1f, Mathf.RoundToInt(result * factor));
        }
        private static void EbfMaxHealth(BodyPartRecord __0, Pawn __1, ref float __result) => Scale(ref __result, HitPointFactor(__1?.health?.hediffSet, __0));
        private static void EbfPartMaxHealth(HediffSet __0, BodyPartRecord __1, ref float __result) => Scale(ref __result, HitPointFactor(__0, __1));
    }

    internal sealed class HediffComp_PartQuality : HediffComp
    {
        // -1 means unknown: parts installed before this mod, or from an item without quality, stay neutral.
        private int quality = -1;
        public bool IsSet => quality >= 0;
        public QualityCategory Quality
        {
            get => IsSet ? (QualityCategory)quality : QualityCategory.Normal;
            set => quality = (int)value;
        }
        public override void CompExposeData() => Scribe_Values.Look(ref quality, "configurableQuality", -1);
        public override string CompLabelInBracketsExtra => IsSet && QualityBionics.Shows ? Quality.GetLabel() : null;
    }
}
