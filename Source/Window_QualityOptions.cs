using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
namespace QualityEverything
{
    internal sealed partial class Window_QualityOptions : Window
    {
        private sealed class BrowserState
        {
            public string Search = "", Group = "", Category = "", Source = "", FilterKey;
            public int ResultFilter, SelectionRevision, HiddenRevision = -1, Hidden;
            public string HiddenFilterKey;
            public bool OverridesOnly, Reviewing;
            public Vector2 TreeScroll, ListScroll;
            public readonly HashSet<string> Selected = new HashSet<string>(StringComparer.Ordinal);
            public BrowserState Previous;
            public List<QualityCatalogEntry> Filtered = new List<QualityCatalogEntry>();
            public BrowserState Filters() => new BrowserState { Search = Search, Group = Group, Category = Category, Source = Source, ResultFilter = ResultFilter, OverridesOnly = OverridesOnly };
            public void Restore(BrowserState f) { Search = f.Search; Group = f.Group; Category = f.Category; Source = f.Source; ResultFilter = f.ResultFilter; OverridesOnly = f.OverridesOnly; Reviewing = false; ListScroll = Vector2.zero; }
        }
        private sealed class CategoryNode { public ThingCategoryDef Def; public string Path; public int Depth; public string[] Ancestors; }
        private readonly BrowserState assignment = new BrowserState(), targets = new BrowserState();
        private BrowserState Browser => participation ? targets : assignment;
        private bool participation, targetView, closing;
        private int page;
        private readonly Action afterClose;
        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        private readonly Stack<QualityConfiguration> undo = new Stack<QualityConfiguration>();
        private Vector2 productionScroll, effectScroll, compatibilityScroll;
        private string error = "";
        private readonly HashSet<string> expanded = new HashSet<string>();
        private readonly List<CategoryNode> categoryNodes;
        private List<CategoryNode> visibleCategoryNodes;
        private readonly string[] groupIds = QualitySettingsState.GroupFields.Keys.ToArray();
        private readonly Dictionary<string, int[]> counts = new Dictionary<string, int[]>();
        private int countRevision = -1;
        private bool countParticipation;
        private QualityConfiguration Draft => QualitySettingsState.Draft;
        private RuleSet Rules => participation ? Draft.Participation : Draft.Assignment;
        public override Vector2 InitialSize => new Vector2(1120, 820);
        private static string T(string key) => ("QEverything.Options." + key).Translate().ToString();
        private static string U(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, T("UX." + key), args);
        private static string GroupLabel(string id) => T("Group." + id);
        private static string ChoiceLabel(RuleChoice choice) => T(choice.ToString());
        public Window_QualityOptions(Action afterClose = null)
        {
            this.afterClose = afterClose; doCloseX = true; closeOnClickedOutside = false; absorbInputAroundWindow = true; forcePause = true;
            QualityCatalog.Build(); categoryNodes = DefDatabase<ThingCategoryDef>.AllDefsListForReading.Select(BuildNode).OrderBy(n => n.Path, StringComparer.CurrentCultureIgnoreCase).ToList();
            foreach (var node in categoryNodes.Where(n => n.Def.parent == null)) expanded.Add(node.Def.defName);
        }
        private static CategoryNode BuildNode(ThingCategoryDef def)
        {
            var chain = new List<ThingCategoryDef>(); var seen = new HashSet<ThingCategoryDef>();
            for (var current = def; current != null && seen.Add(current); current = current.parent) chain.Add(current);
            chain.Reverse(); return new CategoryNode { Def = def, Depth = chain.Count, Path = string.Join(" / ", chain.Select(c => c.LabelCap.ToString())), Ancestors = chain.Where(c => c != def).Select(c => c.defName).ToArray() };
        }
        public override void PostClose() { base.PostClose(); afterClose?.Invoke(); }
        public override bool OnCloseRequest()
        {
            if (!closing && (QualitySettingsState.Dirty || buffers.Any(p => !BufferIsValid(p.Key, p.Value, out _))))
            { if (!Find.WindowStack.IsOpen<Window_QualityClose>()) Find.WindowStack.Add(new Window_QualityClose(this)); return false; }
            return true;
        }
        internal void Finish(bool save) { if (save && !Save()) return; if (!save) QualitySettingsState.Discard(); closing = true; Close(); }
        private bool Save()
        {
            if (buffers.Any(p => !BufferIsValid(p.Key, p.Value, out _))) { error = T("InvalidInput"); return false; }
            var errors = QualitySettingsState.Save(); error = errors.Count == 0 ? T("RestartPending") : T("InvalidInput") + " " + string.Join(", ", errors);
            if (errors.Count == 0) undo.Clear(); return errors.Count == 0;
        }
        private void RememberDraft() { if (undo.Count >= 30) { var recent = undo.Take(29).Reverse().ToArray(); undo.Clear(); foreach (var item in recent) undo.Push(item); } undo.Push(Draft.Clone()); }
        private void DraftChanged() { QualitySettingsState.Changed(); assignment.FilterKey = targets.FilterKey = null; error = ""; }
        public override void DoWindowContents(Rect rect)
        {
            var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color;
            try
            {
                Text.Font = GameFont.Small; var titles = new[] { U("Assignment"), U("Effects"), T("Production") }; float tabWidth = (rect.width - 6 * (titles.Length - 1)) / titles.Length;
                for (int i = 0; i < titles.Length; i++) { var tab = new Rect(rect.x + i * (tabWidth + 6), rect.y, tabWidth, 32); if (page == i) Widgets.DrawHighlightSelected(tab); if (Widgets.ButtonText(tab, titles[i], drawBackground: page != i)) { page = i; GUI.FocusControl(null); } }
                participation = page == 1;
                Widgets.Label(new Rect(rect.x, rect.y + 38, rect.width, 28), (QualitySettingsState.Dirty ? T("DraftPending") : T("DraftSaved")) + "  " + (QualitySettingsState.RestartPending ? T("RestartPending") : T("ActiveSaved")));
                var body = new Rect(rect.x, rect.y + 72, rect.width, Math.Max(100, rect.height - 164));
                if (page == 0) DrawRules(body);
                else if (page == 1) { float half = (body.width - 8) / 2; if (Widgets.ButtonText(new Rect(body.x, body.y, half, 30), U("ChooseEffects"), drawBackground: targetView)) targetView = false; if (Widgets.ButtonText(new Rect(body.x + half + 8, body.y, half, 30), U("EffectItems"), drawBackground: !targetView)) targetView = true; body.y += 38; body.height -= 38; if (targetView) DrawRules(body); else DrawEffects(body); }
                else if (page == 2) DrawProduction(body);
                Widgets.Label(new Rect(rect.x, rect.yMax - 82, rect.width, 43), error.Length > 0 ? error : T("RemovalWarning")); float w = (rect.width - 32) / 5;
                if (Widgets.ButtonText(new Rect(rect.x, rect.yMax - 34, w, 32), U("Undo")) && undo.Count > 0) { QualitySettingsState.ReplaceDraft(undo.Pop()); buffers.Clear(); DraftChanged(); }
                if (Widgets.ButtonText(new Rect(rect.x + w + 8, rect.yMax - 34, w, 32), T("Discard"))) { QualitySettingsState.Discard(); buffers.Clear(); undo.Clear(); error = ""; }
                if (Widgets.ButtonText(new Rect(rect.x + 2 * (w + 8), rect.yMax - 34, w, 32), T("ResetAll"))) Confirm(T("ResetConfirm"), () => { RememberDraft(); Mod_SettingsUtility.RestoreDefaults(); buffers.Clear(); DraftChanged(); });
                if (Widgets.ButtonText(new Rect(rect.x + 3 * (w + 8), rect.yMax - 34, w, 32), T("Save"))) Save();
                if (Widgets.ButtonText(new Rect(rect.x + 4 * (w + 8), rect.yMax - 34, w, 32), T("Close"))) Close();
            }
            finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; }
        }
        private static void Confirm(string text, Action action) => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, action));
        private string ScopeName => Browser.Category.Length > 0 ? (DefDatabase<ThingCategoryDef>.GetNamedSilentFail(Browser.Category)?.LabelCap.ToString() ?? Browser.Category) : Browser.Group.Length > 0 ? GroupLabel(Browser.Group) : U("AllItems");
        private void DrawRules(Rect rect)
        {
            float left = Math.Min(260, rect.width * .28f); DrawBrowser(new Rect(rect.x, rect.y, left, rect.height));
            var area = new Rect(rect.x + left + 12, rect.y, rect.width - left - 12, rect.height); var s = Browser; float y = area.y;
            Widgets.Label(new Rect(area.x, y, area.width, 24), ScopeName); y += 26;
            if (s.Group.Length > 0 || s.Category.Length > 0)
            {
                string current = s.Group.Length > 0 ? U(Rules.Groups.TryGetValue(s.Group, out var yes) && yes ? "DefaultYes" : "DefaultNo") : RuleText(Rules.Categories.TryGetValue(s.Category, out var val) ? val : RuleChoice.Inherit, false);
                if (Widgets.ButtonText(new Rect(area.x, y, area.width * .6f, 30), U(participation ? "ApplyEffects" : "GiveQuality") + ": " + current)) SelectScopeRule();
                if (Widgets.ButtonText(new Rect(area.x + area.width * .62f, y, area.width * .38f, 30), U("CustomItems"))) { s.Search = s.Source = ""; s.ResultFilter = 0; s.OverridesOnly = true; s.Reviewing = false; }
                y += 34; Widgets.Label(new Rect(area.x, y, area.width, 24), ScopeSummary()); y += 27;
            }
            else { Widgets.Label(new Rect(area.x, y, area.width, 40), U(participation ? "TargetsHelp" : "AssignmentHelp")); y += 42; }
            Widgets.Label(new Rect(area.x, y, area.width, 22), U("SearchIn", ScopeName)); y += 22;
            GUI.SetNextControlName(participation ? "quality-target-search" : "quality-assignment-search");
            string next = Widgets.TextField(new Rect(area.x, y, area.width - 72, 30), s.Search); if (next != s.Search) { s.Search = next; s.ListScroll = Vector2.zero; }
            if (Widgets.ButtonText(new Rect(area.xMax - 66, y, 66, 30), U("ClearSearch"))) s.Search = ""; y += 34;
            float third = (area.width - 12) / 3;
            if (Widgets.ButtonText(new Rect(area.x, y, third, 30), U("Mod", s.Source.Length == 0 ? U("AllMods") : s.Source))) SelectSource();
            if (Widgets.ButtonText(new Rect(area.x + third + 6, y, third, 30), U("Show", T(new[] { "AllResults", "Included", "Excluded" }[s.ResultFilter])))) Find.WindowStack.Add(new FloatMenu(Enumerable.Range(0, 3).Select(i => new FloatMenuOption(T(new[] { "AllResults", "Included", "Excluded" }[i]), () => s.ResultFilter = i)).ToList()));
            Widgets.CheckboxLabeled(new Rect(area.x + 2 * (third + 6), y, third, 30), U("CustomOnly"), ref s.OverridesOnly); y += 34; Refresh(); float action = (area.width - 12) / 3;
            if (Widgets.ButtonText(new Rect(area.x, y, action, 28), U("SelectResults", s.Filtered.Count))) { foreach (var e in s.Filtered) s.Selected.Add(e.Target.Id); s.SelectionRevision++; }
            if (Widgets.ButtonText(new Rect(area.x + action + 6, y, action, 28), U("AllCategories"))) { s.Group = s.Category = ""; s.ListScroll = Vector2.zero; }
            if (Widgets.ButtonText(new Rect(area.x + 2 * (action + 6), y, action, 28), U("ClearFilters"))) ClearFilters(); y += 34;
            if (s.Selected.Count > 0 || s.Reviewing)
            {
                int hidden = SelectionHidden(s); Widgets.Label(new Rect(area.x, y, area.width * .48f, 28), U("Selected", s.Selected.Count, hidden));
                if (Widgets.ButtonText(new Rect(area.x + area.width * .49f, y, area.width * .30f, 28), U(s.Reviewing ? "BackResults" : "ReviewSelected"))) ReviewSelected();
                if (Widgets.ButtonText(new Rect(area.x + area.width * .80f, y, area.width * .20f, 28), U("ClearSelection"))) { s.Selected.Clear(); s.SelectionRevision++; if (s.Reviewing) ReviewSelected(); }
                y += 32;
                var choices = new[] { RuleChoice.Include, RuleChoice.Exclude, RuleChoice.Inherit }; for (int i = 0; i < choices.Length; i++) { var choice = choices[i]; if (Widgets.ButtonText(new Rect(area.x + i * (action + 6), y, action, 30), RuleText(choice, true))) ApplySelected(choice); }
                y += 34;
            }
            else { Widgets.Label(new Rect(area.x, y, area.width, 28), U("SelectionHelp")); y += 30; }
            DrawItems(new Rect(area.x, y, area.width, Math.Max(45, area.yMax - y)));
        }
        private int summaryRevision = -1;
        private string summaryKey, summaryText;
        private string ScopeSummary()
        {
            var s = Browser; string key = participation + "|" + s.Group + "|" + s.Category;
            if (summaryRevision == QualitySettingsState.Revision && summaryKey == key) return summaryText;
            summaryRevision = QualitySettingsState.Revision; summaryKey = key;
            var scoped = QualityCatalog.Entries.Where(e => (s.Group.Length == 0 || e.Target.Group == s.Group) && (s.Category.Length == 0 || e.Target.Categories.Any(c => c.Id == s.Category))).ToArray();
            int custom = scoped.Count(e => Rules.Items.ContainsKey(e.Target.Id));
            int deeper = scoped.SelectMany(e => e.Target.Categories).Select(c => c.Id).Distinct().Count(id => id != s.Category && Rules.Categories.ContainsKey(id));
            if (s.Category.Length == 0 || scoped.Length == 0) return summaryText = U("ScopeSummary", scoped.Count(e => Resolve(e).Enabled), scoped.Length, custom, deeper);
            var node = categoryNodes.First(n => n.Def.defName == s.Category);
            var ancestors = node.Ancestors.Select((id, i) => new CategoryPath(id, i + 1)).ToArray();
            var parents = scoped.Select(e => QualityRules.Resolve(new RuleTarget("", e.Target.Group, true, ancestors), Rules)).Select(r => (r.Enabled ? T("Included") : T("Excluded")) + " / " + (r.ReasonKind == "Category" ? (DefDatabase<ThingCategoryDef>.GetNamedSilentFail(r.SourceId)?.LabelCap.ToString() ?? r.SourceId) : GroupLabel(r.SourceId))).Distinct().ToArray();
            return summaryText = U("ParentSummary", parents.Length == 1 ? parents[0] : U("MixedParent"), custom, deeper);
        }
        private string RuleText(RuleChoice choice, bool item) => choice == RuleChoice.Inherit ? U(item ? "UseCategory" : "UseParent") : U(participation ? (choice == RuleChoice.Include ? "AllowEffects" : "BlockEffects") : (choice == RuleChoice.Include ? "GiveQuality" : "NoQuality"));
        private void SelectScopeRule()
        {
            var s = Browser; var choices = new List<FloatMenuOption>(); foreach (RuleChoice choice in Enum.GetValues(typeof(RuleChoice)))
            { if (s.Group.Length > 0 && choice == RuleChoice.Inherit) continue; var captured = choice; choices.Add(new FloatMenuOption(RuleText(choice, false), () => { if (s.Group.Length > 0) { bool current = Rules.Groups.TryGetValue(s.Group, out var b) && b; if (current == (captured == RuleChoice.Include)) return; } else if (!RuleChanges(Rules.Categories, s.Category, captured)) return; RememberDraft(); if (s.Group.Length > 0) Rules.Groups[s.Group] = captured == RuleChoice.Include; else SetRule(Rules.Categories, s.Category, captured); DraftChanged(); })); }
            Find.WindowStack.Add(new FloatMenu(choices));
        }
        private int SelectionHidden(BrowserState s)
        {
            if (s.HiddenRevision != s.SelectionRevision || s.HiddenFilterKey != s.FilterKey) { s.Hidden = s.Selected.Count - s.Filtered.Count(e => s.Selected.Contains(e.Target.Id)); s.HiddenRevision = s.SelectionRevision; s.HiddenFilterKey = s.FilterKey; }
            return s.Hidden;
        }
        private static bool RuleChanges(Dictionary<string, RuleChoice> rules, string id, RuleChoice choice) => (rules.TryGetValue(id, out var old) ? old : RuleChoice.Inherit) != choice;
        private void ApplySelected(RuleChoice choice)
        {
            var changed = Browser.Selected.Where(id => RuleChanges(Rules.Items, id, choice)).ToArray();
            if (changed.Length == 0) { error = U("AlreadyApplied"); return; }
            RememberDraft(); foreach (string id in changed) SetRule(Rules.Items, id, choice); DraftChanged(); error = U("Applied", changed.Length);
        }
        private void ClearFilters() { var s = Browser; s.Search = s.Group = s.Category = s.Source = ""; s.ResultFilter = 0; s.OverridesOnly = s.Reviewing = false; s.ListScroll = Vector2.zero; }
        private void ReviewSelected() { var s = Browser; if (!s.Reviewing) { s.Previous = s.Filters(); ClearFilters(); s.Reviewing = true; } else { if (s.Previous != null) s.Restore(s.Previous); s.Reviewing = false; } s.ListScroll = Vector2.zero; }
        private void RefreshCounts()
        {
            if (countRevision == QualitySettingsState.Revision && countParticipation == participation) return; counts.Clear();
            foreach (var e in QualityCatalog.Entries) { bool included = Resolve(e).Enabled; foreach (string key in e.Target.Categories.Select(c => "c:" + c.Id).Concat(new[] { "g:" + e.Target.Group })) { if (!counts.TryGetValue(key, out var pair)) counts[key] = pair = new int[2]; pair[included ? 0 : 1]++; } }
            countRevision = QualitySettingsState.Revision; countParticipation = participation;
        }
        private string CountLabel(string key) { if (!counts.TryGetValue(key, out var p)) return " (0)"; return " (" + p[0] + "/" + (p[0] + p[1]) + ")"; }
        private void DrawBrowser(Rect rect)
        {
            RefreshCounts(); if (visibleCategoryNodes == null) visibleCategoryNodes = categoryNodes.Where(n => n.Ancestors.All(expanded.Contains)).ToList();
            var nodes = visibleCategoryNodes; int total = groupIds.Length + nodes.Count + 2; const float row = 34; var view = new Rect(0, 0, rect.width - 18, total * row); var s = Browser;
            Widgets.BeginScrollView(rect, ref s.TreeScroll, view); int first = Math.Max(0, (int)(s.TreeScroll.y / row)), last = Math.Min(total, first + (int)(rect.height / row) + 2);
            for (int i = first; i < last; i++)
            {
                float y = i * row; if (i == 0) { if (Widgets.ButtonText(new Rect(0, y, view.width, 30), U("AllItems"))) { s.Group = s.Category = ""; s.Reviewing = false; } continue; }
                if (i <= groupIds.Length) { string id = groupIds[i - 1]; var rr = new Rect(0, y, view.width, 30); if (s.Group == id) Widgets.DrawHighlightSelected(rr); if (Widgets.ButtonText(rr, GroupLabel(id) + CountLabel("g:" + id), drawBackground: s.Group != id)) { s.Group = id; s.Category = ""; s.Reviewing = false; s.ListScroll = Vector2.zero; } continue; }
                if (i == groupIds.Length + 1) { Widgets.Label(new Rect(0, y, view.width, 30), U("CategoryTree")); continue; }
                var node = nodes[i - groupIds.Length - 2]; var def = node.Def; float indent = Math.Min(32, (node.Depth - 1) * 8);
                if (def.childCategories?.Count > 0 && Widgets.ButtonText(new Rect(indent, y, 23, 30), expanded.Contains(def.defName) ? "-" : "+")) { if (!expanded.Add(def.defName)) expanded.Remove(def.defName); visibleCategoryNodes = null; }
                var cr = new Rect(indent + 25, y, Math.Max(35, view.width - indent - 25), 30); if (s.Category == def.defName) Widgets.DrawHighlightSelected(cr);
                if (Widgets.ButtonText(cr, def.LabelCap + CountLabel("c:" + def.defName), drawBackground: s.Category != def.defName)) { s.Category = def.defName; s.Group = ""; s.Reviewing = false; s.ListScroll = Vector2.zero; }
                TooltipHandler.TipRegion(new Rect(0, y, view.width, 30), node.Path);
            }
            Widgets.EndScrollView();
        }
        private void SelectSource() => Find.WindowStack.Add(new Window_QualitySource(value => { Browser.Source = value; Browser.ListScroll = Vector2.zero; }));
        private RuleResolution Resolve(QualityCatalogEntry e) { var t = e.Target; return QualityRules.Resolve(participation ? new RuleTarget(t.Id, t.Group, t.Supported || e.NativeQuality, t.Categories) : t, Rules); }
        private void Refresh()
        {
            var s = Browser; string key = QualityCatalog.Revision + "|" + QualitySettingsState.Revision + "|" + s.Search + "|" + s.Group + "|" + s.Category + "|" + s.Source + "|" + s.ResultFilter + "|" + s.OverridesOnly + "|" + s.Reviewing + "|" + (s.Reviewing ? s.SelectionRevision : 0);
            if (key == s.FilterKey) return; s.FilterKey = key; var entries = new List<QualityCatalogEntry>(QualityCatalog.Entries); var known = new HashSet<string>(entries.Select(e => e.Target.Id));
            foreach (string id in Rules.Items.Keys.Where(id => !known.Contains(id))) entries.Add(new QualityCatalogEntry { Target = new RuleTarget(id, "", false, new CategoryPath[0]), Label = id, Source = T("Unavailable") });
            string query = s.Search.Trim(); s.Filtered = entries.Where(e => (query.Length == 0 || e.Label.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) && (s.Group.Length == 0 || e.Target.Group == s.Group) && (s.Category.Length == 0 || e.Target.Categories.Any(c => c.Id == s.Category)) && (s.Source.Length == 0 || e.Source == s.Source) && (!s.OverridesOnly || Rules.Items.ContainsKey(e.Target.Id)) && (s.ResultFilter == 0 || Resolve(e).Enabled == (s.ResultFilter == 1)) && (!s.Reviewing || s.Selected.Contains(e.Target.Id))).OrderBy(e => e.Label, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        private string ResolutionText(QualityCatalogEntry e)
        {
            var r = Resolve(e); string source = r.ReasonKind == "Group" ? GroupLabel(r.SourceId) : r.ReasonKind == "Category" ? (DefDatabase<ThingCategoryDef>.GetNamedSilentFail(r.SourceId)?.LabelCap.ToString() ?? r.SourceId) : T(r.ReasonKind);
            string status = U(participation ? (r.Enabled ? "EffectsAllowed" : "EffectsBlocked") : (r.Enabled ? "QualityYes" : "QualityNo")); if (!participation && e.NativeQuality && !r.Enabled) status = U("NativeKept"); return status + " / " + source;
        }
        private void DrawItems(Rect rect)
        {
            var s = Browser; const float row = 48; if (s.Filtered.Count == 0) { Widgets.Label(rect, U("NoResults", ScopeName)); return; }
            var view = new Rect(0, 0, rect.width - 18, s.Filtered.Count * row); Widgets.BeginScrollView(rect, ref s.ListScroll, view);
            int first = Math.Max(0, (int)(s.ListScroll.y / row)), last = Math.Min(s.Filtered.Count, first + (int)(rect.height / row) + 2);
            for (int i = first; i < last; i++)
            {
                var e = s.Filtered[i]; float y = i * row; bool check = s.Selected.Contains(e.Target.Id), before = check;
                if (check) Widgets.DrawHighlightSelected(new Rect(0, y, view.width, row)); Widgets.CheckboxLabeled(new Rect(0, y + 9, 28, 28), "", ref check);
                if (check != before) { s.SelectionRevision++; if (check) s.Selected.Add(e.Target.Id); else s.Selected.Remove(e.Target.Id); if (s.Reviewing && s.Selected.Count == 0) ReviewSelected(); }
                if (Widgets.ButtonText(new Rect(34, y + 2, view.width * .43f - 34, row - 4), e.Label)) Find.WindowStack.Add(new Window_QualityItem(this, e));
                Widgets.Label(new Rect(view.width * .45f, y + 3, view.width * .55f, row - 4), ResolutionText(e)); TooltipHandler.TipRegion(new Rect(30, y, view.width - 30, row), e.Target.Id + "\n" + e.Source + "\n" + (e.NativeQuality ? T("NativeProtected") : ""));
            }
            Widgets.EndScrollView();
        }
        private static void SetRule(Dictionary<string, RuleChoice> rules, string id, RuleChoice choice) { if (choice == RuleChoice.Inherit) rules.Remove(id); else rules[id] = choice; }
        private void FloatField(Rect rect, string key, float value, Action<float> set)
        {
            if (!buffers.TryGetValue(key, out string text)) text = value.ToString("R", CultureInfo.InvariantCulture);
            string edited = Widgets.TextField(rect, text); buffers[key] = edited;
            bool valid = BufferIsValid(key, edited, out float number);
            if (!valid)
            {
                var old = GUI.color; var font = Text.Font; GUI.color = new Color(1f, .65f, .6f); Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(rect.x, rect.yMax + 1, rect.width, 18), U("FieldRange", RangeHint(key))); GUI.color = old; Text.Font = font;
                TooltipHandler.TipRegion(rect, U("FieldRange", RangeHint(key)) + " " + U("LastValid"));
            }
            if (edited == text) return;
            if (valid && number != value) { RememberDraft(); set(number); DraftChanged(); }
        }
        private static string RangeHint(string key) => QualityNumericRange.ForInput(key).Hint;
        private static bool BufferIsValid(string key, string text, out float number) => QualityNumericRange.ForInput(key).TryParse(text, out number);
        private sealed class Window_QualityItem : Window
        {
            private readonly Window_QualityOptions owner; private readonly QualityCatalogEntry entry;
            public override Vector2 InitialSize => new Vector2(760, 470);
            public Window_QualityItem(Window_QualityOptions owner, QualityCatalogEntry entry) { this.owner = owner; this.entry = entry; doCloseX = true; forcePause = true; absorbInputAroundWindow = true; }
            public override void DoWindowContents(Rect rect)
            {
                Widgets.Label(new Rect(0, 0, rect.width, 34), entry.Label + " / " + entry.Source); Widgets.Label(new Rect(0, 42, rect.width, 58), owner.ResolutionText(entry)); float w = (rect.width - 16) / 3;
                foreach (RuleChoice choice in Enum.GetValues(typeof(RuleChoice))) { var captured = choice; if (Widgets.ButtonText(new Rect((int)choice * (w + 8), 105, w, 32), owner.RuleText(choice, true))) { if (RuleChanges(owner.Rules.Items, entry.Target.Id, captured)) { owner.RememberDraft(); SetRule(owner.Rules.Items, entry.Target.Id, captured); owner.DraftChanged(); } } }
                Widgets.Label(new Rect(0, 147, rect.width, 54), owner.participation ? T("NeedsQuality") : T("NativeProtected"));
                if (owner.participation && Widgets.ButtonText(new Rect(0, 205, rect.width, 30), U("SupportInfo"))) Find.WindowStack.Add(new Window_QualitySupport(entry));

                if (Widgets.ButtonText(new Rect(rect.width - 120, rect.height - 34, 120, 32), U("Done"))) Close();
            }
        }
        private sealed class Window_QualitySource : Window
        {
            private string search = ""; private Vector2 scroll; private readonly Action<string> choose; private readonly string[] sources;
            public override Vector2 InitialSize => new Vector2(620, 600);
            public Window_QualitySource(Action<string> choose) { this.choose = choose; sources = QualityCatalog.Entries.Select(e => e.Source).Distinct().OrderBy(s => s).ToArray(); doCloseX = true; forcePause = true; absorbInputAroundWindow = true; }
            public override void DoWindowContents(Rect rect)
            {
                Widgets.Label(new Rect(0, 0, rect.width, 30), U("FindMod")); search = Widgets.TextField(new Rect(0, 34, rect.width, 30), search); if (Widgets.ButtonText(new Rect(0, 72, rect.width, 30), U("AllMods"))) { choose(""); Close(); }
                var matches = sources.Where(s => s.IndexOf(search.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0).ToArray(); var view = new Rect(0, 0, rect.width - 18, matches.Length * 36); Widgets.BeginScrollView(new Rect(0, 110, rect.width, rect.height - 114), ref scroll, view); for (int i = 0; i < matches.Length; i++) if (Widgets.ButtonText(new Rect(0, i * 36, view.width, 32), matches[i])) { choose(matches[i]); Close(); }
                Widgets.EndScrollView();
            }
        }
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
