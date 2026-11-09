using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
{
    internal sealed partial class Window_QualityOptions
    {
        private Vector2 limitScroll;
        private static string ProductionSection(string name)
        {
            if (name.StartsWith("inspired", StringComparison.Ordinal) || name.StartsWith("skilled", StringComparison.Ordinal)) return "Skills";
            if (name.EndsWith("SupplyFactor", StringComparison.Ordinal) || name == "multSupplyFactor") return "Supply";
            return "Ingredients";
        }
        private void DrawProduction(Rect rect)
        {
            float searchLabel = Math.Min(rect.width * .4f, Text.CalcSize(A("SearchProduction")).x + 8);
            Widgets.Label(new Rect(rect.x, rect.y, searchLabel, 30), A("SearchProduction"));
            string query = Widgets.TextField(new Rect(rect.x + searchLabel, rect.y, rect.width - searchLabel, 30), productionSearch);
            if (query != productionSearch) { productionSearch = query; productionScroll = limitScroll = Vector2.zero; }
            rect.y += 40; rect.height -= 40;
            // Legacy group flags are compatibility data; assignment is edited in Items & rules.
            var fields = typeof(ProductionSettings).GetFields().Where(f => !QualitySettingsState.GroupFields.Values.Contains(f.Name)
                && !f.Name.StartsWith("min", StringComparison.Ordinal) && !f.Name.StartsWith("max", StringComparison.Ordinal)
                && Matches(A("Production." + f.Name) + " " + A(ProductionSection(f.Name)), productionSearch)).ToArray();
            var limits = Editor.QualityLimits().Where(r => Matches(A("Limit." + r.Group) + " " + A("Limits"), productionSearch)).ToArray();
            if (rect.width >= 1040)
            {
                float rulesWidth = (rect.width - 22) * .53f;
                var rulesRect = new Rect(rect.x, rect.y, rulesWidth, rect.height);
                var limitsRect = new Rect(rulesRect.xMax + 22, rect.y, rect.width - rulesWidth - 22, rect.height);
                float width = rulesRect.width - 18;
                float height = ProductionRules(width, fields, false);
                Widgets.BeginScrollView(rulesRect, ref productionScroll, new Rect(0, 0, width, Math.Max(rulesRect.height, height)));
                ProductionRules(width, fields, true); Widgets.EndScrollView();
                width = limitsRect.width - 18;
                height = ProductionLimits(0, width, limits, false);
                Widgets.BeginScrollView(limitsRect, ref limitScroll, new Rect(0, 0, width, Math.Max(limitsRect.height, height)));
                ProductionLimits(0, width, limits, true); Widgets.EndScrollView();
            }
            else
            {
                // Narrow windows retain full-width controls and one continuous scroll.
                float width = rect.width - 18;
                float ruleHeight = ProductionRules(width, fields, false);
                float height = ProductionLimits(ruleHeight + 18, width, limits, false);
                Widgets.BeginScrollView(rect, ref productionScroll, new Rect(0, 0, width, Math.Max(rect.height, height)));
                ProductionRules(width, fields, true); ProductionLimits(ruleHeight + 18, width, limits, true); Widgets.EndScrollView();
            }
        }
        private float ProductionRules(float width, FieldInfo[] fields, bool draw)
        {
            float y = 0;
            if (fields.Length == 0)
            {
                float h = Math.Max(30, Text.CalcHeight(A("NoResults"), width));
                if (draw) Widgets.Label(new Rect(0, y, width, h), A("NoResults"));
                return h;
            }
            foreach (string section in new[] { "Ingredients", "Skills", "Supply" })
            {
                var rows = fields.Where(f => ProductionSection(f.Name) == section).ToArray();
                if (rows.Length == 0) continue;
                float heading = Math.Max(28, Text.CalcHeight(A(section), width));
                if (draw) Widgets.Label(new Rect(0, y, width, heading), A(section));
                y += heading + 8;
                if (section == "Supply")
                {
                    float help = Text.CalcHeight(A("SupplyHelp"), width);
                    if (draw) Widgets.Label(new Rect(0, y, width, help), A("SupplyHelp"));
                    y += help + 14;
                }
                foreach (var field in rows)
                {
                    string label = A("Production." + field.Name);
                    object value = field.GetValue(Draft.Production);
                    float h = Math.Max(30, Text.CalcHeight(label, width - (value is bool ? 38 : 0)));
                    if (value is bool boolean)
                    {
                        if (draw)
                        {
                            bool before = boolean;
                            Widgets.CheckboxLabeled(new Rect(0, y, width, h), label, ref boolean);
                            if (before != boolean) { RememberDraft(); field.SetValue(Draft.Production, boolean); DraftChanged(); }
                        }
                        y += h + 12;
                    }
                    else
                    {
                        if (draw) Widgets.Label(new Rect(0, y, width, h), label);
                        y += h + 4;
                        if (draw)
                        {
                            if (value is int tier)
                                ProductionTierMenu(new Rect(0, y, Math.Min(width, 240), 32), tier, 0, 6, q =>
                                { RememberDraft(); field.SetValue(Draft.Production, q); DraftChanged(); });
                            else
                            {
                                var input = new Rect(width - 140, y, 140, 30);
                                ProductionNumber(input, field.Name, (float)value);
                                if (field.Name == "tableFactor")
                                {
                                    var slider = new Rect(0, y + 4, width - 160, 24);
                                    if (Event.current.type == EventType.MouseDown && slider.Contains(Event.current.mousePosition)) Editor.BeginGesture("worktable");
                                    float current = Draft.Production.tableFactor;
                                    float next = Widgets.HorizontalSlider(slider, current, 0f, 1f);
                                    if (next != current)
                                    {
                                        GUI.FocusControl(null);
                                        Editor.SetWorktableShare(next);
                                        QualitySettingsState.Changed();
                                    }
                                }
                            }
                        }
                        y += value is float ? 58 : 44;
                    }
                }
                y += 12;
            }
            return y;
        }
        private void ProductionNumber(Rect rect, string field, float value)
        {
            string key = "production:" + field;
            string text = Editor.NumberText(key, value);
            GUI.SetNextControlName("quality-number-" + key);
            string edited = Widgets.TextField(rect, text);
            if (!BufferIsValid(key, edited, out _))
            {
                var color = GUI.color; var font = Text.Font;
                GUI.color = new Color(1f, .65f, .6f); Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(rect.x, rect.yMax + 1, rect.width, 20), U("FieldRange", RangeHint(key)));
                GUI.color = color; Text.Font = font;
                TooltipHandler.TipRegion(rect, U("FieldRange", RangeHint(key)) + " " + U("LastValid"));
            }
            if (edited != text && Editor.EditProductionNumber(field, edited)) { QualitySettingsState.Changed(); DraftChanged(); }
        }
        private float ProductionLimits(float y, float width, QualityLimit[] limits, bool draw)
        {
            float h = Math.Max(28, Text.CalcHeight(A("Limits"), width));
            if (draw) Widgets.Label(new Rect(0, y, width, h), A("Limits"));
            y += h + 8;
            h = Math.Max(32, Text.CalcHeight(ProductionText("RestoreDefaults"), width - 16) + 8);
            if (draw && Widgets.ButtonText(new Rect(0, y, width, h), ProductionText("RestoreDefaults"))) OpenQualityLimitDefaults();
            y += h + 14;
            if (limits.Length == 0)
            {
                h = Math.Max(30, Text.CalcHeight(A("NoResults"), width));
                if (draw) Widgets.Label(new Rect(0, y, width, h), A("NoResults"));
                return y + h;
            }
            foreach (var range in limits)
            {
                h = Math.Max(26, Text.CalcHeight(A("Limit." + range.Group), width));
                if (draw) Widgets.Label(new Rect(0, y, width, h), A("Limit." + range.Group));
                y += h + 4;
                if (draw)
                {
                    ProductionTierMenu(new Rect(0, y, (width - 8) / 2, 32), range.Minimum, 0, range.Maximum,
                        q => { if (Editor.SetQualityLimit(range.Group, q, Editor.QualityLimits().Single(r => r.Group == range.Group).Maximum)) QualitySettingsState.Changed(); }, A("Minimum") + ": ");
                    ProductionTierMenu(new Rect((width + 8) / 2, y, (width - 8) / 2, 32), range.Maximum, range.Minimum, 6,
                        q => { if (Editor.SetQualityLimit(range.Group, Editor.QualityLimits().Single(r => r.Group == range.Group).Minimum, q)) QualitySettingsState.Changed(); }, A("Maximum") + ": ");
                }
                y += 48;
            }
            return y;
        }
        private void ProductionTierMenu(Rect rect, int tier, int minimum, int maximum, Action<int> apply, string prefix = "")
        {
            if (!Widgets.ButtonText(rect, prefix + ((QualityCategory)tier).GetLabel())) return;
            Editor.EndGesture(); GUI.FocusControl(null);
            Find.WindowStack.Add(new FloatMenu(Enumerable.Range(0, 7).Select(q => new FloatMenuOption(((QualityCategory)q).GetLabel(),
                q < minimum || q > maximum ? (Action)null : () => apply(q))).ToList()));
        }
        private static string ProductionText(string key) => ("QEverything.ProductionUI." + key).Translate().ToString();
        private void OpenQualityLimitDefaults()
        {
            Editor.EndGesture(); GUI.FocusControl(null);
            var current = Editor.QualityLimits();
            string preview = string.Join("\n\n", Editor.QualityLimits(true).Select(r =>
            {
                var previous = current.Single(p => p.Group == r.Group);
                return A("Limit." + r.Group) + ": " + ((QualityCategory)previous.Minimum).GetLabel() + " - " + ((QualityCategory)previous.Maximum).GetLabel()
                    + "  >  " + ((QualityCategory)r.Minimum).GetLabel() + " - " + ((QualityCategory)r.Maximum).GetLabel();
            }));
            Find.WindowStack.Add(new Window_QualityLimitDefaults(preview, () =>
            {
                if (Editor.RestoreQualityLimitDefaults()) QualitySettingsState.Changed();
                GUI.FocusControl(null); DraftChanged();
            }));
        }
        private sealed class Window_QualityLimitDefaults : Window
        {
            private readonly string preview;
            private readonly Action apply;
            private Vector2 scroll;
            public override Vector2 InitialSize => new Vector2(720, 640);
            public Window_QualityLimitDefaults(string preview, Action apply)
            { this.preview = preview; this.apply = apply; doCloseX = true; closeOnCancel = true; forcePause = true; absorbInputAroundWindow = true; }
            public override void DoWindowContents(Rect rect)
            {
                float h = Text.CalcHeight(ProductionText("DefaultsHelp"), rect.width);
                Widgets.Label(new Rect(0, 0, rect.width, h), ProductionText("DefaultsHelp"));
                var area = new Rect(0, h + 14, rect.width, Math.Max(50, rect.height - h - 64));
                float width = area.width - 18;
                var view = new Rect(0, 0, width, Math.Max(area.height, Text.CalcHeight(preview, width)));
                Widgets.BeginScrollView(area, ref scroll, view); Widgets.Label(view, preview); Widgets.EndScrollView();
                if (Widgets.ButtonText(new Rect(0, rect.height - 36, (rect.width - 12) / 2, 36), A("GoBack"))) Close();
                if (Widgets.ButtonText(new Rect((rect.width + 12) / 2, rect.height - 36, (rect.width - 12) / 2, 36), A("ApplyDraft"))) { apply(); Close(); }
            }
        }
    }
}