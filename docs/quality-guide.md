# Quality settings and removal

Quality assignment and optional effects are separate. An assignment rule controls whether this mod adds quality support and manages generation/bounds. It does not strip native or third-party quality. Effect participation uses its own rules and needs an actual quality component.

An item exception wins first. Otherwise, the deepest explicit category branch wins, with exclusion winning equally specific conflicts. The group default applies when no exception matches. Inherit clears a local rule. Unsupported things cannot gain quality through an Include rule.

Quality Rules contains the browser, filtered item results and effect controls. Production contains material/table/skill/inspiration settings. Compatibility explains available adapters and limitations. Bulk actions cover the complete filtered result. Search and navigation never change rules.

Options have an editable draft, a saved next-session configuration and an active configuration. Save validates and persists the draft. Restart RimWorld to activate every behavior change, including changes saved from the main menu. Discard restores the draft. Legacy migration preserves saved scalar values, active individual exceptions and recoverable inactive entries, including definitions from missing mods.

The mild preset uses benefit factors of .95/.98/1/1.02/1.05/1.10/1.20 and cost/cooldown factors of 1.05/1.02/1/.98/.95/.90/.80 for Awful through Legendary. Applying a preset is a deliberate draft edit. The Progression preview adds detected prisoner-jumpsuit and hygiene exceptions without resetting unrelated choices. Supported native factory and processor paths retain their configured output policy rather than having this mod reroll or bound the result. Research gates and load order remain the pack's responsibility.

Added effects do not replace native quality curves. Equipment effects scale selected existing gear offsets only. Turret cooldown uses the quality of the turret body through one cooldown route. Internal turret-gun definitions are excluded from melee and ranged weapon cooldown effects, so the same firing action cannot receive both factors. Drug support is limited to ordinary single-stage GoJuiceHigh, WakeUpHigh, YayoHigh and SmokeleafHigh schemas and curated beneficial channels. Changed/custom outcomes remain native. Linked furniture supports the ordinary unpowered native Dresser and EndTable with one contribution per kind and configurable total caps. Custom or already quality-aware facilities are excluded. The Compatibility page is the authority for the loaded definitions' availability.

Storage capacity is unchanged because the inspected Adaptive Storage Framework needs unsupported cache/state reconstruction for quality changes. Installed-bionic efficiency and durability are deferred because exact per-part health and custom installation/removal paths need a broader adapter. Neither introduces a new dependency.

## Before disabling or removing support

Back up the save and mod settings first. Disabling an effect and restarting removes derived scaling on subsequent loads. Durability changes preserve absolute hit points and clamp excess when the maximum falls; they never repair damage. Genuine quality changes preserve condition percentage. Battery changes preserve absolute energy and clamp excess; they never create charge.

Disabling drug strength/duration affects new doses after restart. Existing supported highs keep their serialized dose snapshots through save/load and expire through native redosing/stacking with their captured duration. Untracked existing highs start at Normal-equivalent behavior. Removing the mod returns those highs to native strength and decay. Saved medicine corrections for an in-progress surgery no longer apply after uninstalling.

Removing assignment support that this mod added can discard quality values the next time the colony is saved, including held and stored items. Native quality remains. Uninstalling also removes all derived effects; reduced hit-point or energy maxima can clamp excess on loading. There is no lossless-uninstall guarantee or automatic colony-wide cleanup. Keep the original backup until the changed setup works for the colony.
