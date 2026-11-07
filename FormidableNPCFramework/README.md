# Formidable NPC Framework

Latest integration receipt: [parallel swamp checkpoint](PARALLEL-SWAMP-STATUS-2026-11-07.md). Native shaman registration source compiles; full raid and tiny model remain blocked on the documented runtime/source gaps. No game acceptance is claimed.

Active swamp work: [Shaman behavior and full-faction trial](SHAMAN-AND-SWAMP-TRIAL-2026-11-07.md). Source now includes the control policy and bounded raid compiler; native actor/executor and live encounter acceptance remain separate requirements.

November 7 source development: see [the full-roster execution update](DEVELOPMENT-2026-11-07.md). The lossless Souls roster reader and FormidableAI dart proposal module are implemented and offline-tested. Native conversion/execution remains incomplete; this is not yet an installable RRR replacement. [Content attribution](CONTENT-CREDITS.md) preserves Fantu's authorized contribution separately from the framework.

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

