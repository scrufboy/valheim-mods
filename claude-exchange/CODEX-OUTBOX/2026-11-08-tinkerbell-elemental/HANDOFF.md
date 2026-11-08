# Tinkerbell elemental - shared implementation handoff, November 8

Brian explicitly requested this handoff for Claude and the fire/audio work.

## Latest playtest feedback

Brian's direct feedback, relayed here from the Real Smoke and Fire chat: “What I witnessed was really cool behavior. I gotta see more of it. I want you to just let it know that I just saw amazing stuff in the last play. Oh my goodness.” Preserve the current effects/behavior candidate as the comparison baseline. This positive observation does not imply that every scenario, loot proposal or multiplayer feature is accepted.

## Current source and installed candidate

Canonical direction: https://github.com/scrufboy/lwc-valheim-workbench/blob/main/docs/project/TINKERBELL-ELEMENTAL-CONTRACT-2026-11-08.md

Corrected source checkpoint: work/valheim-workbench commit df384f0fb5f51430ebd03d30470e4d1eade79808, directory exchange/surtling-elemental/2026-11-07/recovery06. Read DELIVERY20.md and ELEMENTAL20-STATUS.md. Local source commit39f22dc. Ordinary pushes fail DNS; connector source checkpoints preserve remote files with non-forced leased updates. Local and remote API-created histories require later reconciliation; do not reset either side or claim all repositories fully synchronized.

Version0.1.10 Recovery20 Package02 is installed/enabled in **LWC Surtling Flight Test**. Toolkit3.0.3 and Server Devcommands1.116 remain enabled. QA-only poison injector is disabled after testing, absent from creature ZIP. Valheim exited after testing.

Actual controlled native damage-path poison testing completed three cycles:
Descend -> actual contact Land ->3s vulnerable Stunned/steam -> arms-out SpecialAttack1 Reignite -> Active, phases1/2/0.120HP reduced to106.5853 without healing. Actual Searing Shards, Cinder Fan and primary fireball releases logged.129 telemetry samples; capsule-foot terrain clearance0.080-0.089m at Land; zero Error/Exception lines. This was injected HitData, not an actual thrown bomb or multiplayer test. Package01's touchdown probe caused upward avoidance; Package02 caps that probe at landing support and completed all cycles without teleport/physics bypass.

Private ZIP: scrufboy-Formidable_Surtling_Female-0.1.10-Recovery20-Package02-PRIVATE.zip
SHA256 ebe48813c5073caaffc78246040a305fb1428f0d830577b68fc07642e632738e
Persistent download: https://chatgpt.com/library?file_id=libfile_07d89c7f0440819181c94c39fba5ca71&account_id=personal

## Shared behavior contract

Ground-oriented hovering fire elemental with its core as heart. Native MonsterAI sensing/hostility and actual ground combat opportunities remain. Low terrain-following buffer; independent facing/travel with forward/back/side/diagonal lean and synchronized turns. Brief tactical elevation only2-3 human heights, approximately4-6m. No scripted circles, timed climb/dive loops, high Drake flight or Deathsquito dive-bomb role. Reusable bounded environment probes; no trained machine-learning claim.

Poison resistance explicitly changes Immune -> Normal on this replacement. Frost remains weak. Native Wet/damage/duration persists, including ongoing damage after leaving water until native Wet expires. Reignition does not cure Wet. Quench cancels stale native attacks and uses an18s cooldown, no zero-HP revival. Acquired NatureManufacture body flame/steam/trailing smoke/near-ground scorch and existing Inferno/Distortion attacks/death effects are bound.

## Ownership and next work

Surtling Branch Recovery owns creature traversal, controller, combat, quenching, body/trail/recovery/death effects and its bounded fire lease. Real Smoke and Fire owns world-fire adapters. Audio work supplies auditioned clip roles/mix notes, not a second creature writer. Need elemental cast/burst, smolder/crackle, steam/hiss and recovery laugh candidates. Current female voice remains provisional.

Read the fire chat's local handoff exchange/real-smoke-fire/2026-11-08/SURTLING-OWNER-DIRECTION.md for the newer finisher/loot/recovery-boost proposals. They are pending reconciliation: the tested build still uses native drops, allows ordinary lethal damage and has no recovery healing. Do not claim the proposed kill-window/rare loot system is implemented. Acquired Elemental summon, rigid ash corpse and variant skins remain pending.

Arena integration is authorized but requires baseline backup, exact roster and coordinated telemetry. Prepared manual event names: lwc_elemental_observe, lwc_elemental_poison, lwc_elemental_frost, lwc_elemental_roster. The prepared CFG is source, not proof of installation or raid execution. No multiplayer or final owner appearance acceptance claimed.

Direct app thread enumeration failed again. This Git handoff is verified shared delivery; no direct message delivery to other chats is claimed. Preserve19, avoid competing source writers/Unity builders, and retain this Package02 baseline.
