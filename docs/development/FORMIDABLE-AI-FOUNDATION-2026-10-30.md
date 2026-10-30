# FormidableAI foundation checkpoint — 2026-10-30

## Outcome

The first offline foundation now exists. It converts LWC's existing specialist-policy direction
into one shared arbitration model rather than a collection of independent controllers.

Evidence status: **CODE-TESTED**, not installed and not live-proven.

## Why this is stronger than timer-driven replacement loops

Timers may throttle observations or enforce cooldowns, but they do not decide alone. Current
Formidable decisions consume explicit facts about:

- authoritative owner and actor readiness;
- stable actor, target and attack identities;
- target authorization and combat commitment;
- strike activity, impact window, range and facing;
- stamina, equipment/capability and incapacitation;
- ally threat/health, support safety, firing lane, weapon range and recovery safety;
- target-isolated evidence and whether threat communication already occurred.

Every decision exposes an action, owner/domain, target, priority and reason. Unclaimed domains have
an explicit fallback. This is the `who / what / where / when / why / how` foundation Brian described.

## Reconciliation decisions

1. The kernel owns arbitration; class modules do not directly perform actions.
2. Class profiles describe capability and tactical priority, not separate brains.
3. `DefensePolicy` alone decides the exact block/dodge reaction. A Warden may express generic
   defense intent, but the exact strike policy has decisive priority and arbitration emits only one
   surviving Defense lease.
4. Actions that require coupled domains receive atomic leases. A class weapon action currently
   requires Attack + Facing; a movement intent requires Movement + Facing.
5. A module exception disables that module without preventing other proposals or native fallback.
6. A remote/non-owner frame cannot grant Formidable leases.
7. Native fallback is a migration and rollback mechanism, not the final architectural ceiling.

## Offline proof

- Kernel: 13 tests.
- Perception/defense: 10 tests.
- Six class profiles: 10 tests.
- Cross-layer integration: 6 tests.
- Read-only shadow telemetry: 8 tests.
- Existing LWC specialist baselines also remain green: 21 melee-role checks, 14 prediction/throw
  checks and 11 comparative ownership/perception/transaction checks.

New Formidable total: **47 passing checks**.

After adding reusable arbitration/proposal/winner/lease/fault buffers, an audit-host smoke benchmark
of 500,000 two-module evaluations measured 205.587 ms total with a 90,152-byte retained-memory delta.
Winner identities remained `warden` for Defense and `archer` for Attack. This coarse GC measurement
is not a Unity/frame benchmark, but it substantially reduces the allocation warning from the first
prototype. Live actor-count timing and allocation profiling remain required.

## What this does not prove

- No Unity component constructs a combat frame yet.
- No class action writes movement, facing, defense, attack or support in Valheim.
- No network-owner migration or dedicated-server behavior has run.
- No allocation/timing benchmark has run against live actor counts.
- No comparison against native action cadence has been observed in game.
- No native combat domain has been retired.

## Safe next sequence

1. Build a read-only `HumanFormidableObserver` that produces frame/decision telemetry in shadow mode.
2. Compare predicted target, defense, class action and ownership against native actions without
   changing them.
3. Benchmark 1/10/50/100 actors and enforce a bounded observation/arbitration budget.
4. Let Brian review trace plus video for reaction timing and class legibility.
5. Transfer the Defense domain first behind a disabled feature flag and immediate native rollback.
6. Only then evaluate Facing, Targeting, Movement and Attack transfers individually.

The accepted voice, spacing/right-of-way, identity, portal and persistence contracts remain
unchanged throughout this work.
