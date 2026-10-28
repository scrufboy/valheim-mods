# Offline Companions and Vikings for Hire: discovery and capability audit

Date: 2026-10-27

## Purpose and boundary

This independent Living World C.O.R.E. audit records public player-facing
behavior and architectural lessons. It does not authorize copying third-party
code, assets, dialogue, identifiers, balancing values, or encounter text.

Sources reviewed:

- [Offline Companions 1.2.0](https://thunderstore.io/c/valheim/p/ProfMags/Offline_Companions/v/1.2.0/)
- [Offline Companions Reborn](https://github.com/MaddhattrRocks/Offline-Companions-Reborn)
- [Vikings for Hire](https://www.nexusmods.com/valheim/mods/4057)
- `LivingWorldHumanNPC/COMPETITOR-AUDIT-VIKINGSFORHIRE-1.6.3.md`

Offline Companions Reborn is public under MIT at the reviewed repository.
Vikings for Hire was reviewed from its public feature description and the
user-supplied 1.6.3 binary already covered by the local audit. Living World
remains an independent implementation over Valheim/Jotunn and its own state
contracts.

## What each project teaches

### Offline Companions

Offline Companions proves that an ally can feel like a second player when
identity, inventory, equipment, food, stamina, skills, voice, home, work, rest,
death, recovery, and travel persist together. Its acquisition is primarily an
automatic first companion and additional trader purchase. Its strongest lesson
is continuity after recruitment, not world discovery.

Useful evidence includes contextual orders, home patrol, beds, fire-side idles,
work modes, finite inventory, equipment decisions, tombstones, respawn, and
ranged leading with gravity compensation. Reborn also documents combat spacing,
stamina reservation, melee openings, combinations, blocking, parrying, and
range-aware bow use.

Living World direction:

- preserve persistent identity, equipment, home, role, and recovery;
- keep contextual orders and believable domestic activity;
- adopt projectile prediction as a reusable service with bounded error;
- do not use a free starter companion or trader catalogue as the main fiction;
- do not replace accepted native `MonsterAI` combat ownership with another full
  movement/attack controller.

### Vikings for Hire

Vikings for Hire proves that peaceful wanderers and foreign settlements can make
recruitment part of exploration. Public descriptions show biome wanderers,
conversation-based hiring, coin or debt, traits, loyalty, work, settlement life,
guard posts, patrol routes, raid defense, and biome-influenced appearance.

Its current public design also gates one squad slot per defeated boss and exposes
a worldwide roster with remote dismissal. This directly demonstrates the balance
value of separating all owned people from the active squad, but its remote
dismissal/inventory return is not adopted as Living World fiction.

Living World direction:

- recruitable people exist before the player meets them;
- replace generic roaming with authored encounter families and field camps;
- show profession and class through activity before dialogue;
- connect wilderness recruitment to settlement reclamation and faction history;
- use trust, rescue, proof, shared combat, aid, or a lore-valid contract;
- never copy dialogue, traits, names, settlement pieces, or package identity.
- keep a bounded active expedition party even when the persistent world roster is
  much larger;
- release a companion into a persistent home, camp, or resident life instead of
  deleting them or making them run blindly into the wilderness.

## Living World's differentiator

Living World recruits are not products, random followers, or idle spawn points.
They are members of regional peoples with an origin, reason to be present,
relationship to local factions, practiced role, and visible task. The player
discovers a situation first and a recruit second.

The encounter must answer five questions without a menu:

1. Why is this person in this biome?
2. What are they doing before the player arrives?
3. Who or what do they regard as an enemy?
4. What proves their class in motion?
5. Why would they leave, remain, or join a reclaimed settlement?

## Required acceptance lesson

Discovery and persistence are separate systems. A camp may unload, resolve,
migrate, or be declined without creating a permanent companion. Once recruited,
stable identity, equipment, relationship, home, and state survive zone unload,
server restart, travel, incapacitation, and settlement assignment. World
encounter logic may never replace a saved person with a fresh roll.
