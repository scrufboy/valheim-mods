# Review: 1.0.32.23 profile (`91b361f`) and 1.0.32.24 bomb range (`d5a7c51`)

Both canonical-built on Linux/Mono against Valheim 1.0.21 refs. Nothing installed.

## Gates (OFFLINE-TESTED)

| Candidate | Ledger | Native | Ownership | Voice lock | Movement writer | Analyzer | Core DLL |
|---|---|---|---|---|---|---|---|
| 1.0.32.23 `91b361f` | 130/130 | 46 | 76 sites, all allowed | LOCKED, unchanged | OK | OK | `a8569135…` |
| 1.0.32.24 `d5a7c51` | 131/131 | 46 | 76 sites, all allowed | LOCKED, unchanged | OK | OK | `8b849bc0…` |

`d5a7c51` is stacked on `91b361f`; it is not independent of the profile candidate. Installing .24 installs both.

## 1.0.32.23 profile findings

1. **Defect - torch counts as a one-handed weapon (SOURCE-INSPECTED).** Native `ItemDrop.ItemData.IsWeapon()`
   (1.0.21 IL) returns true for item types 3, 4, 14, 22 **and 15 (Torch)**. `Profile()` sets `oneHanded` from
   `IsWeapon()` minus bow/two-handed only, so a utility torch plus a shield grants `Shield` doctrine and
   `BlockObservedStrike|ProtectPartner` with no blade. Every companion carries a utility torch. Fix: exclude
   `ItemType.Torch` (and test torch+shield -> no Shield doctrine).
2. **Scout is unreachable on 1.0.x (SOURCE-INSPECTED).** The 1.0.x line registers only `guard` and `archer`
   roles (`NpcFactory`/`CorePlugin`); there is no `"scout"` role string anywhere in `LivingWorldCORE/npc/src`.
   The scout->Pathfinder mapping is correct in the factory but no live companion can reach it; a legacy scout
   prefab persists as `guard` -> Warden. Brian's "scout" class needs a real role/prefab before Pathfinder data exists.
3. **Training band is a default, not a fact.** Every live profile is `Trained` (and `Untrained` is promoted
   to `Trained`). Fine as a placeholder; label it UNKNOWN/provisional in telemetry rather than a rolled value.
4. Doctrine otherwise follows inventory prerequisites; no numeric combat tuning or invented origin/culture
   (Origin=Unknown, Culture="") slipped in. Telemetry fields are additive; the schema-12 analyzer accepts them.
5. Note: the four profile fields are written on every `FormidableShadow` row (~2 Hz x actors). Consider one
   `FormidableProfile` row on change instead.
6. Measurement corrections 1-3 from the live-result request (idle identity, ownership separation, attack window)
   and the cost measurement are **not** in this candidate yet. A shadow run of .23 will repeat the same
   misleading `MissingIdentity`/`DomainOwnerConflict` labels.

## 1.0.32.24 bomb range findings (scope lock respected: only Speed and band changed)

- **Ballistics OK (SOURCE-INSPECTED + arithmetic).** 15 m/s, g=10: flat max 22.5 m; at 20 m the low arc is about 31 deg
  and reaches targets up to about 2.4 m above the thrower; the solver already returns "out of reach" beyond that.
  Flight time at 20 m is about 1.5 s (vs about 1.4 s at 13.5 m under the old speed), and friend clearance already uses
  `FlightTime`, so landing prediction scales with the longer flight. `ThrowAimHook` reads `ThrowPlanner.Speed`,
  so planning and native execution use the same speed.
- **Gates that still limit early acquisition (SOURCE-INSPECTED).** `StepBomb` runs only inside native combat,
  requires a native target (`f.Senses.Target != null`), not-busy (including `IsDrawingBow`), and hostiles that
  `CanSeeTarget` within 30 m. A cluster 15-20 m out is only considered once the companion has already
  taken a target.
- **Live evidence from 1.0.32.22 (LIVE-PROVEN data, prediction INFERRED).** 57 throws: median 9.1 m, min 6.0, max 13.3;
  **28 of 57 (49%) were under 9 m** and would now be refused. Target choices: median 7.4 m, 42% at 9 m or more.
  Expect fewer throws unless clusters are acquired before they close; the video test should show whether the
  companions throw earlier or just throw less.
