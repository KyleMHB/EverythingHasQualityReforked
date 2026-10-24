[h1]Choose what gets quality and what quality changes[/h1]
Configurable Quality lets you choose which eligible item types have quality, what that quality does, and whether quality changes their market value. Set broad rules or make individual exceptions. For RimWorld 1.6 and 1.7.

A fresh installation leaves gameplay unchanged. Configure settings or choose a preset, then save and restart to activate changes. Vanilla-quality removal is a separate opt-in choice.

[h2]Choose what has quality[/h2]
Give quality to selected buildings, resources and other eligible items. Keep it off the things you do not want. You can also remove standard quality from eligible vanilla items, including weapons and beds, through the separate Vanilla quality tab. Official expansion items are included; other mods' items and custom quality components stay protected.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-choices.png[/img]

Vanilla removal starts off. Removing quality removes its native bonuses and price scaling too. Back up your save before using it; saved quality values can be lost.

[h2]Choose what quality does[/h2]
Quality assignment and gameplay effects have separate controls. Enable durability without enabling work speed, choose where effects apply, or leave all added effects off. Supported effects cover durability, work and research, doors, power and batteries, food, medicine, supported drug benefits, equipment bonuses, combat cooldowns, trap damage and selected linked furniture.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-effects.png[/img]

An item can use several effects together. The machining table can use both work-speed and power-use effects. The rifle can use both cooldown and durability effects.

[h2]Control quality prices and colony wealth[/h2]
Disable price scaling globally or for selected groups, categories or item types while keeping their quality tiers and enabled gameplay effects. This removes both quality premiums and discounts from their market value, including the value used for building wealth. Material value, condition and other price modifiers still apply.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-wealth.png[/img]

These price controls apply to quality added by this mod and standard quality on eligible vanilla and official expansion items. Other mods' native quality and custom quality components stay protected. This is a market-value control, so it affects trade valuation as well as colony wealth.

[h2]Set broad rules, then make exceptions[/h2]
A global default applies unless a group, category or item rule overrides it. Group defaults cover broad item families. Category rules override those defaults, and individual item exceptions override categories. Assignment, effect participation, price scaling and vanilla removal each have their own rules. Rules select item types, not individual placed objects or material variants.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-rules.png[/img]

[h2]Set the strength for every tier[/h2]
Choose Mild, Medium or Progression to preview effect settings, then adjust the seven quality levels yourself. Each tier has its own scaling factor for the selected effect. A durability factor of 2.5 multiplies base maximum HP by 2.5. A cooldown factor of 0.4 multiplies the cooldown by 0.4. The factor is the multiplier itself, not an extra multiplier on top of a preset.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/quality-strength.png[/img]

Presets enable the standard quality-assignment groups, production settings and effect curves. Existing item and category exceptions stay in place. Presets leave your market-value and vanilla-removal rules unchanged. Effect strength settings do not change the quality tier rolled for an item or its native price multiplier. Production and Market value have their own controls.

Search by item name, filter by source mod, select items across searches and categories, review the selection, apply a rule and undo draft changes. Production settings separately control materials, work tables, skills, inspirations and quality limits.

[h1]See the scaling[/h1]
[i]Examples use the Medium preset for added effects. HP percentages show the scaling relative to Normal quality, before whole-number rounding. Market value assumes quality price scaling is enabled and shows the vanilla multiplier before caps and rounding, at full condition. Actual trade prices also depend on negotiation and other factors.[/i]

The Scaling factor fields show the durability settings in all three charts. Higher factors increase maximum HP. The rifle's cooldown uses its separate reciprocal Medium curve, so cooldown falls as durability scaling rises. Durability and cooldown remain independently configurable.

[h2]Granite walls that take more damage[/h2]
Building durability enabled. The same granite wall, from Awful to Legendary.

The input fields show the scaling factor for the durability effect at each tier. Type your own values to set the scaling. Market-value multipliers are shown for comparison and use the separate price-scaling rules.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/granite-wall.png[/img]

[i]Quality affects durability only when you enable that effect for these walls.[/i]

[h2]Assault rifles with shorter cooldowns[/h2]
Weapon durability and ranged cooldown enabled. Compare recovery between bursts, maximum hit points and market value.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/assault-rifle.png[/img]

[i]Normal cooldown is 1.70 seconds. Aiming time and the interval between shots within a burst are unchanged.[/i]

[h2]Wooden beds, with their native quality benefits[/h2]
Building durability enabled. Vanilla rest effectiveness stays in place alongside the added durability effect.

