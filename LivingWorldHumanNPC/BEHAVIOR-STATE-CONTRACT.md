# Living World human companion behavior-state contract

This document is the behavioral authority for the companion. A feature is not permitted to add its own independent opinion about whether the actor is in combat, peaceful, following, holding, or available for an animation.

## State model

The companion has one persistent order and one exclusive action owner.

### Persistent order

- **Resident**: lives and works in town. Resident schedules, dwelling, work, food, sleep, and town etiquette apply.
- **Follow**: travels as the player's companion. Formation, guard, awareness, and companion utility behavior apply when no higher action owns the actor.
- **Hold**: remains within the assigned area. It keeps awareness and self/party defense but does not resume follow formation until ordered.

Changing action state must not erase the persistent order. Combat entered from Follow returns to Follow; combat entered from Hold returns to Hold.

### Exclusive action owner, highest priority first

1. **Dead or incapacitated**
2. **Combat**
3. **Combat pursuit**
4. **Combat-clear regroup**
5. **Door crossing**
6. **Alert/readiness**
7. **Ordered movement or formation travel**
8. **Settled companion activity**
9. **Resident activity**

Only the current owner may issue movement, facing, hand-equipment, attack, block, or animation decisions. Lower states may observe facts but may not control the actor.

## Awareness progression

### Peaceful awareness

- The normal scan remains active while following or holding.
- A visible hostile that is not committed, alarmed, attacking, or otherwise an active threat may be watched.
- Looking and the awareness marker inform the player without authorizing an attack.
- Unprovoked wildlife, tame creatures, party members, and players are not combat targets.

### Leading-edge curiosity

- A hostile that has noticed, approached, become curious, or begun committing toward the party raises readiness.
- The companion may face it and adopt a useful flank-side guard position.
- Curiosity does not grant unlimited pursuit and does not run attack code.

### Combat readiness

- Readiness draws weapons and suspends torches, crafting, idle mimicry, ambient facing, and formation correction.
- Readiness exists only as the entry boundary to combat or a short, evidence-backed alert. It must never compete with active combat.

## Combat ownership

Combat is an exclusive mode.

- Native `MonsterAI` owns target approach, circling, facing, attack timing, block behavior, weapon execution, and locomotion.
- The Living World layer authorizes safe hostile targets, preserves party identity, enforces tactical bounds, and supplies the prepared role equipment.
- Follow movement, leader gaze, idle guard scans, ambient activities, posture mimicry, formation leases, torches, cleric casting, and peaceful door-closing cannot run while combat owns the actor.
- The companion does not poll the leader's combat animation to decide what to do. It responds to its own sensed and authorized threats and to attributed attacks on the party.
- A missed scan does not cancel a valid fight. Combat ends only after the current authorization ends and repeated clear scans confirm that no active hostile encounter remains.

### Combat pursuit

- Pursuit is part of combat, not companion follow.
- It remains bounded by the party/order anchor, combat origin, target validity, recent sensing, and recent threat evidence.
- A retired distant target is not automatically reacquired merely because it still exists.
- Doors may be opened for an already-authorized pursuit; peaceful close-behind etiquette does not interrupt that pursuit.

## Combat-clear regroup

- Regroup begins only after combat authorization is gone, encounter ownership has expired, and repeated scans have verified the area clear.
- Weapons stow through the native hand/back lifecycle.
- Companion movement resumes toward an available formation lease rather than a fixed named slot.
- Torches remain prohibited for five uninterrupted clear seconds, then require three seconds of sustained darkness before being equipped.
- Ambient work, guard wandering, sitting/crouching mimicry, and other peaceful activity cannot begin until regroup releases ownership.

## Follow formation

Formation is a set of available leases around the player, not permanent archer/cleric assignments.

- Preferred leases occupy the rear arc and left/right flank-side arcs.
- A companion may take any safe available lease and may reclaim a different one after combat, a door crossing, terrain separation, or independent movement releases the previous lease.
- No lease is directly in front of the moving player.
- Arrival must avoid overlap, pushing, and both companions competing for the same point.
- A small player turn while settled does not immediately force repositioning. Repositioning requires meaningful travel, obstruction, unsafe spacing, or a threat-facing need.
- Native pathfinding owns terrain, inclines, obstacles, and doors.

The flank-arc lease expansion and turn hysteresis are required future implementation. The torch correction must not alter the currently installed formation behavior.

## Settled companion behavior

When Follow is active and the party is settled:

- Guard, observe, and short believable meander activities may run after a quiet delay.
- Opening a map, inventory, build menu, workstation, or sitting is evidence that the player is settled and may shorten that delay.
- Guard/observe can coexist with carrying a torch.
- Actual workstation/crafting animation owns the hands and temporarily defers a torch.
- Party members use staggered timing so their animations and decisions do not look hive-minded.

## Utility torch

The torch is peaceful companion equipment, never combat equipment.

- Eligible during non-combat Follow and Hold when perceived rendered light is low and no nearby built light is sufficient.
- Actual perceived luminance controls the decision regardless of time-of-day labels.
- Blue night ambient must not be mistaken for daylight, and a fog value from another day phase must not suppress the current decision.
- Crouched leader stealth, combat ownership, weapon readiness, swimming/stagger/attack, cleric casting, or a crafting hand lease removes or defers it.
- Weather and mist alone do not prohibit useful light.
- It is removed immediately on combat ownership and cannot return until the verified clear and sustained-darkness delays complete.

## Environmental and presentation overlays

Wetness, dripping, stamina effects, food/health state, equipment-on-back visuals, and similar embodiment are overlays. They do not own movement or combat decisions.

- Wet conditions apply the real native wet status and visual presentation.
- Weapon stowing uses the native hide/show equipment lifecycle and remains visible on the back.
- An overlay may change stats or presentation only through its defined native mechanism; it may not create a second behavior state machine.

## Resident behavior

Resident mode is separate from companion Follow/Hold behavior.

- Dwelling, bed, workstation, food, repair, steward assignments, and town schedules belong to Resident mode.
- Town weapon etiquette may require stowing, but it cannot override active defense.
- Resident work and companion combat share identity and embodiment, not concurrent decision loops.

## Non-regression rule

Every new behavior must declare:

1. Which persistent orders permit it.
2. Which exclusive action state owns it.
3. Which higher state cancels it.
4. What evidence enters it.
5. What verified evidence exits it.
6. Which existing owners it is forbidden to control.

If those six answers are absent, the behavior is not added to runtime code.

