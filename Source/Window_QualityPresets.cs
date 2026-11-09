using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
namespace ConfigurableQuality
{
    internal sealed partial class Window_QualityOptions
    {
        private string matchedPresetName;
        private string presetError;
        private QualityConfiguration matchedPreset;
        private QualityEditorActions comparedPresetEditor;
        private int comparedPresetRevision = -1, comparedPresetEditorRevision = -1;
        private bool comparedPresetInvalidInput, presetMatches;
        internal static string P(string key) => ("QEverything.Presets." + key).Translate().ToString();
        private bool PresetMatchesDraft()
        {
            var editor = Editor;
            bool invalidInput = editor.HasInvalidInput;
            if (comparedPresetEditor != editor || comparedPresetRevision != QualitySettingsState.Revision
                || comparedPresetEditorRevision != editor.Revision || comparedPresetInvalidInput != invalidInput)
            {
                comparedPresetEditor = editor;
                comparedPresetRevision = QualitySettingsState.Revision;
                comparedPresetEditorRevision = editor.Revision;
                comparedPresetInvalidInput = invalidInput;
                presetMatches = matchedPreset != null && !invalidInput && editor.MatchesPreset(matchedPreset);
            }
            return presetMatches;
        }
        private void DrawPresetHeader(Rect rect)
        {
            string name = PresetMatchesDraft() ? matchedPresetName : P("Modified");
            string label = P("Manage") + ": " + name;
            if (Widgets.ButtonText(rect, label.Truncate(rect.width - 16)))
            {
                Editor.EndGesture();
                Find.WindowStack.Add(new Window_QualityPresets(this));
            }
            TooltipHandler.TipRegion(rect, P("Reusable") + "\n" + name);
        }
        internal void OpenBuiltinPreset(bool progression, bool medium = false) => OpenPreset(progression, medium);
        internal void RememberPreset(string name, QualityConfiguration snapshot)
        {
            matchedPresetName = name; matchedPreset = snapshot.Clone();
            comparedPresetEditor = null;
        }
        internal bool CanSaveUserPreset => Editor.CanSavePreset;
        internal QualityConfiguration CapturePreset() => Draft.Clone();
        internal void ShowPresetError(Exception exception) => error = presetError = P("Failure") + " " + exception.Message;
        internal void ClearPresetError()
        {
            if (error == presetError) error = "";
            presetError = null;
        }
        internal void PreviewUserPreset(string name, QualityConfiguration candidate)
        {
            var captured = candidate.Clone();
            string changes = PresetChanges(Draft, captured);
            Find.WindowStack.Add(new Window_QualitySnapshotReview(name, changes, () =>
            {
                Editor.ApplyPreset(captured); QualitySettingsState.Changed();
                RememberPreset(name, captured);
                GUI.FocusControl(null); DraftChanged();
            }));
        }
        private static string PresetChanges(QualityConfiguration before, QualityConfiguration after)
        {
            var left = PresetValues(XElement.Parse(before.Serialize()), "").ToDictionary(p => p.Key, p => p.Value);
            var right = PresetValues(XElement.Parse(after.Serialize()), "").ToDictionary(p => p.Key, p => p.Value);
            var lines = left.Keys.Union(right.Keys).OrderBy(k => k, StringComparer.Ordinal).Where(k => !left.ContainsKey(k) || !right.ContainsKey(k) || left[k] != right[k])
                .Select(k => PresetPathLabel(k) + ": " + (left.TryGetValue(k, out var a) ? PresetValueLabel(k, a) : P("Absent")) + " → " + (right.TryGetValue(k, out var b) ? PresetValueLabel(k, b) : P("Absent")));
            string result = string.Join("\n", lines);
            return result.Length == 0 ? P("NoChanges") : result;
        }
        private static string PresetPathLabel(string path)
        {
            var parts = path.Split(new[] { " / " }, StringSplitOptions.None).Skip(1).ToArray();
            return string.Join(" / ", parts.Select((part, index) =>
            {
                if (index == 2 && parts[1] == "groups")
                    return Translator.CanTranslate("QEverything.Options.Group." + part) ? GroupLabel(part) : part;
                if (index == 2 && parts[1] == "categories")
                    return DefDatabase<ThingCategoryDef>.GetNamedSilentFail(part)?.LabelCap.ToString() ?? part;
                if ((index == 2 && parts[1] == "items") || (index == 1 && parts[0] == "inactive"))
                    return DefDatabase<ThingDef>.GetNamedSilentFail(part)?.LabelCap.ToString() ?? part;
                if (index == 1 && parts[0] == "production") return A("Production." + part);
                if (index == 1 && parts[0] == "effects" && Enum.TryParse(part, out EffectId known) && Enum.IsDefined(typeof(EffectId), known)) return T("Effect." + known);
                switch (part)
                {
                    case "assignment": return T("Assignment");
                    case "participation": return T("Participation");
                    case "linkedBonusCap": return T("LinkedBonusCap");
                    case "linkedPenaltyCap": return T("LinkedPenaltyCap");
                    case "inactive": return T("InactiveLegacy");
                    default: return Translator.CanTranslate("QEverything.Presets.Field." + part) ? P("Field." + part) : part;
                }
            }));
        }
        private static string PresetValueLabel(string path, string value)
        {
            if (bool.TryParse(value, out bool boolean)) return P(boolean ? "Yes" : "No");
            if (path.Contains(" / effects / ") && !path.EndsWith(" / enabled", StringComparison.Ordinal))
            {
                var factors = value.Split(',');
                if (factors.Length == 7) return string.Join("; ", factors.Select((v, tier) => ((RimWorld.QualityCategory)tier).GetLabel() + " " + v + "x"));
            }
            if (Enum.TryParse(value, out RuleChoice choice) && Enum.IsDefined(typeof(RuleChoice), choice)) return ChoiceLabel(choice);
            return value;
        }
        private static IEnumerable<KeyValuePair<string, string>> PresetValues(XElement node, string path)
        {
            string id = (string)node.Attribute("id");
            string key = path.Length == 0 ? "" : path + " / ";
            key += id ?? node.Name.LocalName;
            foreach (var attribute in node.Attributes().Where(a => a.Name != "id" && a.Name != "schema")) yield return new KeyValuePair<string, string>(key + " / " + attribute.Name, attribute.Value);
            // Empty collections are structure, while empty scalar values (including legacy backups) are data.
            bool container = id == null && (node.Name == "quality" || node.Name == "production" || node.Name == "effects"
                || node.Name == "assignment" || node.Name == "participation" || node.Name == "marketValue" || node.Name == "vanillaRemoval"
                || node.Name == "groups" || node.Name == "categories" || node.Name == "items" || node.Name == "inactive");
            if (!node.HasElements && !container) yield return new KeyValuePair<string, string>(key, node.Value);
            else foreach (var child in node.Elements()) foreach (var value in PresetValues(child, key)) yield return value;
        }
    }
    internal sealed class Window_QualityPresets : Window
    {
        private readonly Window_QualityOptions owner;
        private readonly QualityPresetStore store = new QualityPresetStore(Path.Combine(GenFilePaths.ConfigFolderPath, "ConfigurableQualityPresets"), GenText.SanitizeFilename);
        private string[] names = new string[0];
        private string name = "", feedback = "";
        private Vector2 scroll;
        public override Vector2 InitialSize => new Vector2(700, 600);
        private static string P(string key) => Window_QualityOptions.P(key);
        public Window_QualityPresets(Window_QualityOptions owner)
        { this.owner = owner; forcePause = true; absorbInputAroundWindow = true; doCloseX = true; Refresh(); }
        private void Attempt(Action action)
        {
            try { feedback = ""; owner.ClearPresetError(); action(); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException || e is System.Xml.XmlException || e is System.Security.SecurityException)
            { feedback = P("Failure") + " " + e.Message; owner.ShowPresetError(e); }
        }
        private void Refresh() => Attempt(() => names = store.List());
        public override void DoWindowContents(Rect rect)
        {
            Widgets.Label(new Rect(0, 0, rect.width, 30), P("Title"));
            Widgets.Label(new Rect(0, 34, rect.width, 58), P("Reusable"));
            name = Widgets.TextField(new Rect(0, 98, rect.width - 188, 32), name);
            bool enabled = GUI.enabled; GUI.enabled = enabled && owner.CanSaveUserPreset;
            if (Widgets.ButtonText(new Rect(rect.width - 180, 98, 180, 32), P("Save")))
                Attempt(() => { var captured = owner.CapturePreset(); store.Save(name, captured); owner.RememberPreset(name, captured); Refresh(); feedback = P("Saved"); });
            GUI.enabled = enabled;
            TooltipHandler.TipRegion(new Rect(0, 98, rect.width, 32), P("SaveHint"));
            float builtInWidth = (rect.width - 16) / 3;
            if (Widgets.ButtonText(new Rect(0, 140, builtInWidth, 30), "QEverything.Advanced.Mild".Translate())) owner.OpenBuiltinPreset(false);
            if (Widgets.ButtonText(new Rect(builtInWidth + 8, 140, builtInWidth, 30), "QEverything.Advanced.Medium".Translate())) owner.OpenBuiltinPreset(false, true);
            var progressionRect = new Rect((builtInWidth + 8) * 2, 140, builtInWidth, 30);
            if (Widgets.ButtonText(progressionRect, P("Progression"))) owner.OpenBuiltinPreset(true);
            TooltipHandler.TipRegion(progressionRect, "QEverything.Advanced.Progression".Translate());
            float width = rect.width - 18;
            Widgets.BeginScrollView(new Rect(0, 180, rect.width, rect.height - 272), ref scroll, new Rect(0, 0, width, Math.Max(rect.height - 272, names.Length * 78)));
            for (int i = 0; i < names.Length; i++)
            {
                string selected = names[i]; float y = i * 78;
                Widgets.Label(new Rect(0, y, width, 30), selected);
                float button = (width - 16) / 3;
                if (Widgets.ButtonText(new Rect(0, y + 32, button, 30), P("Load"))) Attempt(() => owner.PreviewUserPreset(selected, store.Load(selected)));
                GUI.enabled = enabled && owner.CanSaveUserPreset;
                if (Widgets.ButtonText(new Rect(button + 8, y + 32, button, 30), P("Overwrite"))) Attempt(() =>
                {
                    string expected = store.Read(selected); var captured = owner.CapturePreset();
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(P("OverwriteConfirm") + "\n" + selected, () => Attempt(() => { store.Save(selected, captured, expected); owner.RememberPreset(selected, captured); Refresh(); feedback = P("Saved"); })));
                });
                GUI.enabled = enabled;
                if (Widgets.ButtonText(new Rect((button + 8) * 2, y + 32, button, 30), P("Delete"))) Attempt(() =>
                {
                    string expected = store.Read(selected);
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(P("DeleteConfirm") + "\n" + selected, () => Attempt(() => { store.Delete(selected, expected); Refresh(); })));
                });
            }
            Widgets.EndScrollView();
            Widgets.Label(new Rect(0, rect.height - 84, rect.width, 42), feedback);
            if (Widgets.ButtonText(new Rect(0, rect.height - 36, rect.width, 32), P("Refresh"))) Refresh();
        }
    }
    internal sealed class Window_QualitySnapshotReview : Window
    {
        private readonly string title, changes;
        private readonly Action apply;
        private Vector2 scroll;
        public override Vector2 InitialSize => new Vector2(850, 680);
        public Window_QualitySnapshotReview(string title, string changes, Action apply)
        { this.title = title; this.changes = changes; this.apply = apply; forcePause = true; absorbInputAroundWindow = true; doCloseX = true; }
        public override void DoWindowContents(Rect rect)
        {
            Widgets.Label(new Rect(0, 0, rect.width, 30), title);
            Widgets.Label(new Rect(0, 34, rect.width, 56), Window_QualityOptions.P("Preview"));
            float height = Text.CalcHeight(changes, rect.width - 24) + 12;
            Widgets.BeginScrollView(new Rect(0, 96, rect.width, rect.height - 150), ref scroll, new Rect(0, 0, rect.width - 18, height));
            Widgets.Label(new Rect(0, 0, rect.width - 24, height), changes);
            Widgets.EndScrollView();
            if (Widgets.ButtonText(new Rect(0, rect.height - 40, rect.width / 2 - 4, 36), "QEverything.Advanced.GoBack".Translate())) Close();
            if (Widgets.ButtonText(new Rect(rect.width / 2 + 4, rect.height - 40, rect.width / 2 - 4, 36), "QEverything.Advanced.ApplyDraft".Translate())) { apply(); Close(); }
        }
    }
}
