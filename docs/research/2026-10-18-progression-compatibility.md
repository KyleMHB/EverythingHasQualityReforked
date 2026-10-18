# Progression compatibility: collection and installed-package research

Research date: 18 October 2026. Research only. No deployed files, game settings, load order, saves, or implementation were changed. Steam/local evidence was gathered by the research agent; the Discord observations below were supplied by the parent agent after reading the authenticated Chrome session.

This is the research-phase snapshot. The [approved plan](../../plan%202026-10-18.md), [conditional-effect decisions](2026-10-18-conditional-effects.md) and [implemented behavior guide](../quality-guide.md) record subsequent choices and source-level follow-ups. Runtime Progression compatibility remains unverified.

## Findings that affect the design

Progression compatibility needs to preserve deliberate exclusions and production balance, not merely avoid startup errors. The strongest concrete cases are Prisoner Jumpsuits - No Quality, disabled Dubs Bad Hygiene quality components, factory products intended to remain Normal, and Processor Framework processes with their own quality rules. These are evidenced below. They make automatic blanket quality enablement inappropriate for a Progression preset.

The current user setup already includes the local Everything Has Quality Reforked under package ID `cozarkian.qualityframework`. That ID is **not evidence of a second quality framework**. Installed Quality And Durability is inactive. These observations describe this user's setup, not official collection membership.

The saved mod list is nearly the active list but is not an exact restoration point: one active mod is absent. Preserve the actual current configuration before later testing.

## Collection identity and public guidance

