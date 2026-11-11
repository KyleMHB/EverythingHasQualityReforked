using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace ConfigurableQuality.DevTests;

// Expected behaviour (CHANGELOG 2026-11-11, docs/quality-guide.md): changes made on the mod settings page are saved when
// that window closes; the advanced editor saves only through Save and close, and Cancel discards; saved settings wait for
// a restart, so the running configuration stays as it was.
// Mechanical: the windows are opened and closed and the same edit and close calls the buttons make are invoked directly.
// Nothing is clicked or typed, and the game is not restarted. Mod set "default" starts with no mod settings file, and the
// launcher deletes the file this batch writes before the next launch.
public class SettingsSaveBatch : Batch
{
    public override string Id => "cq-settings";
    public override int SegmentCount => 1;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[]
        { "cq.save-simple-page", "cq.save-editor-guard", "cq.save-editor-prompt", "cq.save-editor-cancel", "cq.save-editor-save" }
        .Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var modType = Harness.Settings.ProductionType;
        var assembly = modType.Assembly;
        var state = assembly.GetType("ConfigurableQuality.QualitySettingsState");
        var editorType = assembly.GetType("ConfigurableQuality.Window_QualityOptions");
        var configType = assembly.GetType("ConfigurableQuality.QualityConfiguration");
        var ruleType = assembly.GetType("ConfigurableQuality.EditorRule");
        var choiceType = assembly.GetType("ConfigurableQuality.RuleChoice");
        T State<T>(string name) => (T)AccessTools.Property(state, name).GetValue(null);
        string Xml(string name) => (string)AccessTools.Method(configType, "Serialize").Invoke(AccessTools.Property(state, name).GetValue(null), null);
        object Actions() => AccessTools.Property(state, "Editor").GetValue(null);
        // The same edit the page's group switch and the editor's default dropdown make.
        void SetGroup(string group, bool on)
        {
            AccessTools.Method(Actions().GetType(), "SetScopeRule").Invoke(Actions(), new object[]
                { Enum.Parse(ruleType, "Assignment"), group, "", Enum.Parse(choiceType, on ? "Include" : "Exclude") });
            AccessTools.Method(state, "Changed").Invoke(null, null);
        }
        var mod = LoadedModManager.ModHandles.First(m => m.GetType() == modType);
        string path = Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename("Mod_" + mod.Content.FolderName + "_" + modType.Name + ".xml"));
        // What a restart would load: the stored configuration, parsed and written back out.
        string OnDisk()
        {
            if (!File.Exists(path)) return "no file";
            string stored = XDocument.Load(path).Descendants("qualityConfiguration").FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(stored)) return "no configuration";
            return (string)AccessTools.Method(configType, "Serialize").Invoke(AccessTools.Method(configType, "Parse").Invoke(null, new object[] { stored }), null);
        }
        bool Open(Type type) => Find.WindowStack.Windows.Any(w => w.GetType() == type);
        string active = Xml("ActiveSnapshot"), start = Xml("Saved");

        // 1. Simple page: an edit, then closing the dialog, saves.
        var dialog = (Window)Activator.CreateInstance(typeof(Dialog_ModSettings), mod);
        Find.WindowStack.Add(dialog);
        yield return Wait.Frames(3);
        SetGroup("Building", true);
        string wanted = Xml("Draft");
        bool dirtyBefore = State<bool>("Dirty");
        dialog.Close(false);
        yield return Wait.Frames(3);
        ctx.Begin("cq.save-simple-page").Expect("saved", "the edited draft, on disk too").Expect("running", "unchanged, restart pending")
            .Observe("dirtyBeforeClose", dirtyBefore).Observe("dirtyAfterClose", State<bool>("Dirty"))
            .Observe("savedIsDraft", Xml("Saved") == wanted).Observe("diskIsDraft", OnDisk() == wanted)
            .Observe("runningUnchanged", Xml("ActiveSnapshot") == active).Observe("restartPending", State<bool>("RestartPending"))
            .Check(dirtyBefore && wanted != start && !State<bool>("Dirty") && Xml("Saved") == wanted && OnDisk() == wanted
                && Xml("ActiveSnapshot") == active && State<bool>("RestartPending"),
                "closing the mod settings page did not save the change for the next restart");

        // 2. Editor open with unsaved work: the host dialog's save must not commit it.
        string savedBefore = Xml("Saved");
        dialog = (Window)Activator.CreateInstance(typeof(Dialog_ModSettings), mod);
        Find.WindowStack.Add(dialog);
        var editor = (Window)Activator.CreateInstance(editorType);
        Find.WindowStack.Add(editor);
        yield return Wait.Frames(3);
        SetGroup("Stuff", true);
        mod.WriteSettings();
        ctx.Begin("cq.save-editor-guard").Expect("saved", "unchanged while the editor holds unsaved changes")
            .Observe("savedUnchanged", Xml("Saved") == savedBefore).Observe("diskUnchanged", OnDisk() == savedBefore).Observe("dirty", State<bool>("Dirty"))
            .Check(Xml("Saved") == savedBefore && OnDisk() == savedBefore && State<bool>("Dirty"), "the host dialog saved the editor's unsaved changes");

        // 3. Closing the editor with unsaved work asks first.
        bool allowed = editor.OnCloseRequest();
        yield return Wait.Frames(3);
        var promptType = assembly.GetType("ConfigurableQuality.Window_QualityClose");
        bool prompted = Open(promptType);
        ctx.Begin("cq.save-editor-prompt").Expect("close", "refused, with the save/discard/keep editing prompt")
            .Observe("closeAllowed", allowed).Observe("promptOpen", prompted).Observe("editorOpen", Open(editorType))
            .Check(!allowed && prompted && Open(editorType), "the editor closed, or did not ask, with unsaved changes");
        Find.WindowStack.Windows.FirstOrDefault(w => w.GetType() == promptType)?.Close(false);

        // 4. Cancel discards and closes.
        AccessTools.Method(editorType, "Finish").Invoke(editor, new object[] { false });
        yield return Wait.Frames(3);
        ctx.Begin("cq.save-editor-cancel").Expect("draft", "back to saved; nothing written")
            .Observe("editorOpen", Open(editorType)).Observe("dirty", State<bool>("Dirty"))
            .Observe("draftIsSaved", Xml("Draft") == savedBefore).Observe("diskUnchanged", OnDisk() == savedBefore)
            .Check(!Open(editorType) && !State<bool>("Dirty") && Xml("Draft") == savedBefore && Xml("Saved") == savedBefore && OnDisk() == savedBefore,
                "Cancel left the editor open, kept the changes or wrote settings");

        // 5. Save and close saves and closes.
        editor = (Window)Activator.CreateInstance(editorType);
        Find.WindowStack.Add(editor);
        yield return Wait.Frames(3);
        SetGroup("Stuff", true);
        wanted = Xml("Draft");
        AccessTools.Method(editorType, "Finish").Invoke(editor, new object[] { true });
        yield return Wait.Frames(3);
        ctx.Begin("cq.save-editor-save").Expect("saved", "the edited draft, on disk too").Expect("running", "unchanged")
            .Observe("editorOpen", Open(editorType)).Observe("dirty", State<bool>("Dirty"))
            .Observe("savedIsDraft", Xml("Saved") == wanted).Observe("diskIsDraft", OnDisk() == wanted).Observe("runningUnchanged", Xml("ActiveSnapshot") == active)
            .Check(wanted != savedBefore && !Open(editorType) && !State<bool>("Dirty") && Xml("Saved") == wanted && OnDisk() == wanted && Xml("ActiveSnapshot") == active,
                "Save and close did not save the change, or left the editor open");
        dialog.Close(false);
    }
}
