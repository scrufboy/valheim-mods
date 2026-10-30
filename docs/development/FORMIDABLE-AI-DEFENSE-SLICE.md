# FormidableAI vertical slice: perception and action-moment defense

Status: offline pure policy only. Not runtime-wired, installed or live-proven.

## Purpose

This slice proves that class-aware defense can be based on **who, what, where, when and why** rather
than a periodic random roll. It deliberately emits no movement position, direction or velocity.

## Inputs

Perception integrates a target identity, evidence rate, elapsed time and explicit domain ownership.
Switching target resets evidence rather than transferring suspicion from one person to another.

Defense receives an immutable strike observation:

- actor, threat and attack identities;
- whether Formidable owns the defense domain;
- target authorization and active-attack evidence;
- blockability, shield and dodge capability;
- incapacitation, stamina, distance and facing;
- current time and predicted time to impact.

## Deterministic ordering

1. Yield when Formidable does not own defense.
2. Reject unauthorized/non-attacks.
3. Reject a previously handled attack identity.
4. Enforce the reaction cooldown.
5. Yield while incapacitated.
6. Require the strike window, range and defensive arc.
7. Block a blockable strike when shield/stamina facts permit.
8. Otherwise dodge when capability/stamina permit.
9. Otherwise yield to the retained native path.

This is priority arbitration, not timer-driven behavior. Time is used only to describe the actual
strike window and bounded recovery after a confirmed reaction.

## Ownership boundary

`DefenseDecision` contains only action, reason, identities and next policy memory. It cannot move,
face or attack the actor. A later reviewed execution adapter must translate Block or Dodge into one
bounded physical action and confirm start/completion/cancel through the Formidable kernel.

## Gates proved offline

- 30/60/120 Hz perception yields the same stage;
- target switches cannot inherit evidence;
- unowned perception does not mutate state;
- block and dodge depend on observable strike facts;
- one attack identity triggers at most one reaction;
- cooldown prevents immediate reaction spam;
- missing ownership or incapacitation yields to native;
- the decision contract exposes no movement command.

## Still required

- adapter mapping from authoritative native animation/attack evidence to time-to-impact;
- attack identity generation stable for one physical attack;
- latency/remote-owner and dedicated-server tests;
- shield raise/dodge execution confirmation and cancellation;
- simultaneous attackers and priority selection;
- allocation/performance benchmarks;
- Brian A/B review for timing, believability, cadence and false reactions.
