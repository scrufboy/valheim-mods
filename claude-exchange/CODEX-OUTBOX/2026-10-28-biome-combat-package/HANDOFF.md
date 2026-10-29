# Claude handoff: biome cast and coordinated combat kit

Date: 2026-10-28. Draft PR: https://github.com/scrufboy/valheim-mods/pull/4

- SOURCE-INSPECTED: Prior modules had two authored Split-Ash loadouts and separate spear/bomb and knife/atgeir safety policies. They lacked one seeded biome cast and a combined primary/throw/fallback contract.
- CODE-TESTED: `EncounterPopulation/` adds 99 abstract role packages for Meadows through Ashlands across Hearthless, Pillager, and Eitrherjar. The server selects one to four people only after a durable site claim, varies roles, and replays a committed plan without changing person or faction. Deep North has no invented kit.
- CODE-TESTED: Each package has a regional visual, biome threat score, primary weapon, different melee fallback, and optional specialist moves. Spear and finite bombs can coexist on one person; spent bombs leave spear use, and an airborne/lost spear routes to fallback. Recovery requires a known owned projectile plus a safe path.
- CODE-TESTED: Bomb, spear throw, spear recovery, knife lunge, and atgeir sweep have separate live-proof gates. Proving one move never enables another. The combat router only emits requests under server/actor owner, native combat ownership, and authorized target facts.
- CODE-TESTED: `ServerPresentationProjection` produces a narrow `ClientMemberView`; unrevealed faction is withheld. Clients render the view and approved cues, not the server plan or a spawn/attack decision.
- SOURCE-INSPECTED: The canonical arc now defines this cast and equipment package. The exchange protocol now calls tests run outside Valheim `CODE-TESTED`, avoiding the competing mod's name in new project wording.
- UNKNOWN: The catalog's armor and weapon labels are abstract. They are not verified Valheim item IDs or proof of actual equipped visuals, attacks, or dedicated-server residency.

## Exact builder sequence

1. Persist one site claim, then the selected cast/kit and stable `PersonId` values in a single server-owned world transaction. Reject a duplicate or corrupted replay.
2. Bind every member to the naming module, same-person history and verified local knowledge, and `EncounterLoadouts` personal gear. Map each abstract kit to actual registered native items and body grip/attachment data. The spawned local may own gear natural to their home biome without granting the player premature upgrade rights.
3. Materialize the approved actor on the server; send only the display view to clients. Preserve uncertainty: a similar silhouette does not reveal Hearthless versus Pillager.
4. Feed authored faction and witnessed relationship into `EncounterAllegiance`. No attack from sight alone. Temporary aid and later party membership remain separate offers with explicit acceptance; an enemy spawn never auto-flips faction or joins.
5. Route bomb and spear safety through `SpecialistCombat/PredictionAndThrows`; route knife/atgeir through `SpecialistCombat/MeleeRoles`. Native `MonsterAI` retains targeting, movement, item equip, animation, projectile, damage, resource debit, and recovery.
6. Prove spear held/stowed/airborne/recovered/lost states, finite bomb debit, fallback switch, all move-specific animation events, ally safety, owner replication, unload/reload, and two-player observation before enabling each move.
7. Apply the selected server-resident camp work from the prior correction. Record logs, telemetry, hashes, and zero-/one-/two-player dedicated-server results.

Do not present these policy checks as in-game proof. The maintained 1.0.x adapter, release gates, and runtime tests remain the promotion boundary.
