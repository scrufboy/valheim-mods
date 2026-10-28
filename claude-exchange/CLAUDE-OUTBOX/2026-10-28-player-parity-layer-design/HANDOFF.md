# Player-parity interior layer — design (Claude, 2026-10-28)

Status: **DESIGN, not built.** Brian asked for this to be written now, as one layer, instead of recursive per-feature
patching (2026-10-28 17:39: "it would be best to write it that way now... instead of later... we divide here").
Brian will share this exchange with ChatGPT so both agents work from the same plan.

## Brian's intent (his words, condensed)
- A special interior mod "that we build to allow things like this [statuses] and skills to be accounted for... part of
  how our brain will work."
- "A real Player set of things." Companions get player status mimicry / equated values, so player features work for them
  too ("even colored map markers").
- Combat needs a little brain: bombs, spears, lunges, atgeir swings, blunt heavy timing, jumping, area magic all need
  leading and aiming. "A predictive cheat model that only allows what naturally would befall a player." A chance to hit,
  not a character always missing.
- Dual-purpose attacks: thinning the incoming herd/horde/pack, hurting, slowing, giving the fast fighters a target to
  dispatch. Combat now resembles Souls combat, which is the goal.
- The test "might need serious scrutiny."

## Why one layer (evidence)
- SOURCE-INSPECTED: weather Cold/Freezing, Shelter, CampFire, Resting are written only in
  `Player.UpdateEnvStatusEffects`. Companions are non-Player Humanoids (`NpcFactory` copies the Player prefab, then
  removes `Player` and `Skills`), so they never receive them. Rain Wet is already patched separately (`NpcWetStatus`).
- SOURCE-INSPECTED: companions have **no skills**. `Character.GetSkillFactor` returns 0 for non-Players. The native
  attack code then applies the worst player values:
  - `Attack` skill-accuracy projectiles lerp to minimum accuracy;
  - bow draw time is the slowest (`Humanoid.GetAttackDrawPercentage`);
  - block power is lowest (`Humanoid.BlockAttack`);
  - attack stamina, eitr and health costs get no skill reduction.
  Part of "always missing" is this, not aim.
- SOURCE-INSPECTED: the native skill-raise calls already fire on companions and are discarded. `Attack` raises on hit
  (`m_raiseSkillAmount`), `Humanoid` raises Blocking, `Character` raises Jump. Base `Character.RaiseSkill` ignores them
  unless tamed. The Player-only raise sites (Run, Swim, Sneak, Dodge) are inside `Player`.
- SOURCE-INSPECTED: minimap player pins come from `ZNet.GetOtherPublicPlayers` (real network players only).
  Companions would need their own pin source; colors and pin types are UNKNOWN until inspected.
- Each feature so far grew its own small mirror: wet status, heavy stamina pool, bomb planner, body-voice triggers.
  The layer gives them one owner.

## The layer: `Parity` (working name, inside the 1.0.x actor line)
One per companion, owner-side, saved in the companion's ZDO. Everything else reads from it. It writes nothing outside
its own domain except native status effects (through SEMan, as the wet patch does today) and the skill answers below.

### 1. Status parity
Run the Player environment rule for each companion at its own position: Cold/Freezing (with frost resistance, warm
areas, shelter, fire), Shelter, CampFire, Resting, Wet from rain (absorbs `NpcWetStatus`). Rested comes later,
together with needs and sleep. The native status effects then do the gameplay, so values match a player in the same spot.
Consumers: body voice (the installed `frost` clips can react to weather cold), and the brain (seek fire or shelter later).

### 2. Skills
A per-companion skill record using the native `Skills.SkillDef` steps and level curve, saved with the person.
- Answer the game's own skill questions for companions only: `GetSkillFactor`, `GetRandomSkillFactor`, `RaiseSkill`.
- Gains come from the native raise calls that already fire (hit, block, jump). Add player-equivalent gains for Run,
  Swim, Sneak and Dodge only where the companion really does the action.
- Starting levels come from the person's identity/class (a Plains huntress starts skilled with spears and bows).
  Starting values are Brian's decision.
- Effect: native accuracy, draw speed, block power and costs improve with skill, exactly as for a player.
- Brian's rule stays: stamina never gates fighting. Skills lower costs; they never add a stamina gate.

### 3. One aiming model ("predictive, but only what a player could do")
Generalize the 1.0.32.12 bomb planner (`ThrowPlanner`) into one shared model:
- **Inputs a player could perceive:** target positions and velocities, the clustering of a group, terrain line,
  friends' positions. No future knowledge beyond extrapolating the motion it can see; no seeing through walls.
- **Output:** aim point plus a timing window (when to release or commit), and a chance to hit.
- **Error:** spread and lead error shrink with the companion's skill in that weapon, on top of the native skill
  accuracy. A novice misses more; an expert leads well. Never a guaranteed hit, never a constant miss.
- **Users, in order:** bombs (move onto the model, same behavior), spear throw, lunges and atgeir sweeps
  (commit timing and reach), blunt heavies (timing on stagger or recovery windows), area magic (lead and cluster).
- **Friend safety** stays a hard rule for every user: never toward the leader or a companion, now or predicted.

### 4. Attack purpose
Each attack the brain chooses carries a purpose: thin a group, slow it, wound, stagger, set up a kill for the fast
fighter, or finish a target. The brain scores options by purpose and party role, so two companions combine (one bombs
the pack, the other dispatches the slowed one). This is the "dual purpose" Brian described.

### 5. Player-style outputs
These are built on the layer after it exists: map pins for companions (own pin source), HUD/status readouts, and other
player features that ask "is this a player?". Each one is inspected before it is promised.

## Build order (each stage live-tested before the next)
1. Status parity (weather cold/freezing, shelter, fire, resting; wet moved in). The frost voice reacts to weather.
2. Skills record + native skill answers + gains. Telemetry shows levels and per-weapon hit rate.
3. Bombs moved onto the shared aiming model with no behavior change (regression ledger proves the same plans).
4. Spear throw on the model.
5. Lunges, atgeir, blunt heavy timing, area magic.
6. Attack purpose scoring.
7. Map pins and other player-style outputs.

## Protected (never changed by this work)
- Combat voice 1.0.32.6 (locked) and the poison body voice and its 40 clips.
- Brian's hard rules: never throw at the leader or a companion; companions never push the player; stamina never gates
  fighting.
- Native combat stays native: the layer answers the game's questions; it does not replace attacks, damage or AI.

## Test scrutiny (Brian: "might need serious scrutiny")
- Offline: pure rules for status, skills and aim in the regression ledger, including "same plan as before" for bombs.
- Live per stage: telemetry lines for status on/off with cause, skill level changes, aim (chosen point, predicted vs
  actual landing, hit/miss) and refusals, so a session can be scored, not just watched.
- Labels: SOURCE-INSPECTED / OFFLINE-TESTED / INSTALLED / LIVE-PROVEN only as earned.

## Open for Brian
- Starting skill levels per class/biome, and the skill gain rate (native rate or a multiplier).
- Whether companions should also need fire and shelter (behavior), or only feel it (status and voice) at first.
- Map pin look (color, icon, name) once inspected.
