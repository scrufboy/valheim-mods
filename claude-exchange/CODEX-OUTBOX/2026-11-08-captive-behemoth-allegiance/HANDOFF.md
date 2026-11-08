# Captive Behemoth instance allegiance handoff

Date: 2026-11-08. Owner: EncounterAllegiance only.

Authority read before implementation:

- `LWCModelSkinStudio/exchange/real-smoke-fire/2026-11-08/SWAMP-FACTION-COORDINATION.md`
- `LWCModelSkinStudio/docs/project/BEHEMOTH-BINDING-RITUAL-2026-11-07.md`
- current `FormidableNPCFramework/src/ShamanControlPolicy.cs` and tests
- current Behemoth, shaman, minion and swamp ownership boundaries

## Exact current step

SOURCE-IMPLEMENTED and OFFLINE-TESTED: the encounter/allegiance owner now provides
one engine-free, instance-local persisted authority in
`LivingWorldCompanionSystems/EncounterAllegiance/CaptiveBehemothAllegiance.cs`.
It consumes verified receipts from existing owners and does not add a creature,
spell, minion, raid or global faction runtime.

The state loop is `DormantBound → Controlled → ReleasedFrenzy → ReleasedUndead`,
with a newer verified binding receipt returning red or green to `Controlled`.
`Dead` is terminal. Physical deployment remains controlled. A real spell break
immediately removes captor protection and starts a caller-configured bounded
frenzy. Expiry restores undead-only alliance while Necks/minions/shaman/player
remain hostile.

The exact shared machine contract is
`captive-behemoth-state-contract.v1.json`. `RuntimeAdapterContracts.cs` defines
the persistence, receipt and instance-relation seams other owners can implement.

## Ownership coordination

- Shaman owner: retain cast/rebind decisions and native completion. Supply only a
  verified `BindingCompleted` receipt with controller ID and newer revision.
- Spell/anchor owner: define actual break criteria and supply only a verified
  `SpellBroken` receipt plus configured frenzy duration.
- Behemoth owner: retain accepted rig, appearance, attacks, normal tuning,
  damage/death/drop/corpse lifecycle and effects. Consume committed presentation
  outputs; do not infer allegiance from colors.
- Minion owners: retain protection/defense execution. Read state while screening
  the shaman; do not grant control or captor safety.
- EncounterAllegiance: owns only the exact-instance state, idempotent transitions,
  persistence shape and both-order relationship answer.

## Exact blocker

No source blocker remains inside the pure allegiance owner. Cross-owner runtime
integration is blocked on the existing owners supplying native bind and real
spell-break receipts, selecting explicit encounter frenzy duration/criteria,
and wiring one network/CAS persistence adapter. Those are deliberately not
invented here.

## Status

- Source: implemented.
- Compiled: pure C# 5 test executables compile with warnings as errors.
- Offline tests: existing encounter policy 55/55; captive Behemoth 35/35.
- Packaged runtime: no.
- Installed: no.
- Game-tested: no.
- Active profiles/assets/accepted creature behavior changed: no.

Planned later lizard or clothing purchases are not required by this state and
relation foundation.
