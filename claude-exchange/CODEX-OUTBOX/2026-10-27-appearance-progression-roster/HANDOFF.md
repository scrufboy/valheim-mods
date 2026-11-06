# Appearance, biome progression, roster, and release handoff

## Owner intent

Make Living World people visibly belong to their identity and biome: elf-like
people need credible ears/proportions, dwarfs must read as stocky adults, magic
users must visibly cast, and the first Plains specialist proof is a formidable
adult female spear hunter in coherent Plains-grade equipment.

Do not alter the accepted companion combat or traversal implementation during
this design/proof work. Do not rename established characters or reinterpret the
owner's descriptive shorthand as a change request.

## Appearance-provider finding

Balrond Character Customization 2.0.25 has Human, Alfir, Dverger, and Fuling
player morphology, multiple male/female body types, height/color controls,
race-owned ears, and equipment-fit work. The cached assembly is Player-oriented
and exposes no documented NPC API found by this audit. The LWC Human Lab copy is
currently disabled as `.old`.

Treat BCC as an optional appearance-provider candidate. Do not extract or ship
its assets, call private internals, or make it a hard dependency. Ask for a
supported non-player `Humanoid` API or build separately licensed Living World
appearance assets behind the same semantic provider contract.

## Party and progression decision

Separate all world people, known people, roster companions, residents, and the
active expedition party. Balance the authored campaign around two active field
companions: first slot after Eikthyr, second after the Elder, with later bosses
unlocking encounter/equipment tiers rather than more automatic combat slots.

Release from Party moves a persistent person to a home, reclaimed village,
allied/origin camp, guard, worker, visitor, or other meaningful life. It never
deletes the person or sends them blindly into unloaded wilderness.

## First presentation proof

Prepare an isolated test profile for the Plains spear huntress:

- compatible adult female human rig;
- mobile padded/Plains silhouette assembled from runtime-verified items;
- finite visible spear/javelin reserve and legal hand/back attachments;
- clear distinction from the heavier Plains pressure fighter;
- stable identity/appearance across save, unload, server, and remote client;
- full idle, traversal, combat, stagger, death, ragdoll, portal, and ship visual
  matrix.

Do not claim a black-metal spear exists merely because the design asks for its
tier. Name the exact verified mesh/item/attack source or label the need for an
actor-local licensed asset.

## Balance acceptance

Include hands-off same-tier raid and boss observations. Two companions must not
reliably complete the encounter while the player remains passive. Preserve real
ammunition, stamina, durability, interruption, injury, recovery, and missed
attacks.

