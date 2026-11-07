# LWC swamp temple and Mirekin family - owner design update

LATEST OWNER EXECUTION ORDER - November 7: full Souls roster, ground creatures and native FormidableAI tests first. ONE composite mounted creature with hit-region-based single-survivor replacement supersedes the older two-actor stacking section below. See the Formidable execution update and valheim-mods/FormidableNPCFramework/DEVELOPMENT-2026-11-07.md. The existing red Neckman Shaman is visibly opened/animated in Workbench0.5.28; it is not a completed game actor. All Behemoth lore remains intact.
Date: 2026-11-07 (America/New_York).
Authority: Brian's current swamp, goblin, spear and documentation directions.
Status: design requirements and proposed names; no creature build, profile installation, world migration or gameplay acceptance is established by this document.

## Superseding owner correction - creatures and ground factions first
Brian's follow-up on November 7 controls the order of work. The large world/Bonemass dungeon is a future hurdle, not the next implementation task. Retain undead in the swamp and around Bonemass; reducing repetitive encounters never means deleting Draugr or overriding the native boss's summons.
Immediate scope: create the swamp creature families, their ground factions and cohesive lore, then test actual creatures. Individual goblin-derived blowgun fighters come first; spear roles and shamans follow from inspected assets. Dynamic stacking is later. Existing neckfolk and goblin-derived small creatures are distinct variants within the evolving swamp story, not an approved replacement of every established actor.
All established Behemoth lore remains right and is retained intact. The new story incorporates it; it does not supersede the fallen-hero, hoard, enslavement/binding or existing creature history.
Brian's LWC origin: these people were summoned/brought here, did not want Odin's assigned task and tried to live and survive. They appealed to their own gods for protection. Odin would not accommodate their rival worship and let the swamp have them. They received protection through becoming the swamp: transformed into its creatures, including the neck variant and the goblin-derived variant. This is Brian's LWC story, not a claim about Iron Gate's canonical Bonemass origin. Exact deity names and extra origin events are not invented here.
Frog Marauder is conditional: retain source and test whether it works; Brian doubts its fit and allows reconsidering it. Do not delete it or call it accepted.
Brian reports the Bear broken in-game and in the tool, with lost animations, and rejects the Troll. Preserve these as unresolved owner-reported regressions; historical compile/probe passes do not override them. Do not certify a diagnosis without reproducing the actual imported rig/controller/clip and installed-package paths.
Brian praises the Behemoth as scary and good. Preserve that accepted appearance and established lore. Remaining work includes beard motion or an explicitly approved removal, club attack completeness and reliable roll/dive recovery. Stick with clubs now. Equip/unequip, club/non-club variants, rock throws, double smash and fist attacks were discussed; do not call the optional ideas implemented or let them displace the current club priority.
Personal Workbench delivery remains a versioned portable build, verified desktop shortcut and visible launch of the current version when using it. The Windows installer is a separate friend-delivery workstream; it does not replace Brian's portable workflow. This documentation pass has built/launched neither. Background source edits are legitimate preparation, but are not a delivered app, demonstrated authoring operation or gameplay proof. Existing permissions/ownership and save/profile protections still apply.

## Names and identity
Recommended working family name: **Mirekin**. This is an original English LWC name, not an attested Old Norse term, Iron Gate species name or owner-approved final name.
- Mirekin Dartstalker: tiny individual blowgun fighter.
- Mirekin Spearhand: individual spear fighter; equipment follows authored material tiers.
- Mirekin Fleshweaver: grotesque shaman directing body-work and bound undead.
- Mirekin Shoulder Pair: descriptive encounter name for two mounted living individuals, not a separate species or a permanent fused body.
- Draugr Behemoth: retain the established creature identity. "Fleshweaver-bound" describes an encounter relationship; do not rename the existing prefab or replace its approved gameplay.

Mirekin are adult monstrous swamp inhabitants. Tiny stature does not imply children. Do not automatically rename or replace the original Souls Neckman, existing Neck Marauder, other neckfolk, or their factions. Decide their exact relationship through later owner review.

## Bonemass is unchanged
Preserve Bonemass's established origin, model, AI, attacks, health/damage policy, summoning requirements, drops, trophy, boss progression and world keys. No invented Gathering origin, body-building origin, rewritten covenant, new boss identity or substitute boss.
Replace Bonemass surface altar locations with temple complexes in the intended new-world configuration. Preserve their boss-discovery destination relationship. These are selected boss-location swamps, not a temple in every swamp. Distinguish location placement from an already summoned live Bonemass.
The temple leads through an apparent broken floor, walkway or stairs into a separate dungeon interior. A visible spiral return stair ends at an explicit exit interaction to the correct exterior entrance. The Queen-lair comparison concerns arena scale and interior presentation only.
Build a cavernous, muddy sunken swamp/crypt interior, with black pools, burial architecture, refuge routes, escape loops and enough clearance for the unchanged boss, minions and attacks. Host the native summoning encounter there. Interior relocation must prove combat, navigation, damage, environmental effects, escape/re-entry, death recovery, boss discovery, drops and progression. A visual swamp set does not prove real biome/environment behavior.
Fresh worlds are the recommended experience. Existing-world support is a separate scoped migration: explored/unexplored locations, player structures, discovered markers, defeated bosses and existing entities must be considered. Do not run global regeneration or destructive reset commands on owner worlds.
JereKuusela's Expand World Data and Upgrade World are research candidates, not an installed or validated dependency decision. Bought temple/ruin assets may supply rooms and landmarks after actual source inspection. Author modular geometry/connectors and validate assembled layouts; no claim that AI has generated a working giant dungeon.

