using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
{
    class Mod_ConfigurableQuality : Mod
    {
        public static ModSettings_ConfigurableQuality settings;
        private int pageRevision = -1, pageCatalogRevision = -1;
        private string currentPreset;
        private readonly Dictionary<string, int[]> groupCounts = new Dictionary<string, int[]>(StringComparer.Ordinal);
        private Vector2 pageScroll;
        private static readonly string[] presets = { "Off", "Mild", "Medium", "Progression" };
        private static readonly string[] families = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Select(Window_QualityOptions.Family).Distinct().ToArray();
        private static readonly Color Muted = new Color(.78f, .78f, .78f);
        public Mod_ConfigurableQuality(ModContentPack content) : base(content)
        {
            // Match Verse.LoadedModManager's verified filename convention before any migration/write.
            string path = Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename("Mod_" + content.FolderName + "_" + GetType().Name + ".xml"));
            try
            {
                if (!File.Exists(path))
                {
                    var oldFolders = new[] { content.FolderName, "EverythingHasQualityReforked", "3710884766" };
                    string previous = oldFolders.Select(folder => Path.Combine(GenFilePaths.ConfigFolderPath,
                        GenText.SanitizeFilename("Mod_" + folder + "_Mod_QEverything.xml"))).FirstOrDefault(File.Exists);
                    if (previous != null)
                    {
                        var migrated = XDocument.Load(previous);
                        foreach (var attribute in migrated.Descendants().Attributes("Class")
                            .Where(a => a.Value == "QualityEverything.ModSettings_QEverything"))
                            attribute.Value = typeof(ModSettings_ConfigurableQuality).FullName;
                        migrated.Save(path);
                        Log.Message("Configurable Quality: copied existing settings to the renamed configuration file.");
                    }
                }
                if (File.Exists(path) && !File.Exists(path + ".legacy-v0.bak"))
                {
                    var root = XDocument.Load(path);
                    if (!root.Descendants("qualityConfiguration").Any()) File.Copy(path, path + ".legacy-v0.bak", false);
                }
            }
            catch (Exception error)
            {
                Log.Warning("Configurable Quality: legacy settings backup failed: " + error.Message);
            }
            settings = GetSettings<ModSettings_ConfigurableQuality>();
        }
        public override string SettingsCategory() => "Configurable Quality";
        private static string T(string key) => ("QEverything.Simple." + key).Translate().ToString();
        // The native dialog writes settings in PreClose and can only serialize Saved. Changes made on
        // this page follow the game's convention: closing the dialog saves them. The editor window
        // keeps its own explicit Save and Cancel, so its unsaved work is never committed from here.
        public override void WriteSettings()
        {
            if (settings == null) return;
            if (QualitySettingsState.Dirty && !Find.WindowStack.IsOpen<Window_QualityOptions>())
            {
                var errors = QualitySettingsState.Save();
                if (errors.Count == 0) return;
                Log.Warning("Configurable Quality: settings were not saved: " + string.Join(", ", errors));
                Messages.Message(T("SaveFailed"), MessageTypeDefOf.RejectInput, false);
            }
            settings.Write();
        }
        private void RefreshPage()
        {
            if (pageRevision == QualitySettingsState.Revision && pageCatalogRevision == QualityCatalog.Revision) return;
            pageRevision = QualitySettingsState.Revision; pageCatalogRevision = QualityCatalog.Revision;
            var draft = QualitySettingsState.Draft; var editor = QualitySettingsState.Editor;
            groupCounts.Clear();
            foreach (var entry in QualityCatalog.Entries)
            {
                if (!groupCounts.TryGetValue(entry.Target.Group, out var pair)) groupCounts[entry.Target.Group] = pair = new int[2];
                pair[1]++;
                if (QualityEditorActions.HasQuality(draft, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality)) pair[0]++;
            }
            // A preset is "current" when applying it again would change nothing.
            var defaults = new QualityConfiguration { LegacyBackup = draft.LegacyBackup, InactiveLegacy = new Dictionary<string, bool>(draft.InactiveLegacy, StringComparer.Ordinal) };
            currentPreset = editor.MatchesPreset(defaults) ? "Off"
                : editor.MatchesPreset(editor.BuiltinPreset(false, QualityProgression.Exclusions.Select(d => d.defName), QualityEffects.Available)) ? "Progression"
                : editor.MatchesPreset(editor.BuiltinPreset(false)) ? "Mild"
                : editor.MatchesPreset(editor.BuiltinPreset(true)) ? "Medium" : null;
        }
        private static void Edit(Action<QualityConfiguration> change)
        {
            QualitySettingsState.Editor.EndGesture();
            QualitySettingsState.Editor.Edit(change);
            QualitySettingsState.Changed();
        }
        private void ChoosePreset(string name)
        {
            if (name == currentPreset) return;
            if (name == "Off")
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(T("OffConfirm"), () => { QualitySettingsState.Editor.Reset(); QualitySettingsState.Changed(); }));
            else Window_QualityOptions.OpenBuiltinPreset(name == "Progression", name == "Medium", null);
        }
        public override void DoSettingsWindowContents(Rect rect)
        {
            QualityCatalog.Build(); RefreshPage();
            var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color; var enabled = GUI.enabled;
            const float row = 27;
            var groups = QualitySettingsState.GroupFields.Keys.ToArray();
            float needed = 170 + Math.Max(groups.Length * row, (families.Length + 1) * row + 134) + 40;
            bool scrolling = needed > rect.height;
            float width = scrolling ? rect.width - 18 : rect.width;
            if (scrolling) Widgets.BeginScrollView(rect, ref pageScroll, new Rect(0, 0, width, needed)); else Widgets.BeginGroup(rect);
            try
            {
                Text.Font = GameFont.Small;
                var draft = QualitySettingsState.Draft;
                float y = 0;
                Widgets.Label(new Rect(0, y, width, 24), T("PresetHeading")); y += 28;
                float presetWidth = (width - 18) / presets.Length;
                for (int i = 0; i < presets.Length; i++)
                {
                    var button = new Rect(i * (presetWidth + 6), y, presetWidth, 34);
                    if (Window_QualityOptions.TabButton(button, T("Preset." + presets[i]), currentPreset == presets[i])) ChoosePreset(presets[i]);
                    TooltipHandler.TipRegion(button, T("Preset." + presets[i] + "Help"));
                }
                y += 40;
                GUI.color = Muted;
                Widgets.Label(new Rect(0, y, width, 44), T(currentPreset == null ? "PresetCustom" : "Preset." + currentPreset + "Help")); GUI.color = color;
                y += 50;
                float column = (width - 28) / 2, top = y;
                // Left: which kinds of item get quality.
                Widgets.Label(new Rect(0, y, column, 24), T("GroupsHeading")); y += 24;
                GUI.color = Muted; Widgets.Label(new Rect(0, y, column, 24), T("GroupsHint")); GUI.color = color; y += 28;
                foreach (string id in groups)
                {
                    // A group with nothing in it (no loaded mod adds such items) would only be a dead switch.
                    if (!groupCounts.TryGetValue(id, out var total) || total[1] == 0) continue;
                    bool on = draft.Assignment.Groups.TryGetValue(id, out bool value) ? value : draft.Assignment.Default, before = on;
                    var line = new Rect(0, y, column, row - 2);
                    Widgets.DrawHighlightIfMouseover(line);
                    Widgets.CheckboxLabeled(new Rect(0, y, column - 120, row - 2), Window_QualityOptions.GroupLabel(id), ref on);
                    groupCounts.TryGetValue(id, out var pair);
                    GUI.color = Muted; Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(new Rect(column - 116, y, 112, row - 2), string.Format(T("Count"), pair?[0] ?? 0, pair?[1] ?? 0));
                    GUI.color = color; Text.Anchor = anchor;
                    TooltipHandler.TipRegion(line, T("GroupTip"));
                    if (on != before) { string group = id; bool next = on; Edit(c => c.Assignment.Groups[group] = next); }
                    y += row;
                }
                // Right: what quality does, then the way into the full editor.
                float x = column + 28; y = top;
                Widgets.Label(new Rect(x, y, column, 24), T("EffectsHeading")); y += 24;
                GUI.color = Muted; Widgets.Label(new Rect(x, y, column, 24), T("EffectsHint")); GUI.color = color; y += 28;
                foreach (string family in families)
                {
                    var ids = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Where(id => Window_QualityOptions.Family(id) == family).ToArray();
                    int active = ids.Count(id => draft.Effects[id.ToString()].Enabled), unavailable = ids.Count(id => !QualityEffects.Available(id));
                    bool on = active == ids.Length, before = on;
                    var line = new Rect(x, y, column, row - 2);
                    Widgets.DrawHighlightIfMouseover(line);
                    Widgets.CheckboxLabeled(new Rect(x, y, column - 120, row - 2), ("QEverything.Advanced." + family).Translate(), ref on);
                    GUI.color = active > 0 && active < ids.Length ? new Color(1f, .75f, .45f) : Muted; Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(new Rect(x + column - 116, y, 112, row - 2), string.Format(T("EffectCount"), active, ids.Length));
                    GUI.color = color; Text.Anchor = anchor;
                    TooltipHandler.TipRegion(line, string.Join("\n", ids.Select(id => ("QEverything.Options.Effect." + id).Translate().ToString()))
                        + (unavailable > 0 ? "\n\n" + string.Format(T("EffectUnavailable"), unavailable) : ""));
                    if (on != before) { bool next = on; Edit(c => { foreach (var id in ids) c.Effects[id.ToString()].Enabled = next; }); }
                    y += row;
                }
                bool price = draft.MarketValue.Default, oldPrice = price;
                var priceLine = new Rect(x, y, column, row - 2);
                Widgets.DrawHighlightIfMouseover(priceLine);
                Widgets.CheckboxLabeled(new Rect(x, y, column - 120, row - 2), T("MarketValue"), ref price);
                TooltipHandler.TipRegion(priceLine, T("MarketValueTip"));
                if (price != oldPrice) Edit(c => c.MarketValue.Default = price);
                y += row + 16;
                if (Widgets.ButtonText(new Rect(x, y, column, 36), T("OpenEditor")) && !Find.WindowStack.IsOpen<Window_QualityOptions>())
                    Find.WindowStack.Add(new Window_QualityOptions());
                y += 40;
                GUI.color = Muted; Widgets.Label(new Rect(x, y, column, 44), T("EditorHint")); GUI.color = color;
                y += 48;
                if (QualitySettingsState.Dirty && Widgets.ButtonText(new Rect(x, y, Math.Min(column, 220), 30), T("Discard"))) QualitySettingsState.Discard();
                // One line says where the changes stand.
                float bottom = scrolling ? needed - 30 : rect.height - 30;
                string status = T(QualitySettingsState.Dirty ? "StatusDirty" : QualitySettingsState.RestartPending ? "StatusRestart" : "StatusClean");
                GUI.color = QualitySettingsState.Dirty || QualitySettingsState.RestartPending ? new Color(1f, .75f, .45f) : Muted;
                Text.Anchor = TextAnchor.MiddleLeft; Widgets.Label(new Rect(0, bottom, width, 30), status);
            }
            finally
            {
                Text.Font = font; Text.Anchor = anchor; GUI.color = color; GUI.enabled = enabled;
                if (scrolling) Widgets.EndScrollView(); else Widgets.EndGroup();
            }
        }
    }
}
