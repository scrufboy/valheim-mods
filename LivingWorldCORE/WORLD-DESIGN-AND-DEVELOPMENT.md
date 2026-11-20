# Living World C.O.R.E. — World design and development

*Characters, Origins, Roles & Encounters · Design overview and discovery record*

Living World C.O.R.E. is a Valheim overhaul about finding the people who came before you, reclaiming places that fell, and giving the living a reason to return. The player can start with smoke on the horizon, a footprint on a road, or an occupied ruin. What follows should leave a mark on the place and the people who live there.

A custom creature can be built with a body, a controller, a movement profile, a set of attacks, and a list of targets. Living World uses that capability to author encounters. We also ask who the creature belongs to, what drew it here, what it wants, who responds when the player intervenes, and what persists afterward. Those answers determine where we put the actor, how it behaves, and how we test it.

This overview connects the existing design authorities and illustrates a production method. The world and encounter descriptions state intent unless a runtime result is named. The [story guide](STORY-AND-WORLD-GUIDE.md), [lore bible](LORE-BIBLE-THE-SECOND-BREATH.md), [faction bible](FACTION-AND-PEOPLES-BIBLE.md), and [package boundaries](PROJECT-BOUNDARIES.md) govern their respective details.

## The world before the player

Earlier arrivals from Midgard took the **Second Breath** in Valheim. They had bodies again. They hungered, worked, raised halls, made roads, buried friends, and died. The player arrives among the remains of their decisions. A road once joined places worth travelling between. A ruined village was somebody’s home.

Odin gathered the dead for a purpose. Some served willingly, some served to survive, and some wanted hearth and kin more than another lord’s war. The **Hearthless** and **Hearthbound** express whether someone has a defended place to belong. A wandering person can find a home, and someone with a home can lose it. Allegiance belongs to people and their choices. Body, ancestry, sex, and biome provide context without assigning morality.

The Second Breath is Living World’s fiction, built to fit Valheim and draw on Norse themes of hospitality, obligation, and remembrance. It gives companions a life beyond their usefulness in a fight. At its intended conclusion, a true death leaves gear to recover and a person to remember. Permanent mortality depends on reliable traversal, recovery, and persistence so that a doorway, ownership bug, or broken save cannot write that ending by accident.

## Discover, reclaim, rebuild

1. **Discover.** Read signs of habitation such as worked stumps, a cooking fire, warding marks, or a watcher on a road. Find people already doing something. A group may be keeping watch, tending a wound, hunting, or deciding whether to move.
2. **Understand.** Establish the encounter’s origin, site, activity, relationship, pressure, and possible outcome. A wary camp might need help securing a route. A grave-warden might know what happened at a nearby ruin. Their response comes from what they know and what they owe to the people around them.
3. **Reclaim.** Clear a danger that actually makes the place unusable. In a draugr village, defeating the revenants releases the bound dead. The survivors who return are living people found through camps, refuges, and roads.
4. **Rebuild.** Give the reclaimed place work, shelter, defenses, and occupants with reasons to stay. A person met in the wild can become a companion, guard, worker, or resident and retain the same identity. A companion who leaves the active party needs a meaningful destination.
5. **Live with the result.** A settlement creates routes worth watching and people worth defending. Pillagers have a reason to prey on it. The player can return and see who stayed, who moved on, and what remains vulnerable.

Those steps describe an intended loop, not a fixed quest sequence. Recruitment is a direct question once the conditions of the encounter are established. An answer of no leaves the person with a life of their own. The [companion discovery design](WORLD-COMPANION-DISCOVERY-AND-BIOME-ROLES.md) gives the encounter and placement rules.

## What the design asks of an actor

| Design question | Implementation consequence |
| --- | --- |
| Where did this actor come from, and why are they here? | Give them an origin, an authored site or route, and a reason to remain or move. |
| What do they do before combat? | Show work, rest, patrol, investigation, or another readable activity the player can observe. |
| Whom do they protect, tolerate, or oppose? | Author relationships explicitly. Treat borrowed faction values as compatibility details. |
| What can change during the encounter? | Give each transition a trigger, a runtime owner, a readable signal, and an exit. |
| What can change afterward? | Resolve pursuit, surrender, release, recovery, death, return to home, or another authored outcome. Keep identity stable where the actor survives. |
| What proves it? | Name the version, test profile, starting state, player action, expected result, and evidence from the game. |

A Plains spear Huntress, for example, has a regional role. Her equipment, traversal, and behavior should make sense in the Plains. Her spear needs a grip that reads, a thrust that makes contact, a throw with a native-compatible release, and a credible recovery. When she leaves the party, the world still needs to know who she is and where she goes. Each of those questions can be worked separately and tested against the same actor contract.

## Worked encounter — the swamp procession

The swamp is a place where living factions work around the dead and sometimes try to put the dead to work.

| Actor | Role in the encounter | Design implication |
| --- | --- | --- |
| **Necks** | Native swamp life | Preserve the familiar small animal identity. |
| **Nekmen** | Adult reptilian people with roles such as warrior and Shaman | The Shaman commands the binding. Defenders respond to danger around the ritual. |
| **Mirekin** | Small ranged defenders around swamp approaches and temple grounds | A blowgun fighter needs a real aim pose, release, dart, and reload. Its ability to reposition and support the group gives that weapon a purpose. |
| **Sunken Marauders** | Frog-like workers and escorts | Show them unearthing the Behemoth. When danger breaks the formation, they can retreat and later rejoin. |
| **Draugr Behemoth** | An embodied undead captive | Use the authored Behemoth/Colossus identity. The captive retains its own movement and combat authority. |

