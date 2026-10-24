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
            var ids = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().ToArray();
            int enabled = ids.Count(id => !Draft.Effects[id.ToString()].Enabled && (!progression || QualityEffects.Available(id)));
            int replaced = ids.Count(id => !Draft.Effects[id.ToString()].Factors.SequenceEqual(QualityEffectCatalog.Curve(id, medium)));
            var sections = new List<KeyValuePair<string, string>>();
            float[] benefits = QualityEffectCatalog.Curve(EffectId.BuildingDurability, medium), costs = QualityEffectCatalog.Curve(EffectId.ConsumerDemand, medium);
            sections.Add(new KeyValuePair<string, string>(A("AllQualityLevels"), string.Join("\n", Enumerable.Range(0, 7).Select(tier => ((QualityCategory)tier).GetLabel() + ": " + A("Benefits") + " " + benefits[tier].ToString("0.###", CultureInfo.InvariantCulture) + "x / " + A("Costs") + " " + costs[tier].ToString("0.###", CultureInfo.InvariantCulture) + "x"))));
            foreach (var family in ids.GroupBy(Family))
                sections.Add(new KeyValuePair<string, string>(A(family.Key) + " (" + family.Count() + ")", string.Join("\n", family.Select(id => T("Effect." + id)))));
            var other = new List<string> { A(progression ? "ProgressionFamilies" : "PresetScope") };
            if (progression)
            {
                other.Add(A("ProgressionScope"));
                other.Add(QualityProgression.HygieneStatus.Translate().ToString());
                other.Add(A("DetectedExclusions") + ": " + QualityProgression.Exclusions.Count());
                other.AddRange(QualityProgression.Exclusions.Select(d => d.LabelCap.ToString() + " [" + d.defName + "]"));
                other.AddRange(QualityNativeOutput.Status);
            }
            else other.Add(A("MildAllSwitches"));
            sections.Add(new KeyValuePair<string, string>(A("OtherChanges") + (progression ? " (" + QualityProgression.Exclusions.Count() + " " + A("Exclusions") + ")" : ""), string.Join("\n\n", other)));
            Find.WindowStack.Add(new Window_QualityPresetReview(progression ? T("ProgressionPreset") : A(medium ? "Medium" : "Mild"), enabled, replaced, benefits, costs, sections, () =>
            {
                RememberDraft();
                Draft.EnablePresetProduction();
                if (progression) QualityProgression.Apply(Draft);
                else foreach (EffectId id in ids) Draft.Effects[id.ToString()] = new EffectSettings { Enabled = true, Factors = QualityEffectCatalog.Curve(id, medium) };
                Editor.SynchronizeInputs();
                GUI.FocusControl(null);
                DraftChanged();
            }));
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
