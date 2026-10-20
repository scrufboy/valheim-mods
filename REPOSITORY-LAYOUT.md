# Repository layout

This is one monorepo with a separate directory for each installable mod or system.

Each active mod directory owns its source, manifest, build script, documentation, tests, and package metadata. Shared architecture lives in `LivingWorldCORE/`; historical candidates live in `archive/`.

`LivingWorldCORE/` owns the standalone foundation and replacement contracts.
`LivingWorldHumanNPC/` owns the Formidable NPC implementation and consumes those
contracts as they become available. Enemy species and biome variants are optional
content packages; they do not belong inside the Core runtime. Local RRRCore,
RunicPack, or Souls repairs belong only under `compatibility/` and may not become
release dependencies of the completed Living World Core.

## Naming

- GitHub owner: `scrufboy`
- Product family: `Living World C.O.R.E.`
- Thunderstore/r2modman package prefix: `scrufboy-`
- BepInEx plugin namespace prefix: `com.scrufboy.livingworldcore.`

Do not attribute Living World work to Codex, ChatGPT, or a third-party mod author. Brian / scrufboy owns the project and its original source.
