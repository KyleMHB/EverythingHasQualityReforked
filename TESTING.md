# Testing

## Automated checks

From the repository root, with the local .NET 10 SDK in `.dotnet/sdk10` for the 1.7 target:

```powershell
.\build-local.ps1 -GameVersion 1.6 -DotNetPath .dotnet\sdk10\dotnet.exe
.\build-local.ps1 -GameVersion 1.7 -DotNetPath .dotnet\sdk10\dotnet.exe -ManagedPath E:\Steam\steamapps\common\RimWorld
.\check-local.ps1
.\check-game-adapters.ps1 -DotNetPath .dotnet\sdk10\dotnet.exe -ManagedPath E:\Steam\steamapps\common\RimWorld
```

`check-local.ps1` runs the seven `net9.0` check programs with the system SDK (the local .NET 10 SDK has no .NET 9 runtime). `package-local.ps1` builds both targets and stages `artifacts/package/ConfigurableQuality`. `deploy.ps1 -Destination <Mods>\ConfigurableQuality` validates and installs that package.

## In-game developer tests

`DevTests/` uses the shared runner in the sibling `RimWorldDevTests` checkout. The game runs in an isolated save-data folder, so the player's mod list, settings and saves are not touched.

- **Install comparison:** `devtests.json` sets `runtimeRoot` to `artifacts/package/ConfigurableQuality`, so the installed mod is compared with the built package, not the repository layout.
- **Install folder:** tests use a separate `Mods/ConfigurableQuality` install with package ID `KyleMHB.ConfigurableQuality`. An older `Cozarkian.QualityFramework` build in `Mods/EverythingHasQualityReforked` can stay in the player's own mod list.

Settings only apply after a restart, so each batch has its own mod set and launch, and `run-all` is refused. Run the batches separately:

```powershell
.\package-local.ps1 -ManagedPath E:\Steam\steamapps\common\RimWorld -DotNetPath .dotnet\sdk10\dotnet.exe
.\deploy.ps1 -Destination E:\Steam\steamapps\common\RimWorld\Mods\ConfigurableQuality
dotnet build DevTests/ConfigurableQuality.DevTests.csproj -c Release
python Tools/cq_test.py run cq-fresh --seed 1
python Tools/cq_test.py run cq-progression --seed 1
python Tools/cq_test.py close-session
python Tools/cq_test.py release-check
```

**`cq-fresh`** (mod set `default`, no settings file). This checks "a fresh installation leaves gameplay unchanged" (`docs/quality-guide.md`):

- **`cq.loaded`:** Harmony patches and the mod's `MaxHitPoints` stat parts are present.
- **`cq.fresh-assignment`:** `Wall`, `Steel`, `MealSimple` and `MedicineIndustrial` gain no quality, and `Apparel_Duster` and `Gun_Revolver` keep native quality.
- **`cq.fresh-effects`:** the mod's own `MaxHitPoints` contribution is ×1 for the duster and revolver at Awful and Legendary.

**`cq-progression`** (mod set `progression`, with `DevTests/modsettings/progression` holding a copy of the Progression preset's settings file):

- **`cq.progression-assignment`:** the Building and Stuff groups give walls and steel quality. Meal and Medicine stay off, and native quality is kept.
- **`cq.edifice-bounds`:** `maxEdificeQuality 4` holds a wall at Excellent when Excellent, Masterwork or Legendary is requested.
- **`cq.building-durability`:** a wall's `MaxHitPoints` contribution follows the Mild curve up to the bound: 0.95/0.98/1/1.02/1.05/1.05/1.05.
- **`cq.apparel-durability`:** the unbounded duster follows the full Mild curve, 0.95/0.98/1/1.02/1.05/1.10/1.20.
- **`cq.wall-hitpoints`:** placed steel walls read 300 HP at Normal and 315 at Excellent, a ratio of 1.05.
- **`cq.reload`:** the Excellent wall keeps its quality and 315 maximum hit points after a save and reload.

The cases are mechanical. They inspect defs, and set quality directly on made or god-mode-placed things. Production, construction, market value, vanilla removal, the other effects and the editor UI are not exercised. A contribution is measured by passing 100 through only this mod's stat parts.

### 2026-11-09 runs

- **Environment:** RimWorld 1.7.5000 rev1275, seed 1. The source was branch `codex/quality-merged`: merge `51353b9` of `codex/quality-ui-integration` into `codex/quality-repair-expansion`, plus the harness files.
- **Merge checks:** 1.6 and 1.7 Release builds had 0 warnings. `check-local.ps1` passed (7 programs) and `check-game-adapters.ps1` passed (19 IL transformations). `package-local.ps1` validated the package, and `deploy.ps1` installed it to `Mods/ConfigurableQuality`.
- **Run `20261109-190233-cq-fresh-s1`:** 3 of 3 passed (61 patched methods).
- **Run `20261109-190246-cq-progression-s1`:** 6 of 6 passed (values above). `release-check` passed.
- **First attempt, `20261109-190041-run-all-s1`:** 2 passed, 5 failed. The failures were test errors, not mod defects:
  - `run-all` merged both mod sets into one launch, so `cq-fresh` ran with the Progression settings.
  - The Progression cases expected Legendary walls and missed the preset's Excellent bound.
  - The launcher now refuses such a `run-all`, and the cases follow the preset's bounds.
- **Not covered:** production and construction quality, market value, vanilla-quality removal, the other effects, the editor, older-settings migration on a real config, and the full mod list.

### 2026-11-09 merge review

Review of merge `51353b9` against both parents, focused on what the resolution dropped from `codex/quality-repair-expansion`:

- **Rule coverage kept:** the redesigned workbench reads and edits all four rule sets (assignment, effects participation, market value and vanilla removal). The removal confirmation and the vanilla-removal outcome markers survive. `Mod_SettingsUtility.RestoreDefaults` now delegates to `QualityEditorActions.Reset`. That resets the same fields the old method listed and keeps `LegacyBackup` and `InactiveLegacy`, so a reset doesn't lose migration data.
- **Ported:** the selection-checkbox change from `a8e016f` (an outlined empty box and the `SelectionHelp` tooltip) now applies to the workbench table. The dropped `VanillaRemovalHelp` and `MarketValueHelp` texts are now tooltips on the Quality and Price column headers; Mod effects shows `ExceptionHelp`.
- **Text fix:** `MarketValueHelp` said presets leave market-value rules unchanged. That is still true for the built-in presets, but `ApplyPreset` replaces the whole draft for a user preset. The English text now says so.
- **Left as is:** `Window_QualityOptions.DrawRules` and its `participation`/`pricing`/`removing` modes are no longer reachable from the 4-tab window; only `Window_QualityItem` still reads those flags. This is dead code to remove in a later cleanup, not a behaviour problem.
- **Checks after the port:** `package-local.ps1` built 1.6 and 1.7 with 0 warnings and validated the package. `check-local.ps1` passed (7 programs). `deploy.ps1` installed it, and `cq-fresh` (`20261109-193113-cq-fresh-s1`, 3/3) and `cq-progression` (`20261109-193125-cq-progression-s1`, 6/6) passed. The tooltips and checkbox drawing were not checked visually in game.
