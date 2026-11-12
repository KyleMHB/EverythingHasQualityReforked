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
python Tools/cq_test.py run cq-production --seed 1
python Tools/cq_test.py run cq-market --seed 1
python Tools/cq_test.py run cq-equipment --seed 1
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

### Production and market value batches

Each of these batches needs its own launch, because their settings folders differ: `run-all` refuses to merge them.

**`cq-production`** (mod set `progression`). Production quality stays within the preset's group bounds:

- **`cq.construction-bound`:** a skill-20 colonist completes 30 steel wall frames through `Frame.CompleteConstruction`, the real path the mod redirects. Every wall is at most Excellent (`maxEdificeQuality 4`). A vanilla skill-20 sample of 200 draws must include qualities above Excellent, so the bound is doing something.
- **`cq.manufactured-bound`:** 200 draws from `Quality_Generator.GenerateQualityForDef` for the first loaded def in the Manufactured group, with a skill-20 colonist, are all at most Excellent.
- **`cq.ingredient-minimum`:** 200 draws for the first Ingredient def with a skill-0 grower are all within Normal..Excellent (`minIngQuality 2`). The vanilla skill-0 sample must include qualities below Normal.

**`cq-market`** (mod set `market`, settings `DevTests/modsettings/market`: the Progression preset plus market-value item exceptions `Exclude` for `Apparel_Duster` and `Wall`):

- **`cq.market-off-vanilla`/`cq.market-off-added`:** with scaling off, the duster's MarketValue and the wall's MarketValue and MarketValueIgnoreHp are the same at Awful, Normal and Legendary.
- **`cq.market-default-vanilla`/`-added`/`-building`:** Gun_Revolver (vanilla quality), Steel (added quality) and a steel Door (added quality, building wealth) still rise from Awful to Normal to Legendary.

The cases are mechanical. Construction calls the frame completion directly rather than letting a colonist build. Crafting and harvesting sample the generator that the patched recipe and harvest paths call. Market values are read from stats on made things; nothing is traded.

- **2026-11-09, `20261109-193456-cq-production-s1`:** 3 of 3 passed. Walls: Normal 3, Good 7, Excellent 20 (vanilla skill 20: Normal 5, Good 80, Excellent 95, Masterwork 20). Manufactured def `Wort`: Normal 6, Good 70, Excellent 124. Ingredient def `RawHops`: Normal 200 (vanilla skill 0: Awful 123, Poor 71, Normal 6).
- **2026-11-09, `20261109-193508-cq-market-s1`:** 5 of 5 passed. Duster 156/156/156 and wall 9.99 for all three qualities with scaling off. With scaling on: revolver 67.7/135.4/675, steel 0.95/1.9/2.85, door 25.28/50.56/75.84.
- **Setup error found on the way:** the first `cq-market` run (`20261109-193419-cq-market-s1`) wrote the item exceptions as `false`. Category and item rules take `RuleChoice` names (`Include`, `Exclude`, `Inherit`); only groups take booleans. The parser silently ignores other values. The first `cq-production` run picked `ComponentIndustrial`, which is in the Stuff group; the batch now looks the def up by group.
- **Not covered:** material and table supply factors, skill requirements, inspirations, recipe and harvest jobs run by colonists, trade prices and vanilla-quality removal.

### Settings screens tour

**`cq-ui`** (mod set `default`) is a diagnostic batch for looking at the settings screens, not a gameplay check. It opens the mod settings page, applies Mild to the draft, opens the advanced editor, visits its four tabs and the preset preview, then discards the draft and closes the windows, so nothing is saved. Its one case passes when all ten screens opened. Layout is judged from the recording:

```powershell
python Tools/cq_test.py run cq-ui --seed 1 --record --show-ui --ffmpeg "<path to ffmpeg.exe>"
```

Each screen is held for four seconds, so frames at 2.5 s, 6.5 s and so on show one screen each. The tour drives the windows through reflection; it does not click, type, save or use a narrow window.

- **2026-11-11, `20261111-162513-cq-ui-s1`:** passed on RimWorld 1.7 with Core, Harmony and this mod. Frames are in `artifacts/ui-check-20261111`. The first run (`20261111-162216-cq-ui-s1`) showed left-aligned selected tabs, an empty Tasty ingredients group, blocked effects listed for every item and lower-case quality names; all four were fixed before this run.
- **Not covered:** clicking and typing, the dropdown menus, the scope picker, the bulk and details pop-ups, saving from either screen, narrow windows, a large mod list, and RimWorld 1.6.

### Saving from the settings screens

**`cq-settings`** (mod set `default`) checks the save lifecycle of both screens. It opens the real windows and calls the same edit and close methods the buttons call; nothing is clicked or typed, and the game is not restarted. "On disk" means the settings file's stored configuration, parsed and written back out, equals the expected one.

- **`cq.save-simple-page`:** after an edit, closing the mod settings page saves it. Saved and the file equal the edited draft, the running configuration is unchanged and a restart is pending.
- **`cq.save-editor-guard`:** while the advanced editor is open with unsaved changes, the host dialog's save leaves Saved and the file unchanged.
- **`cq.save-editor-prompt`:** asking the editor to close with unsaved changes is refused and opens the save, discard or keep editing prompt.
- **`cq.save-editor-cancel`:** Cancel closes the editor, restores the draft to Saved and writes nothing.
- **`cq.save-editor-save`:** Save and close closes the editor; Saved and the file equal the edited draft and the running configuration is unchanged.

### 2026-11-11 runs

