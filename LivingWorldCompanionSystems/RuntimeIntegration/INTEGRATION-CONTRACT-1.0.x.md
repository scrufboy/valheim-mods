# Companion encounter runtime integration contract

Status: engine-free prototype and 1.0.x port contract. This is not a playable build and is not installed.

## Identity and transaction boundary

Every encounter aggregate carries five ordinal, stable IDs:

- `EncounterId`: the deterministic discovery instance (`LeadRecord.EncounterId`), never the encounter template ID.
- `SiteId`: the selected discovery site.
- `PersonId`: the permanent person identity used by lifecycle, roster, death, gear, and spawn claims.
- `CampId`: the permanent/current camp identity used by lifecycle. A rescue `SafeDestination.DestinationId` is not silently treated as a camp ID.
- `IncidentId`: the single danger event used by Friend in Need from first material evidence through combat handoff and resolution.

`EncounterArcState` is the persistence unit. A mutation clones the whole aggregate, applies all module changes, validates cross-module identity, increments one aggregate revision, and publishes it through `CompareExchange`. Rescue plus lead closure and observed death plus roster death therefore cannot commit half a transition. Stable transaction IDs make retries idempotent.

The store implementation must be server-only and provide real compare-and-swap semantics. Per-field ZDO writes are not an acceptable implementation of this contract because they expose partial revisions.

## Runtime sequence

1. Discovery chooses a safe site and produces the encounter instance ID. Creation validates that the lead, camp, lifecycle person, and roster person agree.
2. A zone-load adapter uses `PersonSpawnClaims.TryClaim`. A second GameObject for the same `PersonId` is refused. Zone unload releases only the process claim; persisted identity and life remain.
3. An unloaded observation follows `CampLifecyclePolicy.Observe`: it requests despawn and does not persist death. Player distance also requests despawn unless this is an authored server-resident camp and the adapter proves actual retained-zone ownership plus away-safety enforcement. A resident actor may continue bounded camp routines without a nearby player. Death still commits only with loaded, near, existing actor, authoritative owner, place, and cause evidence.
4. Material danger enters Friend in Need once. When native combat commits, the alert emits `YieldToNativeCombat`; lifecycle receives combat ownership and camp activity yields.
5. Rescue outcome and discovery lead closure publish in one aggregate revision.
6. Release may enter `AwaitingSafeHandoff`. It becomes `BondedResident` only after the destination is established and loaded, the same companion is present there, unsafe transitions are false, and player-loan exchange is complete.

The owner-directed aid/event extension is deliberately staged: uncertain witnessed contact, accepted authored aid, temporary same-person travel to a verified site, a pre-spawn single event claim, native-owned combat, one observed outcome, remembered trust, return consequence, and only later a separate invitation. The landed Needs, QuestBroker, RescueRelease, EncounterActivation, and EncounterLoadouts contracts now own those distinct records; RuntimeIntegration validates their shared IDs and delegates without copying their ledgers. Live movement, spawning, equipment application, and specialist execution remain fail-closed until their 1.0.32.7 adapters pass ownership and native proof.

## Extension seam for affinity, voice, and earned respect

`RecordFacts` accepts stable key/value evidence from independent modules and commits it in the same aggregate revision. Suitable inputs include verified camp/world observations, light-affinity observations, aid outcomes, and remembered earned-respect deeds. The producing module owns the meaning and validation of its vocabulary; this runtime validates only safe stable keys and bounded values.

Facts are evidence only. Recording `respect.aid_outcome=completed` or `affinity.camp_light=warm` does not recruit, promote, or bind anyone. A later invitation adapter must translate the relevant verified facts into `InvitationFacts` and call the deterministic invitation policy as a separate yes/no transaction. Presentation such as encounter voice likewise consumes committed facts without gaining authority to change identity, rescue, combat, or recruitment state.

No raid, portal, light-source, or world-event API is assumed here. Those facts may enter only after a live adapter verifies the corresponding Valheim source API and its server/ownership semantics.

### Quest/request offers

QuestBroker is the sole durable owner of request lifecycle. `EncounterArcState` has no request dictionary, request status, or request revision. `QuestBrokerBridge` delegates creation and presentation directly to `QuestBrokerPolicy` and returns its `BrokerState`; the caller must persist that state through `IQuestBrokerStateStore.TrySave` using the broker revision.

