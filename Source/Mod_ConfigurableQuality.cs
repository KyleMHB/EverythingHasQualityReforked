using RimWorld;
using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;
using Verse;

namespace ConfigurableQuality
{
    class Mod_ConfigurableQuality : Mod
    {
        public static ModSettings_ConfigurableQuality settings;
        public Mod_ConfigurableQuality(ModContentPack content) : base(content)
        {
            // Match Verse.LoadedModManager's verified filename convention before any migration/write.
            string path = Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename("Mod_" + content.FolderName + "_" + GetType().Name + ".xml"));
            try
            {
                if (!File.Exists(path))
                {
                    var oldFolders = new[] { content.FolderName, "EverythingHasQualityReforked", "3710884766" };
                    string previous = oldFolders.Select(folder => Path.Combine(GenFilePaths.ConfigFolderPath,
                        GenText.SanitizeFilename("Mod_" + folder + "_Mod_QEverything.xml"))).FirstOrDefault(File.Exists);
                    if (previous != null)
                    {
                        var migrated = XDocument.Load(previous);
                        foreach (var attribute in migrated.Descendants().Attributes("Class")
                            .Where(a => a.Value == "QualityEverything.ModSettings_QEverything"))
                            attribute.Value = typeof(ModSettings_ConfigurableQuality).FullName;
                        migrated.Save(path);
                        Log.Message("Configurable Quality: copied existing settings to the renamed configuration file.");
                    }
                }
                if (File.Exists(path) && !File.Exists(path + ".legacy-v0.bak"))
                {
                    var root = XDocument.Load(path);
                    if (!root.Descendants("qualityConfiguration").Any()) File.Copy(path, path + ".legacy-v0.bak", false);
                }
            }
            catch (Exception error)
            {
                Log.Warning("Configurable Quality: legacy settings backup failed: " + error.Message);
            }
            settings = GetSettings<ModSettings_ConfigurableQuality>();
        }
        public override string SettingsCategory() => "Configurable Quality";
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
