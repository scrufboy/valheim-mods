# Encounter loadout adapter handoff for 1.0.x

## Source-inspected

- Living World canon requires one permanent `PersonId`, personal gear ownership,
  explicit faction, site-backed knowledge, world-key/material/station gates, and
  continuity of wounds, relationships, history, and death.
- The maintained core source exposes loaded actors through `NpcKernel.All`, owner
  checks through `NpcBody.IsOwner`, and actor equipment application through the
  internal `NpcBody.Outfit` seam. The companion integration contract records that
  stable encounter/person keys and an aggregate compare-and-swap store do not yet
  exist in that source.
- A temporary aid agreement may move the same person and kit to one verified site,
  but request acceptance, single-claim hostile spawning, native combat, rewards,
  and later recruitment remain outside this policy. An adapter must not manufacture
  a second actor/profile for the travel phase or treat task completion as a bond.
- `EncounterLoadoutPolicy` is engine-free. Its IDs such as
  `gear.runa.road-sword` are abstract policy identities, not asserted Valheim
  prefab names. Resolve each one only after inspecting and live-proving a real
  prefab, compatible rig, attachment, native attack, and ownership behavior.

## CLLC compatibility, primary-source checked

Smoothbrain's current Hexium package page for Creature Level & Loot Control 5.0.7
(updated 2026-10-12) says it runs on client and server, can synchronize server
configuration, adjusts creature levels/size/effects/loot, and advertises a public
API for mod authors. It also warns that the client controlling a spawn determines
level when installations differ. The older official Thunderstore package is
deprecated at 4.6.4, so it is not evidence for the current 1.0 line.

Sources:

- https://valheim.hexium.gg/mods/Smoothbrain/CreatureLevelAndLootControl
- https://thunderstore.io/c/valheim/p/Smoothbrain/CreatureLevelAndLootControl/

Those pages do not document a versioned method contract. Therefore CLLC is an
optional compatibility layer, never a dependency and never the owner of Living
World identity or legal progression. Do not reflect into undocumented members or
replace a prefab/person to change level. If a later source inspection finds a
supported API, an adapter may request presentation/combat scaling only after this
policy has approved the tier; it must disable CLLC loot multiplication for
personal-bound companion equipment or otherwise prove that death cannot duplicate
or inflate it. Absence, version mismatch, or refusal must leave the base profile
functional and must not alter `PersonId` or the saved variant.

## Live-proven: none

No 1.0.x DLL was built, no active profile was edited, and no in-game spawn,
equipment visual, faction interaction, save/reload, death drop, CLLC interop, or
multiplayer ownership behavior was tested by this module.

## Required live port gates

1. Add the stable person/origin/variant/revision keys to the single authoritative
   encounter aggregate before actor creation; reject a second loaded or stored
   claim for the same `PersonId`.
2. Map abstract gear IDs to source-inspected prefabs, then prove both near and
   distant silhouettes without adding an omniscient faction label.
3. Apply equipment only on the authoritative owner and preserve personal-vs-loan
   ownership through upgrade, unload, reconnect, death, and legal recovery.
4. Translate actual world keys, station access, and consumed materials into the
   verified facts used by `Upgrade`; make the whole transaction atomic.
5. Prove that advancing Runa updates the same actor record rather than destroying
   and spawning another. Reconnect must retain wounds, relationship, knowledge,
   inventory, and any terminal death record.
6. With CLLC absent and then present at the exact tested version, prove identical
   identity, no extra personal drops, server/client level agreement, and graceful
   fallback. Record hashes, configuration, logs, and two-player results.
