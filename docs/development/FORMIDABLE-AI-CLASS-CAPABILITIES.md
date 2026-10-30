# FormidableAI class capability architecture

Status: pure policy candidate, offline-tested only. No runtime adapter or profile change.

## Principle

A class is a stable composition of capabilities, preferences and limits. It is not a separate
`UpdateAI` loop. `ClassProposalModule` adapts class facts into the shared kernel's `CombatProposal`
and `DecisionDomain` contracts; there is no second class-specific domain model. The kernel grants
at most one owner for each action domain and one final actor plan.

The class policy never calls movement, facing, attack, transform, ZDO or private game methods.
Proposals describe intent, atomic domains and priority. `LastDecision` preserves the reason for
telemetry. The future executor reports start, commit confirmation, recovery or cancellation facts.

## Action lifecycle

`Idle -> Started -> Confirmed -> Recovering -> Idle`, with `Cancelled` as fail-closed handback.

- Started actions retain their exact domains while awaiting physical commit confirmation.
- Confirmed actions retain those domains until recovery completes.
- Target loss or authorization failure emits no proposal, restoring kernel fallback.
- Personal threat cancels support instead of letting a cast blindly own the actor.
- Invalid lifecycle facts fail toward yield rather than guessed continuation.

## Initial classes

| Class | Capabilities | Character-defining decision | Safety boundary |
|---|---|---|---|
| Warden | shield defense, protect ally | Observed strike defense; protect vulnerable ally | Defense preempts movement; no blind interpose |
| Huntress | ranged pressure, firing lane | Maintain a safe useful lane and fire only when clear | Ally in lane/blocked lane requests reposition, never a shot |
| Mystic | heal and support cast | Aid a critical ally in a safe cast window | Personal threat preempts support; cast requires readiness |
| Skirmisher | reach control, bounded throw | Use reach band or reserve a recoverable throw | No throw through ally or without safe recovery |
| Commander | threat anchor, command support, protect ally | Protect allies while applying deliberate pressure | No unbounded chase or autonomous encounter solving |
| Pathfinder | investigation, threat communication, ranged pressure | Investigate target-isolated evidence before commitment | Investigation becomes impossible once combat commits |

## Shared priority model

1. Authority and actor readiness gate every request.
2. Imminent personal defense outranks support, movement and attack.
3. A started action retains its atomic domains through commit/cancel/recovery.
4. Critical safe support may outrank ordinary attack.
5. Ally safety and firing-lane safety outrank damage opportunity.
6. Position recovery outranks an unsafe weapon action.
7. Weapon action occurs only inside the class's valid range/lane/recovery window.
8. Investigation/social warning exists only before committed combat.

Numeric priorities in the pure policy are evidence for arbitration order, not execution permission.
The kernel must still verify domain availability, target identity, cooldown and action confirmation.

## Required frame inputs

- owner/actor readiness and committed-combat state;
- authorized target identity and current action phase;
- incoming strike cue and defense equipment readiness;
- personal and ally threat facts;
- ally critical state and support safety;
- below/in/above weapon range band, firing lane and ally obstruction;
- reach/throw readiness and recovery safety;
- target-isolated investigation evidence and communication status;
- current action identity/domains, lifecycle phase, physical commit and recovery completion.

Every fact must come from an owner-authoritative observation adapter. Absence is false/unknown and
must fail toward yield; class policies may not query the scene independently.

## Conflict examples

- Warden receives an incoming strike while an ally is threatened: defend now, reconsider protect on
  the next confirmed frame.
- Mystic sees a critical ally while personally threatened: yield support so defense/native fallback
  can own the actor.
- Huntress has a target but an ally crosses the shot: reposition intent, not attack.
- Pathfinder has suspicious evidence and then combat commits: discard investigation ownership and
  use the combat policy immediately.
- Skirmisher can throw but recovery is unsafe: retain spear and use reach or reposition.

## Brian-facing acceptance

- Warden blocks at the visible strike moment and does not oscillate between ally and attacker.
- Huntress moves for a reason the overlay can name, then holds a believable range without permanent
  retreat or firing through allies.
- Mystic begins a heal only when safe, cancels immediately for personal danger and cleanly resumes
  normal combat ownership.
- Skirmisher throws rarely and purposefully, with visible recovery and no phantom duplicate weapon.
- Commander feels deliberate and protective without commanding other actors through a second brain.
- Pathfinder visibly notices/investigates before combat and never keeps searching while attacked.

Telemetry must show class, capability, requested action/domain, priority, target, reason, winning
owner, rejection reason, start/confirmation/cancel/completion and next eligible time.

## Missing runtime proof

These policies do not yet prove geometry, latency, multiplayer ownership, animation timing, actual
movement, attack execution or class believability. Integration must remain feature-flagged until the
kernel tests, isolated runtime gates and Brian's A/B observation pass.
