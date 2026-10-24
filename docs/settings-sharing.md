# Settings sharing

Configurable Quality stores its configuration in RimWorld's standard `ModSettings` XML file, in the `qualityConfiguration` element. This includes global defaults and group/category/item overrides for assignment and vanilla-removal rules, effect participation, market-value rules, production choices and effect curves. Save the draft before exporting. Unsaved changes are not exported.

Settings imported before startup initializes the mod become active for that session. Settings imported after initialization replace the saved configuration and editable draft. They leave active behavior unchanged until RimWorld restarts, including on installations that did not have a settings file yet.

## Progression tools

The installed Progression list inspected on 23 October 2026 includes:

| Tool | Package ID | Settings path |
| --- | --- | --- |
| Auto Mod Config (Continued) | `Garethp.ModlistConfigurator` | Applies settings files from preset mods before mod constructors finish. |
| Ferny's Mod Configs | `ferny.modconfigs` | Supplies the `FernyConfigs3` preset through Auto Mod Config. |
| Export and Import Mod Settings | `Mlie.ExportAndImportModSettings` | Exports standard mod-settings XML and reloads imported settings. Its own settings page includes bulk export/import controls. |
| Share Settings | `sk.sharesettings` | Serializes standard mod-settings XML and imports it into the existing settings object. |

No dependency on these tools is required. The package ID is `KyleMHB.ConfigurableQuality`. The settings class is `ConfigurableQuality.ModSettings_ConfigurableQuality`, the mod class is `Mod_ConfigurableQuality`, and the local package folder is `ConfigurableQuality`. On first launch, the renamed mod copies an existing old-name settings file and updates its settings-class attribute. The original file is retained.

## Included Progression preset

The optional add-on in `Presets/Progression` supplies **Configurable Quality - Progression** to Auto Mod Config. It includes local-folder and Workshop-folder filenames. Install the add-on separately; the base mod remains passive on a fresh install.

The preset enables standard assignment groups and production settings, with Mild effect curves. Price scaling stays enabled and vanilla-quality removal stays disabled. A serialized `progressionCompatibility` flag resolves the jumpsuit and hygiene exclusions after provider settings and definitions load. Explicit saved item overrides take precedence. Applying an Auto Mod Config preset replaces the complete configuration snapshot.

Regenerate the files from the configuration source with `dotnet run --project Tools/ExportProgressionPreset -- Presets/Progression/Settings`.

## Distributing a preset

1. Configure the mod, including Market value category rules, then Save for restart.
2. Export its settings file using the chosen tool. For Export and Import Mod Settings, its bulk controls are available from that tool's own settings page.
3. For an Auto Mod Config preset, include the exported file in the preset mod's `Settings` directory. Preserve the standard settings XML envelope and class name. The escaped `qualityConfiguration` value is one complete configuration snapshot, not separate mergeable XML settings.
4. Match the destination settings filename to the mod's installed folder identity. A Workshop install uses the Workshop folder ID, while the local package uses `ConfigurableQuality`. Follow the exporter-generated filename for the intended installation rather than the display name.
5. Restart RimWorld after importing during a running session.

The installed Auto Mod Config source uses `Mod_{folder}_{mod-class}.xml` filenames and compares the serialized configuration as one XML leaf. A preset update therefore replaces the complete quality configuration when that leaf is updated. It cannot merge individual category exceptions inside the escaped configuration string.

## Inspection sources

Auto Mod Config's installed `1.6/Source/SettingsImporter.cs` and `XmlUtils.cs` define file naming, startup imports and XML merging. Ferny's installed `Defs/Preset.xml` identifies its preset. Installed 1.7 assembly metadata and IL for Share Settings and Export and Import Mod Settings show their standard Scribe serialization and import paths. This inspection did not change the installed mods, player configuration or Ferny's preset.
