# Companion encounter and resolution arc

Status: Living World design authority for finding, helping, recruiting, releasing,
and revisiting campaign people. This joins the existing discovery, roster,
behavior-state, Second Breath, and dialogue contracts. It does **not** claim the
current 1.0.32.7 lab party already implements campaign camps.

## The promise

A player finds a person who was living a life before the player arrived. The
person may need help, offer help, refuse an invitation, or later need the player
again. Their camp is a real place with a stable history, not a one-time spawn
button. Leaving the area does not erase or duplicate them. A companion released
from the expedition returns to a life and place that can be visited.

The same identity runs through **lead → encounter → known person → possible bond
→ expedition → safe release or permanent death**. Encounter resolution and
individual life state are separate records. A site may be abandoned while its
survivors remain in the world; a recruited person never becomes an ambient camp
spawn again.

## Approaching people whose allegiance is unknown

A one- or two-person group may be met at a camp, on a hunt, or along a patrol.
From a distance, visible gear and activity give clues without revealing a
friendly/recruitable tag. Hearthless survivors, Pillagers, and an Eitrherjar
Legionary use distinct authored allegiances, even when a silhouette is
uncertain. The player may notice them; they may notice the player. Mutual sight
can produce a pause, turn, warning, or cautious approach. It does not itself
authorize an attack or prove friendship. A person's name and allegiance become
known through witnessed behavior, contact, or a reliable prior fact, never
through omniscient proximity text.

The server-owned relationship record governs tolerated distance, theft,
trespass, warnings, aid, and committed hostility. Third-party creature attacks
remain real combat events. When native combat commits, social signals and camp
routines yield movement, hands, facing, and attacks. A Pillager may raid or
bargain; a Legionary remains its hostile lineage unless a separately authored
exception exists. Faction decisions are not inferred from body, biome, or the
temporary native `Character.Faction` enum. See the
[faction bible](./FACTION-AND-PEOPLES-BIBLE.md) and
[behavior-state contract](../LivingWorldHumanNPC/BEHAVIOR-STATE-CONTRACT.md).

## The first complete arc: a friend in need

The first playable template is the authored Split-Ash Camp at a Meadows/Black
Forest edge, credible after Eikthyr. Runa watches from cover while Edda tends a
small fire with an injured ankle. The player reads cut marks, tracks, smoke, and
a distant hail, then finds an active camp. The people do not all stop their
routines to face the player. The full scene and its state/line IDs live in
[the wilderness encounter arc](../LivingWorldCompanionSystems/EncounterLore/WILDERNESS-COMPANION-ENCOUNTER-ARC.md).

When a real threat commits or a known member is newly injured, one witness
gives a short **shock alert**. The voice is an approved brief surprised/alarm
cue, paired with a point or turn and one concise subtitle naming the danger or
person. The alert invites attention; it does not forcibly recruit the player or
authorize attacking every nearby creature. If the threat commits, normal combat
ownership takes over. The player may defend the camp, tend the wounded, recover
the missing person, make a safe route, or walk away. Each response leaves a
recorded consequence. After immediate danger is over, conversation can establish
what help mattered and whether the person can say yes to **“Will you come with
me?”** A yes requires an open legal bond, a valid home, and world progression.

This is Living World fiction, inspired by the project's Second Breath premise
and existing hospitality/obligation themes. It is not an attested Norse episode.
The event should feel urgent because a specific person is at risk, not because a
generic exclamation loops until the player accepts a quest.

## Site and lead ownership

The server owns campaign encounter selection and commits each site once. A site
record has a stable encounter ID, world/biome region, origin, authored template,
anchor position and facing, valid ground checks, cast identity IDs, pressure,
discovery state, resolution state, last safe positions, and the version of the
template that created it. An actor's personal record has separate identity,
relationships, role, injuries, equipment, bond/home assignment, and life state.
Use stable IDs for references; never infer kinship or encounter membership from
display names.

Candidate camps are chosen only from unexplored, biome-valid regions reached by
the player. Validate reachable ground, slope, water/lava, legal distance from
player structures and boss/sacrificial sites, an open footprint, and no overlap
with another persistent encounter. Reject and retry a bad site within a bounded
search; do not place a camp on a creature spawn point and hope its actors survive.
Do not spawn a camp in view as an instant reward for a timer. Persist the chosen
anchor before showing the first clue so reload cannot relocate the camp.

