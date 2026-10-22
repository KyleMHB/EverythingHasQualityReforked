using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QualityEverything
{
    internal static class QualitySettingsState
    {
        // Runtime callers only see copies. The startup snapshot is never exposed for mutation.
        private static QualityConfigurationSession session = new QualityConfigurationSession(new QualityConfiguration());
        public static QualityConfiguration Active => session.Active;
        internal static QualityConfiguration ActiveSnapshot => session.ActiveSnapshot;
        public static QualityConfiguration Saved => session.Saved;
        public static QualityConfiguration Draft => session.Draft;
        public static int Revision
        {
            get; private set;
        }
        private static int comparedRevision = -1;
        private static bool dirty, restartPending;
        private static string Identity(QualityConfiguration configuration)
        {
            var copy = configuration.Clone();
            copy.LegacyBackup = "";
            return copy.Serialize();
        }
        private static void Compare()
        {
            if (comparedRevision == Revision) return;
            dirty = Identity(Draft) != Identity(Saved);
            restartPending = Identity(Saved) != Identity(ActiveSnapshot);
            comparedRevision = Revision;
        }
        public static bool Dirty
        {
            get
            {
                Compare();
                return dirty;
            }
        }
        public static bool RestartPending
        {
            get
            {
                Compare();
                return restartPending;
            }
        }
        public static void Changed()
        {
            Revision++;
        }
        public static void ReplaceDraft(QualityConfiguration configuration) { session.ReplaceDraft(configuration); Changed(); }
        public static void Discard()
        {
            session.Discard();
            Changed();
        }
        public static List<string> Save()
        {
            var errors = session.Save();
            if (errors.Count != 0) return errors;
            Mod_QEverything.settings?.Write();
            Changed();
            return errors;
        }
        public static void Load(QualityConfiguration configuration)
        {
            var errors = configuration.Validate();
            errors.AddRange(configuration.RecoveredFields);
            if (errors.Count > 0)
            {
                // Repair individual invalid fields without discarding unrelated overrides or unavailable mod entries.
                var defaults = new QualityConfiguration();
                foreach (string key in errors.Distinct())
                {
                    var field = typeof(ProductionSettings).GetField(key);
                    if (field != null) field.SetValue(configuration.Production, field.GetValue(defaults.Production));
                    else if (configuration.Effects.ContainsKey(key)) configuration.Effects[key] = defaults.Effects.ContainsKey(key) ? defaults.Effects[key].Clone() : new EffectSettings();
                    else if (key == "LinkedFurnitureBonusCap") configuration.LinkedFurnitureBonusCap = .02f;
                    else if (key == "LinkedFurniturePenaltyCap") configuration.LinkedFurniturePenaltyCap = .005f;
                }
                foreach (var field in typeof(ProductionSettings).GetFields().Where(f => f.Name.StartsWith("min")))
                {
                    var max = typeof(ProductionSettings).GetField("max" + field.Name.Substring(3));
                    if (max != null && (int)field.GetValue(configuration.Production) > (int)max.GetValue(configuration.Production)) field.SetValue(configuration.Production, max.GetValue(configuration.Production));
                }
                Log.Warning("Everything Has Quality: repaired invalid saved options: " + string.Join(", ", errors));
            }
            session = new QualityConfigurationSession(configuration);
            foreach (var field in typeof(ProductionSettings).GetFields()) typeof(ModSettings_QEverything).GetField(field.Name)?.SetValue(null, field.GetValue(ActiveSnapshot.Production));
            Changed();
        }
        public static readonly Dictionary<string, string> GroupFields = QualityConfiguration.GroupFields;
    }

    internal sealed class QualityCatalogEntry
    {
        public ThingDef Def;
        public RuleTarget Target;
        public bool NativeQuality;
        public string Label, Source;
    }

    internal static class QualityCatalog
    {
        public static readonly List<QualityCatalogEntry> Entries = new List<QualityCatalogEntry>();
        private static readonly Dictionary<ThingDef, QualityCatalogEntry> byDef = new Dictionary<ThingDef, QualityCatalogEntry>();
        private static readonly HashSet<QualityBucket> activeGroups = new HashSet<QualityBucket>();
        private static readonly Dictionary<ThingDef, RuleResolution> activeAssignment = new Dictionary<ThingDef, RuleResolution>(), activeParticipation = new Dictionary<ThingDef, RuleResolution>();
        public static int Revision
        {
            get; private set;
        }
        public static void Build()
        {
            if (Entries.Count > 0) return;
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var paths = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var category in def.thingCategories ?? new List<ThingCategoryDef>())
                {
                    var chain = new List<ThingCategoryDef>();
                    var seen = new HashSet<ThingCategoryDef>();
                    for (var current = category; current != null && seen.Add(current); current = current.parent) chain.Add(current);
                    for (int i = chain.Count - 1; i >= 0; i--)
                    {
                        string id = chain[i].defName;
                        int depth = chain.Count - i;
                        if (!paths.TryGetValue(id, out int old) || depth > old) paths[id] = depth;
                    }
                }
                bool supported = !QualityDefUtility.IsExcludedFromQuality(def) && !def.IsBlueprint && !def.IsFrame && def.thingClass != null && typeof(ThingWithComps).IsAssignableFrom(def.thingClass) && QualityDefUtility.GetBucket(def) != QualityBucket.None;
                bool native = def.comps?.Any(p => p?.compClass != null && typeof(CompQuality).IsAssignableFrom(p.compClass)) ?? false;
                if (!supported && !native) continue;
                string source = def.modContentPack?.Name ?? "Core";
                var entry = new QualityCatalogEntry
                {
                    Def = def,
                    Target = new RuleTarget(def.defName, QualityDefUtility.GetBucket(def).ToString(), supported, paths.Select(p => new CategoryPath(p.Key, p.Value)).ToArray()),
                    NativeQuality = native,
                    Label = def.LabelCap,
                    Source = source
                };
                Entries.Add(entry);
                byDef[def] = entry;
                activeAssignment[def] = QualityRules.Resolve(entry.Target, QualitySettingsState.ActiveSnapshot.Assignment);
                if (activeAssignment[def].Enabled) activeGroups.Add(QualityDefUtility.GetBucket(def));
                activeParticipation[def] = QualityRules.Resolve(new RuleTarget(entry.Target.Id, entry.Target.Group, supported || native, entry.Target.Categories), QualitySettingsState.ActiveSnapshot.Participation);
            }
            Entries.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Label, b.Label));
            Revision++;
        }
        public static QualityCatalogEntry Get(ThingDef def) => def != null && byDef.TryGetValue(def, out var result) ? result : null;
        public static bool Assigned(ThingDef def) => def != null && activeAssignment.TryGetValue(def, out var result) && result.Enabled;
        public static bool Participates(ThingDef def) => def != null && activeParticipation.TryGetValue(def, out var result) && result.Enabled;
        public static bool AnyAssigned(params QualityBucket[] groups) => groups.Any(activeGroups.Contains);
    }

    internal static class QualityEffects
    {
        private static readonly string[] effectKeys = Enum.GetNames(typeof(EffectId));
        private static readonly Dictionary<EffectId, string> reasons = new Dictionary<EffectId, string>();
        private static readonly HashSet<EffectId> available = new HashSet<EffectId>();
        public static void SetCapability(EffectId id, bool supported, string reason)
        {
            if (supported) available.Add(id);
            else available.Remove(id);
            reasons[id] = reason;
        }
        private static readonly Dictionary<EffectId, Dictionary<ThingDef, string>> unsupported = new Dictionary<EffectId, Dictionary<ThingDef, string>>();
        public static void SetUnsupported(EffectId id, ThingDef def, string reason)
        {
            if (def == null) return;
            if (!unsupported.TryGetValue(id, out var entries)) unsupported[id] = entries = new Dictionary<ThingDef, string>();
            if (string.IsNullOrEmpty(reason)) entries.Remove(def);
            else entries[def] = reason;
        }
        public static string UnsupportedReason(EffectId id, ThingDef def) => def != null && unsupported.TryGetValue(id, out var entries) && entries.TryGetValue(def, out var reason) ? reason : null;
        public static bool Available(EffectId id) => available.Contains(id);
        public static string Reason(EffectId id) => reasons.TryGetValue(id, out var reason) ? reason : "QEverything.Options.AdapterPending".Translate().ToString();
        public static float Factor(EffectId id, Thing thing)
        {
            int index = (int)id;
            if (index < 0 || index >= effectKeys.Length || !QualitySettingsState.ActiveSnapshot.Effects.TryGetValue(effectKeys[index], out var effect) || !effect.Enabled) return 1f;
            if (thing == null || !Available(id) || UnsupportedReason(id, thing.def) != null || !QualityCatalog.Participates(thing.def)) return 1f;
            var quality = thing.TryGetComp<CompQuality>();
            if (quality == null) return 1f;
            int tier = (int)quality.Quality;
            return tier >= 0 && tier < effect.Factors.Length ? effect.Factors[tier] : 1f;
        }
    }
}
