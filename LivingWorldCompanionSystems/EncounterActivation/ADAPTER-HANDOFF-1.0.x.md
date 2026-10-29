# Encounter activation adapter handoff (Valheim 1.0.x)

This folder is engine-free policy. It makes no claim that the inspected runtime currently supplies these adapters.

## Ownership boundary

- `QuestBroker` is the sole owner of request offer, acceptance, expiry, completion, and reward claim status.
- `EncounterActivation` owns the event execution ledger: exact site binding, arming, one stable spawn claim/request, adapter receipt, and observed clear evidence.
- The same persistent `RequestId` joins both records. Activation may notify the broker of clear evidence; it never awards or closes the broker request itself.

## Runtime sequence

1. Register the execution plan when the broker creates its offer.
2. Persist `Accepted` only after the broker has persisted deliberate player acceptance.
3. Revalidate progression, exact site identity and safety. Require server authority plus the accepting player and issuer group loaded at the target.
4. For a natural-world target, observe it in place and do not spawn anything.
5. For an authored group/miniboss, persist `Arming`, `encounter-claim:<requestId>`, and `encounter-spawn:<requestId>` before calling `IEncounterSpawnAdapterV1.RequestOnce`.
6. The external 1.0.x adapter must deduplicate that stable pair across retry, reload, zone-owner transfer, and server restart. Bind every spawned actor to the claim and return one durable receipt.
7. Let vanilla AI, damage, death, drops, and FriendInNeed witness/alert behavior proceed normally. Do not start, pause, replace, or reset the vanilla raid clock.
8. Clear only from loaded, witnessed, exact-site evidence with player/party contribution. A miniboss additionally requires its committed claimed death.

The travel agreement is temporary aid, not recruitment or a new permanent combat slot. The adapter must load the same `IssuerPersonId` record with that person's existing equipment; it must not create a stronger replacement. It also must not infer friendship, hostility, or faction from proximity, appearance, biome, or this request. Allegiance remains relationship-owned, recruitment remains a later separate invitation, and specialist attacks remain bounded capabilities under native combat ownership.

Failure and abandonment deliberately emit no cleanup directive. An adapter may retire an uninstantiated controller or later clean already-dead claimed records, but it must not kill offscreen actors, destroy arbitrary same-prefab creatures, or remove living hostiles visible to players. Rewards remain a single broker claim after broker validation.