The [main collection](https://steamcommunity.com/workshop/filedetails/?id=3521297585&l=english) is **The Progression Modpack [UPDATED 1.7]**, assembled by ferny. It presents a cohesive, beginner-oriented progression overhaul, maintained through community testing and patches. The retrieved main page lists 864 entries; linked collections are [Content, part 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3521319712) and [Cosmetics, part 3](https://steamcommunity.com/sharedfiles/filedetails/?id=3637541646). Main-page linked counts were 430/195, while directly retrieved Content displayed 429. Other item pages displayed 867/868 for the parent. These are changing/cached observations, not a locked manifest; do not add them into a claimed exact pack size.

The public description emphasizes coherent presentation, removing duplicated content, performance, and integrating new content into existing progression. It does not establish this mod's compatibility or explain specific quality-mod rejections. Text searches of the retrieved collection pages did not reveal Everything Has Quality, Quality Expanded, or an Infusion listing; incidental references in dependencies/descriptions are not membership. Absence is not proof of rejection. [Main collection](https://steamcommunity.com/workshop/filedetails/?id=3521297585&l=english).

[Ferny's Mod Configs](https://steamcommunity.com/sharedfiles/filedetails/?id=3256902751&l=english) supplies curated settings through Auto Mod Config. Its author describes these as ongoing balance and presentation choices. Local preset identity is `FernyConfigs3`, label `Ferny Configs`, version `5.6`; these identifiers do not mean collection version 5.6. Source: installed `3256902751/Defs/Preset.xml` and `README.md`, with upstream repository [Fernys-Mod-Configs](https://github.com/fernyrepos/Fernys-Mod-Configs).

[Load Them Last](https://steamcommunity.com/sharedfiles/filedetails/?id=3714307386) moves selected mods to the end; its published order is No Ambient Ducking then MissileGirl. [Missile Girl](https://steamcommunity.com/sharedfiles/filedetails/?id=3712928623) requests last position and describes caching patched definitions and calculated stats. Therefore the later test plan must preserve the pack's final order and check cold as well as warm startup when component or stat configuration changes. A correct startup with cached definitions alone is insufficient evidence.

No authoritative full installation/load-order text or individual quality-mod rejection history was recovered from these public text pages. Some presentation is image-based. Read Discord and any linked current setup instructions before choosing an installation procedure; do not substitute third-party guides.

## Read-only local baseline

Sources inspected:

- Active configuration: `C:/Users/Kyle/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Config/ModsConfig.xml`.
- Saved list: `C:/Users/Kyle/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/ModLists/Kyle Prog.rml`.
- Installed Workshop metadata and selected XML: `E:/Steam/steamapps/workshop/content/294100/<Workshop ID>/`.
- Local deployed metadata: `E:/Steam/steamapps/common/RimWorld/Mods/EverythingHasQualityReforked/About/About.xml`.

At inspection the active config reported **1.7.4981 rev136** and **1,522 package IDs**. `Kyle Prog.rml` reported the same game build and **1,521 IDs**. The only membership difference was `mlie.prepareforcombat`, present in active config only. Shared package IDs had identical order. This detects the saved/current discrepancy; it does not authorize changing either.

| File | SHA-256 at inspection |
| --- | --- |
| ModsConfig.xml | `7F71EB07821168920ECA1BCB90210D42272ACFA2B2468EE0C4CD0FE05A44A04A` |
| Kyle Prog.rml | `2C6C46E429C395BFE80112745F40682D8D77DB7B29AF12D146887AEF6F48CF2A` |

These hashes are reference evidence, not backups. Recheck current state when the user finishes playing and before switching lists. Do not infer that files read now are the exact in-memory state of the currently running process.

## Confirmed overlaps and compatibility boundaries

| Installed/active integration | Evidence | Implication for this mod |
| --- | --- | --- |
| Prisoner Jumpsuits - No Quality (`ryuudra.prisonerjumpsuits.noquality`, 3544594291) | Active; `Patches/patch.xml` replaces inherited comps on `OCARINA_PrisonerJumpsuitBase`, retaining color/forbid support and omitting quality. Author explicitly describes quality removal. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3544594291). | Preserve the intentional exclusion in any Progression preset. Inspect descendants after XML inheritance, rather than matching a guessed list of item labels. General apparel enablement could otherwise undo the patch. |
| Dubs Bad Hygiene + Ferny's preset | Installed `3256902751/Settings/Mod_836308268_DubsBadHygieneMod.xml` has `<QualityComps>False</QualityComps>`. | Do not silently re-add quality to those buildings in the compatibility preset. Curator rationale and exact affected definitions still need verification. A disabled setting is evidence of configuration, not its motivation. |
| Progression: Factories (`ferny.progressionfactories`, 3690565613) | Active; installed `Mods and Shit/VFE Factory/Defs/vfefactory_researchprojects.xml`, lines 20 and 195, describes Normal-only output for the automated loom and machining bay. Public page establishes its VFE Factory relationship. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3690565613), [upstream](https://github.com/fernyrepos/Progression-Factories). | Preserve deliberate machine output quality unless the user explicitly chooses an override. Production without a pawn needs a defined policy; a random fallback is not automatically compatible. Actual factory implementation must be traced before designing an adapter. |
| Progression: Kitchen (`ferny.progressionkitchen`, 3424640315) | Active; installed `Mods and Shit/Industrial Hardleather Concrete and Cheese/Defs/industrial cheese press processes.xml` defines `ProcessorFramework.ProcessDef` recipes; apple-juice process explicitly sets `usesQuality=false`. [Upstream](https://github.com/fernyrepos/Progression-Kitchen). | Vanilla fermentation patches do not cover every process. Respect a process's native quality policy and avoid a second result-quality assignment. Audit process consumers before promising broader brewing support. |
| Progression: Storage (`ferny.progressionstorage`, 3292746186) | Active with Adaptive Storage Framework, Reel's Expanded Storage, Adaptive Primitive Storage, Neat Storage, MO Adaptive Storage, and gravship storage. Author documents removals, replacements, labeling, and Deep Storage incompatibility. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3292746186), [upstream](https://github.com/fernyrepos/Progression-Storage). | Resolve actual surviving definitions after patches. Proposed capacity effects must compose with ASF; changing a shared comp property is unsuitable. Do not add Deep Storage as a testing dependency for this pack. |
| Even Storage (`custom.evenstorage`, 3684119183) | Active; author describes distribution by type/material across linked storage groups regardless of item quality. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3684119183). | Test quality preservation when splitting, moving, and merging resources; capacity effects also need overflow checks. This is a testing target, not a confirmed defect. |
| Quality Overlay (`telardo.qualityoverlay`, 2568283376) | Active; author describes quality overlays for items/buildings. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2568283376). | Keep native CompQuality behavior consumable; verify newly quality-bearing definitions display sensibly. No blanket compatibility claim from the description. |
| Quality Colors Continued (`dawnsglow.qualcolor`, 3513846773) | Active; presentation of quality in trade UI. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3513846773). | Check trading with added resource/food qualities, alongside the pack's Trade UI Revised. It is presentation overlap, not duplicate generation/effect functionality. |
| No Quality Filter (`rabiosus.noqualityfilter`, 3528263608) | Active; author describes weapon/apparel filters, and says body-part support requires another SpecialThingFilterDef. Installed `Defs/filter.xml` and assembly exist. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3528263608). | Do not interpret its name as disabling quality. New quality-bearing categories may need appropriate filter behavior; test stockpile/bill selections rather than assume generic coverage. |
| Quality And Durability Continued (`mlie.qualityanddurability`, 2572086023) | Installed About.xml, absent from active IDs. | No evidence the pack currently activates it. Its installation may be from previous research. Do not claim curator rejection or add it to the test baseline. |
| Local Everything Has Quality Reforked (`cozarkian.qualityframework`) | Active ID maps to deployed local mod. | Preserve package identity and existing settings migration. It is not in itself evidence that the official Progression collection includes this fork. |

