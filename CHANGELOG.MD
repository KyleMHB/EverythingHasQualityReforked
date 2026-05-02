# Changelog

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