[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/wooden-bed.png[/img]

[i]This example keeps the bed's vanilla quality and rest benefits. Opting in to vanilla-quality removal removes those quality benefits. No linked furniture is included.[/i]

[h2]Progression modpack support[/h2]
[img]https://raw.githubusercontent.com/KyleMHB/EverythingHasQualityReforked/refs/heads/codex/quality-repair-expansion/docs/images/workshop/progression-support.png[/img]

Choose the Progression preset for Mild effect factors and the pack's deliberate quality exclusions. It respects Prisoner Jumpsuits - No Quality and Dubs Bad Hygiene's QualityComps setting. Supported factory and Processor Framework outputs keep their native quality policies. Research and load order stay under the pack's control.

Use the built-in preset, or install the optional [url=https://github.com/KyleMHB/EverythingHasQualityReforked/tree/codex/quality-repair-expansion/Presets/Progression]Auto Mod Config preset[/url]. The exported preset changes only Configurable Quality settings. It keeps price scaling enabled and vanilla-quality removal disabled. Applying it replaces this mod's configuration snapshot, so export your current settings first.

[url=https://steamcommunity.com/workshop/filedetails/?id=3521297585]The Progression Modpack[/url] by ferny. Progression icon by ferny, from Theming for Progression Modpack.

[h1]How to use[/h1]
[list]
[*]Open Mod settings and choose Configurable Quality.
[*]Use Quality assignment to choose which eligible items receive added quality.
[*]Use Quality effects to enable effects and select their targets. Preview Mild, Medium or Progression, or configure effects individually.
[*]Use Market value to choose where quality changes prices and wealth, including eligible vanilla items.
[*]Use Vanilla quality only if you want to remove standard quality from selected vanilla items.
[*]Save and restart RimWorld to activate your changes.
[/list]

[h1]Settings and configuration[/h1]
Added effects start disabled on new installations and upgrades. Item exceptions override category and group defaults. Batch actions affect selected items, including selections hidden by filters. Review selected lets you check them before applying changes.

Production settings control how materials, work tables, skills and inspirations influence quality. Quality assignment and effect participation have separate rules.

Save before exporting settings through a config-sharing mod. Imported settings apply after restarting RimWorld.

[h1]Requirements and dependencies[/h1]
Requires [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url]. Load Harmony before this mod.

[h1]Compatibility and save safety[/h1]
The renamed package is KyleMHB.ConfigurableQuality. Select Configurable Quality in your mod list instead of enabling both old and new copies. Existing settings are copied on first launch. The Workshop item is unchanged.

Excluding an item from assignment preserves quality supplied by RimWorld or another mod. Vanilla quality is removed only through the separate opt-in removal rules. Support details for individual items show potentially relevant effects and recorded exclusions. Storage capacity and installed-bionic effects are omitted. Multiplayer is unverified.

Quality separates stacks by tier and can increase stack counts. Back up your save before removing quality support or uninstalling. Saved quality values can be lost, and lower durability or battery capacity can clamp excess hit points or charge. See the settings guide for effect limits and removal details.

[h1]Fork history[/h1]
Cozarkian created Everything Has Quality, and pudy248 continued it in EverythingHasQualityFork. This refork continues that work with a revamped settings interface, configurable assignment rules and optional quality effects.

[h1]Credits[/h1]
Thanks to Cozarkian, pudy248 and Harmony authors Brrainz and pardeike. KyleMHB maintains this refork.

[h1]License[/h1]
This refork inherits the original Everything Has Quality MIT license.
[h1]Links[/h1]
Support me on Ko-fi. This does not imply endorsement by the original authors.

[url=https://ko-fi.com/I7L525WMJ6][img]https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white[/img][/url]
[url=https://github.com/KyleMHB/EverythingHasQualityReforked][img]https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white[/img][/url]
[list]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766]Configurable Quality on Steam Workshop[/url]
[*][url=https://github.com/KyleMHB/EverythingHasQualityReforked/blob/main/docs/quality-guide.md]Settings and removal guide[/url]
[*][url=https://github.com/KyleMHB/EverythingHasQualityReforked/issues]Issue tracker[/url]
[*][url=https://github.com/Cozarkian/EverythingHasQuality]Original Everything Has Quality source[/url]
[*][url=https://github.com/pudy248/EverythingHasQualityFork]EverythingHasQualityFork source[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3724294345]Better Pawn Control Forked[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3714050938]Cargo Containers Expanded[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3724312502]Gear Up and Go Reforked[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3724311713]Outfit Stands Plus Forked[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3712317437]Stats Forked[/url]
[/list]
