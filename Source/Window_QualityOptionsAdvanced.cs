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

        private void DrawCompatibility(Rect rect)
        {
            var rows = new[]
            {
                Tuple.Create(A("AvailableEffects"), Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Count(QualityEffects.Available) + "/25", A("CompatibilitySummary")),
                Tuple.Create(A("Storage"), A("Omitted"), T("StorageOmitted")),
                Tuple.Create(A("Bionics"), A("Deferred"), T("BionicsDeferred")),
                Tuple.Create(T("ProgressionPreset"), A("PreviewBeforeApplying"), T("ProgressionStatus") + "\n\n" + QualityProgression.HygieneStatus.Translate().ToString() + "\n\n" + string.Join("\n", QualityNativeOutput.Status)),
                Tuple.Create("Improve This", A("NativePreserved"), T("ImproveThisUnavailable"))
            };
            float contentWidth = rect.width - 18;
            float baseHeight = 44 + rows.Sum(row => 40 + (expandedCompatibility.Contains(row.Item1) ? Text.CalcHeight(row.Item3, contentWidth - 12) + 16 : 0)) + 130;
            var view = new Rect(0, 0, contentWidth, Math.Max(rect.height, baseHeight + (showDiagnostics ? 46 + (QualityPatchUtility.Statuses.Count + 25) * 62 : 0) + (showLegacy ? 34 + Math.Min(Draft.InactiveLegacy.Count, 100) * 30 : 0)));
            Widgets.BeginScrollView(rect, ref compatibilityScroll, view);
            Widgets.Label(new Rect(0, 0, view.width, 32), A("CompatibilityHelp"));
            float y = 44;
            foreach (var row in rows)
            {
                bool open = expandedCompatibility.Contains(row.Item1);
                if (Widgets.ButtonText(new Rect(0, y, view.width * .48f, 30), (open ? "- " : "+ ") + row.Item1))
                { if (open) expandedCompatibility.Remove(row.Item1); else expandedCompatibility.Add(row.Item1); }
                Widgets.Label(new Rect(view.width * .5f, y, view.width * .5f, 30), row.Item2); y += 40;
                if (!open) continue;
                float h = Text.CalcHeight(row.Item3, view.width - 12) + 16;
                Widgets.Label(new Rect(12, y, view.width - 12, h), row.Item3); y += h;
            }
            y += 10;
            Widgets.Label(new Rect(0, y, view.width, 28), T("InactiveLegacy") + ": " + Draft.InactiveLegacy.Count); y += 32;
            if (Draft.InactiveLegacy.Count > 0)
            {
                if (Widgets.ButtonText(new Rect(0, y, view.width * .58f, 30), T("RecoverLegacy"))) Confirm(T("RecoverConfirm"), () =>
                {
                    RememberDraft();
                    foreach (var item in Draft.InactiveLegacy)
                    {
                        string id = item.Key.Substring(item.Key.IndexOf(':') + 1);
                        Draft.Assignment.Items[id] = item.Value ? RuleChoice.Include : RuleChoice.Exclude;
                    }
                    DraftChanged();
                });
                if (Widgets.ButtonText(new Rect(view.width * .6f, y, view.width * .4f, 30), A(showLegacy ? "HideLegacy" : "ShowLegacy"))) showLegacy = !showLegacy;
                y += 38;
                if (showLegacy)
                {
                    Widgets.Label(new Rect(0, y, view.width, 32), A("LegacyPreview")); y += 34;
                    foreach (var entry in Draft.InactiveLegacy.OrderBy(p => p.Key).Take(100))
                    { Widgets.Label(new Rect(0, y, view.width, 28), entry.Key + ": " + T(entry.Value ? "Include" : "Exclude")); y += 30; }
                }
            }
            if (Widgets.ButtonText(new Rect(0, y, view.width, 30), A(showDiagnostics ? "HideDiagnostics" : "ShowDiagnostics"))) showDiagnostics = !showDiagnostics;
            y += 38;
            if (showDiagnostics)
            {
                Widgets.Label(new Rect(0, y, view.width, 42), T("PatchStatusHelp")); y += 46;
                foreach (var patch in QualityPatchUtility.Statuses.OrderBy(p => p.Key))
                { Widgets.Label(new Rect(0, y, view.width, 56), patch.Key + ": " + T(patch.Value ? "PatchVerified" : "NativeFallback")); y += 62; }
                foreach (EffectId id in Enum.GetValues(typeof(EffectId)))
                { Widgets.Label(new Rect(0, y, view.width, 56), T("Effect." + id) + ": " + T(QualityEffects.Available(id) ? "Available" : "Unavailable") + " / " + QualityEffects.Reason(id)); y += 62; }
            }
            Widgets.EndScrollView();
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
