using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
{
    internal sealed partial class Window_QualityOptions
    {
        private Vector2 bulkScroll;
        private int selectedEntriesRevision = -1, selectedCatalogRevision = -1, hiddenSelectionRevision = -1, hiddenSelected;
        private string hiddenSelectionFilter;
        private List<QualityCatalogEntry> selectedEntries = new List<QualityCatalogEntry>();
        private List<EditorItem> selectedItems = new List<EditorItem>();
        private readonly Dictionary<EditorRule, int> eligibleCounts = new Dictionary<EditorRule, int>();
        private static string B(string key, params object[] args) => string.Format(("QEverything.Bulk." + key).Translate().ToString(), args);
        private List<QualityCatalogEntry> SelectedEntries
        {
            get
            {
                if (selectedEntriesRevision == Editor.SelectionRevision && selectedCatalogRevision == QualityCatalog.Revision) return selectedEntries;
                selectedEntriesRevision = Editor.SelectionRevision; selectedCatalogRevision = QualityCatalog.Revision;
                var catalog = QualityCatalog.Entries.ToDictionary(e => e.Target.Id, StringComparer.Ordinal);
                selectedEntries = Editor.SelectedIds.Select(id => catalog.TryGetValue(id, out var entry) ? entry : new QualityCatalogEntry
                { Label = id, Source = T("Unavailable"), Target = new RuleTarget(id, "", false, new CategoryPath[0]) })
                    .OrderBy(e => e.Label, StringComparer.CurrentCultureIgnoreCase).ToList();
                selectedItems = selectedEntries.Select(e => new EditorItem(e.Target, e.NativeQuality, e.RemovableVanillaQuality)).ToList();
                foreach (EditorRule rule in Enum.GetValues(typeof(EditorRule))) eligibleCounts[rule] = Editor.EligibleSelected(rule, selectedItems).Count;
                return selectedEntries;
            }
        }
        private List<EditorItem> BulkItems { get { var entries = SelectedEntries; return selectedItems; } }
        private string SelectionCount
        {
            get
            {
                if (hiddenSelectionRevision != Editor.SelectionRevision || hiddenSelectionFilter != assignment.FilterKey)
                {
                    hiddenSelectionRevision = Editor.SelectionRevision; hiddenSelectionFilter = assignment.FilterKey;
                    hiddenSelected = Editor.HiddenSelectedCount(assignment.Filtered.Select(e => e.Target.Id));
                }
                return B("Count", Editor.SelectedCount, hiddenSelected);
            }
        }
        private float DrawBulkSelectionBar(Rect rect)
        {
            float half = (rect.width - 8) / 2;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, half, 28), B("SelectResults", assignment.Filtered.Count))) Editor.SelectResults(assignment.Filtered.Select(e => e.Target.Id));
            Widgets.Label(new Rect(rect.x + half + 8, rect.y, half, 28), SelectionCount);
            if (!Editor.BulkMode) return 34;
            float width = (rect.width - 16) / 3;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y + 34, width, 28), B("Review"))) Find.WindowStack.Add(new Window_WorkbenchSelection(this));
            if (Widgets.ButtonText(new Rect(rect.x + width + 8, rect.y + 34, width, 28), B("Clear"))) Editor.ClearSelection();
            if (Widgets.ButtonText(new Rect(rect.x + (width + 8) * 2, rect.y + 34, width, 28), B("Edit"))) Find.WindowStack.Add(new Window_WorkbenchBulk(this));
            return 68;
        }
        private void DrawBulkInspector(Rect rect, ref Vector2 scroll)
        {
            if (!Editor.BulkMode) { Widgets.Label(rect, B("Empty")); return; }
            float width = rect.width - 18;
            string help = B("Help");
            float helpHeight = Math.Max(72, Text.CalcHeight(help, width));
            var view = new Rect(0, 0, width, 114 + helpHeight + 4 * 176);
            Widgets.BeginScrollView(rect, ref scroll, view);
            try
            {
                Widgets.Label(new Rect(0, 0, width, 28), B("Title"));
                Widgets.Label(new Rect(0, 30, width, 28), SelectionCount);
                float half = (width - 6) / 2;
                if (Widgets.ButtonText(new Rect(0, 60, half, 32), B("Review"))) Find.WindowStack.Add(new Window_WorkbenchSelection(this));
                if (Widgets.ButtonText(new Rect(half + 6, 60, half, 32), B("Clear"))) Editor.ClearSelection();
                Widgets.Label(new Rect(0, 102, width, helpHeight), help);
                float y = 114 + helpHeight;
                var items = BulkItems;
                foreach (var kind in new[] { EditorRule.Assignment, EditorRule.Participation, EditorRule.Price, EditorRule.Removal })
                {
                    int count = eligibleCounts[kind];
                    string title = kind == EditorRule.Removal ? B("Vanilla") : W(kind == EditorRule.Assignment ? "Quality" : kind == EditorRule.Participation ? "ModEffects" : "Price");
                    Widgets.Label(new Rect(0, y, width, 28), title); y += 28;
                    Widgets.Label(new Rect(0, y, width, 26), B("Eligible", count, Editor.SelectedCount)); y += 28;
                    bool enabled = GUI.enabled; GUI.enabled = enabled && count > 0 && Editor.BulkMode;
                    try
                    {
                        int index = 0;
                        foreach (var choice in new[] { RuleChoice.Include, RuleChoice.Exclude, RuleChoice.Inherit })
                        {
                            string key = kind == EditorRule.Removal ? choice == RuleChoice.Include ? "RemoveVanilla" : choice == RuleChoice.Exclude ? "KeepVanilla" : "UseRemovalRule" : choice == RuleChoice.Include ? "Enable" : choice == RuleChoice.Exclude ? "Disable" : "UseRule";
                            if (Widgets.ButtonText(new Rect(0, y + index++ * 38, width, 32), W(key))) ApplyBulk(kind, choice, items);
                        }
                    }
                    finally { GUI.enabled = enabled; }
                    y += 120;
                }
            }
            finally { Widgets.EndScrollView(); }
        }
        private void ApplyBulk(EditorRule kind, RuleChoice choice, List<EditorItem> items)
        {
            Action apply = () =>
            {
                if (Editor.SetSelectedRule(kind, items, choice)) { QualitySettingsState.Changed(); assignment.FilterKey = null; }
            };
            if (kind != EditorRule.Removal || choice != RuleChoice.Include) { apply(); return; }
            var ids = new HashSet<string>(Editor.EligibleSelected(kind, items).Select(i => i.Target.Id));
            var removable = SelectedEntries.Where(e => ids.Contains(e.Target.Id)).ToList();
            Find.WindowStack.Add(new Window_WorkbenchSelection(this, removable, () => Confirm(U("VanillaRemovalWarning"), apply)));
        }
        private sealed class Window_WorkbenchBulk : Window
        {
            private readonly Window_QualityOptions owner;
            private Vector2 scroll;
            public override Vector2 InitialSize => new Vector2(Math.Min(620, Verse.UI.screenWidth - 40), Math.Min(800, Verse.UI.screenHeight - 40));
            public Window_WorkbenchBulk(Window_QualityOptions owner) { this.owner = owner; doCloseX = true; forcePause = true; absorbInputAroundWindow = true; }
            public override void DoWindowContents(Rect rect)
            {
                var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color; var enabled = GUI.enabled;
                try { Text.Font = GameFont.Small; owner.DrawBulkInspector(rect, ref scroll); }
                finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; GUI.enabled = enabled; }
            }
        }
        private sealed class Window_WorkbenchSelection : Window
        {
            private readonly Window_QualityOptions owner;
            private readonly List<QualityCatalogEntry> removalPreview;
            private readonly Action confirm;
            private Vector2 scroll;
            public override Vector2 InitialSize => new Vector2(Math.Min(720, Verse.UI.screenWidth - 40), Math.Min(760, Verse.UI.screenHeight - 40));
            public Window_WorkbenchSelection(Window_QualityOptions owner, List<QualityCatalogEntry> removalPreview = null, Action confirm = null)
            { this.owner = owner; this.removalPreview = removalPreview; this.confirm = confirm; doCloseX = true; forcePause = true; absorbInputAroundWindow = true; }
            public override void DoWindowContents(Rect rect)
            {
                var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color; var enabled = GUI.enabled;
                try
                {
                    Text.Font = GameFont.Small;
                    var entries = removalPreview ?? owner.SelectedEntries;
                    Widgets.Label(new Rect(rect.x, rect.y, rect.width, 30), B(removalPreview == null ? "ReviewTitle" : "RemovalPreview", entries.Count));
                    float footer = removalPreview == null ? 40 : 100;
                    var list = new Rect(rect.x, rect.y + 36, rect.width, Math.Max(20, rect.height - 36 - footer));
                    const float row = 84;
                    var view = new Rect(0, 0, list.width - 18, entries.Count * row);
                    Widgets.BeginScrollView(list, ref scroll, view);
                    try
                    {
                        int first = Math.Max(0, (int)(scroll.y / row)), last = Math.Min(entries.Count, first + (int)(list.height / row) + 2);
                        for (int i = first; i < last; i++)
                        {
                            var entry = entries[i]; float y = i * row;
                            if (removalPreview == null)
                            {
                                bool check = owner.Editor.IsSelected(entry.Target.Id), before = check;
                                Widgets.CheckboxLabeled(new Rect(0, y + 8, 28, 28), "", ref check);
                                if (check != before) owner.Editor.SetSelected(entry.Target.Id, check);
                            }
                            string text = entry.Label + "\n" + entry.Target.Id + " / " + entry.Source;
                            var label = new Rect(34, y, view.width - 34, row - 4);
                            Widgets.Label(label, text); TooltipHandler.TipRegion(label, text);
                        }
                    }
                    finally { Widgets.EndScrollView(); }
                    if (removalPreview == null)
                    {
                        if (Widgets.ButtonText(new Rect(rect.x, rect.yMax - 32, rect.width, 32), B("Clear"))) owner.Editor.ClearSelection();
                    }
                    else
                    {
                        Widgets.Label(new Rect(rect.x, rect.yMax - 98, rect.width, 60), B("RemovalPreviewHelp"));
                        if (Widgets.ButtonText(new Rect(rect.x, rect.yMax - 32, rect.width, 32), B("ContinueRemoval"))) { Close(); confirm(); }
                    }
                }
                finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; GUI.enabled = enabled; }
            }
        }
    }
}
