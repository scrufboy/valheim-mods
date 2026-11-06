# Prediction and throws

Engine-free policy for a single bounded spear/javelin or bomb/area native special request. It consumes recent sensed motion and authoritative world-query facts, predicts an aim/impact point, reserves finite ammunition, and makes request/release replay idempotent.

It does not acquire or retarget enemies, move or face an actor, choose/equip a weapon, start an attack, play an animation, create a projectile, debit native inventory, apply an effect/damage, or resolve a hit. Those remain exclusively owned by native `MonsterAI` and its attack/item/projectile path. `AvailableAmmo` is an authoritative adapter snapshot/reservation ledger; after `ConfirmNativeRelease`, the adapter must reconcile it with the native inventory debit and fail closed on disagreement.

Encounter acceptance, temporary aid travel, first contact, and party invitation do not authorize a throw. `TargetAuthorized` must come from the current authored faction/relationship and committed-combat boundary. `PersonId`, `EncounterId`, and `GearRevision` bind the request to the same persistent person and current personal kit; the adapter must reject a stale revision rather than replace the person with a stronger actor. A single-claim authored hostile event is upstream evidence only: its server claim must already be committed before its actors can become ordinary authorized native-combat targets.

## Live 1.0.x checks still required

Before calling `Reserve`, an adapter compiled against inspected 1.0.x source must prove, without writing private AI state:

- server authority, `NpcBody.IsOwner`, stable actor/person/encounter/target/request identity, current personal-gear revision, current Combat action ownership, authored target authorization, and encounter bounds;
- the exact equipped native item and attack definition support the requested spear/javelin or bomb, the native AI is in a legal attack-ready state, and no ordinary native attack is already committed;
- exact animation trigger/state, release event, interruption behavior, recovery, held/back item lifecycle, and that one release event produces one projectile;
- projectile prefab speed, gravity, origin, collision mask, terrain/building arc query, native fuse/arming timing, blast radius, area/friendly-fire rules, and damage ownership;
- native inventory debit timing and whether thrown spears return or are consumed; reconcile the debit once and never manufacture a refill or phantom projectile;
- current authoritative positions/velocities for allies and protected neutrals, reachable impact ground and escape route, and a final safety recheck immediately before native commitment;
- owner-only replication of the committed aim/request so remote peers observe the same animation, projectile, impact, and debit without independently predicting or rolling aim error.

The pure tests are not live Valheim acceptance. Dedicated-server tests must cover one release/debit under replay, cancellation before the animation event, interruption, reconnect/reload of reservations, target direction change after release (a legitimate miss), low terrain/building collision, ally crossing during wind-up, blast escape, and bounds expiry.

Run `Run-Tests.ps1`.
