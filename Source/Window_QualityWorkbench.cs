using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
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
        // Vanilla items edit their removal rule from the quality column; everything else edits the column's own rule.
        private static EditorRule ActualRule(QualityCatalogEntry entry, EditorRule kind) => kind == EditorRule.Assignment && entry.NativeQuality ? EditorRule.Removal : kind;
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
            const float gap = 6;
            float y = rect.y, scopeW = rect.width * .24f, searchW = rect.width * .28f, modW = rect.width * .20f, filterW = rect.width * .17f;
            var scopeRect = new Rect(rect.x, y, scopeW - gap, 30);
            if (Widgets.ButtonText(scopeRect, S("Showing", ScopeName).Truncate(scopeRect.width - 16))) Find.WindowStack.Add(new Window_WorkbenchScope(this));
            TooltipHandler.TipRegion(scopeRect, S("ChooseScope") + "\n" + ScopeName);
            var searchRect = new Rect(rect.x + scopeW, y, searchW - gap, 30);
            s.Search = Widgets.TextField(searchRect, s.Search);
            Placeholder(searchRect, W("SearchPlaceholder"), s.Search);
            TooltipHandler.TipRegion(searchRect, W("SearchHelp"));
            if (Widgets.ButtonText(new Rect(searchRect.xMax + gap, y, modW - gap, 30), U("Mod", s.Source.Length == 0 ? U("AllMods") : s.Source).Truncate(modW - gap - 16)))
                Find.WindowStack.Add(new FloatMenu(QualityCatalog.Entries.Select(e => e.Source).Concat(new[] { T("Unavailable") }).Distinct().OrderBy(v => v).Select(v => new FloatMenuOption(v, () => s.Source = v)).Prepend(new FloatMenuOption(U("AllMods"), () => s.Source = "")).ToList()));
            float filterX = searchRect.xMax + gap + modW;
            if (Widgets.ButtonText(new Rect(filterX, y, filterW - gap, 30), W("Filter" + s.ResultFilter)))
                Find.WindowStack.Add(new FloatMenu(Enumerable.Range(0, 5).Select(i => new FloatMenuOption(W("Filter" + i), () => s.ResultFilter = i)).ToList()));
            if (Widgets.ButtonText(new Rect(filterX + filterW, y, rect.xMax - filterX - filterW, 30), W("Clear"))) { s.Search = s.Source = ""; s.ResultFilter = 0; }
            RefreshWorkbench(); y += 36;
            y += DrawWorkbenchScopeRules(new Rect(rect.x, y, rect.width, 30));
            y += DrawBulkSelectionBar(new Rect(rect.x, y, rect.width, 68));
            // Measure complete outcome labels before reserving a persistent inspector.
            float outcome = new[] { W("Enabled"), W("Disabled"), W("NoQuality"), W("Native"), W("Protected"), W("Unavailable"), W("RemovalPlanned"), W("Unsupported"), W("Quality"), W("ModEffects"), W("Price") }.Max(v => Text.CalcSize(v + " *").x) + 28;
            float itemWidth = Math.Max(220, Text.CalcSize(W("Item")).x + 110);
            float tableMinimum = itemWidth + outcome * 3 + 18;
            float inspectorWidth = Math.Max(320, Text.CalcSize(W("ModEffects")).x + 150);
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
        private static readonly Color OnColor = new Color(.62f, .9f, .62f), OffColor = new Color(.78f, .78f, .78f), WarnColor = new Color(1f, .75f, .45f), LockedColor = new Color(.55f, .55f, .55f);
        private static Color OutcomeColor(string key) => key == "Enabled" || key == "Native" ? OnColor : key == "RemovalPlanned" ? WarnColor : key == "Disabled" ? OffColor : LockedColor;
        private void DrawWorkbenchTable(Rect rect, float outcomeWidth, bool inline)
        {
            const float row = 40, lead = 34 + row;
            float width = rect.width - 18, itemWidth = Math.Max(120, width - outcomeWidth * 3);
            var anchor = Text.Anchor; var color = GUI.color;
            string[] headers = { W("Item"), W("Quality"), W("ModEffects"), W("Price") };
            string[] headerHelp = { W("QualityHelp"), W("EffectsHelp"), W("PriceHelp") };
            Text.Anchor = TextAnchor.MiddleLeft; GUI.color = OffColor;
            Widgets.Label(new Rect(rect.x + lead, rect.y, itemWidth - lead, 26), headers[0]);
            Text.Anchor = TextAnchor.MiddleCenter;
            for (int c = 0; c < 3; c++)
            {
                var header = new Rect(rect.x + itemWidth + c * outcomeWidth, rect.y, outcomeWidth - 4, 26);
                Widgets.Label(header, headers[c + 1]);
                TooltipHandler.TipRegion(header, headerHelp[c]);
            }
            Text.Anchor = anchor; GUI.color = color;
            var list = new Rect(rect.x, rect.y + 28, rect.width, rect.height - 28);
            if (assignment.Filtered.Count == 0) { Widgets.Label(list, U("NoResults", ScopeName) + " " + W("ClearHelp")); return; }
            var view = new Rect(0, 0, width, assignment.Filtered.Count * row);
            Widgets.BeginScrollView(list, ref assignment.ListScroll, view);
            try
            {
                bool bulk = Editor.BulkMode;
                int first = Math.Max(0, (int)(assignment.ListScroll.y / row)), last = Math.Min(assignment.Filtered.Count, first + (int)(list.height / row) + 2);
                for (int i = first; i < last; i++)
                {
                    var entry = assignment.Filtered[i]; float y = i * row;
                    var rowRect = new Rect(0, y, width, row);
                    if (focusedItem == entry.Target.Id) Widgets.DrawHighlightSelected(rowRect);
                    else if (i % 2 == 1) Widgets.DrawLightHighlight(rowRect);
                    bool check = Editor.IsSelected(entry.Target.Id), before = check;
                    // Selection only marks items for a batch action, so the unticked box is drawn as an outline, not a cross (a8e016f).
                    var selectionRect = new Rect(0, y + (row - 28) / 2, 28, 28);
                    if (!check) Widgets.DrawBox(new Rect(selectionRect.x + 4, selectionRect.y + 2, 22, 22));
                    Widgets.CheckboxLabeled(selectionRect, "", ref check, texUnchecked: BaseContent.ClearTex);
                    TooltipHandler.TipRegion(selectionRect, U("SelectionHelp"));
                    if (check != before) Editor.SetSelected(entry.Target.Id, check);
                    if (entry.Def != null) Widgets.ThingIcon(new Rect(34, y + 4, row - 8, row - 8), entry.Def);
                    var nameRect = new Rect(lead, y, itemWidth - lead - 4, row);
                    Widgets.DrawHighlightIfMouseover(nameRect);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(nameRect, entry.Label.Truncate(nameRect.width));
                    Text.Anchor = anchor;
                    TooltipHandler.TipRegion(nameRect, entry.Label + "\n" + entry.Source);
                    if (Widgets.ButtonInvisible(nameRect))
                    { focusedItem = entry.Target.Id; focusedWorkbenchEntry = entry; if (!inline && !bulk) Find.WindowStack.Add(new Window_WorkbenchDetails(this, entry)); }
                    for (int c = 0; c < 3; c++)
                    {
                        var kind = (EditorRule)c; var actual = ActualRule(entry, kind); var item = entry;
                        var cell = new Rect(itemWidth + c * outcomeWidth, y + 4, outcomeWidth - 4, row - 8);
                        string key = OutcomeKey(entry, kind);
                        bool editable = !bulk && (QualityEditorActions.TargetFor(actual, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality).Supported || QualityEditorActions.RulesFor(Draft, actual).Items.ContainsKey(entry.Target.Id));
                        GUI.color = OutcomeColor(key);
                        if (editable)
                        {
                            if (Widgets.ButtonText(cell, Outcome(entry, kind))) { focusedItem = entry.Target.Id; focusedWorkbenchEntry = entry; OpenItemMenu(entry, actual); }
                        }
                        else
                        {
                            // Locked outcomes and bulk mode are plain text, so only real choices look clickable.
                            Text.Anchor = TextAnchor.MiddleCenter; Widgets.Label(cell, Outcome(entry, kind)); Text.Anchor = anchor;
                        }
                        GUI.color = color;
                        TooltipHandler.TipRegion(cell, new TipSignal(() => GoverningSource(item, actual), (entry.Target.Id.GetHashCode() * 397) ^ c));
                    }
                }
            }
            finally { Widgets.EndScrollView(); Text.Anchor = anchor; GUI.color = color; }
        }
        private string OutcomeKey(QualityCatalogEntry entry, EditorRule kind)
        {
            if (kind == EditorRule.Assignment && entry.NativeQuality) return entry.RemovableVanillaQuality ? HasQuality(entry) ? "Native" : "RemovalPlanned" : "Protected";
            if (entry.Def == null) return "Unavailable";
            if (kind == EditorRule.Price && entry.NativeQuality && !entry.RemovableVanillaQuality) return "Protected";
            if (kind != EditorRule.Assignment && !HasQuality(entry)) return "NoQuality";
            var target = QualityEditorActions.TargetFor(kind, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality);
            return !target.Supported ? "Unsupported" : QualityRules.Resolve(target, QualityEditorActions.RulesFor(Draft, kind)).Enabled ? "Enabled" : "Disabled";
        }
        private string Outcome(QualityCatalogEntry entry, EditorRule kind) => W(OutcomeKey(entry, kind)) + (QualityEditorActions.RulesFor(Draft, ActualRule(entry, kind)).Items.ContainsKey(entry.Target.Id) ? " *" : "");
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
            foreach (var choice in new[] { RuleChoice.Include, RuleChoice.Exclude, RuleChoice.Inherit })
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
        // One plain sentence that answers "does quality do anything for this item?".
        private string ItemSummary(QualityCatalogEntry entry, List<EffectId> support)
        {
            if (entry.Def == null) return W("UnavailableHelp");
            if (!HasQuality(entry)) return W("SummaryNoQuality");
            var target = QualityEditorActions.TargetFor(EditorRule.Participation, entry.Target, entry.NativeQuality, entry.RemovableVanillaQuality);
            if (!QualityRules.Resolve(target, Draft.Participation).Enabled) return W("SummaryEffectsOff");
            var active = support.Where(id => Window_QualitySupport.Active(id, entry, Draft)).Select(id => T("Effect." + id)).ToArray();
            return active.Length == 0 ? W("SummaryNoEffects") : string.Format(W("SummaryEffects"), active.Length, string.Join(", ", active));
        }
        private void DrawWorkbenchInspector(Rect rect, QualityCatalogEntry entry, ref Vector2 scroll, Action<EffectId> navigate = null)
        {
            navigate = navigate ?? SelectEffect;
            float contentWidth = rect.width - 18;
            string sourceLabel = entry.Source + (entry.Target.Group.Length > 0 ? " / " + GroupLabel(entry.Target.Group) : "");
            string help = entry.Def == null ? "" : entry.NativeQuality ? entry.RemovableVanillaQuality ? W("VanillaHelp") : T("NativeProtected") : "";
            string technical = entry.Target.Id + "\n" + string.Join("\n", entry.Target.Categories.Select(c => categoryNodes.FirstOrDefault(n => n.Def.defName == c.Id)?.Path ?? c.Id));
            var support = Window_QualitySupport.Candidates(entry.Def).ToList();
            string summary = ItemSummary(entry, support);
            Func<string, float, float> height = (text, minimum) => Math.Max(minimum, Text.CalcHeight(text, contentWidth));
            float contentHeight = height(entry.Label, 30) + 6 + height(sourceLabel, 24) + 6 + height(summary, 30) + 12;
            foreach (var kind in new[] { EditorRule.Assignment, EditorRule.Participation, EditorRule.Price })
                contentHeight += 38 + height(GoverningSource(entry, ActualRule(entry, kind)), 24) + 8;
            if (support.Count > 0) contentHeight += 30;
            contentHeight += support.Sum(id => 34 + height(Window_QualitySupport.Status(id, entry, Draft), 24) + 6);
            contentHeight += 38 + (help.Length > 0 ? height(help, 30) + 6 : 0) + 36;
            if (technicalDetails) contentHeight += height(technical, 60) + 6;
            var view = new Rect(0, 0, contentWidth, contentHeight);
            var color = GUI.color;
            Widgets.BeginScrollView(rect, ref scroll, view);
            try
            {
                float y = 0;
                Action<string, float, Color> label = (text, labelHeight, tint) => { GUI.color = tint; Widgets.Label(new Rect(0, y, view.width, labelHeight), text); GUI.color = color; y += labelHeight + 6; };
                label(entry.Label, height(entry.Label, 30), color);
                label(sourceLabel, height(sourceLabel, 24), OffColor);
                label(summary, height(summary, 30), color); y += 6;
                foreach (var kind in new[] { EditorRule.Assignment, EditorRule.Participation, EditorRule.Price })
                {
                    var actual = ActualRule(entry, kind);
                    GUI.color = OutcomeColor(OutcomeKey(entry, kind));
                    if (Widgets.ButtonText(new Rect(0, y, view.width, 32), W(kind == EditorRule.Assignment ? "Quality" : kind == EditorRule.Participation ? "ModEffects" : "Price") + ": " + Outcome(entry, kind))) OpenItemMenu(entry, actual);
                    GUI.color = color;
                    y += 38;
                    string source = GoverningSource(entry, actual);
                    label(source, height(source, 24), OffColor); y += 2;
                }
                if (support.Count > 0) label(W("EffectList"), 24, color);
                foreach (var id in support)
                {
                    if (Widgets.ButtonText(new Rect(0, y, view.width, 28), T("Effect." + id))) navigate(id);
                    y += 34;
                    string status = Window_QualitySupport.Status(id, entry, Draft);
                    label(status, height(status, 24), Window_QualitySupport.Active(id, entry, Draft) ? OnColor : OffColor);
                }
                if (Widgets.ButtonText(new Rect(0, y, view.width, 32), U("SupportInfo"))) Find.WindowStack.Add(new Window_QualitySupport(entry, () => Draft, navigate)); y += 38;
                if (help.Length > 0) label(help, height(help, 30), OffColor);
                if (Widgets.ButtonText(new Rect(0, y, view.width, 30), W("Technical"))) technicalDetails = !technicalDetails; y += 36;
                if (technicalDetails) label(technical, height(technical, 60), OffColor);
            }
            finally { Widgets.EndScrollView(); GUI.color = color; }
        }
        private sealed class Window_WorkbenchDetails : Window
        {
            private readonly Window_QualityOptions owner;
            private readonly QualityCatalogEntry entry;
            private Vector2 scroll;
            public override Vector2 InitialSize => new Vector2(Math.Min(620, Verse.UI.screenWidth - 40), Math.Min(800, Verse.UI.screenHeight - 40));
            public Window_WorkbenchDetails(Window_QualityOptions owner, QualityCatalogEntry entry) { this.owner = owner; this.entry = entry; doCloseX = true; forcePause = true; absorbInputAroundWindow = true; }
            private void NavigateEffect(EffectId id) { owner.SelectEffect(id); Close(); }
            public override void DoWindowContents(Rect rect)
            {
                var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color; var enabled = GUI.enabled;
                try { Text.Font = GameFont.Small; owner.DrawWorkbenchInspector(rect, entry, ref scroll, NavigateEffect); }
                finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; GUI.enabled = enabled; }
            }
        }
    }
}
