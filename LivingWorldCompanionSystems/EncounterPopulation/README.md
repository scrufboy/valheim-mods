# Biome encounter population and combat kit contract

This module selects a varied, persistent set of people for a claimed site. It
does not register Valheim prefabs or install a DLL. The catalog currently offers
99 abstract role packages across Meadows through Ashlands, with Hearthless,
Pillager, and Eitrherjar variants. Deep North is intentionally unassigned until
its land and equipment rules are approved. Armor descriptions and weapon families
are authoring data, not verified vanilla item IDs.

## Server owns people and combat

`EncounterPopulationPolicy.Select` runs only after the world has persisted one
site claim. It uses the world seed, site, encounter, biome, and authored faction
to select one to four roles. A committed plan is replayed, not rerolled. Each
slot gets a stable `PersonId`; no client may select, respawn, promote, or change
allegiance. Every role has a biome-level threat score, a primary weapon, a
different melee fallback, and a regional appearance. Two people at a camp get
different roles when possible.

The population plan feeds the naming module and `EncounterLoadouts` to create
the same person's name, biography, equipped gear, and finite supplies. The
maintained adapter must map abstract weapon and appearance keys to actual
registered items, grip/attachment points, animations, and inventory entries
before it may instantiate an actor. A generated local's starting biome kit is
their history; later upgrades still use the lawful companion progression gates.

`ServerCombatKitPolicy` is a request router, not an AI or damage controller.
It requires server authority, actor ownership, native combat ownership, and a
target authorized by `EncounterAllegiance`. It can request a bomb, spear throw,
owned spear recovery, knife lunge, or atgeir sweep only after that exact move
has passed its live verification and the matching specialist safety policy.
The core's native combat owner remains responsible for target, movement,
animation, projectile, damage, inventory debit, and recovery.

A spear user keeps a different melee fallback. While the spear is airborne or
lost, the fallback remains usable. Reclamation requires a known owned projectile
and a safe recovery fact; it never fabricates another spear. A spear-bombardier
may carry both moves and a finite two-bomb reserve. When bombs are spent, spear
use remains possible; when the spear is unavailable, the fallback remains.
Unknown allegiance, unapproved moves, and remote peers cannot authorize attacks.

## Client receives presentation only

`ServerPresentationProjection` produces `ClientMemberView` after the server
decides what has been revealed. Before reveal the view carries silhouette,
regional armor look, and visible weapon, but no faction. The client rendering
adapter may show those values and play approved encounter cues. It never receives
the server's full population plan, hidden allegiance, site claims, affinity,
combat authorization, or random selection seed as a command surface.

## What remains for the maintained 1.0.x builder

1. Persist the exact population plan and its IDs atomically with the site claim
   before actor creation. Map each person to one registered native prefab and
   a validated personal equipment record.
2. Bind biome and location to actual server world facts; enforce a finite
   camp/actor budget and the selected server-resident zone rule.
3. Wire naming, local history, and verified map knowledge without changing
   the chosen person or faction on reload.
4. Route spear, bomb, knife, and atgeir requests through the existing specialist
   safety policies and the single native combat owner. Prove exact item,
   animation, grip, projectile, debit, recovery, and network behavior in game.
5. Let aid and affinity change a relationship only through witnessed facts.
   Temporary help and later party membership require separate offers and
   acceptance; enemy faction selection never auto-converts to recruitment.

Run `Run-Tests.ps1` for the deterministic package checks. These checks exercise
the policy code outside Valheim. They do not establish that an NPC has spawned,
attacked, or lived in a running server.
