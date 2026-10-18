[h1]Description[/h1]
Everything Has Quality Reforked gives more RimWorld items quality, with inherited category rules, individual exceptions and optional quality effects. It supports RimWorld 1.6 and 1.7 and requires [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url] before this mod.

Cozarkian created Everything Has Quality. pudy248 made the intermediate fork, and KyleMHB maintains this refork.

[h1]Quality rules[/h1]
Choose defaults for buildings, resources, ingredients, meals, drugs, medicine, apparel, weapons, shells and other eligible items. Set quality bounds and production options for materials, work tables, skills and inspirations.

Use Include, Exclude or Inherit on an item or category branch. An item exception wins over its category. The deepest category rule wins over a broader branch, and equally specific conflicting rules exclude. Excluding assignment leaves quality supplied by the game or another mod intact. Optional effects have their own participation rules.

The options show translated names, source mods and the rule behind each result. Search and filters narrow the list. Bulk actions apply to all matches and leave unrelated rules alone.

[h1]Optional effects[/h1]
All added effects start disabled on fresh installs and upgrades. Preview the mild or Progression preset before applying it to the draft, or choose effects individually. Native quality curves stay in place.

[list]
[*]Separate maximum-hit-point effects for buildings, weapons and apparel.
[*]Worktable speed, research speed and door opening speed.
[*]Generator output, power demand, battery capacity and charging efficiency. Charging efficiency cannot exceed 100%.
[*]Food nutrition and inherent ingestion-poisoning risk. Cooking contamination, saved contamination and rotten-food rules stay native.
[*]Medicine potency for supported native tending and surgery calculations, within the game's limits.
[*]Drug benefit strength and duration for supported ordinary go-juice, wake-up, yayo and smokeleaf highs. Support checks reject changed or custom schemas. Selected benefits include positive mood, pain relief, supported capacity/work/focus bonuses, rest and psyfocus gains. Penalties, addiction, overdose and native redosing stay in place.
[*]Existing equipment bonuses in work/yield, movement/survival, medical, social/trade/animal and psycasting groups. Items do not gain bonuses they lack.
[*]Separate melee, ranged and turret cooldown effects, plus trap damage. Turret firing uses one cooldown route.
[*]A capped bed-rest contribution from ordinary unpowered native dressers and end tables. Only one contribution per kind counts. Default total limits are +2 and -0.5 percentage points. Other facilities and the bed's own native quality behavior stay unchanged.
[/list]

Storage capacity and installed-bionic effects are omitted. The inspected storage integration needs invasive cache updates, and installed bionics need broader per-part installation/removal support. This mod adds no required framework for either.

[h1]Production[/h1]
Material, work-table, skill and inspiration options control generated quality. Colony mechs doing supported work use their fixed mech skill level. Pawns themselves do not receive item quality.

When Resources/Ingredients Affect Quality is enabled, a full fermenting-barrel batch records the stack-weighted quality of accepted wort. Finished beer inherits that quality within its configured bounds. The batch data survives saving, and the barrel's own quality does not affect the result.

[h1]Save, restart and removal[/h1]
Save commits options for the next RimWorld session. Restart RimWorld to activate them. Tab changes do not save. Discard abandons draft edits, and closing with unsaved edits offers Save, Discard or Continue editing.

Legacy selections and saved values migrate, including recoverable entries for unavailable mods. New definitions inherit rules. Removing mod-added quality support can discard those item quality values when you later save. Keep a backup before changing support or removing the mod.

Durability settings preserve absolute hit points and clamp excess when the maximum falls. They do not heal items. Genuine item quality changes preserve condition percentage. Battery capacity changes preserve absolute energy and clamp excess without adding charge.

Existing active supported drug highs retain their saved dose strength/duration after those options are disabled. New doses use the new settings after restart. Uninstalling returns native strength and decay and drops saved surgery medicine corrections. Removal is not lossless.

[h1]Progression and other mods[/h1]
The explicit Progression preset preserves detected prisoner-jumpsuit and Dubs Bad Hygiene exclusions. Supported native factory and Processor Framework output policies keep their selected fixed-quality behavior. The preset does not change research gates, replaced content or load order. Review its preview and keep your pack's required order.

Compatibility options report unavailable adapters and unsupported items. Custom drug consumers, powered/custom furniture and other stat-effect providers need individual assessment. Progression gameplay, startup-cache behavior and multiplayer remain unverified. Quality also separates stacks by tier, so enabling high-volume resources can increase stack counts.

[h1]Credits and license[/h1]
Thanks to Cozarkian, pudy248 and Harmony authors Brrainz and pardeike. This refork inherits the original Everything Has Quality MIT license.
[h1]Links[/h1]
Support me on Ko-fi. This does not imply endorsement by the original authors.

[url=https://ko-fi.com/I7L525WMJ6][img]https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white[/img][/url]
[url=https://github.com/KyleMHB/EverythingHasQualityReforked][img]https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white[/img][/url]
[list]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766]Everything Has Quality Reforked on Steam Workshop[/url]
[*][url=https://github.com/KyleMHB/EverythingHasQualityReforked/issues]Issue tracker[/url]
[*][url=https://github.com/Cozarkian/EverythingHasQuality]Original Everything Has Quality source[/url]
[*][url=https://github.com/pudy248/EverythingHasQualityFork]EverythingHasQualityFork source[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3724294345]Better Pawn Control Forked[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3714050938]Cargo Containers Expanded[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3724312502]Gear Up and Go Reforked[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3724311713]Outfit Stands Plus Forked[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3712317437]Stats Forked[/url]
[/list]
