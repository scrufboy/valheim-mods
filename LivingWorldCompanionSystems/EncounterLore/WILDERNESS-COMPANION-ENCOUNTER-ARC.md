# Wilderness companion encounter arc

Status: **Living World encounter design direction; not runtime proof.** This bank
applies the canonical Second Breath, discovery, roster, and release rules without
adding new shared canon. All people, dialogue, routines, rewards, and state
transitions below require implementation and playtest.

## Canon boundaries

- **Living World canon:** Second-Breath people are embodied and persistent; there
  is no Third Breath. The culminating choice is a deterministic yes or no. The
  player may ask **“Will you come with me?”** or, after sufficient witnessed aid,
  the NPC may instead ask **“Will you have me?”**
- **Authority decision:** these are mutually exclusive presentations of the same
  factual choice, following `COMPANION-ENCOUNTER-RESOLUTION-ARC.md`. Neither route
  adds a persuasion check, changes the home/world-key/roster gates, or permits a
  second invitation after a no.
- **Living World canon:** a valid home/bed and an open bonded-roster seat are
  required for permanent acceptance. An expedition slot is separately required
  to follow immediately.
- **Living World canon:** Release from Party happens only at a lawful handoff and
  preserves identity, history, injuries, personal gear, and relationships.
- **Encounter design direction:** the named people, clues, camp events, and lines
  in this document.
- **Runtime unknown:** no prop, gesture, animation, voice asset, spawn profile, or
  equipment mesh is claimed present merely because it is named here. Only the
  approved short voice categories are requested.

## Meadows encounter bank: The Split-Ash Camp

### Encounter records

| Record | Authored value |
| --- | --- |
| Origin | Two Hearthless road survivors seeking a defensible home |
| Site | Small cook camp beside a split ash tree, on reachable Meadows ground |
| Activity | Hunt, cook, repair, watch, tend a strained ankle, sleep in shifts |
| Relationship | Wary; may become friendly or indebted |
| Pressure | Greydwarfs have learned the camp's dusk routine; Edda's ankle slows retreat |
| Outcomes | remain, accept aid, refuse aid, retreat together, invite/recruit Runa, later relocate |

**People.** Runa is a practiced close-first Scout: measured, protective, doubtful
of Odin, and looking for a hearth that admits people who cannot always fight.
Edda is a woodsman and camp cook: plain-spoken, strained by an injured ankle, and
not recruitable in this arc. They are companions, not a quest giver and a prop.

**Placement.** Use a bounded, biome-valid site away from player structures,
sacrificial/boss areas, deep water, steep traps, and other persistent encounters.
The camp persists until resolved or migrates through the ambient encounter rules;
neither person is deleted by unloading it.

### Discovery: signs before subtitles

Present at least two signs before the camp is visible. Each points toward a
physical person rather than placing a map marker.

| Signal | Player read | State/fact |
| --- | --- | --- |
| Low grey smoke, broken by pauses | A managed fire is being hidden, not a wildfire | `splitash.discovery.smoke_seen` |
| Fresh deer dressing with the useful cuts removed | A careful hunter passed recently | `splitash.discovery.butcher_sign_seen` |
| One deep boot trail beside a lighter, dragging step | Two travellers; one is hurt or burdened | `splitash.discovery.limp_tracks_seen` |
| Three shallow axe cuts on the trail-facing side of an ash | A deliberate route mark, not decoration or magic | `splitash.discovery.route_mark_seen` |
| One brief **hail** from the ridge after the player pauses | The watcher has chosen contact | `splitash.contact.signaled` |

No clue grants loot, reputation, or automatic recruitment. Ignoring the trail
leaves the camp unresolved.

### Camp read and ordinary life

Runa watches from offset cover while Edda tends the fire. Only one person leaves
their task to speak. Their loop is staggered so the camp never looks staged:

1. Runa watches the creature route, checks the camp, then repairs bow or shield.
2. Edda cooks, rests the ankle, adds fuel, and sorts the next meal.
3. At dusk Runa takes the outer watch; Edda banks the fire and sleeps.
4. When danger is perceived, both routines cancel. Runa takes the open approach;
   Edda moves toward the fallback, fighting only if escape is closed.

First contact, weapon low:

- Runa — **warning**, low-ready: “Weapon low, stranger. Ours will follow.”
- Edda, without leaving the fire: “If you came for warmth, leave room for sense.”
- Runa, after the player complies: “Runa. That is Edda. We are between hearths.”
- Edda: “And slower than we were yesterday.”

If the player advances with weapon raised after warning, Runa says, “That is far
enough.” The camp becomes guarded. Further violation makes both retreat; attack
makes them defend themselves and closes recruitment unless a separately authored
reconciliation ever changes those facts.

