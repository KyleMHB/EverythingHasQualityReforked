using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ConfigurableQuality
{
    internal static class QualityLinkedFurniture
    {
        private static readonly Dictionary<ThingDef, int> sources = new Dictionary<ThingDef, int>();
        private static bool registered, active;

        public static void Register()
        {
            if (registered) return;
            registered = true;
            string unsupported = "QEverything.Furniture.Unsupported".Translate().ToString();
            foreach (var entry in QualityCatalog.Entries)
            {
                var def = entry.Def;
                int kind = def.defName == "Dresser" ? 0 : def.defName == "EndTable" ? 1 : -1;
                var props = def.GetCompProperties<CompProperties_Facility>();
                // Exact native seeds and ordinary components only: no label guessing or inferred modded support.
                bool supported = kind >= 0 && props?.compClass == typeof(CompFacility) && def.GetCompProperties<CompProperties_Power>() == null &&
                    props.statOffsets?.Any(s => s.stat == StatDefOf.Comfort && s.value > 0f) == true &&
                    !props.statOffsets.Any(s => s.stat == StatDefOf.BedRestEffectiveness && s.value != 0f);
                QualityEffects.SetUnsupported(EffectId.LinkedFurniture, def, supported ? null : unsupported);
                if (supported) sources.Add(def, kind);
            }
            QualityEffects.SetCapability(EffectId.LinkedFurniture, sources.Count > 0, "QEverything.Furniture.Support".Translate());
            active = sources.Count > 0 && QualitySettingsState.ActiveSnapshot.Effects.TryGetValue(nameof(EffectId.LinkedFurniture), out var effect) && effect.Enabled;
            if (!active) return;
            var harmony = new Harmony("kylemhb.configurablequality.furniture");
            try
            {
                var added = AccessTools.Method(typeof(CompAffectedByFacilities), "Notify_NewLink");
                var removed = AccessTools.Method(typeof(CompAffectedByFacilities), "Notify_LinkRemoved");
                var relinked = AccessTools.Method(typeof(CompAffectedByFacilities), "LinkToNearbyFacilities");
                var unlinked = AccessTools.Method(typeof(CompAffectedByFacilities), "UnlinkAll");
                if (added == null || removed == null || relinked == null || unlinked == null) throw new InvalidOperationException("Native facility link notifications are missing.");
                harmony.Patch(added, postfix: Method(nameof(LinkChanged)));
                harmony.Patch(removed, postfix: Method(nameof(LinkChanged)));
                // Native receiver relinking writes its lists directly, bypassing receiver notifications.
                harmony.Patch(relinked, postfix: Method(nameof(LinkChanged)));
                harmony.Patch(unlinked, postfix: Method(nameof(LinkChanged)));
                harmony.Patch(AccessTools.Method(typeof(CompQuality), nameof(CompQuality.SetQuality)), prefix: Method(nameof(BeforeQuality)), postfix: Method(nameof(AfterQuality)));
                // Despawn unlinks native facilities. Clear both sides around the lifecycle operation,
                // since the reverse list is emptied by its native implementation.
                harmony.Patch(AccessTools.Method(typeof(CompFacility), nameof(CompFacility.PostDeSpawn)), prefix: Method(nameof(FacilityLifecycle)), postfix: Method(nameof(FacilityLifecycle)));
                harmony.Patch(AccessTools.Method(typeof(CompFacility), nameof(CompFacility.PostSpawnSetup)), postfix: Method(nameof(FacilityLifecycle)));
                var stat = StatDefOf.BedRestEffectiveness;
                stat.parts = stat.parts ?? new List<StatPart>();
                stat.parts.Add(new StatPart_LinkedFurniture { parentStat = stat });
            }
            catch (Exception error)
            {
                active = false;
                QualityEffects.SetCapability(EffectId.LinkedFurniture, false, error.Message);
                Log.Warning("QEverything linked furniture unavailable: " + error.Message);
            }
        }
        private static HarmonyMethod Method(string name) => new HarmonyMethod(typeof(QualityLinkedFurniture), name);

        internal static float Contribution(Thing bed)
        {
            if (!active || !(bed is Building_Bed) || !bed.Spawned) return 0f;
            var receiver = bed.TryGetComp<CompAffectedByFacilities>();
            if (receiver == null || receiver.GetType() != typeof(CompAffectedByFacilities)) return 0f;
            var contribution = new QualityFurnitureContribution();
            var links = receiver.LinkedFacilitiesListForReading;
            for (int i = 0; i < links.Count; i++)
            {
                var source = links[i];
                if (source == null || !source.Spawned || !sources.TryGetValue(source.def, out int kind) ||
                    source.TryGetComp<CompFacility>()?.GetType() != typeof(CompFacility) ||
                    source.TryGetComp<CompQuality>() == null || !QualityCatalog.Participates(source.def) || !receiver.IsFacilityActive(source)) continue;
                contribution.Offer(kind, QualityEffects.Factor(EffectId.LinkedFurniture, source));
            }
            var options = QualitySettingsState.ActiveSnapshot;
            return contribution.Delta(options.LinkedFurnitureBonusCap, options.LinkedFurniturePenaltyCap);
        }
        private static void Clear(Thing bed)
        {
            if (bed is Building_Bed) StatDefOf.BedRestEffectiveness.Worker.ClearCacheForThing(bed);
        }
        private static void ClearLinked(CompFacility facility)
        {
            if (!active || facility?.parent == null || !sources.ContainsKey(facility.parent.def)) return;
            var beds = facility.LinkedBuildings;
            for (int i = 0; i < beds.Count; i++) Clear(beds[i]);
        }
        private static void LinkChanged(CompAffectedByFacilities __instance)
        {
            if (active) Clear(__instance.parent);
        }
        private static void FacilityLifecycle(CompFacility __instance) => ClearLinked(__instance);
        private static void BeforeQuality(CompQuality __instance, out QualityCategory __state) => __state = __instance.Quality;
        [HarmonyPriority(Priority.Last)]
        private static void AfterQuality(CompQuality __instance, QualityCategory __state)
        {
            if (!active || __instance.Quality == __state) return;
            var thing = __instance.parent;
            Clear(thing);
            if (thing != null && sources.ContainsKey(thing.def)) ClearLinked(thing.TryGetComp<CompFacility>());
        }
    }

    // Added after the bed's existing native quality parts. Facility Comfort is never changed.
    internal sealed class StatPart_LinkedFurniture : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing) val += QualityLinkedFurniture.Contribution(req.Thing);
        }
        public override string ExplanationPart(StatRequest req)
        {
            float delta = req.HasThing ? QualityLinkedFurniture.Contribution(req.Thing) : 0f;
            if (delta == 0f) return null;
            var options = QualitySettingsState.ActiveSnapshot;
            return "QEverything.Furniture.Explanation".Translate(delta.ToStringWithSign("0.##%"), options.LinkedFurnitureBonusCap.ToStringPercent(), options.LinkedFurniturePenaltyCap.ToStringPercent());
        }
    }
}
