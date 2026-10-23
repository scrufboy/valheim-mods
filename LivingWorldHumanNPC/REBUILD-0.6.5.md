# Living World Human NPC 0.6.5

## Purpose

0.6.5 is the registration repair for the rejected 0.6.4 build. It removes the
invalid attempt to rewrite Valheim 1.0.20's private static literal target-scan
constants. No replacement scan loop is added.

## Evidence from the failed run

The 2026-10-23 log proves 0.6.4 loaded but never registered its human prefabs:

- `REGISTRATION FAILED: Valheim's native target-scan interval contract changed.`
- `STATUS 0.6.4: registered=False; partyBusy=False; loadedV2=0.`
- Every `lwparty` transaction failed before spawning and retained no partial
  party.
- Telemetry recorded the leader only. It contains no companion-combat evidence
  and therefore cannot support any behavioral conclusion.

Mono.Cecil inspection of the exact installed `assembly_valheim.dll` identifies
both interval members as `Private, Static, Literal, HasDefault`, with constants
2 and 6. Runtime `Type.GetField` correctly returned null for the requested
instance-field contract.

## Correction and preserved work

- Removes only the invalid target-interval reflection and writes.
- Preserves the exact two-member `lwparty` transaction and random identity,
  sex, hair, and male-beard system.
- Preserves native combat ownership, immediate native `EquipBestWeapon` handoff
  on a newly committed target, actor/party cohesion hysteresis, native-owned
  combat equipment, calm visible-back stowing, traversal, and settled guard.
- Preserves telemetry schema 9 and its combat/cohesion/equipment observations.
- Adds a build-time native compatibility gate. Every private Valheim field and
  method currently used through reflection must exist in the exact game
  assembly with the required instance/signature contract; static or literal
  fields fail the build.

## Verification

- Native reflection compatibility gate: PASS
- Foundation contract tests: PASS
- Human NPC SHA-256:
  `F8F51AB77BBC585A57C495074E06FC3F3B3BF1D4F0EFDB7779A1EAFB54417525`
- Telemetry SHA-256:
  `E89AEFBC19FECE0EEB3FC6C85104500BE7B4CE44CDD42E15F1755AD0C1510BBA`
- Installed profile: `LWC Human Lab v1`
- Installed DLL hashes match the verified build artifacts.
- Recoverable pre-install backup:
  `BepInEx/LivingWorldBackups/Rebuild/20261023-023209-pre-0.6.5`
- Live Unity registration and party acceptance: pending test
