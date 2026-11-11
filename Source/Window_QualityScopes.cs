using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
{
    internal sealed partial class Window_QualityOptions
    {
        private static string S(string key, params object[] args) => string.Format(("QEverything.Scopes." + key).Translate().ToString(), args);
        private string ScopeRuleLabel(EditorRule rule) => W(rule == EditorRule.Assignment ? "Quality" : rule == EditorRule.Participation ? "ModEffects" : "Price");
        private RuleChoice ScopeRuleState(EditorRule rule)
        {
            var rules = QualityEditorActions.RulesFor(Draft, rule);
            return assignment.Category.Length > 0 ? rules.Categories.TryGetValue(assignment.Category, out var c) ? c : RuleChoice.Inherit
                : assignment.Group.Length > 0 ? rules.Groups.TryGetValue(assignment.Group, out bool g) ? g ? RuleChoice.Include : RuleChoice.Exclude : RuleChoice.Inherit
                : rules.Default ? RuleChoice.Include : RuleChoice.Exclude;
        }
        // One dropdown per rule replaces the old rule picker, "Set rule" button and summary sentence.
        private float DrawWorkbenchScopeRules(Rect rect)
        {
            var anchor = Text.Anchor; var color = GUI.color;
            string label = S("DefaultFor", ScopeName);
            float labelWidth = Math.Min(rect.width * .34f, Text.CalcSize(label).x + 12);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, 30), label.Truncate(labelWidth));
            Text.Anchor = anchor;
            TooltipHandler.TipRegion(new Rect(rect.x, rect.y, labelWidth, 30), S("RuleHelp"));
            var kinds = new[] { EditorRule.Assignment, EditorRule.Participation, EditorRule.Price };
            float width = (rect.width - labelWidth - 6 * kinds.Length) / kinds.Length;
            for (int i = 0; i < kinds.Length; i++)
            {
                var rule = kinds[i]; var state = ScopeRuleState(rule);
                var button = new Rect(rect.x + labelWidth + 6 + i * (width + 6), rect.y, width, 30);
                GUI.color = state == RuleChoice.Include ? OnColor : state == RuleChoice.Exclude ? OffColor : color;
                if (Widgets.ButtonText(button, (ScopeRuleLabel(rule) + ": " + S(state == RuleChoice.Inherit ? "Parent" : state == RuleChoice.Include ? "Enabled" : "Disabled")).Truncate(width - 16))) OpenWorkbenchScopeRule(rule);
                GUI.color = color;
                TooltipHandler.TipRegion(button, S("RuleHelp"));
            }
            return 36;
        }
        private void OpenWorkbenchScopeRule(EditorRule rule)
        {
            string group = assignment.Group, category = assignment.Category;
            var choices = new List<FloatMenuOption>();
            foreach (var choice in new[] { RuleChoice.Include, RuleChoice.Exclude, RuleChoice.Inherit })
            {
                if (choice == RuleChoice.Inherit && group.Length == 0 && category.Length == 0) continue;
                var selected = choice;
                choices.Add(new FloatMenuOption(S(choice == RuleChoice.Inherit ? "Parent" : choice == RuleChoice.Include ? "Enabled" : "Disabled"), () =>
                {
                    if (Editor.SetScopeRule(rule, group, category, selected)) { QualitySettingsState.Changed(); assignment.FilterKey = null; }
                }));
            }
            var targets = QualityCatalog.Entries.Select(e => e.Target).ToArray();
            int exceptions = Editor.ScopeItemExceptions(rule, group, category, targets).Length;
            if (exceptions > 0)
                choices.Add(new FloatMenuOption(S("ClearExceptions", exceptions), () => Confirm(S("ClearPreview", ScopeRuleLabel(rule), ScopeName, exceptions), () =>
                {
                    if (Editor.ClearScopeItemExceptions(rule, group, category, targets)) { QualitySettingsState.Changed(); assignment.FilterKey = null; }
                })));
            Find.WindowStack.Add(new FloatMenu(choices));
        }
        private sealed class Window_WorkbenchScope : Window
        {
            private readonly Window_QualityOptions owner;
            private string search = "", nodeKey;
            private bool categories;
            private Vector2 scroll;
            private List<CategoryNode> nodes = new List<CategoryNode>();
            private string[] groups = new string[0];
            private float[] heights = new float[0], offsets = new float[0];
            private float contentHeight;
            private readonly Dictionary<string, int> totals = new Dictionary<string, int>();
            private int catalogRevision = -1, settingsRevision = -1, allCount;
            public override Vector2 InitialSize => new Vector2(Math.Min(720, Verse.UI.screenWidth - 40), Math.Min(760, Verse.UI.screenHeight - 40));
            public Window_WorkbenchScope(Window_QualityOptions owner)
            { this.owner = owner; categories = owner.assignment.Category.Length > 0; doCloseX = true; forcePause = true; absorbInputAroundWindow = true; }
            private void Choose(string group, string category)
            {
                owner.assignment.Group = group; owner.assignment.Category = category;
                owner.assignment.FilterKey = null; owner.assignment.ListScroll = Vector2.zero;
                Close();
            }
            private int Count(string key) => totals.TryGetValue(key, out int count) ? count : 0;
            public override void DoWindowContents(Rect rect)
            {
                var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color; var enabled = GUI.enabled;
                try
                {
                    Text.Font = GameFont.Small;
                    if (catalogRevision != QualityCatalog.Revision)
                    {
                        totals.Clear();
                        foreach (var entry in QualityCatalog.Entries)
                            foreach (string countKey in entry.Target.Categories.Select(c => "c:" + c.Id).Distinct().Concat(new[] { "g:" + entry.Target.Group }))
                                totals[countKey] = Count(countKey) + 1;
                        catalogRevision = QualityCatalog.Revision; settingsRevision = -1;
                    }
                    if (settingsRevision != QualitySettingsState.Revision)
                    {
                        var known = new HashSet<string>(QualityCatalog.Entries.Select(e => e.Target.Id));
                        allCount = known.Count + owner.WorkbenchRules.SelectMany(r => r.Items.Keys).Distinct().Count(id => !known.Contains(id));
                        settingsRevision = QualitySettingsState.Revision;
                    }
                    float y = rect.y;
                    string next = Widgets.TextField(new Rect(rect.x, y, rect.width - 84, 30), search);
                    Placeholder(new Rect(rect.x, y, rect.width - 84, 30), S("SearchPlaceholder"), next);
                    if (next != search) { search = next; scroll = Vector2.zero; }
                    if (Widgets.ButtonText(new Rect(rect.xMax - 78, y, 78, 30), W("Clear"))) { search = ""; scroll = Vector2.zero; }
                    TooltipHandler.TipRegion(new Rect(rect.x, y, rect.width - 84, 30), S("SearchHelp")); y += 36;
                    float half = (rect.width - 6) / 2;
                    if (Widgets.ButtonText(new Rect(rect.x, y, half, 30), S("Groups"), drawBackground: categories)) { categories = false; scroll = Vector2.zero; }
                    if (Widgets.ButtonText(new Rect(rect.x + half + 6, y, half, 30), S("Categories"), drawBackground: !categories)) { categories = true; scroll = Vector2.zero; } y += 36;
                    if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, 30), U("AllItems") + " (" + allCount + ")")) Choose("", ""); y += 36;
                    string query = search.Trim();
                    float width = rect.width - 18;
                    string key = categories + "|" + query + "|" + width + "|" + catalogRevision + "|" + string.Join("|", owner.expanded.OrderBy(v => v));
                    if (key != nodeKey)
                    {
                        nodeKey = key;
                        nodes = owner.categoryNodes.Where(n => query.Length > 0 ? Matches(n.Path, query) || Matches(n.Def.defName, query) : n.Ancestors.All(owner.expanded.Contains)).ToList();
                        groups = owner.groupIds.Where(id => query.Length == 0 || Matches(GroupLabel(id), query) || Matches(id, query)).ToArray();
                        heights = categories ? nodes.Select(n => Math.Max(34, Text.CalcHeight(n.Path + " (" + Count("c:" + n.Def.defName) + ")", Math.Max(50, width - 50)) + 12)).ToArray()
                            : groups.Select(id => Math.Max(34, Text.CalcHeight(GroupLabel(id) + " (" + Count("g:" + id) + ")", width - 12) + 12)).ToArray();
                        offsets = new float[heights.Length]; contentHeight = 0;
                        for (int i = 0; i < heights.Length; i++) { offsets[i] = contentHeight; contentHeight += heights[i]; }
                    }
                    var list = new Rect(rect.x, y, rect.width, Math.Max(30, rect.yMax - y));
                    if (heights.Length == 0) { Widgets.Label(list, S("NoMatches")); return; }
                    Widgets.BeginScrollView(list, ref scroll, new Rect(0, 0, width, contentHeight));
                    try
                    {
                        int first = Array.BinarySearch(offsets, scroll.y);
                        if (first < 0) first = Math.Max(0, ~first - 1);
                        for (int i = first; i < heights.Length && offsets[i] <= scroll.y + list.height; i++)
                        {
                            float height = heights[i], offset = offsets[i];
                            if (offset + height >= scroll.y && offset <= scroll.y + list.height)
                            {
                                if (!categories)
                                {
                                    string id = groups[i];
                                    if (Widgets.ButtonText(new Rect(0, offset, width, height - 4), GroupLabel(id) + " (" + Count("g:" + id) + ")")) Choose(id, "");
                                }
                                else
                                {
                                    var node = nodes[i]; string id = node.Def.defName;
                                    if (node.Def.childCategories?.Count > 0 && Widgets.ButtonText(new Rect(0, offset, 30, 30), owner.expanded.Contains(id) ? "-" : "+"))
                                    { if (!owner.expanded.Add(id)) owner.expanded.Remove(id); nodeKey = null; }
                                    if (Widgets.ButtonText(new Rect(36, offset, width - 36, height - 4), node.Path + " (" + Count("c:" + id) + ")")) Choose("", id);
                                }
                            }
                        }
                    }
                    finally { Widgets.EndScrollView(); }
                }
                finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; GUI.enabled = enabled; }
            }
        }
    }
}
