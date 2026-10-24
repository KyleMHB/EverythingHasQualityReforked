using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    internal sealed partial class Window_QualityOptions
    {
        private EffectId selectedEffect;
        private Vector2 effectDetailScroll;
        private static string E(string key) => ("QEverything.Curves." + key).Translate().ToString();
        internal void SelectEffect(EffectId id)
        {
            if (Editor.EndGesture()) QualitySettingsState.Changed();
            selectedEffect = id; effectSearch = ""; page = 1;
            effectDetailScroll = Vector2.zero; GUI.FocusControl(null);
        }
        private void EffectCommand(Action action)
        {
            Editor.EndGesture(); action(); GUI.FocusControl(null);
            QualitySettingsState.Changed(); DraftChanged();
        }
        private void DrawEffects(Rect rect)
        {
            float half = (rect.width - 8) / 2;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, half, 30), A("ChoosePreset")))
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> {
                    new FloatMenuOption(A("Mild"), () => OpenPreset(false)),
                    new FloatMenuOption(A("Medium"), () => OpenPreset(false, true)),
                    new FloatMenuOption(A("Progression"), () => OpenPreset(true)) }));
            if (Widgets.ButtonText(new Rect(rect.x + half + 8, rect.y, half, 30), E("AllActions")))
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> {
                    new FloatMenuOption(E("AllMild"), () => PreviewAllCurves(CurveBase.Mild)),
                    new FloatMenuOption(E("AllMedium"), () => PreviewAllCurves(CurveBase.Medium)),
                    new FloatMenuOption(E("DisableAll"), () => Confirm(E("DisableConfirm"), () => EffectCommand(() => Editor.DisableAllEffects()))) }));
            rect.y += 38; rect.height -= 38;
            float listWidth = Math.Min(330, rect.width * .38f);
            var searchRect = new Rect(rect.x, rect.y, listWidth, 28);
            TooltipHandler.TipRegion(searchRect, A("SearchEffects"));
            string query = Widgets.TextField(searchRect, effectSearch);
            if (query != effectSearch) { effectSearch = query; effectScroll = Vector2.zero; }
            var ids = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Where(id => Matches(T("Effect." + id) + " " + A(Family(id)), effectSearch)).ToArray();
            var list = new Rect(rect.x, rect.y + 36, listWidth, rect.height - 36);
            float height = ids.Length * 66 + ids.Select(Family).Distinct().Count() * 30;
            var view = new Rect(0, 0, listWidth - 18, Math.Max(list.height, height));
            Widgets.BeginScrollView(list, ref effectScroll, view);
            float y = 0; string last = null;
            if (ids.Length == 0) Widgets.Label(new Rect(0, 0, view.width, 32), A("NoResults"));
            foreach (var id in ids)
            {
                string family = Family(id);
                if (family != last) { Widgets.Label(new Rect(0, y, view.width, 28), A(family)); y += 30; last = family; }
                if (selectedEffect == id) Widgets.DrawHighlightSelected(new Rect(0, y, view.width, 64));
                bool enabled = Draft.Effects[id.ToString()].Enabled, old = enabled;
                Widgets.CheckboxLabeled(new Rect(0, y + 3, 26, 28), "", ref enabled);
                if (old != enabled) { var selected = id; EffectCommand(() => Editor.Edit(c => c.Effects[selected.ToString()].Enabled = enabled)); }
                if (Widgets.ButtonText(new Rect(30, y, view.width - 30, 32), T("Effect." + id), drawBackground: false))
                { if (Editor.EndGesture()) QualitySettingsState.Changed(); selectedEffect = id; effectDetailScroll = Vector2.zero; GUI.FocusControl(null); }
                Widgets.Label(new Rect(30, y + 34, view.width - 30, 28), A(QualityEffects.Available(id) ? "AdapterAvailable" : "AdapterUnavailable"));
                y += 66;
            }
            Widgets.EndScrollView();
            DrawEffectDetail(new Rect(rect.x + listWidth + 12, rect.y, rect.width - listWidth - 12, rect.height));
        }
        private void DrawEffectDetail(Rect rect)
        {
            var id = selectedEffect;
            var effect = Draft.Effects[id.ToString()];
            var choice = Editor.ClassifyEffectCurve(id);
            float width = rect.width - 18;
            string help = (QualityEffectCatalog.IsCost(id) ? T("CostCurve") : T("BenefitCurve")) + "\n" + QualityEffects.Reason(id);
            float helpHeight = Text.CalcHeight(help, width) + 12;
            string strengthText = choice.Base == CurveBase.Custom ? E("CustomHelp") : E("Strength") + ": " + choice.Strength.ToString("0.00", CultureInfo.InvariantCulture) + "×";
            float strengthHeight = Math.Max(28, Text.CalcHeight(strengthText, width));
            float height = 36 + helpHeight + 32 + 36 + strengthHeight + 4 + 32 + 7 * 54 + (id == EffectId.LinkedFurniture ? 140 : 0) + 8;
            Widgets.BeginScrollView(rect, ref effectDetailScroll, new Rect(0, 0, width, Math.Max(rect.height, height)));
            Widgets.Label(new Rect(0, 0, width, 32), T("Effect." + id));
            Widgets.Label(new Rect(0, 36, width, helpHeight), help);
            float y = 36 + helpHeight;
            Widgets.Label(new Rect(0, y, width, 28), E("Curve") + ": " + E(choice.Base.ToString())); y += 32;
            float half = (width - 8) / 2;
            if (Widgets.ButtonText(new Rect(0, y, half, 30), A("Mild"))) PreviewEffectCurve(id, CurveBase.Mild);
            if (Widgets.ButtonText(new Rect(half + 8, y, half, 30), A("Medium"))) PreviewEffectCurve(id, CurveBase.Medium);
            y += 36;
            Widgets.Label(new Rect(0, y, width, strengthHeight), strengthText); y += strengthHeight + 4;
            var slider = new Rect(0, y, width, 24);
            bool wasEnabled = GUI.enabled; GUI.enabled = wasEnabled && choice.Base != CurveBase.Custom;
            if (Event.current.type == EventType.MouseDown && slider.Contains(Event.current.mousePosition) && GUI.enabled) Editor.BeginGesture("effect-strength:" + id);
            float next = Widgets.HorizontalSlider(slider, choice.Strength, .25f, 2f);
            if (GUI.enabled && Math.Abs(next - choice.Strength) > .000001f)
            { Editor.ApplyEffectCurve(id, choice.Base, next); QualitySettingsState.Changed(); }
            GUI.enabled = wasEnabled; y += 32;
            for (int tier = 0; tier < 7; tier++)
            {
                Widgets.Label(new Rect(0, y, width * .52f, 28), ((QualityCategory)tier).GetLabel());
                DrawEffectFactor(new Rect(width * .54f, y, width * .46f, 28), id, tier, effect.Factors[tier]); y += 54;
            }
            if (id == EffectId.LinkedFurniture)
            {
                Widgets.Label(new Rect(0, y, width, 28), T("LinkedBonusCap")); y += 28;
                FloatField(new Rect(0, y, width, 28), "linkedBonus", Draft.LinkedFurnitureBonusCap, v => Draft.LinkedFurnitureBonusCap = v); y += 42;
                Widgets.Label(new Rect(0, y, width, 28), T("LinkedPenaltyCap")); y += 28;
                FloatField(new Rect(0, y, width, 28), "linkedPenalty", Draft.LinkedFurniturePenaltyCap, v => Draft.LinkedFurniturePenaltyCap = v);
            }
            Widgets.EndScrollView();
        }
        private void DrawEffectFactor(Rect rect, EffectId id, int tier, float value)
        {
            string key = "effect:" + id + ":" + tier, text = Editor.NumberText(key, value);
            GUI.SetNextControlName("quality-number-" + key);
            string edited = Widgets.TextField(rect, text);
            if (!BufferIsValid(key, edited, out _))
            {
                var color = GUI.color; var font = Text.Font; GUI.color = new Color(1f, .65f, .6f); Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(rect.x, rect.yMax + 1, rect.width, 18), U("FieldRange", RangeHint(key))); GUI.color = color; Text.Font = font;
                TooltipHandler.TipRegion(rect, U("LastValid"));
            }
            if (edited != text && Editor.EditEffectFactor(id, tier, edited)) { QualitySettingsState.Changed(); DraftChanged(); }
        }
        private string CurvePreview(EffectId id, CurveBase basis)
        {
            var factors = QualityEditorActions.GeneratedCurve(id, basis, 1);
            return string.Join("\n", Enumerable.Range(0, 7).Select(tier => ((QualityCategory)tier).GetLabel() + ": " + Draft.Effects[id.ToString()].Factors[tier].ToString("R", CultureInfo.InvariantCulture) + "× → " + factors[tier].ToString("R", CultureInfo.InvariantCulture) + "×"));
        }
        private void PreviewEffectCurve(EffectId id, CurveBase basis)
        {
            Editor.EndGesture();
            Action apply = () => EffectCommand(() => Editor.ApplyEffectCurve(id, basis, 1, true));
            if (Editor.ClassifyEffectCurve(id).Base == CurveBase.Custom)
                Confirm(E("ReplaceCustom") + "\n\n" + T("Effect." + id) + "\n" + CurvePreview(id, basis), apply);
            else apply();
        }
        private void PreviewAllCurves(CurveBase basis)
        {
            var ids = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().ToArray();
            var sections = ids.Select(id => new KeyValuePair<string, string>(T("Effect." + id), CurvePreview(id, basis))).ToList();
            int replaced = ids.Count(id => !Draft.Effects[id.ToString()].Factors.SequenceEqual(QualityEditorActions.GeneratedCurve(id, basis, 1)));
            Find.WindowStack.Add(new Window_QualityPresetReview(E("All" + basis) + " — " + E("CurvesOnly"), 0, replaced,
                QualityEffectCatalog.Curve(EffectId.BuildingDurability, basis == CurveBase.Medium), QualityEffectCatalog.Curve(EffectId.ConsumerDemand, basis == CurveBase.Medium), sections,
                () => EffectCommand(() => Editor.ApplyAllEffectCurves(basis, true))));
        }
    }
}
