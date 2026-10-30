# FormidableAI shadow observer and telemetry

Status: wired as a disabled-by-default, zero-domain behavior in the 1.0.32.22 candidate.
Offline build, installation and live proof are still pending.

## Purpose

Shadow mode lets FormidableAI predict a decision while native combat remains authoritative. It then
compares the observed native action with the prediction without issuing any action, movement,
facing, target or attack command. This creates evidence for transferring one decision domain rather
than replacing behavior on intuition.

## Recorded facts

- decision domain and observed/expected owner;
- native action and predicted Formidable action;
- prediction reason;
- target and physical attack identities;
- observed and predicted action time;
- signed timing delta;
- deterministic mismatch classification;
- actor identity and monotonic trace sequence.

## Mismatch precedence

The first structurally meaningful disagreement wins:

1. missing observation or prediction;
2. domain/owner conflict;
3. missing or different target identity;
4. different physical attack identity;
5. different action;
6. prediction early or late beyond tolerance;
7. match.

This precedence prevents a timing number from hiding that two systems acted on different people,
different attacks or under conflicting ownership.

## Bounded trace

`BoundedShadowTrace` is a fixed-capacity ring. It assigns monotonic sequence numbers, overwrites the
oldest entry when full and returns a read-only chronological snapshot. Runtime integration should
use one bounded buffer per observed actor or a globally budgeted equivalent; no unbounded combat log
belongs in the actor update.

## Proof gates

Offline tests cover match, ownership, target, attack, action and timing classification; signed timing
deltas; missing sides; deterministic ring eviction/order; immutable snapshots; and absence of an
execution-command surface.

## 1.0.x adapter boundary

- `LivingWorldCORE/npc/src/Behaviors/FormidableShadowBehavior.cs` reads the `NpcFrame` already
  sampled by `NpcKernel` and declares `NpcDomain.None`;
- it observes native target/action identity without changing them;
- it feeds class predictions from the same sampled frame used by the 1.0.x kernel;
- it is off unless `FormidableAI/ShadowObserver=true`;
- each actor retains only 64 comparisons;
- comparison sampling is capped at 2 Hz per actor and every retained comparison is exported as a
  `FormidableShadow` JSONL record by the existing session telemetry;
- no 0.3.x component or prefab participates in this candidate.

## Remaining adapter work

- define stable physical attack IDs and authoritative timestamps;
- measure sampling/export cost at 1/10/50/100 actors;
- add analyzer aggregates for mismatch counts after the first wiring session establishes the schema;
- never let shadow exceptions or missing reflection affect native combat.
