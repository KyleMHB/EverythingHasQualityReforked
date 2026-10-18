using RimWorld;
using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    class Mod_QEverything : Mod
    {
        public static ModSettings_QEverything settings;
        public Mod_QEverything(ModContentPack content) : base(content)
        {
            // Match Verse.LoadedModManager's verified filename convention before any migration/write.
            string path = Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename("Mod_" + content.FolderName + "_" + GetType().Name + ".xml"));
            try
            {
                if (File.Exists(path) && !File.Exists(path + ".legacy-v0.bak"))
                {
                    var root = XDocument.Load(path);
                    if (!root.Descendants("qualityConfiguration").Any()) File.Copy(path, path + ".legacy-v0.bak", false);
                }
            }
            catch (Exception error)
            {
                Log.Warning("Everything Has Quality: legacy settings backup failed: " + error.Message);
            }
            settings = GetSettings<ModSettings_QEverything>();
        }
        public override string SettingsCategory() => "Everything Has Quality";
        // The native dialog writes settings in PreClose. It can only serialize Saved.
        public override void WriteSettings()
        {
            settings?.Write();
        }
        public override void DoSettingsWindowContents(Rect rect)
        {
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 85), "QEverything.Options.Intro".Translate());
            if (Widgets.ButtonText(new Rect(rect.x, rect.y + 95, rect.width, 36), "QEverything.Options.Open".Translate())) Find.WindowStack.Add(new Window_QualityOptions());
            Widgets.Label(new Rect(rect.x, rect.y + 145, rect.width, 65), (QualitySettingsState.RestartPending ? "QEverything.Options.RestartPending" : "QEverything.Options.ActiveSaved").Translate());
        }
    }
}
