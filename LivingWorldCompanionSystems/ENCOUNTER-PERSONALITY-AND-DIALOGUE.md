# Encounter personality, initiative, and dialogue grammar

Status: authority for first contact and the yes/no companion invitation.

## Performance model

Companions do not require spoken modern-language voice acting. Meaning uses:

1. posture, distance, facing, weapon state, and group position;
2. short nonverbal vocalization from the approved voice library;
3. gesture or safe native emote;
4. concise written line assembled from authored components;
5. a plain player response or invitation.

Text is never random word salad. Each line has one intent and only compatible
authored clauses. Voice and gesture carry emotion; text carries exact lore.

## Stable persona record

Each NPC receives values before first contact:

- **initiative:** waits, signals, approaches, intercepts, or calls from cover;
- **courage:** avoids, watches, holds, challenges, or advances under danger;
- **caution:** open, measured, guarded, suspicious, or nearly unreachable;
- **affinity:** remembered personal conduct, not a visible affection bar;
- **standing:** what the camp or people know of witnessed conduct;
- **mercy:** willingness to aid strangers, wounded enemies, or defectors;
- **pride:** plain, reserved, boasting, competitive, or status-conscious;
- **need:** secure, strained, hungry, injured, hunted, bereaved, or desperate;
- **bond:** alone, paired, family, war-band, leader, dependent, or debtor;
- **purpose:** survive, defend, hunt, avenge, recover, study, trade, rebuild,
  pilgrimage, escape, or find a hearth;
- **role confidence:** novice, practiced, veteran, master, or diminished;
- **Odin stance:** faithful, dutiful, doubtful, resentful, defiant, or unknown.

These select expression and valid actions. They do not roll recruitment.

## Initiative before dialogue

| State | Visible behavior |
| --- | --- |
| welcoming | wave, open-hand point to fire, warm call; watcher stays posted |
| needy | repeated call, points to wound/threat/supplies, stays with dependent |
| wary | halt gesture, low-ready weapon, sideways spacing, short warning |
| proud | challenge/roar/weapon display, then waits rather than charging |
| hidden | watcher signals from cover; camp reveals itself after pause or aid |
| fighting | dialogue waits; NPC may signal target or safe flank |
| hostile-tolerant | orders distance, allows passage/trade, attacks on violation |

One speaker owns dialogue while watchers keep awareness. Camp members never all
wave, shout, and abandon the perimeter together.

## Line assembly

Opening: `[address] + [identity/origin] + [plight or offer] + [specific ask]`.

Later: `[belief] + [goal] + [boundary] + [availability]`.

Every component is tagged for tone, need, biome, origin, relationship, and
required facts. Contradictory clauses are rejected. A starving stranger cannot
boast of full stores unless deception is authored. A guardian of an injured
sibling cannot claim no obligations.

Intent families are pleading, selling, bragging, asking, warning, inviting,
refusing, and accepting. Selling temporary aid is not permanent recruitment.

## The yes/no invitation

After enough shared context, the player asks **“Will you come with me?”** The NPC
answers yes or no.

Deterministic blockers include hostility, betrayal, a dependent who cannot travel,
unfinished immediate duty, incompatible allegiance, no essential safe destination,
or a role-specific vow. Positive facts include shared defense, rescue, honored
warning, returned property, sanctuary, compatible purpose, prior standing, and a
demonstrated defended hearth.

There is no percentage, persuasion stat, gift spam, reload reroll, or hidden
random check. The answer stores its reasons for debug evidence. Ordinary UI shows
only the person's honest expression.

## Authored component bank

### Signals and greetings

- “Hold there. We saw your smoke before you saw ours.”
- “You are not the first raven-marked soul to cross this road.”
- “Weapon low, stranger. Ours will follow.”
- “The fire is small, but there is room if you bring no feud.”
- “Over here! Quickly—before they circle back.”

### Origins and beliefs

- “I woke beneath branches I had never seen, with my last battle still in my mouth.”
- “Odin remembered my spear. He remembered nothing of why I carried it.”
- “We answered the raven once. Now we answer one another.”
- “This is a dead land wearing the work of living hands.”
- “The hall behind us was old before any of us drew Second Breath.”

### Plights and asks

- “Our hunter has not returned. Help me bring them home.”
- “The dead rise whenever the mist thickens. Hold this fire until dawn.”
- “We can move the wounded or defend the stores. We cannot do both.”
- “That black-armored brute struck Fuling and traveller alike. It serves only the fight.”
- “Clear the burial house, but do not break the marked stones.”

### Boasts and offers

- “I have put a spear through a running Fuling at twenty paces.”
- “Give me a clear lane and nothing reaches your shield wall untouched.”
- “I know which ground carries weight beneath this snow.”
- “I can mend iron, but I will not forge chains.”
- “Point me toward the thing that thinks armor makes it brave.”

### Questions

- “Do you clear halls for homes, or only for what lies in their chests?”
- “When someone tells you no, does your hand stay off your weapon?”
- “Whose peace follows after your victories?”
- “Will your hearth take the wounded, or only useful hands?”
- “Are you Odin's hand, or your own?”

### Acceptance

- “Yes. My road and yours run together now.”
- “Yes—but when my people call, I answer them.”
- “I will come. Show me the hearth you mean to defend.”
- “You held when leaving was easier. I will do the same.”
- “My answer is yes. Ask no oath you would not swear beside me.”

### Refusal

- “No. Not while one of mine is still missing.”
- “No. You have strength, but I do not yet know what it serves.”
- “I will share the fire. I will not follow you.”
- “Not now. Make that village a home, then ask me again.”
- “No. I gave one god more battles than he deserved.”

## Nonverbal vocabulary

Approved short vocal categories: hail, warning, acknowledgment, question, effort,
challenge, alarm, pain, grief, relief, laugh, cheer, refusal, and assent. Each
identity receives a restrained subset.

Safe gesture candidates in current metadata include wave, point, challenge,
cheer, no, thumbs-up, bow, cower, cry, despair, flex, come-here, kneel, laugh,
roar, shrug, toast, rest, and relax. Candidate is not approved: each needs event,
cancellation, hand-lease, root-motion, body, and network checks. `KneelDeath` is
forbidden because its clip owns a death event.

## Writing constraints

- Keep first-contact lines under roughly two subtitle breaths.
- Never make every NPC a poet.
- Avoid fake Old Norse decoration and unexplained rune mysticism.
- Refer to witnessed facts whenever possible.
- Let people be funny, tired, vain, frightened, practical, or wrong.
- Never expose numerical personality, affinity, karma, or thresholds.
- Camp members may disagree without changing shared faction.
