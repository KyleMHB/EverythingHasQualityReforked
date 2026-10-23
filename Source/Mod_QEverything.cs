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
        private int summaryRevision = -1, summaryCatalogRevision = -1;
        private string savedSummary;
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
                Log.Warning("Configurable Quality: legacy settings backup failed: " + error.Message);
            }
            settings = GetSettings<ModSettings_QEverything>();
        }
        public override string SettingsCategory() => "Configurable Quality";
        // The native dialog writes settings in PreClose. It can only serialize Saved.
        public override void WriteSettings()
        {
            settings?.Write();
        }
        public override void DoSettingsWindowContents(Rect rect)
        {
            QualityCatalog.Build();
            string T(string key) => ("QEverything.Options." + key).Translate().ToString();
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 60), T("Intro"));
            if (summaryRevision != QualitySettingsState.Revision || summaryCatalogRevision != QualityCatalog.Revision)
            {
                var saved = QualitySettingsState.Saved;
                int quality = QualityCatalog.Entries.Count(e => QualityEditorActions.HasQuality(saved, e.Target, e.NativeQuality, e.RemovableVanillaQuality));
                savedSummary = string.Format(T("SavedSummary"), quality, QualityCatalog.Entries.Count, saved.Effects.Values.Count(e => e.Enabled));
                summaryRevision = QualitySettingsState.Revision; summaryCatalogRevision = QualityCatalog.Revision;
            }
            Widgets.Label(new Rect(rect.x, rect.y + 68, rect.width, 48), savedSummary);
            Widgets.Label(new Rect(rect.x, rect.y + 122, rect.width, 32), T(QualitySettingsState.RestartPending ? "RestartPending" : "ActiveSaved"));
            if (Widgets.ButtonText(new Rect(rect.x, rect.y + 166, Math.Min(320, rect.width), 40), T("Open"))
                && !Find.WindowStack.IsOpen<Window_QualityOptions>())
            {
                Find.WindowStack.Add(new Window_QualityOptions());
            }
        }
    }
}
