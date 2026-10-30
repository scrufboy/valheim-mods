# FormidableAI shadow observer and telemetry

Status: offline read-only contracts. Not runtime-wired, installed or live-proven.

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

## Required adapter work

- observe native target/action/attack identity without changing them;
- feed predictions from the same immutable combat frame used by policy;
- define stable physical attack IDs and authoritative timestamps;
- cap sampling/export cost and redact no personal/game paths into Git;
- expose aggregate mismatch counts plus a short trace for Brian's A/B review;
- never let shadow exceptions or missing reflection affect native combat.