## Swamp encounters and LWC story
Reduce repetitive surface Draugr encounters and establish neckfolk/Mirekin populations, shamans and ruins. Draugr remain present around and within temple imprisonment/experiment areas. Exact population and allegiance rules require authored scope; do not change every vanilla Undead relationship.
Brian's direction is a freakish shaman and their undead Draugr beast. The shamans bind, enlarge, cut and assemble experimental undead. Preserve the existing Behemoth fallen-hero/hoard/enslavement history intact; this story incorporates that history, not a replacement origin for the Behemoth, Bonemass or all Draugr.
Art targets: a large stone worktable, a once-ragdolled then frozen Behemoth study pose, local gashes/gouges and separate anatomical remains as authored props. Preserve the live original and its rig. Corpses and props must not run hidden actor AI or duplicate loot.
"Trying to give Bonemass a body" was Brian's tentative speculation, not accepted lore or authority to change Bonemass. Face trophies from rivals/ascension are likewise an optional disturbing mask-story proposal, not Diablo canon or a settled LWC practice.
Acts, lesser bosses, trinkets and access progression remain design exploration. Do not invent a required new item gate that changes native Bonemass summoning without explicit approval.

## First delivery: individuals
First build a tiny standalone Goblin Scout-derived blowgun actor. Verify source import, rig/clips, walk/run/turn, aim/fire, real projectile origin, hits, death, drops and save/reload before adding paired behavior.
Brian reports acquiring Goblin Scout1 (Unity Asset Store 393680) and Low-poly spears (66326). Purchase is reported, not verified local bytes. Record actual source paths, version and hashes when available. Do not substitute the earlier gray minion or Red Neck Shaman preview for a completed purchased Scout import.
Provide body/height variation through supported derived assets and reusable recipes. Preserve source geometry, rig, UVs, original textures and clips. Nonuniform proportion changes require deformation, equipment and corpse checks; no universal slider compatibility claim.
Use the new spear shapes with existing Valheim materials/metals and appropriate stat tiers. Assign by faction, progression and role, including the huntress, tiny individuals and shoulder pair. A mesh's appearance alone does not create a tier, recipe, thrown projectile, AI attack or retrieval system. No numerical balance or new metals are approved here.

## Masks and attachments
Design a separate grotesque mask: compressed nose, enlarged teeth, distorted skull/shrunken-head/Frankenstein-like silhouette. Wear it over the actual goblin face. A human base head is unnecessary unless authoring a human wearer.
The real face and mask must be distinct renderable geometry; mask removal reveals the real underlying face. Separately authored ears use their own UV/material treatment and verified attachment/weights. Inspect fused versus separate equipment and reconstruct missing head/body surfaces as needed.
Diablo imagery is visual reference, not imported Blizzard geometry, established mask lore or proof of a historical mask-removal mechanic.
The existing temple-family runtime README describes an inert-by-default mask/faction candidate. Its damage-driven cosmetic mask break is not evidence of completed Scout anatomy, actor spawning, pair AI, or in-game acceptance. Preserve its existing source; coordinate with its owner rather than starting another writer.

## Later delivery: dynamic two-actor stacking
Each eligible individual needs a verified shoulder/neck seat and a compatible mounted pose. Body variation must preserve usable seat clearance.
Two lone eligible allies can discover one another, reserve one pairing, choose carrier/rider roles and perform a bounded jump/mount sequence. Never force all pairings to be interchangeable: shaman/weapon/size roles determine eligibility.
Two health pools and persistent identities remain. Carrier owns navigation; mounted rider attacks without independent ground navigation or colliding against the carrier. One authoritative pairing record must survive network ownership changes.
Rider death releases only the rider; carrier continues. Carrier death dismounts the living rider to validated ground; it recovers and resumes an authored solo role with its remaining health/equipment. Simultaneous death never produces a survivor. Survivors may find another lone partner after a bounded recovery/cooldown.
Require interruption, blocked/failed jump, missing partner, unload/reload, late join and ownership-transfer behavior, plus no duplicate actors, loot or resurrection. No such dynamic pair is claimed implemented.

## Required coordination and Git receipt
All design/runtime/art/world workstreams must read this update and reconcile their owned records before continuing. Keep the approved build evidence and historical records intact.
Each owner commits only owned changes, pushes their assigned branch, and returns repository, branch, commit SHA, paths changed, checks, build/install status and remaining blockers. Fetch/compare remote state before integrating; never force-push, stage another owner's dirty files or claim synchronization from a local commit alone.
Use existing Git exchange paths for shared handoffs. Files are the coordination mechanism; this update does not claim separate chat conversations, Claude or Cursor were directly messaged.