### Living needs and earned-respect requests

Ordinary routines expose a concrete need before either person asks for help. At
most one request module is active. Selection is deterministic from verified world
facts; it is not a random chore roll, affection farm, or required substitute for
the Friend in Need event. The player may aid, decline plainly, or leave. Declining
a small request does not create hostility.

| Module | Routine evidence | Request line and cue | Completion fact | Gate and limit |
| --- | --- | --- | --- | --- |
| Shared meal | Edda divides the last cooked portion; drying line is empty | Edda — **question**, indicates the pot: “We have fire for one meal and food for half. Will you help even that?” | `splitash.request.meal_resolved` | Accept one suitable, progression-legal meal contribution, or the nearby boar-hunt branch below; no gift repetition |
| Nearby boar hunt | Runa reads fresh local rooting while Edda prepares the empty pot | Runa — **acknowledgment**, points toward verified tracks: “Boar passed close. Hunt with me, and we both eat before dark.” | `splitash.request.boar_resolved` | Only if live nearby boar evidence and a safe bounded route are verified; naturally spawned animals only |
| Nearby threat | Runa repeatedly checks one screened approach; Edda keeps travel gear packed | Runa — **warning**, turns toward the known approach: “Something nests too near the path. Clear it with us, or give it distance.” | `splitash.request.threat_resolved` | Only a verified, reachable, ordinary nearby threat; never invent a raid, den, dungeon, or spawn |
| Lawful core retrieval | Edda tests a cold fire fixture and stores an unfinished repair | Edda — **question**, shows the incomplete fixture: “A Surtling core would finish this. Only if your road has made such things lawful.” | `splitash.request.core_resolved` | Only after progression and source facts prove the player can legally retrieve one from already valid world content; never generate or imply a chamber, portal, or core source |
| Nearby village help | Runa studies a known route mark; Edda bundles repair supplies | Runa — **question**, opens a hand toward the route: “That village asked for hands, not heroes. Will you help us answer?” | `splitash.request.village_resolved` | Only with a real nearby unresolved village need; bounded supply, repair, escort, or watch work; no assumed raid |
| Map-informed safe route | Edda compares their worn route marks with a location the player has already discovered | Runa — **question**, looks from the player's offered map to the trail: “You have walked that crossing. Show us the safe side, and we will remember the road.” | `splitash.request.mapped_route_resolved` | Optional only when the player deliberately shares eligible discovered map knowledge and a reachable route target already exists; never reads undiscovered terrain or private markers automatically |

The boar hunt is a branch of the meal need, not a second task. Offering suitable
food and hunting are equivalent evidence of practical aid; neither may be repeated
to grind standing. A completed module records `splitash.earned_respect`. Runa
answers with **acknowledgment**, weapon stowed: “You saw the work and put a hand
to it.” Edda adds, with restrained **relief**: “That is better than a speech.”

On acceptance Runa gives a brief **acknowledgment** and turns back to the work:
“Good. We do it together.” On refusal Edda gives a small head shake; an optional
`shrug` emote may replace it only after animation/network approval: “Fair enough.
The need remains ours.” These cues request approved voice categories and ordinary
physical acting. `point`, `open-hand`, and `shrug` are presentation candidates,
not claims that an emote asset is approved or available.

### Map knowledge and later request presenters

Map knowledge can shorten uncertainty; it cannot create a target. For the
map-informed route module, the player explicitly offers an eligible discovered
location. The request stores the target's stable world reference and the
knowledge provenance (`player_shared_map`), then asks the player to guide or
verify the bounded route in the loaded world. Removing or renaming a personal map
label does not delete the request, and no completion occurs from a marker alone.

A personal need remains with its speaker by default. Runa's and Edda's requests
are private camp conversations unless that issuer explicitly makes a specific
request public and the record stores that visibility change. Only then may a
future notice board or jarl conversation display the same persisted request ID,
target, issuer, state, and outcome. Those are later presentation surfaces—not
copies, rerolls, or second quests. Accepting at camp and reading an authorized
public notice must resolve one record; rewards and completion remain single-claim
under the future QuestBroker authority.

Civic board work and a jarl's own jobs are not automatic reposts of a person's
private respect test. They are separate request records with their own issuer,
scope, visibility, language, and consequences, while reusing the same broker
mechanics. This encounter bank defines dialogue and meaning only, not broker
mechanics or a board/jarl asset.

A future higher-tier request may concern a named local champion or small boss
only when QuestBroker has a verified target, lawful spawn/ownership/cleanup,
biome-appropriate progression, and a world gate that makes the request credible.
Split-Ash Camp never offers a boss request in its Meadows pilot. A map marker,
notice board, or jarl title cannot bypass those gates or send an early player to
a later-biome threat for earned respect.

