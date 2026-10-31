# Tooler 2.6.1 restores the current FormidableAI raid launcher

## Intent

- **SOURCE-INSPECTED:** Brian correctly reminded both chats that the installed Tooler is the project-owned raid launcher. Requiring him to remember console event identifiers bypassed an existing project responsibility.
- **SOURCE-INSPECTED:** Tooler 2.6.0 had a Raids page, but it still exposed the superseded `lwc_souls_pillagers` / `lwc_krauguk_legion` trials and did not expose the installed `lwc_formidable_arena` / `lwc_formidable_souls_swamp` events.

## Exact correction

- Branch `work/tooler-current-raids-2.6.1`, head `394e874`.
- Tooler Raids page now starts the two current 70–95 m Elder-pattern training events.
- The standard `Noon` and `STOP TRAINING RAID` controls sit with those events.
- The two dead/superseded trial buttons were removed; the package no longer depends on their retired config.
- The exact current `custom_raids.supplemental.lwc_formidable_souls.cfg` is now a Tooler package input and is checked for both event names, all six Souls prefabs, `Random=false`, `HuntPlayer=false`, and the complete 70–95 m ring.
- Every vanilla raid button and every non-raid Tooler page remains.
- This creates no defended-player state, faction layer, combat behavior, companion status, or new spawn law.

## Installed

- **INSTALLED:** Tooler 2.6.1 in `LWC Formidable Arena`.
- Extension SHA-256: `F9578F40BEDB70E6A58A669A234EB521BD32E7B62119C8AF24B4A33ACED310F0`.
- Core SHA-256: `F601C2CB4D4C798F53E0525AF4B4834ED0C56E4C6726AFFCCDF5D0977F39165F`.
- Raid config SHA-256: `B8907AA1E420FAB7540216C56A655C5BD2854D250AACDB891295E680079A61C8`.
- Previous 2.6.0 DLLs are recoverable under `BepInEx/LivingWorldBackups/Tooler/2026-10-31-before-2.6.1-current-formidable-raids/`.

## Remaining unknown

- **UNKNOWN:** runtime UI/command launch is not yet live-proven. Brian's next entry should open Tooler with Delete, choose Raids, choose Noon, and start Formidable Arena. The game log must show Tooler 2.6.1 and `Random event set:lwc_formidable_arena`.
- The earlier native `ZNetScene.RemoveObjects` burst remains a separate investigation; it was not caused or repaired by this UI correction.
