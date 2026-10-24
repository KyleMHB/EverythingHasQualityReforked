using System;
using System.IO;
using System.Xml.Linq;
using ConfigurableQuality;

if (args.Length != 1) throw new ArgumentException("Supply the preset add-on's Settings directory.");
var configuration = new QualityConfiguration { ProgressionCompatibility = true };
configuration.EnablePresetProduction();
foreach (EffectId id in Enum.GetValues(typeof(EffectId)))
    configuration.Effects[id.ToString()] = new EffectSettings { Enabled = true, Factors = QualityEffectCatalog.Curve(id) };
// Optional adapters still fail closed when their provider or supported target is absent.
// Provider exclusions are resolved at startup, after the pack's own settings and defs load.
if (configuration.Validate().Count != 0) throw new Exception("Invalid preset configuration.");
Directory.CreateDirectory(args[0]);
var document = new XDocument(new XElement("SettingsBlock",
    new XElement("ModSettings", new XAttribute("Class", "ConfigurableQuality.ModSettings_ConfigurableQuality"),
        new XElement("qualityConfiguration", configuration.Serialize()))));
foreach (string folder in new[] { "ConfigurableQuality", "3710884766" })
    document.Save(Path.Combine(args[0], "Mod_" + folder + "_Mod_ConfigurableQuality.xml"));
Console.WriteLine("Exported Progression preset for local and Workshop installations.");
