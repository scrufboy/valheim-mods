# Colossus owner review / handoff

Source pushed: scrufboy/lwc-valheim-workbench work/valheim-workbench, commit 810d2d0. Full review: docs/project/COLOSSUS-OWNER-REVIEW-1326-2026-11-05.md.

BUILT: Colossus 0.1.11 DarkHair01, ZIP C:/Users/scruf/Documents/LWC Model Skin Studio/Exports/Undead-Colossus-0.1.11-DarkHair01/LivingWorldCORE-Undead_Colossus_Prototype-0.1.11.zip. Existing LWC dark matted hair selected per Brian's preference; added exact hair/beard/tone/corpse selection logging. Rig, head placement, AI, death timing and eye beams unchanged. NOT INSTALLED: r2modman running; guarded installer refused transaction. Arena remains enabled 0.1.10. No gameplay launched or desktop controlled.

VIDEO: sampled latest owner footage shows green chin hair and cyan eyes/illumination; upper lip not conclusive. All supported Beard2..10 meshes contain moustache geometry. Do not claim chin-only style or a fixed beard fit. Exact selected style was not logged in 0.1.10.

CORE/LIFECYCLE FOLLOW-UP: latest log has 511 NRE blocks in ZNetScene.RemoveObjects/CreateDestroyObjects/Update beginning 13:26:01 with respawn. Diagnostic finds four destroyed registered ZNetViews: hash 805484646 twice (native sfx_sword_swing) and -1257621178 twice (unresolved), persisting across 21 scans. No emitter/destruction caller established. No evidence to blame nearby RRR NPC destruction or hair/eyes specifically. Recommend bounded trace of AddInstance/Destroy/OnDestroy for these two hashes only, recording creation parent and caller. No blanket null suppression or speculative fix.

TELEMETRY: 510 Colossus-related rows in 5253-record session, approach/contact around 74m and subsequent melee exchanges including ~98 damage to companion. Owner judges visuals and gameplay.

NEXT: install built 0.1.11 only when manager closed; Brian tests. Correlate upper-lip issue with selected style before alignment edits. User explicitly prohibits autonomous game launches, spawns or desktop control. Do not interrupt his recording/testing.
