# Next true build phase: FormidableAI initiative, defense and explicit friendly classes

Brian's direction is to stop treating the observer and raid plumbing as the product. The next candidate must begin replacing missing combat judgment in the real 1.0.x companions and improve the measured numbers. Build the friendly classes first; keep the now-working Souls raids as opposing pressure. Do not branch into a complete hostile lore roster yet.

## Start point and preservation boundary

- Start from the current canonical 1.0.32.29 actor line, including the live-proven appearance fix, A1 awareness, bombs, voices, heavies, partner defense and telemetry. Do not revive a stale A1-only checkout.
- Preserve combat-voice lock, native-write ownership gate, movement-writer gate, appearance behavior, bomb poison safety and all existing regression gates.
- Preserve one universal awareness service. No weapon or class behavior may search the world for its own excuse to act.
- Use feature switches with immediate native fallback, but this is an execution candidate, not another observer-only proposal.
- Brian receives one ordinary gameplay control through the Tooler or installed config. No developer commands or artificial defended-player state.

## Evidence that defines the work

From the 1,353-second full-roster session:

- Awareness envelope median 69.46 m; evidence 54.74 m; noticed 52.90 m; committed 50.94 m.
- Native companion target acquisition median 9.04 m; action median 7.89 m.
- Committed-to-response-ready median 3.90 s.
- Distant/flying targets held melee in 20% of 762 samples.
- Combat cohesion: mean 10.2 m, max 33.7 m, 18% over 16 m.
- Fighting with a nearby hostile but no target: 10%.
- Emergency fallbacks: 12; only 6 reached clear space, 6 were caught.
- Shields remained in hand in 55% of calm samples.
- Blocking was disabled, so the run contains no evidence that the current character can defend an incoming strike.
- Bombs: 61 throws, 9.2–19.8 m, median 13.6 m; the execution envelope works but native initiative still lets most contacts close before the opening response.
- Positive foundations to preserve: 99–100% focus on real threats, no wildlife initiation, zero walking away from a nearby engaged enemy, 46 partner-defense choices, safe heavy selection, no ownership or kernel faults.

## Build order on one branch

### A2.1 — Initiative and posture become real control

Convert the shared awareness frame into prompt, class-aware response proposals:

1. `Noticed`: orient/look/call out and update formation without drawing weapons solely from curiosity.
2. `Alerted`: establish readiness unless stealth doctrine still has a fair hold.
3. `Committed`: choose the class response immediately—ranged opening, form/hold/intercept, reposition for a lane, cover a partner, or withdraw. Commitment must not wait for native target acquisition.
4. All proposals include who, what, when, where, why and how: contact/group identity, evidence, range/closing/time-to-contact, intended victim, class/loadout capability, safety, stamina and current ownership.

The arbiter, not a timer, chooses. Timers remain only for physical cooldowns, animation windows and bounded resampling.

### A2.2 — Explicit character classes and weapon doctrine

Stop inferring Huntress from “has bow” and everyone else as Warden. Add an explicit profile/class identity connected to the character roll and loadout:

- **Warden:** shield/one-hand competence, hold corridor, intercept, protect partner, controlled advance, block/parry preference.
- **Huntress:** early bow acquisition, safe firing lane, kite/restore distance, ranged area-control opportunity.
- **Scout:** high awareness/mobility, spear or knife doctrine, flank/disrupt/reconnoiter, rapid disengagement.
- **Legion/heavy:** two-handed/heavy weapon doctrine, stagger exploitation, area denial, absorb/parry/counter according to equipment.

The character profile owns starting skills, biome training, armor, food, equipment and learned knowledge. The tactical core reads those facts; it does not invent capabilities the loadout lacks.

### A2.3 — Formidable Defense owns the first execution handoff

Blocking is required, but no periodic “block every N seconds” behavior is acceptable. Build incoming-strike evidence and one defense proposal per threat window:

- attacker and intended victim;
- attack start, predicted impact window, reach/arc and damage type when fairly knowable;
- equipped shield/weapon, block power, stamina, stagger state and escape lanes;
- partner position, poison/status field and competing threats.

The arbiter chooses `Block`, `Parry`, `Dodge`, `Evade`, `Interrupt`, `Counter` or deliberate `Absorb`. Raise block shortly before impact, hold through the threat window, and release afterward. Warden, Huntress, Scout and heavy doctrine weight those choices differently. Emergency defense outranks attack; it does not erase navigation or friendly safety.

### A2.4 — Cohesion, target continuity and fallback

- Class-aware formation keeps the pair mutually supportable without collapsing ranged spacing.
- A committed hostile already represented in the shared frame must not disappear between native target transitions.
- Fallback selects a viable lane and cover/partner relationship before moving; it must not be a blind backward timer.
- Do not force regroup while a partner is actively attacked.

## Arbitration priority

Use a clear priority order, with domain claims kept explicit:

1. Imminent personal defense / escape lethal field
2. Save or cover an endangered partner
3. Safe opening area control against a committed group
4. Class-appropriate attack or interrupt
5. Reposition for range, lane, formation or interception
6. Observe/readiness/calm behavior

Re-evaluate on meaningful evidence changes plus the bounded kernel heartbeat. Do not allocate new strings/objects every frame in release behavior.

## Acceptance targets for the next ordinary raid session

These are directionally strict without demanding cheating or zero deaths:

- Committed-to-response-ready median: **≤ 0.75 s** (from 3.90 s).
- Awareness-late native target cases: **no regression above the current three**, median lag no worse than 0.76 s.
- Huntress/archer ranged target acquisition reported separately from melee; committed ranged contacts should no longer wait for the 9 m native median.
- Melee held against distant/flying targets: **≤ 5%** (from 20%).
- Fighting with hostile nearby but no target: **≤ 5%** (from 10%).
- Combat samples over 16 m party separation: **≤ 10%** (from 18%), without destroying trained ranged spacing.
- Fallback clear outcome: **≥ 75%** (from 50%).
- Shield in calm: **≤ 5%** unless an explicit readiness threat justifies it (from 55%).
- Defense telemetry must report opportunities, selected response, timing, stamina, outcome and damage avoided/taken. No periodic block cadence should appear.
- Bombs remain friendly-safe and poison-aware; for committed multi-member opening groups, prefer the outer usable envelope and report why a closer release was chosen.
- Zero ownership violations, kernel/behavior faults, movement-writer violations or combat-voice changes.

Survival is evaluated relative to biome, gear and class training. Do not satisfy it through immunity, hidden omniscience, health inflation or scripted victory.

## Packaging boundary

Create only the interface/profile boundary needed for a future `Living World Hostile Peoples` content pack. The future hostile roster must consume this exact core through faction/class/doctrine profiles. It must not fork awareness, arbitration or defense into a second implementation. Third-party Souls/RRR assets remain optional compatibility content unless redistribution permission exists.

## Requested delivery

Build and push one reviewable candidate branch with:

- ownership map for every new write;
- class/profile mapping and fallback behavior;
- pure tests for arbitration, strike windows and class doctrine;
- analyzer support for the new acceptance metrics;
- complete build/gate output;
- a concise presentation of what now controls the companion and what still remains native.

Do not ask Brian another design questionnaire before beginning. Where a value needs tuning, choose a conservative explicit starting value, expose it in telemetry/config, and mark it as a test parameter rather than blocking the build.
