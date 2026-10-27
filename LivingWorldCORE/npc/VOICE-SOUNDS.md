# Living World C.O.R.E. — companion voice sounds (as of NPC 1.0.31)

Source of truth: `LivingWorldCORE/npc/src/Companion/Combat/CompanionCombatVoice.cs`, `CombatVoicePolicy` in `src/Kernel/Kernel.Pure.cs`.
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

Since 1.0.32: `voices/female/clips/*.wav` + `voices/female/lines.txt` (`line: file, file, ...`). Edit lines.txt to approve/reject; no rebuild. The older per-line folders are ignored while lines.txt exists.
Behavior is shared: the same CombatVoicePolicy/WarCryPolicy for both genders; gender only selects the clip pool inside `CompanionCombatVoice.Say`.

**Status: first pass chosen from acoustic measurements only (Claude cannot listen). Every clip still needs Brian's in-game audition.**

| Line | Clips (first pass) | Measured reason |
|---|---|---|
| alerted | reaction_quick_02, _03, _05; UI_investigate_01, _03; combat_attack_04, _06 | short (0.3-0.6 s), rising pitch = surprise/heads-up |
| exert | combat_swing_01-04; combat_attack_quick_01-03 | short single bursts at swing start |
| hit (light) | combat_grunt_01-03; combat_pain_light_01-04; combat_pain_gasp_01 | brief, falling, moderate |
| hit_heavy (devastating, new pool) | combat_pain_02, _04, _05, _06 | sharp falling pain |
| death | combat_pain_01, _03 | longest fading tails (-19/-17 dB); the pack has no death category, so these are the closest available, not confirmed terminal |
| idle (kill / mighty blow) | personality_laugh_01, _02, _04; personality_ack_03; personality_annoyed_06 | short, falling (satisfied laugh, "hm", scoff) |
| warcry | combat_attack_02, _05, _07 | the three longest shouts; exclusive to warcry |

Not used, by measurement: combat_attack_01/_08 (falling = effort, not notice), combat_attack_03 (very high pitch), personality_laugh_03 (very high and bright, giggle-like), personality_laugh_05 (two bursts = "ha-ha"), Misc/vocal clips (0.9-1.5 s, too long), UI hover/select/confirm/cancel, Idle calm/sigh/thinking.
No clip is in two lines; startup warns on any overlap (by name and by audio hash), missing/empty/failed clip, or a line falling back to male.

Level: clips normalized to 0.98 peak (pack peaks ~0.55); playback = 0.5 template x `Voice.FemaleVoiceGain` (config, default 1.58 = Brian's in-game setting from 1.0.31; set 1.0 for the unboosted template level).

Dev audition log (`Voice.DevVoiceLog = true` in the lab config, off by default): every line logs `VOICE <name> (<gender>) line=<line> clip=<file> trigger=<event> [FALLBACK=male]`.
Triggers: first sight at Nm, swing, heavy attack, light hit N, hit while speaking N, devastating hit N, partner devastated, kill N, mighty blow N, raid start. Death is the native death effect (pool f:death or the Fuling death), not logged per clip.

Audition scenarios (Brian): first sight of a distant enemy; ordinary swing; heavy attack; light damage; devastating damage; partner devastated; mighty blow; kill; death; raid start with two companions.
