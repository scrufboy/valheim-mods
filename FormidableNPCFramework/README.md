# Formidable NPC Framework

**Working product name.** This is the standalone, community-usable NPC authoring
and runtime framework that will replace the capabilities currently obtained from
RRRCore in the private lab stack.

It is deliberately separate from the Living World C.O.R.E. story and content
packages. Other creators must be able to install and use the framework without
installing Living World companions, villages, lore, enemy factions, telemetry, or
the Tooler.

## Target dependencies

- BepInEx for Valheim
- Jotunn

RRRCore, Souls of the Land, RunicPack, MadHatter, Living World C.O.R.E., the
Tooler, and telemetry are not framework dependencies.

## Responsibility

- network-safe NPC and prefab registration;
- definition loading and validated configuration;
- persistent identity, names, factions, hostility, ownership, and state;
- humanoid and creature visual assembly;
- equipment, armor, attachments, stowing, materials, colors, and visual hashes;
- animation, audio, effects, status, environmental presentation, and interaction
  hooks;
- melee, ranged, projectile, thrown, bomb, area-of-effect, and status-delivery
  attack profiles;
- spawn, encounter, raid, and world integration;
- synchronized server configuration;
- stable public extension APIs, diagnostics, and migration tools.

## Licensing and migration boundary

The independently authored framework source can be released under this
repository's MIT license. Existing third-party binaries and assets are not renamed,
relicensed, or shipped as framework source. The repaired RRR/Runic/Souls stack is
temporary lab infrastructure used to measure required behavior and validate
compatibility while the replacement is built.

The release gate is functional: the framework is not an RRRCore replacement until
Living World NPC definitions and the required benchmark scenarios run with
RRRCore absent from the plug-in load tree.

