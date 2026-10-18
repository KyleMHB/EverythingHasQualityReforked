# Optional quality effects: Workshop research and recommendations

Research date: 18 October 2026. Status: research and recommendations complete for the documented search scope. Public Workshop completeness cannot be guaranteed. Research only: no mod code, settings, balance, assemblies, or release files were changed by this work. Existing working-tree edits were preserved.

This report records the research phase. The [approved plan](../../plan%202026-10-18.md) defines the selected implementation; its choices supersede recommendations here. [Conditional-effect decisions](2026-10-18-conditional-effects.md) explain the storage and installed-bionic outcomes. Raw source/IL captures remain local under `.scratch/quality-effects/`.

## Findings that matter most

A small optional effects layer is feasible. Most common effects can use shared stats and generic definition classification, with category switches and per-item overrides. HP, storage capacity, battery capacity, installed bionics, and active drug effects have lifecycle consequences; they cannot all be treated as interchangeable stat multipliers.

The inspected HP mods commonly heal an object whenever quality is assigned. Quality Expanded also includes periodic quality-degradation scans, source/Workshop version differences, crossed food/trap settings and reversed fallback cooldown curves. Those are reasons to implement the intended mechanics independently rather than copy an entire framework. The pinned source and downloaded-package evidence below distinguishes these findings from author claims and unverified failure reports. Current package evidence takes precedence over the initial description-only survey and stale public source snapshots.

## Requirements retained

- Optional effects for categories including buildings, apparel, weapons, resources, food, medicine, drugs, and other eligible items.
- Blacklist and whitelist controls for individual definitions.
- Vanilla and modded content, broad compatibility, maintainability, performance, and proportional complexity.
- Consolidate useful quality-effect mechanics; coexistence with overlapping quality mods is not a requirement.
- Support RimWorld 1.6 and 1.7.
- Prefer safe midgame removal and identify unavoidable limits.
- Investigate the supplied four mods plus discoverable related Workshop implementations; downloading for inspection is allowed. No implementation yet.

## Recommendations for the later design

These are recommendations, not decisions accepted by the user.

1. **Separate quality assignment from quality effects.** Existing category and per-def settings determine whether a definition receives quality. Add an independent optional effects policy so disabling added weapon effects preserves ordinary vanilla weapon quality. Keep the new master switch off by default for existing users.
2. **Use a small explicit effects registry.** Each supported effect needs a stable identity, applicable categories, a seven-tier curve, and a known consumer. Resolve category and per-def filters after defs load and when settings change. Keep common stat evaluation to an eligibility lookup and factor lookup. Avoid a general-purpose scripting/rules engine.
3. **Offer category defaults with explicit item overrides.** A three-state per-definition control (inherit / include / exclude) can express blacklist and whitelist behavior without competing lists. A separate whitelist-only mode could support opt-in mod lists. Recommend master-off always wins, explicit exclude wins over include if separate lists are retained, and item include overrides a category default only within effects that support that item. Confirm the exact precedence during later design.
4. **Prioritize the shared-stat effects, without discarding broader research.** HP, work/research speed, doors, weapon cooldowns, traps, medicine, nutrition, poisoning chance, and selected equipment offsets are the initial candidates. Registry entries must identify already-vanilla effects so a new multiplier does not accidentally double them. Preserve vanilla behavior by default; changing vanilla curves is a distinct optional feature.
5. **Use bounded adapters for mechanics outside stats.** Power production/consumption, battery efficiency/capacity, turret overrides, shelf capacity, drug effects, bionic efficiency/HP, and facility-to-bed bonuses need individual treatment. Their research remains in scope. Do not promise every custom modded system participates just because its definition has quality.
6. **Make lifecycle policy explicit.** HP changes must account for generation, construction, damaged items, quality upgrades, settings changes, loading, and removal. Never use an unconditional full-heal hook. Compare damage-ratio preservation against absolute-HP preservation/clamping before implementing. Battery charge, excess shelf stacks, installed parts and active hediffs need their own policies.
7. **Prefer derived values and vanilla persistence.** Ordinary effects should not need new saved classes or per-tick scans. Quality degradation, affixes, and installed-health effects introduce persistent consequences; make those separate opt-ins if selected. A disable-and-normalize path before removal may improve safety, but must cover inventories, equipment, containers, caravans and world-held things. No clean-uninstall guarantee has been demonstrated.
8. **Validate versions and consumers separately.** Installed runtime is 1.7.4981; project references are 1.6.4633. A reference-only DLL proves signatures, not runtime parity. Validate actual damage, repair, production, consumption, ingestion, equipment offsets, save/load, and disable/remove transitions on both versions. Do not change global stat caps or shared component properties as a shortcut.

## Coverage and limits

The survey covers fourteen principal mod entries plus a shelf continuation, with an adjacent furniture-facility repository separately identified. Fourteen Workshop packages were downloaded for inspection, alongside the already-installed Quality And Durability package. The four supplied examples all have actual source/XML or binary evidence. Public-source and package inspections cover HP/degradation, weapons/apparel, production/research, doors, power, food, drugs, storage, bionics and affixes. A local metadata pass inspected 1,516 Workshop About.xml files; quality-of-life, birth/tend quality and presentation-only matches were separated from gameplay effects. Search coverage and unverified discovery leads are retained below. This is a broad evidence-backed survey, not proof that every relevant Workshop item exists in the catalogue.

Actual files establish more than descriptions: Stronger Scaling replaces whole stat definitions; Quality Matters targets 65 named stats; current Quality Expanded differs from its stale public source; standalone Power/Workbench DLLs expose concrete adapters; drugs and bionics persist health state; Infusion Continued delivers separate 1.6 and 1.7 binaries. File identities and SHA-256 values are recorded in [the package manifest](../../.scratch/quality-effects/package-manifest.json).

No downloaded mod was enabled or executed. Static inspection identifies likely failure paths, not reproduced in-game bugs or compatibility guarantees. License permission is unknown where no license was supplied; recommend independent implementation of behavior. Quality Furniture Matters keeps its C# source private and is an adjacent repository lead, not a verified Workshop listing. Actual 1.6 runtime bodies were unavailable; installed runtime inspection used 1.7.4981 and cached 1.6 reference signatures. These limits do not prevent research recommendations but must remain release-validation requirements for any later implementation.

## Feature map for later design

| Effect family | Typical implementation | Main maintenance/transition concern |
| --- | --- | --- |
| Armor, insulation, accuracy, damage, comfort, beauty, rest, recreation | Existing vanilla quality parts; optional curve replacement if separately chosen | Preserve vanilla parts and caps; avoid duplicate scaling |
| Work/research, doors, cooldowns, traps, medicine, food nutrition | Shared stat parts with filtered eligibility | Confirm actual consumer and beneficial direction; equipment offsets use a distinct request path |
| HP/durability | MaxHP stat plus creation/change/load lifecycle policy | Preserve chosen damage policy; cache invalidation; excess absolute HP on removal |
| Food poisoning | Inherent-ingestion stat for simple effect; separate contamination hooks for broader behavior | Cook contamination, saved poison and rotten food are separate pathways |
| Power production/consumption | Dedicated power getter/consumer adapter | Positive output versus negative demand; dynamic producers; EMP/broken state |
| Battery efficiency and capacity | Dedicated adapters across charging, accepted energy, percentage, saves and UI | Shared props, mismatched formulas, charge loss and vanilla save clamps |
| Turret cooldown overrides | Building-specific cooldown adapter | Distinguish building override from contained gun; prevent double scaling |
| Shelf capacity | Storage-capacity adapter | Compose with storage mods; quality invalidation and overflow when reduced |
| Drug bonuses, duration, needs, addiction, overdose | Event-time ingestion plus persistent health effects where needed | Named drug schemas, stage semantics, active-effect migration and removal |
| Installed bionics | Surgery/removal transfer plus health-capacity/HP adapters | Saved quality, body-part matching, health-framework dependency, shared-def mutations |
| Infusions/affixes | Quality controls slots and random effect tiers; separate equipment system | Custom saved components, jobs/items/health state, substantial extra scope |
| Linked furniture quality | Facility quality contributes to bed rest stat | Dependent-object cache invalidation and contribution caps |

Every family remains researched. This table does not authorize implementation or silently remove advanced effects from future consideration. A small first implementation and later optional families are a recommendation to discuss after this research.

## Workshop discovery

## Workshop quality-effects survey

Research date: 2026-10-18. This is a discovery and first-party-description evidence report, not an exhaustive catalogue or a claim of tested compatibility. No mod implementation was changed.

**Later package evidence supersedes the initial version-discovery gap below:** `package-health-affixes.md` records downloaded Infusion 2 Continued packages with separate real 1.6/1.7 DLLs, runtime reference differences, detailed persistence/effects, and current LGPL source. It also verifies the bionics DLL's key source mechanisms and the packaged changelog's dropped 1.5 support. Read that supplement alongside this earlier discovery record.

### Evidence levels and coverage

- **D**: initial discovery used the Workshop author's description; later implementation/package sections provide stronger evidence where available.
- **M**: locally installed metadata inspected; proves packaged declarations, not runtime behavior.
- **R**: linked repository inspected at root/README level; code internals still require the separate implementation-evidence report.
- User comments identify investigation targets and failure scenarios only; they do not establish a reproduced bug.
- Search began with all four user seeds, followed author links and the related-mod links, and searched for quality effects across buildings, apparel, weapons, durability, cooldown, medicine, food/nutrition/poisoning, drugs/addiction/overdose, storage/shelves, power, production/research benches, bionics, plants/yields, fermentation, recreation, range and fuel consumption.
- Web query families included `site:steamcommunity.com/sharedfiles "quality" "power" Rimworld`, `"Quality Doors"`, `"Quality Cooldown"`, `"Quality Workbenches"`, `"quality" "medicine" Rimworld`, `"quality" "food poisoning"`, `"quality bionics" remastered`, `Rimworld mod "quality" "storage capacity"`, `Rimworld mod "quality" "drugs" "effects"`, `Rimworld mod "quality" "fermentation"`, `Rimworld mod "quality" "recreation"`, `"Quality Affects"`, and variants excluding QOL/collections.
- Generic quality queries are heavily contaminated by “quality of life”, medical tend quality, birth quality, and meal class terminology. Those do not necessarily involve `CompQuality`/Awful–Legendary and were excluded from the feature inventory.
- Direct Workshop browse pagination for `quality` returned inaccessible URLs. A cached Workshop browse result exposed two additional leads (CE-Firearms Quality+ and Quality Based Durability), but reopening it returned different contents; neither lead is verified enough to classify mechanics.
- Nested GitHub tree/raw/API requests repeatedly failed through the browsing tool. A shell GitHub API read failed DNS. Source repositories below remain explicit follow-up targets, not claimed source inspections. Another assigned agent is inspecting downloaded source for the main seeds.
- The initial description-only discovery found no 1.7 declaration. Subsequent downloaded Infusion 2 Continued files establish explicit 1.6/1.7 packaging with separate binaries; see the package section. Version declarations and binary references still do not establish in-game compatibility.
- Steam's extracted pages repeatedly contain generic “removed/incompatible” notices alongside public descriptions. Those notices are not treated as authoritative proof of actual delisting or incompatibility.

### Confirmed feature families

