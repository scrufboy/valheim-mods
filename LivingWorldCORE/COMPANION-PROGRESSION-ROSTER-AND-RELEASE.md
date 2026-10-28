# Companion progression, roster, field-party, and release design

Status: design authority. This document does not modify the accepted traversal or
combat implementation.

## Core rule: meet many, travel with few

Living World may contain many persistent people without allowing the player to
march through Valheim behind an army. Discovery, settlement population, personal
roster, and active expedition party are different counts.

- **World people** live in camps, routes, foreign settlements, and reclaimed
  villages whether or not the player recruits them.
- **Known people** remember the player and may trade, offer work, refuse, relocate,
  or later become available.
- **Roster companions** have accepted an ongoing relationship with the player but
  may be resident, resting, recovering, holding an outpost, or available.
- **Active expedition companions** are the only roster members who follow the
  player across the world and contribute to ordinary field combat.

The default active expedition limit is **two companions**. One slot becomes
available after Eikthyr and the second after the Elder. Further boss defeats open
new encounter tiers, equipment credibility, resident capacity, and role choices;
they do not add an unlimited combat slot every time.

The limit is a balance default and server-authoritative rule. A future explicit
configuration may change it, but the authored campaign, encounters, and acceptance
tests balance around two. A full party does not erase a newly discovered person:
the player may invite them to a safe settlement, remember their camp, or exchange
an active companion after reaching a lawful transition point.

The default **bonded companion roster limit is six living people**, including the
two active expedition assignments. Known people and ordinary residents are not
owned companions and do not consume this bond limit. A seventh person cannot be
recruited by building another bed or opening a menu: the player must first release
an existing bond at a safe home/camp or live with the consequences of a permanent
death. The value may be server-configurable later, but six is the authored balance
ceiling and two remains the field-combat ceiling.

## Debug spawning is not recruitment

The current pause/console spawner is a development instrument. It can create
specific tiered test actors on demand precisely because live tests need repeatable
equipment and behavior. That is cheating if exposed as campaign acquisition.

- every tool-spawned actor is marked as a lab/debug identity;
- a debug actor cannot enter the campaign roster, satisfy a discovery, unlock a
  class, become a resident, or survive export as a legitimate recruit;
- release packages hide or exclude spawn controls unless an explicit developer
  mode is enabled;
- no campaign UI offers biome, class, sex, equipment tier, or instant replacement
  spawning;
- campaign companions originate only from persistent world encounters.

The test spawner remains useful and unchanged for development. It is never the
fiction, cost, or delivery system for Living World companions.

## Progress follows Valheim rather than bypassing it

Companions never manufacture progression the player has not earned. Their spawn
tier, equipment eligibility, upgrade stations, food credibility, resistance, and
special capabilities are gated by world keys and the material/station access the
player has actually established.

| World progress | Encounter and roster consequence |
| --- | --- |
| Before Eikthyr | people may be seen and helped; no permanent expedition slot |
| Eikthyr defeated | first Meadows-capable expedition companion may join |
| Elder defeated | second and final default expedition slot; Black Forest roles and materials become credible |
| Bonemass defeated | Swamp specialists and iron-tier upgrade paths become credible |
| Moder defeated | Mountain specialists and silver/cold-survival paths become credible |
| Yagluth defeated | Plains specialists, including the spear hunter/heavy pair, become recruitable under the approved world-key policy |
| Queen defeated | Mistlands staff, crossbow, carapace, and eitr-capable roles may become credible after capability proof |
| Later lethal biomes | Ashlands/Deep North encounters require established veteran identity and survival-grade presentation; they are not casual recruits |

The exact world key for discovering versus recruiting a Plains camp remains a
balance test item. The principle is fixed: seeing evidence of later people may
precede recruitment, but hiring them cannot hand the player an unearned biome win.

## Recruitment investment and cadence

The final question remains a plain yes or no; the investment happens in the world
before that question. A recruitable person must be found at a valid persistent
encounter. The player must understand and resolve that encounter's concrete need:
shared defense, rescue, provision, proof of safe shelter, completion of an owed
task, recovery of a person or possession, or another authored obligation. A valid
home/bed and an open bonded-roster seat are required before a permanent yes can be
accepted.

Recruitment does not print endgame equipment. Personal starting gear is part of
the NPC's identity and is tier-bounded; continued service costs food, ammunition,
repairs, replacement equipment, safe lodging, and the risk of losing the person
and what was invested in them.

Only one unresolved recruitable camp lead may be active for a player/party. After
a recruitment, refusal, abandonment, or companion death, a new lead cannot become
eligible for **five in-game days**. Eligibility is not delivery: the player must
then read a new rumor/sign and search an unexplored, biome-valid area. No timer
spawns a replacement beside the player, and no dead person's class is guaranteed
to be the next one found. Five days is the initial authored tuning value and must
be exposed for later balance testing without becoming a zero-cost production
default.

## Equipment and power economy

