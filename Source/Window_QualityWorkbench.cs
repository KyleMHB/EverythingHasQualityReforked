using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    internal sealed partial class Window_QualityOptions
    {
        private string focusedItem;
        private QualityCatalogEntry focusedWorkbenchEntry;
        private Vector2 inspectorScroll;
        private bool technicalDetails;
        private int scopeTotal;
        private static string W(string key) => ("QEverything.Workbench." + key).Translate().ToString();
        private IEnumerable<RuleSet> WorkbenchRules => new[] { Draft.Assignment, Draft.Participation, Draft.MarketValue, Draft.VanillaRemoval };
        private bool HasException(QualityCatalogEntry entry) => WorkbenchRules.Any(r => r.Items.ContainsKey(entry.Target.Id));
        private bool HasQuality(QualityCatalogEntry entry) => QualityEditorActions.HasQuality(Draft, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality);
        private void RefreshWorkbench()
        {
            var s = assignment;
            string key = QualityCatalog.Revision + "|" + QualitySettingsState.Revision + "|" + s.Search + "|" + s.Group + "|" + s.Category + "|" + s.Source + "|" + s.ResultFilter;
            if (key == s.FilterKey) return;
            s.FilterKey = key;
            var entries = new List<QualityCatalogEntry>(QualityCatalog.Entries);
            var known = new HashSet<string>(entries.Select(e => e.Target.Id));
            foreach (string id in WorkbenchRules.SelectMany(r => r.Items.Keys).Distinct().Where(id => !known.Contains(id)))
                entries.Add(new QualityCatalogEntry { Target = new RuleTarget(id, "", false, new CategoryPath[0]), Label = id, Source = T("Unavailable") });
            var scoped = entries.Where(e => (s.Group.Length == 0 || e.Target.Group == s.Group) && (s.Category.Length == 0 || e.Target.Categories.Any(c => c.Id == s.Category))).ToList();
            scopeTotal = scoped.Count;
            string query = s.Search.Trim();
            s.Filtered = scoped.Where(e => (query.Length == 0 || Matches(e.Label, query) || Matches(e.Target.Id, query) || Matches(e.Source, query))
                && (s.Source.Length == 0 || s.Source == e.Source)
                && (s.ResultFilter == 0 || s.ResultFilter == 1 && HasQuality(e) || s.ResultFilter == 2 && !HasQuality(e) || s.ResultFilter == 3 && HasException(e) || s.ResultFilter == 4 && e.NativeQuality))
                .OrderBy(e => e.Label, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        private void DrawWorkbench(Rect rect)
        {
            var s = assignment;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, rect.width * .55f, 30), S("ChooseScope"))) Find.WindowStack.Add(new Window_WorkbenchScope(this));
            Widgets.Label(new Rect(rect.x + rect.width * .57f, rect.y, rect.width * .43f, 30), W("DraftOutcomes"));
            float y = rect.y + 36;
            s.Search = Widgets.TextField(new Rect(rect.x, y, rect.width * .48f, 30), s.Search);
            TooltipHandler.TipRegion(new Rect(rect.x, y, rect.width * .48f, 30), W("SearchHelp"));
            if (Widgets.ButtonText(new Rect(rect.x + rect.width * .49f, y, rect.width * .20f, 30), U("Mod", s.Source.Length == 0 ? U("AllMods") : s.Source)))
                Find.WindowStack.Add(new FloatMenu(QualityCatalog.Entries.Select(e => e.Source).Concat(new[] { T("Unavailable") }).Distinct().OrderBy(v => v).Select(v => new FloatMenuOption(v, () => s.Source = v)).Prepend(new FloatMenuOption(U("AllMods"), () => s.Source = "")).ToList()));
            if (Widgets.ButtonText(new Rect(rect.x + rect.width * .70f, y, rect.width * .19f, 30), W("Filter" + s.ResultFilter)))
                Find.WindowStack.Add(new FloatMenu(Enumerable.Range(0, 5).Select(i => new FloatMenuOption(W("Filter" + i), () => s.ResultFilter = i)).ToList()));
            if (Widgets.ButtonText(new Rect(rect.x + rect.width * .90f, y, rect.width * .10f, 30), W("Clear"))) { s.Search = s.Source = ""; s.ResultFilter = 0; }
            RefreshWorkbench(); y += 36;
            y += DrawWorkbenchScopeControls(new Rect(rect.x, y, rect.width, rect.yMax - y));
            y += DrawBulkSelectionBar(new Rect(rect.x, y, rect.width, 68));
            // Measure complete outcome labels before reserving a persistent inspector.
            float outcome = new[] { W("Enabled"), W("Disabled"), W("NoQuality"), W("Native"), W("Protected"), W("Unavailable"), W("RemovalPlanned"), W("Unsupported") }.Max(v => Text.CalcSize(v + " *").x) + 18;
            float itemWidth = Math.Max(170, Text.CalcSize(W("Item")).x + 40);
            float tableMinimum = itemWidth + outcome * 3 + 18;
            float inspectorWidth = Math.Max(300, Text.CalcSize(W("ModEffects")).x + 150);
            bool inline = rect.width >= tableMinimum + inspectorWidth + 12;
            var table = new Rect(rect.x, y, inline ? rect.width - inspectorWidth - 12 : rect.width, rect.yMax - y);
            DrawWorkbenchTable(table, outcome, inline);
            if (inline)
            {
                var entry = WorkbenchEntry(focusedItem);
                var inspector = new Rect(table.xMax + 12, y, inspectorWidth, rect.yMax - y);
                if (Editor.BulkMode) DrawBulkInspector(inspector, ref bulkScroll);
                else if (entry == null) Widgets.Label(inspector, W("FocusHelp"));
                else DrawWorkbenchInspector(inspector, entry, ref inspectorScroll);
            }
        }
        private QualityCatalogEntry WorkbenchEntry(string id) => id == null ? null : QualityCatalog.Entries.FirstOrDefault(e => e.Target.Id == id) ?? (focusedWorkbenchEntry?.Target.Id == id ? focusedWorkbenchEntry : assignment.Filtered.FirstOrDefault(e => e.Target.Id == id));
        private void DrawWorkbenchTable(Rect rect, float outcomeWidth, bool inline)
        {
            float width = rect.width - 18, itemWidth = Math.Max(80, width - outcomeWidth * 3);
            string[] headers = { W("Item"), W("Quality"), W("ModEffects"), W("Price") };
            Widgets.Label(new Rect(rect.x, rect.y, itemWidth, 26), headers[0]);
            for (int c = 0; c < 3; c++) Widgets.Label(new Rect(rect.x + itemWidth + c * outcomeWidth, rect.y, outcomeWidth, 26), headers[c + 1]);
            var list = new Rect(rect.x, rect.y + 28, rect.width, rect.height - 28);
            if (assignment.Filtered.Count == 0) { Widgets.Label(list, U("NoResults", ScopeName) + " " + W("ClearHelp")); return; }
            const float row = 62;
            var view = new Rect(0, 0, width, assignment.Filtered.Count * row);
            Widgets.BeginScrollView(list, ref assignment.ListScroll, view);
            try
            {
                int first = Math.Max(0, (int)(assignment.ListScroll.y / row)), last = Math.Min(assignment.Filtered.Count, first + (int)(list.height / row) + 2);
                for (int i = first; i < last; i++)
                {
                    var entry = assignment.Filtered[i]; float y = i * row;
                    if (focusedItem == entry.Target.Id) Widgets.DrawHighlightSelected(new Rect(0, y, width, row));
                    bool check = Editor.IsSelected(entry.Target.Id), before = check;
                    Widgets.CheckboxLabeled(new Rect(0, y + 16, 28, 28), "", ref check);
                    if (check != before) Editor.SetSelected(entry.Target.Id, check);
                    if (Widgets.ButtonText(new Rect(32, y + 2, itemWidth - 36, row - 4), entry.Label + (inline || Editor.BulkMode ? "" : "\n" + W("Details")), drawBackground: false))
                    { focusedItem = entry.Target.Id; focusedWorkbenchEntry = entry; if (!inline && !Editor.BulkMode) Find.WindowStack.Add(new Window_WorkbenchDetails(this, entry)); }
                    for (int c = 0; c < 3; c++)
                    {
                        var kind = (EditorRule)c;
                        var cell = new Rect(itemWidth + c * outcomeWidth, y + 6, outcomeWidth - 4, row - 12);
                        if (Widgets.ButtonText(cell, Outcome(entry, kind), drawBackground: false)) { focusedItem = entry.Target.Id; focusedWorkbenchEntry = entry; if (Editor.BulkMode) { if (!inline) Find.WindowStack.Add(new Window_WorkbenchBulk(this)); } else OpenItemMenu(entry, kind == EditorRule.Assignment && entry.NativeQuality ? EditorRule.Removal : kind); }
                    }
                }
            }
            finally { Widgets.EndScrollView(); }
        }
        private string Outcome(QualityCatalogEntry entry, EditorRule kind)
        {
            var rules = QualityEditorActions.RulesFor(Draft, kind);
            string marker = rules.Items.ContainsKey(entry.Target.Id) ? " *" : "";
            if (kind == EditorRule.Assignment && entry.NativeQuality)
                return W(entry.RemovableVanillaQuality ? HasQuality(entry) ? "Native" : "RemovalPlanned" : "Protected") + (Draft.VanillaRemoval.Items.ContainsKey(entry.Target.Id) ? " *" : "");
            if (entry.Def == null) return W("Unavailable") + marker;
            if (kind == EditorRule.Price && entry.NativeQuality && !entry.RemovableVanillaQuality) return W("Protected") + marker;
            if (kind != EditorRule.Assignment && !HasQuality(entry)) return W("NoQuality") + marker;
            var target = QualityEditorActions.TargetFor(kind, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality);
            return W(!target.Supported ? "Unsupported" : QualityRules.Resolve(target, rules).Enabled ? "Enabled" : "Disabled") + marker;
        }
        private string GoverningSource(QualityCatalogEntry entry, EditorRule kind)
        {
            var target = QualityEditorActions.TargetFor(kind, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality);
            var resolution = QualityRules.Resolve(target, QualityEditorActions.RulesFor(Draft, kind));
            string source = resolution.ReasonKind == "Category" ? categoryNodes.FirstOrDefault(n => n.Def.defName == resolution.SourceId)?.Path ?? resolution.SourceId : resolution.ReasonKind == "Group" ? GroupLabel(resolution.SourceId) : resolution.ReasonKind == "Global" ? U("GlobalDefault") : T(resolution.ReasonKind);
            return W("Governing") + ": " + source + (resolution.Conflict ? "\n" + W("Conflict") : "");
        }
        private void OpenItemMenu(QualityCatalogEntry entry, EditorRule kind)
        {
            var options = new List<FloatMenuOption> { new FloatMenuOption(GoverningSource(entry, kind), null) };
            bool eligible = QualityEditorActions.TargetFor(kind, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality).Supported;
            foreach (RuleChoice choice in Enum.GetValues(typeof(RuleChoice)))
            {
                var selected = choice;
                string label = kind == EditorRule.Removal ? W(choice == RuleChoice.Inherit ? "UseRemovalRule" : choice == RuleChoice.Include ? "RemoveVanilla" : "KeepVanilla") : W(choice == RuleChoice.Inherit ? "UseRule" : choice == RuleChoice.Include ? "Enable" : "Disable");
                options.Add(new FloatMenuOption(label, eligible || choice == RuleChoice.Inherit ? (Action)(() =>
                {
                    Action apply = () => { if (Editor.SetItemRule(kind, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality, selected)) { QualitySettingsState.Changed(); assignment.FilterKey = null; } };
                    if (kind == EditorRule.Removal && selected == RuleChoice.Include) Confirm(U("VanillaRemovalWarning"), apply); else apply();
                }) : null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        private void DrawWorkbenchInspector(Rect rect, QualityCatalogEntry entry, ref Vector2 scroll)
        {
            float contentWidth = rect.width - 18;
            string sourceLabel = entry.Source + (entry.Target.Group.Length > 0 ? " / " + GroupLabel(entry.Target.Group) : "");
            string help = entry.Def == null ? W("UnavailableHelp") : entry.NativeQuality ? entry.RemovableVanillaQuality ? W("VanillaHelp") : T("NativeProtected") : T("NeedsQuality");
            string technical = entry.Target.Id + "\n" + string.Join("\n", entry.Target.Categories.Select(c => categoryNodes.FirstOrDefault(n => n.Def.defName == c.Id)?.Path ?? c.Id));
            Func<string, float, float> height = (text, minimum) => Math.Max(minimum, Text.CalcHeight(text, contentWidth));
            float contentHeight = height(entry.Label, 42) + 6 + height(sourceLabel, 46) + 6 + height(W("ExceptionHelp"), 42) + 6;
            foreach (var kind in new[] { EditorRule.Assignment, EditorRule.Participation, EditorRule.Price })
            {
                var actual = kind == EditorRule.Assignment && entry.NativeQuality ? EditorRule.Removal : kind;
                contentHeight += 42 + height(GoverningSource(entry, actual), 38) + 6;
            }
            contentHeight += 38 + height(help, 56) + 6 + 36;
            if (technicalDetails) contentHeight += height(technical, 160) + 6;
            var view = new Rect(0, 0, contentWidth, contentHeight);
            Widgets.BeginScrollView(rect, ref scroll, view);
            try
            {
                float y = 0;
                Action<string, float> label = (text, labelHeight) => { Widgets.Label(new Rect(0, y, view.width, labelHeight), text); y += labelHeight + 6; };
                label(entry.Label, Math.Max(42, Text.CalcHeight(entry.Label, view.width)));
                label(sourceLabel, height(sourceLabel, 46));
                label(W("ExceptionHelp"), height(W("ExceptionHelp"), 42));
                foreach (var kind in new[] { EditorRule.Assignment, EditorRule.Participation, EditorRule.Price })
                {
                    var actual = kind == EditorRule.Assignment && entry.NativeQuality ? EditorRule.Removal : kind;
                    if (Widgets.ButtonText(new Rect(0, y, view.width, 36), W(kind == EditorRule.Assignment ? "Quality" : kind == EditorRule.Participation ? "ModEffects" : "Price") + ": " + Outcome(entry, kind))) OpenItemMenu(entry, actual);
                    y += 42;
                    string source = GoverningSource(entry, actual);
                    label(source, Math.Max(38, Text.CalcHeight(source, view.width)));
                }
                if (Widgets.ButtonText(new Rect(0, y, view.width, 32), U("SupportInfo"))) Find.WindowStack.Add(new Window_QualitySupport(entry)); y += 38;
                label(help, Math.Max(56, Text.CalcHeight(help, view.width)));
                if (Widgets.ButtonText(new Rect(0, y, view.width, 30), W("Technical"))) technicalDetails = !technicalDetails; y += 36;
                if (technicalDetails) label(technical, height(technical, 160));
            }
            finally { Widgets.EndScrollView(); }
        }
        private sealed class Window_WorkbenchDetails : Window
        {
            private readonly Window_QualityOptions owner;
            private readonly QualityCatalogEntry entry;
            private Vector2 scroll;
            public override Vector2 InitialSize => new Vector2(Math.Min(620, Verse.UI.screenWidth - 40), Math.Min(800, Verse.UI.screenHeight - 40));
            public Window_WorkbenchDetails(Window_QualityOptions owner, QualityCatalogEntry entry) { this.owner = owner; this.entry = entry; doCloseX = true; forcePause = true; absorbInputAroundWindow = true; }
            public override void DoWindowContents(Rect rect)
            {
                var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color; var enabled = GUI.enabled;
                try { Text.Font = GameFont.Small; owner.DrawWorkbenchInspector(rect, entry, ref scroll); }
                finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; GUI.enabled = enabled; }
            }
        }
    }
}