| Mod | Quality-driven behavior | Version evidence | Evidence/source |
| --- | --- | --- | --- |
| Stronger Quality Scaling | Strengthens quality variation for clothing, armor, weapons and constructed objects. Workshop text delegates complete stat list to files/spreadsheet; do not infer its entire stat coverage from the short description. | Title and tags explicitly 1.6; author says updated July 2025. | D: [Workshop 1206001612](https://steamcommunity.com/sharedfiles/filedetails/?id=1206001612) |
| Quality Matters | Adds quality factors to item stats and equipment buffs. Author identifies XML patch implementation and unsupported negative stat offsets. Mod-added stats were future work. | Tagged 1.3; no author 1.6/1.7 declaration observed. | D: [Workshop 2585864821](https://steamcommunity.com/sharedfiles/filedetails/?id=2585864821) |
| Quality Expanded [1.4+ Fork] | HP, quality deterioration from damage, work/research/doors, power production/use/battery efficiency, melee/ranged/turret cooldown, medicine, traps, nutrition and poisoning; configuration also controls some vanilla effects. | Tags 1.4/1.5/1.6. | D/R: [Workshop 2885572675](https://steamcommunity.com/sharedfiles/filedetails/?id=2885572675), [source](https://github.com/pudy248/QualityFrameworkFork) |
| Quality And Durability (Continued) | Weapon and armor maximum HP, with configurable tier multipliers. | Installed About.xml lists 1.2–1.6 and modVersion 1.6.0. | M/R: [Workshop 2572086023](https://steamcommunity.com/sharedfiles/filedetails/?id=2572086023), [source](https://github.com/emipa606/QualityAndDurability) |
| Quality Affects HP | All things with quality and HP, including furniture/art and quality added by other mods. Configurable multiplier curves, with restart. | Workshop tags 1.4/1.5/1.6. | D: [Workshop 2898409109](https://steamcommunity.com/sharedfiles/filedetails/?id=2898409109) |
| Quality Doors | Adds door quality, HP scaling and open speed; also minification and construction/material work changes. Derived vanilla-door definitions inherit support; explicit mod patches exist. | Author declares 1.5 update/HP fix; 1.6 only user reports in viewed page. | D: [Workshop 2455489002](https://steamcommunity.com/sharedfiles/filedetails/?id=2455489002) |
| Quality Cooldown | Melee and ranged cooldown; ranged also depends on weapon condition. | Page last update 2022; 1.6 working reports are comments, not an inspected current build. | D/R: [Workshop 1543069597](https://steamcommunity.com/sharedfiles/filedetails/?id=1543069597&searchtext=quality), [source](https://github.com/dninemfive/qualitycooldown) |
| Quality Workbenches | Detects worktables and research benches; tier factors affect their respective speeds, plus a shared additive beauty bonus. | Tagged 1.6. | D: [Workshop 3551543324](https://steamcommunity.com/sharedfiles/filedetails/?id=3551543324), [author changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3551543324) |
| Quality Power | Separate generator and battery multipliers plus beauty; settings apply immediately and explicitly target those building types. | No 1.7 declaration observed; exact tags not captured in retrieved description. | D: [Workshop 3552862196](https://steamcommunity.com/sharedfiles/filedetails/?id=3552862196) |
| Quality Affects Nutrition and Food Poison Chance | Patch extends food nutrition and item poisoning-chance stat factors; requires another mod to give food quality. | Author explicitly states June 27, 2025 update for 1.6. | D: [Workshop 2754055111](https://steamcommunity.com/sharedfiles/filedetails/?id=2754055111+packageId%3A+qanafpc.velcroboy333&l=indonesian) |
| Quality Drugs | Quality-dependent drug benefits/penalties, duration, addiction and overdose. Limited named vanilla/VBE coverage; more invasive than a normal item StatPart. | Tagged 1.6. | D: [Workshop 3590181106](https://steamcommunity.com/sharedfiles/filedetails/?id=3590181106) |
| Shelf Quality Matters | Quality selects items per storage cell; configurable defaults 1/2/3/4/5/6/10. Author says it patches Building_Storage and warns about mid-save changes/storage mods. | Title says 1.4. | D/R: [Workshop 3125228510](https://steamcommunity.com/sharedfiles/filedetails/?id=3125228510), [source](https://github.com/MadaraUchiha/ShelfQualityMatters) |
| Quality Bionics Remastered | Quality changes installed bionic efficiency and HP; adjustable tech threshold and per-tier tuning. | Workshop says 1.6 with 1.5 backwards compatibility. | D/R: [Workshop 3528798857](https://steamcommunity.com/sharedfiles/filedetails/?id=3528798857), [source](https://github.com/WimStienstra/Quality-Bionics-Remastered) |
| Infusion 2 | Item type/quality controls randomly added enhancements; those enhancements supply stat changes and effects such as poison/healing. Quality is an eligibility input to a second enhancement system. | Viewed original updated May 2024; no inspected 1.6/1.7 declaration. A Continued fork exists. | D: [Workshop 2063008864](https://steamcommunity.com/sharedfiles/filedetails/?id=2063008864), [Continued comments](https://steamcommunity.com/sharedfiles/filedetails/comments/3536247747) |

### Implementation-relevant details and cautions

#### Quality Expanded

The author describes conditional Harmony and definition patches, requiring restart for many settings. This is a useful precedent for disabling unneeded patches. Deterioration permanently lowers quality based on HP: buildings/armor check when damaged, other items daily, with special treatment for art. Therefore “quality improves HP” and “damage reduces quality” are separate features. Its published performance number is an author measurement, not a benchmark reproduced here. The fork's listed food/trap additions should be verified against the delivered files. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2885572675)

#### HP lifecycle and uninstall safety

Quality Affects HP documents a concrete migration problem: increasing maximum HP leaves current HP unchanged, so undamaged legendary equipment can become 10% condition; removing it can leave current HP above the reverted maximum. Defaults are .5/.75/1/1.75/3/6/10. The author lists changed quality, other max-HP modifiers and alternate item creation paths as compatibility risks. This establishes that “no custom save class” alone does not guarantee a clean transition. User comments add unverified regression candidates around repairability and crafting bills; source and runtime checks are required. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2898409109)

#### Workbench discovery and settings

The author's changelog names Building_WorkTable / Building_ResearchBench discovery with Bills-tab fallback, duplicate-safe CompProperties_Quality injection, separate speed curves and additive beauty. It also changes global StatDef caps for its uncap setting; copying that would expand the compatibility surface beyond adding an item-specific modifier. Its current published defaults are .95/.98/1/1.02/1.05/1.10/1.20, with beauty -1/0/0/1/2/3/4. This is an author-stated implementation, not code inspection. [Changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3551543324)

#### Food

Published nutrition factors are .65/.88/1/1.12/1.24/1.45/1.70; poisoning factors 1.50/1.18/1/.90/.65/.30/.05. These are **multipliers of an existing chance**, not absolute probabilities. The food patch's useful feature is independent control of two effects; adding quality itself is delegated to Everything Has Quality or Crop and Meat Quality. Whether it alters all actual poisoning paths needs source/game inspection. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2754055111+packageId%3A+qanafpc.velcroboy333&l=indonesian)

#### Drugs

Named supported drugs are go-juice, wake-up, psychite tea, flake, yayo, plus VBE cigarettes/cigars. Quality applies to both items and hediffs; same-quality durations stack, benefits use a factor and penalties its inverse. The mod also modifies addiction severity/need refill, random large overdose and regular overdose severity. Settings require restart. Its optional flat-offset-to-multiplier rebalance and blood-pumping/filtration overdose-duration changes are distinct additional design choices, rather than requirements for supporting quality. No source link was exposed. Custom hediff persistence/removal and broad modded-drug coverage remain unresolved. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3590181106)

#### Bionics

The current page both lists Elite Bionics Framework as required and retains stale text claiming no dependency beyond Harmony. Treat actual metadata/source as authoritative. It claims all eligible modded parts but explicitly excludes A Dog Said to avoid crashes. This contradicts an unconditional “all bionic mods” reading. Earlier Quality Bionics warned of existing parts becoming awful or disappearing on mid-save installation; that is lineage evidence, not proof that the remaster has the same bug. Installed parts become hediffs, so this is a separate state-transfer/migration feature. [Remaster](https://steamcommunity.com/sharedfiles/filedetails/?id=3528798857), [original](https://steamcommunity.com/sharedfiles/filedetails/?id=2527206368&l=latam)

#### Storage

Shelf Quality Matters explicitly warns against add/removal with built shelves and expects incompatibility with other storage systems. Its author says mods deriving from Building_Storage have values overridden. Do not translate its behavior into a blanket capacity multiplier across every storage mod. Initial source cloning was rejected, but later read-only GitHub access succeeded; the shelf source supplement records the inspected getter and stale-quality cache. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3125228510), [source](https://github.com/MadaraUchiha/ShelfQualityMatters)

### Adjacent scope and unresolved leads

- Everything Has Quality's original and forks add quality and propagate material/workbench/skill quality rather than providing a complete effects layer. Their quality-generation/inspiration mechanics already overlap this repository's purpose; avoid counting each continuation as a distinct gameplay feature. [Original](https://steamcommunity.com/sharedfiles/filedetails/?id=2627433311), [unofficial fix](https://steamcommunity.com/sharedfiles/filedetails/?id=2793138726), [current refork](https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766&searchtext=)
- QualityBuilder, Improve This, Quality Upgrade, Weapon Upgrade and static-quality mods govern who produces/improves quality, not new stat effects. They matter as quality-change invalidation scenarios. [Improve This](https://steamcommunity.com/sharedfiles/filedetails/?id=2785022023), [Quality Upgrade](https://steamcommunity.com/sharedfiles/filedetails/?id=3176082972), [Weapon Upgrade comments](https://steamcommunity.com/sharedfiles/filedetails/comments/3395769930)
- Crop and Meat Quality is a food-quality generation/ingredient-restriction lead referenced by the food patch and EHQ authors; exact current implementation/version not independently inspected in this survey. [Food author's reference](https://steamcommunity.com/sharedfiles/filedetails/?id=2754055111+packageId%3A+qanafpc.velcroboy333&l=indonesian)
- Quality Of Building, Quality Colors and Quality Overlay change presentation. They are outside gameplay-stat integration, but visual refresh after quality changes can matter. [Quality Overlay](https://steamcommunity.com/sharedfiles/filedetails/?id=2568283376)
- Enhanced Shield Belts is content with reported quality-sensitive shield capacity; description alone did not distinguish inherited vanilla quality stats from new quality logic. Do not count this as an independent effects module without XML/source inspection. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3664107576&searchtext=)
- Infusion 2 illustrates a separate affix/enchantment system and additional persistent state. Continued-fork author comments describe a January fix for interaction with raised item HP; this is a good lifecycle regression scenario, not a reason to import an entire affix framework. [Discussion](https://steamcommunity.com/sharedfiles/filedetails/comments/3536247747)
- CE-Firearms Quality+ and Quality Based Durability are discovery leads only; Workshop IDs and first-party detailed mechanics were not verified.
- No independently verified new quality effect was found for ordinary plants/harvest yield, fuel consumption, fermentation, heating/cooling, or non-battery storage outside the identified shelf mod. This does not prove none exists; many content mods expose inherited quality-sensitive StatDefs rather than advertising a standalone quality feature.

### Questions pursued by the subsequent implementation and package inspections

1. Main seeds' actual stat lists, curves and hooks (separate implementation agent).
2. Shelf getter and how it handles stored stacks when capacity shrinks.
3. Bionic surgery/hediff persistence and removal behavior; EBF dependency evidence.
4. Quality Drugs download/source: custom hediff classes, generated defs, ingestion and overdose hooks.
5. Food XML and actual poisoning pathway coverage.
6. Quality Power refresh/invalidation when quality changes in place; specialized solar/wind/battery subclasses.
7. Current game 1.7 metadata/API availability and an independently buildable 1.7 reference set. None of this survey's declarations proves 1.7 support.

### Supplemental source inspection: Quality Bionics Remastered

Read-only GitHub connector access subsequently succeeded. The following findings are **S: inspected source**, superseding the description-only limitations for this one mod. Repository tree snapshot: `053c5676f36c9e1b09be0827540abf31c30c0cdc` (main, inspected 2026-10-18). No source was executed and no Workshop binary was proved identical to this repository snapshot.

#### Selection and configuration

`AddQualityToImplants` scans HediffDefs once at static startup. It requires `spawnThingOnRemoved`, its item's `isTechHediff`, and `addedPartProps`; applies excluded mod package IDs and excluded hediff defNames; then accepts a name containing bionic/archotech, an exact custom name synthetic/cybernetic, or an item above the configured technology threshold. Name-based matches therefore bypass the technology threshold. It injects custom hediff props plus vanilla CompQuality on the returned item, checking the item comp for duplicates. This structurally discovers modded parts but is not universal support for any item marketed as a bionic. [Initialization and surgery source](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Patch/RecipeWorker_ApplyOnPawn.cs)

Settings default to Industrial and independent efficiency/HP curves .50/.75/1/1.25/1.50/1.70/2. The exclusion sets exist in code, including two A Dog Said package IDs, but are neither serialized by `ExposeData` nor exposed by the inspected settings UI. Thus these are not an implemented user blacklist/whitelist precedent. Eligibility is duplicated between initialization and manager helpers. [Settings](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Core/QualityBionicsSettings.cs), [manager](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Core/QualityBionicsManager.cs)

#### Persistent state, surgery and returned items

`HediffCompQualityBionics` stores its own quality, defaults to Normal and scribes it under `quality`. It implements EBF's `IHediffCompAdjustsMaxHp` by returning a `BodyPartMaxHpAdjustment.ScaleMultiplier`. This is custom installed-part state in addition to the normal CompQuality item state. [Hediff component](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Comps/HediffCompQualityBionics.cs)

Harmony targets `ApplyOnPawn` methods across nonabstract RecipeWorker subclasses. After installation it finds the last matching `addsHediff` on the pawn and copies quality from the ingredient whose definition equals `spawnThingOnRemoved`. The match does not constrain the installed hediff by body-part record, an important multi-part/failed-surgery test case. Removal uses a static optional `(ThingDef, QualityCategory)` plus a second static list filled around `MedicalRecipesUtility.SpawnThingsFromHediffs`; `ThingWithComps.SpawnSetup` consumes matching returned definitions and sets their quality. This is event-bound transfer, not continuous scanning. [Surgery](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Patch/RecipeWorker_ApplyOnPawn.cs), [medical removal](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Patch/MedicalRecipesUtility_SpawnThingsFromHediffs.cs), [spawn transfer](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Patch/ThingWithComps_SpawnSetup.cs)

The repository also contains a concurrent `QualityTransferManager`, but the inspected live surgery/spawn paths use the static pair/list instead. Do not infer that concurrency helper's presence proves that actual transfers are concurrency-safe. [Transfer helper](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Core/QualityTransferManager.cs)

#### Mid-save migration and uninstall implications

`QualityBionicsGameComponent` persists `hasMigrated`. On first `FinalizeInit` it scans map things and map pawn inventories, converts eligible Awful/no-quality stored bionics to Normal, and records completion. The visible scan does not cover caravan/world-pawn inventories or recursively walk arbitrary containers, and cannot distinguish legitimate pre-existing Awful bionics from newly added zero-valued CompQuality. Installed hediff components independently default to Normal. These are concrete migration limitations, not reproduced runtime errors. [Game component](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Core/QualityBionicsGameComponent.cs), [migration manager](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Core/QualityBionicsManager.cs)

The inspected repository does not supply a dedicated uninstall cleanup. Both its custom hediff component and GameComponent have saved state, so transparent mid-save removal remains **unverified** and cannot be promised from the description. BackCompatibility maps old settings class names only; it is not a general uninstall migration. [BackCompatibility patch](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Patch/BackCompatibility_GetBackCompatibleType.cs)

#### Compatibility and maintenance evidence

The actual efficiency patch temporarily writes a quality-adjusted value into shared `HediffDef.addedPartProps.partEfficiency`, saving/restoring it with Harmony state and a postfix. It handles parent added parts and the direct part. No finalizer restores the shared definition after an exception. This mutation pattern warrants reentrancy, other patches and exception-path scrutiny; copying the advertised “thread-safe/non-destructive” wording would be unjustified. A cached per-instance calculation that avoids changing global definitions is a design candidate, but must be validated against the game's capacity API before choosing it. [Efficiency patch](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/Patch/PawnCapacityUtility_CalculatePartEfficiency.cs)

`About.xml` declares 1.5/1.6, includes EBF as a general dependency and a 1.5-specific Harmony-only dependency section. The csproj references EBF API 1.0.0 and selects either 1.5 or 1.6 reference packages; the tree contains a 1.6 assembly directory and LoadFolders.xml only names v1.6. These files establish explicit EBF integration for the inspected implementation, but do not independently prove a working 1.5 package. No 1.7 branch/build path is present in the inspected project. [Metadata](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/About/About.xml), [project](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/QualityBionicsRemastered.csproj), [load folders](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/LoadFolders.xml)

## Inspected core implementations and delivered seed packages

## Quality effects: source implementation evidence

Research date: 2026-10-18. Research only; no mod implementation changed. Downloaded repositories were read as source; their code and bundled assemblies were not executed. This note covers the assigned implementation examples, not the whole Workshop survey.

### Sources and version boundaries

| Source | Inspected revision | Actual version evidence |
| --- | --- | --- |
| pudy248/QualityFrameworkFork (despite repository name, this is **Quality Expanded**) | GitHub `08372b27b161be6c6407c5e0f620829c24877ab0`, subsequently compared with actual Workshop 2885572675 package | GitHub only declares 1.4; **downloaded package declares and ships 1.4/1.5/1.6**. Important shipped-source and binary findings below. No 1.7 runtime proof. |
| Cozarkian/QualityExpanded | `d29ced0844f3de84f3471cf0cbd455ea85d4a41b` | [About.xml](https://github.com/Cozarkian/QualityExpanded/blob/d29ced0844f3de84f3471cf0cbd455ea85d4a41b/About/About.xml#L3-L7): 1.3. |
| emipa606/QualityAndDurability, Workshop 2572086023 | `900a5d01aeb72327b7cd6c2d3841869bfc00944c` | [About.xml](https://github.com/emipa606/QualityAndDurability/blob/900a5d01aeb72327b7cd6c2d3841869bfc00944c/About/About.xml#L7-L13) declares 1.2–1.6. Installed Workshop copy at `E:\Steam\steamapps\workshop\content\294100\2572086023` also declares 1.6. No 1.7 declaration or runtime verification. |
| Stronger Quality Scaling, Workshop 1206001612 | Steam manifest `312860916551793988`, downloaded and inspected 2026-10-18 | Actual About.xml declares through 1.6; 1.6 XML inspected below. No 1.7 runtime proof. |
| Quality Matters, Workshop 2585864821 | Steam manifest `7231344520731411710`, downloaded and inspected 2026-10-18 | Actual About.xml declares 1.3; complete delivered patch and generation source inspected below. |

The installed game's `E:\Steam\steamapps\common\RimWorld\Version.txt` was directly read and contains `1.7.4981 rev136`. This does not validate these mods against 1.7. Do not equate Workshop tags or reference-package compilation with runtime support.

Also cloned Cozarkian/QualityFramework at `6b1c92b9f99855155a374304ebe10fcd49cfe251`; that original repository contains the Everything Has Quality generation code, rather than the Quality Expanded effects code. The distinct names matter when locating upstream implementations. [Framework About.xml](https://github.com/Cozarkian/QualityFramework/blob/6b1c92b9f99855155a374304ebe10fcd49cfe251/About/About.xml).

### Quality Expanded: what it actually changes

#### Actual Workshop package comparison (supersedes stale GitHub version metadata)

Workshop 2885572675 was downloaded on 2026-10-18. Its [About.xml](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2885572675/About/About.xml:6) and [LoadFolders.xml](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2885572675/LoadFolders.xml:15) declare/load 1.6. Ten of the eleven top-level C# source files (all except StatPatcher.cs) have identical SHA hashes to the pinned GitHub versions examined below. Shipped 1.6 fallback XML and the shared XML Extensions patch are also byte-identical to the GitHub versions. Thus the algorithms/defects below apply to the shipped source too, with one additional defect:

The shipped [StatPatcher.cs](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2885572675/Source/StatPatcher.cs:145) adds removal of quality parts from **MedicalTendQuality and JoyGainFactor whenever medQuality is false**. That flag defaults false. Medicine settings therefore also disable vanilla recreation quality scaling, independent of their label. This is a concrete reason to retain ownership of parts rather than remove all vanilla quality parts.

To avoid relying only on source/binary parity, Mono.Cecil read the actual delivered **1.6 DLL as metadata/IL without executing it**. Key-method output saved locally as [qe16-key-il.txt](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/qe16-key-il.txt). The binary confirms: SetQuality full heal, nutrition UI and serialization using the trapQuality field, MedicalTendQuality/JoyGainFactor removal branches, and reflection access to basePowerConsumption. This is selected-method verification, not a full decompilation or runtime test.

Delivered assembly SHA-256: 1.4 `73879B7D0119769717ED4075A3AB746D18385AB966078B6C78BCF19A0A20470F`; 1.5 `F14141447F1B1490A0374F16EE74BB2956976E3502705A24C54F83844FC70011`; 1.6 `C19FD0AC22B42DA57835ECECE4663DB46AEB8A1181F775DE7C31373727B78977`. Different assemblies are supplied for each supported version; shipped metadata does not prove 1.7 compatibility.

| Effect | Algorithm / integration | Important limitation |
| --- | --- | --- |
| Max HP | Quality multiplier on Thing.MaxHitPoints, six mutually exclusive categories | Unconditional SetQuality full-heal hook; not damage-preserving migration |
| Research / worktable / door speed | Shared StatPart_Quality, generally 0.8→1.5 | Needs quality-bearing item and consumer using that stat |
| Ranged / melee cooldown | Shared stat part; configured decreasing factors (better quality = shorter cooldown) | Non-XML-Extensions fallback has opposite direction |
| Trap damage / nutrition | Added StatPart_Quality in fork, generally 0.8→1.5 load defaults | Nutrition settings mistakenly target trap flag |
| Medicine potency | Shared stat part, 0.9→1.3 initialized curve (legendary load default differs) | Stat caps and actual medical consumer still need version-specific checking |
| Generator output | Multiply power setter result by quality factor | Only CompPowerTrader setter path; 0.8→1.4 default factors |
| Consumer demand | Divide power setter result by same factor | Inverse curve; denominator must remain positive |
| Battery efficiency | IL insert multiplier into AddEnergy and divide into AmountCanAccept; matching inspect display | Does not alter stored-energy capacity; body-sensitive transpilers |
| Explicit turret burst cooldown | Multiply Building_TurretGun.BurstCooldownTime result using body quality | Only nonnegative explicit building override; separate from gun quality |
| Damage-based quality degradation | Probability based on damage ratio plus lower-tier HP threshold; daily lists + damage callbacks | Permanently mutates quality; optional complexity beyond HP scaling |

Sources and exact boundaries for each row are detailed immediately below.

#### HP and damage-dependent loss of quality

`Thing.MaxHitPoints` receives two Harmony postfixes: capture an early result into `__state`, then set a late result to `RoundToInt(__state * qualityFactor)`. Ordered classification switches cover buildings, stuff, ingestibles, weapons, apparel, and everything else. It uses general ThingDef properties and `CompQuality`, so it is not restricted to named vanilla definitions. HP factors Awful through Legendary default to `0.5, 0.75, 1, 1.25, 1.5, 1.75, 2`. Sources: [HP patch lines 11–50](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Quality_HitPoints.cs#L11-L50), [settings lines 16–37](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Settings_QE.cs#L16-L37).

Every `CompQuality.SetQuality` call also sets current HP to maximum, without testing the category switches or whether the object was damaged. This is not a migration preserving damage: it is a full heal, including calls made by other mods. A future implementation should decide damage-preservation policy explicitly and avoid copying this hook as-is. [SetQuality postfix lines 53–57](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Quality_HitPoints.cs#L53-L57).

Quality loss is separate optional behavior. A `MapComponentTick` checks elapsed time and, each 60,000 ticks, walks colonists' equipped weapons, spawned weapons, spawned apparel, and optionally haulable other items. Building damage/spawn and worn-apparel damage callbacks also invoke the loss calculation. It compares current HP against the previous quality's theoretical max HP and rolls a probability based on damage fraction. Art-bearing masterworks/legendaries receive a special adjustment. After reducing quality it restores the prior current HP only when below the new maximum. [Map scans and loss logic](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/QualityLoss.cs#L17-L149), [damage hooks](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/HarmonyPatcher.cs#L39-L52).

Static-analysis implications: ordinary HP lookup is constant work, but damage/loss adds ongoing callbacks and periodic scans. Quality degradation permanently changes vanilla quality data, so disabling/removing the effect does not recover prior quality. Replacing the late HP result from captured state may overwrite other postfix changes occurring between the capture and replacement; compatibility needs explicit checking. No benchmark or uninstall save test was run.

#### Ordinary stats

Most added effects use the existing `StatPart_Quality` with seven factors, attached by XML to shared StatDefs: research speed, worktable speed, door opening speed, ranged cooldown, melee cooldown, medicine potency; fork adds trap damage and nutrition. These apply to compatible modded things using those same stats and having quality. They do not automatically affect bespoke systems that bypass those stats. Sources: [fallback XML](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/1.4/Patches/Patch_QualityExpanded.xml), [XML Extensions path](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/ModPatches/XML_Extensions/Patches/Patch_BldgQuality.xml).

With XML Extensions present, settings values are interpolated into patches at load time. Without it, fallback XML hardcodes factors and conditionally gates building/medicine/nutrition/trap additions on a mod display-name match for `Quality Framework`. The ranged/melee fallback uses rising factors from `0.8` to `1.5`, while configured cooldown settings decrease with quality (ranged `1.2` to `0.8`; melee `1.1` to `0.9`). Thus the fallback reverses the intended cooldown direction. Do not blindly adopt its defaults or display-name checks. [Fallback lines 7–45](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/1.4/Patches/Patch_QualityExpanded.xml#L7-L45), [settings cooldown factors](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Settings_QE.cs#L80-L98).

Stat disabling loops remove all exact `StatPart_Quality` instances from the StatDef, rather than only instances owned by this mod. This can strip vanilla or other mods' factors. No per-definition blacklist/whitelist exists in the inspected effects code. [StatPatcher](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/StatPatcher.cs#L21-L153).

Fork-specific wiring defects found by static inspection: the nutrition checkbox edits `trapQuality`; serialization writes `trapQuality` under the `foodQuality` key. Food and trap controls therefore are not independent as intended. Some initialized defaults differ from serialized defaults (legendary medicine starts `1.3`, load default `1.25`; awful trap/food starts `0.9`, load default `0.8`). [Settings UI](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Settings_Config.cs), [Settings_QE](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Settings_QE.cs#L209-L245).

#### Power, batteries, turrets

Power changes use a postfix on `CompPowerTrader.PowerOutput` setter. Negative base consumption means generator: multiply power output. Positive means consumer: divide by quality factor. Default factors `0.8, 0.9, 1, 1.1, 1.2, 1.3, 1.4`. It reads a cached FieldInfo twice on each setter callback, not a per-tick global search. Battery charging efficiency and accepted energy use three IL transpilers targeting the efficiency field in `AddEnergy`, `AmountCanAccept`, and inspect text. It does **not** patch maximum battery storage capacity. Sources: [power logic/transpilers](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Quality_Power.cs#L13-L119), [patch registration](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/HarmonyPatcher.cs#L29-L36).

Transpilers find a field and assume nearby opcodes without a clear failed-match diagnostic. Inference: these hooks are more maintenance-sensitive across 1.6/1.7 than stat parts and can miss mod-specific power components; a future adapter should verify target bodies and avoid silently applying partially. No IL compatibility test was performed.

`Building_TurretGun.BurstCooldownTime` postfix scales body quality only when the building definition has a nonnegative explicit turret burst cooldown. Otherwise gun behavior is left to the weapon path. That prevents a building override and gun-stat fallback from necessarily being scaled twice. [Turret code](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Quality_Turret.cs#L8-L43).

Patches are registered from the Mod constructor based on startup settings, while other settings are read during callbacks and XML factors at load. Restart requirements are consequently mixed. [Mod constructor](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/Mod_QE.cs#L15-L19), [registration gates](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/Source/HarmonyPatcher.cs#L11-L26).

### Quality And Durability (Continued)

Much smaller implementation: one MaxHitPoints postfix gates on `def.IsWeapon || def.IsApparel`, gets CompQuality, multiplies existing result, and truncates to integer. Defaults Awful→Legendary are `0.5, 0.75, 1, 1.25, 1.5, 2.5, 5`, with configurable sliders. Because it uses generic def flags, modded weapons/apparel participate if they use the vanilla HP/quality path. It does not cover walls or other buildings. [MaxHP patch](https://github.com/emipa606/QualityAndDurability/blob/900a5d01aeb72327b7cd6c2d3841869bfc00944c/Source/AmnabiTemplate/Thing_MaxHitPoints.cs#L7-L23), [defaults](https://github.com/emipa606/QualityAndDurability/blob/900a5d01aeb72327b7cd6c2d3841869bfc00944c/Source/AmnabiTemplate/QualityAndDurabilitySettings.cs#L10-L34).

It shares the unconditional SetQuality full-heal hook, even for objects outside the weapon/apparel scope. [SetQuality patch](https://github.com/emipa606/QualityAndDurability/blob/900a5d01aeb72327b7cd6c2d3841869bfc00944c/Source/AmnabiTemplate/CompQuality_SetQuality.cs#L6-L12). No HP-ratio migration, current-HP repair when settings change, blacklist/whitelist, custom per-item persistence, or ongoing map scans were found in the inspected source files. Settings update live; existing stored current HP is not proportionally adjusted by these settings setters. Removing the multiplier changes the denominator but leaves saved absolute HP; a full uninstall guarantee needs save/load tests, particularly current HP above vanilla maximum. These are source deductions, not observed in-game failures.

### Stronger Quality Scaling: delivered 1.6 implementation

The [author's Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=1206001612) says it increases quality variation in clothing, armor, weapons and constructables. The author links a [balance spreadsheet](https://docs.google.com/spreadsheets/d/1i21d1QG6JruUiFY7h5YJf810lnV4UXiJ6pYQJfj_gec/edit?usp=sharing) for all values and says the 1.6 update included an accuracy adjustment. Author comments also point to a [Combat Extended patch](https://steamcommunity.com/sharedfiles/filedetails/?id=3531511640), because CE changes weapon stats.

SteamCMD subsequently obtained the actual package, manifest `312860916551793988`. Primary local evidence root: [downloaded package](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612). It ships XML definitions in folders 1.0 through 1.6, metadata and previews; there are no C# files, assemblies, license files, or LoadFolders.xml in the inspected delivered file list. [About.xml](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/About/About.xml:9) declares 1.6. The following describes its **1.6 folder**, not older releases.

It redeclares entire vanilla StatDefs and abstract bases under `Defs`, rather than targeted patch operations. The quality calculations themselves use vanilla `StatPart_Quality`; it also copies unrelated vanilla worker/classes/limits/other parts. This is a concrete maintenance/compatibility warning: loading a later RimWorld or another stat-def replacement may alter unrelated behavior or cause conflicts. Do not copy whole StatDefs into the proposed EHQ effects module. Its curve values can inform a configurable balance preset implemented through owned parts.

All factor lists below are Awful, Poor, Normal, Good, Excellent, Masterwork, Legendary, in that order.

| Stat(s) | Quality factors | Primary delivered file |
| --- | --- | --- |
| MarketValue | `0.2, 0.6, 1, 1.5, 2, 4, 9` | [Basics:17](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_BasicsStrongerGeneral.xml:17) |
| DeteriorationRate | `2.5, 1.75, 1, 0.75, 0.5, 0.25, 0.01` | [Basics:60](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_BasicsStrongerGeneral.xml:60) |
| Beauty | `-0.5, 0, 1, 3, 5, 8, 12` | [Basics:101](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_BasicsStrongerGeneral.xml:101) |
| Comfort | `0.52, 0.76, 1, 1.24, 1.48, 1.72, 1.96` | [Basics:126](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_BasicsStrongerGeneral.xml:126) |
| BedRestEffectiveness | `0.6, 0.84, 1, 1.16, 1.32, 1.48, 1.8` | [Building:16](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_BuildingStrongerSpecial.xml:16) |
| JoyGainFactor | `0.52, 0.76, 1, 1.24, 1.48, 1.8, 2.6` | [Building:62](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_BuildingStrongerSpecial.xml:62) |
| SurgerySuccessChanceFactor | `0.75, 0.9, 1, 1.1, 1.2, 1.35, 1.55` | [Building:101](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_BuildingStrongerSpecial.xml:101) |
| ArmorRating_Sharp / Blunt / Heat (inherited ArmorRatingBase) | `0.2, 0.6, 1, 1.3, 1.6, 1.9, 2.35` | [Apparel:115](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_ApparelStrongerUpdateScaling.xml:115) |
| Insulation_Cold / Heat (inherited InsulationBase) | `0.3, 0.7, 1, 1.2, 1.5, 1.9, 2.4`; applies to negative values | [Apparel:134](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_ApparelStrongerUpdateScaling.xml:134) |
| EnergyShieldEnergyMax | `0.2, 0.6, 1, 1.4, 1.8, 2.4, 3` | [Apparel:167](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_ApparelStrongerUpdateScaling.xml:167) |
| EnergyShieldRechargeRate | `0.6, 0.8, 1, 1.1, 1.2, 1.4, 1.6` | [Apparel:191](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_ApparelStrongerUpdateScaling.xml:191) |
| PackRadius | `0.5, 0.8, 1, 1.2, 1.5, 1.9, 2.65` | [Apparel:216](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_ApparelStrongerUpdateScaling.xml:216) |
| AccuracyTouch / Short / Medium / Long (AccuracyBase) | `0.55, 0.8, 1, 1.2, 1.45, 1.75, 2.1` | [Ranged:5](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_WeaponsStrongerRangedScaling.xml:5) |
| RangedWeapon_Cooldown | `1.2, 1.1, 1, 0.95, 0.88, 0.79, 0.7` | [Ranged:66](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_WeaponsStrongerRangedScaling.xml:66) |
| RangedWeapon_DamageMultiplier | `0.55, 0.75, 1, 1.1, 1.25, 1.45, 1.7` | [Ranged:90](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_WeaponsStrongerRangedScaling.xml:90) |
| MeleeWeapon_DamageMultiplier | `0.55, 0.8, 1, 1.2, 1.35, 1.65, 1.9` | [Melee:16](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/1206001612/1.6/Defs/Stats_WeaponsStrongerMeleeScaling.xml:16) |

Important boundary details from those files:

- AccuracyBase retains `maxValue=1`; scaling a high base accuracy above 100% still reaches that cap. This package does not implement overcapped hit probability.
- MarketValue also sets quality maximum gain caps: Good `500`, Excellent `2000`, Masterwork `3500`, Legendary `6000`, and a health curve `(0,0), (0.5,0.1), (0.6,0.5), (0.9,1)`. Multipliers alone do not describe price behavior.
- ResearchSpeedFactor is redeclared, but contains outdoors, temperature and room-stat parts **without a quality part**. Do not list quality-based research as a delivered Stronger Scaling feature.
- The 1.6 files contain no MaxHitPoints, MeleeWeapon_CooldownMultiplier, Nutrition, power, battery, turret-specific, or food-poison additions. No settings, category switches or per-def lists exist.
- No custom state or runtime code exists in this delivered package. Inference: no mod-owned save objects, migration, periodic scans or per-tick C# callbacks; normal stat evaluation and consumer behavior still carry their normal cost. Removing it removes the definition changes after restart; no in-game uninstall test was performed, and changed wealth/shield behavior may affect play while active.

SHA-256 hashes for the five inspected 1.6 XML files, respectively Apparel / Basics / Building / Melee / Ranged: `0E350940C563F28EC9DB8ABE6C0846BCE0EE3D5F15438508833DEEDAE7963A9B`, `4B95F1B876648C83688D1F5A26D2AD0F637ED0BF23762EDABED134C21A2BD2E7`, `C126BFC5C7A3C78E7F187AE232F41CFBB96F0409E7CB4E61DDEED2B530298E75`, `2E30B64E85EC5297A96FEABE91643B387B93D984D351313FFC2DF6AFE8E41E70`, `AAC573970C8119BF1CBFA1A50239C004ED6393FC849046B2637C3EDF4F320000`.

### Additional source checks: cooldown and doors

Read-only GitHub connector source inspection completed after initial report; no additional shell downloads used.

**Quality Cooldown** (`dninemfive/qualitycooldown`, commit `b143dc3466da6889f859ad6d32d962d5d37e463f`, dated 2022-11-08) is a minimal XML implementation. It ensures `/parts` exists, then appends vanilla `StatPart_Quality` to `MeleeWeapon_CooldownMultiplier` with factors `1.1, 1.03, 1, 0.98, 0.95, 0.925, 0.9`, and to `RangedWeapon_Cooldown` with `1.2, 1.05, 1, 0.975, 0.95, 0.9, 0.8`. Ranged cooldown also gets `StatPart_Health` with curve points `(0,1.75), (0.3,1.3), (0.9,1)`. This means damaged guns become slower independently of quality; it is not damage-dependent quality degradation. [Melee XML](https://github.com/dninemfive/qualitycooldown/blob/b143dc3466da6889f859ad6d32d962d5d37e463f/Patches/Stats_Weapons_Melee.xml#L11-L24), [ranged XML](https://github.com/dninemfive/qualitycooldown/blob/b143dc3466da6889f859ad6d32d962d5d37e463f/Patches/Stats_Weapons_Ranged.xml#L11-L34).

The inspected tree contains two XML patches, no assemblies or C# code. It has no configuration, per-def overrides, or runtime persistence of its own. Inference: read-time stat overhead only, and no custom save objects to migrate; this still is not an uninstall test. Its [About.xml](https://github.com/dninemfive/qualitycooldown/blob/b143dc3466da6889f859ad6d32d962d5d37e463f/About/About.xml#L6-L12) declares through 1.4. [MIT license](https://github.com/dninemfive/qualitycooldown/blob/b143dc3466da6889f859ad6d32d962d5d37e463f/LICENSE) copyright 2022 dninemfive.

**Quality Doors** source identified as `Steveblandon/RWMod-QualityDoors`, commit `6bd4e278ef12e4e56fad95ddf6a0f7c009b2d654`, dated 2024-11-09. [PublishedFileId.txt](https://github.com/Steveblandon/RWMod-QualityDoors/blob/6bd4e278ef12e4e56fad95ddf6a0f7c009b2d654/About/PublishedFileId.txt) verifies Workshop ID 2455489002. Its [About.xml](https://github.com/Steveblandon/RWMod-QualityDoors/blob/6bd4e278ef12e4e56fad95ddf6a0f7c009b2d654/About/About.xml#L5-L10) declares through 1.5; no proven 1.6/1.7 support.

It appends `CompQuality` to the XML `DoorBase`, makes doors minifiable, changes mass and construction work, and adds a `DoorOpenSpeed` quality stat part with factors `0.8, 0.9, 1, 1.1, 1.2, 1.3, 1.5`. Door HP uses a `Thing.MaxHitPoints` postfix gated by `def.IsDoor`, factors `0.6, 0.9, 1, 1.2, 1.5, 2, 3`. The same unconditional `CompQuality.SetQuality` full heal appears again; it is not scoped to doors. Sources: [core XML](https://github.com/Steveblandon/RWMod-QualityDoors/blob/6bd4e278ef12e4e56fad95ddf6a0f7c009b2d654/Patches/1.2/Core.xml), [Harmony code](https://github.com/Steveblandon/RWMod-QualityDoors/blob/6bd4e278ef12e4e56fad95ddf6a0f7c009b2d654/Source/QualityDoorsHarmonyPatch.cs#L24-L75).

The HP classification is generic, but adding quality/minification uses named XML inheritance roots, plus special XML for `HeronDoorBase`/`AbstractHeronDoorBase` and `SterileDoorBase`. Thus modded doors outside those roots need another quality provider or patches. It additionally changes uranium/plasteel/wood/steel construction-work factors globally, which are balance choices beyond quality itself. [Doors Expanded patch](https://github.com/Steveblandon/RWMod-QualityDoors/blob/6bd4e278ef12e4e56fad95ddf6a0f7c009b2d654/Patches/1.2/DoorsExpandedPatch.xml), [Hospital patch](https://github.com/Steveblandon/RWMod-QualityDoors/blob/6bd4e278ef12e4e56fad95ddf6a0f7c009b2d654/Patches/1.2/HospitalsExpandedPatch.xml).

No license file, license declaration, or README was present in the complete inspected Quality Doors tree. Treat source as reference only; no code reuse permission verified.

### Primary-description evidence, superseded where downloaded files are available

**Quality Affects Nutrition and Food Poison Chance**, Workshop [2754055111](https://steamcommunity.com/sharedfiles/filedetails/?id=2754055111), author velcroboy333, declares 1.6 and describes a simple patch requiring another mod to add food quality. Author's table gives nutrition factors Awful→Legendary `0.65, 0.88, 1, 1.12, 1.24, 1.45, 1.7`, poison-chance factors `1.5, 1.18, 1, 0.9, 0.65, 0.3, 0.05`. Those are multipliers, not absolute poisoning probabilities. The initial description exposed no source repository. The subsequently downloaded XML targets Nutrition and FoodPoisonChanceFixedHuman; installed runtime inspection confirms that cook contamination, saved poisonPct and rotten-food pathways are separate. See the downloaded food and runtime sections. License remains unverified.

**Quality Matters**, Workshop [2585864821](https://steamcommunity.com/workshop/filedetails/?id=2585864821), was subsequently downloaded: manifest `7231344520731411710`. The package contains one generated XML patch plus its authoring inputs `Source/list.xml` and `Source/rule.xsl`; no assemblies, C# code or license file. Actual [About.xml](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2585864821/About/About.xml:6) declares only 1.3. The patch SHA-256 is `170111191CEAFEABF2965AE18A8AD7C78F5531BBD929548613B93A05AACE26D2`.

The delivered [Patch_QualityMatters.xml](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2585864821/Patches/Patch_QualityMatters.xml:1) has **65 targeted StatDef operations**. Each first ensures a parts list, then conditionally appends StatPart_Quality only when no exact `Class="StatPart_Quality"` already exists. This avoids stacking a duplicate in that case but means it leaves any existing vanilla/other-mod curve intact rather than supplying its own preset. It neither adds CompQuality nor controls which definitions receive quality. For pawn stats used as equipment offsets, actual effectiveness still depends on the game's equipment-offset evaluation; a listing on an info card alone is not proof of applied pawn benefit.

The three inverse targets are `Ability_CastingTime`, `Ability_PsyfocusCost`, `HungerRateMultiplier`, using `1.35, 1.1, 1, 0.9, 0.85, 0.8, 0.75` (Awful→Legendary). These are separately specified decreasing curves, not mathematical reciprocals of the default curve. All other 62 targets use `0.4, 0.75, 1, 1.15, 1.25, 1.3, 1.35`. Sources: [inverse casting operation](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2585864821/Patches/Patch_QualityMatters.xml:501), [generator factor templates](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2585864821/Source/rule.xsl:40).

Complete default-factor target list, grouped for readability (all names below occur in the delivered patch):

| Family | Targets |
| --- | --- |
| General/production work | WorkSpeedGlobal, MiningSpeed, DeepDrillingSpeed, MiningYield, SmoothingSpeed, ResearchSpeed, AnimalGatherSpeed, AnimalGatherYield, PlantWorkSpeed, PlantHarvestYield, DrugHarvestYield, HuntingStealth, ConstructionSpeed, ConstructSuccessChance, FixBrokenDownBuildingSuccessChance |
| Medicine | MedicalPotency, MedicalTendSpeed, MedicalOperationSpeed, MedicalSurgerySuccessChance, MedicalTendQualityOffset |
| Abilities/psycasting | Ability_EntropyGain, Ability_Range, Ability_Duration, Ability_EffectRadius, PsychicSensitivityOffset, PsychicEntropyMax, PsychicEntropyRecoveryRate, PsychicEntropyGain, MeditationFocusGain |
| Pawn movement/survival | MoveSpeed, GlobalLearningFactor, RestRateMultiplier, EatingSpeed, ImmunityGainSpeed, InjuryHealingFactor, PainShockThreshold, ForagedNutritionPerDay, CarryingCapacity |
| Social/trade/animals | NegotiationAbility, PawnBeauty, ArrestSuccessChance, TradePriceImprovement, DrugSellPriceImprovement, SocialImpact, TameAnimalChance, TrainAnimalChance, AnimalProductsSellImprovement |
| Recipe work | SmeltingSpeed, GeneralLaborSpeed, DrugSynthesisSpeed, CookSpeed, DrugCookingSpeed, ButcheryFleshSpeed, ButcheryMechanoidSpeed, ButcheryFleshEfficiency, ButcheryMechanoidEfficiency |
| Ideology/security/combat | SlaveSuppressionOffset, ConversionPower, SuppressionPower, HackingSpeed, MeleeDodgeChance, ShootingAccuracyPawn |

The generation list comments out MentalBreakThreshold, ToxicSensitivity and FoodPoisonChance; these are **not delivered effects**. Author states negative offsets do not scale despite display. This package does not set `applyToNegativeValues`, so that caveat should be investigated through actual offset consumers before promising full equipment-buff support. Defaults that increase entropy cost also deserve effect-by-effect review instead of treating every increasing number as beneficial. [Source/list.xml](E:/Coding/Rimworld/EverythingHasQualityReforked/.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2585864821/Source/list.xml:7).

The package targets obsolete `HungerRateMultiplier`. Direct read-only search of installed **1.7.4981** game XML found no such StatDef and found `BedHungerRateFactor` instead at [Stats_Pawns_General.xml:133](E:/Steam/steamapps/common/RimWorld/Data/Core/Defs/Stats/Stats_Pawns_General.xml:133). Its XML guards check only parts/existing quality parts, not existence of a missing StatDef; missing targets therefore cause failed patch operations. The 1.3 package should not be installed unmodified as a verified 1.6/1.7 solution. No runtime claim is made from this static check.

There is no per-item state, migration, map scan, settings UI, category switch or blacklist/whitelist in the delivered files. Performance inference: startup patch work plus ordinary stat-part evaluation; no custom ongoing polling. No uninstall save test was run. License remains unverified despite source being included.

**Stronger Quality Scaling**: actual delivered XML was subsequently obtained and inspected above. Only license/source-reuse permission and runtime compatibility remain unverified; no license was included in its package. Do not substitute third-party mod mirrors or old error logs as proof of current implementation.

### License summary for the original core examples

Quality Expanded original/fork both carry MIT copyright 2021 Cozarkian; Quality And Durability repo carries MIT copyright 2020 Mlie. Preserve applicable notices for any substantial copied code. [Expanded license](https://github.com/pudy248/QualityFrameworkFork/blob/08372b27b161be6c6407c5e0f620829c24877ab0/LICENSE), [Durability license](https://github.com/emipa606/QualityAndDurability/blob/900a5d01aeb72327b7cd6c2d3841869bfc00944c/LICENSE.md). Stronger Quality Scaling license unverified. Independent implementation from observed behavior avoids requiring verbatim source reuse; this note does not grant permission for unlicensed source/assets.

### Design conclusions supported by these examples

1. Most broad effects can be implemented through the shared stat pipeline. A single mod-owned stat part can consult cached category/ThingDef eligibility and a seven-value curve; no polling is needed for these read-time effects. This is a recommendation inferred from the working patterns, not implemented code.
2. Separate eligibility (buildings/apparel/weapons/etc. plus def overrides) from effect families. Quality Expanded has category switches only for HP, so copying its settings model would not satisfy the requested whole-system category controls and black/whitelists.
3. Treat HP as stateful despite its stat-like maximum. Choose behavior for existing damage when enabled/disabled, quality changed, curves edited, game loaded, and mod removed. Avoid unconditional healing; test actual damage events rather than only info-card numbers.
4. Power, battery, turret overrides and installed bionics need explicit adapters when their mechanics bypass ordinary stats. Do not build a universal event framework merely to support a few adapters.
5. Avoid degradation/map scans unless explicitly selected as a feature. Quality-dependent HP durability does not require automatic quality loss. Neither source proves a need for persistent per-item effect state in the ordinary stat/HP multiplier case.
6. Preserve other stat parts instead of removing all `StatPart_Quality`; use stable package IDs, verify duplicate scaling deliberately, and keep adapters conditional. Declare 1.6 and 1.7 support only after compiling and smoke testing their actual API behavior separately.

## Shelf source supplement

## Shelf Quality Matters: inspected source supplement

Inspected 18 October 2026 through the read-only GitHub connector, after shell cloning was rejected. Revision `5415dddaaa4774d9aa735913d5c549e76003a6c1`. This closes the earlier shelf source-access gap; it does not validate runtime behavior on 1.6 or 1.7.

The prefix targets `Building.get_MaxItemsInCell`, applies to every `Building_Storage` with quality, returns an absolute capacity, and skips the original method. It does not multiply an existing storage capacity or distinguish shelves from other storage subclasses. Thus it can suppress a storage mod's original capacity calculation. [Patches.cs](https://github.com/MadaraUchiha/ShelfQualityMatters/blob/5415dddaaa4774d9aa735913d5c549e76003a6c1/Source/Patches.cs).

It caches `QualityCategory` by `thingIDNumber` in a static dictionary. The complete inspected source set contains no invalidation on quality changes, building destruction, or new-game/load transitions. The settings curve is read on every getter call, so curve edits change the returned number, but a changed building quality remains cached. Memory can grow with encountered storage objects. This is a static-analysis conclusion, not a reproduced runtime failure. [Patches.cs](https://github.com/MadaraUchiha/ShelfQualityMatters/blob/5415dddaaa4774d9aa735913d5c549e76003a6c1/Source/Patches.cs), [source tree](https://github.com/MadaraUchiha/ShelfQualityMatters/tree/5415dddaaa4774d9aa735913d5c549e76003a6c1/Source).

Awful through Legendary settings default to 1, 2, 3, 4, 5, 6, 10. The settings UI writes changes directly. No category switches, per-def overrides, stack overflow reconciliation, or uninstall cleanup exists in these three source files. It serializes mod settings, not a custom building class or custom per-building save data. That reduces structural save coupling but does not establish safety for already stored stacks when capacity shrinks. [Settings.cs](https://github.com/MadaraUchiha/ShelfQualityMatters/blob/5415dddaaa4774d9aa735913d5c549e76003a6c1/Source/Settings.cs), [Main.cs](https://github.com/MadaraUchiha/ShelfQualityMatters/blob/5415dddaaa4774d9aa735913d5c549e76003a6c1/Source/Main.cs).

Recommendation: treat storage capacity as an optional dedicated effect with a defined overflow policy and explicit compatibility boundaries. Prefer composing with existing capacity where the desired behavior permits. Avoid an unbounded cache of mutable quality when the normal quality lookup is already constant-sized work. These are design recommendations, not accepted implementation decisions.

## Downloaded food and drug implementations

## Food and drugs: downloaded implementation evidence

Inspected 18 October 2026. Both packages were downloaded anonymously with SteamCMD into the research scratch folder, never enabled or executed. Source/XML inspection does not establish source-to-binary parity or game compatibility.

### Food nutrition and poisoning

[Quality Affects Nutrition and Food Poison Chance](https://steamcommunity.com/sharedfiles/filedetails/?id=2754055111) ships a single relevant XML patch, `Patches/MaxHitPoints_Nutrition_FoodPoisonStats_Quality.xml`. Despite that filename, it does not change HP. It adds `StatPart_Quality` to `Nutrition` and `FoodPoisonChanceFixedHuman`, with these Awful-to-Legendary factors:

| Effect | Awful | Poor | Normal | Good | Excellent | Masterwork | Legendary |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Nutrition | .65 | .88 | 1 | 1.12 | 1.24 | 1.45 | 1.7 |
| Fixed human food-poison chance | 1.5 | 1.18 | 1 | .9 | .65 | .30 | .05 |

It adds no quality comp itself, no settings, no saved component and no scanning loop. It relies on another mod to give food quality. The two unconditional XML additions assume the target `parts` nodes exist and do not avoid duplicate quality parts. Its About.xml declares 1.3–1.6. These are shared stat targets, so modded food using them can participate; this does not prove every contamination/poisoning path is affected. There is no evidence that it changes a cook's contamination outcome or removes poison already recorded on a stack. Source: [downloaded XML](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2754055111/Patches/MaxHitPoints_Nutrition_FoodPoisonStats_Quality.xml), [metadata](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/2754055111/About/About.xml).

Recommendation: expose nutrition and poisoning as separate effects and use truthful UI names tied to the actual consumers. Quality cannot improve a chance that is already zero through multiplication. Removal of the stat part stops the derived modifier; already consumed nutrition and historical illness are consequences that do not rewind.

### Quality Drugs

[Quality Drugs](https://steamcommunity.com/sharedfiles/filedetails/?id=3590181106) bundles C# source and a DLL. About.xml declares 1.6 and Harmony. `Patches/Entry.xml` enumerates GoJuice, WakeUp, PsychiteTea, Flake and Yayo; `Mod_VBE.xml` adds named cigarettes/cigars using display-name mod detection. It does not discover every modded drug generically.

`PatchDrug.ApplyWorker` replaces matched ingestion outcome-doer classes, changes the existing HediffDef to custom `HediffWithQuality`, replaces severity-per-day with a disappearance timer, clones its single stage into seven stages and uses stage severity to encode quality. It rejects hediffs with multiple stages. It adds quality if its XML check finds none. It assumes particular XML nodes/classes exist, parses numbers using the process culture, and rewrites existing health definitions. This is a much broader compatibility surface than attaching a stat part. [PatchDrug source](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/3590181106/Source/PatchDrug.cs), [entry XML](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/3590181106/Patches/Entry.xml).

Forward quality factors are .6/.8/1/1.1/1.3/1.5/2. The separate inverse curve is 1.2/1.1/1/.9/.85/.8/.75; it is not the mathematical reciprocal. Stage rewriting handles offsets and factors differently and special-cases hunger/rest/joy fall rates. Stage values are baked at load time, while some ingestion modifiers read current settings, so the restart instruction should be respected. [Settings](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/3590181106/Source/ModSettings.cs), [stage rewriting](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/3590181106/Source/PatchDrug.cs).

The custom GiveHediff doer assigns quality as severity (.1 for Awful), scales duration, and applies tolerance/body-size handling. Same-def/same-part hediffs merge: same-quality timers add; a different quality replaces remaining duration and severity. Need and psyfocus doers multiply their effects by quality and ingested count. These are concrete behavior decisions, not inevitable consequences of supporting quality. [Subclass.cs](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/3590181106/Source/Subclass.cs).

Harmony transpilers insert adjustments into CompDrug for existing-addiction severity, chemical need refill and large overdose chance. The code also attempts a random-overdose-severity match using `AccessTools.Field` for `FloatRange.RandomInRange`; that target needs runtime/API verification before claiming it works. The startup display patch has a display-name exclusion for Xenobionic Patcher. [Harmony source](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/3590181106/Source/HarmonyPatches.cs).

`OverdosePatch.xml` additionally changes overdose recovery to a custom severity-per-day component weighted by blood pumping and filtration. That is a global recovery rebalance, distinct from quality scaling. The optional alternate-drug-stats patch rewrites base drug effects before quality stages are generated. Both should be treated as independent optional features rather than imported incidentally. [Overdose XML](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/3590181106/Patches/OverdosePatch.xml), [conditional patch](../../.scratch/quality-effects/downloads/steamapps/workshop/content/294100/3590181106/Source/PatchConditional.cs).

No uninstall cleanup was found in the bundled source. Existing saves can contain the custom hediff class and custom comps, with severity meaning quality rather than vanilla drug intensity. Removing the mod while these effects are active is not demonstrated safe and can lose or reinterpret state. A future implementation should prefer narrow event-time effects where feasible, and explicitly decide how longer-lived quality effects survive saves and removal. Do not promise arbitrary modded drugs without adapters or schema checks. No category/per-item blacklist UI exists in this package.

No license file was found in either inspected package. Read the implementations as behavioral references; permission to redistribute their code/assets is unverified. The current report recommends independent implementation.

## Downloaded power, workbench and HP implementations

## Standalone Quality Power and Quality Workbenches: package inspection

Inspected 18 October 2026. Primary sources: SteamCMD-downloaded Workshop packages, their own About.xml, and their compiled IL read with Mono.Cecil. No downloaded code was executed. Findings describe inspected packages; inferred runtime failures are identified as inferences and were not reproduced in-game.

| Package | Stated support / version | DLL SHA-256 |
| --- | --- | --- |
| [Quality Power, 3552862196](https://steamcommunity.com/sharedfiles/filedetails/?id=3552862196) | About.xml: 1.6, update 1.0.1 | `C65D2317E900AC5FDE2FC3BAB5105E35824A4D56E92D67A0BBF980926E3942AE` |
| [Quality Workbenches, 3551543324](https://steamcommunity.com/sharedfiles/filedetails/?id=3551543324) | About.xml: 1.6, v1.1.1 | `01DF6164E7479500D7A260875BC6464C86B2029E99E2A2DFB4C51FF11BF74A79` |

Local evidence: `downloads/steamapps/workshop/content/294100/<id>/About/About.xml`, corresponding `Assemblies/*.dll`, and [Power IL](../../.scratch/quality-effects/3552862196-il.txt) / [Workbenches IL](../../.scratch/quality-effects/3551543324-il.txt). `inspect-adapters.ps1` reproduces the captures. Compare the installed game's direct consumer behavior in [runtime-seams.md](../../.scratch/quality-effects/runtime-seams.md). Neither package declares 1.7 support in its metadata.

### Quality Power

#### Implemented effects and detection

`DefInjector..cctor` scans all ThingDefs once. It detects generators by a comp class assignable to `CompPowerPlant`, batteries by a comp class assignable to `CompPowerBattery`, and adds vanilla `CompQuality` when absent. It also adds its own display comp to generators. This is structural detection and can include modded subclasses without per-def XML patches. It does not detect every possible custom generator architecture. [Power IL: `DefInjector` and its predicates.]

`Patch_CompPowerTrader_PowerOutput.Postfix` multiplies only a **positive** returned `PowerOutput`. Thus this mod increases generator production, not power consumer efficiency, and leaves an EMP zero untouched at this seam. Multipliers use a quality lookup and dictionary lookup; they do not repeatedly mutate output fields. The generic getter target can cover a quality-bearing custom producer even if its def was not auto-injected. [Power IL: named patch and `QualityPowerUtil.GenMultiplier`.]

Battery capacity is implemented through patches for `AmountCanAccept`, `StoredEnergyPct`, `SetStoredEnergyPct`, `PostExposeData` and inspect text. Capacity is `base capacity * tier multiplier`. It reflects the private stored-energy field via a cached FieldInfo but still calls `FieldInfo.GetValue` (boxing) on repeated capacity/percentage queries. Efficiency itself is unchanged. [Power IL: `Patch_CompPowerBattery`.]

Beauty is an additive custom stat part on Beauty. It applies to a thing with `CompPower`, so its scope is broader than only generators/batteries if another mod supplies quality to power consumers. [Power IL: `StatPart_QualityPowerBeauty.TransformValue/Applies`.]

The metadata gives defaults Awful -5%, Legendary +20% for generation/capacity and beauty -1 through +4, with all seven tiers configurable. The settings persist `genMul`, `batMul`, `beautyAdd`; there are no category toggles or per-def allow/deny lists in the inspected settings fields or application path. [About.xml; Power IL `QualityPowerSettings` fields and `ExposeData/ApplyNow`.]

#### Issues not to copy

The following are direct IL comparisons against the installed **1.7** runtime, not confirmation that its author observed these problems on 1.6:

- `AmountCanAccept_Postfix` replaces the result with `max(0, (scaledMax - storedEnergy) / efficiency)` whenever the multiplier differs from 1. It does not preserve vanilla's broken/EMP early-return zero. Inference: querying a disabled battery may report positive acceptance. `AddEnergy` still has its own EMP guard in installed vanilla, but broken-state behavior should not be discarded.
- `SetStoredEnergyPct_Prefix` turns its input into `Clamp01(pct * multiplier)`, after which vanilla multiplies by **base** capacity. For a 1.2 multiplier and pct=1, the setter fills only base capacity, not boosted capacity. This follows algebraically from the two inspected methods.
- Its `PostExposeData` postfix only caps to scaled capacity **after** vanilla's method has already capped to base capacity. It cannot restore the excess removed earlier. Inference: the installed 1.7 runtime would discard capacity above baseline during exposure despite the mod's boosted maximum. This makes boosted energy persistence an explicit validation requirement, not a solved behavior to transplant.
- Inspect text uses literal replacement of a formatted `" / <base> Wd"` segment, which depends on vanilla's formatting and unit text. A proper shared effective-capacity calculation should feed gameplay and display.

[Sources: Power IL, named battery patches; installed `CompPowerBattery` methods captured in runtime-seams-il.txt.]

`QualityPowerApply.ApplyNow` only attempts to clear the stats report cache. Its reflection helper casts `StatsReportUtility.cachedDrawEntries` to `IDictionary`. In installed 1.7 this field is `List<StatDrawEntry>`, so the cast fails and no clear occurs. It does not clear StatWorker caches. Generator/battery arithmetic reads current settings on demand, but beauty/report freshness is not fully established by this “apply” path. [Power IL `ApplyNow/TryClearStatsReportCache`; installed field metadata inspected by `inspect-quality-cache.ps1`.]

Its spawn/backfill path sets Normal only when `CompQuality` exists **and** `TryGetQuality` returns false. In installed 1.7, a present standard quality comp makes that utility return true; default newly added comp quality cannot be distinguished by this check. Its construction postfix similarly has a no-quality condition before assigning a pawn-generated quality and full HP. The vanilla construction assignment may already provide quality; this fallback should not be used as evidence that old buildings receive Normal successfully. [Power IL: `Patch_Thing_SpawnSetup`, `Patch_Frame_CompleteConstruction`; installed `QualityUtility.TryGetQuality`.]

#### Relevance to this mod

Keep the positive getter scaling idea, with cached tier factors and explicit eligible defs. Do not copy battery patches wholesale. Capacity requires consistent charging, percentage, setters, serialization, UI, EMP/broken handling, quality changes, and setting changes. Support custom producers only when they share the verified seam. A runtime display comp is avoidable if the host mod can explain the effect through existing stat/report hooks.

### Quality Workbenches

#### Implemented effects and detection

At startup `ATQ_DefInjections` scans definitions and adds vanilla `CompQuality` to player-buildable building defs with building properties and a class, excluding Frame and build/install blueprint classes. A worktable is a `Building_WorkTable` subclass or exposes the Bills inspector tab. Research benches are `Building_ResearchBench` subclasses. That handles ordinary vanilla and modded furniture patterns, but Bills UI presence alone does not prove a custom bench actually consumes the standard work-speed stat. [Workbenches IL: `IsSpawnableBuilding/IsWorkTable/IsResearchBench/.cctor`.]

Custom `StatPart_TableQuality` multiplies work/research speed; the research-bench classification chooses the separate research multiplier. It requires a real Thing request. `StatPart_BenchBeauty` adds configured beauty. Startup registration attaches to named speed stat alternatives and removes a stray same-name `StatPart_TableQuality` from Beauty before adding its beauty part. Detection logic is duplicated in the injector, speed part and beauty part. [Workbenches IL: registrar, injection helpers, stat parts.]

Default effects stated in About.xml are multipliers 0.95, 0.98, 1, 1.02, 1.05, 1.10, 1.20 and beauty -1, 0, 0, 1, 2, 3, 4. Settings provide separate per-tier work/research multipliers, beauty offsets and an uncap option. Whitelisting is explicitly **planned** in About.xml and is absent from the inspected settings/application paths. There are no per-def blacklist or whitelist controls to reuse. [About.xml; Workbenches IL settings fields, `ExposeData`, `ApplyNow`.]

#### Performance, refresh and lifecycle limits

- The effect uses normal stat evaluation, with no custom every-tick updater. However each transform resolves up to four named stat defs and repeats class/tab classification. Def eligibility and selected StatDefs can instead be resolved once and shared.
- `ApplyNow` clamps work/research multipliers to [0.01,300] or [0.01,999] when uncapped, updates settings and calls the same `cachedDrawEntries as IDictionary` refresh pattern. On installed 1.7 the list type makes that ineffective, and no StatWorker invalidation is present.
- `QW_Caps.Apply` sets the **global** worktable speed StatDef.maxValue to 300 or 999; this changes the cap for every consumer, not just this mod's factor. It does not preserve another mod's pre-existing maximum. Avoid copying this unless an independently scoped cap feature is wanted.
- `ATQ_GameInit.FinalizeInit` is a once-per-game map scan. Its Normal backfill condition is `comp != null && !TryGetQuality(thing, out ...)`; with the installed 1.7 standard comp semantics this does not detect an uninitialized/default quality. It is map-only; it does not walk minified things in containers or world-held items.
- Its construction postfix returns if the consumed Frame has no Map, and assigns random equal-chance quality only if TryGetQuality fails. The expected vanilla path already assigns quality for an injected CompQuality. This fallback is not evidence of a reliable post-construction assignment mechanism.

[Workbenches IL: `StatPart_TableQuality.TransformValue`, `ATQ_Settings.ApplyNow/TryClearStatsReportCache`, `QW_Caps.Apply`, `ATQ_GameInit.FinalizeInit`, construction postfix; installed runtime metadata/IL in runtime-seams.md.]

#### Relevance to this mod

The useful mechanism is a narrow speed stat part plus shared structural classification. The host already owns quality assignment, so adopt effect routing without a second assignment/backfill system. Precompute eligible defs, keep separate research/worktable effects if desired, make per-def overrides explicit, and invalidate the actual stat caches when settings change. Native stat parts allow inheritance-based mod compatibility without hardcoding individual benches.

### Removal and scope conclusion

Both packages mostly compute effects using vanilla quality and normal fields; neither introduces a saved replacement building class in the inspected code. Workbenches defines a GameComponent but no custom ExposeData override, and Power's display comp defines only display behavior. This is a relatively small persistence footprint, **not** proof of universally safe removal. Extra battery energy can be lost, added quality labels can disappear, and old injected qualities need actual save/load checks. Neither package demonstrates 1.7 support, robust item allow/deny policy, or complete cache invalidation. They are useful implementation examples and feature references, not drop-in foundations.

### Quality Affects HP, 2898409109: additional inspected package

Primary sources: [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2898409109), downloaded `2898409109/About/About.xml`, `1.6/Patches/GermanQuality.xml`, and `1.6/Assemblies/GermanQuality.dll`; [IL capture](../../.scratch/quality-effects/2898409109-il.txt). Metadata declares 1.4/1.5/1.6, not 1.7, and requires Harmony plus HugsLib.

The XML adds a vanilla `StatPart_Quality` to `MaxHitPoints` with Awful/Poor/Normal/Good/Excellent/Masterwork/Legendary factors 0.5/0.75/1/1.75/3/6/10. `GermanQualityLF.ApplySettingsToDef` runs at startup and updates the factor fields from HugsLib settings. Its About.xml explicitly requires restart for changed settings. There are seven tier settings, no per-category switches or allow/deny lists. It scales existing quality-bearing things; no quality-assignment expansion is present in this small package.

Its lifecycle approach is more invasive than the XML suggests:

- Harmony `QualitySetter.Postfix` on `CompQuality.SetQuality` computes `RoundToInt(parent.MaxHitPoints * Clamp01(def.startingHpRange.RandomInRange))`, passes that to `GermanQualityUtility.CalculateQualityMaxHP`, then overwrites current HP. The helper multiplies by the tier factor again, truncates to int, and rounds to a multiple of five when over 200. It reads HugsLib settings and parses strings during that operation. There is no preservation of current damage. Inference: a quality change can repair/reset damage; if MaxHitPoints already reflects the newly set quality, applying the helper again can double-scale current HP relative to the stat maximum. Actual observed outcome also depends on the game's MaxHP cache; this was not executed.
- `Frame_CompleteConstruction_Patch.Transpiler` inserts a check of hardcoded local slot 11 and branches past vanilla's HP assignment when it is non-null. It finds locations by stringified instruction text and literal strings for HitPoints methods. This bypasses vanilla's transfer of frame damage ratio and depends on compiler IL layout. It is a poor fit for maintaining both 1.6 and 1.7.
- `MinifyHandler.Postfix` on `MinifyUtility.MakeMinified` assigns the minified wrapper HP to `CalculateQualityMaxHP(100, innerQuality)` when the inner thing uses HP and has quality; it does not preserve a previous wrapper damage ratio.

Recommendation: borrow the narrow MaxHitPoints stat seam and configurable tiers, not the repeated multipliers, HP resets, HugsLib dependency or local-index transpiler. Compute effective MaxHP once, handle creation and later quality/settings changes explicitly, preserve a defined damage policy, and verify minify/reinstall/save/load separately. This package supplies no evidence that uninstall automatically normalizes boosted current HP.


### Food poisoning consumer clarification (installed 1.7)

The patch-only Food Quality mod 2754055111 was identified by the main researcher as adding quality parts to Nutrition and FoodPoisonChanceFixedHuman. Inspecting the latter's actual consumers establishes a narrower effect than “quality reduces food poisoning”:

- `Thing.Ingested` IL_0598–05d6 first asks `FoodUtility.TryGetFoodPoisoningChanceOverrideFromTraits`. Without an override it computes the food Thing's `FoodPoisonChanceFixedHuman * FoodUtility.GetFoodPoisonChanceFactor(ingester)` and rolls for a humanlike ingester. An override supplies its own probability. This is the food's inherent ingestion risk.
- `CompFoodPoisonable.Notify_RecipeProduced` separately rolls the **room** `RoomStatDefOf.FoodPoisonChance`, then the **cook pawn** `StatDefOf.FoodPoisonChance`; it marks the meal poisoned with a cause. It does not query the finished food's FoodPoisonChanceFixedHuman.
- `CompFoodPoisonable.PostIngested` rolls saved `poisonPct * GetFoodPoisonChanceFactor(ingester)` and applies its stored cause. A quality part on FoodPoisonChanceFixedHuman does not modify that saved contamination.
- `CompRottable.PostIngested` is another separate route: a nonfresh rot stage and a positive poison factor produce rotten-food poisoning; it does not query FoodPoisonChanceFixedHuman.

Primary evidence: installed Assembly-CSharp.dll, captured by `inspect-foodpoison.ps1` into [foodpoison-il.txt](../../.scratch/quality-effects/foodpoison-il.txt), named methods. No game execution. Research recommendation: label the simple stat effect “inherent food poisoning risk”; changing cook contamination or rotten-food outcomes requires separate explicitly chosen mechanics. Multipliers also leave a zero inherent base risk at zero.


## Downloaded bionics and affix implementations

## Downloaded package inspection: bionics and affixes

Date: 2026-10-18. Evidence: SteamCMD packages under `.scratch/quality-effects/downloads/steamapps/workshop/content/294100`, XML/text inspection, and static Mono.Cecil IL inspection only. No downloaded mod code was executed. Dumps: `3528798857-1.6-il.txt`, `3536247747-1.6-il.txt`, `3536247747-1.7-il.txt`; reproducible inspection script `inspect-health-affixes.ps1`. Runtime compatibility, load-order interaction and clean removal remain untested.

### Package identity and version evidence

| Package | Actual delivered evidence | DLL SHA-256 |
| --- | --- | --- |
| [Quality Bionics Remastered 3528798857](https://steamcommunity.com/sharedfiles/filedetails/?id=3528798857) | About claims 1.5/1.6; only v1.6 LoadFolders and 1.6 DLL delivered. Changelog 0.3.2 explicitly says 1.5 support was dropped, contradicting stale About prose. DLL assembly version remains 0.2.1.0; references Assembly-CSharp 1.6.9564.19633, Harmony 2.2.2 and EBF API 1.0. | `9C7C7D8AC21F11D4DF8DCCAA68940D3C7BC08097FDEC549A4D9D18EF9046A216` |
| [Infusion 2 Continued 3536247747](https://steamcommunity.com/sharedfiles/filedetails/?id=3536247747), 1.6 | About supports 1.6/1.7, modVersion 1.15.2. Separate 1.6 DLL is assembly version 1.14.6, references Assembly-CSharp 1.6.9676.17735, Harmony 2.4, LessUI 1.0; LessUI DLL included here. | `3562AD1D24270F7611E20D823E67C1B4284509ACEBF6C18D9E326301A30472A3` |
| Infusion 2 Continued, 1.7 | Separate v1.7 LoadFolders and real DLL assembly version 1.15.2, references Assembly-CSharp 1.7.9786.4087, System.Runtime 10.0, Harmony 3.0 and UnityStubs 1.0. It is substantial 1.7 packaging/API evidence, not an in-game test. | `5F890487581C1DEF156EAA0B6357D8AA029AF8EC3C75C6B892ACCABE5C2FACA5` |

Infusion's author states October 2026 migration to 1.7 and discontinuation of ongoing 1.6 maintenance. Both packages remain delivered. Therefore 1.6 and 1.7 should not be treated as one interchangeable DLL target. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3536247747)

### Quality Bionics Remastered: delivered IL compared with source

The key types and methods in the actual DLL agree with the inspected public source snapshot `053c5676f36c9e1b09be0827540abf31c30c0cdc`: static startup eligibility/injection, custom HediffCompQualityBionics with serialized quality, persisted migration GameComponent, surgical quality transfer via static pending pair/list, and temporary mutation/restoration of shared `AddedBodyPartProps.partEfficiency`. This is a method-level behavioral comparison, not a bit-for-bit reproducible source build. See survey.md supplemental section for pinned source links and detailed selection logic.

#### Scope, filters and persistence

- Startup scans removable technical added-part HediffDefs, excluding package IDs and named hediffs, then selects bionic/archotech names, exact synthetic/cybernetic names, or configurable minimum tech level (Industrial default). It adds vanilla item CompQuality and a custom hediff comp. This supports structurally similar modded parts, rather than every implant by definition.
- Separate efficiency/HP quality curves default to .5/.75/1/1.25/1.5/1.7/2. Package exclusion sets include A Dog Said; the inspected settings serialize curves and tech threshold, not those exclusion sets. User blacklist/whitelist controls are therefore not demonstrated.
- The installed hediff comp scribes `quality` (Normal default) and implements EBF's `IHediffCompAdjustsMaxHp`; removal copies it back into the spawned item. The GameComponent scribes `hasMigrated` and on first load migrates map items/map pawn inventories from Awful/no-quality to Normal. Caravan inventories and arbitrary nested containers are not covered by the visible scan. Legitimate pre-existing Awful items cannot be distinguished by that heuristic.
- Existing source and package changelog establish a specific mid-save migration fix, not blanket uninstall safety. Custom saved hediff comps and GameComponent state exist. No explicit removal cleanup was found. Clean uninstall remains unverified.

#### Performance and compatibility

Initialization is a one-time Def scan, but creation/migration helper lookups linearly search HediffDefs. Capacity calculations traverse relevant hediffs and temporarily modify shared definition efficiency, restoring in a postfix rather than finalizer. The actual DLL contains those stores; advertising alone that all patches are non-destructive or thread-safe would be inaccurate. Exception/reentrancy/multiple-body-part cases deserve attention. Surgery matches installed `addsHediff` using a last-match lookup without constraining the body-part record; returned-item transfer is event driven through SpawnSetup. Neither clone-ready architecture nor universal compatibility follows from the package.

Metadata requires EBF generally (with a stale 1.5-specific Harmony-only section), and the actual 1.6 DLL directly references EBF API. This is a separate health subsystem with persistence and dependency cost, compared with ordinary item-stat quality factors.

#### License

The package includes `LICENSE` stating `Apache-2.0 OR MIT`, and both full license files. The public `SettingsHelper.cs` additionally warns that its helper code was copied from an unknown source and disclaims provenance; do not assume that helper is safe to copy merely from the package's top-level license. [Source helper](https://github.com/WimStienstra/Quality-Bionics-Remastered/blob/053c5676f36c9e1b09be0827540abf31c30c0cdc/Source/QualityBionicsRemastered/SettingsHelper.cs)

### Infusion 2 Continued: a separate randomized equipment system

The package contains no C# sources; Assets.zip contains media/build-manifest assets, not source. Its Workshop links to [AhHanie/Infusion-2](https://github.com/AhHanie/Infusion-2). Read-only source inspection succeeded at tree `71e84560b2ae70a1941aca8e3a1eede37811f8a3`, including 1.6/1.7 source trees. The package IL confirms the core methods below. Precise details of every special effect were not audited; this is a scope/mechanism assessment.

#### How quality matters

This mod makes quality govern the available number of infusion slots and the probability/weight of affix tiers. It does not simply multiply all item stats by one quality curve. `CompQuality.SetQuality` postfix calculates slots, chooses matching infusions and sets them on CompInfusion (with crafting controls). The 1.6 DLL queues a delayed full-HP reset after generated infusions. Its XML TierDefs store distinct quality maps for chances and weights; e.g. Common chance rises from Normal .3 to Masterwork/Legendary 1, while rarer tiers have separate maps. Configuration adjusts base slots by quality, global stat/amount/chance factors, body coverage/layer effects, enabled tiers and disabled infusion defs.

Source confirmation: [CompInfusion](https://github.com/AhHanie/Infusion-2/blob/71e84560b2ae70a1941aca8e3a1eede37811f8a3/1.6/source/Comps/CompInfusion.cs), [quality hook](https://github.com/AhHanie/Infusion-2/blob/71e84560b2ae70a1941aca8e3a1eede37811f8a3/1.6/source/Harmonize/CompQuality.cs), package `1.6/Base/Defs/Tiers/Everything.xml`, dump `Infusion.Harmonize.CompQualityPatches/SetQuality::Postfix`.

#### Targets, effects and filtering

`DefsForReading` caches ThingDefs in Apparel/Weapons or descendants, excludes SingleUseWeapon tags, requires existing CompQuality and no existing CompInfusion, optionally excludes Odyssey unique weapons. Injection inserts CompInfusion and an Infusions inspect tab. This targets modded content using those conventions but does not extend to buildings, food, drugs or materials. Matchers refine affix eligibility by equipment type, weapon class, projectile, shields and utility status. Settings disable particular infusion defs/tiers, and apparel policies can prohibit selected infusions; these are affix filters, rather than a general per-ThingDef allowlist/denylist. [Definition selection](https://github.com/AhHanie/Infusion-2/blob/71e84560b2ae70a1941aca8e3a1eede37811f8a3/1.6/source/DefsForReading.cs)

Package XML demonstrates ordinary stat effects such as blunt/sharp armor, equip delay, mental break threshold, pawn beauty, max HP, insulation, shield capacity, weapon performance; specialized workers add on-hit healing/damage/hediffs, explosions, projectiles, taming, summons, research and skill XP. Later affixes add crafting/construction material refunds and mining loot. These are a substantial enchanting/loot system with items/jobs/policies, not a small stat-effect layer. The Workshop documents that theme; exact worker types are visible in delivered IL and XML.

#### Persistence and removal implications

CompInfusion persists `effectsEnabled`, slotCount, infusionsCount and definition collections `infusions`, `wanting`, `removal`. On load it migrates/replaces infusion defs and rebuilds caches. It refuses stacking (`AllowStackWith` always false) and copies infusion state when split. There are custom infusers, jobs, hediffs and temporary ally components. The GameComponent persists Aegis shield state, unstable projectile choices, pending HP resets, necrosis state and apparel infusion policies. This state is materially beyond vanilla quality.

The loaded-set path calls `SetInfusions(..., true)`; the body rebuilds state/caches and avoids unconditionally healing equipment there. The distinct delayed reset path after quality generation must still be considered when interacting with other max-HP mods. Removing the mod removes these custom definitions and effects; clean removal and retention of health/condition cannot be established from static inspection. [Game component](https://github.com/AhHanie/Infusion-2/blob/71e84560b2ae70a1941aca8e3a1eede37811f8a3/1.6/source/Comps/GameComponent_Infusion.cs)

#### Performance and compatibility

There are cached eligible definitions, per-item stat/on-hit caches, cached pawn gear/stat eligibility and explicit invalidation hooks for equipment/apparel/health changes. GameComponentTick processes pending necrosis/HP resets and periodically clears gear caches (36000 ticks in 1.6). InfusionMapComp ticks components of registered items and cleans up periodically (3600 ticks). This is bounded registries/caches rather than an unrestricted all-item map scan every tick, but more runtime work and more persistent objects than a stat-only approach. No timing benchmark was run.

Separate conditional content folders patch DLCs and Combat Extended. The CE XML explicitly disables the Rare/Legendary Ricochet infusions. The DLL includes CE impact adapters and Toggleable Shields detection. Author-listed compatibility includes Quality and Durability, CE and Quality Colors; transformation weapons can reroll infusions, and some event loot may miss initial affixes. These are author's claims plus observed adapter presence, not reproduced compatibility guarantees. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3536247747)

#### License and reuse

The downloaded package did not contain a license file. Its linked current repository includes GNU LGPL v3 text in LICENSE; GitHub metadata identifies LGPL-3.0. That is a source-level license finding, not a declaration that every bundled media asset shares identical terms. Copying the affix implementation wholesale would also introduce licensing and subsystem complexity that should be explicitly decided. [Repository license](https://github.com/AhHanie/Infusion-2/blob/71e84560b2ae70a1941aca8e3a1eede37811f8a3/LICENSE)

### Implication for the requested feature research

Both families demonstrate legitimate quality-linked gameplay effects. They also establish why the design should distinguish ordinary item/building stat modifiers, installed-body-part health effects and randomized affixes. Bionics can be implemented only with explicit surgery/state-transfer decisions. Infusion-style special behaviors and loot infrastructure should be recorded as researched options, without silently importing an entire enchanting system into basic category toggles. Any decision to omit them from eventual implementation must remain an explicit scope choice, not an implication that these mods contain no quality-driven mechanics.

## Local vanilla baseline

## Local baseline: quality effects

Inspected 18 October 2026. Installed game: 1.7.4981 rev136

This inventory reads shipped XML; it does not prove runtime behavior or include effects implemented only in code.

### Shipped stat quality parts

| Stat | Definition | Part and configured values |
| --- | --- | --- |
| ArmorRatingBase | `Data\Core\Defs\Stats\Stats_Apparel.xml` | StatPart_Quality: factorAwful=0.60; factorPoor=0.80; factorNormal=1.00; factorGood=1.15; factorExcellent=1.30; factorMasterwork=1.45; factorLegendary=1.80 |
| InsulationBase | `Data\Core\Defs\Stats\Stats_Apparel.xml` | StatPart_Quality: factorAwful=0.8; factorPoor=0.9; factorNormal=1; factorGood=1.1; factorExcellent=1.2; factorMasterwork=1.5; factorLegendary=1.8; applyToNegativeValues=true |
| EnergyShieldEnergyMax | `Data\Core\Defs\Stats\Stats_Apparel.xml` | StatPart_Quality: factorAwful=0.6; factorPoor=0.8; factorNormal=1; factorGood=1.2; factorExcellent=1.4; factorMasterwork=1.7; factorLegendary=2.1 |
| EnergyShieldRechargeRate | `Data\Core\Defs\Stats\Stats_Apparel.xml` | StatPart_Quality: factorAwful=0.9; factorPoor=0.95; factorNormal=1; factorGood=1.05; factorExcellent=1.1; factorMasterwork=1.2; factorLegendary=1.3 |
| PackRadius | `Data\Core\Defs\Stats\Stats_Apparel.xml` | StatPart_Quality: factorAwful=0.84; factorPoor=0.92; factorNormal=1; factorGood=1.08; factorExcellent=1.16; factorMasterwork=1.3; factorLegendary=1.5 |
| MarketValueBase | `Data\Core\Defs\Stats\Stats_Basics_General.xml` | StatPart_Quality: factorAwful=0.5; factorPoor=0.75; factorNormal=1; factorGood=1.25; maxGainGood=500; factorExcellent=1.5; maxGainExcellent=1000; factorMasterwork=2.5; maxGainMasterwork=2000; factorLegendary=5; maxGainLegendary=3000 |
| DeteriorationRate | `Data\Core\Defs\Stats\Stats_Basics_General.xml` | StatPart_Quality: factorAwful=2; factorPoor=1.5; factorNormal=1; factorGood=0.8; factorExcellent=0.6; factorMasterwork=0.3; factorLegendary=0.1 |
| Beauty | `Data\Core\Defs\Stats\Stats_Basics_General.xml` | StatPart_Quality: factorAwful=-0.1; factorPoor=0.5; factorNormal=1; factorGood=2; factorExcellent=3; factorMasterwork=5; factorLegendary=8 |
| Comfort | `Data\Core\Defs\Stats\Stats_Basics_General.xml` | StatPart_Quality: factorAwful=0.76; factorPoor=0.88; factorNormal=1; factorGood=1.12; factorExcellent=1.24; factorMasterwork=1.45; factorLegendary=1.7 |
| PsychicSensitivityOffset | `Data\Core\Defs\Stats\Stats_Basics_Special.xml` | StatPart_Quality: factorAwful=0.5; factorPoor=0.66; factorNormal=0.83; factorGood=1.0; factorExcellent=1.16; factorMasterwork=1.32; factorLegendary=1.5; applyToNegativeValues=true |
| BedRestEffectiveness | `Data\Core\Defs\Stats\Stats_Building_Special.xml` | StatPart_Quality: factorAwful=0.86; factorPoor=0.92; factorNormal=1; factorGood=1.08; factorExcellent=1.14; factorMasterwork=1.25; factorLegendary=1.6 |
| JoyGainFactor | `Data\Core\Defs\Stats\Stats_Building_Special.xml` | StatPart_Quality: factorAwful=0.76; factorPoor=0.88; factorNormal=1; factorGood=1.12; factorExcellent=1.24; factorMasterwork=1.4; factorLegendary=1.8 |
| SurgerySuccessChanceFactor | `Data\Core\Defs\Stats\Stats_Building_Special.xml` | StatPart_Quality: factorAwful=0.90; factorPoor=0.95; factorNormal=1; factorGood=1.05; factorExcellent=1.1; factorMasterwork=1.15; factorLegendary=1.3 |
| MeleeWeapon_DamageMultiplier | `Data\Core\Defs\Stats\Stats_Weapons_Melee.xml` | StatPart_Quality: factorAwful=0.8; factorPoor=0.9; factorNormal=1; factorGood=1.1; factorExcellent=1.2; factorMasterwork=1.45; factorLegendary=1.65 |
| AccuracyBase | `Data\Core\Defs\Stats\Stats_Weapons_Ranged.xml` | StatPart_Quality: factorAwful=0.8; factorPoor=0.9; factorNormal=1; factorGood=1.1; factorExcellent=1.2; factorMasterwork=1.35; factorLegendary=1.5 |
| RangedWeapon_DamageMultiplier | `Data\Core\Defs\Stats\Stats_Weapons_Ranged.xml` | StatPart_Quality: factorAwful=0.9; factorPoor=1; factorNormal=1; factorGood=1; factorExcellent=1; factorMasterwork=1.25; factorLegendary=1.5 |
| RangedWeapon_ArmorPenetrationMultiplier | `Data\Core\Defs\Stats\Stats_Weapons_Ranged.xml` | StatPart_Quality: factorAwful=0.9; factorPoor=1; factorNormal=1; factorGood=1; factorExcellent=1; factorMasterwork=1.25; factorLegendary=1.5 |
| JumpRange | `Data\Royalty\Defs\Stats\Stats_Apparel.xml` | StatPart_Quality: factorAwful=0.75; factorPoor=0.9; factorNormal=1; factorGood=1.06; factorExcellent=1.13; factorMasterwork=1.19; factorLegendary=1.25 |

### Current mod seams

- `Source/Quality_DefUtility.cs`: centralized quality eligibility and category/individual controls. Existing selection controls whether a thing receives quality, not which effects apply.
- `Source/Quality_CompPatch.cs`: adds vanilla `CompQuality` and applies quality bounds; no HP multiplier.
- `Source/Quality_Construction.cs`: replaces construction quality generation and reads material quality.
- `Source/QualityEverything.csproj`: compile reference `Krafs.Rimworld.Ref` 1.6.4633, net48.
- `LoadFolders.xml`: 1.6 and 1.7 both load `1.6` and `Common`.
- Local `Data/Core/Defs/Stats/Stats_Basics_General.xml`, `MaxHitPoints`: no quality stat part.

### Interpretation for design (recommendations, not accepted decisions)

- Keep quality assignment and optional quality effects separate. Excluding a weapon from extra effects should not erase its vanilla quality.
- Start from an explicit registry of supported effects; inspect each consumer before assuming that every stat handles quality or signed offsets identically.
- Reuse classification and per-def identifiers where possible, but specify separate effect filters and precedence before implementation.
- Support for modded defs can follow shared stats and base classes; bespoke custom mechanics may need explicit adapters.
- Treat 1.6 and 1.7 as separate validation targets. The local installed version only establishes 1.7 data availability.

## Runtime evidence and version limits

## Runtime seams for optional quality effects

Inspected 18 October 2026. Research only; no mod implementation changed.

### Evidence and version limits

Primary evidence is the installed game's own IL, read as data with Mono.Cecil, without loading or executing game or Workshop assemblies:

- [Installed 1.7 Assembly-CSharp.dll](E:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll), installed version 1.7.4981 rev136.
- [1.6 reference assembly](C:/Users/Kyle/.nuget/packages/krafs.rimworld.ref/1.6.4633/ref/net472/Assembly-CSharp.dll), Krafs.Rimworld.Ref 1.6.4633.
- [Captured core IL](../../.scratch/quality-effects/runtime-seams-il.txt), [extra lifecycle IL](../../.scratch/quality-effects/runtime-extra-il.txt), [construction and HP IL](../../.scratch/quality-effects/hp-il.txt). These are local inspection scratch, not redistribution candidates.

The 1.6 reference contains signatures with zero-length method bodies. It proves the named APIs exist in that target, **not** that their implementations match 1.7. No full 1.6 runtime assembly was inspected. Actual 1.6 play/save validation remains required.

### Ordinary stats and signed offsets

`RimWorld.StatPart_Quality.TransformValue` (1.7 IL_0000–0040) returns immediately when `val <= 0` unless `applyToNegativeValues` is true. Otherwise it computes `gain = val * qualityFactor - val`, caps that with `Mathf.Min(gain, maxGain)`, then adds the capped gain. Default factors are 1 and max gains are 9,999,999; `applyToNegativeValues` defaults to false. This is multiplicative behavior, not an additive bonus. Zero stays zero, including when negative values are enabled. [Source: core IL, `StatPart_Quality.TransformValue` and constructor.]

Consequences for design (recommendations):

- Positive armor, speed, HP etc. can use a small quality-aware stat part with eligibility checks.
- Signed equipment offsets need explicit policy. Multiplying a negative penalty by 1.5 makes that penalty worse. A single blanket multiplier is not equivalent to “higher quality is better.” Some lower values are desirable, so each supported effect needs a direction, rather than automatically applying the same factor to every stat.
- Avoid stacking another multiplier over vanilla quality parts unless intentional. The existing vanilla part should remain authoritative for already-covered effects.

`StatWorker.StatOffsetFromGear(Thing gear, StatDef stat)` first reads `gear.def.equippedStatOffsets`, adds relevant bladelink trait offsets, and, when the result is nonzero, applies **every part on the target stat** using `StatRequest.For(gear)`. Thus a stat part on a pawn stat can also transform that stat's gear contribution. It does not simply read the gear's usual stat value. [Source: core IL, `StatOffsetFromGear`, IL_0000–00a0.]

This is a useful existing seam for quality-sensitive equipped offsets. It also means a broad new stat part must distinguish the gear request from a pawn request and must not multiply the entire pawn stat accidentally. The current method aggregates base and bladelink offsets before transforming, so a part naturally affects their combined contribution. Deciding whether trait contributions should scale is a design question.

`StatRequest.For(Thing)` records `TryGetQuality` into the request; the buildable-def overload records its supplied quality. A custom effect should guard real instances with an actual `CompQuality`/successful quality lookup, not assume every thing has usable quality merely because a request exposes a quality enum. Abstract info-card requests need an explicit policy so they remain meaningful. [Source: `inspect-factory.ps1` capture and `StatRequest.For` methods in the installed assembly.]

### HP lifecycle and uninstall consequences

| Seam | Observed 1.7 behavior | Consequence |
| --- | --- | --- |
| `Thing.MaxHitPoints` | `GetStatValue(MaxHitPoints, true, 10)`, rounded to int | Stat-based HP reaches the normal damage/repair consumers; cache invalidation matters. |
| `Thing.HitPoints` setter | Assigns `hitPointsInt` directly | There is no automatic range clamp in this setter. |
| `Thing.PostMake` | Sets HP to rounded `MaxHitPoints * Clamp01(startingHpRange.RandomInRange)` | Initial HP is calculated during construction of the object. |
| `ThingWithComps.PostMake` | Calls `Thing.PostMake` **before** initializing comps | The initial HP query occurs before `CompQuality` initialization. |
| `CompQuality.SetQuality` | Stores quality, initializes art when requested, calls `parent.PostQualitySet`, then facility quality hook | Does not itself invalidate stat caches or resize HP. |
| `Thing.PostQualitySet` | Empty virtual method | A quality change alone does not make existing HP match new MaxHP. |
| `Frame.CompleteConstruction` | Assigns building quality, then sets building HP to `ceil(frame.HP / frame.MaxHP * building.MaxHP)` before spawning | Construction naturally transfers damage proportion to the quality-scaled building maximum, provided the stat cache is current. |
| `Thing.ExposeData` | Serializes `health` directly into `hitPointsInt` | Saved HP is absolute, not a saved percentage. |
| `Thing.SpawnSetup` | No general `HitPoints <= MaxHitPoints` clamp; contains a destroyed-object recovery to at least 1 HP | Do not promise uninstall clamps excess HP automatically. |

Sources: core IL and extra lifecycle IL for named methods; construction IL `Frame.CompleteConstruction`, quality at IL_0138 and HP transfer at IL_01b6–01d5.

Recommendations: treat HP as a lifecycle feature, not just an XML multiplier. Define whether live setting changes preserve damage ratio or clamp absolute HP. Make fresh generated/crafted things receive the intended HP after quality assignment. Changing settings must not become a repair exploit. Clear MaxHP caches before reading a changed maximum, and update repairable-building membership when appropriate (`ListerBuildingsRepairable.UpdateBuilding` compares current to maximum).

Removal can be structurally simple when using vanilla `CompQuality` and computed stats without saved custom Thing classes or required comps. It cannot be described as lossless: saves retain absolute HP, quality labels may disappear on defs that lose their quality comp, quality-separated stacks may merge, and weakened maxima can leave excess HP without explicit cleanup. A pre-removal disable/normalize workflow is safer than a blanket promise; its scope must include unspawned gear, inventories, containers and world-held things, not maps alone. Those are design recommendations, not verified implemented behavior.

### Power and batteries need dedicated adapters

`CompPowerTrader.PowerOutput` returns `powerOutputInt`, or zero during EMP stun. Positive is production; negative is draw. `EnergyOutputPerTick` multiplies the getter by watts-to-energy conversion. `SetUpPowerVars` assigns negative idle draw or negative `Props.PowerConsumption`; the property includes research upgrade factors. [Source: core IL for `CompPowerTrader` and extra IL for `CompProperties_Power.get_PowerConsumption`.]

A stat part alone does not affect this field path. A getter adjustment can cover vanilla callers that use `PowerOutput`, including generators whose output changes over time, while preserving their calculations. Production and consumption must be treated separately; higher quality should not accidentally multiply demand upward. Avoid repeatedly scaling the stored field: dynamic producers overwrite it, and repeated scaling can compound. Custom subclasses or mods that bypass this getter require separate evidence/adapters. EMP must still return zero.

Battery capacity and efficiency are shared properties, accessed directly:

- `AmountCanAccept = (Props.storedEnergyMax - storedEnergy) / Props.efficiency`, with broken/EMP early returns.
- `AddEnergy` limits incoming energy using `AmountCanAccept`, then multiplies it by `Props.efficiency`.
- `StoredEnergyPct = storedEnergy / Props.storedEnergyMax`.
- `SetStoredEnergyPct` clamps its fraction to [0,1], then multiplies by `Props.storedEnergyMax`.
- `PostExposeData` serializes `storedPower` then clamps stored energy above `Props.storedEnergyMax`; the inspected code does not guard that clamp to a particular load/save stage.
- `CompInspectStringExtra` displays both property fields directly.
- `get_Props` simply casts the comp's `props` reference; it does not create a per-instance copy.

[Source: core IL, named `CompPowerBattery` methods; factory capture for `get_Props`.]

A capacity-only patch to `AmountCanAccept` leaves percentage display, setters, load/save cap and inspect text inconsistent. Mutating shared battery properties for one quality would affect other instances using that definition. A per-instance derived properties approach could centralize reads, but introduces identity, allocation, invalidation and third-party mutation questions. A bounded adapter across the listed consumers is more explicit but has more patch seams. This is a higher-maintenance effect than normal stat multipliers; do not claim universal custom-battery compatibility. Removing a boosted capacity can legitimately discard stored energy because vanilla serialization caps it.

### Cache behavior and performance

`StatWorker.GetValue(Thing, ..., cacheStaleAfterTicks)` checks the immutable cache first when `stat.immutable`; it does not consult the stale-tick bypass there. Otherwise its temporary cache is valid while `TicksGame - entry.gameTick < cacheStaleAfterTicks`; `-1` bypasses that temporary read. `MaxHitPoints` supplies 10. [Source: core IL `StatWorker.GetValue(Thing,bool,int)`.]

`ClearCacheForThing` removes an entry from both caches. `TryClearCache` clears both entire caches; `DeleteStatCache` nulls them and is not a normal invalidation operation. `CompQuality.SetQuality` has no call to these. [Source: core IL and caller inventory from `inspect-callers.ps1`.]

Recommendations: resolve category/filter policy once per def after definitions load or settings change; use a small effect registry and seven precomputed factors. Keep stat-path evaluation to constant-time eligibility and factor lookup. Invalidate only affected StatWorkers on settings changes and clear the changed thing's affected caches on quality changes. Do not globally disable game caching. If gear offsets change, account for cached pawn stats as well as gear stats; clearing only a gear thing cannot invalidate a cache entry keyed by its wearer. Prefer invalidation events over per-tick whole-map rescans. Custom consumer caches still need case-specific investigation.

### Existing 1.7 compatibility issue to track separately

The current `Source/Quality_Construction.cs` transpiler resolves the exact overload `QualityUtility.GenerateQualityCreatedByPawn(Pawn, SkillDef)`. Installed 1.7 `Frame.CompleteConstruction` calls `(Pawn, SkillDef, bool)` instead (IL_0127). The replacement may therefore miss 1.7 construction calls. This is an observed signature mismatch, not an in-game reproduction or a change made in this research.

### Food poisoning consumer clarification (installed 1.7)

The patch-only Food Quality mod 2754055111 was identified by the main researcher as adding quality parts to Nutrition and FoodPoisonChanceFixedHuman. Inspecting the latter's actual consumers establishes a narrower effect than “quality reduces food poisoning”:

- `Thing.Ingested` IL_0598–05d6 first asks `FoodUtility.TryGetFoodPoisoningChanceOverrideFromTraits`. Without an override it computes the food Thing's `FoodPoisonChanceFixedHuman * FoodUtility.GetFoodPoisonChanceFactor(ingester)` and rolls for a humanlike ingester. An override supplies its own probability. This is the food's inherent ingestion risk.
- `CompFoodPoisonable.Notify_RecipeProduced` separately rolls the **room** `RoomStatDefOf.FoodPoisonChance`, then the **cook pawn** `StatDefOf.FoodPoisonChance`; it marks the meal poisoned with a cause. It does not query the finished food's FoodPoisonChanceFixedHuman.
- `CompFoodPoisonable.PostIngested` rolls saved `poisonPct * GetFoodPoisonChanceFactor(ingester)` and applies its stored cause. A quality part on FoodPoisonChanceFixedHuman does not modify that saved contamination.
- `CompRottable.PostIngested` is another separate route: a nonfresh rot stage and a positive poison factor produce rotten-food poisoning; it does not query FoodPoisonChanceFixedHuman.

Primary evidence: installed Assembly-CSharp.dll, captured by `inspect-foodpoison.ps1` into [foodpoison-il.txt](../../.scratch/quality-effects/foodpoison-il.txt), named methods. No game execution. Research recommendation: label the simple stat effect “inherent food poisoning risk”; changing cook contamination or rotten-food outcomes requires separate explicitly chosen mechanics. Multipliers also leave a zero inherent base risk at zero.

## Additional source discovery: shelf continuation and facility quality

The [Shelf Quality Matters continuation](https://steamcommunity.com/sharedfiles/filedetails/?id=3292740066) is tied to its Workshop item by the repository PublishedFileId. At revision `53a1c78be944f25e1dd5af9115f4d41616ad2b8c`, its storage getter source is identical to the original source inspected above. Its 1.5 XML changes ShelfBase to inherit FurnitureWithQualityBase. This adds quality eligibility but retains the original absolute-capacity replacement and cached-quality limitations. [Getter](https://github.com/Roasio2370/ShelfQualityMatters_Continued/blob/53a1c78be944f25e1dd5af9115f4d41616ad2b8c/Source/Patches.cs), [shelf XML](https://github.com/Roasio2370/ShelfQualityMatters_Continued/blob/53a1c78be944f25e1dd5af9115f4d41616ad2b8c/1.5/Patches/ShelfPatch.xml), [Workshop identity](https://github.com/Roasio2370/ShelfQualityMatters_Continued/blob/53a1c78be944f25e1dd5af9115f4d41616ad2b8c/About/PublishedFileId.txt).

Quality Furniture Matters is an adjacent public repository rather than a verified Workshop listing. The author describes quality-dependent additive rest-effectiveness contributions from linked dressers/end tables: -1%, -.5%, 0%, +.5%, +1%, +2%, +4%, with per-type and total caps. The shipped XML attaches a custom `StatPart_FacilityQuality` to BedRestEffectiveness and defines configurable furniture def-name lists. This demonstrates a cross-object effect: a facility's quality affects a bed's stat. The repository states its C# source moved to private backup, so the exact counting/cache mechanics were not inspected. About.xml declares 1.6. README default caps and an older XML comment disagree; runtime defaults are unproven. Recommendation: investigate facility changes and dependent-bed cache invalidation before supporting this pattern. [Author README](https://github.com/GrandMasterK414/QualityFurnitureMatters/blob/9574cb9e3bea2d56cb22a0ab147cdd9c014d7f35/README.md), [stat-part XML](https://github.com/GrandMasterK414/QualityFurnitureMatters/blob/9574cb9e3bea2d56cb22a0ab147cdd9c014d7f35/Patches/Patches.xml), [definition lists](https://github.com/GrandMasterK414/QualityFurnitureMatters/blob/9574cb9e3bea2d56cb22a0ab147cdd9c014d7f35/Defs/QualityFurnitureMattersConfigDefs.xml).

## Requirement audit

| Requirement | Research evidence and recommendation | Status |
| --- | --- | --- |
| Four supplied examples | Workshop metadata plus downloaded XML/source or selected delivered IL; exact curves and hooks recorded | Covered |
| Broad Workshop investigation | Feature catalogue, search queries, related mods, package manifest and exclusions; finite coverage explicitly bounded | Covered within documented search scope |
| What effects and how implemented | Source/IL tables and actual consumer analysis distinguish stat parts, direct patches and persistent systems | Covered |
| Category toggles and item black/whitelist | Existing eligibility reviewed; recommend independent effects policy and inherit/include/exclude overrides | Recommendation recorded; detailed design deferred by user |
| Vanilla and modded content | Structural/shared-stat seams and custom-consumer exceptions identified | Recommendation recorded; no universal compatibility claim |
| Maintainability and proportional complexity | Small explicit registry, shared classification, bounded adapters; avoid full-def replacement and copied frameworks | Recommendation recorded |
| Performance | Hot-path lookups, cache invalidation, periodic scans and persistent updater costs analyzed statically | Research covered; benchmark future implementation |
| Midgame uninstall where feasible | Absolute HP, charge, stacks, active hediffs, installed parts and affixes evaluated | Risks and possible normalization approach recorded; no guarantee |
| RimWorld 1.6 and 1.7 | Actual package metadata/references, Infusion separate builds, installed1.7 IL and reference-only1.6 limits | Research covered; two-version release validation still needed later |
| No implementation changes | Working-tree audit shows no new tracked Source/Common/1.6 changes from this effort | Preserved |
| Research and wayfinder skills | Background primary-source research, single consolidated report and closed local research map | Completed for research-first destination |
