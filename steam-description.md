[h1]Choose what gets quality and what quality changes[/h1]
Configurable Quality lets you choose which eligible items have quality, what it does, and whether it changes their price and colony wealth. For RimWorld 1.6 and 1.7.

A fresh installation leaves gameplay unchanged. Configure settings or choose a preset, then save and restart.

[h2]Choose what has quality[/h2]
Add quality to selected items or leave it off. You can also remove standard quality from eligible vanilla weapons, beds and other items through the Vanilla quality tab. Official expansions are included; other mods' items and custom quality components stay protected.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-choices.png[/img]

Vanilla removal starts off. It removes native quality bonuses and price scaling too. Back up first; saved quality values can be lost.

[h2]Choose what quality does[/h2]
Assignment and effects have separate controls. Enable durability without work speed, combine effects, and choose their targets. Effects cover durability, work, research, doors, power, batteries, food, medicine, supported drug benefits, equipment bonuses, combat cooldowns, trap damage and selected linked furniture.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-effects.png[/img]

[h2]Control quality prices and colony wealth[/h2]
Disable price scaling globally or for selected groups, categories or item types. Quality tiers and enabled effects stay. This removes quality premiums and discounts from market value, including building wealth. Material value, condition and other price modifiers still apply.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-wealth.png[/img]

Price rules cover added quality and standard quality on eligible vanilla and expansion items. Other mods' native quality and custom components stay protected. These rules affect trade valuation as well as wealth.

[h2]Set broad rules, then make exceptions[/h2]
Item exceptions override categories, categories override groups, and groups override the global default. Assignment, effects, prices and vanilla removal have separate rules. Rules select item types, not placed objects or material variants.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-rules.png[/img]

Search by name, filter by source mod, select items across searches, review selections, apply rules and undo draft changes.

[h2]Set the strength for every tier[/h2]
Preview Mild, Medium or Progression, then edit each tier's factor. A durability factor of 2.5 means 2.5 times base HP; a cooldown factor of 0.4 means 40% of base cooldown. Each factor is the multiplier itself.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-strength.png[/img]

Presets enable standard assignment groups, production settings and effects. Item/category exceptions stay. Market-value and vanilla-removal rules stay unchanged. Production separately controls materials, worktables, skills, inspirations and quality limits.

[h1]See the scaling[/h1]
[i]Charts use Medium added effects. HP percentages are relative to Normal, before whole-number rounding. Market values show native quality factors at full condition, before gain caps and rounding. Actual trade prices also depend on negotiation.[/i]

White inputs show durability factors. Cooldown has its own reciprocal Medium factors; durability and cooldown remain independently configurable.

[h2]Granite walls that take more damage[/h2]
Building durability enabled, from Awful to Legendary.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/granite-wall.png[/img]

[h2]Assault rifles with shorter cooldowns[/h2]
Weapon durability and ranged cooldown enabled.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/assault-rifle.png[/img]

[i]Normal cooldown is 1.70 seconds. Aiming time and intervals between shots within a burst stay unchanged.[/i]

[h2]Wooden beds, with native quality benefits[/h2]
Building durability enabled alongside vanilla rest effectiveness. No linked furniture is included.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/wooden-bed.png[/img]

[i]Removing vanilla quality also removes its native rest benefits.[/i]

[h2]Progression modpack support[/h2]
[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/progression-support.png[/img]

The Progression preset uses Mild factors and respects Prisoner Jumpsuits - No Quality and Dubs Bad Hygiene's QualityComps setting. Supported factory and Processor Framework outputs keep their native quality policies. Research and load order stay under the pack's control.

Choose the built-in preset, or install the optional [url=https://github.com/KyleMHB/EverythingHasQualityReforked/tree/codex/quality-repair-expansion/Presets/Progression]Auto Mod Config preset[/url]. The export changes only this mod's settings, keeping price scaling enabled and vanilla removal disabled. Applying it replaces this mod's configuration snapshot; export your settings first.

[url=https://steamcommunity.com/workshop/filedetails/?id=3521297585]The Progression Modpack[/url] and its icon are by ferny. Icon sourced from Theming for Progression Modpack.

[h1]How to use[/h1]
[list]
[*]Open Mod settings and choose Configurable Quality.
[*]Choose Quality assignment, Quality effects, Production, Market value or Vanilla quality.
[*]Configure rules or preview a preset, then Save and restart RimWorld.
[/list]

Save before exporting through a config-sharing mod. Imported settings apply after restart. Batch actions include selected items hidden by filters; Review selected shows the full selection.

[h1]Requirements and save safety[/h1]
Requires [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url], loaded first.

The renamed package is KyleMHB.ConfigurableQuality. Select the renamed mod instead of enabling both copies. Existing settings are copied on first launch. The Workshop item is unchanged.

Excluding assignment preserves native quality; only the separate removal rules remove it. Storage-capacity and installed-bionic effects are omitted. Multiplayer is unverified.

Quality separates stacks by tier and can increase stack counts. Back up saves before removing quality support or uninstalling. Saved tiers can be lost; reducing durability or battery capacity can clamp excess HP or charge.

[h1]Credits and license[/h1]
Cozarkian created Everything Has Quality; pudy248 continued it in EverythingHasQualityFork. KyleMHB maintains this refork. Thanks to Harmony authors Brrainz and pardeike. This mod inherits the original MIT license.

[h1]Links[/h1]
[list]
[*][url=https://ko-fi.com/I7L525WMJ6]Support on Ko-fi[/url]
[*][url=https://github.com/KyleMHB/EverythingHasQualityReforked]GitHub repository and issue tracker[/url]
[*][url=https://github.com/KyleMHB/EverythingHasQualityReforked/blob/codex/quality-repair-expansion/docs/quality-guide.md]Settings and removal guide[/url]
[*][url=https://github.com/Cozarkian/EverythingHasQuality]Original Everything Has Quality[/url]
[*][url=https://github.com/pudy248/EverythingHasQualityFork]pudy248's fork[/url]
[/list]
