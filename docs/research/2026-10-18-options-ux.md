# Options UI redesign: focused research

18 October 2026. Research for an interactive review prototype only; no mod implementation changes. Recommendations below apply established patterns to Kyle's ten annotated comments. They are design judgments, not evidence that the redesigned interface has already passed user review.

## Findings and decisions

**Show choices instead of requiring users to remember interactions.** NNGroup recommends recognition over recall and visible, understandable options. Use visible filter labels and choice menus rather than buttons whose next click silently cycles to another mode. Prefer “Use category setting” for items and “Use parent setting” for category branches. “Vanilla” is inaccurate because the resolved rule can be a user-defined parent rule. Show the effective result and source, for example “Excluded by Buildings.” [Recognition and recall](https://www.nngroup.com/articles/recognition-and-recall/), [usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/).

**Separate selecting items from changing their rules.** Carbon documents row checkboxes and a contextual batch-action bar as a way to apply one operation to several selected items. WAI's mixed checkbox represents an aggregate of checked/unchecked children; it does not mean “inherit.” Use checkboxes solely for selection, a mixed header checkbox for partial selection, and separate status text/rule controls. [Carbon data tables](https://www.carbondesignsystem.com/building-blocks/core/components/data-table/guidelines), [WAI checkbox pattern](https://www.w3.org/WAI/ARIA/apg/patterns/checkbox/).

**Preserve a selection across searches.** This is a task-specific decision derived from Kyle's example of fifteen unrelated items, not a universal requirement established by the sources. Store selected item IDs independently of filtered results. Show “15 selected · 12 hidden by filters,” with “Review selected” and “Clear selection.” Batch actions affect exactly those IDs. A labelled “Select all N results” affects the current result set; it must not silently select the entire catalogue. Selection alone never modifies quality rules.

**Hide advanced detail while keeping essential decisions visible.** NNGroup's progressive-disclosure guidance recommends revealing advanced/less frequent choices when needed. Carbon similarly supports expandable rows. Show an effect's switch, purpose and strength summary first; expand its seven quality-tier multipliers on request. Show compatibility status and actionable limitations before patch diagnostics. Never hide a warning required to understand a chosen action. [Progressive disclosure](https://www.nngroup.com/articles/progressive-disclosure/), [Carbon expandable rows](https://www.carbondesignsystem.com/building-blocks/core/components/data-table/guidelines).

**Group related controls and label each input.** WAI recommends visual and programmatic grouping of related fields and explicit labels for form controls. Put each quality group's minimum and maximum on one row under clear column labels; give each input a unique accessible name including the group. Search is an additional aid, not a substitute for sections. [Grouping controls](https://www.w3.org/WAI/tutorials/forms/grouping/), [labelling controls](https://www.w3.org/WAI/tutorials/forms/labels/).

**Make changes understandable and reversible.** Summarize preset changes against the draft, including switches enabled, custom curves replaced and exclusions added. Offer expandable details before applying. Keep Discard and an unsaved-change guard; use explicit “Save for restart” wording. Identify invalid numbers and minimum/maximum conflicts beside their fields. WAI recommends review/correction, meaningful validation and undo for reversible actions. [Validation guidance](https://www.w3.org/WAI/tutorials/forms/validation/).

**Use text in addition to color.** “Enabled,” “Excluded,” “Not installed” and “Limited” must remain readable without green/red interpretation. Native HTML controls, visible keyboard focus, labelled checkboxes and keyboard-accessible disclosure buttons are appropriate for the browser prototype. The eventual Unity UI needs equivalent affordances; browser semantics alone do not establish game accessibility. [WAI use of color](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html), [WAI labels](https://www.w3.org/WAI/tutorials/forms/labels/).

## Mapping to the ten review comments

| Comment | Prototype response |
| --- | --- |
| 1: redundant entry screen | Open the actual editor directly; keep restart status in its header/footer. |
| 2: “Inherit” terminology | Use category/parent setting, with its resolved source visible. |
| 3: support text wall | Relevant effect/status rows; expand reasons and diagnostics. |
| 4: unexplained filters | Search items, Mod, Show status, and Only customized items; visible menus. |
| 5: fifteen unrelated items | Persistent checkbox selection, selected/hidden counts, review selection and explicit selected-item actions. |
| 6: duplicate policy browsers | One item/category browser with separate labelled “Quality” and “Added effects” controls. Retain independent rules underneath. |
| 7: preset numbers wall | Compact change summary, grouped effect changes, expandable exact values. |
| 8: production list | Search plus sections for ingredients/worktables, skills/inspirations and quality limits. |
| 9: min/max separation | One row per group with Minimum and Maximum side by side. |
| 10: compatibility dump | Integration/status/reason rows, useful summary, technical details collapsed. |

## Constraints from the current implementation

The current options window is 1120×820 logical units. `DrawRules` combines unlabelled search, a cycling filter and actions over all filtered results. `DrawItems` tracks one detail selection rather than a multi-selection. `DrawDetails` generates an unstructured support message. `DrawEffects` always displays seven numeric fields for every effect; preset previews are newline-separated curve dumps. `DrawProduction` reflects fields directly into a linear list. `DrawCompatibility` combines limitations, provider status, patch reports, effect reasons and legacy records in one scroll view. [Current window source](../../Source/Window_QualityOptions.cs).

Keep the existing item → deepest category → group precedence and independent assignment/effect-participation rules. Including an effect target does not add quality or enable an effect; excluding assignment must not promise removal of vanilla/mod-owned quality. Preserve the restart lifecycle rather than making a prototype imply live game updates. [Accepted rule/lifecycle decision](../adr/0004-inherited-rules-and-restart-activation.md).

The prototype should feel like a modest RimWorld settings window: dark panels, straightforward buttons, restrained accent colors and comfortable row heights. Carbon informs interaction behavior; importing an enterprise component library, adding dashboards, or copying its visual brand is unnecessary. Use illustrative data with an explicit prototype label. Compatibility examples must not be presented as a fresh scan of the live Progression pack.

## Short review tasks

1. Exclude Buildings, then include Animal bed as an exception. Identify why the item is included.
2. Select unrelated items through several searches; review the complete selection and exclude it once.
3. Leave item quality enabled but disable added effects for one item.
4. Enable weapon durability, customize Legendary, then inspect a preset before applying it.
5. Find a production setting by search and edit a group's minimum/maximum together.
6. Find an unavailable integration without reading diagnostics, then discard the draft.

These tasks are the practical review scope; no large testing programme is implied.

## Review refinement: separate assignment and effects

Kyle found the combined quality/effects batch target confusing and requested separate tabs. This supersedes the combined-browser recommendation above: **Quality assignment** now contains only quality rules; **Quality effects** contains effect switches plus a separate **Items these effects apply to** view. The batch target dropdown and dual-policy columns are removed. Each policy keeps its own selections and filters, and actions in one cannot change the other. This refinement is implemented only in the throwaway prototype pending review.

## RimWorld feasibility and task review (prototype revision 3)

The review was redirected toward task usability rather than browser typography. The existing [options window](../../Source/Window_QualityOptions.cs) already uses `Widgets.TextField`, `Widgets.CheckboxLabeled`, `Widgets.BeginScrollView`, `Widgets.ButtonText`, `FloatMenu`, tooltips and ordinary `Window` dialogs. The proposed workflows can be drawn with those primitives plus in-memory state. Persistent multi-selection, independent tab filters and selection review are custom mod behavior, not built-in RimWorld features. Collapsible effect rows likewise require a Boolean per expanded row. No web component library or new required settings framework is implied.

The current custom window requests 1120×820 logical units; that is a working layout budget, not a hard engine limit. Actual screen size and UI scale still need native adaptation. The browser is an interaction model, not a pixel-accurate RimWorld layout. The plain stock settings callback can host custom-drawn controls; whether the complete editor fits there without an extra window remains an implementation/layout decision. The existing wrapper screen is not evidence that an extra user click is required by RimWorld. [Settings entry implementation](../../Source/Mod_QEverything.cs).

Revision 3 concentrates on these task improvements:

- Quality assignment and effects remain separate, with their own selections and filters. No batch target chooser.
- Explicit **Select all N results** and a selected/hidden count make bulk scope visible.
- **Review selected → Back to previous results** restores the category and filters instead of abandoning the search context.
- Category defaults report custom item and child-category rules that can override them.
- The source-mod filter uses a searchable list. A real implementation can use a small custom Window containing a TextField and scroll view; a huge unsearchable FloatMenu is unsuitable for this collection.
- Effect strengths remain optional detail, open rows survive changes, and presets are reviewed by setting family instead of individual serialized scalar values.
- Minimum/maximum are together; impossible choices are disabled. Invalid numbers report their problem beside the field and never replace the valid draft value.
- No-op rule changes do not create false unsaved changes.

The remaining design question is user task success: can Kyle exclude a branch with an exception, select unrelated items, block effects without changing quality, find a production limit, and understand a preset before saving? No layout is labelled “best” before that review. Full native fit and feel still require a later RimWorld pass after prototype approval.

## Task-driven refinements (prototype revision 4)

The interaction-logic walkthrough exposed confusing behavior, separate from visual placement:

- Searching an item name also matched category ancestry, so “bed” returned dressers and stools. The prototype now searches item names only, with category and mod filtering handled explicitly.
- A category-scoped search could hide an unrelated item with no direct recovery. Search labels now identify the category, and **Search all categories** broadens the search without losing selections or search text.
- Unticking the last item in selection review removed the return control. Emptying the selection now restores the previous results automatically.
- **Clear all filters** now also clears category scope, while preserving selected IDs.
- Category defaults expose a **Show custom items** action, making item exceptions discoverable without knowing the advanced filter.
- Save-review values use the same action language as the controls; repeated bulk choices report that items already use that setting instead of suggesting another change occurred.

These remain prototype-only interaction decisions, compatible with the existing TextField, ButtonText, checkbox, scroll-view and custom Window approach. Browser visual inspection is still unavailable for the local file; no rendered-browser usability result or native-game validation is claimed.
