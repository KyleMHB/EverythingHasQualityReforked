# Everything has quality and it matters

Everything has quality and it matters expands item quality and offers optional quality effects for RimWorld 1.6 and 1.7. It is a fork of pudy248's fork of Cozarkian's Everything Has Quality.

## Features

- Choose which eligible buildings, resources, food, medicine, drugs, weapons, apparel and shells receive quality, using group, category and item rules.
- Find items by translated name, category or source mod. Review selections across searches, apply batch changes and undo draft edits.
- Enable optional effects for durability, work and research speed, doors, power systems and bed rest.
- Adjust food nutrition and poisoning risk, medicine potency and supported drug benefits. Native drug penalties remain in place.
- Scale supported existing equipment bonuses, combat cooldowns and trap damage.
- Adjust production quality using materials, work tables, skills and inspirations, including quality transfer from wort to beer.

## Install from Steam Workshop

Subscribe to [Everything has quality and it matters](https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766) and [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077). Enable Harmony before this mod.

## Usage

1. Open RimWorld's mod settings and select **Everything Has Quality**.
2. Use **Quality assignment** to choose which items receive quality.
3. Use **Quality effects** to enable effects and choose their targets. Effects start disabled on new installations and upgrades; preview **Mild**, **Medium** or **Progression** for a starting point.
4. Use **Production** to adjust generated quality and its bounds.
5. Save your settings and restart RimWorld to activate them.

## Configuration

Individual item rules override category and group defaults. Quality assignment and effect participation have separate rules. Excluding an item preserves quality supplied by RimWorld or another mod, and existing native quality curves stay in place.

Batch actions apply to **selected items**, including selections hidden by current filters. **Select all results** adds the current results to your selection. Use **Review selected** before applying a change and **Undo** to reverse draft edits. **Discard** restores saved settings.

Support details for individual items show potentially relevant effects and recorded exclusions. Supported factory and Processor Framework outputs retain their own quality policies. Storage capacity and installed-bionic effects are omitted. See [the settings and removal guide](docs/quality-guide.md) for effect limits, preset details and production rules.

## Existing saves and removal

Quality separates stacks by tier, which can increase stack counts for high-volume resources. Back up your save before removing quality support or uninstalling: saved quality values can be lost, and reducing durability or battery capacity can clamp excess hit points or charge. Read [the removal guidance](docs/quality-guide.md) before changing an existing colony's quality support.

## Build a local package

Use a .NET 10 SDK. The 1.6 target uses .NET Framework 4.8 reference packages from NuGet. The 1.7 target needs a local 1.7 game directory containing `Assembly-CSharp.dll`, `UnityStubs.dll` and `Waylith.UnityCompat.dll`. Package restore needs access to NuGet or an existing local package cache.

From this repository, run:

```powershell
.\package-local.ps1 -DotNetPath 'C:\path\to\dotnet.exe' -ManagedPath 'C:\path\to\RimWorld'
```

The package is `artifacts\package\EverythingHasQualityReforked`. Copy that folder into RimWorld's `Mods` directory, then enable Harmony before this mod. Keep its `LICENSE` and `build-manifest.json` files with the package. The manifest records the source commit, local changes, build SDK, game references and file hashes. A source checkout does not contain ready-to-load binaries.

For one target without packaging, run:

```powershell
.\build-local.ps1 -GameVersion 1.6 -DotNetPath 'C:\path\to\dotnet.exe'
.\build-local.ps1 -GameVersion 1.7 -DotNetPath 'C:\path\to\dotnet.exe' -ManagedPath 'C:\path\to\RimWorld'
```

Builds write only to `artifacts\1.6` or `artifacts\1.7` and read game references without changing the game installation. The package shares `Common` content and loads one `QualityEverything.dll` for each game version. Harmony comes from the separate Harmony mod.

## Install a prepared local package

Close RimWorld before replacing an installed copy. Back up that copy and any saves you intend to use. To replace this mod's package folders at an explicit destination, run:

```powershell
.\deploy.ps1 -Destination 'C:\path\to\RimWorld\Mods\EverythingHasQualityReforked'
```

The destination folder must be named `EverythingHasQualityReforked`. Deployment uses the prepared package and replaces its owned content folders, including obsolete shared assemblies. It does not build, select a mod list or launch RimWorld.

## Fork history

This refork continues [pudy248's EverythingHasQualityFork](https://github.com/pudy248/EverythingHasQualityFork), based on [Cozarkian's Everything Has Quality](https://github.com/Cozarkian/EverythingHasQuality).

## Contributing

Contributions, issues, and feature requests are welcome. Report problems or suggest changes through [GitHub Issues](https://github.com/KyleMHB/EverythingHasQualityReforked/issues).

## Credits

- Cozarkian for the original Everything Has Quality mod.
- pudy248 for the intermediate EverythingHasQualityFork project.
- kylohb for maintaining the current refork.
- Harmony by Brrainz / pardeike for runtime patching support.

## License

This project inherits the original Everything Has Quality project's MIT license. See [LICENSE](LICENSE) and the [original project](https://github.com/Cozarkian/EverythingHasQuality).

## Links

Support me on Ko-fi. This does not imply endorsement by the original authors.

[![Support me on Ko-fi](https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white)](https://ko-fi.com/I7L525WMJ6)
[![GitHub Repository](https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/KyleMHB/EverythingHasQualityReforked)

- **Steam Workshop:** [Everything has quality and it matters](https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766)
- **Source Repository:** [KyleMHB/EverythingHasQualityReforked](https://github.com/KyleMHB/EverythingHasQualityReforked)
- **Issue Tracker:** [GitHub Issues](https://github.com/KyleMHB/EverythingHasQualityReforked/issues)
- **Original Mod:** [Cozarkian/EverythingHasQuality](https://github.com/Cozarkian/EverythingHasQuality)
- **Intermediate Fork:** [pudy248/EverythingHasQualityFork](https://github.com/pudy248/EverythingHasQualityFork)
- **Harmony:** [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)

- [Better Pawn Control Forked](https://steamcommunity.com/sharedfiles/filedetails/?id=3724294345)
- [Cargo Containers Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3714050938)
- [Gear Up and Go Reforked](https://steamcommunity.com/sharedfiles/filedetails/?id=3724312502)
- [Outfit Stands Plus Forked](https://steamcommunity.com/sharedfiles/filedetails/?id=3724311713)
- [Stats Forked](https://steamcommunity.com/sharedfiles/filedetails/?id=3712317437)
