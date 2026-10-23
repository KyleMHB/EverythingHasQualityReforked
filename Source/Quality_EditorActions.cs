using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QualityEverything
{
    // One edit boundary for native controls and standalone behavioral checks.
    internal sealed class QualityEditorActions
    {
        private readonly QualityConfigurationSession session;
        private readonly List<QualityConfiguration> history = new List<QualityConfiguration>();
        private QualityConfiguration before;
        private string beforeIdentity, gesture;
        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        public bool HasInvalidInput => buffers.Any(p => !QualityNumericRange.ForInput(p.Key).TryParse(p.Value, out _));
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
