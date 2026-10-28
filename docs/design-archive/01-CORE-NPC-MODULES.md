# Core NPC modules

All roles use the same actor core. A module declares the persistent orders that
permit it, its exclusive action owner, higher-priority cancellation, entry evidence,
exit evidence, and controls it may never own.

| Module | Core responsibility | Boundary |
| --- | --- | --- |
| Identity | stable name, sex/body recipe, appearance, origin, relationship and history | never rerolled by role or travel |
| Persistence | authoritative save, ownership, versioning, migration and duplicate suppression | server authority; no silent reconstruction |
| Traversal | Follow/Hold/Resident travel, regroup, doors, terrain, portals and instances | native pathfinding remains executor |
| Party spacing | soft leased regions, personal space, sidestep, deterministic right-of-way | no teleport, force, direct velocity, or second combat mover |
| Awareness/reaction | peaceful notice, curiosity, readiness, legal threat evidence | awareness does not grant combat by itself |
| Investigation | bounded interest and return to order anchor | cannot become unlimited pursuit |
| Combat | authorize legal targets and role equipment; native `MonsterAI` owns committed combat | no competing combat controller |
| Needs | food, stamina/health resources, rest, environment and retreat/re-engage | shared embodiment, not a second state machine |
| Gear | owned/loaned equipment, finite ammunition, durability, repair, stow and visuals | no free progression or duplication |
| Progression | biome/world-key credibility, skills, capabilities and investments | follows earned Valheim progress |
| Roles/classes | companion, guard, worker, resident and combat specialization | plugins over one identity, not cloned actors |
| Voice/presentation | semantic combat voice pools and later non-combat speech modules | combat voice baseline remains locked separately |
| Mortality | pre-death aid, authoritative permanent death, gear recovery and memorial | no resurrection after committed death |

Persistent orders are Resident, Follow and Hold. Exclusive ownership priority is:
dead/incapacitated, combat, combat pursuit, regroup, transition/door crossing,
alert/readiness, ordered movement, settled companion activity, resident activity.

