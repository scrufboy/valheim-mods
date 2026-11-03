# HANDOFF — Swamp frog creature: audio sourcing

Date: 2026-11-03 · From: Claude · To: ChatGPT / Codex · Parent: valheim-mods @ 32f4e34

## Intent
Brian downloaded a Unity frog-type swamp creature asset (hostile, combat, motivations, drops,
animations) to bring into Valheim via ASharpPen's Spawn That / Drop That. The asset has no
proper sound pack. This submission is the **research and tooling** for building one:
battle cries, hunt, investigate, alarmed, respond, exclaim, hurt, death, night chatter,
ambience — several variants each, tone-checked so variation/timing can be tuned like the
human pack.

## Brian's words (request, verbatim excerpt)
> "we need to scour for aggressive frog sounds ... battle cries, nighttime chatter, ambiance,
> single query like inquisitive, investigate, hunt, become alarmed, get hurt, respond, exclaim
> with several variants ... Make the handoff for ChatGPT and the repo"

This authorizes research + a handoff. It does **not** authorize creature code, configs,
installs or Souls changes (PROTOCOL: discussion is not authorization).

## What is in this submission
- `research/swamp-frog-audio/SOURCES.md` — compendium + licence tiers + state map
- `research/swamp-frog-audio/slots.v1.json` — PROPOSED slot set (Brian adjusts)
- `research/swamp-frog-audio/tools/harvest.py`, `analyze.py`, `README.md`

## Evidence summary
- SOURCE-INSPECTED: xeno-canto frogs group + API v3 (key required since 2025-10-10, per-recording sound type + licence); Freesound APIv2 (licence filter, previews, analysis); iNaturalist sound licence filters; Sonniss GDC bundles royalty-free; BBC RemArc = non-commercial; Macaulay = licence request; californiaherps.com bullfrog alarm / encounter / release / distress-scream pages.
- OFFLINE-TESTED: `analyze.py` on synthetic clips (event split + measurements OK; slot guesses rough).
- UNKNOWN: any live API call (sandbox blocked); Sonniss bundles' frog content; Unity asset redistribution terms; how the human pack was tone-checked (not found in repo — whoever ran it should align `analyze.py` columns with that pass).

## Open decisions (Brian)
1. Ship policy for NC sources.
2. Creature home: `LivingWorldEnemyFamilies/` (repo layout calls enemy species Living World content) vs the Souls of the Land fix (`compatibility/` only, not a release dependency).
3. Slot list / variant counts in `slots.v1.json`.

## Addendum 18:15 - first audio pulled
Claude's sandbox reaches GitHub only. Pulled from GitHub:
- ESC-50 frog class (40 clips; per-clip Freesound licences: 2 CC0, 29 CC BY, 9 CC BY-NC) - OFFLINE-TESTED (downloaded, analysed)
- Yfyangd/frog (128 Taiwan frog recordings) and h-phantasmagorical/froggy-sounds (15 leopard frog snippets) - no licence file, REF only
Cut into 548 single-call clips + 14 pitched demos, delivered to Brian as
`LWC-swamp-frog-audio-candidates-2026-11-03.zip` (outside the repo, third-party audio).
`research/swamp-frog-audio/DOWNLOAD-TARGETS.md` = concrete URLs/queries for ChatGPT to fetch next.
Gap: almost no low/aggressive calls yet; battle_cry and hunt need Freesound/xeno-canto bullfrog and distress material.
