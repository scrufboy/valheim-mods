# FormidableAI offline foundation update

Date: 2026-10-30

## Outcome

Following Brian's direction that FormidableAI become LWC's full replacement combat AI, Codex and
three parallel review/implementation workstreams completed the first clean-room offline foundation.

Evidence classification: **CODE-TESTED**. Nothing is installed or live-proven.

## Implemented foundation

### Kernel

- Immutable `CombatFrame` and threat observations.
- Explicit domains: Perception, Targeting, Movement, Facing, Defense, Equipment, Attack and Support.
- Deterministic priority and tie arbitration.
- Atomic multi-domain leases.
- Explicit native fallback for unclaimed migration domains.
- Cancellation/handback and remote-owner refusal.
- Per-module exception isolation and disablement.
- Reusable candidate, proposal, winner, lease, fault and output buffers.

### Action-moment defense

- Target-isolated, time-based perception.
- Block/dodge decisions based on stable actor/threat/attack identity, ownership, authorization,
  attack activity, impact window, range, facing, stamina, equipment and incapacitation.
- Exact-once reaction memory and bounded cooldown.
- No movement, facing or attack command surface in the defense policy.

### Class capabilities

- Warden, Huntress, Mystic, Skirmisher, Commander and Pathfinder compositions.
- Class modules now emit the shared kernel's `CombatProposal` and `DecisionDomain`; the duplicate
  class-domain abstraction was removed.
- Range-band and firing-lane reasoning, support safety, bounded throws, investigation handoff,
  target-loss cancellation and Started → Confirmed → Recovering → Idle lifecycle.
- Exact defense policy has decisive priority over generic class defense intent; arbitration retains
  one Defense owner.

### Shadow telemetry

- Read-only native-observation versus Formidable-prediction comparison.
- Captures expected owner/domain, action/reason, target identity, physical attack identity, signed
  timing difference and deterministic mismatch classification.
- Fixed-capacity chronological trace.
- No execution, movement, facing or destination command.

## Verification

- Kernel: 13 passing checks.
- Defense/perception: 10 passing checks.
- Class proposals/lifecycle: 10 passing checks.
- Cross-layer integration: 6 passing checks.
- Shadow telemetry: 8 passing checks.
- Total: **47 FormidableAI checks passing**.

Existing specialist baselines also remained green during foundation work:

- 21 melee-role checks.
- 14 prediction/throw checks.
- 11 comparative ownership/perception/transaction checks.

An audit-host benchmark of 500,000 buffered two-module arbitration evaluations measured 205.587 ms
total with a coarse 90,152-byte retained-memory delta. Winner identities remained deterministic.
This is not Unity or live actor-count evidence.

## Reconciliation decisions

- One kernel arbitrates all class/specialist proposals.
- Class profiles grant capabilities and priorities; they are not independent brains.
- Coupled actions lease all required domains atomically.
- Timers may throttle scans or enforce cooldowns but do not decide without explicit world/action
  facts.
- Native fallback remains during migration only. No native domain has been retired.
- No package code or assets from VikingAI, VikingNPC or RenegadeVikings were copied.

## Next vertical slice

Build a read-only runtime observer that constructs Formidable frames and emits shadow comparisons
while native combat remains the sole runtime owner. It must first prove:

1. stable identity and target/attack correlation;
2. bounded observation and arbitration cost at 1/10/50/100 actors;
3. accurate prediction-versus-native telemetry;
4. no movement, facing, attack, ZDO or ownership writes;
5. safe behavior on missing reflection/API members;
6. dedicated-server/remote-owner passivity.

Only after Brian reviews shadow traces and video should the Defense domain be considered for the
first feature-flagged execution transfer with immediate native rollback.

## Commits

- `920ca3d` — class capability policies.
- `a377ee5` — arbitration kernel.
- `c4e9eef` — action-moment defense policies.
- `7e4b904` — decision-domain integration.
- `8900249` — reusable arbitration buffers.
- `826bf1b` — kernel-compatible class proposals/lifecycle.
- `a143e03` — shadow comparison telemetry.
