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
            if (!Find.WindowStack.IsOpen<Window_QualityOptions>())
            {
                var host = Find.WindowStack.Windows.OfType<Dialog_ModSettings>().LastOrDefault();
                Find.WindowStack.Add(new Window_QualityOptions(() => host?.Close()));
            }
        }
    }
}
