# Tooler raid launcher doesn't match the Arena profile (Brian: the Tooler is the raid launcher; check it first)

Brian's reminder: the Tooler (`LivingWorldCORETooler`, Delete key → Raids page) exists to launch these raids. Neither
chat used it. Both of us told Brian to use console commands.

## Findings (Arena profile, 2026-10-31)

- **Installed Tooler is stale:** `plugins/LivingWorldCORETooler/LivingWorld.CORE.Tooler.dll` (mtime ~10-23). Its string
  table has `lwc_souls_pillagers`, the vanilla `army_*` buttons and `stopevent`, but **not** `lwc_krauguk_legion`, which
  repo source `2dfb4a2`/`e5d0aae` (`src/LivingWorldTrainerPanel.cs` Raids page) includes.
- **Souls buttons point at events not defined in this profile:** the Tooler starts `lwc_souls_pillagers` and
  `lwc_krauguk_legion`, defined in the repo's `config/custom_raids.supplemental.lwc_formidable_trials.cfg`, which is **not
  installed** in the Arena config.
- **The profile's raids have no buttons:** Arena config `custom_raids.supplemental.lwc_formidable_souls.cfg` defines
  `lwc_formidable_arena` (Pillager, WoodsSoul, NinoBosque, OrcBrute) and `lwc_formidable_souls_swamp` (Pillager,
  Neckman, Servant, OrcBrute). Neither appears in the Tooler.
- Vanilla raid buttons should work as they are.

## Required before the next raid session (one change set)

1. Build the Tooler from current repo source and install it in `LWC Formidable Arena`.
2. Make every Souls button name an event defined in the installed Custom Raids configs: add `lwc_formidable_arena` and
   `lwc_formidable_souls_swamp`; for the two trial buttons, either install the trials cfg or remove them.
3. Launch gate: each Tooler raid button starts its event once (log shows the event start and the Souls prefab spawns).
   This comes before any play session.
4. Trace the unattributed `ZNetScene.RemoveObjects` NRE burst from the 1.0.32.29 session
   (`2026-10-31-live-1.0.32.29`) before a raid.

Brian decides who takes it. Claude can build and install the Tooler if asked.
