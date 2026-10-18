using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QualityEverything
{
    internal static class QualityDurabilityProduction
    {
        private static readonly Dictionary<EffectId, HashSet<ThingDef>> targets = new Dictionary<EffectId, HashSet<ThingDef>>();
        private static readonly Dictionary<ThingDef, List<StatDef>> activeStats = new Dictionary<ThingDef, List<StatDef>>();
        private static readonly HashSet<ThingDef> activeDurability = new HashSet<ThingDef>();
        private static readonly Dictionary<Thing, int> creating = new Dictionary<Thing, int>();
        private static bool registered;
        private static readonly EffectId[] durabilityIds = { EffectId.BuildingDurability, EffectId.WeaponDurability, EffectId.ApparelDurability };

        public static void Register()
        {
            if (registered) return;
            registered = true;
            Add(EffectId.BuildingDurability, StatDefOf.MaxHitPoints);
            Add(EffectId.WeaponDurability, StatDefOf.MaxHitPoints);
            Add(EffectId.ApparelDurability, StatDefOf.MaxHitPoints);
            Add(EffectId.WorktableSpeed, StatDefOf.WorkTableWorkSpeedFactor);
            Add(EffectId.ResearchSpeed, StatDefOf.ResearchSpeedFactor);
            Add(EffectId.DoorSpeed, StatDefOf.DoorOpenSpeed);
        }

        private static bool Matches(EffectId id, ThingDef def)
        {
            if (def.thingClass == null || def.IsBlueprint || def.IsFrame) return false;
            switch (id)
            {
                case EffectId.BuildingDurability: return def.useHitPoints && typeof(Building).IsAssignableFrom(def.thingClass);
                case EffectId.WeaponDurability: return def.useHitPoints && def.IsWeapon && !typeof(Building).IsAssignableFrom(def.thingClass);
                case EffectId.ApparelDurability: return def.useHitPoints && def.IsApparel && !def.IsWeapon && !typeof(Building).IsAssignableFrom(def.thingClass);
                case EffectId.WorktableSpeed: return typeof(Building_WorkTable).IsAssignableFrom(def.thingClass) && !typeof(Building_ResearchBench).IsAssignableFrom(def.thingClass);
                case EffectId.ResearchSpeed: return typeof(Building_ResearchBench).IsAssignableFrom(def.thingClass);
                case EffectId.DoorSpeed: return typeof(Building_Door).IsAssignableFrom(def.thingClass);
                default: return false;
            }
        }

        private static void Add(EffectId id, StatDef stat)
        {
            var eligible = targets[id] = new HashSet<ThingDef>();
            // A native/custom quality part owns its curve. Unknown quality-named parts are conservatively protected too.
            bool existingQuality = stat.parts?.Any(p => !(p is StatPart_OptionalQuality) && (p is StatPart_Quality || p.GetType().Name.IndexOf("Quality", StringComparison.OrdinalIgnoreCase) >= 0)) == true;
            foreach (var entry in QualityCatalog.Entries)
            {
                if (!Matches(id, entry.Def))
                {
                    QualityEffects.SetUnsupported(id, entry.Def, "QEverything.Durability.NotApplicable".Translate());
                    continue;
                }
                if (existingQuality)
                {
                    QualityEffects.SetUnsupported(id, entry.Def, "QEverything.Durability.NativeCurve".Translate());
                    continue;
                }
                eligible.Add(entry.Def);
                if (QualityCatalog.Participates(entry.Def) && QualitySettingsState.ActiveSnapshot.Effects.TryGetValue(id.ToString(), out var effect) && effect.Enabled &&
                    entry.Def.comps?.Any(p => p?.compClass != null && typeof(CompQuality).IsAssignableFrom(p.compClass)) == true)
                {
                    if (!activeStats.TryGetValue(entry.Def, out var affected)) activeStats[entry.Def] = affected = new List<StatDef>();
                    if (!affected.Contains(stat)) affected.Add(stat);
                    if (durabilityIds.Contains(id)) activeDurability.Add(entry.Def);
                }
            }
            QualityEffects.SetCapability(id, !existingQuality, (existingQuality ? "QEverything.Durability.NativeCurve" : "QEverything.Durability.Available").Translate());
            if (existingQuality) return;
            if (stat.parts == null) stat.parts = new List<StatPart>();
            stat.parts.Add(new StatPart_OptionalQuality(id) { parentStat = stat });
        }

        internal static float Factor(EffectId id, Thing thing) => thing != null && targets.TryGetValue(id, out var set) && set.Contains(thing.def) ? QualityEffects.Factor(id, thing) : 1f;
        private static bool SupportedDurability(Thing thing) => thing != null && durabilityIds.Any(id => targets.TryGetValue(id, out var set) && set.Contains(thing.def));
        private static bool Durable(Thing thing) => SupportedDurability(thing) && thing.TryGetComp<CompQuality>() != null;

        private static void ClearActive(Thing thing)
        {
            // Clear immutable as well as short-lived stat caches before observing a new maximum.
            if (thing != null && activeStats.TryGetValue(thing.def, out var affected))
                foreach (var stat in affected) stat.Worker.ClearCacheForThing(thing);
        }

        [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.PostMake))]
        private static class Creation
        {
            private static void Prefix(ThingWithComps __instance, out bool __state)
            {
                __state = registered && activeDurability.Contains(__instance.def) && __instance.TryGetComp<CompQuality>() == null && !creating.ContainsKey(__instance);
                if (__state) creating[__instance] = __instance.MaxHitPoints;
            }
            private static void Postfix(ThingWithComps __instance, bool __state)
            {
                if (!__state || !creating.TryGetValue(__instance, out int baseline)) return;
                if (Durable(__instance) && !__instance.Destroyed)
                {
                    StatDefOf.MaxHitPoints.Worker.ClearCacheForThing(__instance);
                    __instance.HitPoints = QualityCondition.Remap(__instance.HitPoints, baseline, __instance.MaxHitPoints);
                }
            }
            private static Exception Finalizer(ThingWithComps __instance, bool __state, Exception __exception)
            {
                if (__state) creating.Remove(__instance);
                return __exception;
            }
        }

        private struct QualityChange
        {
            public QualityCategory Quality;
            public int Hp, Maximum;
            public bool Remap, Affected;
        }

        [HarmonyPatch(typeof(CompQuality), nameof(CompQuality.SetQuality))]
        private static class ChangedQuality
        {
            private static void Prefix(CompQuality __instance, out QualityChange __state)
            {
                var thing = __instance.parent;
                __state = new QualityChange { Quality = __instance.Quality };
                if (!registered || thing == null || !activeStats.ContainsKey(thing.def)) return;
                __state.Affected = true;
                ClearActive(thing);
                __state.Remap = activeDurability.Contains(thing.def) && Durable(thing) && !creating.ContainsKey(thing) && !thing.Destroyed && thing.HitPoints > 0;
                if (__state.Remap) { __state.Hp = thing.HitPoints; __state.Maximum = thing.MaxHitPoints; }
            }
            // Read actual final quality, allowing other patches to constrain the incoming tier.
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(CompQuality __instance, QualityChange __state)
            {
                if (!__state.Affected || __state.Quality == __instance.Quality) return;
                var thing = __instance.parent;
                if (thing == null) return;
                ClearActive(thing);
                if (__state.Remap && !thing.Destroyed && thing.HitPoints > 0)
                    thing.HitPoints = QualityCondition.Remap(__state.Hp, __state.Maximum, thing.MaxHitPoints);
            }
        }

        [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.ExposeData))]
        private static class Loaded
        {
            private static void Postfix(ThingWithComps __instance)
            {
                if (!registered || Scribe.mode != LoadSaveMode.PostLoadInit || !SupportedDurability(__instance) || __instance.Destroyed) return;
                // Includes held items and removal of mod-added quality support. Restart changes never repair.
                StatDefOf.MaxHitPoints.Worker.ClearCacheForThing(__instance);
                __instance.HitPoints = QualityCondition.ClampLoaded(__instance.HitPoints, __instance.MaxHitPoints);
            }
        }
    }

    internal sealed class StatPart_OptionalQuality : StatPart
    {
        private readonly EffectId effect;
        public StatPart_OptionalQuality(EffectId effect) { this.effect = effect; }
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing) val *= QualityDurabilityProduction.Factor(effect, req.Thing);
        }
        public override string ExplanationPart(StatRequest req)
        {
            float factor = req.HasThing ? QualityDurabilityProduction.Factor(effect, req.Thing) : 1f;
            return factor == 1f ? null : "QEverything.Durability.Explanation".Translate(("QEverything.Options.Effect." + effect).Translate(), factor.ToStringPercent());
        }
    }
}
