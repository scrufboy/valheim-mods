# Human NPC 0.3.45 guard scan coverage and native wet presentation

This candidate replaces purposeless timed guard wandering with a resolved coverage loop and corrects the missing native wet particle presentation on the non-Player human root.

## Guard coverage

- A settled guard selects a safe reachable sector because that sector is unscanned or is the stalest completed coverage.
- Arrival begins a bounded left/center/right field-of-view sweep.
- Only the completed sweep resolves the sector; the opposing sector becomes the next priority.
- Party sector leases keep companions from selecting the same guard sector and prefer opposing coverage.
- Meaningful leader displacement clears coverage from the old area.
- Direct routes through the leader's exclusion radius are rejected.
- Hostile combat, movement, doors, crouching, swimming, and other higher owners still interrupt the optional guard action.

## Wet presentation

- Native Wet status and its stamina modifiers remain authoritative.
- Valheim's Wet asset is player-effect-only, so the actor's cloned runtime status enables and retriggers its native networked `vfx_Wet` effect.
- No global status-effect asset is changed and no cosmetic wet flag substitutes for the real status.

The installed 0.3.44 DLL is not changed while Valheim is running. This candidate requires the complete offline build and a later closed-game installation.