Other active targets relevant to expanded effects include Progression: Production, Education, Bionics, Drugs, Agriculture; Vanilla Skills Expanded and Alpha Skills; Most Skilled Bill Restriction; Alpha Crafts; Food Poisoning Stack Fix; Pick Up And Haul; and Performance Optimizer. Their presence is confirmed from active IDs and installed About.xml. It does not establish which methods they patch or whether a proposed effect works. Investigate only the selected feature's consumer paths, avoiding an unnecessary audit of all 1,522 entries.

## Recommended compatibility design and focused test sequence

These are recommendations for the spec, not implementation or accepted balance decisions.

1. Offer an explicit Progression preset whose exceptions are visible and editable. Preserve native/other-mod quality behavior, deliberate removals, and production limits. Keep generic category/item rules independent of this preset. Decide whether it should be suggested or automatic in the interview; never silently alter the user's saved selections.
2. Keep quality assignment and optional effects separate. Check both existing effects and native qualities before adding scaling. Build a small set of adapters only for selected mechanics. Processor outputs, factory generation, storage, drugs, and installed bionics require different consumers/lifecycles.
3. Keep the existing pack research gates and definitions. Do not unlock recipes, replace research, resurrect removed buildings, or assume label/category names remain vanilla. Category-tree discovery should run after definitions are finalized and handle mod-added branches.
4. Initial test list: Core, applicable DLC, Harmony, this mod, and only development tools needed for a disposable test colony. Test options, persistence, generation, exclusions, and save/load here; do not load the full pack for every iteration.
5. Targeted lists: add the dependencies and relevant integrations for one feature family at a time—intentional exclusions; factory/process output; storage/hauling; trade/presentation; skills/inspirations; selected health/drug adapters. Use the user's saved ordering for shared mods rather than guessing an order. Include the curated config preset only when exercising its behavior, since it writes settings by design.
6. Final checks: exact current full Progression list plus the built mod, copied test saves, cold and warm startup, representative early-/late-era production, UI responsiveness with the full definition count, and a short performance comparison focused on resource stack fragmentation and selected effects. Restore the exact pre-test list/settings after testing. One stable full-pack run is a smoke check, not exhaustive compatibility certification.

