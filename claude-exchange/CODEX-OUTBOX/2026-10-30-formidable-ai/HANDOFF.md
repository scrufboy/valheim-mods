# FormidableAI architecture directive

Date: 2026-10-30

## Intent

Brian has made FormidableAI the immediate foundation for Living World C.O.R.E. The simulated human
cannot remain only a generic native combatant with peripheral class effects. LWC must manage
distinct combat classes whose perception, defensive reactions, positioning, weapon behavior,
support choices and specialist actions feel independently authored.

This is an expansion of the current hybrid architecture, not authorization to stack multiple AI
loops. FormidableAI must be the single class-aware arbitration layer above the actor's physical
combat executor.

## Evidence

- **SOURCE-INSPECTED:** VikingNPC replaces `MonsterAI.UpdateAI` with one monolithic ordered loop
  controlling target search, follow, work, flee, circle, block, dodge, move, face, weapon choice and
  attack. This proves the scope of decisions a replacement brain can expose, but its random polling,
  spacing, authority and fault-containment behavior are not suitable for LWC.
- **SOURCE-INSPECTED:** Current LWC `HumanCompanion.BeforeUpdate` is hybrid. It suppresses native
  updates while the companion cannot act or a bounded specialist owns an action, but normally lets
  native `MonsterAI.UpdateAI` execute committed combat.
- **SOURCE-INSPECTED:** Current LWC already owns or constrains important decisions: target legality,
  encounter bounds, combat handoff/clear, peaceful movement, shield reaction, cleric casting,
  specialist reservations, equipment permissions, doors and spacing.
- **CODE-TESTED:** Comparative clean-room gates cover one movement owner, native combat handoff,
  coherent loadouts, target-isolated/time-based perception, transactional work and transitions.
- **LIVE-PROVEN:** Existing accepted baselines demonstrate that native combat can execute the
  physical fight and that Brian can distinguish cadence/ownership failures through telemetry.
- **UNKNOWN:** Which class decisions should be replaced first, and where native behavior remains
  experientially superior, require explicit isolation and live A/B acceptance.

## Required architecture

FormidableAI should separate four layers:

1. **Observation:** immutable combat frame containing identity, class, native target, authorized
   target, threats, weapon/loadout, health/stamina/eitr, incoming action cues, allies, geometry,
   order/anchor, ownership and current native phase.
2. **Class policies:** pure deterministic modules for decisions such as defend, dodge, reposition,
   support, cast, select bounded specialist action, or yield. Modules cannot directly move, turn,
   attack, patch global AI or mutate ZDO state.
3. **Arbitration:** exactly one winning action owner/domain lease per actor. Arbitration resolves
   class priority, cooldown, safety, authority, target legality and conflicts with native combat.
4. **Execution adapters:** explicit narrow adapters. A decision either yields to native combat,
   configures a supported native input, or temporarily reserves one bounded action with start,
   confirmation, completion/cancel and telemetry.

Native `MonsterAI` remains the default/fallback executor. FormidableAI may progressively replace a
specific decision only after deterministic tests and Brian's live comparison prove the replacement.
There must never be two movement, facing or attack-cadence owners in the same frame.

## Initial class capability targets

- **Shield/warden:** action-moment block, protect/interpose intent, bounded hold/reposition.
- **Archer/huntress:** range-band intent, firing-lane safety, ammunition awareness and controlled
  reposition without perpetual retreat.
- **Cleric/mystic:** heal/support selection, cast reservation, self-defense interruption and clean
  handback to native combat.
- **Spear/skirmisher:** spacing-aware reach use, opportunistic throw reservation and recovery.
- **Heavy/commander:** threat anchoring, deliberate pressure and ally protection without autonomous
  encounter solving.
- **Pathfinder/scout:** awareness, investigation and threat communication before combat, followed by
  strict yield when combat commits.

These are capability compositions, not rigid prefab subclasses. One actor has one identity and one
authoritative action owner while carrying several compatible capabilities.

## Non-negotiable contracts

- Preserve locked combat voice 1.0.32.6 and accepted 1.0.32.8b spacing/right-of-way behavior.
- One actor, one identity, one action owner and one movement writer.
- No global BaseAI/Pathfinding replacement.
- No class module may silently call `MoveTo`, `StopMoving`, `LookAt`, `StartAttack`, transform/ZDO
  relocation or private combat methods.
- No class decision persists as combat truth across reload/ownership migration.
- Every bounded action exposes reason, owner, target, start, confirmation, completion/cancel and
  handback in telemetry.
- A module fault disables that module/actor safely; it cannot stop all NPC combat.
- Brian's live observation is final acceptance; compilation or synthetic tests are not completion.

## First implementation slice

Do not start with a full replacement `UpdateAI`. Extract the existing shield and cleric decisions
into a shared Formidable combat-frame/arbitration contract, add a no-op/native-yield class module,
and prove ownership invariants. Then add one class behavior at a time behind feature flags, beginning
with action-moment defense because LWC already has evidence and telemetry for it.

## Remaining unknowns

- Whether FormidableAI should eventually own target ranking or continue to adopt native acquisition.
- Which movement intents can be expressed through the accepted LWC movement writer without harming
  spacing cadence.
- Which native weapon/range decisions require replacement for each class.
- Exact priority semantics when defense, support, specialist and native attack opportunities overlap.
- Performance budgets at 1/10/50/100 actors and dedicated-server ownership migration.
