# Everything Has Quality Reforked

Everything Has Quality Reforked expands item quality and offers optional quality effects for RimWorld 1.6 and 1.7. It is a fork of pudy248's fork of Cozarkian's Everything Has Quality.

## Install from Steam Workshop

Subscribe to [Everything Has Quality Reforked](https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766) and [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077). Enable Harmony before this mod.

## Build a local package

Use a .NET 10 SDK. The 1.6 target uses .NET Framework 4.8 reference packages from NuGet. The 1.7 target needs a local 1.7 game directory containing `Assembly-CSharp.dll`, `UnityStubs.dll` and `Waylith.UnityCompat.dll`. Package restore needs access to NuGet or an existing local package cache.

From this repository, run:

```powershell
.\package-local.ps1 -DotNetPath 'C:\path\to\dotnet.exe' -ManagedPath 'C:\path\to\RimWorld'
```

The package is `artifacts\package\EverythingHasQualityReforked`. Copy that folder into RimWorld's `Mods` directory, then enable Harmony before this mod. A source checkout does not contain ready-to-load binaries.

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

Read [the settings and removal guide](docs/quality-guide.md) before changing an existing colony's quality support.
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

- [Better Pawn Control Forked](https://steamcommunity.com/sharedfiles/filedetails/?id=3724294345)
- [Cargo Containers Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3714050938)
- [Gear Up and Go Reforked](https://steamcommunity.com/sharedfiles/filedetails/?id=3724312502)
- [Outfit Stands Plus Forked](https://steamcommunity.com/sharedfiles/filedetails/?id=3724311713)
- [Stats Forked](https://steamcommunity.com/sharedfiles/filedetails/?id=3712317437)

## License

> This project is a fork of **Everything Has Quality** and inherits the original project's MIT license. See the original project for license terms: [Cozarkian/EverythingHasQuality](https://github.com/Cozarkian/EverythingHasQuality).

## Credits

- Cozarkian for the original Everything Has Quality mod.
- pudy248 for the intermediate EverythingHasQualityFork project.
- kylohb for the current RimWorld 1.6 refork.
- Harmony by Brrainz / pardeike for runtime patching support.
