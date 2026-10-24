using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QualityEverything
{
    internal enum EditorRule { Assignment, Participation, Price, Removal }
    internal enum EditorCloseAction { Save, Discard, KeepEditing }
    internal enum CurveBase { Custom, Mild, Medium }
    internal struct CurveSelection
    {
        public CurveBase Base;
        public float Strength;
        public CurveSelection(CurveBase basis, float strength) { Base = basis; Strength = strength; }
    }
    // One edit boundary for native controls and standalone behavioral checks.
    internal sealed class QualityEditorActions
    {
        private readonly QualityConfigurationSession session;
        private readonly List<QualityConfiguration> history = new List<QualityConfiguration>();
        private QualityConfiguration before;
        private string beforeIdentity, gesture;
        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        private readonly HashSet<EffectId> manuallyEditedCurves = new HashSet<EffectId>();
        public static float[] GeneratedCurve(EffectId id, CurveBase basis, float strength)
        {
            if (basis == CurveBase.Custom || float.IsNaN(strength) || strength < .25f || strength > 2f) throw new ArgumentOutOfRangeException();
            var factors = QualityEffectCatalog.Curve(id, basis == CurveBase.Medium);
            if (strength == 1f) return factors;
            return factors.Select((v, tier) => tier == 2 ? 1f : (float)Math.Exp(strength * Math.Log(v))).ToArray();
        }
        public CurveSelection ClassifyEffectCurve(EffectId id)
        {
            var factors = session.Draft.Effects[id.ToString()].Factors;
            if (manuallyEditedCurves.Contains(id) || factors[2] != 1f) return new CurveSelection(CurveBase.Custom, 1f);
            foreach (var basis in new[] { CurveBase.Mild, CurveBase.Medium })
            {
                var original = QualityEffectCatalog.Curve(id, basis == CurveBase.Medium);
                float strength = (float)(Math.Log(factors[6]) / Math.Log(original[6]));
                if (float.IsNaN(strength) || strength < .25f - .000001f || strength > 2.000001f) continue;
                strength = Math.Max(.25f, Math.Min(2f, strength));
                var generated = GeneratedCurve(id, basis, strength);
                if (factors.Select((value, tier) => Math.Abs(value - generated[tier]) <= .000001f * Math.Max(1f, Math.Abs(value))).All(v => v)) return new CurveSelection(basis, strength);
            }
            return new CurveSelection(CurveBase.Custom, 1f);
        }
        public bool ApplyEffectCurve(EffectId id, CurveBase basis, float strength, bool replaceCustom = false)
        {
            var factors = GeneratedCurve(id, basis, strength);
            if (!replaceCustom && ClassifyEffectCurve(id).Base == CurveBase.Custom) return false;
            bool changed = Edit(c => c.Effects[id.ToString()].Factors = factors);
            manuallyEditedCurves.Remove(id);
            for (int tier = 0; tier < 7; tier++) SynchronizeInput("effect:" + id + ":" + tier);
            return changed;
        }
        public bool EditEffectFactor(EffectId id, int tier, string text)
        {
            if (tier < 0 || tier > 6) throw new ArgumentOutOfRangeException(nameof(tier));
            var effect = session.Draft.Effects[id.ToString()];
            return EditNumber("effect:" + id + ":" + tier, text, effect.Factors[tier], v => { effect.Factors[tier] = v; manuallyEditedCurves.Add(id); });
        }
        public bool ApplyAllEffectCurves(CurveBase basis, bool replaceCustom = false)
        {
            var ids = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().ToArray();
            if (!replaceCustom && ids.Any(id => ClassifyEffectCurve(id).Base == CurveBase.Custom)) return false;
            var factors = ids.ToDictionary(id => id, id => GeneratedCurve(id, basis, 1));
            EndGesture();
            bool changed = Edit(c => { foreach (var id in ids) c.Effects[id.ToString()].Factors = factors[id]; });
            foreach (var id in ids) { manuallyEditedCurves.Remove(id); for (int tier = 0; tier < 7; tier++) SynchronizeInput("effect:" + id + ":" + tier); }
            return changed;
        }
        public bool DisableAllEffects()
        {
            EndGesture();
            return Edit(c => { foreach (EffectId id in Enum.GetValues(typeof(EffectId))) c.Effects[id.ToString()].Enabled = false; });
        }
        public bool HasInvalidInput => buffers.Any(p => !QualityNumericRange.ForInput(p.Key).TryParse(p.Value, out _));
        public bool NeedsCloseConfirmation => HasInvalidInput || session.Draft.Serialize() != session.Saved.Serialize();
        public static bool HasQuality(QualityConfiguration configuration, RuleTarget target, bool nativeQuality, bool removableVanillaQuality)
        {
            if (nativeQuality) return !removableVanillaQuality || !QualityRules.Resolve(target, configuration.VanillaRemoval).Enabled;
            return QualityRules.Resolve(target, configuration.Assignment).Enabled;
        }
        public static RuleSet RulesFor(QualityConfiguration configuration, EditorRule rule) => rule == EditorRule.Assignment ? configuration.Assignment : rule == EditorRule.Participation ? configuration.Participation : rule == EditorRule.Price ? configuration.MarketValue : configuration.VanillaRemoval;
        public static RuleTarget TargetFor(EditorRule rule, RuleTarget target, bool nativeQuality, bool removable)
        {
            bool supported = rule == EditorRule.Removal ? removable : rule == EditorRule.Participation ? target.Supported || nativeQuality : rule == EditorRule.Price ? target.Supported && (!nativeQuality || removable) : target.Supported && !nativeQuality;
            return new RuleTarget(target.Id, target.Group, supported, target.Categories);
        }
        public bool SetItemRule(EditorRule rule, RuleTarget target, bool nativeQuality, bool removable, RuleChoice choice)
        {
            if (choice != RuleChoice.Inherit && !TargetFor(rule, target, nativeQuality, removable).Supported) return false;
            EndGesture();
            return Edit(c => { var items = RulesFor(c, rule).Items; if (choice == RuleChoice.Inherit) items.Remove(target.Id); else items[target.Id] = choice; });
        }
        public bool TryClose(EditorCloseAction action, out List<string> errors, Action persist = null)
        {
            errors = new List<string>();
            if (action == EditorCloseAction.KeepEditing) return false;
            if (action == EditorCloseAction.Discard) { Discard(); return true; }
            errors = Save(persist);
            return errors.Count == 0;
        }
        public string NumberText(string key, float value) => buffers.TryGetValue(key, out var text) ? text : value.ToString("R", CultureInfo.InvariantCulture);
        public bool EditNumber(string key, string text, float value, Action<float> set)
        {
            buffers[key] = text;
            if (!QualityNumericRange.ForInput(key).TryParse(text, out float number) || number == value) return false;
            return Edit(c => set(number));
        }
        public void SynchronizeInputs() { buffers.Clear(); manuallyEditedCurves.Clear(); }
        public void SynchronizeInput(string key) => buffers.Remove(key);
        public List<string> Save(Action persist = null)
        {
            EndGesture();
            if (HasInvalidInput) return new List<string> { "InvalidInput" };
            var errors = session.Save(persist);
            if (errors.Count == 0) history.Clear();
            return errors;
        }
        public void Discard()
        {
            before = null; gesture = null;
            session.Discard();
            history.Clear(); SynchronizeInputs(); Revision++;
        }
        public void Reset()
        {
            EndGesture();
            var defaults = new QualityConfiguration();
            defaults.LegacyBackup = session.Draft.LegacyBackup;
            defaults.InactiveLegacy = new Dictionary<string, bool>(session.Draft.InactiveLegacy, StringComparer.Ordinal);
            Edit(c => session.ReplaceDraft(defaults));
            SynchronizeInputs();
        }
        public bool CanUndo => history.Count > 0;
        public int Revision { get; private set; }
        public QualityEditorActions(QualityConfigurationSession session) { this.session = session; }
        public void BeginEdit()
        {
            if (before != null) return;
            before = session.Draft.Clone();
            beforeIdentity = before.Serialize();
        }
        public bool CompleteEdit()
        {
            if (before == null || gesture != null) return false;
            bool changed = beforeIdentity != session.Draft.Serialize();
            if (changed)
            {
                if (history.Count == 30) history.RemoveAt(0);
                history.Add(before);
                Revision++;
            }
            before = null;
            beforeIdentity = null;
            return changed;
        }
        public bool Edit(Action<QualityConfiguration> edit)
        {
            BeginEdit();
            edit(session.Draft);
            return CompleteEdit();
        }
        public void BeginGesture(string key)
        {
            if (gesture == key) return;
            EndGesture();
            BeginEdit();
            gesture = key;
        }
        public bool EndGesture()
        {
            gesture = null;
            return CompleteEdit();
        }
        public bool Undo()
        {
            EndGesture();
            if (!CanUndo) return false;
            int last = history.Count - 1;
            session.ReplaceDraft(history[last]);
            history.RemoveAt(last);
            SynchronizeInputs();
            Revision++;
            return true;
        }
    }
}
