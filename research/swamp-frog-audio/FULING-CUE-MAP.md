# Swamp frog mob = Fuling cue set, frog voice

Owner direction (Brian, 2026-11-03): "we are basically using fuling cues with frog sounds.. for this mob."
So the slot list is **the Fuling's own sound cues**, not the ten slots in `slots.v1.json`
(that file is superseded for this mob; keep it only as search-term reference).

ChatGPT is the judge: it listens to the candidates and picks per cue. AUDITION.html is optional.

## Step 1 - get the real cue list (required before picking)
- SOURCE-INSPECTED: `sfx_goblin_alerted` exists; 3 clips, source pitch 1.10-1.20
  (`LivingWorldHumanNPC/tests/inspect_ambient_assets.py`, `ManualTestPlugin.cs:130`).
- UNKNOWN: every other Fuling cue. Dump them from the `Goblin` prefab (and GoblinArcher /
  GoblinBrute / GoblinShaman if the frog mob gets variants), with the same UnityPy approach as
  `inspect_ambient_assets.py`: for each EffectList field on Humanoid / MonsterAI / attack
  ItemDrops (alerted, idle sound, hit, death, attack/swing, taunt if present, footsteps,
  consume/eat), record the sfx prefab name, clip count, clip lengths, pitch range,
  volume range, and the idle-sound interval / chance.
- Write the result as a table in this file (the "Cue" columns below), labelled SOURCE-INSPECTED.

## Step 2 - fill one frog set per cue
Match each cue's **clip count, length range and pitch/volume randomisation** so the mob feels
like a Fuling in timing, only frog-voiced. Expected kind of frog call per cue (a suggestion to listen against):

| Fuling cue (fill exact sfx name) | Clips | Len | Frog material to judge for |
|---|---|---|---|
| alerted (`sfx_goblin_alerted`, verified) | 3 | ? | sharp territorial/encounter call, bullfrog alarm squeak |
| idle | ? | ? | single croaks, low grunts, chorus fragments (night chatter) |
| attack / swing | ? | ? | short exhaled grunt/croak, growl burst |
| hit (hurt) | ? | ? | release call, short distress yelp |
| death | ? | ? | long distress scream (Leptodactylus, leopard frog, bullfrog) |
| taunt / other (if present) | ? | ? | encounter growl, toad trill |
| footsteps / splash (if Fuling uses voice-adjacent sfx) | ? | ? | wet/slap, not vocal - optional |

## Step 3 - judging rules for ChatGPT
1. Listen to every candidate in `C:\Users\scruf\Downloads\LWC-swamp-frog-audio-candidates-2026-11-03.zip`
   (548 clips, folder names are a rough auto-sort - ignore them) plus whatever it downloads from
   `DOWNLOAD-TARGETS.md`.
2. Per cue, return ranked picks: file, cue, why it fits, licence tier, and any processing needed
   (pitch shift amount, trim). Prefer SHIP / SHIP-ATTRIB; mark NC and REF picks as placeholders.
3. Fuling clips are pitched/varied by the game; judge whether a frog clip survives the cue's
   pitch range without turning comic.
4. Gaps (expected: attack, death, alerted need low/aggressive calls) -> say which source to pull next.
5. Do not install, change prefabs, Spawn That / Drop That configs, or Souls. Return in a CODEX-OUTBOX submission.
