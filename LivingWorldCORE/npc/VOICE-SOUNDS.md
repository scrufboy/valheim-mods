# Living World C.O.R.E. — companion voice sounds (as of NPC 1.0.31)

Source of truth: `LivingWorldCORE/npc/src/Embodiment/NpcVoice.cs`, `VoicePolicy` in `src/Kernel/Kernel.Pure.cs`.
Design rule (Brian): mostly silent. Noticing happens BEFORE action, exertion DURING it, reactions and hits AFTER.

## When each line plays (same rules for men and women)

| Line (key) | Moment | Rule |
|---|---|---|
| `alerted` — call-out | Before: first notices an enemy | Only before contact: no own strike and no hit taken in the last 3 s, enemy >= 6 m away. 70% chance, once per enemy per party (remembered 180 s), 4 s party cooldown. Also: partner's reaction 0.6 s after a devastating hit on the other. |
| `exert` — exertion | During: as a swing starts (`Humanoid.StartAttack`) | Heavy attack 80%, ordinary swing 15%, max one per 4 s per companion. |
| `hit` — hurt | After: taking damage | Devastating hit (>= 15% max health): always, 4 s cooldown. Ordinary hit: 35%, 2.5 s cooldown. Hit while speaking: line cut off, answers 80%, 1.5 s cooldown. |
| `idle` — gloat | After: a mighty blow (>= 25% of the target's max health) or a kill, by a companion or the leader | 35%, 10 s cooldown. |
| `death` | Death | Always (native death effect). |
| `warcry` | Raid start, leader inside the raid area | Once per raid per companion, 0-0.9 s apart, with the vanilla Roar emote (`emote_roar`). Never with an enemy within 6 m, mid-swing, falling back or moving; 10 s window. |

## Male set — vanilla Valheim assets only

| Line | Vanilla source prefab | Clips (vanilla WAVs inside) | Pitch |
|---|---|---|---|
| alerted | `sfx_goblin_alerted` | Male_LS_D_Surprised x3 | 0.8-0.9 (Fuling pitched down to human) |
| hit | `sfx_goblin_hit` | Male_LS_D_HurtGrunt x9 | 0.8-0.9 |
| exert | same as hit (`sfx_goblin_hit`) | Male_LS_D_HurtGrunt x9 | 0.8-0.9 |
| idle | `sfx_goblin_idle` | Male_LS_D_Laugh x4 | 0.8-0.9 |
| death | `sfx_goblin_death` | Male_LS_D_Death x4 | 0.8-0.9 |
| warcry | `sfx_goblinbrute_taunt` (Fuling Berserker roar) | Male_LS_D_Scream02-04 | native 0.4-0.6 -> 0.7-0.85 (halfway between the native beast roar and the rejected 1.0-1.1) |

WAV names are from the vanilla asset manifest; which WAV sits in which prefab is inferred from the names (Surprised -> alerted, HurtGrunt -> hit, Laugh -> idle, Death -> death), not read from the prefabs.
Clones are registered with ZNetScene (`LWC_sfx_alerted/hit/death/idle/warcry`); unregistered clones caused the 1.0.17-1.0.21 respawn error floods.

## Female set — "Ultimate Female Voice Pack" by topmademarti (licensed by Brian)

License: use in personal/commercial games, editing allowed; no redistribution as a standalone pack -> ship packed inside the mod, never as a loose folder; clips are not in the git repo. Credit (optional): "Voice Pack by topmademarti".
Loaded at startup from `BepInEx/plugins/LivingWorldHumanNpc/voices/female/<line>/*.wav`.
Level: each clip normalized to 0.98 peak (~+5 dB; pack peaks ~0.55), playback volume 0.5 template x1.58 (+4 dB) -> about +9 dB over the raw pack. Pitch 0.97-1.03 (natural variation only).

| Line | Folder | Pack files |
|---|---|---|
| alerted | `female/alerted` (11) | Combat/Attack: combat_attack_01-08, combat_attack_quick_01-03 |
| exert | `female/exert` (7) | Combat/Swings: combat_swing_01-04; Combat/Attack: combat_attack_quick_01-03 |
| hit | `female/hit` (14) | Combat/Grunt: combat_grunt_01-03; Combat/Pain: combat_pain_01-06, combat_pain_gasp_01, combat_pain_light_01-04 |
| idle (gloat) | `female/idle` (5) | Personality/Laugh: personality_laugh_01-05 |
| death | `female/death` (6) | Combat/Pain: combat_pain_01-06 (the heavy ones) |
| warcry | `female/warcry` (3) | Combat/Attack: combat_attack_02, _05, _07 (longest shouts) |

Not used yet: Idle (Calm, Misc, Sigh, Thinking), Personality (Acknowledgment, Annoyed, Bored, Misc, Quick reactions), UI.
Fallback: a female line with no live clips plays the male line (logged once).

## Vanilla female candidates found (not used)
Hildir (trader) laughs/greet/talk/trade/goodbye; Dvergr VO alerted/attack/hurt/death/idle (gender unverified by ear); `UI_Vomit_Female_M_01-03`.
