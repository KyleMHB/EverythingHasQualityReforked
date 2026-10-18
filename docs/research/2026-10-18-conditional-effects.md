# Conditional quality effects: implementation decisions

18 October 2026. Source and installed-assembly inspection only; these are feasibility decisions, not reproduced game failures. Both use the fallbacks approved in the implementation plan.

## Storage capacity: omit

Adaptive Storage Framework's installed assembly identifies as 1.2.4. Its native `Extension.maxItemsPerCellByQuality` supplies quality-dependent capacities, but `ThingClass.PostInitialize` caches per-cell capacity and `TotalSlots` during creation/loading. Construction can assign quality after that initialization. There is no supported public quality-change capacity refresh in the inspected implementation.

`CurrentSlotLimit` alone cannot refresh the cached total. Calling `PostInitialize` again would reconstruct collection/render state, and manipulating private capacity caches would require additional notifications and overflow handling. That exceeds the agreed small, clean integration. Leave existing capacities unchanged and explain the omission in Compatibility rather than showing an ineffective switch.

Evidence: installed source and IL at Workshop package 3033901359, especially `ThingClass.PostInitialize`, `DefaultMaxItemsInCell`, `ResetMaxItemsByCell`, `CurrentSlotLimit`, and `ThingCollection.UpdateAllValidStoredItems`. Progression Storage (3292746186) adjusts surviving storage capacities, so any future adapter must use final patched definitions. [ASF upstream](https://github.com/bbradson/Adaptive-Storage-Framework), [Progression Storage upstream](https://github.com/fernyrepos/Progression-Storage).

## Installed bionics: defer

Native `PawnCapacityUtility.CalculatePartEfficiency` offers an instance-aware efficiency seam, but `BodyPartDef.GetMaxHealth(Pawn)` receives no exact body-part record. Differently qualified left/right implants share that definition. Sixteen inspected native callers span damage, health, capacities and UI; changing one result would leave inconsistent maxima elsewhere.

The active Progression health stack also includes custom Bionic Modularity removal workers that bypass vanilla medical removal, plus EBSG usable-module installation through Progression Bionics. Preserving quality through installation/removal would require more than a vanilla surgery hook. Supporting efficiency and durability together would create a separate health adapter subsystem. Defer both without adding a required framework, persistent hediff state or misleading switches. Quality on a removable bionic item does not imply an installed efficiency/durability effect.

Evidence: installed RimWorld 1.7.4981 IL (`BodyPartDef.GetMaxHealth`, `HediffSet.GetPartHealth`, `PawnCapacityUtility.CalculatePartEfficiency`, medical installation/removal), Bionic Modularity package 3589154549, modularity compatibility package 3749402559 and Progression Bionics package 3819707781. Reference signatures were also inspected for 1.6; this does not establish 1.6 runtime behavior. Recent Discord framework complaints are not the deciding evidence.

Detailed local exploration captures remain outside the repository; the decisions above are the durable implementation constraints. Neither decision changes ordinary item-quality assignment or the other approved effect families.
