# Neck Marauder and undead Colossus: authorized creature work

Owner: Brian. Date: November 3, 2026. This is an explicit execution work order,
superseding the earlier frog-audio research-only scope for these named tasks.
It does not authorize unrelated roster changes or public publication.

## Deliver two additive creatures in the existing Souls test stack

**Neck Marauder:** the acquired Frog Marauder body, retaining its original helmet,
axes and compatible humanoid rig, becomes a lore-authored swamp Neck relative.
Use native Fuling/Goblin animation events, protest/chatter/alert/attack cues and
MonsterAI for this phase, with frog/Neck voice rather than Fuling speech. Native
Neck sounds are an approved immediate fallback and a preferred part of the sound
identity. Preserve existing network prefab IDs for already installed prototypes;
change the display/lore name without silently rewriting saved actors.

- Bounded groups of two or three; swamp/crypt surroundings and verified selected
  crypt interiors. Add distinct entries; do not replace skeletons or old Souls NPCs.
- Inspect and reuse the actual native skeleton emergence/wakeup mechanism:
  sunken ambushers rise from below when alerted. Do not guess a controller trigger,
  make every creature float, or allow buried actors to attack/move prematurely.
- Hostile to players and Leeches, neutral to Necks; preserve the existing scoped
  Skeleton hostility and Draugr retaliation policy unless a later explicit rule
  changes it. Leech targeting is hunting behavior, not proof of an implemented
  digestion/hunger/needs simulation.
- Fuling weapon/combat behavior and a bounded native-compatible bomb option.
  A larger atgeir/brute Marauder is an optional design variant, not required to
  complete this first slice. No extra combat or movement controller beside native AI.
- Neck-like worn swamp coloration, possible yellow mucus-like glow and compatible
  armor are art options. Extract originals and UV/material guides so Brian can
  paint them. Do not replace authored texture depth with a generic pattern.

**Undead Colossus:** the acquired Andean Colossus becomes a distinct large swamp
undead creature. Use native Fuling Berserker combat/attacks on a verified compatible
rig and native Draugr/zombie voice as the initial fallback. Preserve its real visual,
weapon and an equivalent authored corpse. Extract the Colossus maps alongside
Draugr Elite references; its foreign UV layout requires deliberate repainting or
reprojection, not an unchanged vanilla atlas blindly applied to the model.

Both actors must retain their authored appearance through animation, equipment,
effects, death/ragdoll and despawn. Scope species relations and spawn lists to
these identities. No global Fuling, Neck, Draugr or player replacement.

## Audio coordination for Claude and Cursor

Authoritative existing research branch:
`scrufboy/valheim-mods:research/swamp-frog-audio`, including
`research/swamp-frog-audio/FULING-CUE-MAP.md` and the November3 audio outbox.
The first supplied handoff contains research, slot proposals and tools. The newer
research branch points to a separate local candidate ZIP; root verified593 audio
files (548 cut clips,14 pitched demos and31 source recordings), about43 MB, at
C:/Users/scruf/Downloads/LWC-swamp-frog-audio-candidates-2026-11-03.zip.
Those recordings exist and require listening; they are not yet an accepted voice pack.

Build a candidate library mapped to actual native events: idle/chatter, alert,
attack effort/protest, hurt and death, with optional investigate/respond/exclaim
where a real runtime event exists. Use frog/toad recordings selected by listening;
Neck clones/pitch layers can supply the in-game identity immediately. Optional
frog/Neck hybrids require actual audio inputs, not a fabricated "AI generated"
claim. Do not ship extracted vanilla audio; resolve native clips at runtime or
apply the mix there. Record source URLs, exact files/hashes, edits and attribution
for external recordings. Prefer readily reusable sources to keep the first test
moving. Zombie candidates need the same provenance and listening review.

Research helper/API compilation and synthetic tests are not real recording
downloads or subjective listening acceptance. Report those stages separately.
Do not ask Brian to decide technical slot names or enter a character/prefab ID.

## Deployment, raids and boundaries

The immediate destination is the existing **Souls of the Land** profile already
containing the private Marauder prototype and updated Souls work. Inventory and
back it up first. Root coordinates builds, registrations, Tooler raid definitions
and reversible profile transactions after owned native QA. Preserve old encounters,
NPC identities, drops and source packages; add the two creatures rather than
silently substituting them for existing Legion/Pillager/Neckman types.

Add deliberate Tooler test encounters/raid entries for these registered prefabs
without enabling unbounded natural spawns. Test separately, then together in the
selected Souls test stack. Do not deploy experiments into Formidable Arena or
the combat/telemetry baseline. No automatic main-world launch or save conversion.

Native gates: matching Unity build, actual prefab/item registration, animation and
weapon/hand alignment, bounded group emergence/alert behavior, native AI damage,
scoped relations, correct own corpse and drops, no player/vanilla asset mutation,
and normal shutdown/restoration. Installed-profile acceptance, audio quality,
terrain/interior placements, save/reload and multiplayer remain separate evidence.

FormidableAI migration is later. This phase deliberately uses native MonsterAI
inside the Souls/RRR-compatible test stack and does not claim the Workbench now
automatically exports complete AI creatures from any imported mesh. Frozen
Workbench0.5.19 RC2 and Papa1.2.3 remain unchanged.

## Subsequent owner detail: a living swamp defence

The Sunken Marauders are swamp dwellers with a purpose, not a generic raid wave.
Use more activity around crypt-bearing swamp areas, selected crypt interiors
and entrances. Some hunt Leeches or wander; others wait below ground. A nearby
shaman/lookout can alert existing Marauders and Neckmen, who rise and join the
same fight. This local alarm needs a bounded radius, response cooldown and
responder cap; it must not create an endless wave or relay without limit.
Existing species relationships, identity, native combat and network ownership
remain explicit. The shaman near old-world carved landmarks and the captain-like
rally are authored roles, not permission to alias existing characters. The exact
requested wooden landmark is not yet identified: the catalog has goblin_totempole
and RuneStone_Swamps candidates; do not guess an Elder altar or global spawn point.

The Colossus is rare and almost boss-like, suitable for a deliberate swamp/
Bonemass-themed raid or Tooler encounter rather than a common natural spawn.
Brian also requested a visible mining pickaxe carried on Marauders' backs, not
a pickaxe drop, and support for compatible clubs, spears, shields and Fuling
weapons. Preserve the original two-axe variant. Iron scraps are a possible loot
idea, not a reason to invent an unbounded reward or replace current loot silently.

Reclamation's maypole/town/repair/rescued-hero vision is the broader foundation.
Dedicated chats now separate Reclamation planning and Gale interoperability
research from this live creature implementation. "Maybe incorporate Gale" is
research authority, not installation or replacement of r2modman. Preserve all
existing Guard, Archer and Scout identities. The Workbench remains frozen.

## Assigned ownership (implementation)

Root: scope/Git, audio-source intake, Unity batch export, integration/builds,
native launch semaphore, profile/raid installation and final evidence.
Marauder agent: Marauder runtime/emergence/weapon changes and focused probes.
Colossus agent: new additive Colossus runtime/rig/corpse and focused probes.
Asset agent: data-only source skin extraction/catalog, UV/material/Photoshop guides.
Claude/Cursor: audio sourcing/listening and their requested exchange handoffs;
no concurrent production, profile or Workbench changes without an assigned file.
