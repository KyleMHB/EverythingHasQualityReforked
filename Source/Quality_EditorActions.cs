using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QualityEverything
{
    internal enum EditorRule { Assignment, Participation, Price, Removal }
    internal enum EditorCloseAction { Save, Discard, KeepEditing }
    // One edit boundary for native controls and standalone behavioral checks.
    internal sealed class QualityEditorActions
    {
        private readonly QualityConfigurationSession session;
        private readonly List<QualityConfiguration> history = new List<QualityConfiguration>();
        private QualityConfiguration before;
        private string beforeIdentity, gesture;
        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
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
        public void SynchronizeInputs() => buffers.Clear();
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
            history.Clear(); buffers.Clear(); Revision++;
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