The user is playing now. No switching mod lists, changing configs, deployment, game automation, or restart may occur until they say the playing session is over. Research reads and local documentation are allowed. Existing saved lists must not be overwritten.

## Curator Discord evidence observed by the parent agent

The parent agent read the original messages and surrounding context through the user's Chrome session, without sending messages. These observations narrow the public-page uncertainty:

- ferny announced Prisoner Jumpsuits - No Quality on **8 September 2025**, confirming deliberate curator inclusion rather than merely a personal installed mod. [Announcement](https://discord.com/channels/1194271482533138502/1392662695005655100/1414698591158665328).
- Quality Colors was added on **24 March 2026**. [Announcement](https://discord.com/channels/1194271482533138502/1392662695005655100/1485854813693542531).
- Quality Overlay was added on **4 December 2025**. ferny reported approximately **0.004 ms** overhead in that context; this is the curator's measurement claim, with no benchmark protocol established here. It must not be applied to this mod or to all quality effects. [Announcement](https://discord.com/channels/1194271482533138502/1392662695005655100/1445913034965909504).
- The **6 June 2026** design-principles discussion favors measuring performance instead of judging mod count, focused mods, predictable behavior, and isolatable patches. Surprise merged behavior and patch compilations are maintainability concerns for the curator. [Performance](https://discord.com/channels/1194271482533138502/1461539261814931712/1512870210498072697), [focused behavior](https://discord.com/channels/1194271482533138502/1461539261814931712/1512870229976289351), [patch isolation](https://discord.com/channels/1194271482533138502/1461539261814931712/1512870246757699634).

Implication: any optional effects expansion should have explicit switches and visible ownership, preserve deliberate exclusions, and make individual integrations easy to disable. This does not prove the curator endorses the planned expansion or establish a rejection reason for Quality Expanded, Infusion, or Everything Has Quality.

## Outstanding evidence

### Additional authenticated Discord search, 18 October 2026

The parent searched `quality` server-wide (1,623 reported results), `in:pack-updates quality` (47 results; both pages inspected), the phrases `quality expanded`, `quality matters`, `durability`, and quality in the current additions forum. The targeted searches did not establish a curator rejection reason for the broad quality-effect mods. Discord search behavior, archives, inaccessible replies and changing channel layout limit negative conclusions; this is not an exhaustive read of all server history. Quality Builder received a July 2026 UI objection, but that is a construction-automation mod and must not be conflated with this mod or quality-stat expansions.

The focused query `in:mod-developers bionics after:2026-10-12` returned eight results. Reading the original October 17 discussion showed users waiting for Elite Bionics Framework because of reported compatibility problems and their Quality Bionics dependency. One participant corrected their earlier claim about a 1.7 version. These are dated user reports, not a reproduced defect or proof of official pack rejection. [Original discussion and surrounding context](https://discord.com/channels/1194271482533138502/1278122871922622565/1560907646238724128). If installed-bionic effects are selected, inspect current framework support before making it a dependency and test the actual active health-overhaul combination.

- Curators' Discord discussion explaining quality-mod inclusion/removal and performance/balance objections; parent research owns browser access.
- Current official setup instructions and any recommended pack-specific quality settings not visible in retrieved text.
- Exact runtime consumers for factory and Processor Framework quality, and proposed storage/bionic/drug effects.
- Whether a Progression preset should suppress all HP/market-value/resource effects by default or use a curated opt-in set.
- Actual gameplay behavior of current patches on 1.7; this note did not execute the game.

Public Steam extraction sometimes included contradictory generic removal/incompatibility banners while also returning current descriptions. Those banners were not treated as evidence that the collection is unavailable or broken. A direct sharedfiles URL initially returned a region error; the normal workshop/filedetails variant returned the public collection. Some pages and counts disagree due to retrieval timing/cache. Assertions above distinguish installed evidence, author claims, and recommendations.
