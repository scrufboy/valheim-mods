# World companion discovery and specialist class handoff

Date: 2026-10-27

## Intent

Translate the current owner direction into repository design authority for how
companions are discovered in the ordinary world, how biome class pairs differ,
and how bombs, spear throws, knife lunges, heavy attacks, magic, encampments,
raid response, and reclaimed-village migration fit the accepted Human NPC state
contract.

## Governing decisions

- SOURCE-INSPECTED: the current Human NPC line keeps native `MonsterAI` as combat
  executor and uses a moving party anchor for tactical bounds.
- SOURCE-INSPECTED: Bow-first and close-first are preferences of one adaptable
  Scout class, not separate classes.
- DESIGN-AUTHORITY: wilderness recruits are found through active biome-native
  camps and situations, not primarily automatic grants or a trader catalogue.
- DESIGN-AUTHORITY: every specialist class is a composition of reusable
  capabilities and actor-local loadout data.
- DESIGN-AUTHORITY: bombs are shared thrown-projectile infrastructure; Sapper is
  one role that uses it, not the only actor permitted to carry a bomb.
- DESIGN-AUTHORITY: the first post-bomb spawnable pair is the Plains spear hunter
  and a heavy pressure fighter.
- DESIGN-AUTHORITY: knife lunge and magic follow only after their prediction,
  animation-event, cancellation, networking, and resource contracts are proven.

## Authoritative documents

- `LivingWorldCORE/WORLD-COMPANION-DISCOVERY-AND-BIOME-ROLES.md`
- `LivingWorldHumanNPC/SPECIALIST-COMBAT-CAPABILITY-PLAN.md`
- `research/COMPETITOR-AUDIT-OFFLINE-COMPANIONS.md`
- `LivingWorldCompanionSystems/README.md`

## Art included

- `art/brand/LWC-OBS-Intro-Key-Art-v1.png`
- `art/concepts/npc-classes/LWC-Plains-Spear-Huntress-Concept-v1.png`

The spear image is concept reference, not a runtime-ready or licensed Valheim
asset declaration.

## Implementation order

1. Finish and accept traversal/settled-player collision safety.
2. Build pure projectile prediction tests and one finite native bomb path.
3. Prove owner-authoritative multiplayer release and inventory debit.
4. Reuse prediction for a Plains thrown spear.
5. Validate one heavy native attack set.
6. Build one Plains field-camp encounter and its raid/recruitment transition.
7. Add knife lunge, then bounded magic candidates.

Do not implement all biome classes at once. Preserve the current protected
voices, shouts, stagger, death, formation, moving-anchor defense, doors, torch,
cleric, persistence, and native-combat proof at every gate.

