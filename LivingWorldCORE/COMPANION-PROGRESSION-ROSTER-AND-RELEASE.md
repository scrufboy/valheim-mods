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
