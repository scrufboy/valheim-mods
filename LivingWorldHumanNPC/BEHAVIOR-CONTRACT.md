# Living World Human NPC behavior contract

This document is the governing behavior plan for the modular human NPC rebuild. A feature is not complete merely because it appears once. It is complete only when its owner, entry conditions, exit conditions, telemetry, and regression test are defined and passing.

## Non-negotiable combat rule

Once a hostile encounter is committed, the companion enters **full native combat**.

`MonsterAI` and the native `Humanoid` exclusively own target acquisition, target facing, weapon selection, pursuit, circling, attacks, blocking, hit reactions, stagger, and damage until the encounter reaches verified clear.

The following systems are prohibited from issuing instructions during full combat:

- formation and ordinary follow positioning;
- settled guard movement and sector scanning;
- leader-facing and companion mimicry;
- torches and other calm presentation choices;
- crouch mimicry;
- chairs, beds, workstations, conversations, and other ambient actions;
- custom attack timing, custom facing, or substitute target selection.

The companion controller may enforce only the party's outer battlefield boundary. Inside that boundary, combat remains fully native.

## One arbiter, one writer per domain

The behavior arbiter selects a phase and records why. It does not perform movement, equipment changes, attacks, or interactions itself.

| Domain | Sole owner | Rule |
| --- | --- | --- |
| Behavior phase | State arbiter | The only component permitted to change the active phase. |
| Committed combat | Native `MonsterAI`/`Humanoid` | No companion module may compete with its target, facing, movement, or attack decisions. |
| Calm traversal | Traversal module | Supplies one native follow destination and pace; it never attacks or equips. |
| Formation and guard positions | Lease services | Offer unoccupied destinations; they never push, teleport, or directly steer an actor. |
| Hands, back slots, and torch | Equipment module | Reconciles one atomic visual posture; no other module equips or stows. |
| Attention and look direction | Awareness module | May look at an observed subject only outside committed combat. |
| Doors | Door traversal service | Assists the active path; it does not choose another behavioral phase. |
| Chairs, beds, and workstations | Interaction service | Runs only when the current phase and location allow an ambient action. |
| Wetness and other status effects | Native status-effect system | Represents the actor's actual exposure, not imitation of the leader. |

Every transition performs its entry or exit transaction once. No module may repeatedly equip, release, reacquire, or cancel the same instruction every update tick.

## Governing phases

### 1. Awaiting identity or leader

The actor completes native network identity, randomized sex, hair, male beard, name, inventory, and role before behavior begins. A missing dependency retains the current state; it never silently falls through to a different behavior.

`lwparty` remains a laboratory transaction that replaces the current test party with exactly two fresh actors. It is not the future world-discovery rule for companions.

### 2. Calm companion

The companion travels with the leader using native pathfinding. Either companion may lease any suitable unoccupied rear or side position. Roles do not permanently own left or right positions.

- Walking matches walking; sprinting matches sprinting; catch-up is faster but bounded.
- Companions must not path through or push the leader or one another.
- Native pathfinding handles terrain and obstacles.
- When the leader stops, leases may settle and then release into guard/ambient behavior instead of causing constant repositioning after every small turn.
- Menus, the map, sitting, and workstation use shorten the delay before settled behavior because they are strong evidence that the leader has stopped to do something.

### 3. Awareness and curiosity

Awareness observes the native world without starting an artificial combat loop.

- A detected passive, uncertain, or not-yet-committed subject may receive native-style attention, facing, and a question/alert indicator.
- Companions dynamically cover different sectors by checking what their teammates are already observing. No actor owns a permanent compass direction.
- Night torches remain valid during ordinary attention and curiosity. Merely noticing something must not flicker a torch off.
- Leader crouch requests stealth outside combat. It does not force crouching after combat has committed.
- In stealth, companions observe and warn but do not initiate against an uncommitted target. Being attacked or receiving a committed hostile target overrides stealth and enters full combat.

### 4. Combat readiness

Readiness is not combat. It is the short posture before a confirmed engagement.

- Drawing a real weapon by the leader may place companions in readiness and draw their weapons.
- A clearly imminent hostile commitment may also enter readiness.
- Curiosity alone does not enter readiness.
- Readiness may face the probable threat, but it may not issue attacks or pursuit.
- The instant native AI owns a valid hostile target, readiness yields to full native combat.

### 5. Full native combat

The formation lease, ambient destination, leader look, calm torch posture, and interaction claims are released on entry. The native combatant then does the job without companion logic interrupting it.

Awareness distance and pursuit distance are different. A companion may see a distant threat and an archer may shoot at native range without a melee actor pursuing indefinitely.

The party-cohesion shell uses hysteresis:

1. Inside the operating area, it does nothing.
2. Crossing the hard separation boundary for a sustained interval ends that out-of-area pursuit once using Valheim's complete native encounter-release transaction.
3. The actor enters a latched combat-regroup return and cannot reacquire the same out-of-area pursuit every decision tick.
4. The latch clears only after the actor reaches the inner return area. An enemy that follows the actor back into the operating area can then be fought in full native combat.
5. No teleport, forced facing, custom attack, or formation movement is permitted inside full combat.