A recruit arrives with a coherent survival loadout, not a free chest of boss-tier
loot. Personal equipment is identity-bound while worn and cannot be stripped as a
progression exploit. Upgrades require legal stations, materials, knowledge, and an
authored transaction. No silent level scaling creates armor or weapons.

Combat contribution must preserve player agency:

- native stamina, ammunition, durability, recovery, line-of-fire, and attack
  commitment remain real constraints;
- companions may miss, be interrupted, become injured, retreat, or require rescue;
- role coordination creates openings for the player rather than guaranteeing
  kills;
- two companions must not reliably defeat a same-tier boss or full raid while the
  player remains passive;
- boss and raid acceptance includes a deliberate **hands-off observation** to
  measure whether the party is playing the game by itself;
- encounter difficulty may account for active expedition strength, but it must not
  inflate every peaceful resident into player-count scaling.

New biome companions provide different answers—throwing lanes, pressure control,
wards, healing, knife pursuit, heavy stagger—not a vertical stack of followers.

## Mortality: there is no Third Breath

The Second Breath is the lore explanation for embodied life in Valheim, not an
infinite resurrection service. Companions can be healed while alive. Once actual
death is committed, it is permanent.

- zero-health/death confirmation ends active and bonded assignment permanently;
- reload, recall, rest, a bed, a cleric, a console command, or party recovery may
  not recreate that identity as living;
- the death record persists with name, deeds, relationship, class, appearance,
  place, time, and cause so the world may remember the person;
- carried equipment follows a real corpse/tombstone/recovery transaction; recovery
  of items is not recovery of the person, and nothing is mailed magically;
- the open roster/party seat is not an instant replacement token. The five-day
  lead cooldown and world search still apply;
- a later NPC with the same role is a new person with a new history, not a reroll
  or respawn of the dead companion.

No downed/revive system may quietly convert death into a harmless timeout. A
future **pre-death** wounded state is permissible only if it is clearly timed,
interruptible, dangerous to rescue, and ends in permanent death when it fails.
Healing cannot target a committed death. Permanent mortality must remain disabled
until traversal, lava/water safety, network ownership, and death attribution are
live-proven; the player must not lose a companion to an uncorrected follow bug or
duplicate death callback.

## Release from active service

The ordinary command is **Release from Party**, not fire, delete, abandon, or
dismiss into nothing.

1. The companion acknowledges the end of the current expedition assignment.
2. Combat, incapacitation, sailing, portals, and unsafe terrain block the transfer
   until a safe transition exists.
3. The player chooses an established home, reclaimed village, allied camp, or the
   companion's surviving origin camp when more than one is valid.
4. Personal gear remains with its owner. Player-loaned items are resolved through
   an explicit inventory exchange; nothing is duplicated or mailed magically.
5. The companion becomes a persistent resident, guard, worker, visitor, or
   independent known person and continues having a life.
6. Relationship, history, injuries, equipment, class growth, and availability are
   retained. Rejoining later is a reunion, not a fresh spawn.

If no safe home exists, the companion holds at the nearest valid camp or travels
with the player only until a lawful handoff location is reached. They never run in
a straight line into unloaded wilderness. Permanent exile or severing the
relationship, if ever added, is a separate serious dialogue choice with explicit
consequences; it is not the normal way to free a party slot.

## Lessons from other companion projects

Vikings for Hire uses boss-gated squad slots and a worldwide roster. Its public
description allows remote dismissal and returns the NPC's bag to the player.
Offline Companions emphasizes persistent home, Follow on/off, jobs, beds, and
identity rather than a clear removal ritual. These are useful evidence, not
Living World behavior to copy.

Living World keeps the useful roster and home concepts but rejects disappearance
as the fiction. A person leaves active service by going somewhere meaningful.

## Persistence record

Every known or recruited person needs stable fields for identity, origin camp,
current home, relationship, availability, active-slot assignment, role, visual
recipe, personal and loaned equipment, injuries/recovery, promises, and last safe
world position. Changing party status never changes identity or rerolls appearance.

## Required acceptance scenarios

1. Attempt to recruit with both expedition slots occupied; nobody disappears and
   no third follower leaks into combat.
2. Release at a reclaimed village; the person becomes resident and survives
   reload, zone unload, and server restart.
3. Release with no valid home; safe handoff is required rather than wilderness
   deletion or blind running.
4. Exchange a Meadows companion for a Plains specialist; both identities,
   inventories, relationships, and homes remain intact.
5. Observe two correctly equipped companions against a same-tier raid and boss
   while the player does nothing; tune until passive victory is not dependable.
6. Verify residents do not count as active expedition strength and cannot be
   recalled remotely to bypass the two-slot limit.
7. Spawn a lab actor and prove it cannot become a legitimate campaign recruit or
   satisfy any roster/discovery record.
8. Kill a campaign companion and verify permanent identity closure, one legal
   equipment-recovery transaction, memorial persistence, slot release, five-day
   lead delay, and no reload/recall/revive path.
9. Fill all six bonded seats and prove a seventh recruitment returns a truthful
   no until a living bond is safely released or a death is permanently recorded.
