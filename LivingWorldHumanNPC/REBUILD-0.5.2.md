# Living World Human NPC 0.5.2 measured state-boundary correction

0.5.2 preserves the accepted native-human creation transaction, randomized sex,
body, hair, beard, name, loadouts, and exactly-two `lwparty` laboratory command.
It also preserves the 0.5.1 exclusive state machine, travel-derived formation
frame, native combat handoff, equipment stowing lifecycle, and native Follow as
the only calm pathfinding writer.

## Evidence corrected

The synchronized 2026-10-21 18:28 run proved three remaining boundary faults.

1. The controller treated `Player.GetCurrentWeapon()` as visible posture.
   Valheim returns an invisible unarmed/fists attack through that API when both
   visible hands are empty. Both companions therefore remained in readiness for
   roughly 215 seconds even though the user's weapon was visibly stowed.
2. Native Follow deliberately accepts arrival anywhere inside three metres of
   its target. Supplying the intended formation lease as that target made actors
   stop up to three metres behind the lease, then restart as the leader moved.
3. Marking the actors tame borrowed one useful native target-range rule but did
   not supply Valheim's complete `Tameable` pet lifecycle. Native skill handling
   consequently emitted repeated "tamed but missing tameable" warnings.

## Correction

- Readiness now inspects only the actual left/right hand-item fields. A real
  visible non-torch weapon counts; the invisible unarmed fallback cannot count.
- Native Follow remains the only traversal writer. While the leader is actually
  travelling, the hidden follow anchor is placed 3.25 metres ahead of the lease.
  Native Follow's three-metre arrival radius therefore settles the actor near the
  lease instead of three metres behind it. The lead disappears when the leader
  stops, and stationary turning still cannot rotate the formation frame.
- The false tame flag is removed. During combat the actor still retains the
  leader as its native follow target for deterministic return. The controller
  enforces one explicit outer party rule: a target beyond twenty metres from the
  moving leader is retired. Inside that boundary, native `MonsterAI` exclusively
  owns selection, facing, pursuit, blocking, weapon use, and attacks.
- Telemetry 0.5.1/schema 6 separately records visible left/right leader hands,
  the native current attack item, and combat-boundary release count. This makes a
  false readiness state or excessive boundary churn directly measurable.

## Offline verification

The human and telemetry assemblies compile with warnings treated as errors.
Identity, durable combat ownership, invalid-input retention, exclusive state
selection, readiness precedence, catch-up hysteresis, and regroup contracts pass.
Build guards reject the old current-weapon readiness test, fake tame state, a
second traversal movement writer, or loss of arrival-radius compensation.

Live Unity verification is still required for visible stow/readiness release,
smooth walking formation arrival, sprint catch-up, one bounded natural encounter,
and the absence of the prior tame-integration warning.
