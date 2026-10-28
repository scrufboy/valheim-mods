# Mandatory portal and instanced-interior traversal gate

Active Follow companions must travel with the player through legal Valheim portals
and supported instanced interiors, including Burial Chambers, Troll Caves, Sunken
Crypts, Frost Caves, Infested Mines, and equivalent current-game entrances/exits.

Only active expedition companions under Follow are eligible. Residents, Hold
actors, camp NPCs, workers and unrelated people do not transfer.

The same persistent identity crosses exactly once. Preserve name, appearance,
sex/body recipe, role/class, health, resources, inventory, equipment and stow state,
relationship, order and active-slot assignment. A transition may not reroll,
replace, resurrect or duplicate an NPC.

Queue from the player's real native transition. Suspend the source authority, wait
for the destination instance and correct network owner, materialize authoritatively,
then place at staggered reachable safe points. Avoid overlap, pushing, falls,
lava/water, and entrance blocking. Release into regroup/formation, then ordinary
native combat. Do not introduce a second movement controller.

Respect portal tags, item restrictions and dungeon rules. Illegal transfer returns
a truthful wait/refusal; it never bypasses rules silently.

Entry/exit must be symmetric and idempotent. Explicitly handle combat,
incapacitation, permanent death, excessive distance, owner disconnect, rapid
re-entry, interrupted loading, partial-party failure, unload/reload, restart and
late join. Prevent duplicate ZDOs, source ghosts, inventory duplication/loss,
health resets, order loss and accidental revival.

Required matrix: solo host, remote client, dedicated server; one/two companions;
each entrance and exit; portal round trip; rapid repetition; unload/reload;
save/restart; late join; full inventory; far, fighting, dead and failed companions.

Telemetry: request, eligible identities, source suspension, destination readiness,
authoritative materialization, duplicate suppression, safe placement and regroup
release. Permanent mortality remains disabled until this gate and authoritative
death attribution are live-proven.

