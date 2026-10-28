# Quest broker adapter handoff — maintained 1.0.x

The files in this directory are pure policy. They do not prove that board UI,
jarl actors, map-pin sharing, encounter spawning, or reward transactions exist in
the maintained Unity line.

## Authoritative transaction order

1. On the server, resolve the stable party, campaign person, encounter, world,
   and persisted site records. Join an authored request definition to current
   world keys and site facts. Missing observations are false.
2. Prove the site identity—not merely a display name or coordinates—plus biome,
   safe ground, reachability, and bounded issuer-to-target distance. Use actual
   stored Discovery/world knowledge. Never substitute an arbitrary nearest
   dungeon, village, portal, boss altar, or creature spawn.
3. Call `Create`, then compare-and-swap `BrokerState` by revision. Only after the
   save succeeds may UI or dialogue present the offer. On a conflict, reload and
   retry with the same command ID.
4. A camp person, board, or jarl must have its own verified `PresentationProof`.
   `PresentExisting` adds a presentation referencing the existing request ID. It
   must never clone the request to make a second quest journal entry or payout.
   Visibility defaults to private: do not set `PublicSurfaces` merely because a
   board or jarl exists. Public relay requires an authored decision by the issuer;
   private hunger, grief, trust, and personal obligations remain private.
5. Acceptance and refusal mutate the shared request. All surfaces read that
   status. Remove closed UI presentations after persistence succeeds; do not
   delete the history.
6. Build `CompletionProof` only from a loaded, authoritative observation at the
   exact stored site identity with the player present. Absence after zone unload,
   elapsed time, client RPC claims, or a different regenerated site is not proof.
7. Save the completed state before performing any downstream acknowledgement.
   `SingleClaimCommitted` and `ClaimId` authorize at most one adapter transaction;
   they do not describe or guarantee loot. The reward adapter must independently
   enforce progression and its own idempotent claim ledger.
8. Expiry is an explicit server tick transition. It closes bookkeeping only. It
   does not kill enemies, rescue people, move actors, consume supplies, grant
   reputation, or pay rewards offscreen.

## Surface work still required

- **Camp person:** connect authored dialogue to stable campaign identity and the
  existing Needs/Affinity record. The person must be loaded and nearby.
- **Settlement board:** implement a board object and journal UI that stores the
  broker request ID. Establish the settlement as known and ensure the board is a
  real persisted object before presentation.
- **Jarl:** no jarl is assumed to exist. A future stable campaign actor may relay
  an existing request only after actor, settlement, and knowledge facts are live.
- **Map pins:** share a pin only for the request's verified `SiteIdentity` and only
  when the authored source is entitled to precise knowledge. Otherwise present a
  bounded direction or region. Never invent a location from narrative text.
- **Miniboss encounter:** implement `IAuthoredEncounterAdapter` against a reviewed
  prefab/spawn plan. It must own safe placement, server-only spawn, stable actor
  identity, duplicate suppression, cleanup, death attribution, unload/reload,
  and terminal outcome. Until all checks pass, candidate flags remain false and
  the request cannot be created. The broker itself never calls a raw spawner.
- **Rewards:** define and verify a separate progression-safe, single-claim game
  transaction. No authored text should promise loot before this adapter exists.

## Live proof gates

Test dedicated-server authority; two players opening different surfaces at once;
save/restart in every status; stale compare-and-swap retries; source destruction;
board and jarl knowledge boundaries; exact pin identity; target regeneration;
zone unload during an accepted request; expiry without simulation; illegal world
keys; cooldown; duplicate completion callbacks; and one claim across server
restart. For minibosses additionally test blocked terrain, duplicate activation,
unload/reload, cleanup, ownership transfer, death attribution, and no reward on a
failed or mismatched outcome.
