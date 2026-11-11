using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Visual tour of the settings screens for a recording (run with --record --show-ui). It asserts only that each screen
// opened without an error; layout is judged from the recorded frames. Draft edits are discarded before the windows
// close, so nothing is saved. Mod set "default" starts with no mod settings file.
public class UiBatch : Batch
{
    private const double Hold = 4;
    public override string Id => "cq-ui";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override bool Diagnostic => true;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { new PlannedCase("cq.ui-tour", Levels.Diagnostic, Results.Pass) };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var modType = Harness.Settings.ProductionType;
        var assembly = modType.Assembly;
        var state = assembly.GetType("ConfigurableQuality.QualitySettingsState");
        var editorType = assembly.GetType("ConfigurableQuality.Window_QualityOptions");
        object Actions() => AccessTools.Property(state, "Editor").GetValue(null);
        void Changed() => AccessTools.Method(state, "Changed").Invoke(null, null);
        void Call(string method, params object[] args) => AccessTools.Method(Actions().GetType(), method).Invoke(Actions(), args);
        var shown = new List<string>();
        Wait Show(string screen) { shown.Add(screen); ctx.Setup("screen: " + screen); return Wait.Seconds(Hold); }

        var mod = LoadedModManager.ModHandles.First(m => m.GetType() == modType);
        var dialog = (Window)Activator.CreateInstance(typeof(Dialog_ModSettings), mod);
        Find.WindowStack.Add(dialog);
        yield return Show("01 simple page, fresh install");

        Call("ApplyBuiltinPreset", false); Changed();
        yield return Show("02 simple page, Mild applied");

        var editor = (Window)Activator.CreateInstance(editorType);
        Find.WindowStack.Add(editor);
        yield return Show("03 items tab");

        AccessTools.Field(editorType, "focusedItem").SetValue(editor, "Wall");
        yield return Show("04 items tab, Wall focused");

        foreach (string id in new[] { "Wall", "Steel", "Door" }) Call("SetSelected", id, true);
        yield return Show("05 items tab, three selected");
        Call("ClearSelection");

        AccessTools.Field(editorType, "page").SetValue(editor, 1);
        yield return Show("06 effects tab");
        AccessTools.Field(editorType, "selectedEffect").SetValue(editor, Enum.Parse(assembly.GetType("ConfigurableQuality.EffectId"), "RangedCooldown"));
        yield return Show("07 effects tab, cooldown effect");

        AccessTools.Field(editorType, "page").SetValue(editor, 2);
        yield return Show("08 crafting tab");
        AccessTools.Field(editorType, "page").SetValue(editor, 3);
        yield return Show("09 other mods tab");

        editorType.GetMethods(AccessTools.all).First(m => m.Name == "OpenBuiltinPreset" && m.IsStatic).Invoke(null, new object[] { false, true, null });
        yield return Show("10 preset preview, Medium");
        Find.WindowStack.Windows.LastOrDefault(w => w.GetType().Name == "Window_QualitySnapshotReview")?.Close(false);

        // Leave nothing behind: drop the draft before the host dialog's PreClose writes settings.
        AccessTools.Method(state, "Discard").Invoke(null, null);
        editor.Close(false);
        dialog.Close(false);
        yield return Wait.Seconds(1);

        ctx.Begin("cq.ui-tour").Expect("screens", "10 opened").Observe("screens", string.Join("; ", shown))
            .Check(shown.Count == 10, "the settings tour did not reach every screen");
    }
}
