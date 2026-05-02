# Everything Has Quality Reforked - Project Changes

## Gameplay And Quality Logic

- Added centralized quality eligibility logic in `Quality_DefUtility`.
- Added safer quality component synchronization so defs keep original quality components and only gain or lose generated quality support when settings require it.
- Added dynamic compatibility handling for RimWorld Improve This Building components without requiring a hard compile-time dependency.
- Hardened quality generation around null pawns, missing skill records, mechanoid skill levels, skill requirement adjustments, and min/max quality clamping.
- Preserved art items as eligible for legendary quality while routing general min/max quality decisions through shared utility logic.
- Improved handling for buildings, resources, meals, drugs, medicine, apparel, weapons, shells, manufactured goods, stone blocks, animal products, crops, mining, and butchering.

## Settings UI And Performance

- Added searchable settings lists for long modded item lists.
- Added `Mod_SearchableList` / `SearchableListUI` support for virtualized checkbox lists.
- Added per-tab search state for resources, buildings, weapons/apparel, and other items.
- Added cached, pre-partitioned, pre-sorted settings list data to reduce repeated work every settings frame.
- Refactored repeated enable/select/deselect and scroll-list UI logic into shared helpers.
- Kept individual-category controls for resources, buildings, weapons, apparel, and other items while improving layout behavior.
- Added "No matches" localization for empty search results.

## Stability And Bug Fixes

- Added guard logging when the haul queue patch target cannot be found.
- Fixed trader currency handling so only normal-quality silver behaves as currency.
- Added null checks around tradeable silver selection.
- Fixed silver sorting patch behavior so silver receives the intended priority and the original method is skipped only when appropriate.
- Hardened settings population and def sync paths against missing or invalid defs.
- Reduced stale state risk by centralizing quality category and individual-setting decisions.

## Inspirations And Translation Support

- Added or refreshed custom inspiration definitions for:
  - Inspired butchering
  - Inspired chemistry
  - Inspired construction
  - Inspired cooking
  - Inspired gathering
  - Inspired harvesting
  - Inspired mining
  - Inspired stonecutting
- Added English `DefInjected/InspirationDef` translations so translation mods can override custom inspiration labels, letters, end messages, and inspect lines.
- Updated keyed English strings for settings search and category controls.

## RimWorld 1.6 Packaging

- Added `LoadFolders.xml` for RimWorld 1.6 loading through `1.6` and `Common`.
- Added `1.6/Assemblies/QualityEverything.dll`.
- Kept shared assemblies under `Common/Assemblies`.
- Added Harmony dependency metadata in `About/About.xml`.
- Updated supported version metadata for RimWorld 1.6.
- Updated Workshop metadata, preview art, and published file id.
- Renamed/replaced the building preview asset from `About/Bldg.jpg` to `About/bldg.png`.

## Documentation

- Rewrote `README.md` for the refork, supported version, requirements, feature list, fork history, and issue tracker.
- Added `CHANGELOG.md`.
- Removed the old `ChangeLog.txt` in favor of current markdown changelog documentation.
- Added this `change.md` as the consolidated project change summary.

## Build And Repository Cleanup

- Converted `Source/QualityEverything.csproj` to SDK-style `Microsoft.NET.Sdk`.
- Kept target framework at `net48`.
- Kept deterministic builds and disabled generated assembly info so existing `AssemblyInfo.cs` remains authoritative.
- Preserved RimWorld deploy output behavior for `Common/Assemblies`, `1.6/Assemblies`, and release output.
- Removed stale hard references to local external DLL paths that were no longer needed by source code.
- Added NuGet package references for `Krafs.Rimworld.Ref` and `Lib.Harmony`.
- Expanded `.gitignore` for local `.dotnet`, `.vs`, `bin`, `obj`, and Visual Studio user state files.
- Removed tracked generated files and caches:
  - `.dotnet/`
  - `Source/.vs/`
  - `Source/bin/`
  - `Source/obj/`
- Verified the cleaned repository builds with:
  - `dotnet build .\Source\QualityEverything.csproj -c Debug /p:UseSharedCompilation=false`
