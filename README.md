# Everything Has Quality Reforked

Everything Has Quality Reforked is a RimWorld 1.6 mod that expands RimWorld's quality system to more item types, with configurable category rules and individual def controls.

This project is a fork of a fork of Cozarkian's original Everything Has Quality. The refork keeps the original goal intact while updating compatibility, settings usability, and stability for modern RimWorld 1.6 mod lists.

## Features

- **Expanded quality support** for buildings, resources, ingredients, meals, drugs, medicine, apparel, weapons, shells, and other manufactured items.
- **Category-level controls** for enabling or disabling quality by item group.
- **Minimum and maximum quality ranges** for supported item categories.
- **Individual def overrides** for resources, buildings, weapons, apparel, and other items.
- **Searchable settings lists** for large modpacks with many defs.
- **Material and work-table quality factors** for production quality calculations.
- **Passive fermentation quality** that transfers the weighted-average quality of a full wort batch to the finished beer when material quality influence is enabled.
- **Skill and inspiration options** for butchering, chemistry, construction, cooking, gathering, harvesting, mining, and stonecutting.
- **RimWorld 1.6 packaging** through `LoadFolders.xml`, `1.6`, and `Common`.

## Installation

### Steam Workshop

Subscribe on Steam Workshop if using the published Workshop version:

- [Everything Has Quality Reforked](https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766)

### Manual Installation

1. Download or clone this repository.
2. Place the repository folder in your RimWorld `Mods` directory.
3. Enable **Harmony** before this mod in RimWorld's mod list.
4. Enable **Everything Has Quality Reforked**.

## Usage

After enabling the mod, open RimWorld's mod settings and select **Everything Has Quality**.

Use the settings tabs to:

- enable or disable quality by category
- set minimum and maximum quality limits
- configure whether materials, work tables, skill requirements, and inspirations affect generated quality
- enable individual def customization for specific resources, buildings, weapons, apparel, and other items
- search long settings lists when working with large modpacks

When **Resources/Ingredients Affect Quality** is enabled, a full fermenting-barrel batch records the weighted-average quality of the wort it accepts and applies that quality to the finished beer. The active batch data is saved, the barrel's own quality does not affect the result, and the configured quality bounds still apply.

This mod enables quality on more things, but it does not make every quality-bearing item automatically scale every stat. Broader stat scaling requires a separate mod and should be checked for compatibility with your current mod list.

## Configuration

Configuration is stored through RimWorld's normal mod settings system.

Important options include:

- **Quality categories:** toggles for buildings, resources, ingredients, meals, drugs, medicine, apparel, weapons, shells, and manufactured items.
- **Quality bounds:** per-category minimum and maximum quality values.
- **Production inputs:** optional quality influence from materials, work tables, and skill requirements.
- **Supply quality factors:** multipliers for how input quality contributes to output quality.
- **Individual overrides:** per-def enablement lists for selected categories.
- **Inspirations:** custom inspiration toggles for production activities added by this fork.

Some category changes require applying settings in the mod options before affected defs are synchronized.

## Building from Source

Prerequisites:

- .NET SDK capable of building `net48` projects
- RimWorld 1.6 reference package access through the project package references

Build command:

```powershell
dotnet build .\Source\QualityEverything.csproj -c Debug /p:UseSharedCompilation=false
```

The Debug build writes `QualityEverything.dll` to `Common\Assemblies`. It does not copy the assembly to `1.6\Assemblies`.

For the repository's full Release deployment workflow, run:

```powershell
.\deploy.ps1
```

The deployment script uses the shared `..\_Shared\RimWorldModTools.ps1` tooling to build the Release configuration and package the `About`, `Common`, and `1.6` folders.

## Testing and Validation

The primary validation command is the project build:

```powershell
dotnet build .\Source\QualityEverything.csproj -c Debug /p:UseSharedCompilation=false
```

There is no separate automated test suite in this repository.

## Contributing and Forking Policy

> Contributions, issues, and feature requests are welcome.
>
> **Forking Policy:** If your fork primarily consists of bug fixes or feature additions that align with the core vision of this project, I reserve the right to request that your changes be submitted as a Pull Request to this existing codebase rather than being published as a completely separate standalone release, package, listing, or distribution.

## Links


Support me on Ko-fi. This does not imply endorsement by the original authors.

[![Support me on Ko-fi](https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white)](https://ko-fi.com/I7L525WMJ6)
[![GitHub Repository](https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/KyleMHB/EverythingHasQualityReforked)
- **Steam Workshop:** [Everything Has Quality Reforked](https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766)
- **Source Repository:** [KyleMHB/EverythingHasQualityReforked](https://github.com/KyleMHB/EverythingHasQualityReforked)
- **Issue Tracker:** [GitHub Issues](https://github.com/KyleMHB/EverythingHasQualityReforked/issues)
- **Original Mod:** [Cozarkian/EverythingHasQuality](https://github.com/Cozarkian/EverythingHasQuality)
- **Intermediate Fork:** [pudy248/EverythingHasQualityFork](https://github.com/pudy248/EverythingHasQualityFork)
- **Harmony:** [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)

## License

> This project is a fork of **Everything Has Quality** and inherits the original project's MIT license. See the original project for license terms: [Cozarkian/EverythingHasQuality](https://github.com/Cozarkian/EverythingHasQuality).

## Credits

- Cozarkian for the original Everything Has Quality mod.
- pudy248 for the intermediate EverythingHasQualityFork project.
- kylohb for the current RimWorld 1.6 refork.
- Harmony by Brrainz / pardeike for runtime patching support.
