# Rescue-to-release policy module

This directory is an engine-free C# policy boundary for persistent companion
rescue, invitation, party release, death, and equipment recovery. It targets the
older compiler/API surface already used by the maintained Living World 1.0.x
runtime and has no Unity, Valheim, BepInEx, profile, or filesystem dependency.

## Implemented here

- stable person records, separate rescue/life/assignment state, two expedition
  slots, and six living bonded people; a full expedition accepts a valid new bond
  as a resident when a bonded seat and established safe home remain;
- deterministic yes/no invitation with stored machine-readable reasons;
- permanent rejection of debug identities and identities without a persistent
  world encounter;
- release request versus commit: unsafe/no-destination requests remain
  `AwaitingSafeHandoff`; commit requires an established home, origin camp, or
  reclaimed village plus proof that its zone is loaded and the same actor is
  physically present; ordinary party release always retains the living bond,
  including at an origin camp;
- companion-owned gear retention, explicit removal of player loans, and
  single-claim corpse gear recovery;
- terminal death records and idempotent command IDs/revisions suitable for reload,
  duplicate callbacks, zone unload, and server restart;
- temporary request aid as an explicit non-bonded assignment: it preserves the
  same person and gear, consumes one of the two active field positions, records
  authored request/return/portal terms, waits through combat/portal/unsafe return,
  and ends only at a proven safe handoff without automatic recruitment;
- optimistic persistence and Unity/world adapter interfaces.

Run `tests/Run-Tests.ps1`. The harness compiles to the temporary directory and
does not touch a game profile.

## Required 1.0.x integration (not implemented here)

The maintained runtime must adapt its existing stable NPC/save keys into
`WorldState`, persist the full snapshot (including applied command IDs and death
records) with compare-and-swap revision semantics, and refuse older snapshots
that would discard a committed death. It must map Eikthyr/Elder world keys to
zero/one/two unlocked slots and invoke `Invite` only for authored persistent
encounters.

Unity-side release flow must supply combat/incapacitation/sailing/portal/terrain
safety, present valid destination choice, load/pin the destination zone, place or
escort the existing network identity to a safe anchor, complete visible loan
exchange, and only then call `CommitRelease`. A waiting actor remains party-owned
for slot counting and must never be destroyed, remotely recalled, or sent toward
an unloaded position.

Temporary-aid integration translates only committed NeedsAndAffinity/QuestBroker
facts (stable request/person/encounter IDs, accepted status, and the person's
explicit agreement). It does not infer agreement from affinity or request
completion. RuntimeIntegration must retain the temporary assignment on zone
unload, restore the same `PersonId`, enforce its authored portal permission, and
count temporary and return-pending allies against the same two active positions.
Request completion may begin a safe return but never calls `Invite`; full party
membership remains a later, separate factual yes/no transaction.

Death integration must remain feature-gated until traversal and network ownership
are live-proven. The server must coalesce death callbacks, commit once with place,
time, and cause, disable every spawn/recall/revive path for that person ID, leave
gear on a real corpse/tombstone, and call recovery only after an actual inventory
transfer. Memorial UI, dialogue text, settlement jobs, five-day lead cooldown,
and world encounter spawning remain responsibilities of their owning systems.