Exact boundary numbers are acceptance parameters, not scattered constants. The first calibrated target is to keep the party close enough that two companions do not exceed 16 metres of pair separation for more than a brief obstacle-driven exception.

### 6. Verified combat clear

Losing one target does not immediately end combat.

Combat-clear requires five uninterrupted seconds with all of the following true:

- no valid native creature or static target;
- no active encounter ownership;
- no incoming damage or fresh hostile commitment;
- no alerted hostile detected by the scheduled awareness sweeps;
- two completed full-area verification sweeps.

Any contrary evidence resets the clear interval and returns directly to full native combat. Weapons remain ready and torches remain prohibited during verification.

### 7. Regroup and return to calm

After verified clear, actors use native traversal to gather near the leader without trying to occupy the same point. When each actor reaches an available lease:

- combat posture ends;
- last-used weapons stow visibly on the back;
- the appropriate calm, traversal, hold, or town behavior resumes;
- a torch may return if the independent light rule calls for it.

## Presentation rules

### Weapons

Visible hands and visible back slots are one atomic posture. The same native last-used equipment moves between them. A stale sword, bow, shield, or torch may not remain visible in a second slot.

### Torches

Torch need is derived from darkness, nearby practical illumination, indoor/town context, and phase—not from a single time-of-day edge.

- Valid in calm traversal, settled guard, and curiosity.
- Prohibited from full combat through verified combat clear.
- Returned only after clear succeeds and the darkness/light hysteresis still requests one.
- Minimum hold and release intervals prevent flickering near light boundaries.
- Equipment arbitration guarantees that a torch cannot coexist with an impossible weapon posture.

### Wetness and swimming

Rain and water apply the native wet effect, visual dripping, timer, and penalties to the actual exposed actor. Entering water stows incompatible equipment. Leaving the water restores the posture required by the current phase; it does not blindly restore an obsolete pre-swim pose.

## Doors and path continuity

- A closed door on the actor's current valid route is opened; the actor does not abandon it merely because another door is already open elsewhere.
- Calm traversal may close a door after the party has cleared it, with one temporary door lease preventing multiple actors from fighting over it.
- Combat may open a required door. It leaves the door open by default.
- Combat closing is reserved for an explicit future retreat, hold, or building-defense tactic; it is not ordinary combat housekeeping.
- A failed doorway records the obstruction and retries through native pathfinding without entering an endless open/close or route-switch loop.

## Location and command contexts

Location changes the available calm actions; it does not replace the combat system.

- **Field companion:** travel, awareness, guard, stealth, combat, clear, and regroup.
- **Town companion:** respectful traversal and stowed posture, followed by chairs, beds, workstations, conversations, and resident actions when released by the leader.
- **Hold/stay:** remain inside a bounded area while guarding and performing allowed local ambient actions; it does not mean freezing permanently in place.
- **Hunt:** a future explicit player command that authorizes proactive attacks on normally passive nearby creatures. It must never be smuggled into ordinary companion mode.

Town context is determined by the town boundary/anchor, such as the maypole system. Companion status remains distinct from town residency, so entering town does not destroy the companion relationship.

## Development order

1. Preserve the accepted spawn, identity, hair/beard, two-member lab transaction, native following, visible weapon stow, and atomic visual-slot milestones.
2. Correct combat cohesion using the outer shell described above while leaving full native combat untouched.
3. Prove combat entry, sustained fighting, survival, verified clear, and regroup in one continuous test.
4. Restore awareness/readiness indicators and stealth rules without adding a combat writer.
5. Restore torch arbitration and validate darkness hysteresis through curiosity, commitment, clear, and regroup.
6. Validate wetness, swimming, and state-correct re-equipping.
7. Add door continuity.
8. Add town/hold interactions one service at a time: chairs, beds, workstations, and later productive jobs.

No later step may modify an earlier module merely to make the new feature appear to work.

## Acceptance gates

The next raid build is not accepted unless all of these pass in the same run:

- exactly two complete, visibly randomized companions remain present;
- both enter full native combat and face their actual targets rather than the leader;
- no calm, torch, formation, mimicry, or interaction command executes during full combat;
- both companions remain alive through the raid and at least 60 seconds of cleanup;
- party separation stays within the recorded cohesion limit except for a short pathfinding obstruction;
- an out-of-area target produces one latched release, not repeated release/reacquire churn;
- clear verification lasts the full required interval and resets on a renewed threat;
- both companions regroup and visibly stow their actual last-used equipment;
- all logical and persistent hand/back visual slots agree;
- telemetry drops zero records;
- the log contains no Living World exception and no `ZNetScene.RemoveObjects` failure.

Failed gates are recorded as failures. A visually interesting moment cannot override contradictory telemetry.
