# Headless Showcase Director — private administration and QA policy

## Status and purpose

The Headless Showcase Director is an internal Living World administration and testing bridge. It is not a public gameplay convenience, progression feature, raid reward system, or cheat utility. It exists so an authorized operator can reproduce controlled encounter conditions without keeping the Tooler window open, including during OBS capture.

The source/package default is **disabled**. Enabling it is an affirmative administrative decision, and all accepted and refused actions are written to audit telemetry. This feature is not currently a public release candidate.

## Allowed uses

- Private or local worlds owned by the operator.
- Authorized server administration where the operator has permission to create and clean up test encounters.
- Quality assurance, regression testing, prefab-registration checks and encounter validation.
- Controlled balance, approach-distance, wave-timing and cleanup verification.
- Cinematics, trailers, OBS demonstrations and reveal events that are clearly presented as staged or administrative demonstrations.
- Recovery and cleanup of Tooler-owned showcase test instances created during the same audited run.

## Prohibited uses

- Gaining an advantage in public multiplayer or on a server without explicit authorization.
- Advancing normal progression, farming loot/resources, power-leveling or bypassing encounter rules.
- Creating enemies, allies or rewards to alter a genuine playthrough's economy or difficulty.
- Concealing administrative intervention or presenting staged spawns as organic gameplay.
- Using the bridge for impersonating another administrator, or using another person's Stream Deck/hotkey configuration without permission.
- Disabling telemetry, removing provenance tags, substituting unregistered prefabs, rewriting factions, or retaining Tooler-owned test actors as permanent world residents.
- Resurrecting, replacing, recalling or mutating canonical companions or their persistent save records.

## Technical boundaries

- `Showcase Director enabled` defaults to `false` in every general source/package build.
- Stream Deck triggers are inert until the bridge is explicitly enabled.
- The director requires a loaded local player and `ZNet.IsServer()` authority. Unprivileged remote clients are refused and audited.
- Every cohort requires the exact loaded plugin and registered prefab. There is no vanilla fallback.
- Valheim continues to own one native `RandEventSystem` event. Showcase cohorts are separately tagged Tooler test instances, not fake concurrent native raids.
- The director never calls a faction rewrite. Team intent is descriptive; the registered prefab remains authoritative.
- Every owned actor carries run/cohort/wave provenance. Stop/cleanup destroys only actors bearing Tooler showcase provenance.
- Companion reinforcement requires the dedicated `LWC_ShowcaseCompanion_Test` identity. Ordinary persistent companion identities are not accepted.
- No progression, inventory, drops, world keys or canonical companion saves are intentionally modified by the director. A live release gate must still verify these boundaries.

## Audit expectations

Keep the complete JSONL record under `BepInEx/config/LivingWorld/Tooler/ShowcaseTelemetry`. An auditable demonstration includes the Stream Deck/UI action, UTC time, native event, run ID, cohort ID, requested and actual prefab, spawn position/distance, wave, arrival, death, reinforcement/recall, cleanup and any refusal reason.

Do not edit telemetry to make a staged encounter appear organic. For server work, retain the relevant server log, Tooler telemetry and video timestamps together. Stop the run and investigate if an actor lacks provenance, a prefab mismatch occurs, authority is refused, cleanup is incomplete, or canonical state changes.

## Why this is not a public release candidate

The director can materially alter an encounter and therefore needs an explicit trust boundary, live multiplayer authority verification, cleanup verification across disconnect/restart, drop/progression audits, and clear disclosure in captured footage. Several reveal creatures are also still candidates rather than production-valid registrations. Until those gates are live-proven, the bridge remains private, disabled by default, and unsuitable for Thunderstore, Nexus, Hexium or another public mod channel.

## Brian's private lab profile

Brian may set `Showcase Director enabled = true` in the private `LWC Formidable Arena` profile for the authorized testing described above. That local exception does not change the safe default distributed in source or packages.
