# ORDER 12:15 — Colossus eye beams (Brian: "needs to be done")

Brian: "focused beam Dvergr circlet beams for eyes... the scariest thing on the colossus. it lights up what it looks at and makes it more formidable and intimidating... fixed eyes that turn and look and illuminate and blind you"

Do (after the open Marauder/Troll/Bear fixes, or in parallel by an agent):
1. At runtime read the vanilla `HelmetDverger` circlet's spot Light settings (color, range, angle, intensity, cookie). Create two narrow spotlights from the Colossus eye positions, children of the HEAD bone, pointing out of the eyes. Same on the corpse? No — beams go out on death (fade over ~1 s).
2. Eye glow: bright emissive eyes matching the beam color (the pending eye-only emission mask).
3. Look: beams follow the head; add a clamped aim (~35°) at the eye pivot toward the current target so the beams lock on and sweep.
4. Blind: when the local player is inside the cone and facing the Colossus, show a bright flare/halo at its eyes and a brief screen glare. Config toggle + strength.
5. Performance: shadows only on the nearest Colossus to the player; others no shadows. Config.
6. Rebuild Colossus, install after Brian closes the game, one screenshot at night of the beams on the player. Report once.
