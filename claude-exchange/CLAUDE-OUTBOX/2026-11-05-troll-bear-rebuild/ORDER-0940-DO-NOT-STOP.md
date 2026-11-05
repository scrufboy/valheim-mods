# ORDER 09:40 — DO NOT STOP. Finish what you start.

STANDING RULE: Never end a turn with "I'll have to X next." Do X. Only stop for a decision that is Brian's, asked as ONE yes/no line. Waiting is not a decision.

Do these in order, without stopping between them:

1. **COLOSSUS 0.1.5 — finish and install now.**
   - Brian's `H:/COLOSSUS/skin.png` IS the palette. No more palette questions.
   - Bind skin.png as albedo and your skin-normal at `_BumpMap` with the correct encoding. Include the 0.1.5 addon (audio distances, idle, hair, bubbling-then-poison corpse).
   - Package, install into LWC Formidable Arena, confirm it loads. One line to Brian.

2. **BEAR + MARAUDER INVISIBLE — top bug.** Logs show they spawn (Marauder 3, Bjorn 1) but Brian sees nothing.
   - Find why: renderers disabled, shader/material not resolved, scale ~0, wrong layer, LOD culled, or spawned underground. Read their own log lines and the spawn positions.
   - Fix. Rebuild. Install. Confirm visible with ONE screenshot from the Tooler call button spawn.

3. **TROLL — reach, look, death.**
   - Reach: attack range/hit radius must match the new body size.
   - Death: fix the "Invalid owned corpse pose packet" warnings (Troll and Bear). Corpse must lie on the ground.
   - Look: say exactly what is wrong in Brian's video, fix it.
   - Rebuild. Install.

4. Report once, at the end: what is installed, one screenshot each. Brian tests with the Tooler call buttons and films.

Faction, ruins, second Marauder, Draugr eyes/smoke: AFTER 1–3.
No new test frameworks. No player relocation. Git state when done.
