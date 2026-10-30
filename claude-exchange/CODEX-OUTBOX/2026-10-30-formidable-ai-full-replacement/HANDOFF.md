# FormidableAI destination clarification

Date: 2026-10-30

## Brian's decision

FormidableAI is intended to become Living World C.O.R.E.'s own **full replacement combat AI**.
The current hybrid architecture and native `MonsterAI` handoff are the migration platform, not the
final destination.

Brian's reasoning is that LWC already contains stronger ideas and increasingly owns the decisions
that make a simulated human believable: identity, class composition, perception, reaction timing,
defense, specialist actions, movement ownership, spacing, doors, equipment, support, telemetry and
encounter continuity. These systems need one coherent brain to make every surrounding world and
roleplay system work reliably.

## Architectural interpretation

“Full replacement” does not mean copying VikingNPC's monolithic override or replacing everything in
one speculative patch. It means FormidableAI ultimately owns the complete decision lifecycle:

`observe -> remember -> assess -> select intent -> arbitrate class capability -> plan -> move/face
-> defend/attack/support -> confirm action -> reassess/disengage -> recover`

The implementation should be modular internally but singular in authority externally:

- one actor brain;
- one immutable observation frame;
- one persistent-but-bounded memory model;
- one intent/plan at a time;
- one owner per action domain;
- one movement/facing writer;
- class capabilities selected through arbitration, not independent update loops;
- explicit confirmation of physical actions and their consequences;
- safe cancellation and handback;
- per-module fault containment.

Native `MonsterAI` remains available during migration as a fallback executor for domains not yet
transferred. It is retired only after FormidableAI owns every required domain and the complete
replacement passes offline, isolated-runtime and Brian live acceptance gates.

## Required replacement map

Claude's design should explicitly map and sequence these domains:

1. Observation and target-isolated perception.
2. Threat/ally memory, investigation and escalation.
3. Target legality, ranking, commitment and switching.
4. Intent and class-role arbitration.
5. Navigation request, spacing, right-of-way and formation-free natural group movement.
6. Combat positioning, range bands, line-of-fire and environmental constraints.
7. Facing/aim ownership.
8. Action-moment defense, dodge, parry and interruption.
9. Weapon/loadout/ability selection.
10. Attack/cast/support cadence and physical execution.
11. Cooperative roles without hive-mind synchronization or duplicated controllers.
12. Disengagement, pursuit limits, regrouping and return to peaceful activity.
13. Network authority, save/reload, ownership migration and dedicated-server continuity.
14. Diagnostics, performance budgets and module fault isolation.

## Acceptance rule

Each domain transfer needs an explicit old owner, new owner, transition gate, rollback, telemetry and
Brian-facing A/B scenario. A domain is not transferred merely because its new implementation
compiles. The retained native path remains available until the replacement is observably better and
does not regress the already accepted voice, spacing, identity or world contracts.

## Evidence state

- Direction: **USER-AUTHORIZED**.
- Existing hybrid and comparative mod audits: **SOURCE-INSPECTED**.
- Current clean-room ownership/perception/work/loadout/transition gates: **CODE-TESTED**.
- Full FormidableAI replacement: **NOT IMPLEMENTED / NOT INSTALLED / NOT LIVE-PROVEN**.
