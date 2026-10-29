# Friend in Need alert contract

This module defines the pure, adapter-neutral event/state contract for danger
witnessed at a persistent camp or against a known friend. It does not acquire a
combat target, drive movement, equip weapons, or assign audio clips.

## Ownership and priority

- Permitted persistent orders: Resident, Follow, and Hold. Encounter NPCs without
  a companion order may also publish through their encounter owner.
- Exclusive owner: Alert/readiness only.
- Higher-state cancellation: dead/incapacitated and native Combat immediately
  prevent or cancel alert ownership. `YieldToNativeCombat` means the adapter must
  stop all Friend in Need facing, movement, hand, and animation work.
- Entry evidence: an identified camp or known friend plus committed or material
  danger: hostile targeting, attributed damage, camp damage, or protected-actor
  incapacitation. `VisibleHostile` alone remains peaceful watching/readiness and
  cannot originate this alert.
- Exit evidence: an explicit Safe/Defeated/Lost resolution, stale evidence, or
  native combat commitment. Combat yield is not resolution; the adapter records
  the final outcome after combat-clear evidence.
- Forbidden control: native target selection, pursuit, attack timing, blocking,
  weapon lifecycle, formation, door traversal, and resident/settled activity.

The first non-combat witness emits one symbolic request for
`lwc.voice.surprised.existing`, matching the currently approved surprised A/B
implementation. No filename, gender mapping, probability, cooldown, or trigger
is defined here. If combat is already committed, the alert does not emit that
request because native combat presentation already owns the transition.

The initial danger signal is followed by at most one refresh, no faster than
eight seconds apart and only for a newly witnessed material fact such as damage,
a breach/loss, or incapacitation. Continued visibility and elapsed time never
create a refresh. Resolution is versioned and persistent. Re-observing the
same resolved event ID after load produces no commands, preventing shock, text,
or marker replay spam.

Run `Test-FriendInNeedContract.ps1` for pure offline coverage.

## Accepted-aid sites

`WitnessAcceptedAidSite` is a signal-only boundary. `EncounterActivation` owns
site activation, hostile creation, and event execution. Rumor and a merely visible
creature produce nothing. An authored site incident requires deliberate accepted
aid, verified site activation, a real committed attack/damage/casualty, and a local
witness. A naturally arriving third-party creature may independently qualify once
it commits or causes harm. Remote facts never originate alerts. Existing event-ID
persistence provides the same no-replay guarantee as other incidents.
