PoP Village Workers - Test 1

Exit Valheim. Extract into the Souls profile BepInEx folder. Replace all.
Load your reclaimed village. No new world is needed.

This adapts Horem/sonofmusk FriendliesAI to PoP commoners HumanM01, HumanF01,
and HumanM02. Guards, elves, dwarves, Souls enemies and leftover Friendlies NPCs
are excluded. Requires the repaired RRRCore and existing PoP definitions.
No Friendlies companion assets or spawn configurations are included.

FIRST TEST: place a wooden chest, a workbench, and a damaged wooden wall near
a commoner. Place cooked food in the chest or drop it near the villager. Leave
the commoner in Stay mode so following does not take precedence over jobs.
Watch for a repair attempt and an AI status in its hover text.

WORKSTATION TEST: get a commoner to follow you, bring it within 5m of a smelter,
and demonstrate adding fuel/ore repeatedly until its training messages confirm
the skill is learned. Put coal and suitable ore in a nearby wooden chest, with
cooked food available. Set the commoner to Stay near that workstation. Observe
whether it fetches resources and fills the smelter. Provide LogOutput.log and
what you observed. The inherited AI uses training; it does not automatically
know every workstation.

Code changes: civilian recognition, explicit allowed workstation configuration,
five obsolete Piece.m_allPieces reflection lookups changed to s_allPieces,
removed dependency on unrelated Friendlies companion plugin, disabled its
redundant equipment rewiring (RRRCore supplies NPC equipment), and added delayed
registration for NPCs that become tamed after Character.Awake.

Included: patched FriendliesAI.dll, patched MobAILib.dll, unchanged Stateless.dll,
and WorkerBridge.dll. No reclaim DLL, NPC JSON, or spawn rules are replaced.
Existing worker search/food settings remain in som.horem.FriendliesAI.cfg.

Managed reference checks and package checks passed. Gameplay repair, resource
use, training, pathfinding, and save/reload behavior require the first runtime
test; this is not a finished village simulation. Repairs restore existing pieces
and do not rebuild destroyed pieces. Three daily meals, crop routines, leader,
extra guards, and saved building blueprints are later steps.

Original AI author credit: sonofmusk/Horem; MobAILib: RagnarsRokare.
Private project adaptation. Preserve upstream licenses and distribution terms.
