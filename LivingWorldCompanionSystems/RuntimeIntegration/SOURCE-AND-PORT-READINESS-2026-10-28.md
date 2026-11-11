# Source and port readiness — 2026-10-28

Status: engine-free integration proof only. No profile was changed and no playable encounter build was produced.

## Authoritative source candidates inspected

- Live-proven installed line: full source commit `9933763978fd7c29bfa9909e7ea8b6c47f86e421`. Its `CorePlugin.Version` is `1.0.32.7`. Preserved runtime evidence reports installed DLL SHA-256 `89B5BEAF24550E79978CF8C7A6E69EC2A2B54BBEE6ACDADC01FD45EE408A1E6F` and a successful 2026-10-28 load. This is the current live-proven baseline.
- Newer maintained review source: checkout HEAD `33addad57523d5d95df7eb2572403e4f76d1ea36`, project-record name `1.0.32.8b-party-spacing-test`. Source declares `CorePlugin.Version = "1.0.32.8"`. It has offline spacing tests but is not established here as installed/live-proven.
- The checkout's local branch label `claude/voice-1.0.32.1` is stale metadata: the checked-out commit is `33addad...`. Neither that label nor an old 1.0.1 tree is treated as current proof.

## Engine-free integrations now compiled

- `EncounterAllegianceBridge` validates arc person/camp identity and delegates witnessed facts to the module-owned `PersonContactRecord`. `AttackCommitted` returns a request to yield to native combat; it does not write native AI state.
- `NeedsAidBridge` delegates offers and completion to the module-owned `NeedsState`, validating shared encounter/person/request identity. Concrete nearby aid becomes Needs-owned remembered trust. Recruitment remains RescueRelease's separate yes/no decision.
- `SpecialistCombatBridge` validates the arc actor identity and delegates a bounded reservation to `PredictionAndThrowsPolicy`. Ammo changes only after `ConfirmNativeRelease`; prediction never replaces native projectile physics.
- QuestBroker remains the only durable world-request ledger. RuntimeIntegration does not copy request status or revision.
- `EncounterActivationBridge` joins one accepted QuestBroker `WorldRequest` to one module-owned `ActivationEntry`, requiring exact issuer, party, world, site, and site-identity agreement. EncounterActivation persists its claim before emitting `RequestSpawn` and owns only event execution/observed clear.
- `TemporaryAidBridge` consumes Needs' accepted personal aid evidence, the accepted broker request, and its Activation entry, then delegates field assignment exclusively to RescueRelease. RescueRelease owns `TemporaryAlly`, `AwaitingTemporaryReturn`, portal permission, and safe return to `ReleasedKnown` without promotion.
- `EncounterLoadoutBridge` validates person/origin encounter/origin camp identity and delegates creation/upgrades to EncounterLoadouts. Upgrades increment the same `PersonLoadoutState` lineage rather than replacing the person.
- Specialist combat now compiles against both prediction/throws and melee-role capability policies. Identity is checked at the arc boundary; the policies still only request verified native actions.

## Reconciliation with current owner direction

The updated `COMPANION-ENCOUNTER-RESOLUTION-ARC.md` fixes the required sequence and ownership boundaries. RuntimeIntegration follows it as below:

| Owner direction | Durable owner / integration rule |
| --- | --- |
| Uncertain friendly/enemy first contact | EncounterAllegiance owns witnessed contact, authored allegiance, readiness, identity reveal, and relationship. Visibility alone never authorizes attack or proves friendship. |
| Accepted aid travel to a verified site | NeedsAndAffinity owns personal agreement/evidence; QuestBroker owns accepted request/arrival; RescueRelease owns temporary assignment and safe return. RuntimeIntegration validates their IDs and delegates—none is copied into the arc aggregate. |
| Optional single-claim authored hostile event | QuestBroker owns request/outcome claim. EncounterActivation owns exact-site execution, persists `encounter-claim:<RequestId>` and `encounter-spawn:<RequestId>` before its adapter request, and reports one observed clear back to QuestBroker. It never uses the vanilla raid clock. |
| Later separate party invitation | RescueRelease remains the only invitation/bond/assignment authority. Aid, travel, combat, and event completion supply evidence only and cannot promote the person. |
| Same-person gear progression | EncounterLoadouts owns authored profile, variant, personal gear, knowledge, and revision; RescueRelease preserves personal/loan ownership through assignment. Both join on `PersonId`; replacing the person is forbidden. |
| Specialist combat under native ownership | SpecialistCombat may reserve a bounded action only after authoritative ownership, combat ownership, native readiness, target authorization, and safety facts. Native release confirms ammunition use; policy prediction never owns projectile physics. |

