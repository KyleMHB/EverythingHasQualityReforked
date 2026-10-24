# Progression preset

This optional add-on supplies a Configurable Quality preset to Auto Mod Config (Continued). It does not change Ferny's preset or other mods' settings.

Copy this directory to RimWorld's `Mods` folder as `ConfigurableQualityProgressionPreset`. Enable it alongside Configurable Quality and Auto Mod Config (Continued). In Auto Mod Config, select **Configurable Quality - Progression**, apply it, then restart RimWorld.

Applying the preset replaces Configurable Quality's complete configuration snapshot. Export your current settings first. It enables the standard assignment groups and production settings with Mild effect factors. Optional effects only operate where a supported adapter and target are available. Price scaling remains enabled; vanilla-quality removal stays disabled.

At startup, the preset detects deliberate prisoner-jumpsuit and Dubs Bad Hygiene exclusions. Explicit item overrides already in the configuration take precedence. The factory and Processor Framework adapters retain machine-authored output policies. Research and load order remain under the modpack's control.

The two settings filenames support the local `ConfigurableQuality` folder and Workshop item `3710884766`. Rebuild them after changing configuration defaults or preset factors:

```powershell
dotnet run --project Tools/ExportProgressionPreset -- Presets/Progression/Settings
```
