# Living World Human NPC 0.5.0 behavior kernel

0.5.0 preserves the accepted 0.4.1 native-human creation and appearance path.
The individual human factory, randomized sex/body/hair/beard identity, saved
appearance record, and helmet-free acceptance loadouts were not redesigned.

## Runtime ownership

`HumanBehaviorController` is the only writer of the companion's behavior
state. It takes one bounded snapshot and chooses exactly one state:

- `AwaitingAppearance`
- `AwaitingLeader`
- `IdleGuard`
- `Traversal`
- `TraversalSprint`
- `CatchUp`
- `CombatReadiness`
- `NativeCombat`
- `CombatClear`
- `Regroup`

Invalid or incomplete input retains the exact current state. It is not counted
as evidence that combat ended or that another behavior should begin.

`HumanTraversalModule` owns movement only during calm traversal, readiness,
catch-up, and regroup. It leases one of six rear/flank positions, routes around
the leader exclusion area, matches ordinary travel pace, gains a bounded speed
advantage during catch-up, and releases its lease and follow target immediately
when native combat owns the actor.

`HumanEquipmentModule` owns only hand/back posture. Calm traversal and regroup
use Valheim's native `HideHandItems` lifecycle. Readiness and combat restore the
same hidden pair through native `ShowHandItems`; the module does not substitute
a hardcoded weapon or shield.

`MonsterAI` exclusively owns target selection, facing, pursuit, circling,
blocking, weapon choice, and attacks during `NativeCombat` and the five-second
`CombatClear` verification window. Formation movement cannot execute in either
state.

## Scope boundary

The exactly-two rule belongs only to the `lwparty` laboratory replacement
transaction. Future world encounters and recruitment may create one person or
any deliberately authored group, while every individual still passes through
the same verified native-human factory.

This build does not yet claim torch, wetness, door, workstation, chair, bed,
town-resident, settled guard-sector, or command-wheel behavior. Those modules
must enter through the state contract without changing the accepted identity
factory or competing with native combat.

## Verification

The build gate compiles with warnings treated as errors and runs deterministic
contracts for randomized visible identity, durable combat ownership, invalid
input retention, catch-up hysteresis, combat-clear timing, and regroup release.
Live Unity verification is still required for visible back stowing, draw
restoration, traversal pace, non-pushing formation travel, and uninterrupted
native combat.