The existing default cadence applies: one unresolved recruitable lead per
player/party and a five-in-game-day delay after recruitment, refusal,
abandonment, or companion death before another lead is eligible. Eligibility
requires a fresh clue and search; it does not materialize a replacement. World
keys and earned equipment/stations limit recruitment and loadouts, although
foreshadowing a later people or biome may precede its recruitable tier.

## Reading the world

Discovery proceeds through durable facts rather than a catalogue:

| Stage | Player evidence | Persistent change |
| --- | --- | --- |
| Rumor/sign | smoke, worked wood, footprints, butchered game, ward mark, or a messenger's specific account | lead known; no person owned |
| Trail | two or more locally coherent signs point toward one anchor; clues may be found in either order | site region narrowed; no duplicate lead |
| First sight | camp and people exist as a scene, with watchers and tasks | stable identities become known |
| Contact | greeting, warning, trade, aid request, or active distress | witnessed facts and relationship updated |
| Resolution | aid, refusal, retreat, departure, safe invitation, or loss | encounter outcome committed once |

A clue remains legible if the first sign is missed. A rough local direction is
enough; a perfect map pin should be earned by a reliable messenger or later
contact. The player can discover a camp that says no, and can revisit a known
person without generating a new encounter.

## Who lives at the site

The server commits a seed-stable cast and kit before creating scene actors.
Biome and verified site context shape a one- to four-person selection: regional
clothing and armor, a credible threat level, distinct combat roles, personal
weapons, finite supplies, and local knowledge. The same seeded choice and
`PersonId` return on reload. A Meadows wanderer, marsh survivor, and Plains
warrior should look and fight as people of their land, without assuming any of
them are already friends. The named Split-Ash pair remain authored exceptions;
the generated catalog fills other claimed sites.

The client receives only visible silhouette, equipment presentation, approved
voice, and server-revealed identity. The server alone owns site and spawn claims,
allegiance, relationship, affinity, invitations, inventory, and combat requests.
An armed stranger may belong to Hearthless, Pillager, or Eitrherjar people;
equipment never proves allegiance. Enemy cast selection does not silently turn
into a recruitable companion. Alliance or temporary aid requires witnessed
terms, and full party membership requires a later distinct offer and acceptance.

Spear and bomb work as one equipment package. A spear user has a different
melee fallback while the spear is in flight or lost. Reclamation requires the
same owned projectile and a safe return path. A finite bomb reserve can coexist
with spear use; exhausting bombs leaves spear and fallback behavior intact.
Knife lunge and atgeir attacks are separate native-combat capabilities, each
enabled only after its exact live animation, item, safety, and replication checks.

## Needs, aid, and earned respect

Camp life can reveal a person's immediate need: food, a threatened approach,
materials for a real repair, or someone missing from a known place. The person
asks in their own voice and terms. The player may help, decline, or leave. Aid
changes remembered facts and trust; it never purchases obedience, fills a
repeatable reputation bar, or recruits someone automatically. People keep
boundaries, duties, and relationships even when grateful. The player can learn a
short account of who they are from the tasks they do, what they ask for, and how
they respond to the result.

Food cannot tame a person, coin cannot hire one into obedience, and sight alone
cannot turn anyone into a follower. The server records the exact accepted
request, the person's own agreement, witnessed help, safe temporary return,
and a later separate invitation. Loaded and unloaded scenes project those
durable facts rather than reset or invent them. The active field limit remains
two people, counting temporary help as a field position.

Each request binds to a verified nearby world target and an authored need. A
food request can use a bounded hunt or delivery; a safety request can point to a
real hostile place; a material request can refer to reachable, progression-legal
resources; a settlement request can concern a specific known person or site.
If no valid target exists, use a different need or a small authored encounter
whose spawning, cleanup, and rewards are single-authority and single-claim.
Never fabricate a map pin to an unverified dungeon, portal, raid, village, or
creature spawn. No hidden offscreen completion or death advances a request.
People may know a seeded place because they traveled, hunted, traded, or heard
reliable local testimony; their map knowledge is a persistent fact with a source
and confidence. A camp conversation, a settlement notice board, and a jarl may
surface the same request when its issuer has made it public; those views share
one request ID, target, and outcome. A private personal need stays with its
speaker. Civic jobs and jarl commissions use the same request machinery with
their own issuer, reach, reward, and consequences. A board or jarl adds world
texture and access; neither manufactures duplicate rewards.
Small boss or bounty fights are future gated request types: the target must be
progression-legal, placed at a verified safe site, and claimed once by the
server before any spawned actors or reward are created.

