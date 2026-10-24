using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ConfigurableQuality
{
    internal static class QualityProgression
    {
        private static readonly string[] Jumpsuits = { "OCARINA_PrisonerJumpsuit_Blue", "OCARINA_PrisonerJumpsuit_Orange", "OCARINA_PrisonerJumpsuit_Red", "OCARINA_PrisonerJumpsuit_Custom" };
        private static readonly List<ThingDef> exclusions = new List<ThingDef>();
        internal static string HygieneStatus { get; private set; } = "";
        private static bool Active(string id) => LoadedModManager.RunningModsListForReading.Any(m => string.Equals(m.PackageIdPlayerFacing, id, StringComparison.OrdinalIgnoreCase));

        internal static void Initialize()
        {
            if (Active("ryuudra.prisonerjumpsuits.noquality"))
                foreach (string id in Jumpsuits)
                {
                    var def = DefDatabase<ThingDef>.GetNamedSilentFail(id);
                    if (def != null) exclusions.Add(def);
                }
            if (!Active("Dubwise.DubsBadHygiene")) { HygieneStatus = "QEverything.Options.HygieneAbsent"; return; }
            try
            {
                // Match DBH's own public setting and content ownership. Never edit its settings.
                Type modType = AccessTools.TypeByName("DubsBadHygiene.DubsBadHygieneMod");
                FieldInfo settingsField = modType == null ? null : AccessTools.Field(modType, "Settings");
                object settings = settingsField?.GetValue(null);
                FieldInfo qualityField = settings == null ? null : AccessTools.Field(settings.GetType(), "QualityComps");
                if (qualityField?.FieldType != typeof(bool)) { HygieneStatus = "QEverything.Options.HygieneUnknown"; return; }
                bool enabled = (bool)qualityField.GetValue(settings);
                HygieneStatus = enabled ? "QEverything.Options.HygieneEnabled" : "QEverything.Options.HygieneDisabled";
                if (!enabled)
                    exclusions.AddRange(DefDatabase<ThingDef>.AllDefsListForReading.Where(d => string.Equals(d.modContentPack?.PackageIdPlayerFacing, "Dubwise.DubsBadHygiene", StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception error)
            {
                HygieneStatus = "QEverything.Options.HygieneUnknown";
                Log.Warning("Configurable Quality: could not inspect Dubs Bad Hygiene QualityComps: " + error.GetType().Name);
            }
        }

        internal static IEnumerable<ThingDef> Exclusions => exclusions.Distinct().OrderBy(d => d.defName, StringComparer.Ordinal);
        internal static void PrepareStartup()
        {
            var configuration = QualitySettingsState.Active;
            if (!configuration.ProgressionCompatibility) return;
            // Imported presets can be installed before provider defs/settings are available.
            // Resolve exclusions now, retaining any explicit user exceptions already saved.
            ApplyExclusions(configuration, false);
            QualitySettingsState.Load(configuration);
        }
        internal static void Apply(QualityConfiguration draft)
        {
            draft.ProgressionCompatibility = true;
            foreach (EffectId id in Enum.GetValues(typeof(EffectId)))
                draft.Effects[id.ToString()] = new EffectSettings { Enabled = QualityEffects.Available(id), Factors = QualityEffectCatalog.Curve(id) };
            ApplyExclusions(draft, true);
        }
        private static void ApplyExclusions(QualityConfiguration draft, bool replace)
        {
            foreach (ThingDef def in Exclusions)
            {
                if (replace || !draft.Assignment.Items.ContainsKey(def.defName)) draft.Assignment.Items[def.defName] = RuleChoice.Exclude;
                if (replace || !draft.Participation.Items.ContainsKey(def.defName)) draft.Participation.Items[def.defName] = RuleChoice.Exclude;
            }
        }
    }

    internal static class QualityNativeOutput
    {
        [ThreadStatic] private static int depth;
        internal static bool PreserveBounds => depth > 0;
        internal static readonly List<string> Status = new List<string>();
        internal static void Register()
        {
            var harmony = new Harmony("kylemhb.configurablequality.native-output");
            Register(harmony, "PipeSystem.Process", "ApplyIngredientsAndQuality", new[] { typeof(Thing) });
            Type activeProcess = AccessTools.TypeByName("ProcessorFramework.ActiveProcess");
            Register(harmony, "ProcessorFramework.CompProcessor", "TakeOutProduct", activeProcess == null ? null : new[] { activeProcess });
        }
        private static void Register(Harmony harmony, string typeName, string methodName, Type[] arguments)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null) { Status.Add(typeName + ": " + "QEverything.Options.NativeOutputAbsent".Translate()); return; }
            MethodInfo method = arguments == null ? null : AccessTools.DeclaredMethod(type, methodName, arguments);
            if (method == null || method.IsStatic || method.ReturnType != (methodName == "TakeOutProduct" ? typeof(Thing) : typeof(void)))
            {
                Status.Add(typeName + ": " + "QEverything.Options.NativeOutputUnknown".Translate());
                Log.Warning("Configurable Quality: native output bounds exemption unavailable for " + typeName + "." + methodName + "; check the installed provider version.");
                return;
            }
            try
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(QualityNativeOutput), nameof(Enter)), finalizer: new HarmonyMethod(typeof(QualityNativeOutput), nameof(Exit)));
                Status.Add(typeName + ": " + "QEverything.Options.NativeOutputSupported".Translate());
            }
            catch (Exception error)
            {
                Status.Add(typeName + ": " + "QEverything.Options.NativeOutputUnknown".Translate());
                Log.Warning("Configurable Quality: native output bounds exemption failed for " + typeName + ": " + error.GetType().Name);
            }
        }
        private sealed class Scope { internal int Previous; }
        private static void Enter(out Scope __state) { __state = new Scope { Previous = depth }; depth++; }
        private static Exception Exit(Exception __exception, Scope __state)
        {
            if (__state != null) depth = __state.Previous;
            return __exception;
        }
        internal static void SetNativeQuality(CompQuality comp, QualityCategory quality, ArtGenerationContext context)
        {
            int previous = depth;
            try { depth++; comp.SetQuality(quality, context); }
            finally { depth = previous; }
        }
    }
}
