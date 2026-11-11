using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
namespace ConfigurableQuality
{
    internal sealed partial class Window_QualityOptions : Window
    {
        private sealed class BrowserState
        {
            public string Search = "", Group = "", Category = "", Source = "", FilterKey;
            public int ResultFilter;
            public Vector2 ListScroll;
            public List<QualityCatalogEntry> Filtered = new List<QualityCatalogEntry>();
        }
        private sealed class CategoryNode { public ThingCategoryDef Def; public string Path; public int Depth; public string[] Ancestors; }
        private readonly BrowserState assignment = new BrowserState();
        private bool closing;
        private int page;
        private QualityEditorActions Editor => QualitySettingsState.Editor;
        private Vector2 productionScroll, effectScroll, compatibilityScroll;
        private string error = "";
        private readonly HashSet<string> expanded = new HashSet<string>();
        private readonly List<CategoryNode> categoryNodes;
        private readonly string[] groupIds = QualitySettingsState.GroupFields.Keys.ToArray();
        private QualityConfiguration Draft => QualitySettingsState.Draft;
        public override Vector2 InitialSize => new Vector2(Math.Min(1480, Verse.UI.screenWidth - 40), Math.Min(900, Verse.UI.screenHeight - 40));
        private static string T(string key) => ("QEverything.Options." + key).Translate().ToString();
        private static string U(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, T("UX." + key), args);
        internal static string GroupLabel(string id) => T("Group." + id);
        private static string ChoiceLabel(RuleChoice choice) => T(choice.ToString());
        // +5% / -20% reads faster than a raw 1.05 / 0.8 multiplier.
        internal static string Percent(float factor) => ((factor - 1f) * 100f).ToString("+0.#;-0.#;0", CultureInfo.InvariantCulture) + "%";
        // The selected tab is a highlighted, centred label; the others are ordinary buttons.
        internal static bool TabButton(Rect rect, string label, bool selected)
        {
            if (!selected) return Widgets.ButtonText(rect, label);
            var anchor = Text.Anchor;
            Widgets.DrawHighlightSelected(rect);
            Text.Anchor = TextAnchor.MiddleCenter; Widgets.Label(rect, label); Text.Anchor = anchor;
            return false;
        }
        internal static void Placeholder(Rect rect, string text, string value)
        {
            if (!string.IsNullOrEmpty(value)) return;
            var color = GUI.color; var anchor = Text.Anchor;
            GUI.color = new Color(1f, 1f, 1f, .35f); Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x + 6, rect.y, rect.width - 6, rect.height), text);
            GUI.color = color; Text.Anchor = anchor;
        }
        public Window_QualityOptions()
        {
            doCloseX = true; closeOnCancel = true; closeOnClickedOutside = false; absorbInputAroundWindow = true; forcePause = true;
            QualityCatalog.Build(); categoryNodes = DefDatabase<ThingCategoryDef>.AllDefsListForReading.Select(BuildNode).OrderBy(n => n.Path, StringComparer.CurrentCultureIgnoreCase).ToList();
            foreach (var node in categoryNodes.Where(n => n.Def.parent == null)) expanded.Add(node.Def.defName);
        }
        private static CategoryNode BuildNode(ThingCategoryDef def)
        {
            var chain = new List<ThingCategoryDef>(); var seen = new HashSet<ThingCategoryDef>();
            for (var current = def; current != null && seen.Add(current); current = current.parent) chain.Add(current);
            chain.Reverse(); return new CategoryNode { Def = def, Depth = chain.Count, Path = string.Join(" / ", chain.Select(c => c.LabelCap.ToString())), Ancestors = chain.Where(c => c != def).Select(c => c.defName).ToArray() };
        }
        public override bool OnCloseRequest()
        {
            if (!closing && Editor.NeedsCloseConfirmation)
            { if (!Find.WindowStack.IsOpen<Window_QualityClose>()) Find.WindowStack.Add(new Window_QualityClose(this)); return false; }
            return true;
        }
        internal void Finish(bool save)
        {
            if (!QualitySettingsState.TryClose(save ? EditorCloseAction.Save : EditorCloseAction.Discard, out var errors))
            {
                string details = string.Join(", ", errors.Where(e => e != "InvalidInput"));
                error = T(errors.Any(e => e.StartsWith("Persistence:", StringComparison.Ordinal)) ? "SaveFailed" : "InvalidInput")
                    + (details.Length == 0 ? "" : " " + details);
                return;
            }
            closing = true; Close();
        }
        private void RememberDraft() { Editor.EndGesture(); Editor.BeginEdit(); }
        private void DraftChanged() { if (Editor.CompleteEdit()) QualitySettingsState.Changed(); assignment.FilterKey = null; error = ""; }
        public override void DoWindowContents(Rect rect)
        {
            var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color; var enabled = GUI.enabled;
            try
            {
                if (Event.current.rawType == EventType.MouseUp && Editor.EndGesture()) QualitySettingsState.Changed();
                Text.Font = GameFont.Medium; Widgets.Label(new Rect(rect.x, rect.y, rect.width - 360, 32), T("EditorTitle"));
                Text.Font = GameFont.Small; DrawPresetHeader(new Rect(rect.xMax - 345, rect.y, 320, 30)); var titles = new[] { W("ItemsTab"), W("EffectsTab"), W("ProductionTab"), T("Compatibility") }; float tabWidth = (rect.width - 6 * (titles.Length - 1)) / titles.Length;
                for (int i = 0; i < titles.Length; i++) { var tab = new Rect(rect.x + i * (tabWidth + 6), rect.y + 36, tabWidth, 32); if (TabButton(tab, titles[i], page == i)) { page = i; GUI.FocusControl(null); } }
                var body = new Rect(rect.x, rect.y + 78, rect.width, Math.Max(100, rect.height - 78 - 46));
                if (page == 0) DrawWorkbench(body);
                else if (page == 1) DrawEffects(body);
                else if (page == 2) DrawProduction(body);
                else DrawCompatibility(body);
                // One status line: an error, or the single thing the player needs to know about saving.
                const float side = 150, save = 200;
                bool problem = error.Length > 0 || Editor.HasInvalidInput;
                string status = error.Length > 0 ? error : Editor.HasInvalidInput ? T("InvalidInput") : QualitySettingsState.Dirty ? T("DraftPending") : QualitySettingsState.RestartPending ? T("RestartPending") : T("ActiveSaved");
                if (problem) GUI.color = new Color(1f, .65f, .6f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(rect.x + 2 * (side + 8), rect.yMax - 36, Math.Max(40, rect.width - 3 * (side + 8) - save - 8), 36), status);
                Text.Anchor = anchor; GUI.color = color;
                bool guiEnabled = GUI.enabled; GUI.enabled = guiEnabled && Editor.CanUndo;
                if (Widgets.ButtonText(new Rect(rect.x, rect.yMax - 34, side, 32), U("Undo")) && Editor.Undo()) { QualitySettingsState.Changed(); GUI.FocusControl(null); DraftChanged(); }
                GUI.enabled = guiEnabled;
                if (Widgets.ButtonText(new Rect(rect.x + side + 8, rect.yMax - 34, side, 32), T("More")))
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> {
                        new FloatMenuOption(T("Discard"), QualitySettingsState.Dirty || Editor.HasInvalidInput ? (Action)(() => Confirm(T("DiscardConfirm"), () => { QualitySettingsState.Discard(); GUI.FocusControl(null); assignment.FilterKey = null; error = ""; })) : null),
                        new FloatMenuOption(T("ResetAll"), () => Confirm(T("ResetConfirm"), () => { Editor.Reset(); QualitySettingsState.Changed(); GUI.FocusControl(null); DraftChanged(); })) }));
                if (Widgets.ButtonText(new Rect(rect.xMax - save - side - 8, rect.yMax - 34, side, 32), T("Cancel")))
                { if (Editor.NeedsCloseConfirmation) Confirm(T("DiscardConfirm"), () => Finish(false)); else Finish(false); }
                if (Widgets.ButtonText(new Rect(rect.xMax - save, rect.yMax - 34, save, 32), T("Save"))) Finish(true);
            }
            finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; GUI.enabled = enabled; }
        }
        private static void Confirm(string text, Action action) => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, action));
        private string ScopeName => assignment.Category.Length > 0 ? (categoryNodes.FirstOrDefault(n => n.Def.defName == assignment.Category)?.Path ?? assignment.Category) : assignment.Group.Length > 0 ? GroupLabel(assignment.Group) : U("AllItems");
        private void FloatField(Rect rect, string key, float value, Action<float> set)
        {
            string text = Editor.NumberText(key, value);
            GUI.SetNextControlName("quality-number-" + key);
            string edited = Widgets.TextField(rect, text);
            bool valid = BufferIsValid(key, edited, out float number);
            if (!valid)
            {
                var old = GUI.color; var font = Text.Font; GUI.color = new Color(1f, .65f, .6f); Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(rect.x, rect.yMax + 1, rect.width, 18), U("FieldRange", RangeHint(key))); GUI.color = old; Text.Font = font;
                TooltipHandler.TipRegion(rect, U("FieldRange", RangeHint(key)) + " " + U("LastValid"));
            }
            if (edited == text) return;
            if (Editor.EditNumber(key, edited, value, set)) { QualitySettingsState.Changed(); DraftChanged(); }
        }
        private static string RangeHint(string key) => QualityNumericRange.ForInput(key).Hint;
        private static bool BufferIsValid(string key, string text, out float number) => QualityNumericRange.ForInput(key).TryParse(text, out number);
    }
    internal sealed class Window_QualityClose : Window
    {
        private readonly Window_QualityOptions owner; public override Vector2 InitialSize => new Vector2(600, 200);
        public Window_QualityClose(Window_QualityOptions owner) { this.owner = owner; forcePause = true; absorbInputAroundWindow = true; doCloseX = true; }
        public override void DoWindowContents(Rect rect)
        {
            Widgets.Label(new Rect(0, 0, rect.width, 70), "QEverything.Options.ClosePrompt".Translate()); float w = (rect.width - 16) / 3;
            if (Widgets.ButtonText(new Rect(0, 85, w, 40), "QEverything.Options.Save".Translate())) { Close(); owner.Finish(true); }
            if (Widgets.ButtonText(new Rect(w + 8, 85, w, 40), "QEverything.Options.Discard".Translate())) { Close(); owner.Finish(false); }
            if (Widgets.ButtonText(new Rect(2 * (w + 8), 85, w, 40), "QEverything.Options.Continue".Translate())) Close();
        }
    }
}
