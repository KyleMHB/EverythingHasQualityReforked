using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    internal sealed partial class Window_QualityOptions
    {
        private static string C(string key) => ("QEverything.Compatibility." + key).Translate().ToString();
        private void DrawCompatibility(Rect rect)
        {
            var rows = new[]
            {
                Tuple.Create(A("AvailableEffects"), Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Count(QualityEffects.Available) + "/25", A("CompatibilitySummary")),
                Tuple.Create(A("Storage"), A("Omitted"), T("StorageOmitted")),
                Tuple.Create(A("Bionics"), A("Deferred"), T("BionicsDeferred")),
                Tuple.Create(T("ProgressionPreset"), A("PreviewBeforeApplying"), C("ProgressionSummary") + "\n\n" + QualityProgression.HygieneStatus.Translate().ToString() + "\n\n" + string.Join("\n", QualityNativeOutput.Status)),
                Tuple.Create("Improve This", A("NativePreserved"), T("ImproveThisUnavailable"))
            };
            float contentWidth = rect.width - 18;
            var diagnostics = QualityPatchUtility.Statuses.OrderBy(p => p.Key).Select(p => p.Key + ": " + T(p.Value ? "PatchVerified" : "NativeFallback"))
                .Concat(Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Select(id => T("Effect." + id) + ": " + T(QualityEffects.Available(id) ? "Available" : "Unavailable") + " / " + QualityEffects.Reason(id))).ToArray();
            float diagnosticHeight = diagnostics.Sum(text => Math.Max(56, Text.CalcHeight(text, contentWidth)) + 6);
            float baseHeight = 140 + Text.CalcHeight(C("PolicyHelp"), contentWidth) + rows.Sum(row => 40 + (expandedCompatibility.Contains(row.Item1) ? Text.CalcHeight(row.Item3, contentWidth - 12) + 16 : 0)) + 130;
            var view = new Rect(0, 0, contentWidth, Math.Max(rect.height, baseHeight + (showDiagnostics ? 46 + diagnosticHeight : 0) + (showLegacy ? 34 + Draft.InactiveLegacy.Count * 30 : 0)));
            Widgets.BeginScrollView(rect, ref compatibilityScroll, view);
            try
            {
            Widgets.Label(new Rect(0, 0, view.width, 32), A("CompatibilityHelp"));
            float y = 44;
            bool policy = Draft.ProgressionCompatibility, oldPolicy = policy;
            Widgets.CheckboxLabeled(new Rect(0, y, view.width, 32), C("ProviderPolicy"), ref policy); y += 38;
            if (policy != oldPolicy)
            {
                bool requested = policy;
                Find.WindowStack.Add(new Window_CompatibilityReview(C("ProviderPolicy"), C("PolicyPreview") + "\n\n" +
                    string.Join("\n", QualityProgression.Exclusions.Select(d => d.LabelCap.ToString() + " [" + d.defName + "]")),
                    () => { if (Editor.SetProviderPolicy(requested)) QualitySettingsState.Changed(); DraftChanged(); }));
            }
            float policyHeight = Text.CalcHeight(C("PolicyHelp"), view.width);
            Widgets.Label(new Rect(0, y, view.width, policyHeight), C("PolicyHelp")); y += policyHeight + 12;
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
                if (Widgets.ButtonText(new Rect(0, y, view.width * .58f, 30), T("RecoverLegacy"))) Find.WindowStack.Add(new Window_CompatibilityReview(T("RecoverLegacy"), C("RecoveryPreview") + "\n\n" +
                    string.Join("\n", Draft.InactiveLegacy.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + ": " + T(p.Value ? "Include" : "Exclude"))),
                    () => { if (Editor.RecoverLegacy()) QualitySettingsState.Changed(); DraftChanged(); }));
                if (Widgets.ButtonText(new Rect(view.width * .6f, y, view.width * .4f, 30), A(showLegacy ? "HideLegacy" : "ShowLegacy"))) showLegacy = !showLegacy;
                y += 38;
                if (showLegacy)
                {
                    Widgets.Label(new Rect(0, y, view.width, 32), C("LegacyPreview")); y += 34;
                    foreach (var entry in Draft.InactiveLegacy.OrderBy(p => p.Key))
                    { Widgets.Label(new Rect(0, y, view.width, 28), entry.Key + ": " + T(entry.Value ? "Include" : "Exclude")); y += 30; }
                }
            }
            if (Widgets.ButtonText(new Rect(0, y, view.width, 30), A(showDiagnostics ? "HideDiagnostics" : "ShowDiagnostics"))) showDiagnostics = !showDiagnostics;
            y += 38;
            if (showDiagnostics)
            {
                Widgets.Label(new Rect(0, y, view.width, 42), T("PatchStatusHelp")); y += 46;
                foreach (string text in diagnostics)
                {
                    float h = Math.Max(56, Text.CalcHeight(text, view.width));
                    Widgets.Label(new Rect(0, y, view.width, h), text); y += h + 6;
                }
            }
            }
            finally { Widgets.EndScrollView(); }
        }
    }
    internal sealed class Window_CompatibilityReview : Window
    {
        private readonly string title, preview;
        private readonly Action apply;
        private Vector2 scroll;
        public override Vector2 InitialSize => new Vector2(Math.Min(720, UI.screenWidth - 40), Math.Min(650, UI.screenHeight - 40));
        public Window_CompatibilityReview(string title, string preview, Action apply)
        { this.title = title; this.preview = preview; this.apply = apply; doCloseX = true; forcePause = true; absorbInputAroundWindow = true; }
        public override void DoWindowContents(Rect rect)
        {
            var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color; var enabled = GUI.enabled;
            try
            {
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(0, 0, rect.width, 32), title);
                var viewport = new Rect(0, 40, rect.width, rect.height - 90);
                var view = new Rect(0, 0, rect.width - 18, Math.Max(viewport.height, Text.CalcHeight(preview, rect.width - 18)));
                Widgets.BeginScrollView(viewport, ref scroll, view);
                try { Widgets.Label(view, preview); } finally { Widgets.EndScrollView(); }
                if (Widgets.ButtonText(new Rect(0, rect.height - 40, rect.width / 2 - 5, 36), "QEverything.Advanced.GoBack".Translate())) Close();
                if (Widgets.ButtonText(new Rect(rect.width / 2 + 5, rect.height - 40, rect.width / 2 - 5, 36), "QEverything.Advanced.ApplyDraft".Translate())) { apply(); Close(); }
            }
            finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; GUI.enabled = enabled; }
        }
    }
}
