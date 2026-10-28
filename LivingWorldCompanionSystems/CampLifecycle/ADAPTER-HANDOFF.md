# Camp lifecycle runtime handoff

Status: pure deterministic policy and tests implemented; maintained 1.0.x Unity adapter not yet wired.

## Persisted records

Use one world-authoritative camp record and one record per stable `person_id`. A person record owns life, presence, current/origin/destination camp IDs, last safe anchor, routine epoch, role/identity references, and immutable death facts. Encounter retirement must never delete the person record. Camp membership is an index, not identity.

The runtime must acquire a global `livingworld.person.<person_id>` spawn claim before instantiation. Camps at both ends of a migration may read the same person, but only the current authoritative record may hold that claim. Never derive identity from prefab, class, name, camp, or scene instance.

## 1.0.x adapter boundary

1. Read ZDO/world records into `CampState`, `CampPersonState`, and `Observation`; call the pure policy only on the network owner.
2. Apply a decision with a compare-and-swap on record revision. Persist the complete person transition before spawning/despawning. Retry from a fresh read on conflict.
3. For every loaded camp, collect its living actor IDs and station facts, call `AssignCampRoutines`, and pass each result into its `Observation`. This reserves exactly one watcher, staggers other work, and excludes cook/rest/repair when the required fire/shelter/work surface is absent. Map `RunRoutine` to a cancellable peaceful lease. The lease may control only while the existing exclusive owner is at the camp/resident tier.
4. On readiness, combat, combat pursuit/regroup, incapacity, or death, cancel animation/navigation/hand claims synchronously. Do not resume until the higher owner explicitly releases; then call the policy again.
5. On zone unload or player absence, save the last safe anchor and remove the scene actor. Do not advance health, hazards, combat, inventory, or mortality offscreen.
6. Commit death only from the authoritative loaded actor's confirmed native death transaction. Make it idempotent by `person_id`; write memorial facts and release roster/party assignments in the same server transaction. Never turn a missing actor into a casualty.
7. Migration begins by persisting `Migrating + destination`. The origin remains authoritative until a loaded destination confirms arrival. Commit the new camp and clear destination atomically; only then transfer the spawn claim. A failed or interrupted journey remains retryable from the last safe anchor.
8. Return/release uses the same stable record. Rejoining changes assignment only; it never creates a new person or rerolls appearance, history, equipment, relationship, or wounds.

## Needs and Affinity fact seam

`ObserveRoutineNeed` emits a small, stable `CampFactEvent` from concrete runtime evidence while an ordinary routine owns the actor. It does not create a quest, persist a need ledger, choose dialogue, attribute aid, or award affinity. Feed `NeedObserved` to the Needs/Affinity adapter as an observation keyed by `FactId`. That module may independently decide whether the fact is worth presenting or tracking.

After the external module/runtime has verified actual aid, feed the same evidence key back with `AidConfirmed`. The seam emits the same `FactId` and `ResumeOrdinaryLife=true`; the next normal lifecycle observation continues the coordinated routine. Repeated evidence has a stable ID for external deduplication. Combat, alert, regroup, death, unload, and away presence emit no ordinary need facts.

Runtime evidence examples are deliberately concrete and routine-scoped: blocked watch visibility, low cooking supplies, damaged rest shelter, low repair materials, or an obstructed patrol route. Station availability remains an earlier assignment gate: an actor is never assigned cook/rest/repair merely to manufacture a need when the corresponding station does not exist.

## Adapter acceptance still required

- Dedicated-server and owner-transfer tests for one spawn per `person_id`.
- Save/reload and zone unload/reload through actual ZDO serialization.
- Combat preemption of every routine, including native hands, path, facing, and animation cleanup.
- Live station discovery and loss (fire destroyed, shelter invalidated, work surface removed) followed by deterministic reassignment with one watcher retained.
- Interrupted migration at each persistence boundary, destination contention, and server restart.
- One authoritative casualty callback, memorial persistence, roster slot release, corpse/equipment transaction, and proof that recall/reload cannot revive.
- Release/return through a lawful camp or home with identity and inventory unchanged.

Permanent campaign mortality must remain disabled until the existing traversal, lava/water, ownership, and attribution proof gates are satisfied.