### Friend in Need: the shock moment

This is an observed emergency, not a timed dialogue puzzle. It may fire only
after the player has had a fair chance to read the camp and while both people are
alive, local, and outside combat.

Runa hears brush movement and turns toward the expected route. A second group
breaks from the camp's blind side. Edda rises too quickly, the ankle folds, and a
greydwarf stone strikes the cook frame. The pot overturns; the fire gutters.
Runa gives one sharp **alarm**: “Edda—down!” She places herself between Edda and
the attackers, then calls to the player: “Left side! Do not let them through!”

The shock is the collapse of a competent routine: the watched route was a feint,
the meal and fallback are lost at once, and Runa must choose Edda over a clean
fighting lane. Do not kill Edda by cutscene, seize the player's controls, or make
the enemies wait for dialogue.

### Player conduct and immediate outcomes

**Aid.** The player engages the attackers without striking the pair. Runa still
fights, can be hurt, and does not guarantee victory. When the area is verified
clear, she checks Edda before addressing the player.

- Runa — quiet **relief**: “You held the open side. Edda still breathes.”
- Edda: “The supper does not. I will mourn it properly.”
- Runa: “Help me brace the ankle. Then we speak.”

Aid records witnessed shared defense. Bracing is a short care interaction, not a
healing miracle; it does not erase the injury or award an item bundle.

**Refusal.** The player explicitly declines before danger begins, or withdraws
without attacking either person. Runa answers with **acknowledgment**: “Go, then.
I will not spend breath begging twice.” The pair conduct their authored retreat.
The camp remembers refusal, but does not turn hostile merely because the player
said no. If they survive and are met later, the invitation remains blocked until
new witnessed facts justify a change.

**Retreat together.** If the camp is overrun or Runa reaches the low-health
retreat condition, she calls “To the stones. Together!” Both move to the nearby
prevalidated fallback. The player may cover the retreat. Success records rescue,
not victory; the original camp remains damaged and its stores are not magically
restored.

**Violation.** Theft from the marked personal stores, friendly fire not resolved
as an implementation-safe accident, or pursuing the pair past their clear warning
records harm. Runa: “We offered distance. You answered with taking.” Recruitment
closes while that fact remains true.

### Fire talk and the plain question

After aid or a covered retreat, and only when safe, the pair return to a small
fire or fallback rest. The player can learn the relevant facts:

- Runa: “I woke under a sky I knew, in a land I did not. The raven wanted a
  weapon. Edda needed a road.”
- Edda: “She chose the road. That is the useful part of the tale.”
- Runa: “I will not leave Edda in open country. Show me a defended bed for them,
  and I can range farther than this fire.”

The player-initiated invitation is exactly:

> Will you come with me?

Alternatively, after sufficient witnessed aid and only when all the same yes
facts are already true, Runa may initiate:

- Runa — **question**, weapon stowed: “Edda is safe. Your road is one I would
  choose. Will you have me?”
- Player yes: “Yes. Come with me.”
- Runa — **assent**: “Then yes. My road and yours run together.”
- Player no: “No. Not now.”
- Runa — **acknowledgment**: “Then we part as friends.”

The two invitation routes are mutually exclusive for the current resolution.
Both are evaluated from stored facts, never rolled. A player no leaves Runa a
known person at the safe camp/home; it causes no hostility or consolation reward.

| Facts at the question | Answer and presentation |
| --- | --- |
| Shared defense, covered retreat, or earned respect plus required encounter resolution; Edda alive; valid home/bed for Edda; open bonded seat; recruitment world gate met; no unresolved harm | Player route: Runa answers **assent**: “Yes. Edda has a hearth, and you held when leaving was easier. I will come.” NPC route after sufficient aid: Runa asks “Will you have me?” and bond completes only after the player's yes. |
| Edda alive, but no valid home/bed | Runa — **refusal**: “No. Not while Edda's only roof is smoke. Make a home, then ask again.” |
| No open bonded-roster seat | Runa — **refusal**: “No. Your promises already fill your hall. Keep them before you make another.” |
| Permanent recruitment gate not met | Runa — **refusal**: “No. You have a road yet to prove. We can still share this fire.” |
| Player refused aid and no later qualifying deed exists | Runa — **refusal**: “No. When the line broke, you chose your road. I remember it.” |
| Unresolved theft, attack, or pursuit | Runa — **refusal**, guarded: “No. Strength without restraint is only another danger.” |