The player can discover the workers at the excavation or encounter the procession later. An orange glow tells the player the Behemoth is bound. If the binding breaks, red signals a frenzy. The escorts respond while the captive acts under its own authority. Green signals the cooling phase, during which the Behemoth targets undead. Surviving defenders can buy the Shaman time to rebind it. The encounter needs an outcome when rebinding fails, with the Behemoth still present in the world.

That gives art, animation, AI, and testing a common problem to solve. A leash or hand token shows a relationship. The runtime state controls whether the relationship still holds. The encounter owns that state locally so one Shaman’s failure cannot release another instance’s captive. To prove the first slice, reproduce a break, observe the change in target eligibility and escort behavior, and confirm that the actor persists through the transition. Procession paths, additional variants, and natural placement can follow separately. This is the intended encounter design. The full sequence has not been established as live-proven in the records reviewed.

## Runtime and package boundaries

- **Living World C.O.R.E.** owns the world fiction, character identities, roles, relations, settlements, and authored encounters.
- **Formidable NPC Framework** is the independently implemented runtime foundation being developed for shared registration, identity, equipment, effects, combat, and persistence capabilities. Migration away from the repaired RRRCore reference stack is staged and depends on proven capability parity.
- **Actor and content packages** own the specific families, presentation, and behavior granted by their roles. An approved new actor is additive and has its own identity. Shared capabilities can serve a resident, companion, or enemy.
- **Tooler and telemetry** help spawn fixtures, reproduce cases, and record results. A Tooler fixture is distinct from a persistent campaign encounter. Released behavior is designed to run independently of development instrumentation.

Start by inspecting native Valheim behavior and the exported DevKit. Reuse a compatible native combat path where it meets the actor’s requirements. Define who owns movement, targeting, combat, animation, and encounter state before extending them. For instance, a Surtling flight decision can supply a direction and select a bounded animation state. The animation makes the turn or climb readable. It must have clear entry and completion, and the movement controller remains accountable for the actual trajectory.

## What development has taught us

| Recorded result | Consequence for the workflow |
| --- | --- |
| **Surtling registration.** The chainloader loaded the plugin, then registration aborted because the expected Smoked HUD asset was missing. The world still opened. | Check prefab and effect registration explicitly. A loaded DLL and an open world prove different things. |
| **Surtling candidate packaging.** A later private candidate compiled, passed 79 offline checks, and was packaged. Its checkpoint recorded that installation and live testing were still open. | Carry one exact package through installation and observation. Offline coverage narrows uncertainty about the candidate. Visible attack timing and flight still need the game. |
| **Behemoth identity.** A candidate was installed and rejected because it used a Troll-derived visual and runtime foundation outside the approved scope. The legacy packages were restored. | Check model and runtime lineage against the character contract before installing. Preserve a known rollback. A technically running candidate can still fail the product requirement. |
| **Huntress spear behavior.** The DevKit review found distinct native poke and throw paths and identified a duplicate-dispatch risk around their shared animation event. | Trace an authored action from input or AI choice through the actual event, release, hit or projectile, and recovery. Code inspection identifies the test. The assembled actor still has to pass it. |
| **Profile and reporting drift.** Project recovery records documented profiles carrying different Tooler generations and offline work described as ready. | Name and verify the package, dependencies, and profile the tester will actually use. Update readiness from evidence at that stage. |

These were practical failures in AI-assisted development. Agents produced work that looked plausible, reported success at the wrong stage, or filled an asset or runtime gap with an unauthorized substitute. Review must catch these errors before they cost another launch. Where evidence is missing, carry it forward as an open question with a named next test. The [Claude/Codex exchange protocol](../claude-exchange/PROTOCOL.md) defines the evidence labels, comparison rules, and promotion criteria.

## How the project moves in short work sessions

The world design is the reference for each small job. Development can stop and resume between jobs because scope, evidence, and the next action live in Git. A shared project channel can carry links, questions, and observations so people can contribute on different schedules.

1. **Pick one observable outcome.** For example, a Surtling at its intended scale casts a native fireball while airborne, shows the release, and returns to flight. The broader actor contract records the other attacks, recovery, and encounter role.
2. **Inspect the relevant native path and the actual asset.** Find the prefab, rig, controller, animation event, attachment point, or game rule that governs the behavior. Record the gap before implementing.
3. **Assign a bounded job.** Name the owner, baseline, files, constraints, expected result, and handoff location. A lightweight session with the relevant access can route already-defined work. Use dedicated sessions and specialist review where implementation, animation, or runtime diagnosis requires it.
4. **Return a reproducible checkpoint.** Record the branch and commit, changed files, affected dependencies, package identity and hash where applicable, last completed stage, and outstanding uncertainty. Source, compile, offline check, package, install, game registration, gameplay test, and acceptance each get their own result.
5. **Integrate one candidate at a time against a named baseline.** Confirm what the test profile actually loaded. Test the smallest observable behavior and its cleanup. Preserve the prior working state and record how to restore it.
6. **Carry the result back to the world design.** A proven fireball cast advances Surtling combat. A proven binding break advances the swamp encounter. A proven return to a home advances reclamation. The next task should say which of those promises it advances.

For a collaborator coming in on a short session, the useful starting point is one named build and one question. Review a rig, inspect the timing of a release, test a faction reaction, or reproduce a transition. Leave evidence and the next decision where the next person can pick it up. The [development chronicle](../docs/LIVING-WORLD-CHRONICLE.md) records changes in world direction and accepted milestones. The [current handoff index](../claude-exchange/CURRENT.md) points to active engineering work.

The world needs to make sense before every piece is playable. Each piece also needs to earn its place through behavior the player can see. Reclamation becomes real when the player can return to a place, recognize the people they helped, and see what those people are doing with the home they recovered.