The intended orchestration order is therefore: witnessed contact → optional accepted request → verified temporary travel → single claimed site activation → native combat → observed outcome → remembered aid/trust → return/home consequence → later independent invitation. No current method collapses these stages into recruitment.

## Remaining exact blockers for a live 1.0.32.7 port

1. **Stable identity/store adapter:** the live core still lacks PersonId/EncounterId/SiteId/CampId/IncidentId ZDO keys and compare-exchange stores for the module ledgers.
2. **Spawn adapter:** `IEncounterSpawnAdapterV1` is now defined, but no 1.0.32.7 implementation proves safe placement, provenance tags, idempotence by spawn request/claim, reconnect, or cleanup. Core raid test history is not a spawn API.
3. **Travel movement adapter:** RescueRelease now owns temporary assignment/return and portal permission, but no live movement adapter carries that same actor to the verified target while respecting core movement-domain ownership, unload, reconnect, sailing, hazards, and portal rules.
4. **Loadout application adapter:** policy owns legal variants, but abstract gear IDs are not verified Valheim prefab IDs and no atomic same-actor inventory/application adapter connects them to `NpcBody.Outfit` or a finer personal-item seam.
5. **Native combat handoff:** EncounterAllegiance defines `INativeCombatHandoff`, but 1.0.32.7 has no supported public authored-target commit method. Integration must wait for native target/phase evidence until a domain-safe seam is added.
6. **Allegiance adapter:** authored allegiance resolution is still absent; native `Character.Faction` alone is insufficient.
7. **Specialist adapters:** no current adapter supplies live-verified attack candidates, authoritative motion/casts, ally/neutral safety, native readiness/recovery, or confirmed projectile/ammo release.
8. **Observed completion adapter:** `IEncounterCombatObserver` is defined but not implemented against claimed actor provenance and authoritative loaded death/damage evidence.

## First compiler-ready 1.0.32.7 adapter seam

The first safe compiler step is an in-assembly `CampaignEncounterAdapter` added beside the maintained core's `NpcKernel`/`NpcBody`, compiling these pure modules as references but implementing only observation and storage initially:

1. Gate every mutation with `ZNet.instance.IsServer()` and loaded actor writes with `NpcBody.IsOwner`.
2. Add versioned stable identity/revision ZDO keys and module serializers/stores; expose compare-exchange by expected revision.
3. Project `NpcKernel.All`, `NpcKernel.Damaged`, `NpcKernel.Removed`, `NpcKernel.Frame.Phase`, `Body.View.GetZDO()`, and loaded target/death facts into allegiance, activation observation, and safe-return evidence.
4. Keep `IEncounterSpawnAdapterV1.RequestOnce`, `INativeCombatHandoff.CommitTarget`, loadout application, movement, and specialist execution disabled/fail-closed until each has its separate ownership/native/live proof.

This storage/observation adapter is compiler-ready against the source-inspected 1.0.32.7 surfaces without inventing a spawn or combat writer. It can be followed by the spawn adapter only after stable claims/provenance are durable.

## Next implementation order

1. Implement the compiler-ready storage/observation seam above on a new post-1.0.32.7 source branch.
2. Implement same-actor temporary travel through the core movement/domain scheduler; keep RescueRelease as assignment owner.
3. Implement `IEncounterSpawnAdapterV1` with durable claim/provenance and no vanilla raid-clock dependency.
4. Implement loadout mapping/application against verified item prefabs while preserving the same actor and personal gear lineage.
5. Implement authored allegiance resolution and only then a domain-safe native target handoff.
6. Implement specialist execution through the core's sole native-state writer, then run native/ownership/IL gates and dedicated-server tests.
