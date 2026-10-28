# Living World faction and peoples bible

Status: canonical replacement map for current Souls/RRR lineages. Internal prefab
IDs remain compatibility facts until migration; display names and story identities
follow this document after approval.

## Alignment is chosen, not biological

Species, body type, sex, and biome never determine morality. Faction records
describe current allegiance and permitted hostility. Individuals may defect,
refuse recruitment, tolerate a local people, or protect a mixed camp.

## One core, multiple world variants

Living World does not import a Souls NPC as one indivisible prefab-and-faction
truth. It preserves useful visual, equipment, attack, and role evidence, then
rebuilds approved variants on the Formidable actor core.

An archetype may therefore have:

- a hostile wilderness or raid variant;
- a neutral camp or unaffiliated variant;
- a friendly/recruitable variant when its origin supports personhood and choice;
- corrupted, bound, revenant, freed, or living presentations where lore supports
  those states;
- biome and tier variants that share capabilities without sharing allegiance.

Variants are separate content profiles with stable identities. They are not a
mid-combat morality switch and do not make every enemy recruitable. Friendly and
hostile people simply coexist in the world using the same reliable actor systems.

## Friendly and recruitable lineages

| Existing functional lineage | Living World place in the story |
| --- | --- |
| Starving Drifter / Outcast | newly arrived Hearthless; survival and dignity precede heroism |
| Wandering Protector | Meadows road and camp watcher; first Scout tier |
| Darkwood Stalker | Black Forest knife/atgeir/bow specialist |
| Troll Hunter | heavy forest pressure fighter; large-target rescue lineage |
| Swamp Survivor | Fen route keeper; spear/knife/bow mobility |
| Undead Slayer | grave-warden who distinguishes release from plunder |
| Mountain Nomad | mobile cold-route survivor and heavy axe lineage |
| Wolf Warrior | close mountain guardian; pack and hearth loyalty |
| Plainswalker | spear/bow outrider and thrown-spear foundation |
| Goblin Slayer | Plains heavy veteran; deed-title, not species hatred doctrine |
| Dwarf | stocky craft/axe lineage; a person and people, never comic relief |
| Mage / Necromancer | provisional labels; replace with specific rune, seidr, galdr, ward, memory, or death-lore traditions |
| Ashe, Dvorah, Eir | named legacy heroes requiring individual review; Eir must not casually claim the attested healing figure's identity |

Biome tier gates express survival credibility, not ownership. A Plains specialist
appears only after the world and player state can sustain a Plains encounter.
Earlier camps may speak of later peoples before they can spawn.

## Hostile and ambiguous peoples

### Pillagers (`RRRN_Pillager`)

Second-Breath humans who answered scarcity with predation. They raid camps,
reclaimed villages, and travellers because taking feels safer than rebuilding.
Some claim Odin rewards victory; others believe only in the next winter. Their
humanity matters: they can bargain, flee, boast, betray, or rarely defect. They
are not generic undead despite the transitional runtime faction value.

### Dark War-Lieutenant (`RRRN_OrcBrute`, final name pending)

Replacement role for “Krauguk Legions” and the misleading “Eitrherjar Legionary.”
This is not a Fuling-aligned people. The installed Souls actor shares the
`PlainsMonsters` combat faction with Fulings, which accidentally makes them ignore
one another. Living World treats that as inherited implementation behavior, not
lore.

The replacement is a solitary or sparsely deployed battlefield lieutenant: a
war-made destroyer that enters existing conflict, drives pressure, and attacks
whoever remains. It does not require soldiers, a nation, or lasting allegiance.
Its heavy armor, mixed brutal weapon loadout, high battlefield presence, and
lieutenant behavior are preserved from the Souls profile while its relationship
matrix becomes deliberately hostile to Hearthbound, player, Fuling, Pillager,
Guardian, and rival hostile forces. Temporary target priority is tactical, never
proof of friendship.

Its origin and life-state remain open for final naming: oath-broken Second-Breath
champion, undead or revenant war-captain, Forsaken-made enforcer, or a person
hollowed into an office of war. Do not decide that it is living or undead merely
from the old `PlainsMonsters` value or its model. If the approved origin supports
both, the core may instantiate a hostile corrupted lieutenant and a distinct
living/freed counterpart with different identity and allegiance profiles.

`Einherjar` remains reserved for Odin's chosen battle-dead and must not be used as
a generic elite title.

### Fen Draugr (`RRRN_Neckman`)

Swamp-bound revenants and grave-keepers altered by fen rot. Some guard passages or
burial wealth; others were defenders whose final command outlived everyone it
meant to protect. They are released, not recruited as a population.

### Myrkvidr Thralls (`RRRN_Servant`)

Bound Second-Breath dead or revenants compelled by a forest power, oath-holder,
or corrupting will. “Thrall” describes bondage rather than guilt. Destroying the
master, token, or binding may free a specifically authored individual. Myrkvidr
is a dark-wood poetic association, not proof of one literal cosmological faction.

### Forest Guardians (`RRRN_WoodsSoul`, `RRRN_NinoBosque`)

Replacement identity for the former Forest Child names. They are small, ancient-
presenting wardens shaped by the memory of woodland and mountain places, not human
children. The green guardian carries poison/rot expression; the blue guardian
carries frost/ice expression. Hostility depends on violation, allegiance, and
corrupted stewardship.

### Rimebound (`RRRN_LWC_Rimebound`)

Second-Breath adults or transformed people who survived by binding body, gear,
and oath to cold. Some bands are hostile, some isolationist, and some guard
northern roads. The existing approval actor is hostile; the people are not
universally evil.

### Emberworn (`RRRN_LWC_Emberworn`)

Ash-adapted adults marked by heat, smoke, and long exposure to southern war. The
hostile approval actor is one allegiance, not a racial verdict. Friendly
Emberworn require survival-grade presentation and a reason to oppose local powers.

## Village dead and returning settlers

Village draugr are corrupted remains of prior inhabitants and occupiers. Clearing
them releases those remains. Reclaimed residents are Hearthless survivors who
return because the site was made defensible. A village may include humans, dwarfs,
elves, and regional peoples when faction and origin permit it.

## Relationship matrix rules

Every faction definition must state default relationships to the player,
Hearthbound, Pillagers, war-lieutenants, Fulings, Guardians, draugr, Rimebound,
Emberworn, and Forsaken servants; whether reputation is individual, camp, people,
or world-wide; what witnessed acts change it; how combat may end; and which
relationship survives recruitment or village assignment.

No relationship may be inferred only from Valheim's transitional
`Character.Faction` enum. That value implements filtering; Living World allegiance
is richer and persists independently.

## Naming rules

- Prefer descriptive English when an Old Norse form is uncertain.
- Use attested terms with their attested sense or label a project coinage.
- Never use *einherji* merely to mean elite.
- Avoid modern fantasy race morality.
- Titles are earned deeds, offices, conditions, or allegiances.
- Preserve internal prefab IDs until save migration proves replacement safe.
