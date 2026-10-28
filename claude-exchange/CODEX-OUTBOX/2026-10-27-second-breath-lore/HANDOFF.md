# The Second Breath lore and encounter-language handoff

Date: 2026-10-27

## Owner direction captured

Companions must have a lore-valid existence before recruitment. They appear in
leveled biome camps, recognize the player, signal, guard themselves, express
persona and plight, and answer one plain invitation with yes or no. Recruitment
is not a currency, affection, persuasion, or random-roll mechanic.

## Canon decisions

- **Living World canon:** people drawn from Midgard receive an embodied Second
  Breath in Valheim. They are dead-origin people who hunger, work, bleed, and die;
  they are not all einherjar.
- **Living World canon:** Odin selected for deeds and usefulness but misjudged
  consent and the full hearts of people.
- **Living World canon:** prior arrivals built the inhabited world seen in dreams
  and ruins before the player arrived.
- **Living World canon:** village draugr are bound/corrupted remains. Defeating
  them releases those dead; other survivors return to resettle the safe village.
- **Living World canon:** Hearthless/Hearthbound describe whether people have a
  defended home, not a race or permanent moral faction.
- **Design authority:** dialogue meaning combines nonverbal voice, safe gesture,
  posture, and short authored text components.
- **Design authority:** persona affects initiative and expression. Recruitment
  uses deterministic known facts and produces yes/no without a hidden roll.

## Faction reconciliation

The current Souls/RRR roster was inspected in the installed profile. The bible
maps every discovered friendly tier and the hostile Pillager, OrcBrute, Neckman,
Servant, WoodsSoul, NinoBosque, Rimebound, and Emberworn lineages.

“Eitrherjar Legionary” remains only a working implementation display name. It
should become **Idolbound Legionary** after owner approval because *einherjar* is
not a generic elite-warrior word. Former Forest Children become **Forest
Guardians** in story authority; runtime rename/migration is a later scoped change.

## Implementation boundary

This submission changes documentation and authored dialogue data only. It does
not alter prefabs, factions, spawn configs, UI, or runtime dialogue. Do not change
the live overlay names until Brian approves the proposed canon names and a save-
safe/display-only change is separately tested.

## Next implementation request

When current traversal and bomb priorities permit, define data contracts for:

1. stable persona record and remembered encounter facts;
2. camp speaker/watch roles and nonverbal first-contact state;
3. tagged dialogue component compatibility;
4. deterministic invitation answer with debug reasons;
5. tier-gated biome camp spawning and persistent encounter resolution.

No runtime work should invent numeric karma or persuasion simply because the
authoring repository includes affinity, standing, courage, and initiative.

