# Needs and affinity policy

Engine-free policy for a persistent person to express one concrete, authored need
that is supported by loaded nearby world facts. Supported categories are food,
clearing a specific nearby danger, retrieving progression-legal dungeon
materials, and helping a specific settlement.

Affinity is not a currency. The state stores named evidence: help completed,
refusal without hostility, a boundary honored, or griefing witnessed. The public
reading is qualitative (`Wary`, `Known`, `Trusting`, or `Unsafe`). Repeating an
action cannot add another reward or duplicate the memory.

Selection is deterministic and input-order independent. A request must have a
stable authored ID, encounter/person/target IDs, a loaded nearby person and
target, a currently true need, a non-player-owned target, and a positive required
amount. Dungeon materials additionally require verified progression legality.
Requests are never repeatable, and no request or consequence advances offscreen.

`BuildInvitationHandoff` supplies only factual evidence for the separate
`RescueRelease` policy. It does not recruit, assign, spawn, follow, pay, or promise
a yes. A full bonded roster returns `bonded_roster_full` while the person and all
memories remain intact.

`TryBuildQuestRequestFact` is the narrow read-only seam for QuestBroker. It
exposes stable request, person, encounter, authored-need, and target IDs plus the
current outcome. QuestBroker owns camp/message-board/jarl routing and any
seed/map-informed location knowledge. This policy never discovers a location or
decides that a boss site is safe; such a request must arrive as an authored,
progression-legal candidate backed by verified nearby world facts.

Run `tests/Run-Tests.ps1`. The harness compiles in the temporary directory and
does not modify a game profile or the maintained 1.0.x runtime.
