# APPROVED COMBAT-VOICE BASELINE — LivingWorld.HumanNpc 1.0.32.6

Companion combat module, combat voice: **complete and locked** (Brian, 2026-10-27). This locks only the combat voice; the future NPC voice system as a whole is not complete.

## Approved build (copied directly from the installed LWC Human Lab v1 profile)

| File here | Installed at (`...\profiles\LWC Human Lab v1\BepInEx\`) | SHA-256 |
|---|---|---|
| `LivingWorld.HumanNpc.dll` | `plugins\LivingWorldHumanNpc\LivingWorld.HumanNpc.dll` | `4dee75d49c01a32f0250a09595b166e2deac5ad43d8b46aabeb3257b2667cc6e` |
| `local.livingworld.humannpc.cfg` | `config\local.livingworld.humannpc.cfg` | `95624ae117823c14e864f38446f9a07dfd116c47ef34bfc5fc5ba68894ddb17c` (`CompanionVoiceGain = 2`, `DevVoiceLog = true`) |
| `voices\companion\male\<pool>\` | `plugins\LivingWorldHumanNpc\voices\companion\male\<pool>\` | 40 WAVs, Gamemaster Human Male B |
| `voices\companion\female\<pool>\` | `plugins\LivingWorldHumanNpc\voices\companion\female\<pool>\` | 40 WAVs, Gamemaster Human Female A |

Pools per gender: alerted 2, exert 9, hit 9, hit_heavy 9, gloat 4, death 4, warcry 3. Every file: `SHA256SUMS.txt`. Audio data only (use to verify copies; transfers may append metadata chunks): `AUDIO-DATA-SHA256.txt`.

Source: git tag `combat-voice-approved-1.0.32.6` (source commit `7d59611`); a clean build of that tag reproduces the DLL hash above byte for byte.

## Test evidence

- Long trip (1.0.32.5 at gain 2, 02:44 session): ~22 min combat, 219 lines; every heavy attack voiced at the attack frame; alerts, exertions, hits, gloats, war cries heard.
- Fuling raid (1.0.32.5): female and male devastating-hit clips played; male and female stagger cries fired. Four companion deaths silent -> death-line defect found.
- Death test (1.0.32.6, 12:08 session): 14 telemetry-confirmed companion deaths, 14 `VOICE-DEATH` events, exactly one voice per death (`voices=1`), all detached (`attached=0`), correct gender pools (5 female, 9 male), no missing death events, no duplicate death voices, no NPC-code errors.

## Immutable behavior contract

1. Gender: pool chosen by the companion's identity (`m` / `f`) after the decision; no fallback to the other gender, Fuling, Berserker or any enemy recording; an empty pool is silent.
2. Pools and semantics: alerted, exert, hit, hit_heavy, gloat (line `idle`), death, warcry; files, order, counts and audio exactly as locked; no clip in two pools.
3. Selection: uniform random over the selected pool (vanilla ZSFX), one instance per line.
4. Playback: one shared gain (`0.5 x CompanionVoiceGain`, clamp 0-2, max 1.0); vanilla `sfx_goblin_hit` spatial container; as delivered (no normalization, pitch 1.0).
5. Alerts: only before contact (3 s quiet, enemy >= 6 m), 70%, once per enemy per 180 s, party cooldown 4 s; partner reacts 0.6 s after a devastating hit.
6. Exertion: every heavy attack voiced, held to the attack frame (`Attack.OnAttackTrigger`), aborted if dead / staggered / not attacking; ordinary swings 30%, 2.5 s cooldown, one decision per attack.
7. Pain: ordinary hit 65% / 1.5 s; devastating (>= 15% max health) guaranteed / 4 s; a hit mid-line always answers (1.5 s); stagger = one guaranteed heavy cry per 1.5 s, and its damage is the same event (0.3 s).
8. Priority: stagger / devastating 4 > hit 3 > war cry 2 > alert / exert / gloat 1; a pain line is protected 0.7 s; any hit cuts off the current line.
9. Gloat: kills or blows >= 25% of target health only, 35%, 10 s cooldown.
10. War cry: once per raid, at a calm raid start (no enemy within 6 m, not attacking, falling back or moving).
11. Death: guaranteed, from the identity's own death pool, exactly one line, created at the death position and not attached to the body.

Enforced by `tools/combat_voice_gate.py` (+ `combat_voice.lock.json`) and the `LOCK combat voice` regression tests; both run in every build.

## Module boundary

Settlement, conversation, work, needs, hiring, command and relationship vocals are separate non-combat modules with their own semantic pools. They may request playback through a shared presentation service; they must not change this combat policy, its pools or its timing.

Licensed audio (Gamemaster): private use only, no redistribution.