The culminating choice is personal and mutual. The existing plain player
question, **“Will you come with me?”**, remains valid. After sufficient witnessed
aid, a person may instead initiate with **“Will you have me?”** and the player
answers yes or no. Either path uses the same factual home, world-key, roster,
and expedition rules; a respected person can still say no, and the player can
still decline. Short approved voice and emote cues accompany concise text at
meaningful beats, with one speaker and cooldowns. A voice cue never substitutes
for the readable need or the actual choice.

An accepted request may ask the player and a person to travel together to a
known, verified site. This is a temporary aid agreement with the same persistent
person and equipment, not an instant bonded companion or an extra permanent
combat slot. Arrival may activate one bounded authored hostile group or a
small boss only when the player accepted, the site and progression remain valid,
the participants are loaded, and the server records a single event claim before
spawning. The event then uses normal faction targeting and native combat. It
cannot hijack the vanilla raid clock, duplicate on reload, pay out for offscreen
death, or turn a rumor into a guaranteed fight. A defended route, a shared hunt,
and a later village reclamation are different authored needs using the same
request and witness machinery. The person may return home after the task;
recruitment is a separate later invitation with the usual factual gates.

Travelers arrive with survival-credible, role-specific kit and local knowledge
that fits their origin and world progress. Their personal gear, wounds, history,
and relationship stay attached to `PersonId`. A later tier or earned weapon
improves that same person's record and actor presentation; deleting a named NPC
and spawning a stronger replacement would break lineage and the no-respawn
rule. Spear throws, finite bombs, knife lunges, and atgeir/heavy pressure are
bounded capability requests under native combat ownership. The
[specialist combat plan](../LivingWorldHumanNPC/SPECIALIST-COMBAT-CAPABILITY-PLAN.md)
defines prediction, ally safety, ammunition, animation, and live proof gates.

## Camp routines while the zone is loaded

The encounter anchor defines a bounded safe footprint and stations: fire,
shelter/bed, work surface, lookout, cache, route gate, and retreat point. Missing
stations disable the related activity instead of creating a phantom animation.
At least two members have staggered ordinary tasks while a watcher remains on
duty. Day/night, weather, injuries, supplies, and current threat select from
watch, cook/eat, repair, tend wounds, sleep, short patrol, and conversation.
Nobody works, sleeps, or gestures while their actor is owned by combat,
incapacitation, evacuation, or a higher-priority interaction. Hands and weapons
use the native equipment lease. Reactions and voice lines have per-person
cooldowns so the camp is not synchronized like a chorus.

Camp defense uses authored faction relationships, not a borrowed Valheim faction
enum. A neutral stranger is watched. A committed threat triggers readiness and
then native combat, with role-specific positions around the defended anchor.
After confirmed clear, actors regroup, check casualties, tend wounds, and only
then resume a changed routine. A destroyed fire or lost shelter changes what the
camp can do; routines cannot pretend the furniture still exists.

## Shock alert and Friend in Need event

An alert requires a witnessed fact: a known friend injured, an attacker
committing against camp members, a missing person's real trace, or a dangerous
change at the site. Pure proximity to an uncommitted hostile may justify
watching or readiness, but not a rescue cry or attack order.

Only one speaker owns the first alarm for the incident. On the transition into
the incident, select one short approved surprised/alarm clip and one matching
gesture/subtitle. Preserve the already approved male/female voice assignments
and their shared event timing; do not assign a newly purchased clip by filename
alone. Record the incident ID and alert-fired flag before playback, so reload,
zone re-entry, or two observers cannot restart the same shock chorus. If a new
material fact occurs (a new casualty, attacker breaking through, or fire lost),
one bounded follow-up call may occur after a cooldown. No repeated scream per
enemy scan, no remote voice from unloaded terrain, and no alert claiming a
casualty that has not happened.

The order of control is **witness → shock signal → readiness → committed combat or
aid/evacuation → verified clear → casualty check → conversation**. The signal
controller never owns combat movement or attack. A known recruited companion
can respond to a friend at a nearby camp with the same incident contract, but
cannot hear an omniscient summons across the map; a messenger, marker, or loaded
scene must convey the evidence.

## Rescue, refusal, and recruitment

Rescue is an event outcome, not an automatic recruitment token. Persist who was
helped, what happened, what the player witnessed, who remains missing or injured,
and what the camp still needs. A person may be grateful and still say no because
of dependents, vows, allegiance, injury, or a duty to stay. The yes/no answer is
deterministic from those facts and retained across reload. The active expedition
limit remains two; the bonded roster default remains six. A full roster yields a
truthful no or deferred invitation without deleting the discovered person.

