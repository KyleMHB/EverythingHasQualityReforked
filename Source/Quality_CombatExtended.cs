using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ConfigurableQuality
{
    // Combat Extended keeps its weapon and apparel numbers in item stats, so each effect is a stat part on those stats.
    // No Combat Extended code is referenced: stats are found by name and the effects stay off when it is not loaded.
    internal static partial class QualityEquipmentCombat
    {
        internal const string CombatExtendedId = "CETeam.CombatExtended";
        private sealed class CeStat
        {
            public string Name;
            public bool LowerIsBetter;
            public StatDef Stat;
        }
        private static CeStat Higher(string name) => new CeStat { Name = name };
        private static CeStat Lower(string name) => new CeStat { Name = name, LowerIsBetter = true };
        private static readonly Dictionary<EffectId, CeStat[]> ceStats = new Dictionary<EffectId, CeStat[]>
        {
            { EffectId.CeRange, new[] { Higher("RangedWeapon_RangeMultiplier") } },
            { EffectId.CeHandling, new[] { Lower("RangedWeapon_WarmupMultiplier"), Lower("CE_RangedWeapon_ReloadFactor"), Lower("SwayFactor"), Lower("Recoil") } },
            // Combat Extended already scales the stats below by quality, so these effects wait for the take-over switch.
            { EffectId.CeAccuracy, new[] { Higher("SightsEfficiency"), Lower("ShotSpread") } },
            { EffectId.CeMelee, new[] { Higher("MeleePenetrationFactor"), Higher("ToughnessRating") } },
            { EffectId.CeApparel, new[] { Lower("WornBulk"), Higher("NightVisionEfficiency_Apparel") } }
        };
        internal static readonly EffectId[] CombatExtendedIds = ceStats.Keys.ToArray();
        // Stats this mod scales for each effect, used to refresh an item's cached values when its quality changes.
        private static readonly Dictionary<EffectId, List<StatDef>> ceOwned = new Dictionary<EffectId, List<StatDef>>();

        internal static bool CombatExtendedCandidate(EffectId id, ThingDef def)
        {
            if (def == null || turretWeapons.Contains(def)) return false;
            switch (id)
            {
                case EffectId.CeRange:
                case EffectId.CeHandling:
                case EffectId.CeAccuracy: return def.IsRangedWeapon;
                case EffectId.CeMelee: return def.IsWeapon && def.tools?.Count > 0;
                case EffectId.CeApparel: return def.IsApparel && !def.IsWeapon;
                default: return false;
            }
        }

        private static void RegisterCombatExtended(bool cacheReady)
        {
            bool loaded = ModsConfig.IsActive(CombatExtendedId);
            foreach (var pair in ceStats)
            {
                var id = pair.Key;
                if (!loaded)
                {
                    QualityEffects.SetCapability(id, false, Text("CeMissing"), neededMod: "Combat Extended");
                    continue;
                }
                var owned = ceOwned[id] = new List<StatDef>();
                int native = 0;
                foreach (var entry in pair.Value)
                {
                    entry.Stat = DefDatabase<StatDef>.GetNamedSilentFail(entry.Name);
                    if (entry.Stat == null || !cacheReady) continue;
                    if (entry.Stat.parts?.Any(QualityEffects.IsForeignQualityPart) == true)
                    {
                        if (!QualityEffects.TakesOver(id)) { native++; continue; }
                        QualityEffects.RemoveForeignQualityParts(entry.Stat);
                        QualityEffects.MarkTakenOver("the existing quality curve of " + entry.Stat.defName, id);
                    }
                    if (entry.Stat.parts == null) entry.Stat.parts = new List<StatPart>();
                    entry.Stat.parts.Add(new StatPart_QualityCombatExtended(id, entry.LowerIsBetter) { parentStat = entry.Stat });
                    owned.Add(entry.Stat);
                }
                bool ready = owned.Count > 0;
                RegisterCombat(id, ready, null, false, d => CombatExtendedCandidate(id, d));
                // A partly native group still works for the stats this mod owns; a fully native one waits for take-over.
                if (!ready && native > 0)
                    foreach (var item in QualityCatalog.Entries) QualityEffects.SetUnsupported(id, item.Def, Text("CeNative"));
                if (!ready) QualityEffects.SetCapability(id, false, Text(native > 0 ? "CeNative" : "Conflict"), native > 0);
                else if (QualityEffects.Available(id)) QualityEffects.SetCapability(id, true, Text(native > 0 ? "CePartial" : "CeAvailable"));
            }
        }

        private static void ClearCombatExtended(Thing gear)
        {
            foreach (var pair in ceOwned)
                if (activeTargets.TryGetValue(pair.Key, out var defs) && defs.Contains(gear.def))
                    foreach (var stat in pair.Value) stat.Worker.ClearCacheForThing(gear);
        }
    }

    internal sealed class StatPart_QualityCombatExtended : StatPart
    {
        private readonly EffectId effect;
        private readonly bool lowerIsBetter;
        internal StatPart_QualityCombatExtended(EffectId effect, bool lowerIsBetter) { this.effect = effect; this.lowerIsBetter = lowerIsBetter; }
        // The configured factor is "how much better": a lower-is-better stat such as recoil is divided by it.
        private float Factor(StatRequest req)
        {
            float factor = req.HasThing ? QualityEquipmentCombat.Factor(effect, req.Thing) : 1f;
            return factor <= 0f ? 1f : lowerIsBetter ? 1f / factor : factor;
        }
        public override void TransformValue(StatRequest req, ref float val) => val *= Factor(req);
        public override string ExplanationPart(StatRequest req)
        {
            float factor = Factor(req);
            return factor == 1f ? null : "QEverything.EquipmentCombat.CeExplanation".Translate(factor.ToStringPercent());
        }
    }
}
