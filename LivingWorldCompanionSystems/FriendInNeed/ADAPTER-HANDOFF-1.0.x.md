# Friend in Need adapter handoff — Living World 1.0.x

Status: maintained integration boundary for the 1.0.x line. The pure contract is
implemented and tested; no game-profile files are changed by this module.

## Adapter responsibilities

1. Build a stable `eventId` from the persistent encounter/friend identity and a
   danger-episode identity. Do not use frame time or a random value regenerated
   on load.
2. Call `Witness` only on the authoritative network owner. Persist the returned
   state in the incident/encounter record whenever `Persist` is emitted.
   A merely visible nearby hostile is awareness only; do not manufacture committed
   or damage evidence to make it enter Friend in Need.
3. Map `ShowDangerSignal` and `ShowFollowUpSignal` to one clear HUD/world signal
   that identifies the endangered camp or friend. Remote clients display the
   replicated command; they do not run the reducer independently.
4. Resolve `PlayExistingSurprisedVoice` through the installed shared voice
   policy's existing surprised A/B pool. Do not add a clip, select a gender actor,
   alter weights, or reuse the current trigger for follow-ups.
5. Feed native combat commitment into `Witness`/`Tick`. On
   `YieldToNativeCombat`, release Alert/readiness immediately and let native
   `MonsterAI` own target, movement, facing, hands, and attacks.
6. After native combat clear, derive an explicit outcome from authoritative
   encounter facts and call `Resolve`. Persist before retiring or unloading the
   site. A yielded record must never be treated as automatically safe.
7. Call `Tick` with `newMaterialEvidence` only for a newly witnessed material fact
   in this episode, such as a new casualty/incapacitation, attributed injury, camp
   breach, or fire/structure loss. Cooldown expiry, continued visibility, and the
   same replicated fact are not follow-up evidence. Deduplicate source fact IDs in
   the adapter before reducing them to evidence flags. The contract permits one
   follow-up call total; later material facts update world state without another
   Friend in Need call.
8. On load, restore with `TryRead` before evaluating observations. A malformed or
   future-version record fails closed and must be logged for migration rather
   than overwritten silently.
9. For accepted-aid sites, call `WitnessAcceptedAidSite` only from local witnessed
   facts. `EncounterActivation` remains the sole owner of activation, spawning,
   and hostile commitment; Friend in Need must not start or advance that event.
   Pass rumor and visibility honestly—they produce no command. Set verified site
   activation only after the activation owner confirms it, and set natural third-
   party attack only for an independently committed creature attack. Do not relay
   an unwitnessed remote camp event into this API. Use the activation episode or
   natural-attack episode identity in `eventId` so replication/reload cannot replay
   the warning.

## Exact integration gaps

- The 1.0.x runtime's authoritative encounter persistence key/store and
  network-owner callback are not exposed in this folder.
- No verified API is identified for resolving a persistent camp/friend ID to a
  HUD marker position and localized display name.
- The shared voice policy's public symbolic resolver name for the existing
  surprised A/B pool is not documented; this module intentionally supplies only
  `lwc.voice.surprised.existing` as an adapter token.
- The exact native-combat commitment and repeated combat-clear callbacks for the
  1.0.x runtime are outside this module. The adapter must use the behavior-state
  authority, not infer commitment merely from a visible hostile.
- Multiplayer command replication/deduplication and late-join signal rebuilding
  need a runtime transport owner. The saved state contains the data required to
  rebuild a current signal without replaying voice.
- Localized signal copy and accessibility presentation are not yet authored.
  The contract carries subject identity and evidence, not final prose.
- The 1.0.x adapter still needs a verified `EncounterActivation` receipt and a
  local-witness/proximity callback for accepted-aid sites. Network receipt alone
  is not local witness evidence.

Any 1.0.x adapter change that alters these assumptions must update this handoff,
the pure tests, and the save-schema migration notes together.