If the active expedition party is full but the bonded roster has room, Runa may
accept the bond and go to the selected safe home; she does not become a third
follower. Edda becomes a persistent resident/worker at the same destination or
another selected lawful home. Personal gear stays with its owner. There is no
instant loot, skill unlock, affection payout, or camp chest reward.

## Release from Party: the returning fire

This scene is available only at a safe home selected through the canonical
handoff. Combat, incapacitation, sailing, portals, and unsafe terrain block it.
Loaned items are settled through explicit exchange before transfer.

Player chooses **Release from Party**. Runa looks first toward the home's watch
line, then toward Edda or the resident who keeps the fire.

- Runa — **question**: “You mean this watch, not the end of our road?”
- Player confirmation: “Stay here. Keep this place.”
- Runa — **assent**: “Then I come home standing. I will keep the outer path.”
- Edda, if present: “Good. The roof has been unbearably quiet.”
- Runa, with restrained **laugh**: “You complained when it leaked.”
- Edda: “I contain multitudes.”

Runa becomes a resident guard/scout, returns personal gear to its normal carried
state, and takes a home routine: inspect the outer path, repair by the fire, eat,
rest, and acknowledge returning expedition members. Her first later reunion line
is: “The path is clear. Are we ranging again?” Rejoining restores the same person
and history; it does not replay recruitment or spawn a replacement.

If Edda is elsewhere but safe, Runa instead says, “Then send word to Edda. I have
a roof to report.” If no lawful home is actually available, Release from Party
does not complete: “Not here. Find us a fire that will still be burning tomorrow.”

## Plains example: The Trampled Millet Watch

Status: **encounter design direction and capability target, not runtime proof.**
The exact Plains equipment, meshes, throw animation, and spawn profiles remain
unverified. This example must not become recruitable before the approved world
gate or provide black-metal progression.

A formidable adult spear hunter, Yrsa, and a heavy pressure fighter, Ketil, watch
a Fuling route from a damaged farm shelter. Signs are a Fuling track deliberately
crossed out with a boot heel, millet cut low for sight lines, two spent plain
wooden marker shafts, and smoke vented along the ground. These are authored
signals, not claims for new art assets.

Yrsa checks lanes and finite throwing reserve; Ketil reinforces the narrow entry,
rests, and watches Yrsa's blind side. When Fulings drive a lox across the planned
lane, their careful ambush collapses. Ketil is knocked from the entry. Yrsa gives
one **alarm**—“Ketil! Wall side!”—then to the player: “Draw them past the stones;
give me a lane!” The player creates opportunity; the pair do not passively win
the encounter.

After verified safety:

- Yrsa — **relief**: “You made the opening. We spent it well.”
- Ketil: “And I spent a wall.”
- Yrsa: “Fulings return by this track. We leave when the wounded and stores are
  clear—not before.”

The player may aid the evacuation, refuse without hostility, or cover a retreat.
Recruitment cannot bypass Yagluth/world-key policy, an open roster seat, a valid
home, or Yrsa's duty to Ketil. When facts permit, the player asks **“Will you come
with me?”** Yrsa's yes is: “Yes. Ketil has a wall, and you know how to make a
lane.” Her honest no while duty remains is: “No. A hunter who abandons her wall
soon hunts alone.” Neither answer grants her personal equipment to the player.

Release at a safe farm watch gives a smaller echo of the Meadows homecoming:
Yrsa studies the cleared sight line and says, “This field has room to throw and a
door worth returning through. I will take the watch.” Her next reunion begins,
“The east track is quiet. Shall we change that?”

## Presentation limits

- Subtitle lines should remain under roughly two breaths; split exchanges rather
  than stacking lore into one speech.
- Use only the requested voice categories: hail, warning, acknowledgment,
  question, alarm, relief, laugh, refusal, and assent. No specific audio clip is
  asserted.
- Physical acting uses ordinary facing, spacing, weapon state, work interruption,
  and movement. Any gesture/emote still needs separate runtime approval.
- Request modules depend on verified local facts. They do not promise generated
  dungeons, raids, portals, villages, creatures, props, or navigation support.
- Multiple presentations may reference one persisted request only after its
  issuer explicitly makes it public. Private camp needs stay with their speaker;
  civic and jarl jobs use separate issuer/scope records. No surface may duplicate
  acceptance, completion, or reward claims.
- Dialogue never exposes thresholds, percentages, personality axes, world keys,
  roster numbers, or debug reason codes.
- Camps and people persist through authored resolution. Refusal, retreat, release,
  zone unload, and a full party never delete a person.

The machine-facing mapping for every authored line and signal is in
[`encounter-arc-contract.v1.json`](./encounter-arc-contract.v1.json).