- **Environment:** RimWorld 1.7, seed 1, branch `codex/quality-merged` at `48856f1` plus the `cq-settings` batch. The installed package was built from the same source before the commit.
- **Checks:** `check-local.ps1` passed (7 programs) and `check-game-adapters.ps1` passed (19 IL transformations). 1.6 and 1.7 Release builds had 0 warnings.
- **`20261111-163204-cq-settings-s1-c78d5608`:** 5 of 5 passed.
- **`20261111-163216-cq-fresh-s1-f34a6908`:** 3 of 3 passed. It reused the `cq-settings` process, whose saved settings differed from defaults by then; the running configuration is fixed at startup, which `cq-settings` itself asserts.
- **`20261111-163220-cq-progression-s1-0c0b184b`:** 6 of 6 passed. **`20261111-163233-cq-production-s1-f43a1f88`:** 3 of 3 passed. **`20261111-163237-cq-market-s1-559515c8`:** 5 of 5 passed. `release-check` passed.
- **Not covered:** clicking and typing in either screen, a real restart after saving from the new screens (`cq-progression` shows that a settings file drives behaviour after launch), RimWorld 1.6 in game, narrow windows and a large mod list.

### 2026-11-12 runs

- **Environment:** RimWorld 1.7, seed 1, `main` at `4776180` (the published Workshop release, with the gear-effect conflict fix `0ce17aa`). The deployed package was the one uploaded to the Workshop.
- **Checks:** 1.6 and 1.7 Release builds had 0 warnings; all eight check programs passed, including `check-game-adapters.ps1` (19 IL transformations).
- **`20261112-185455-cq-fresh-s1-cbaec1bf`:** 3 of 3 passed. **`20261112-185521-cq-settings-s1-617014d9`:** 5 of 5 passed. **`20261112-185525-cq-progression-s1-cf06b73c`:** 6 of 6 passed. **`20261112-185539-cq-production-s1-f961609d`:** 3 of 3 passed. **`20261112-185543-cq-market-s1-2458b6d4`:** 5 of 5 passed. `release-check` passed.
- **Not covered by these five:** the equipment effects. `cq-equipment` below was added for that.

### Equipment effects alongside other mods' patches

**`cq-equipment`** (mod set `equipment`: the Progression settings plus `Mod_ConfigurableQuality_ForeignPatchProbe.xml`). The marker makes `DevTests/ForeignPatchProbe.cs`, a `Mod` subclass in the developer DLL, put no-op postfixes owned by `devtests.foreigncache` on `CompQuality.SetQuality`, `Thing.Notify_Equipped`, `Thing.Notify_Unequipped` and `StatWorker.StatOffsetFromGear` before the mod's startup registration. This is the situation from the Steam report (Performance Fish patches `SetQuality`).

- **`cq.foreign-hooks`:** the probe ran before the mod owned any of the four methods, and both now patch each one.
- **`cq.gear-available`:** Equipment Social and Movement, melee and ranged cooldown and trap damage are available. No gear effect is marked as handled elsewhere or carries the conflict note. Work, Medicine and Abilities are honestly unavailable (no Core item adds those offsets).
- **`cq.gear-curve-social`/`cq.gear-curve-toxic`:** `StatOffsetFromGear` for `Apparel_CowboyHat` SocialImpact (+0.10) and `Apparel_ClothMask` ToxicEnvironmentResistance (+0.5) follow the Mild curve relative to Normal.
- **`cq.gear-worn`:** a colonist wears the hat, it goes from Normal to Legendary, and then it is removed. The hat's share of SocialImpact (worn minus bare) at Legendary is 1.2× its share at Normal, as the Mild curve gives. The share is compared rather than the raw rise because SocialImpact multiplies base plus gear offsets by the colonist's Social skill and capacity factors. SocialImpact is not cacheable, so this does not show the `SetQuality` cache refresh working.

Runs, RimWorld 1.7, seed 1:

- **2026-11-12, `20261112-190314-cq-equipment-s1-80507c3e`** (release `4776180`): 5 of 5 passed. Offsets 0.095/0.098/0.1/0.102/0.105/0.11/0.12 and 0.475/…/0.6; worn SocialImpact 1.174 → 1.196.
- **2026-11-12, `20261112-190355-cq-equipment-s1-ef179f84`** (negative control, pre-fix `a5b8bf0`): 2 failed, 3 skipped. The mod never patched the equip hooks or `StatOffsetFromGear`, all five gear effects were marked as handled elsewhere, and cooldowns and trap damage were unavailable, matching the Steam report. The release package was redeployed afterwards.
- **2026-11-12, `20261112-202830-cq-equipment-s1-77f9d5ac`** (release, RimWorld 1.7.5005): `cq.gear-worn` failed. SocialImpact went 0.902 → 0.918, a rise of 0.016 against the hat's 0.02 offset difference. The colonist's Social skill factor (about 0.8) scales gear offsets; the first run's colonist had a factor of about 1.1 (rise 0.022), which passed only at the edge of the tolerance. This was a test flaw, not a mod fault, and the case was changed to the share ratio described above.
- **2026-11-12, `20261112-202948-cq-equipment-s1-a3eaa5f1`** (release, RimWorld 1.7.5005, revised case): 5 of 5 passed. SocialImpact bare 0.8475, Normal hat 0.9323, Legendary hat 0.9492; share ratio 1.2. Release check passed afterwards.
- **Not covered:** Combat Extended's MoveSpeed and WorkSpeedGlobal workers (CE not installed), a real quality provider and the Harmony-ID note it causes, and the cache refresh for a cacheable gear stat.