“Release” has two distinct story moments. **Rescue release** ends immediate
danger: a person may escape, be stabilized, return to camp, or move to a known
refuge. It does not force them to join. **Release from Party** later ends an
expedition assignment at a safe home, surviving origin camp, allied camp, or
reclaimed village. It is a handoff with preserved identity, role, injuries,
relationships, personal gear, and an explicit exchange of player-loaned items.
Combat, incapacitation, sailing, portal transit, or unsafe terrain defer that
handoff. If no valid home exists, the companion remains with the party or holds
at a genuinely safe place until a handoff can be made; no offscreen straight-line
walk or deletion frees a slot. Rejoining is a reunion. Committed death is
permanent and cannot be converted into release, rescue, or a new spawn.

## When the player leaves

Selected authored camps may be **server-resident areas** while a dedicated
server is running. Their named people retain the same persistent identities,
loaded actors, and bounded camp routines even when no player is nearby. The
server must actually retain each selected zone, own the actors, enforce an
unwitnessed-combat safety rule, and stay within an explicit camp/actor budget.
A saved ZDO alone does not prove continuous AI: persistent objects are
instantiated when their zone loads. The current pure camp policy supports a
server-resident decision, but the maintained Valheim adapter has not yet been
wired to retain zones or enforce that safety rule. This is a required code and
dedicated-server test gate, not a claim of a running feature. See Jotunn's
[zone and persistent-object documentation](https://valheim-modding.github.io/Jotunn/tutorials/zones.html).

Other camps may unload normally. Zone unload checkpoints the site, cast,
incident state, injuries, gear, threat state, and last safe location. The
server may advance bounded, nonlethal world-time facts, such as a watch
rotation, strained supplies, or planned migration. It must never roll a hidden
death, pay out resources, create a replacement, or turn a living named person
into an untracked ambient spawn.

Leaving during a Friend in Need incident cannot reset it for infinite rewards.
If combat has already produced an authoritative death, that death stays. If
people are alive when the area unloads, the incident can enter a persisted
**withdrawn/uncertain** state: the camp may be damaged or abandoned, survivors
move to a known refuge or leave a track, and the player may face a harder
follow-up. This is a specific consequence, not a random offscreen massacre.
The unresolved person remains discoverable by identity and last reliable lead.
On return the host reconstructs actors from their records at their valid current
site, never from the original template as new people. Re-entering repeats no
first-contact reward, first shock voice, or recruitment roll.

If the player stays nearby and sees the event through to a confirmed death, the
Second Breath rule governs that named person once campaign mortality is enabled.
Campaign permanent mortality must remain disabled until traversal, hazard,
network ownership, and death attribution meet the existing live proof gates.

## Pilot and acceptance gates

Build one Meadows edge camp and one Friend in Need incident before multiplying
biome templates. A production pilot requires:

1. Stable camp/person IDs and server-only site selection; reloading before first
   sight cannot move or multiply the camp.
2. Safe anchor placement; real stations and staggered routines; watcher never
   sleeps simultaneously with the whole camp.
3. Clue trail and first contact that work without a menu or debug-spawn command.
4. One witnessed shock alert with approved clip/gesture/subtitle, no replay on
   reload, correct cancellation and native combat handoff.
5. Player helps, declines, or leaves; each outcome persists and produces a
   coherent return scene without unseen lethal simulation.
6. Rescue does not imply recruitment. Yes/no reasons, world key, home, two active
   slots, six bonds, and five-day lead cooldown all survive reload.
7. Safe Release from Party at camp/village/home preserves the actor and gear;
   unsafe release cannot delete or strand them.
8. Zone unload, server restart, re-entry, two players arriving together, and
   overlapping site candidates create no duplicate incident or person.
9. Lab/debug actors fail every campaign-recruit, roster, residence, memorial,
   and discovery gate.
10. Campaign death, when separately enabled, is single-authority and terminal;
    no hidden respawn/revive path exists.

The current 1.0.x NPC line has a lab party, voice and combat behavior. The camp
placer, campaign encounter record, offscreen camp policy, Friend in Need event,
and safe release integration are new modules. Pure policies and tests can be
built in parallel; Unity/world adapters must be integrated and live-proven on
the maintained 1.0.x line before this arc is advertised as playable.
