# Claude handoff: friendly/enemy encounter and specialist-combat wave

Date: 2026-10-28. Owner: Brian (`scrufboy`). This is a new submission under
`claude-exchange/PROTOCOL.md`; the earlier
`2026-10-28-companion-encounter-world` submission remains intact. Brian's newest
words are in `REQUEST.md`. Review this wave on draft PR
`https://github.com/scrufboy/valheim-mods/pull/4`, based on
`work/norse-human-names`.

## What was built

- SOURCE-INSPECTED: `LivingWorldCORE/COMPANION-ENCOUNTER-RESOLUTION-ARC.md` now
  defines mutual uncertain first contact; separate friendly, Pillager, and
  Eitrherjar allegiances; temporary accepted aid to a verified site; an optional
  single-claim authored fight; witnessed trust; safe return; and a later separate
  full-party invitation. Same-person gear advancement and specialist native
  combat ownership are explicit.
- OFFLINE-TESTED: `Discovery/` selects one- or two-person hunting, camp, or
  patrol entry scenes without distance-labeling allegiance, and binds local
  knowledge to stable site facts.
- OFFLINE-TESTED: `EncounterAllegiance/` models authored faction relationships,
  cautious mutual notice, reveal, warnings, aid, and committed hostility.
  Sighting alone never grants a target or proves a person friendly.
- OFFLINE-TESTED: `CampLifecycle/` exposes ordinary camp, hunt, patrol, and
  cautious-notice facts while preserving one watcher, station checks, combat
  preemption, and no offscreen death.
- OFFLINE-TESTED: `QuestBroker/` owns accepted request, exact-site arrival,
  activation handoff, completion, privacy, and single reward claim.
  `EncounterActivation/` separately owns a single persisted event/spawn claim
  and observed clear. It never hijacks the vanilla raid clock.
- OFFLINE-TESTED: `NeedsAndAffinity/` records explicit aid agreement,
  contribution, trust, and later invitation eligibility. `RescueRelease/` alone
  owns temporary field assignment/return, active-slot accounting, and final
  bond/party membership. No aid result auto-recruits.
- OFFLINE-TESTED: `EncounterLoadouts/` records equipped, history-aware Runa and
  Edda variants and legal tier upgrades on one `PersonId`, with finite kit and
  no identity replacement.
- OFFLINE-TESTED: `SpecialistCombat/PredictionAndThrows/` authorizes bounded
  finite spear/bomb requests with target motion, arc, ally, and blast safety;
  `SpecialistCombat/MeleeRoles/` checks precommit knife lunge and atgeir/heavy
  arcs. Native `MonsterAI` remains the only owner of real targeting, movement,
  attack animation, projectile, damage, debit, and recovery.
- OFFLINE-TESTED: `RuntimeIntegration/` composes the separate module ledgers
  through stable person/request/site identity. The full offline arc exercises
  accepted aid → temporary travel → one event claim → observed clear → trust →
  safe return → later distinct invitation.
- SOURCE-INSPECTED: `EncounterLore/` adds varied cautious first contacts,
  warning and combat interruptions, help/travel lines, and later invitation.
  `EncounterVoice/` has candidate bindings for those lines; new conversational
  clips remain unapproved and silent in normal mode.
- SOURCE-INSPECTED: `research/CompanionRespectQuests/` adds current Creature
  Level & Loot Control and faction-collision research. CLLC compatibility is a
  possibility, not a verified dependency. Same-person upgrade is the Living
  World invariant regardless of CLLC.

## Maintained 1.0.x source seam

- SOURCE-INSPECTED: Live-proven installed source baseline is commit
  `9933763978fd7c29bfa9909e7ea8b6c47f86e421`, version 1.0.32.7. The
  newer local review source `33addad57523d5d95df7eb2572403e4f76d1ea36`
  declares 1.0.32.8 and is not claimed installed/live-proven here.
- OFFLINE-TESTED: `LiveCoreAdapter/CoreEncounterAdapter.cs` is a first
  compile-in core slice: server-only actor creation, strict allow-listed prefab
  scan, persistent five-ID ZDO tags, process duplicate claim, and observation-
  only native-combat handoff. It requires the campaign aggregate to be saved
  before creation; it does not invent one. The adapter is not installed.
- OFFLINE-TESTED: Nine pinned 1.0.32.7 source signature checks pass.
  Compilation of the existing read-only 1.0.32.8 source together with the
  adapter passes. The native ownership gate passes with 61 approved write
  sites. The resulting DLL is a local verification artifact, not a release
  candidate and is not committed to GitHub.
- FAILED: The maintained combat-voice and movement lock gates reject the
  combined 1.0.32.8 assembly. The same legacy locks also reject a clean build
  of the approved 1.0.32.7 source, so this is compiler/metadata-layout
  dependence, not evidence that the adapter changed those behaviors. This is a
  release blocker until the maintained gates are repaired and reviewed. Never
  bypass or blindly regenerate the locks.
- OFFLINE-TESTED: `LiveCoreAdapter/GateCompatibility/` supplies a semantic IL
  comparison prototype. It finds equivalent locked methods across the approved
  reference, core-only 1.0.32.8, and core-plus-adapter 1.0.32.8 builds, and
  rejects deliberate voice-priority and movement-predicate mutations. Its
  README specifies the maintained-gate integration and new trusted lock review;
  the proposal has not yet been merged into those gates.
- UNKNOWN: No 1.0.32.7 combined core DLL, installed encounter DLL, game session,
  dedicated-server result, 3D equipment visual, native spear/knife/bomb/atgeir
  attack, zone return, raid activation, or full recruitment journey has been
  live-proven from this wave.

## Exact next work for Claude

1. Review the prior submission and this PR; identify the authoritative source
   commit and preserve the live 1.0.32.7 baseline/rollback hash.
2. Integrate the five-ID server-owned encounter record and atomic world save
   before enabling actor creation. Keep one durable request ledger
   (`QuestBroker`), one event execution ledger (`EncounterActivation`), one
   person/assignment ledger (`RescueRelease`), and one relationship record
   (`EncounterAllegiance`).
3. Wire one Meadows Split-Ash encounter end to end: safe spawn; Runa and Edda
   gear/knowledge; uncertain sight and warning; camp/hunt/patrol; a deliberately
   accepted nearby request; same-person temporary travel; a single loaded-site
   threat; native combat; witnessed result; safe return; later party offer.
4. Fix/review the gate comparison before installation. Compile on the maintained
   branch and run the core native, ownership, voice, movement, and regression
   checks without muting existing approved combat voices.
5. Treat spear throw, bomb, knife lunge, and atgeir as gated capability trials.
   `LiveCoreAdapter/CAPABILITY-MATRIX-1.0.32.7.md` names the source-level
   candidate paths and what remains unknown. Prove exact item/animation event,
   grip, projectile/contact, ammo/debit, ally safety, owner replication, and
   recovery in game before enabling each move.
6. Record installed DLL hash, startup/log/telemetry, one- and two-player tests,
   save/reload, zone unload, third-party creature combat, and the release/return
   result. Do not report a combat loss as a code failure without evidence of a
   violated behavior contract.

The GitHub source and offline evidence are ready for review. A playable claim
requires the missing maintained-runtime integration and live proof.
