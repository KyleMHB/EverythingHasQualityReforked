# Changelog

Agent read route: read only the newest relevant release; locate older versions by searching `^## ` headings.

## 2026-10-25

### Changed

- Renamed the package to `KyleMHB.ConfigurableQuality` and the assembly and namespace to `ConfigurableQuality`. Existing settings are copied to the new configuration identity on first launch.
- Updated the Progression config preset for the renamed package.

## 2026-10-23

### Added

- Added an optional Progression preset for Auto Mod Config, with Mild factors and startup handling for the pack's jumpsuit and hygiene exclusions.
- Added opt-in vanilla-quality removal with a global default and group, category and item overrides. Existing configurations keep vanilla quality unless removal is explicitly selected.
- Added illustrated Workshop explainers for quality assignment, optional effects, price and wealth controls, rule inheritance and custom strength settings.
- Added separate market-value rules with a global default and group, category and item overrides. Disabling scaling removes quality premiums and discounts from added quality and eligible vanilla items. Other mods' native quality stays protected.

### Changed

- Made fresh installations leave gameplay unchanged until settings or a preset are applied. Existing saved choices are retained.
- Made presets enable standard assignment groups and production settings as well as effects.
- Renamed the mod to Configurable Quality in the mod list, settings menu and public descriptions.
- Replaced the title artwork and Workshop preview with the new name and a tagline explaining the configuration options.

### Fixed

- Kept active settings unchanged until restart when config-sharing tools import settings during play.

## 2026-10-21

### Fixed

- Fixed negative production-role quality modifiers increasing quality instead of lowering it.
- Fixed queued ingredient collection when resource quality is disabled but other ingredient stacks have quality.
- Matched numeric input limits to the values accepted by Save.
- Included the complete inherited license notice in local packages and repeated deployments.

### Internal

- Isolated check-program builds and added a runner that rejects results from the wrong program.
- Added package file hashes and source/build identifiers to local packages.
- Removed unused settings helpers and shared scalar defaults with the retained public settings fields.

## 2026-10-18

### Added

- Added inherited item/category rules with translated-name search, source filters, explanations and scoped bulk actions.
- Added separate opt-in quality effects for durability, production, equipment, combat, power, batteries, food, medicine, supported ordinary drugs and native linked furniture.
- Added previewable mild and Progression presets. All new effects start disabled on fresh installs and upgrades.

### Changed

- Changed saved options to activate after restarting RimWorld, with Save/Discard controls and separate assignment/effect rules.
- Preserved legacy settings and inactive selections during migration.
- Preserved selected native factory and Processor Framework fixed-quality output policies and detected Progression exclusions.
- Documented support limits, omitted storage and installed-bionic effects, and described disable/removal behavior.

### Fixed

- Fixed version-specific quality production patches and isolated deferred recipe operations.
- Removed live component reinitialization when saving settings.
- Preserved native quality support when assignment is excluded.
- Scoped currency and favor handling and aligned quality eligibility with effective rules.

### Internal

- Added isolated 1.6/.NET Framework 4.8 and 1.7/.NET 10 build/package tooling with explicit deployment destinations.
- Removed stale duplicate assemblies and bundled Harmony files. Packages now share content and load one mod assembly per version.
- Corrected the assembly title and product metadata.
## 2026-10-14

### Changed

- Added RimWorld 1.7 compatibility while retaining 1.6 support.

## 2026-08-28

### Added

- Added save-safe passive fermentation quality for vanilla fermenting barrels and compatible subclasses. Finished beer inherits the deterministic weighted-average quality of the wort accepted by the barrel when resource and ingredient quality influence is enabled.

### Changed

- Expanded the Steam Workshop description with fermentation behavior and guidance for performance, resource quality, turrets, colony mechs, organs, and enforced quality ranges.

## 2026-05-02

### Added

- Added updated README and Steam Workshop description files for the documentation refresh.
- Added a GitHub Markdown `README.md` generated from the global README template.
- Added `steamDescription.MD` with Steam Workshop BBCode generated from the global Steam description template.
- Added searchable settings lists for long modded item lists.
- Added `Mod_SearchableList` and `SearchableListUI` support for virtualized checkbox lists.
- Added per-tab search state for resources, buildings, weapons, apparel, and other items.
- Added cached, pre-partitioned, pre-sorted settings list data to reduce repeated settings-window work.
- Added custom inspiration definitions for butchering, chemistry, construction, cooking, gathering, harvesting, mining, and stonecutting.
- Added English `DefInjected/InspirationDef` translations for custom inspiration labels, letters, end messages, and inspect lines.
- Added `LoadFolders.xml` for RimWorld 1.6 loading through `1.6` and `Common`.
- Added Harmony dependency metadata in `About/About.xml`.
- Added RimWorld 1.6 assembly output under `1.6/Assemblies`.

### Changed

- Changed `CHANGELOG.MD` to `CHANGELOG.md` and normalized it to the global dated changelog template.
- Updated the mod metadata for RimWorld 1.6 support.
- Updated Workshop metadata, preview art, and published file id.
- Replaced the previous Workshop-formatted `README.md` with repository-focused GitHub Markdown.
- Refactored repeated enable, select, deselect, and scroll-list settings UI logic into shared helpers.
- Converted `Source/QualityEverything.csproj` to SDK-style `Microsoft.NET.Sdk` while preserving `net48`.
- Updated keyed English strings for settings search and category controls.

### Fixed

- Fixed trader currency handling so only normal-quality silver behaves as currency.
- Fixed silver sorting patch behavior so silver receives the intended priority and the original method is skipped only when appropriate.
- Fixed legacy weapon quality setting loading for the previous misspelled setting keys.
- Hardened quality generation around null pawns, missing skill records, mechanoid skill levels, skill requirement adjustments, and min/max quality clamping.
- Hardened settings population and def synchronization paths against missing or invalid defs.
- Added guard logging when the haul queue patch target cannot be found.

### Removed

- Removed stale tracked build and IDE artifacts from the repository.
- Removed the old `ChangeLog.txt` in favor of markdown changelog documentation.
- Removed stale hard references to local external DLL paths that are no longer required by source code.

### Internal

- Added centralized quality eligibility logic in `Quality_DefUtility`.
- Added safer quality component synchronization so defs keep original quality components and only gain or lose generated quality support when settings require it.
- Added dynamic compatibility handling for RimWorld Improve This Building components without a hard compile-time dependency.
- Preserved art items as eligible for legendary quality while routing general min/max quality decisions through shared utility logic.
- Added NuGet package references for `Krafs.Rimworld.Ref` and `Lib.Harmony`.
- Expanded `.gitignore` for local `.dotnet`, `.vs`, `bin`, `obj`, and Visual Studio user state files.
