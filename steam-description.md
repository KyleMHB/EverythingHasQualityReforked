[h1]Description[/h1]
[i]A fork of a fork that brings quality to far more things in RimWorld, with configurable categories, individual item control, search, and better settings performance for large mod packs.[/i]

Everything Has Quality Reforked expands RimWorld's quality system so more items can have quality while letting you decide where quality applies.

[h1]Features[/h1]
[list]
[*][b]Expanded quality support[/b] for buildings, resources, ingredients, meals, drugs, medicine, apparel, weapons, shells, and other manufactured items.
[*][b]Category-wide controls[/b] and individual item overrides.
[*][b]Searchable, cached settings lists[/b] for large mod packs with many definitions.
[*][b]Configurable quality bounds[/b] for supported categories.
[*][b]Material, work-table, skill, and inspiration options[/b] for production quality logic.
[*][b]Passive fermentation quality[/b] so beer inherits the weighted quality of the wort placed in its barrel.
[*][b]RimWorld 1.6 packaging and stability work[/b].
[/list]

[h1]How to Use[/h1]
Enable the mod, then open RimWorld's mod settings and select [b]Everything Has Quality[/b]. Configure quality by category, adjust quality ranges, enable individual definition customization, search long item lists, and apply category changes.

When [b]Resources/Ingredients Affect Quality[/b] is enabled, fermented beer inherits the weighted-average quality of the wort accepted by the barrel. The barrel's own quality does not affect the result. Fermentation quality is saved with an active batch, and finished beer respects its configured minimum and maximum quality.

This mod enables quality on more things, but it does [b]not[/b] make every quality-bearing item automatically scale every stat. A separate compatible mod is required for broader quality-based stat effects.

[h1]Settings and Configuration[/h1]
Use RimWorld's mod options under [b]Everything Has Quality[/b] to configure:
[list]
[*]Supported quality categories and individual definition inclusion lists.
[*]Minimum and maximum quality bounds.
[*]Whether materials, work tables, and skills affect generated quality.
[*]Supply-quality multipliers for awful through legendary inputs.
[*]Custom inspirations for butchering, chemistry, construction, cooking, gathering, harvesting, mining, and stonecutting.
[/list]

[h1]Requirements and Dependencies[/h1]
[list]
[*]RimWorld 1.6.
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url].
[/list]

[h1]Compatibility, Load Order, Multiplayer, and Save Safety[/h1]
[list]
[*][b]Load order:[/b] Load Harmony before this mod.
[*][b]Large mod packs:[/b] Searchable and cached settings lists reduce repeated rebuilding and sorting of long definition lists.
[*][b]Performance note:[/b] Quality adds per-item state, and differently rated items cannot share a stack. Enabling quality for high-volume resources can increase stack counts.
[*][b]Known conflicts:[/b] No specific hard conflicts are documented in this repository.
[/list]

[h1]Frequently Asked Questions[/h1]
[b]How does resource quality affect crafted items?[/b]

Quality-bearing ingredients are averaged by stack count and used as a supply-quality input when the relevant option is enabled. Passive fermentation transfers weighted-average wort quality directly because no pawn performs a crafting roll.

[b]How do turret qualities work?[/b]

Turret body quality and turret weapon quality are separate. A high-quality turret body does not automatically create a matching-quality gun.

[b]Are mechanoids supported?[/b]

Colony mechs performing supported work use their fixed mech skill level when generating crafted-item quality. Mechanoid pawns themselves do not receive item quality.

[b]Can organs or body parts have quality?[/b]

Standard organs and body parts are not a supported quality category because installed parts become hediffs rather than inventory items.

[h1]Fork History[/h1]
Everything Has Quality was created by Cozarkian and later forked by pudy248. This refork keeps the original idea working on RimWorld 1.6 while improving settings usability and stability for modern modded games.

Compared with the upstream versions, this refork adds searchable cached settings lists, individual definition controls, configurable quality bounds, passive fermentation quality, current packaging, and stability improvements for larger mod lists.

[h1]Credits[/h1]
[list]
[*]Original Everything Has Quality by Cozarkian.
[*]Intermediate EverythingHasQualityFork by pudy248.
[*]Current refork maintained by KyleMHB.
[*]Harmony by Brrainz and pardeike.
[/list]

[h1]License and Forking Policy[/h1]
This refork inherits the original Everything Has Quality MIT license.

If your fork primarily consists of bug fixes or feature additions that align with the core vision of this mod, I reserve the right to request that your changes be submitted as a Pull Request to my existing codebase rather than being published as a completely separate standalone release.

This is a project request, not an additional restriction on the MIT license.

[h1]Links[/h1]
Support me on Ko-fi. This does not imply endorsement by the original authors.

[url=https://ko-fi.com/I7L525WMJ6][img]https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white[/img][/url]
[url=https://github.com/KyleMHB/EverythingHasQualityReforked][img]https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white[/img][/url]
[list]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3710884766]Everything Has Quality Reforked on Steam Workshop[/url]
[*][url=https://github.com/KyleMHB/EverythingHasQualityReforked/issues]Issue tracker[/url]
[*][url=https://github.com/Cozarkian/EverythingHasQuality]Original Everything Has Quality source[/url]
[*][url=https://github.com/pudy248/EverythingHasQualityFork]EverythingHasQualityFork source[/url]
[/list]
