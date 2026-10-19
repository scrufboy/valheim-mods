# Human NPC 0.3.42 native combat ownership

Date: 2026-10-18

## Boundary correction

Companion behavior must not become a second combat AI. Once a legal hostile is authorized, the configured native `MonsterAI` owns movement, facing, range selection, weapon selection, pursuit, attack cadence and attack execution. These are the same Valheim AI fields and Souls of the Land values already configured on the actor.

## Removed competing combat writers

- Manual per-frame bow turn/draw/attack requests.
- Postfix weapon-choice override and four-second weapon commitment state.
- Custom nearby-threat weapon-switch decision.
- Custom firing-lane decision loop.
- Custom shield reaction and block window.
- Custom riposte queue and direct return-strike request.
- Harmony interception of native block resolution.

## Retained boundaries

- Legal hostile authorization and bounded encounter exit.
- Players, tamed creatures and companions remain invalid victims.
- Native `SelectBestAttack` and `EquipBestWeapon` choose equipment from native AI ranges.
- An empty bow is made ineligible.
- The narrow bow compatibility bridge temporarily presents the per-actor bow attack as an ordinary native attack during `MonsterAI.DoAttack`; native Attack still owns projectile, ammo, animation trigger, hit and damage.
- Companion ambience, posture, social gaze, healing and torches remain locked out for the complete combat-readiness state.
- Weapon stowing, doors, formation, wet parity, awareness HUD and party creation are unchanged.

## Verification

- Full offline build and regression gate: PASS.
- Party integration: 204 assertions PASS.
- DLL SHA-256: `F657B0AF5775BCB4A142B02445B798DC3B64486052C9D9CED9DDE9450D962364`
- Package SHA-256: `477DCA7A6A01CB3CD7C5DBAA97D24C1D33AE1AB761CBDBA43721124F7461B4E9`
- Installed into `LWC Human Lab v1` after confirming Valheim was not running.
- 0.3.41 installed DLL preserved in the profile backup directory.

Live Unity combat remains the acceptance test.
