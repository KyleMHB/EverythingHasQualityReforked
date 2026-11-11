using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
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
            float listWidth = Math.Min(360, rect.width * .38f);
            var searchRect = new Rect(rect.x, rect.y, listWidth, 30);
            string query = Widgets.TextField(searchRect, effectSearch);
            Placeholder(searchRect, A("SearchEffects"), query);
            if (query != effectSearch) { effectSearch = query; effectScroll = Vector2.zero; }
            float allWidth = Math.Min(320, rect.width - listWidth - 12);
            if (Widgets.ButtonText(new Rect(rect.xMax - allWidth, rect.y, allWidth, 30), E("AllActions")))
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> {
                    new FloatMenuOption(E("AllMild"), () => PreviewAllCurves(CurveBase.Mild)),
                    new FloatMenuOption(E("AllMedium"), () => PreviewAllCurves(CurveBase.Medium)),
                    new FloatMenuOption(E("DisableAll"), () => Confirm(E("DisableConfirm"), () => EffectCommand(() => Editor.DisableAllEffects()))) }));
            rect.y += 38; rect.height -= 38;
            var ids = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Where(id => Matches(T("Effect." + id) + " " + A(Family(id)), effectSearch)).ToArray();
            var list = new Rect(rect.x, rect.y, listWidth, rect.height);
            // Only effects that cannot work here get a second line, so the normal case stays one row.
            float height = ids.Sum(id => QualityEffects.Available(id) ? 34f : 54f) + ids.Select(Family).Distinct().Count() * 32;
            var view = new Rect(0, 0, listWidth - 18, Math.Max(list.height, height));
            var color = GUI.color; var anchor = Text.Anchor;
            Widgets.BeginScrollView(list, ref effectScroll, view);
            float y = 0; string last = null;
            if (ids.Length == 0) Widgets.Label(new Rect(0, 0, view.width, 32), A("NoResults"));
            foreach (var id in ids)
            {
                string family = Family(id);
                if (family != last) { GUI.color = OffColor; Text.Anchor = TextAnchor.LowerLeft; Widgets.Label(new Rect(0, y, view.width, 28), A(family)); GUI.color = color; Text.Anchor = anchor; y += 32; last = family; }
                bool available = QualityEffects.Available(id); float rowHeight = available ? 34 : 54;
                if (selectedEffect == id) Widgets.DrawHighlightSelected(new Rect(0, y, view.width, rowHeight - 2));
                bool enabled = Draft.Effects[id.ToString()].Enabled, old = enabled;
                Widgets.CheckboxLabeled(new Rect(0, y + 3, 26, 28), "", ref enabled);
                if (old != enabled) { var selected = id; EffectCommand(() => Editor.Edit(c => c.Effects[selected.ToString()].Enabled = enabled)); }
                var nameRect = new Rect(32, y, view.width - 32, 32);
                Widgets.DrawHighlightIfMouseover(nameRect);
                Text.Anchor = TextAnchor.MiddleLeft; Widgets.Label(nameRect, T("Effect." + id).Truncate(nameRect.width)); Text.Anchor = anchor;
                if (Widgets.ButtonInvisible(nameRect))
                { if (Editor.EndGesture()) QualitySettingsState.Changed(); selectedEffect = id; effectDetailScroll = Vector2.zero; GUI.FocusControl(null); }
                if (!available)
                {
                    GUI.color = WarnColor; Widgets.Label(new Rect(32, y + 30, view.width - 32, 22), A(QualityEffects.HandledElsewhere(id) ? "AdapterExternal" : "AdapterUnavailable")); GUI.color = color;
                    TooltipHandler.TipRegion(new Rect(32, y, view.width - 32, rowHeight), QualityEffects.Reason(id));
                }
                y += rowHeight;
            }
            Widgets.EndScrollView();
            DrawEffectDetail(new Rect(rect.x + listWidth + 12, rect.y, rect.width - listWidth - 12, rect.height));
        }
        private void DrawEffectDetail(Rect rect)
        {
            var id = selectedEffect;
            var effect = Draft.Effects[id.ToString()];
            var choice = Editor.ClassifyEffectCurve(id);
            bool cost = QualityEffectCatalog.IsCost(id), available = QualityEffects.Available(id);
            float width = Math.Min(rect.width - 18, 720);
            string help = (cost ? T("CostCurve") : T("BenefitCurve")) + (available ? "" : "\n" + A(QualityEffects.HandledElsewhere(id) ? "AdapterExternal" : "AdapterUnavailable") + ": " + QualityEffects.Reason(id));
            float helpHeight = Text.CalcHeight(help, width) + 12;
            string strengthText = choice.Base == CurveBase.Custom ? E("CustomHelp") : E("Strength") + ": " + choice.Strength.ToString("0.00", CultureInfo.InvariantCulture) + "x";
            float strengthHeight = Math.Max(28, Text.CalcHeight(strengthText, width));
            float height = 38 + helpHeight + 36 + strengthHeight + 4 + 34 + 28 + 7 * 34 + (id == EffectId.LinkedFurniture ? 150 : 0) + 8;
            var color = GUI.color; var anchor = Text.Anchor;
            Widgets.BeginScrollView(rect, ref effectDetailScroll, new Rect(0, 0, rect.width - 18, Math.Max(rect.height, height)));
            // The on/off switch sits with the settings it controls, as well as in the list.
            bool enabled = effect.Enabled, old = enabled;
            Widgets.CheckboxLabeled(new Rect(0, 0, width, 32), T("Effect." + id) + ": " + E(enabled ? "On" : "Off"), ref enabled);
            if (old != enabled) EffectCommand(() => Editor.Edit(c => c.Effects[id.ToString()].Enabled = enabled));
            GUI.color = available ? OffColor : WarnColor; Widgets.Label(new Rect(0, 38, width, helpHeight), help); GUI.color = color;
            float y = 38 + helpHeight;
            float third = (width - 16) / 3;
            Text.Anchor = TextAnchor.MiddleLeft; Widgets.Label(new Rect(0, y, third, 30), E("Curve") + ": " + E(choice.Base.ToString())); Text.Anchor = anchor;
            if (Widgets.ButtonText(new Rect(third + 8, y, third, 30), E("UseMild"))) PreviewEffectCurve(id, CurveBase.Mild);
            if (Widgets.ButtonText(new Rect(2 * (third + 8), y, third, 30), E("UseMedium"))) PreviewEffectCurve(id, CurveBase.Medium);
            y += 36;
            Widgets.Label(new Rect(0, y, width, strengthHeight), strengthText); y += strengthHeight + 4;
            var slider = new Rect(0, y, width, 24);
            bool wasEnabled = GUI.enabled; GUI.enabled = wasEnabled && choice.Base != CurveBase.Custom;
            if (Event.current.type == EventType.MouseDown && slider.Contains(Event.current.mousePosition) && GUI.enabled) Editor.BeginGesture("effect-strength:" + id);
            float next = Widgets.HorizontalSlider(slider, choice.Strength, .25f, 2f);
            if (GUI.enabled && Math.Abs(next - choice.Strength) > .000001f)
            { Editor.ApplyEffectCurve(id, choice.Base, next); QualitySettingsState.Changed(); }
            GUI.enabled = wasEnabled; y += 34;
            float tierWidth = width * .34f, fieldWidth = width * .22f;
            GUI.color = OffColor;
            Widgets.Label(new Rect(0, y, tierWidth, 26), E("TierHeader"));
            Widgets.Label(new Rect(tierWidth + 8, y, fieldWidth, 26), E("FactorHeader"));
            Widgets.Label(new Rect(tierWidth + fieldWidth + 20, y, width - tierWidth - fieldWidth - 20, 26), E(cost ? "MeaningCost" : "MeaningBenefit"));
            GUI.color = color; y += 28;
            for (int tier = 0; tier < 7; tier++)
            {
                Text.Anchor = TextAnchor.MiddleLeft; Widgets.Label(new Rect(0, y, tierWidth, 28), ((QualityCategory)tier).GetLabel().CapitalizeFirst()); Text.Anchor = anchor;
                DrawEffectFactor(new Rect(tierWidth + 8, y, fieldWidth, 28), new Rect(tierWidth + fieldWidth + 20, y, width - tierWidth - fieldWidth - 20, 28), id, tier, effect.Factors[tier], cost);
                y += 34;
            }
            if (id == EffectId.LinkedFurniture)
            {
                y += 8;
                Widgets.Label(new Rect(0, y, width, 28), T("LinkedBonusCap")); y += 28;
                FloatField(new Rect(0, y, width, 28), "linkedBonus", Draft.LinkedFurnitureBonusCap, v => Draft.LinkedFurnitureBonusCap = v); y += 42;
                Widgets.Label(new Rect(0, y, width, 28), T("LinkedPenaltyCap")); y += 28;
                FloatField(new Rect(0, y, width, 28), "linkedPenalty", Draft.LinkedFurniturePenaltyCap, v => Draft.LinkedFurniturePenaltyCap = v);
            }
            Widgets.EndScrollView();
        }
        private void DrawEffectFactor(Rect rect, Rect meaning, EffectId id, int tier, float value, bool cost)
        {
            string key = "effect:" + id + ":" + tier, text = Editor.NumberText(key, value);
            GUI.SetNextControlName("quality-number-" + key);
            string edited = Widgets.TextField(rect, text);
            var color = GUI.color; var anchor = Text.Anchor; Text.Anchor = TextAnchor.MiddleLeft;
            if (!BufferIsValid(key, edited, out _))
            {
                GUI.color = new Color(1f, .65f, .6f);
                Widgets.Label(meaning, U("FieldRange", RangeHint(key)));
                TooltipHandler.TipRegion(rect, U("LastValid"));
            }
            else
            {
                // Say what the multiplier does: +5% for a bonus, -20% for a shorter cooldown.
                bool neutral = Math.Abs(value - 1f) < .0005f, good = cost ? value < 1f : value > 1f;
                GUI.color = neutral ? OffColor : good ? OnColor : WarnColor;
                Widgets.Label(meaning, neutral ? E("NoChange") : Percent(value));
            }
            GUI.color = color; Text.Anchor = anchor;
            if (edited != text && Editor.EditEffectFactor(id, tier, edited)) { QualitySettingsState.Changed(); DraftChanged(); }
        }
        private string CurvePreview(EffectId id, CurveBase basis)
        {
            var factors = QualityEditorActions.GeneratedCurve(id, basis, 1);
            return string.Join("\n", Enumerable.Range(0, 7).Select(tier => ((QualityCategory)tier).GetLabel() + ": " + Percent(Draft.Effects[id.ToString()].Factors[tier]) + " → " + Percent(factors[tier])));
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
