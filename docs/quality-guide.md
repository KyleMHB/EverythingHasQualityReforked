# Quality settings and removal

Quality assignment and optional effects are separate. An assignment rule controls whether this mod adds quality support and manages generation/bounds. It does not strip native or third-party quality. Vanilla removal has its own opt-in rules. Effect participation uses its own rules and needs an actual quality component.

An item exception wins first. Otherwise, the deepest explicit category branch wins, with exclusion winning equally specific conflicts. The group override applies next, then the global default. Use global default clears a group override. Use category setting clears an item exception; Use parent setting clears a category rule. Unsupported things cannot gain quality through a Give quality rule.

Quality assignment controls which items this mod gives quality. Quality effects contains effect switches and strength, with a separate browser for effect targets. Production contains searchable material/table/skill/inspiration settings, with minimum and maximum quality on the same row. Compatibility contains expandable integration details and diagnostics.

Tick items to build a selection across searches and categories. Review selected shows them together; Back to results restores your previous filters. Give quality, Don't add quality and Use category setting apply only to selected items, including selected items hidden by the current filters. Select all results adds the complete filtered result to your selection. Search, selection and navigation never change rules. The source-mod picker filters items by the mod that defines them; it does not enable or disable mods. Undo change reverses a draft edit, including a batch action.

Options have an editable draft, a saved next-session configuration and an active configuration. Save validates and persists the draft. Restart RimWorld to activate every behavior change, including changes saved from the main menu. Discard restores the draft. Legacy migration preserves saved scalar values, active individual exceptions and recoverable inactive entries, including definitions from missing mods.

Choose a preset from the dropdown in Quality effects to preview its changes. Mild uses benefit factors of .95/.98/1/1.02/1.05/1.10/1.20 and cost/cooldown factors of 1.05/1.02/1/.98/.95/.90/.80 for Awful through Legendary. Medium uses benefit factors of .6/.8/1/1.375/1.75/2.125/2.5, with reciprocal factors for costs, risks and cooldowns (about 1.667 at Awful, 1 at Normal and .4 at Legendary). Normal remains neutral; existing effect limits still apply. Expand All seven quality levels in the preview to inspect every factor.

Applying a preset is a deliberate draft edit. The Progression preset uses mild factors and adds detected prisoner-jumpsuit and hygiene exceptions without resetting unrelated choices. All three presets enable standard assignment groups and production settings while preserving item/category exceptions, market-value rules and vanilla-removal rules. Supported native factory and processor paths retain their configured output policy rather than having this mod reroll or bound the result. Research gates and load order remain the pack's responsibility.

Added effects do not replace native quality curves. Equipment effects scale selected existing gear offsets only. Turret cooldown uses the quality of the turret body through one cooldown route. Internal turret-gun definitions are excluded from melee and ranged weapon cooldown effects, so the same firing action cannot receive both factors. Drug support is limited to ordinary single-stage GoJuiceHigh, WakeUpHigh, YayoHigh and SmokeleafHigh schemas and curated beneficial channels. Changed/custom outcomes remain native. Linked furniture supports the ordinary unpowered native Dresser and EndTable with one contribution per kind and configurable total caps. Custom or already quality-aware facilities are excluded. Support details for individual items show recorded exclusions for loaded definitions.

Storage capacity is unchanged because the inspected Adaptive Storage Framework needs unsupported cache/state reconstruction for quality changes. Installed-bionic efficiency and durability are deferred because exact per-part health and custom installation/removal paths need a broader adapter. Neither introduces a new dependency.

A fresh installation leaves gameplay unchanged. Quality assignment, effects, production changes and vanilla removal start disabled. Existing quality prices stay intact. Configure settings or apply a preset, then save and restart. Existing saved configurations are preserved.

## Market value and colony wealth

Market value has its own rules, separate from quality assignment and gameplay-effect participation. A global default applies unless a group, category or item override replaces it. The deepest category rule wins, and an individual item exception overrides categories. New and existing configurations keep quality price scaling enabled until you change these rules. Mild, Medium and Progression do not change them.

Disable price scaling globally or for a group, category or item to remove both quality premiums and discounts. These rules cover quality added by this mod and standard quality on eligible vanilla and official expansion items. Their quality tiers and enabled gameplay effects remain intact. Other mods' native quality and custom quality components keep their existing price behavior. Material value, condition and other price modifiers still apply. These are market-value rules, so they affect trade valuation as well as any colony-wealth contribution based on that value. They are not a separate raid-only wealth adjustment.

Save and restart RimWorld to activate changes. See [settings sharing](settings-sharing.md) for exporting configuration or distributing it with a mod pack.

## Removing vanilla quality

The Vanilla quality tab has a separate global default with group, category and item overrides for removal. All removal defaults start off on fresh installations and upgrades. Choose Remove vanilla quality to opt in, or Keep vanilla quality to override a broader removal rule. Item exceptions override categories, and the deepest category rule wins. Removal overrides added-quality assignment for the selected definition.

Only eligible item definitions from Core and official expansions with standard quality components can be removed. Definitions from other mods and custom quality subclasses remain protected. Don't add quality in the assignment tab still preserves native quality; it is not the removal control.

Back up your save, save the settings and restart. Removal applies when definitions initialize, before items and saves load. It removes quality tiers and the native effects and price modifiers that depend on them. Other components, such as art, are retained. Saving the colony without quality can discard existing tier values. Turning removal off and restarting restores quality support, but cannot recover tiers already lost from a saved colony. The mod does not rewrite placed items' component lists during play.

## Before disabling or removing support

Back up the save and mod settings first. Disabling an effect and restarting removes derived scaling on subsequent loads. Durability changes preserve absolute hit points and clamp excess when the maximum falls; they never repair damage. Genuine quality changes preserve condition percentage. Battery changes preserve absolute energy and clamp excess; they never create charge.

Disabling drug strength/duration affects new doses after restart. Existing supported highs keep their serialized dose snapshots through save/load and expire through native redosing/stacking with their captured duration. Untracked existing highs start at Normal-equivalent behavior. Removing the mod returns those highs to native strength and decay. Saved medicine corrections for an in-progress surgery no longer apply after uninstalling.

Removing assignment support that this mod added can discard quality values the next time the colony is saved, including held and stored items. Assignment exclusions keep native quality; separate vanilla-removal rules are the opt-in exception. Uninstalling also removes all derived effects; reduced hit-point or energy maxima can clamp excess on loading. There is no lossless-uninstall guarantee or automatic colony-wide cleanup. Keep the original backup until the changed setup works for the colony.