Camp people, settlement boards, and jarls create `OfferPresentation` records that all reference the same `WorldRequest.RequestId` in one `BrokerState.Requests` dictionary. Integration may persist authoritative knowledge evidence in its fact ledger, but it cannot translate that evidence into a second request record. QuestBroker validates the persisted target site and current knowledge facts when constructing its `RequestCandidate`.

Visibility is fail-closed. The default `PrivateIssuerOnly` request may be presented only by its camp issuer; a board or jarl relay must be rejected without changing broker revision or adding a presentation. Cross-surface relay requires the candidate to carry explicit `RequestVisibility.PublicSurfaces` authorization before QuestBroker creates the durable request. Public camp, board, and jarl presentations still point to one broker-owned request record rather than copying its status.

The bridge does not accept the request, spawn a target, activate a boss, place a map marker, or infer that camp/board/jarl objects exist. Those effects require QuestBroker's separately verified server-side adapters. Optional boss gating remains in QuestBroker policy and facts, not in an unverified spawn call.

## Verified 1.0.x source seam (read-only inspection)

The maintained source exposes these usable facts/APIs:

- `ZNet.instance.IsServer()` is already the plugin's server-authority gate (`CorePlugin.RequireWorld`).
- `NpcKernel.All` is the loaded companion registry; `NpcKernel.Removed` distinguishes `"death"` from `"unloaded/destroyed"`; `NpcKernel.Damaged` and `RecentAttacker(seconds)` provide concrete damage evidence.
- `NpcKernel.Frame.Phase` exposes `Readiness`, `Combat`, `CombatClear`, and `CombatRegroup`; native combat is already the high-priority behavior and owns all domains during combat.
- `NpcBody.IsOwner` verifies network ownership. `NpcBody.View.GetZDO()` is available for adapter persistence, though aggregate serialization and CAS still need to be added.
- `NpcLeaderBinding.LeaderId` is public and persisted. `Bind` is internal, so integration inside the same assembly can bind after recruitment; an external assembly cannot call it directly.
- `NpcKernel.Died`, `Body.Human.IsDead()`, and the `Removed` reason can contribute loaded death evidence. Unload alone must never be converted into death.
- `ZDOMan.GetAllZDOsWithPrefabIterative`, `ZNetScene.instance.FindInstance`, owner-checked `ZNetScene.instance.Destroy`, and `ZDOMan.instance.DestroyZDO` are demonstrated for loaded/unloaded record enumeration and cleanup.
- `NpcBody.PlaceAt`, `HealFull`, and `Outfit` are internal. They are available only if this adapter is compiled into the core assembly or the core deliberately exposes public adapter methods.

## APIs that do not exist yet

The inspected 1.0.x source has no stable `PersonId`, `EncounterId`, `SiteId`, `CampId`, or incident keys on companion ZDOs; no encounter aggregate serializer/CAS store; no camp/site prefab lifecycle; no resident/home handoff adapter; no supported public spawn/recruit API; no safe-destination proof API; no loaned-item ownership ledger; and no API that forces native combat from an alert. The integration must observe native combat commitment, not synthesize it by writing private AI state.

The existing `lwparty` transaction creates a debug pair with a random generation and player binding. It must not be reused as encounter recruitment without adding the stable identities and a single-person, server-authoritative creation transaction.

## Concrete live-port steps

1. Add versioned ZDO keys for the five IDs and the aggregate revision, plus a serializer with corruption/unknown-schema refusal.
2. Add a server-owned aggregate store with one serialized record and compare-exchange against its revision; route every encounter command through it.
3. Add a public/internal core adapter that creates exactly one actor from `PersonId`, writes all IDs before promotion, and enforces `livingworld.person.<PersonId>` across loaded actors and ZDO records.
4. Feed `NpcKernel.Damaged`, hostile targeting, camp damage, and incapacitation into Friend in Need; treat `Frame.Phase` combat states as the handoff confirmation.
5. Feed `Removed` plus loaded actor/owner/death facts into the death gate. Explicitly classify `unloaded/destroyed` as non-death.
6. Implement destination-to-camp mapping and physical arrival proof. Do not copy a safe destination ID into `CampPerson.CampId` without that mapping.
7. Implement loan ownership/exchange, then connect release request/commit to the physical handoff.
8. Compile inside the 1.0.x project, run its ownership/native/IL gates, then run dedicated-server and reconnect tests before any profile install.
