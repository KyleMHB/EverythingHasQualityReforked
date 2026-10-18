using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    internal sealed class Window_QualityOptions : Window
    {
        private sealed class CategoryNode
        {
            public ThingCategoryDef Def;
            public string Path;
            public int Depth;
            public string[] Ancestors;
        }

        private int page, resultFilter;
        private bool effects, participation, overridesOnly, closing;
        private string search = "", group = "", category = "", source = "", selected = "", filterKey, error = "";
        private Vector2 browserScroll, listScroll, productionScroll, effectScroll, compatibilityScroll;
        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        private readonly HashSet<string> expanded = new HashSet<string>();
        private readonly List<CategoryNode> categoryNodes;
        private List<CategoryNode> visibleCategoryNodes;
        private readonly string[] groupIds = QualitySettingsState.GroupFields.Keys.ToArray();
        private int compatibilityRevision = -1;
        private List<string> compatibilityLines;
        private readonly Dictionary<string, int[]> counts = new Dictionary<string, int[]>();
        private int countRevision = -1;
        private bool countParticipation;
        private List<QualityCatalogEntry> filtered = new List<QualityCatalogEntry>();
        private QualityConfiguration Draft => QualitySettingsState.Draft;
        private RuleSet Rules => participation ? Draft.Participation : Draft.Assignment;
        public override Vector2 InitialSize => new Vector2(1120, 820);
        private static string T(string key) => ("QEverything.Options." + key).Translate().ToString();
        private static string GroupLabel(string id) => T("Group." + id);
        private static string ChoiceLabel(RuleChoice choice) => T(choice.ToString());

        public Window_QualityOptions()
        {
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            forcePause = true;
            QualityCatalog.Build();
            categoryNodes = DefDatabase<ThingCategoryDef>.AllDefsListForReading.Select(BuildNode)
                .OrderBy(n => n.Path, StringComparer.CurrentCultureIgnoreCase).ToList();
            foreach (var node in categoryNodes.Where(n => n.Def.parent == null)) expanded.Add(node.Def.defName);
        }

        private static CategoryNode BuildNode(ThingCategoryDef def)
        {
            var chain = new List<ThingCategoryDef>();
            var seen = new HashSet<ThingCategoryDef>();
            for (var current = def; current != null && seen.Add(current); current = current.parent) chain.Add(current);
            chain.Reverse();
            return new CategoryNode
            {
                Def = def,
                Depth = chain.Count,
                Path = string.Join(" / ", chain.Select(c => c.LabelCap.ToString())),
                Ancestors = chain.Where(c => c != def).Select(c => c.defName).ToArray()
            };
        }

        public override bool OnCloseRequest()
        {
            if (!closing && (QualitySettingsState.Dirty || buffers.Any(p => !ValidBuffer(p.Value))))
            {
                if (!Find.WindowStack.IsOpen<Window_QualityClose>()) Find.WindowStack.Add(new Window_QualityClose(this));
                return false;
            }
            return true;
        }

        internal void Finish(bool save)
        {
            if (save && !Save()) return;
            if (!save) QualitySettingsState.Discard();
            closing = true;
            Close();
        }

        private bool Save()
        {
            if (buffers.Any(p => !ValidBuffer(p.Value)))
            {
                error = T("InvalidInput");
                return false;
            }
            var errors = QualitySettingsState.Save();
            error = errors.Count == 0 ? "" : T("InvalidInput") + " " + string.Join(", ", errors);
            return errors.Count == 0;
        }

        public override void DoWindowContents(Rect rect)
        {
            var oldFont = Text.Font;
            var oldAnchor = Text.Anchor;
            var oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                float tabWidth = (rect.width - 16) / 3;
                for (int i = 0; i < 3; i++)
                    if (Widgets.ButtonText(new Rect(rect.x + i * (tabWidth + 8), rect.y, tabWidth, 32), T(new[] { "Rules", "Production", "Compatibility" }[i]))) page = i;
                string state = (QualitySettingsState.Dirty ? T("DraftPending") : T("DraftSaved")) + "  "
                    + (QualitySettingsState.RestartPending ? T("RestartPending") : T("ActiveSaved"));
                Widgets.Label(new Rect(rect.x, rect.y + 38, rect.width, 42), state);
                var body = new Rect(rect.x, rect.y + 85, rect.width, Math.Max(100, rect.height - 170));
                if (page == 0) { if (effects) DrawEffects(body); else DrawRules(body); }
                else if (page == 1) DrawProduction(body);
                else DrawCompatibility(body);
                Widgets.Label(new Rect(rect.x, rect.yMax - 78, rect.width, 38), string.IsNullOrEmpty(error) ? T("RemovalWarning") : error);
                float width = (rect.width - 24) / 4;
                if (Widgets.ButtonText(new Rect(rect.x, rect.yMax - 34, width, 32), T("Save"))) Save();
                if (Widgets.ButtonText(new Rect(rect.x + width + 8, rect.yMax - 34, width, 32), T("Discard")))
                {
                    QualitySettingsState.Discard(); buffers.Clear(); error = "";
                }
                if (Widgets.ButtonText(new Rect(rect.x + 2 * (width + 8), rect.yMax - 34, width, 32), T("ResetAll")))
                    Confirm(T("ResetConfirm"), ResetEverything);
                if (Widgets.ButtonText(new Rect(rect.x + 3 * (width + 8), rect.yMax - 34, width, 32), T("Close"))) Close();
            }
            finally { Text.Font = oldFont; Text.Anchor = oldAnchor; GUI.color = oldColor; }
        }

        private static void Confirm(string message, Action action) => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(message, action));
        private void ResetEverything()
        {
            Mod_SettingsUtility.RestoreDefaults();
            buffers.Clear();
        }

        private void DrawRules(Rect rect)
        {
            float left = Math.Min(300, rect.width * .32f), right = rect.width - left - 12;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, left, 30), participation ? T("Participation") : T("Assignment"))) participation = !participation;
            if (Widgets.ButtonText(new Rect(rect.x + left + 12, rect.y, right, 30), T("Effects"))) { effects = true; return; }
            DrawBrowser(new Rect(rect.x, rect.y + 38, left, rect.height - 38));
            float x = rect.x + left + 12, y = rect.y + 38;
            string scope = category.Length > 0 ? (DefDatabase<ThingCategoryDef>.GetNamedSilentFail(category)?.LabelCap.ToString() ?? category)
                : group.Length > 0 ? GroupLabel(group) : T("AllGroups");
            Widgets.Label(new Rect(x, y, right, 25), scope); y += 27;
            string next = Widgets.TextField(new Rect(x, y, right, 30), search);
            if (next != search) { search = next; listScroll = Vector2.zero; }
            TooltipHandler.TipRegion(new Rect(x, y, right, 30), T("SearchHelp")); y += 36;
            float third = (right - 12) / 3;
            if (Widgets.ButtonText(new Rect(x, y, third, 28), source.Length == 0 ? T("AllSources") : source)) SelectSource();
            if (Widgets.ButtonText(new Rect(x + third + 6, y, third, 28), T(new[] { "AllResults", "Included", "Excluded" }[resultFilter]))) resultFilter = (resultFilter + 1) % 3;
            Widgets.CheckboxLabeled(new Rect(x + 2 * (third + 6), y, third, 28), T("Overrides"), ref overridesOnly); y += 34;
            Refresh();
            for (int i = 0; i < 3; i++)
            {
                RuleChoice choice = (RuleChoice)new[] { 1, 2, 0 }[i];
                if (Widgets.ButtonText(new Rect(x + i * (third + 6), y, third, 36), T(new[] { "BulkInclude", "BulkExclude", "BulkClear" }[i]) + " (" + filtered.Count + ")"))
                {
                    foreach (string id in filtered.Select(e => e.Target.Id).ToArray()) SetRule(Rules.Items, id, choice);
                    QualitySettingsState.Changed();
                }
            }
            y += 42;
            float detailsHeight = Math.Min(205, rect.height * .4f);
            DrawItems(new Rect(x, y, right, Math.Max(40, rect.yMax - y - detailsHeight - 6)));
            DrawDetails(new Rect(x, rect.yMax - detailsHeight, right, detailsHeight));
        }

        private void RefreshCounts()
        {
            if (countRevision == QualitySettingsState.Revision && countParticipation == participation) return;
            counts.Clear();
            foreach (var entry in QualityCatalog.Entries)
            {
                bool included = Resolve(entry).Enabled;
                foreach (string key in entry.Target.Categories.Select(c => "c:" + c.Id).Concat(new[] { "g:" + entry.Target.Group }))
                {
                    if (!counts.TryGetValue(key, out var pair)) counts[key] = pair = new int[2];
                    pair[included ? 0 : 1]++;
                }
            }
            countRevision = QualitySettingsState.Revision; countParticipation = participation;
        }

        private string CountLabel(string key)
        {
            if (!counts.TryGetValue(key, out var pair)) return " (0)";
            return " (" + pair[0] + "/" + (pair[0] + pair[1]) + (pair[0] > 0 && pair[1] > 0 ? " " + T("Mixed") : "") + ")";
        }

        private void DrawBrowser(Rect rect)
        {
            RefreshCounts();
            if (visibleCategoryNodes == null) visibleCategoryNodes = categoryNodes.Where(n => n.Ancestors.All(expanded.Contains)).ToList();
            var categories = visibleCategoryNodes;
            var groups = groupIds;
            int total = groups.Length + categories.Count + 2;
            const float row = 36;
            var view = new Rect(0, 0, rect.width - 18, total * row);
            Widgets.BeginScrollView(rect, ref browserScroll, view);
            int first = Math.Max(0, (int)(browserScroll.y / row)), last = Math.Min(total, first + (int)(rect.height / row) + 2);
            for (int index = first; index < last; index++)
            {
                float y = index * row;
                if (index == 0)
                {
                    if (Widgets.ButtonText(new Rect(0, y, view.width, 30), T("AllGroups"))) { group = ""; category = ""; }
                    continue;
                }
                if (index <= groups.Length)
                {
                    string id = groups[index - 1];
                    if (Widgets.ButtonText(new Rect(0, y, view.width - 32, 30), GroupLabel(id) + CountLabel("g:" + id))) { group = id; category = ""; listScroll = Vector2.zero; }
                    bool enabled = Rules.Groups.TryGetValue(id, out bool b) && b, before = enabled;
                    Widgets.CheckboxLabeled(new Rect(view.width - 28, y, 28, 30), "", ref enabled);
                    if (before != enabled) { Rules.Groups[id] = enabled; QualitySettingsState.Changed(); }
                    continue;
                }
                if (index == groups.Length + 1)
                {
                    if (Widgets.ButtonText(new Rect(0, y, view.width, 30), T("ResetScope"))) Confirm(T("ResetScopeConfirm"), ResetScope);
                    continue;
                }
                var node = categories[index - groups.Length - 2]; var def = node.Def;
                float indent = Math.Min(30, (node.Depth - 1) * 8);
                if (def.childCategories?.Count > 0 && Widgets.ButtonText(new Rect(indent, y, 23, 30), expanded.Contains(def.defName) ? "-" : "+"))
                {
                    if (!expanded.Add(def.defName)) expanded.Remove(def.defName);
                    visibleCategoryNodes = null;
                }
                if (Widgets.ButtonText(new Rect(indent + 25, y, Math.Max(35, view.width - indent - 94), 30), def.LabelCap + CountLabel("c:" + def.defName))) { category = def.defName; group = ""; listScroll = Vector2.zero; }
                RuleChoice choice = Rules.Categories.TryGetValue(def.defName, out var rule) ? rule : RuleChoice.Inherit;
                if (Widgets.ButtonText(new Rect(view.width - 65, y, 65, 30), ChoiceLabel(choice))) { SetRule(Rules.Categories, def.defName, (RuleChoice)(((int)choice + 1) % 3)); QualitySettingsState.Changed(); }
                TooltipHandler.TipRegion(new Rect(0, y, view.width, 30), node.Path + " (" + def.defName + ")" + CountLabel("c:" + def.defName));
            }
            Widgets.EndScrollView();
        }

        private void SelectSource()
        {
            var choices = new List<FloatMenuOption> { new FloatMenuOption(T("AllSources"), () => source = "") };
            choices.AddRange(QualityCatalog.Entries.Select(e => e.Source).Distinct().OrderBy(s => s).Select(s => new FloatMenuOption(s, () => source = s)));
            Find.WindowStack.Add(new FloatMenu(choices));
        }

        private RuleResolution Resolve(QualityCatalogEntry entry)
        {
            var target = entry.Target;
            return QualityRules.Resolve(participation ? new RuleTarget(target.Id, target.Group, target.Supported || entry.NativeQuality, target.Categories) : target, Rules);
        }

        private void Refresh()
        {
            string key = QualityCatalog.Revision + "|" + QualitySettingsState.Revision + "|" + search + "|" + group + "|" + category + "|" + source + "|" + resultFilter + "|" + overridesOnly + "|" + participation;
            if (key == filterKey) return;
            filterKey = key;
            var entries = new List<QualityCatalogEntry>(QualityCatalog.Entries);
            var knownIds = new HashSet<string>(entries.Select(e => e.Target.Id));
            foreach (string id in Rules.Items.Keys.Where(id => !knownIds.Contains(id)))
                entries.Add(new QualityCatalogEntry { Target = new RuleTarget(id, "", false, new CategoryPath[0]), Label = id, Source = T("Unavailable"), Search = id.ToLowerInvariant() });
            string query = search.Trim().ToLowerInvariant();
            filtered = entries.Where(e => (query.Length == 0 || e.Search.Contains(query))
                && (group.Length == 0 || e.Target.Group == group)
                && (category.Length == 0 || e.Target.Categories.Any(c => c.Id == category))
                && (source.Length == 0 || e.Source == source)
                && (!overridesOnly || Rules.Items.ContainsKey(e.Target.Id))
                && (resultFilter == 0 || Resolve(e).Enabled == (resultFilter == 1))).ToList();
        }

        private void DrawItems(Rect rect)
        {
            const float row = 34;
            var view = new Rect(0, 0, rect.width - 18, filtered.Count * row);
            Widgets.BeginScrollView(rect, ref listScroll, view);
            int first = Math.Max(0, (int)(listScroll.y / row)), last = Math.Min(filtered.Count, first + (int)(rect.height / row) + 2);
            for (int i = first; i < last; i++)
            {
                var entry = filtered[i]; var resolution = Resolve(entry); float y = i * row;
                if (Widgets.ButtonText(new Rect(0, y, view.width * .62f, row - 2), entry.Label)) selected = entry.Target.Id;
                Widgets.Label(new Rect(view.width * .63f, y, view.width * .37f, row), T(resolution.Enabled ? "Included" : "Excluded") + " / " + T(resolution.ReasonKind));
                TooltipHandler.TipRegion(new Rect(0, y, view.width, row), entry.Target.Id + "\n" + entry.Source + "\n" + resolution.SourceId);
            }
            Widgets.EndScrollView();
        }

        private void DrawDetails(Rect rect)
        {
            var entry = filtered.FirstOrDefault(e => e.Target.Id == selected);
            if (entry == null) { Widgets.Label(rect, T("SelectItem")); return; }
            var resolution = Resolve(entry); string reason = resolution.SourceId;
            if (resolution.ReasonKind == "Group") reason = GroupLabel(reason);
            if (resolution.ReasonKind == "Category") reason = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(reason)?.LabelCap.ToString() ?? reason;
            string info = entry.Label + " / " + entry.Source + "\n" + T(resolution.ReasonKind) + ": " + reason
                + (resolution.Conflict ? " / " + T("Conflict") : "") + (entry.NativeQuality ? "\n" + T("NativeProtected") : "");
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 90), info);
            float width = (rect.width - 12) / 3;
            for (int i = 0; i < 3; i++)
            {
                var choice = (RuleChoice)i;
                if (Widgets.ButtonText(new Rect(rect.x + i * (width + 6), rect.y + 94, width, 30), ChoiceLabel(choice))) { SetRule(Rules.Items, entry.Target.Id, choice); QualitySettingsState.Changed(); }
            }
            if (Widgets.ButtonText(new Rect(rect.x, rect.y + 130, rect.width, 28), T("EffectSupport")))
                Find.WindowStack.Add(new Dialog_MessageBox(string.Join("\n", Enum.GetValues(typeof(EffectId)).Cast<EffectId>()
                    .Select(id => T("Effect." + id) + ": " + (QualityEffects.UnsupportedReason(id, entry.Def) ?? QualityEffects.Reason(id))))));
            if (participation && !entry.NativeQuality && !QualityCatalog.Assigned(entry.Def)) Widgets.Label(new Rect(rect.x, rect.y + 160, rect.width, 40), T("NeedsQuality"));
        }

        private static void SetRule(Dictionary<string, RuleChoice> rules, string id, RuleChoice choice)
        {
            if (choice == RuleChoice.Inherit) rules.Remove(id); else rules[id] = choice;
        }

        private void ResetScope()
        {
            foreach (var entry in filtered) Rules.Items.Remove(entry.Target.Id);
            if (category.Length > 0) Rules.Categories.Remove(category);
            if (group.Length > 0)
            {
                var defaults = new QualityConfiguration();
                Rules.Groups[group] = (participation ? defaults.Participation : defaults.Assignment).Groups[group];
            }
            QualitySettingsState.Changed();
        }

        private void DrawEffects(Rect rect)
        {
            float third = (rect.width - 12) / 3;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, third, 30), T("Rules"))) { effects = false; return; }
            if (Widgets.ButtonText(new Rect(rect.x + third + 6, rect.y, third, 30), T("MildPreset"))) PreviewPreset();
            if (Widgets.ButtonText(new Rect(rect.x + 2 * (third + 6), rect.y, third, 30), T("ProgressionPreset"))) PreviewProgression();
            rect.y += 38; rect.height -= 38;
            var ids = (EffectId[])Enum.GetValues(typeof(EffectId));
            var view = new Rect(0, 0, rect.width - 18, ids.Length * 132 + 95);
            Widgets.BeginScrollView(rect, ref effectScroll, view);
            float y = 0;
            foreach (var id in ids)
            {
                var effect = Draft.Effects[id.ToString()]; bool before = effect.Enabled;
                Widgets.CheckboxLabeled(new Rect(0, y, view.width, 28), T("Effect." + id), ref effect.Enabled);
                if (before != effect.Enabled) QualitySettingsState.Changed();
                Widgets.Label(new Rect(0, y + 29, view.width, 27), (QualityEffectCatalog.IsCost(id) ? T("CostCurve") : T("BenefitCurve")) + " / " + QualityEffects.Reason(id));
                float width = view.width / 7;
                for (int tier = 0; tier < 7; tier++)
                {
                    int index = tier;
                    Widgets.Label(new Rect(width * tier, y + 58, width, 25), ((QualityCategory)tier).GetLabel());
                    FloatField(new Rect(width * tier, y + 85, width - 6, 28), "effect:" + id + ":" + tier, effect.Factors[tier], v => effect.Factors[index] = v);
                }
                y += 132;
            }
            Widgets.Label(new Rect(0, y, view.width * .7f, 28), T("LinkedBonusCap"));
            FloatField(new Rect(view.width * .72f, y, view.width * .25f, 28), "linkedBonus", Draft.LinkedFurnitureBonusCap, v => Draft.LinkedFurnitureBonusCap = v); y += 36;
            Widgets.Label(new Rect(0, y, view.width * .7f, 28), T("LinkedPenaltyCap"));
            FloatField(new Rect(view.width * .72f, y, view.width * .25f, 28), "linkedPenalty", Draft.LinkedFurniturePenaltyCap, v => Draft.LinkedFurniturePenaltyCap = v);
            Widgets.EndScrollView();
        }

        private void PreviewProgression()
        {
            string preview = T("ProgressionPreview") + "\n" + QualityProgression.HygieneStatus.Translate() + "\n" +
                string.Join("\n", QualityProgression.Exclusions.Select(d => d.LabelCap + " [" + d.defName + "]")) + "\n" +
                string.Join("\n", QualityNativeOutput.Status) + "\n" +
                string.Join("\n", Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Where(QualityEffects.Available)
                    .Select(id => T("Effect." + id) + ": " + string.Join(" / ", QualityEffectCatalog.Curve(id).Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)))));
            Confirm(preview, () =>
            {
                QualityProgression.Apply(Draft);
                foreach (string key in buffers.Keys.Where(k => k.StartsWith("effect:")).ToArray()) buffers.Remove(key);
                QualitySettingsState.Changed();
            });
        }

        private void PreviewPreset()
        {
            string preview = T("PresetPreview") + "\n" + string.Join("\n", Enum.GetValues(typeof(EffectId)).Cast<EffectId>()
                .Select(id => T("Effect." + id) + ": " + string.Join(" / ", QualityEffectCatalog.Curve(id).Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)))));
            Confirm(preview, () =>
            {
                foreach (EffectId id in Enum.GetValues(typeof(EffectId))) Draft.Effects[id.ToString()] = new EffectSettings { Enabled = true, Factors = QualityEffectCatalog.Curve(id) };
                foreach (string key in buffers.Keys.Where(k => k.StartsWith("effect:")).ToArray()) buffers.Remove(key);
                QualitySettingsState.Changed();
            });
        }

        private void DrawProduction(Rect rect)
        {
            var fields = typeof(ProductionSettings).GetFields().Where(f => !QualitySettingsState.GroupFields.Values.Contains(f.Name)).ToArray();
            var view = new Rect(0, 0, rect.width - 18, fields.Length * 38);
            Widgets.BeginScrollView(rect, ref productionScroll, view);
            float y = 0;
            foreach (var field in fields)
            {
                string label = T("Production." + field.Name); object value = field.GetValue(Draft.Production);
                if (value is bool boolean)
                {
                    bool before = boolean;
                    Widgets.CheckboxLabeled(new Rect(0, y, view.width, 30), label, ref boolean);
                    if (before != boolean) { field.SetValue(Draft.Production, boolean); QualitySettingsState.Changed(); }
                }
                else
                {
                    Widgets.Label(new Rect(0, y, view.width * .62f, 30), label);
                    if (value is int tier)
                    {
                        if (Widgets.ButtonText(new Rect(view.width * .64f, y, view.width * .36f, 30), ((QualityCategory)tier).GetLabel()))
                            Find.WindowStack.Add(new FloatMenu(Enumerable.Range(0, 7).Select(q => new FloatMenuOption(((QualityCategory)q).GetLabel(), () => { field.SetValue(Draft.Production, q); QualitySettingsState.Changed(); })).ToList()));
                    }
                    else FloatField(new Rect(view.width * .64f, y, view.width * .36f, 30), "production:" + field.Name, (float)value, v => field.SetValue(Draft.Production, v));
                }
                y += 38;
            }
            Widgets.EndScrollView();
        }

        private void FloatField(Rect rect, string key, float value, Action<float> set)
        {
            if (!buffers.TryGetValue(key, out string text)) text = value.ToString("R", CultureInfo.InvariantCulture);
            string edited = Widgets.TextField(rect, text); buffers[key] = edited;
            if (edited == text) return;
            if (float.TryParse(edited, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)) set(number);
            QualitySettingsState.Changed();
        }
        private static bool ValidBuffer(string value) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) && !float.IsNaN(number) && !float.IsInfinity(number);

        private void DrawCompatibility(Rect rect)
        {
            if (compatibilityLines == null || compatibilityRevision != QualitySettingsState.Revision)
            {
                var lines = new List<string> { T("CompatibilityIntro"), T("StorageOmitted"), T("BionicsDeferred"), T("ProgressionStatus"), QualityProgression.HygieneStatus.Translate().ToString(), T("PatchStatusHelp"), T("ImproveThisUnavailable") };
                lines.AddRange(QualityNativeOutput.Status);
                lines.AddRange(QualityPatchUtility.Statuses.OrderBy(p => p.Key).Select(p => p.Key + ": " + T(p.Value ? "PatchVerified" : "NativeFallback")));
                lines.AddRange(Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Select(id => T("Effect." + id) + ": " + (QualityEffects.Available(id) ? T("Available") : T("Unavailable")) + " / " + QualityEffects.Reason(id)));
                lines.Add(T("InactiveLegacy") + ": " + Draft.InactiveLegacy.Count);
                lines.AddRange(Draft.InactiveLegacy.OrderBy(p => p.Key).Select(p => p.Key + ": " + ChoiceLabel(p.Value ? RuleChoice.Include : RuleChoice.Exclude)));
                compatibilityLines = lines;
                compatibilityRevision = QualitySettingsState.Revision;
            }
            var linesToDraw = compatibilityLines;
            var view = new Rect(0, 0, rect.width - 18, linesToDraw.Count * 46 + 40);
            Widgets.BeginScrollView(rect, ref compatibilityScroll, view);
            int first = Math.Max(0, (int)(compatibilityScroll.y / 46)), last = Math.Min(linesToDraw.Count, first + (int)(rect.height / 46) + 2);
            for (int i = first; i < last; i++) Widgets.Label(new Rect(0, i * 46, view.width, 44), linesToDraw[i]);
            if (Draft.InactiveLegacy.Count > 0 && Widgets.ButtonText(new Rect(0, linesToDraw.Count * 46, view.width, 32), T("RecoverLegacy")))
                Confirm(T("RecoverConfirm"), () =>
                {
                    foreach (var item in Draft.InactiveLegacy)
                    {
                        string id = item.Key.Substring(item.Key.IndexOf(':') + 1);
                        Draft.Assignment.Items[id] = item.Value ? RuleChoice.Include : RuleChoice.Exclude;
                    }
                    QualitySettingsState.Changed();
                });
            Widgets.EndScrollView();
        }
    }

    internal sealed class Window_QualityClose : Window
    {
        private readonly Window_QualityOptions owner;
        public override Vector2 InitialSize => new Vector2(600, 200);
        public Window_QualityClose(Window_QualityOptions owner)
        {
            this.owner = owner;
            forcePause = true; absorbInputAroundWindow = true; doCloseX = true;
        }
        public override void DoWindowContents(Rect rect)
        {
            Widgets.Label(new Rect(0, 0, rect.width, 70), "QEverything.Options.ClosePrompt".Translate());
            float width = (rect.width - 16) / 3;
            if (Widgets.ButtonText(new Rect(0, 85, width, 40), "QEverything.Options.Save".Translate())) { Close(); owner.Finish(true); }
            if (Widgets.ButtonText(new Rect(width + 8, 85, width, 40), "QEverything.Options.Discard".Translate())) { Close(); owner.Finish(false); }
            if (Widgets.ButtonText(new Rect(2 * (width + 8), 85, width, 40), "QEverything.Options.Continue".Translate())) Close();
        }
    }
}
