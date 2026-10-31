# Completed arena preparation — sync before reviewing the next session

ChatGPT completed the isolated Arena preparation after Brian corrected the test boundary.

## Test boundary

- Brian is not a defended object and receives no artificial state.
- All six raid entries use `HuntPlayer = false`.
- Neither side receives a defend, escort, protect-leader, hide-observer, or forced-acquisition objective.
- Crouching, torchlight, buildings, line of sight, movement and ordinary aggro remain the game's own inputs.
- Brian stays unseen and stationary by ordinary gameplay choice; if he stands or moves, the ordinary perception result
  is allowed to change.

## Raid now staged

Profile:

`C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena`

Event `lwc_formidable_arena` is a ten-minute normal raid envelope with repeated groups entering from 50–65 m:

- `RRRN_Pillager`: 2–3, every 25 s, max 6 alive;
- `RRRN_OrcBrute`: 1–2, every 45 s, max 3;
- `RRRN_WoodsSoul`: 1–2, every 35 s, max 2;
- `RRRN_Neckman`: 1–2, every 40 s, max 2;
- `RRRN_NinoBosque`: 1–2, every 42 s, max 2;
- `RRRN_Servant`: 1, every 60 s, max 1.

The raid applies `PlainsMonsters` to the spawned horde so its members do not fight each other. That is enemy-faction
setup only; it does not alter Brian or assign the companions an objective. Spawn That remains off.

## A1 revision 5 build and install

ChatGPT rebuilt `7c22507` twice with Mono 6.12.0.206 against the current Valheim 1.0.21 managed assemblies. Both runs
were byte-reproducible locally and passed:

- regression ledger 157/157;
- native gate 46;
- ownership gate 76;
- combat-voice lock: unchanged;
- movement-writer gate: OK;
- analyzer self-test: OK.

Installed 1.0.32.27 hashes in the Arena profile:

- core `979972ebe0a617ddb7189295c9932b4c988c615d05fc6e2f260aa1acd190edfa`;
- telemetry `42298a29ef4b5cc0951f4c81907df0b0dedfa827a561d0c3b47a5d096879a270`.

These differ from the hashes produced in Claude's Linux build environment, but the source head is exact, the current
game references are exact, the outputs repeated byte-for-byte, and every locked/gated check passed. The previous .26
DLLs and config are backed up under:

`BepInEx\LivingWorldBackups\2026-10-31-before-1.0.32.27-arena`

## RRRCore current-game compatibility

The first launch gate correctly failed RRRCore 3.1.5 because its embedded ServerSync code used `ldsfld` against
`ZRoutedRpc.Everybody`; Valheim 1.0.21 exposes the same public value as a literal `const long 0`, which has no runtime
field storage. The packaged AWL compatibility mod only detected this and loaded after RRRCore, so it could not repair
the failure.

ChatGPT patched exactly three legacy `ldsfld ZRoutedRpc.Everybody` instructions in the isolated profile's RRRCore to
the current constant `0L`. It did not alter routing, permissions, AI or gameplay. Patched RRRCore hash:

`753a3645038fef8ee66f14cc1e2bf05bbcbb729f0c3958253e1e91e6b62aec58`

The original RRRCore is backed up beside the .26 DLLs as `RRRCore-3.1.5-original.dll`.

## Launch result

Valheim 1.0.21 is currently running from the Arena profile. The final launch log shows:

- Custom Raids 1.8.2 loaded;
- patched RRRCore 3.1.5 loaded without the exception;
- Living World NPC and Telemetry 1.0.32.27 loaded;
- RunicPack and SagesVault loaded;
- 13 plugins loaded, 0 skipped, 0 failed;
- zero `[Error]` records.

The next action is Brian entering the test world, bringing the ordinary companion party to the chosen arena, recording
video, and starting `event lwc_formidable_arena`. No further setup scenario or synthetic status is authorized.
