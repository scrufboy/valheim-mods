# Append-only decisions

## 2026-10-17 — Non-regression authority

The user requires Claude and Codex to build Living World C.O.R.E. collaboratively without repeatedly removing working behavior. Working systems must not be erased or inverted while another subsystem is repaired.

## 2026-10-17 — Current sequencing

Finish and live-prove the Human NPC companion before beginning UnJohnny's Outsiders, Smoothbrain's Timed Item Destruction, or other postponed expansion work.

## 2026-10-17 — Shared GitHub exchange

The user authorized a GitHub handoff folder that Claude and Codex can both use. Submissions remain dated and append-only. Neither agent may silently replace the other's evidence or treat offline success as live proof.

## 2026-10-27 — Traversal before bombs and new classes (owner statement)

Brian: traversal must be finished first. That means live-testing party spacing
(walking around without bumping into the player or each other), then portals
and caves/dungeons: who may go through a portal, and active companions must
follow the player into troll caves and other dungeons ("I walked into a troll
cave and my guys aren't with me"). Then bombs. Then new classes such as the
Plains spear huntress. This settles the archive's open portal-versus-bombs
ordering question: portals and interiors come first.

## 2026-10-27 — One shared sync routine

Brian reported that keeping the ChatGPT and Claude chats current is getting
harder. Both agents follow `claude-exchange/SYNC.md`: pull and read at session
start, commit and push at session end, and stop before installing a build from
a different actor line.

## 2026-10-28 — Nomadic camps, release to camp, memorial token (owner statement)

Brian, as the safe starting version:

- **Nomadic camps.** A recruitable camp may despawn and later reappear somewhere
  in the same general area where it was first spawned. Returning after time, the
  player finds it near the old spot, possibly moved. The people in it are the same
  persisted people and remember the player. This is how the existing rule
  ("rejected or unresolved camps expire or migrate") is implemented first.
- **Release goes back the way they were found.** For now, Release from Party
  returns the companion to their nomadic origin camp. Homes, reclaimed villages
  and allied camps remain the later destinations in
  `COMPANION-PROGRESSION-ROSTER-AND-RELEASE.md`.
- **Pairs.** Brian: "If it's a single from a pair..." (sentence unfinished).
  Claude's reading, unconfirmed: when one of a camp's pair is recruited, the
  partner stays and the camp persists around them, and a released companion
  rejoins that partner. Needs Brian's confirmation.
- **The end is a token.** On a companion's permanent death the player receives a
  memorial token and chooses whether to place a tombstone. This is separate from
  the equipment-recovery transaction and is not a replacement or revival token.

## 2026-10-28 — Correction: "camp of one" and the found companion (owner idea, unresolved)

Supersedes Claude's unconfirmed "Pairs" reading in the previous entry; that
reading was wrong.

Brian: a companion released back to the wild forms a **camp of one**. When the
player later re-enters the general area, the camp (possibly moved) may hold a
**second person**: someone who found them. Lore question in the world: is there
another out there, and have they found each other? Brian marks this as an
unresolved idea with a quick code implementation.

Claude's proposed shape (DESIGN-AUTHORITY only after Brian accepts):

- On each camp respawn, a camp of one may gain one newcomer, decided
  deterministically from the camp id and respawn count (no reload rerolls).
- The newcomer is a new persisted person with a new identity and history,
  never a reroll of the existing one and never a dead companion returning.
- The newcomer's role complements the existing one (the biome class pair).
- At most one newcomer; a camp never grows past two through this rule.
- Open: does the newcomer count as a recruitment lead under the one-active-lead
  and five-day rules, and how long must the player be away first?

## 2026-10-28 — Body states and the player-parity layer (Brian)

- Brian approved building ChatGPT's body-reactions handoff ("ok Now you can build...."): 1.0.32.14 installed, live
  test and per-clip audition pending.
- Weather Cold/Freezing for companions is not to be patched on its own. Brian: the status piece belongs in "a special
  interior mod that we build to allow things like this and skills to be accounted for... part of how our brain will work."
- Brian: write that layer now, as one design, instead of later recursive and possibly regressive patching ("we divide
  here"). Scope in his words: "a real Player set of things", a predictive model "that only allows what naturally would
  befall a player", "a chance to hit, and not a character always missing", dual-purpose attacks.
- Design written by Claude (DESIGN, not built): `claude-exchange/CLAUDE-OUTBOX/2026-10-28-player-parity-layer-design`.
  Brian shares it with ChatGPT before code.

## 2026-10-28 18:14 — Player-parity owner decisions (Brian)

- **Starting skills:** ranged by where the companion is recruited, and "worked against the game level and loot control
  intel... how many days and how many bosses or which boss is defeated". So seed levels scale with world progression
  (days survived, which bosses are defeated), not with biome alone. ChatGPT's pilot ranges stay as calibration input.
  Open: what "loot control intel" means exactly (Claude reads it as the world's own difficulty/loot settings; not yet
  confirmed).
- **Cold has gameplay consequences, not only voice.** Brian: cold makes companions slow during drake raids, "so other
  effects matter if armor combats them too". The native effects apply, and armor resistance counters them as for a player.
  - Claude's evidence (SOURCE-INSPECTED, 1.0.21): the drake slow is the damage **Frost** status (`SE_Frost.ModifySpeed`).
    It already reaches companions natively.
  - Frost resistance is read through `Character.GetDamageModifiers`, which includes worn armor for companions through
    the existing `ArmorParity` patch. Not live-proven.
  - Weather Cold/Freezing (regen and health effects) need the parity status writer.
- **Death:** a dead companion is dead, and their skills go with them. If any restoration mechanic ever arises, "let
  Odin decide that", and a restored companion gets the same reduced skills a player has after death.
  - Native rule (SOURCE-INSPECTED): `Skills.OnDeath` lowers every skill by `m_DeathLowerFactor × Game.m_skillReductionRate`
    and resets the accumulator. The code default is 0.25; the Player prefab value is to be read at runtime. The world
    modifier `SkillReductionRate` applies.

## 2026-10-29 — Bomb cloud exposure direction (Brian)

- Bombs work as Brian wants for now; no change scheduled.
- Direction, not yet a build order: a **reduced effect** from one's own or the party's clouds, for the player class and
  non-player characters, together with **heightened awareness** of clouds (area, avoidance).
  - Hostile NPCs must be affected by poison, as we are by theirs.
  - Leaving a cloud is a tactical decision.
  - A party knows where its poison is; being pushed in by combat is the realistic exception.
- A test arena is needed for this. R&D record: `research/BOMBS-CLOUDS-AND-STATUS-EXPOSURE-RND.md`.

## 2026-10-30 — FormidableAI combat objective and survival (Brian)

- Every companion must always try to beat their opponents. Survival is part of competent combat,
  not an optional behavior or a reason to accept a wipe as successful AI.
- Tier and gear explain difficulty; they do not change the objective. Seekers may exceed a party's
  tier, but Plains-quality companions should contest Fulings more evenly than the 1.0.32.22 run did.
- Long-term survival has two cooperating owners: combat judgment now (defense, spacing, threat,
  disengagement, protection and target/action choice) and progression later (levels, armor and
  weapons). Lack of progression makes better combat more urgent, not less.
- Permanent death remains meaningful. FormidableAI must improve the odds honestly rather than grant
  invulnerability, hidden stat cheats or guaranteed victory.

## 2026-10-30 — What “Formidable” means: trained combat competence (Brian)

- Valheim is a survival game. Surviving combat by making the proper choice at the proper time is the
  core meaning of combat intelligence and therefore the core meaning of FormidableAI.
- Each NPC type must be trained for its actual weapons, combat role and home biome. A class name is
  not enough, and one generic attack policy with different labels is not a replacement AI.
- Every companion needs a shared survival foundation: read threats, preserve stamina and health,
  choose safe timing, avoid being surrounded, use cover/space, protect allies, disengage when the
  current exchange cannot be won, and re-engage when conditions improve.
- On top of that foundation, each type needs distinct doctrine:
  - weapon competence: reach, timing, recovery, stamina cost, block/parry/dodge options, projectile
    flight, firing lanes, minimum/maximum effective range and weapon switching;
  - biome competence: terrain, visibility, hazards, common enemy combinations, resistances,
    vulnerabilities and escape routes found in the biome where that type belongs;
  - enemy competence: recognize attack tells, dangerous windups, ranged pressure, area attacks,
    stagger opportunities, priority threats and when an enemy is baiting an unsafe chase;
  - party competence: know its position and responsibility relative to the other class and leader.
- Training means encoded and testable combat knowledge plus progression through lived experience;
  it does not mean omniscience. An NPC may begin with its class/biome fundamentals and learn or
  improve beyond them through skills, equipment and survival.
- FormidableAI is successful when the NPC's choices are legible as those of a trained survivor using
  that body, weapon, role and environment—not merely when its attack flag matches native AI.

## 2026-10-30 — Character rollup owns training and loadout (Brian)

- An NPC's combat training is part of the character rollup and weapon loadout, not a later AI label.
  The rollup is one coherent starting package: identity, origin biome, people/culture, gender
  expression, job/class, starting attributes, skills, armor, food state, weapons, ammunition,
  equipment and the doctrine for using them.
- Biome and job jointly determine plausible starting competence and kit. World progression (days,
  bosses and loot/difficulty state) scales the package so a newly found companion belongs in the
  current world rather than spawning as a fixed weak template.
- Weapon doctrine must be generated from the actual equipped loadout. If the loadout changes, the
  usable doctrine changes with it; an NPC cannot claim bow competence without a bow/arrows or use
  shield doctrine without an eligible shield/weapon pairing.
- People/culture and gender provide identity, appearance, voice, names and believable variation.
  They may influence culturally plausible equipment or background where established by Living World
  lore, but they do not impose arbitrary competence penalties or biological stereotypes.
- Starting skills and attributes have bounded individual variance so two companions of the same job
  are recognizable as that job without being clones. Progression then belongs to the persisted
  person and is never rerolled on reload, release or recruitment.
- Food is part of readiness and the starting stat budget, not a hidden unlimited pool. The rollup
  names what food effects or equivalent companion budget justify health and stamina.
- Explicit quality-of-life invariants belong in the package contract. Example: companions have an
  endless utility-torch supply (or equivalent non-depleting torch service) so routine light does not
  become inventory chores, while combat ammunition and special consumables follow their own stated
  balance rules.
- Character generation must emit one inspectable profile explaining why this NPC has these stats,
  skills, gear, food budget and combat doctrine. FormidableAI reads that profile; it does not infer a
  generic class from prefab name alone.

## 2026-10-30 — Living World C.O.R.E. is the FormidableAI character system (Brian)

- Living World C.O.R.E. and FormidableAI are not separate piles of features. The core owns the
  persistent person and the decision process; voice, combat, status, environment, social behavior,
  emotes, camps, spawning, equipment and progression are behavioral/capability modules attached
  through that core.
- The governing order is:
  1. **Who:** stable identity, origin, people/culture, gender expression, relationships, faction and history.
  2. **Where/with whom:** biome, terrain, hazards, party, bonds, camp/home and current social context.
  3. **What they can do:** body, stats, skills, food budget, equipment, weapon training, spells and supplies.
  4. **Why:** survival, party protection, hunger/needs, duty, hunting, travel, rest and other current goals.
  5. **What now:** one shared world frame and arbitrator choose a valid intent for the current mode.
  6. **How:** bounded action skills execute that intent with entry conditions, commitment, feedback,
     cancellation, recovery and domain ownership.
  7. **What changed:** memory, consequences, skill growth, equipment changes, injury and relationships persist.
  8. **How it is expressed:** voice, posture, emotes, animation and visible status expose the same internal state.
- Modules may propose facts or actions; they may not become independent brains, create parallel state,
  write native behavior without an owned domain, or bolt timers directly onto the actor.
- Scripted maneuvers are valid skills, not hacks, when selected for a reason. A kite, lunge, dash-stop,
  spear throw, aerial throw, circle, shield wall, herding move or bomb setup must declare prerequisites,
  tactical purpose, expected outcome, abort conditions, recovery and the domains it owns.
- Randomness selects among tactically valid choices and provides personal variation. It never makes an
  invalid action intelligent and never substitutes for perception or prediction.
- Mode limits the decision space. Peaceful/local-safe modes can permit hunting, gathering, social and
  rest needs; danger/readiness/combat modes suppress irrelevant goals and prioritize survival and duty.
- Biome doctrine is reusable knowledge attached to the rolled character, not a forked AI. A Mistlands-
  trained companion brings relevant terrain, hazard and enemy knowledge, but still needs suitable gear,
  party composition and judgment.
- Camps and spawning tools decide where persisted people enter the world and which plausible pairs or
  mixed parties live there. They do not own the person's brain. External mod techniques may be adapted
  behind Living World interfaces, but no external system becomes a second authority.
- A full party should support genuinely different play: martial defender, ranged hunter, scout,
  magic user, healer/cleric, heavy weapon specialist and future roles. Sex/gender pairing is not a
  class constraint; pairs may be any believable combination of persisted people.
- The immediate core must be protected before expanding maneuvers: real character profile, real facts,
  arbitration, lifecycle, survival/team reasoning and weapon doctrine first; spectacular class routines
  plug in afterward without rewriting those foundations.

## 2026-10-30 — Fair information, attempted actions and continuous combat replanning (Brian)

- FormidableAI may present a trained NPC with an opportunity to respond; it may not grant the outcome.
  Closing a gap, reaching a partner, holding a corridor, pushing through pressure, blocking, dodging,
  striking and withdrawing must still succeed or fail through Valheim's native movement, collision,
  stamina, animation, weapon, damage, status and timing rules.
- The AI does not cheat merely by reading facts the game already knows, provided the character has a
  fair perception or learned-knowledge basis for using them. It must not use unseen enemies, future
  random outcomes, impossible paths, private state with no perceptible consequence, or perfect attack
  knowledge beyond the character's training and observed tells.
- Combat is a continuing sense-predict-choose-attempt-observe loop. An action is not considered complete
  because it was requested. The system observes navigation, obstruction, interruption, damage, target
  change, partner state, stamina, commitment and recovery, then continues, adapts or hands back.
- Immediate danger may interrupt a plan: an attacker engaging the NPC can become the local priority;
  the NPC may defend, stagger, push past or defeat it before resuming the partner-rescue or positioning
  intent. The original purpose remains in memory rather than being lost on every target change.
- Survival is the objective, not a guarantee. Failure remains possible because perception, execution,
  terrain, enemy pressure, equipment and timing are real constraints.

## 2026-10-30 — Bombs are ranged area control and require video evidence (Brian)

- The live cluster/throw decision is promising, but companions acquire and release bombs too close.
  Bomb use must read as a trained ranged attack: earlier group acquisition and farther release, not a
  close panic throw.
- Numeric range/velocity changes are stress-test candidates until telemetry and video show acquisition,
  arc, timing, safety and visual plausibility together.
- Brian will provide video for spatial/animation judgments that telemetry cannot establish. Missing
  video does not erase telemetry evidence, but visual acceptance requires it.

## 2026-10-30 — Torches are a tactical tool, not light only (Brian)

- In normal play a player uses the torch as an off-hand combat option (sword and torch), not only for
  light. The AI must not dismiss torches arbitrarily as useless in combat.
- Doctrine: off-hand torch is its own capability, separate from shield-and-blade. Choosing torch vs
  shield is a situational decision (darkness, fire use against the enemy, shield availability, the
  cost of giving up block against heavy hitters), made by the same profile/arbitration as other choices.
- The current 1.0.x torch rule (torch removed from the hands in combat/readiness) and the analyzer's
  "no torch in combat" checks reflect an older light-only assumption; they change only through a
  reviewed build that implements this doctrine. Until then the runtime is unchanged.

## 2026-10-30 — Learned world knowledge belongs to persisted companions (Brian)

- A companion should know what the party has legitimately learned. When the player reads a lore stone
  or unlocks compendium information while companions are present, the corresponding facts may become
  shared party knowledge and later inform their decisions.
- Persist compact knowledge identifiers and provenance, not copied prose or an ever-growing event log.
  Canonical definitions live once; world/party/person records store bounded references such as learned
  enemy, resistance, immunity, hazard or tactic facts.
- Knowledge is not omniscience. A companion may use learned resistance and defense information only
  after a fair learning source (lore, witnessed combat, training, biome origin or direct experience),
  and still has to perceive the current enemy and successfully execute the chosen response.
- Persistent memory must be lightweight: stable facts and meaningful consequences survive saves;
  transient frames, candidate actions and sensory observations remain bounded runtime state.

## 2026-10-30 — A bomb opportunity may initiate combat arbitration (Brian)

- Merely increasing projectile speed and maximum range is insufficient. A trained companion must be
  able to recognize a visible, incoming, bomb-worthy group before native melee target acquisition.
- A safe ranged area-control opportunity may claim the combat action, select a real visible enemy near
  the planned impact point and open the encounter by poisoning, weakening and scattering the group.
- Existing friend safety, visibility, susceptibility, value, trajectory and range tests remain hard
  prerequisites. This creates an opportunity to act; it does not guarantee a hit or reveal enemies.

## 2026-10-30 — Companion combat decisions do not wait for the leader's weapon (Brian)

- Companions decide from their own perception whether or not Brian's weapon is drawn. The leader's weapon may
  shape formation and readiness posture; it is never a prerequisite for a companion to recognize and act on a
  combat opportunity.

## 2026-10-30 — Awareness owns discovery; combat skills never search for excuses (Brian)

- The pre-1.0 behavior contract already separated native alert, committed threat, combat readiness and
  native combat. FormidableAI preserves and completes that ordering; it does not replace it with a
  weapon-specific world poll.
- One shared awareness service discovers and maintains fair contacts. Bomb, bow, defense, voice and
  formation modules consume the same contact/group facts and may propose responses; none may independently
  scan the entire world to manufacture the condition that makes its own action eligible.
- An action module may perform narrow execution validation (trajectory, lane, range, friendly clearance,
  stamina, equipment and current obstruction). It does not decide whether the contact exists, is alerted,
  is committed, threatens the party or deserves engagement.
- Awareness is continuous evidence gathering, not a repeating action timer: perception samples update a
  bounded contact record; state changes only when evidence changes enough to cross explicit thresholds.

## 2026-10-30 — Tactical awareness doctrine derives responses; it does not ask for global switches (Brian)

- Brian's sight is not a source or prerequisite. "If I can see them, they can see them" means a rendered,
  unobstructed enemy is fairly available to competent companion perception. Companions notice through their own
  senses; they do not wait for Brian to look, face, mark or call out. Companion-to-companion reports may share a
  contact, but every attacker still validates its own current line and arc.
- Sixty metres is a test ceiling for clear daylight noticing, not a universal sight radius or engagement range.
  Visibility is an environment fact affected by darkness, fog, Mistlands obscurity, weather, lighting, terrain,
  target size/motion and the observing character's training.
- Ranged engagement is bounded by the equipped capability. Bows may answer a committed distant threat within
  their trained effective envelope. A 9–20 m bomb cannot engage beyond 30 m. Melee pursuit retains its leash;
  awareness may instead choose ready, cover, reposition, withdraw or hold.
- Sneaking does not create a global fire/no-fire answer. If stealth is intact, companions avoid initiating on a
  merely noticed or suspicious contact. If a group has genuinely committed to the party and combat is becoming
  unavoidable, the arbiter may choose the best survival response, including opening area control.
- Native damage modifiers are not automatically character knowledge. Resistance/immunity may influence a
  tactical proposal only through legitimate training, biome doctrine, lore/compendium learning or observed
  combat evidence. Execution physics may remain exact; tactical knowledge may not be omniscient.

## 2026-10-31 — Souls opponents are competitive FormidableAI evidence (Brian)

- Restore Souls of the Land actors as genuine world combatants, not adjacent
  fixtures or disposable target dummies. Their original biome-based natural
  spawning and a controlled raid both provide opposing combat evidence.
- Friendly and hostile observations feed the same universal FormidableAI core.
  The goal is a combat brain that can later live beneath every Living World
  combatant, with class/weapon/biome doctrine composed above it.
- The controlled Souls raid clones Valheim's `army_theelder` spawn law: ordinary
  groups arrive from varied outer positions, with bounded population and one
  specialist/heavy at a time. It must not be redesigned as a packed barrage.
- Krauguk Legionnaires (`RRRN_OrcBrute`) occupy the Elder brute slot: sporadic,
  single heavy pressure. They are not Greydwarf-equivalent group fodder; if they
  block and coordinate, they can defeat the current companion pair.

## 2026-10-31 — The Tooler owns deliberate raid launch (Brian)

- Brian is not expected to remember or retype project-specific event identifiers for normal test sessions.
- The Living World C.O.R.E. Tooler's Raids page must expose the currently installed FormidableAI training raids and their ordinary time/stop controls.
- A raid button must name an event actually supplied by the Tooler package, the active profile or Valheim itself. Superseded buttons must not remain as dead interface promises.
- Launching through the Tooler remains a normal Valheim event command. It must not add a defended-player status, faction invention, combat behavior or artificial test state.

## 2026-10-31 — Raids hunt the player; Souls hostiles need FormidableAI help (Brian)

- Raids must work as raids: every mob a raid spawns hunts the player (`HuntPlayer = true`). The 2026-10-31 Arena/Swamp
  training raids spawned with `HuntPlayer = false` at a 70–95 m ring and didn't come in; Brian had to use a visual aid on
  the map to find them. This supersedes the earlier "Brian is not a forced target" setting for training raids.
- The Souls hostiles are currently easy ("we owned them"). They are the hostile lineage FormidableAI is meant to make
  formidable, which is the goal for the hostile side of the shared character core.

## 2026-10-31 — Internal diagnostic commands are not Brian's test interface (Brian)

- Brian is not the project's console operator. A diagnostic may expose internal commands for developers, but an ordinary requested test must be automatic, available through the Tooler, or operated by an agent after Brian explicitly says the world is ready.
- Brian's role is to judge gameplay and provide video when human visual judgment matters. He is not expected to memorize or type multi-step spawn/remove command sequences merely to generate engineering evidence.
- Test instructions must begin with the gameplay question being answered and explain why a live run is necessary. If the engineering operation is more complex than loading a world, pressing one clearly named control and exiting, the team owns the automation.
