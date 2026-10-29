# External appearance capability research

Date: 2026-10-27

## Purpose and boundary

This audit asks whether Balrond Character Customization (BCC) can help Living
World people look like elves, dwarfs, and visually distinct regional characters.
It does not authorize extracting, redistributing, modifying, or reverse-engineering
third-party assets beyond compatibility inspection. BCC remains Balrond's work.

Sources inspected:

- [Balrond Character Customization 2.0.25](https://thunderstore.io/c/valheim/p/Balrond/balrond_character_customization/);
- locally cached 2.0.25 package, manifest, README, changelog, and assembly metadata;
- the disabled `.old` copy in the `LWC Human Lab v1` profile;
- current Living World human factory, presentation, and persistence contracts.

Local 2.0.25 DLL SHA-256:
`2A2AD6352024A3C18C2F598A7BE4872D3A01351EF9334326DB12F3950F36C880`.

## Proven package capabilities

BCC 2.0.25 provides four player race/morphology presets: Human, Alfir, Dverger,
and Fuling. It also provides Male2–Male6 and Female2–Female6 body types, authored
race proportions, height adjustment, RGB skin/hair/eye color, hairstyles, tattoos,
custom-race equipment fitting, and automatically created race-owned features such
as ears.

Assembly metadata confirms implementation paths named `CreateRaceEars`,
`ApplyRaceBoneMorphs`, `ApplyDvergerTuning`, `ApplyRaceVisuals`, race/body/height
ZDO keys, and multiplayer appearance revision state. It also shows that the
documented implementation is strongly **Player-specific**: persistence and
application paths repeatedly target `Player`, player custom data, character
creation, and the Barber Station.

The LWC Human Lab profile contains the package only as `.old`; it is not currently
an enabled dependency of the accepted companion build.

## What is not yet proven

- no documented public API has been found for applying a BCC race recipe to an
  arbitrary non-player `Humanoid` or Living World prefab;
- the package description does not grant Living World permission to redistribute
  its embedded ear, body, hair, tattoo, or other model assets;
- a player-only appearance patch may ignore an NPC, repeatedly overwrite it, or
  assume player save/UI state that an NPC does not own;
- custom proportions may clip armor, alter attachment placement, or disturb
  animation, bow, spear, staff, mounted, stagger, death, and ragdoll presentation;
- Xbox/crossplay clients without the same visual asset owner cannot be assumed to
  render these features.

## Approved integration strategy

BCC is an **optional appearance provider candidate**, not a hard runtime
dependency and not an asset source to copy.

1. Ask Balrond whether BCC supports or may support non-player `Humanoid` recipes
   through a stable public API.
2. If an API is available, let BCC retain ownership of its meshes, materials,
   proportions, extras, and fitting. Living World stores only stable semantic
   choices such as `alfir`, `dverger`, body presentation, height band, and colors.
3. Detect BCC softly. Absence must select a Living World-owned fallback appearance
   without breaking identity, combat, saves, or networking.
4. Never call private internals or extract embedded assets for release.
5. If there is no supported NPC path, create or commission separately licensed
   Living World ear/proportion assets and register them through Formidable's
   appearance-provider contract.

## Required proof spike

Use an isolated visual test actor, never the accepted companion prefab first.

1. Spawn one male and one female test actor for Human, Alfir, and Dverger recipes.
2. Verify local host, remote client, save/reload, zone unload/reload, and late join.
3. Equip representative light, heavy, padded, cape, hood, helmet, bow, spear,
   shield, two-handed axe, and staff sets.
4. Exercise idle, walk, sprint, swim, sit, crouch, block, every approved attack,
   stagger, death, ragdoll, portal, and ship traversal.
5. Record ear/headgear clipping, hand and back attachment alignment, foot contact,
   hitbox/collider independence, and animation deformation.
6. Prove fallback appearance with BCC absent before any content profile depends on
   the provider.

## Living World appearance recipes

Race morphology, biome, class, and morality are separate fields. An Alfir body is
not automatically a mage; a Dverger body is not automatically an axe fighter; a
Plains specialist is not automatically a Fuling.

| Content proof | First visual recipe to test | Non-negotiable read |
| --- | --- | --- |
| Alfir/elf person | BCC Alfir if supported; otherwise LWC-owned ear extra on the compatible human rig | ears and proportions remain visible without destroying helmets, hair, or animation |
| Dverger/dwarf person | BCC Dverger if supported; otherwise LWC-owned compact morphology | shorter, stockier adult—not a scaled child or comic caricature |
| Plains spear huntress | female human-compatible rig, mobile padded/Plains palette, finite spear reserve, practical cape/cloth choices | fast veteran hunter; weapon and armor belong in the Plains and remain readable in motion |
| Plains heavy | adult human-compatible rig, heavier padded/black-metal silhouette, proven two-handed set | pressure fighter distinct from the spear hunter without becoming an oversized monster |
| Magic user | identity-appropriate morphology plus one proven staff/hand-effect school | magic is shown by equipment, cast language, and bounded VFX—not ears alone |

The existing Plains spear-huntress art is a composition target, not evidence that
the exact armor, spear mesh, ears, or animation already exists in the runtime.
