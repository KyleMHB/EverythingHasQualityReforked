using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    internal sealed partial class Window_QualityOptions
    {
        private string effectSearch = "", productionSearch = "";
        private bool showDiagnostics, showLegacy;
        private readonly HashSet<string> expandedCompatibility = new HashSet<string>();
        private static string A(string key) => ("QEverything.Advanced." + key).Translate().ToString();
        private static bool Matches(string text, string query) => string.IsNullOrWhiteSpace(query) || text.IndexOf(query.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0;
        private static string Family(EffectId id)
        {
            int n = (int)id;
            return n < 3 ? "Durability" : n < 6 ? "Work" : n < 10 ? "Power" : n < 15 ? "Consumables" : n < 20 ? "Equipment" : n < 24 ? "Combat" : "Furniture";
        }

        private void OpenPreset(bool progression, bool medium = false)
        {
            var candidate = Editor.BuiltinPreset(medium,
                progression ? QualityProgression.Exclusions.Select(d => d.defName) : null,
                progression ? (Func<EffectId, bool>)QualityEffects.Available : null);
            PreviewUserPreset(progression ? T("ProgressionPreset") : A(medium ? "Medium" : "Mild"), candidate);
        }
        private static string ProductionSection(string name)
        {
            if (name.StartsWith("inspired", StringComparison.Ordinal) || name.StartsWith("skilled", StringComparison.Ordinal)) return "Skills";
            if (name.EndsWith("SupplyFactor", StringComparison.Ordinal) || name == "multSupplyFactor") return "Supply";
            return "Ingredients";
        }
        private static readonly string[] LimitSuffixes = { "Edifice", "Work", "Security", "Stuff", "Ing", "Tasty", "Meal", "Drug", "Med", "Manuf", "Apparel", "Weapon", "Shell" };

        private void DrawProduction(Rect rect)
        {
            Widgets.Label(new Rect(rect.x, rect.y, 150, 28), A("SearchProduction"));
            string query = Widgets.TextField(new Rect(rect.x + 155, rect.y, rect.width - 155, 28), productionSearch);
            if (query != productionSearch) { productionSearch = query; productionScroll = Vector2.zero; }
            rect.y += 38; rect.height -= 38;
            var fields = typeof(ProductionSettings).GetFields().Where(f => !QualitySettingsState.GroupFields.Values.Contains(f.Name) && !f.Name.StartsWith("min", StringComparison.Ordinal) && !f.Name.StartsWith("max", StringComparison.Ordinal)).ToArray();
            var rows = fields.Where(f => Matches(A("Production." + f.Name) + " " + A(ProductionSection(f.Name)), productionSearch)).ToList();
            var limits = LimitSuffixes.Where(s => Matches(A("Limit." + s) + " " + A("Limits"), productionSearch)).ToList();
            float height = rows.Count * 58 + limits.Count * 42 + 300;
            var view = new Rect(0, 0, rect.width - 18, Math.Max(rect.height, height));
            Widgets.BeginScrollView(rect, ref productionScroll, view);
            float y = 0;
            foreach (string section in new[] { "Ingredients", "Skills", "Supply" })
            {
                var current = rows.Where(f => ProductionSection(f.Name) == section).ToList();
                if (current.Count == 0) continue;
                Widgets.Label(new Rect(0, y, view.width, 28), A(section)); y += 34;
                if (section == "Supply")
                {
                    Widgets.Label(new Rect(0, y, view.width, 44), A("SupplyHelp")); y += 48;
                }
                foreach (var field in current)
                {
                    string label = A("Production." + field.Name);
                    object value = field.GetValue(Draft.Production);
                    if (value is bool boolean)
                    {
                        bool before = boolean;
                        Widgets.CheckboxLabeled(new Rect(0, y, view.width, 30), label, ref boolean);
                        if (before != boolean) { RememberDraft(); field.SetValue(Draft.Production, boolean); DraftChanged(); }
                    }
                    else
                    {
                        Widgets.Label(new Rect(0, y, view.width * .63f, 30), label);
                        var input = new Rect(view.width * .65f, y, view.width * .35f, 30);
                        if (value is int tier) DrawTier(input, field, tier, 0, 6);
                        else
                        {
                            FloatField(input, "production:" + field.Name, (float)value, v => field.SetValue(Draft.Production, v));
                            if (field.Name == "tableFactor")
                            {
                                var slider = new Rect(0, y + 32, view.width * .61f, 22);
                                if (Event.current.type == EventType.MouseDown && slider.Contains(Event.current.mousePosition)) Editor.BeginGesture("worktable");
                                float currentShare = Draft.Production.tableFactor;
                                float next = Widgets.HorizontalSlider(slider, currentShare, 0f, 1f);
                                if (next != currentShare)
                                {
                                    Editor.Edit(c => c.Production.tableFactor = next);
                                    Editor.SynchronizeInput("production:tableFactor");
                                    QualitySettingsState.Changed();
                                }
                            }
                        }
                    }
                    y += value is float ? 58 : 42;
                }
            }
            if (limits.Count > 0)
            {
                Widgets.Label(new Rect(0, y, view.width * .49f, 28), A("Limits"));
                Widgets.Label(new Rect(view.width * .5f, y, view.width * .24f, 28), A("Minimum"));
                Widgets.Label(new Rect(view.width * .76f, y, view.width * .24f, 28), A("Maximum")); y += 34;
                foreach (string suffix in limits)
                {
                    var min = typeof(ProductionSettings).GetField("min" + suffix + "Quality");
                    var max = typeof(ProductionSettings).GetField("max" + suffix + "Quality");
                    int low = (int)min.GetValue(Draft.Production), high = (int)max.GetValue(Draft.Production);
                    Widgets.Label(new Rect(0, y, view.width * .48f, 30), A("Limit." + suffix));
                    DrawTier(new Rect(view.width * .5f, y, view.width * .23f, 30), min, low, 0, high);
                    DrawTier(new Rect(view.width * .76f, y, view.width * .24f, 30), max, high, low, 6);
                    y += 42;
                }
            }
            if (rows.Count == 0 && limits.Count == 0) Widgets.Label(new Rect(0, 0, view.width, 32), A("NoResults"));
            Widgets.EndScrollView();
        }

        private void DrawTier(Rect rect, FieldInfo field, int tier, int min, int max)
        {
            if (!Widgets.ButtonText(rect, ((QualityCategory)tier).GetLabel())) return;
            Find.WindowStack.Add(new FloatMenu(Enumerable.Range(0, 7).Select(q =>
            {
                string label = ((QualityCategory)q).GetLabel();
                return q < min || q > max ? new FloatMenuOption(label, null) : new FloatMenuOption(label, () =>
                {
                    if ((int)field.GetValue(Draft.Production) == q) return;
                    RememberDraft(); field.SetValue(Draft.Production, q); DraftChanged();
                });
            }).ToList()));
        }


    }

    internal sealed class Window_QualityPresetReview : Window
    {
        private readonly string title;
        private readonly int enabled, replaced;
        private readonly float[] benefits, costs;
        private readonly List<KeyValuePair<string, string>> sections;
        private readonly HashSet<string> expanded = new HashSet<string>();
        private readonly Action apply;
        private Vector2 scroll;
        public override Vector2 InitialSize => new Vector2(760, 650);
        public Window_QualityPresetReview(string title, int enabled, int replaced, float[] benefits, float[] costs, List<KeyValuePair<string, string>> sections, Action apply)
        { this.title = title; this.enabled = enabled; this.replaced = replaced; this.benefits = benefits; this.costs = costs; this.sections = sections; this.apply = apply; forcePause = true; absorbInputAroundWindow = true; doCloseX = true; }
        private static string A(string key) => ("QEverything.Advanced." + key).Translate().ToString();
        public override void DoWindowContents(Rect rect)
        {
            Widgets.Label(new Rect(0, 0, rect.width, 30), title);
            Widgets.Label(new Rect(0, 38, rect.width, 46), A("PresetIntro"));
            Widgets.Label(new Rect(0, 88, rect.width * .5f, 42), A("EnableCount").Replace("{0}", enabled.ToString()));
            Widgets.Label(new Rect(rect.width * .5f, 88, rect.width * .5f, 42), A("ReplaceCount").Replace("{0}", replaced.ToString()));
            float first = rect.width * .48f, column = rect.width * .17f;
            Widgets.Label(new Rect(first, 136, column, 26), ((QualityCategory)0).GetLabel());
            Widgets.Label(new Rect(first + column, 136, column, 26), ((QualityCategory)2).GetLabel());
            Widgets.Label(new Rect(first + column * 2, 136, column, 26), ((QualityCategory)6).GetLabel());
            Widgets.Label(new Rect(0, 164, first, 26), A("Benefits"));
            Widgets.Label(new Rect(0, 192, first, 26), A("Costs"));
            for (int i = 0; i < 3; i++)
            {
                int tier = i == 0 ? 0 : i == 1 ? 2 : 6;
                Widgets.Label(new Rect(first + column * i, 164, column, 26), benefits[tier].ToString("0.###", CultureInfo.InvariantCulture) + "x");
                Widgets.Label(new Rect(first + column * i, 192, column, 26), costs[tier].ToString("0.###", CultureInfo.InvariantCulture) + "x");
            }
            float width = rect.width - 18;
            float height = sections.Sum(section => 36 + (expanded.Contains(section.Key) ? Text.CalcHeight(section.Value, width - 12) + 16 : 0));
            Widgets.BeginScrollView(new Rect(0, 234, rect.width, rect.height - 286), ref scroll, new Rect(0, 0, width, height));
            float y = 0;
            foreach (var section in sections)
            {
                bool open = expanded.Contains(section.Key);
                if (Widgets.ButtonText(new Rect(0, y, width, 30), (open ? "- " : "+ ") + section.Key))
                { if (open) expanded.Remove(section.Key); else expanded.Add(section.Key); }
                y += 36;
                if (!open) continue;
                float h = Text.CalcHeight(section.Value, width - 12) + 16;
                Widgets.Label(new Rect(12, y, width - 12, h), section.Value); y += h;
            }
            Widgets.EndScrollView();
            if (Widgets.ButtonText(new Rect(0, rect.height - 40, rect.width / 2 - 5, 36), "QEverything.Advanced.GoBack".Translate())) Close();
            if (Widgets.ButtonText(new Rect(rect.width / 2 + 5, rect.height - 40, rect.width / 2 - 5, 36), "QEverything.Advanced.ApplyDraft".Translate())) { apply(); Close(); }
        }
    }
}
