# Living World quest broker

This directory contains an engine-free, server-authoritative policy for presenting
one persistent world request through a camp person, a settlement notice board, or
a jarl. Those are delivery surfaces, not separate quest generators. Every
presentation points to the same `WorldRequest.RequestId`, target site identity,
status, expiry, and single claim.

Visibility defaults to `PrivateIssuerOnly`. A companion's hunger, trust test, or
other personal need stays in direct conversation unless its authored record
explicitly sets `PublicSurfaces`. Boards and jarls fail closed for private
requests. Civic work and jarl commissions may use the same machinery, but retain
their own stable issuer and authored scope.

The broker accepts only authored requests joined to current server facts:

- a stable campaign person and party;
- an actual persisted site and exact site identity known to the issuer;
- matching world, biome, progression, safe-ground, reachability, and bounded
  distance facts;
- a deterministic expiry and post-resolution cooldown;
- for a miniboss, an authored encounter and a verified safe runtime adapter.

It never searches for the nearest convenient dungeon or enemy, fabricates a map
pin, simulates remote success, spawns a creature, or promises loot. Completion
requires an authoritative observation while the player and target are loaded at
the recorded site. A stable claim is committed once; its meaning and any legal
reward transaction belong to the live integration.

## Module boundary

`NeedsAndAffinity` remains the owner of a person's concrete need, remembered aid,
and qualitative trust. An adapter may promote an eligible authored need into this
broker by retaining the exact person, encounter, target, and authored-need
identity. The broker adds durable multi-surface offer tracking; it does not add
affinity or decide recruitment. `RescueRelease` remains the only owner of the
final invitation, roster, home, and expedition decision.

`Discovery` remains the source of persisted campaign site identity and safe
placement. The broker consumes known site facts; it does not place sites.

## State model

`Offered -> Accepted -> Completed` is the successful path. `Offered -> Refused`
and either open state to `Expired` are terminal alternatives. Completion is never
inferred from expiry or unload. Refusal, completion, and expiry retain the request
forever, preventing the same authored request from becoming a repeatable bounty.
Applied command IDs make network retries idempotent, and all records serialize
through `QuestBrokerPersistence`.

Run the engine-free checks with:

```powershell
& .\tests\Run-Tests.ps1
```
