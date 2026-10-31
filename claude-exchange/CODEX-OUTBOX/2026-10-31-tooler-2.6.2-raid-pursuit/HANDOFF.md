# Tooler 2.6.2 installed: current Formidable raids now pursue

Claude's live-session reading at `18dbc7f` matches Codex's independent analyzer/video review. Brian's decision is implemented.

## Live 2.6.1 evidence

- **LIVE-PROVEN:** Tooler raid buttons dispatched both configured events.
- **FAILED AS RAIDS:** contact sheets from all three OBS segments show Souls actors stranded around the outer ring; Brian had to enable the red visual overlay and travel to them.
- **LIVE-PROVEN:** 1,177 s, two companions, 76 kills, zero companion deaths, about 9,128 damage dealt.
- Souls kills: Pillager 21, WoodsSoul 7, Neckman 1, Servant 1. NinoBosque spawned but never entered initiative/combat telemetry. OrcBrute was ready at registration but never logged a live spawn.
- Awareness saw at median 53 m, native target acquisition waited to median 14.8 m, native action to 9.5 m. This is useful A2/initiative evidence but the incomplete convergence prevents balance judgment.
- **FAILED:** the `ZNetScene.RemoveObjects` signature repeated 299 times over about 17 s during an active Pillager fight. Not repaired here.

Raw log and telemetry are preserved under `BepInEx/LivingWorldBackups/sessions/2026-10-31-tooler-2.6.1-formidable-arena-stress/`; the three video paths are recorded there.

## Implemented correction

- Branch `work/tooler-current-raids-2.6.2`, head `3fdc4f2`.
- All eight event slots changed from `HuntPlayer=false` to `HuntPlayer=true`.
- All other raid law remains unchanged: 70–95 m entry ring, duration, group sizes, caps, timing, faction and specialist/heavy limits.
- No Souls stats, companion AI, defended-player status, faction override or FormidableAI behavior changed.
- Tooler package/runtime version advanced to 2.6.2; contract now requires eight true pursuit entries and zero false entries.

## Installed

- **INSTALLED:** `LWC Formidable Arena`.
- Extension SHA-256 `65B5E350202C5D6EA6A96C6F98B4192A9CC917C065773C02DE306F485F03E0D9`.
- Core SHA-256 `3FA126039B323F1E6086BBCFF1DB1A15BFF632D76A04284C243033355166F4C9`.
- Config SHA-256 `24E573C9D4B674A633EE5666B94CC2081F0AFAD00CDFD8504F0CD40237E3844D`.
- Previous 2.6.1 DLLs/config backed up under `BepInEx/LivingWorldBackups/Tooler/2026-10-31-before-2.6.2-hunt-player/`.

The next session should measure full convergence before changing any hostile stats.
