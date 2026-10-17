# Current shared handoff

Updated: 2026-10-17 after Claude baseline-blocker review and direct profile inspection.

## Governing direction

Stabilize and prove the active Human NPC companion without deleting working behavior. Combat, facing, bow execution, hands/stow, torch, pursuit, regrouping, doors, wetness, formation, identity, and commands remain independent protected contracts.

## Verified current machine state

### Installed candidate

- Profile: `LWC Human Lab v1`
- Installed DLL: `BepInEx/plugins/LivingWorldHumanNpc/LivingWorld.HumanNpc.dll`
- Installed DLL SHA-256: `8575846E75CE86ACE270875520D6090767CE3D5EB6590D9BA8EA91195A6DC3E6`
- Matching source/build candidate: **0.3.34 — Automatic Wave Site Search Lab**
- Classification: **INSTALLED, OFFLINE-TESTED, NOT LAUNCHED, NOT LIVE-PROVEN**

The DLL was installed at 2:31 PM on 2026-10-17. The latest BepInEx log ended at 2:29 PM, so that log cannot certify 0.3.34.

### Latest launched candidate

The latest available startup log states:

`Loading [Living World Human NPC Lab 0.3.33]`

and later:

`STATUS human plugin=0.3.33; registered=True; check=idle; scoutLoaded=True.`

Classification: **LAUNCHED**. This proves load/registration only; it does not prove the protected behavior matrix.

0.3.33 build DLL SHA-256:

`ECDC0FE24D75A398A47F6D055B2B2F1DDC3D0E8E684141B465F6D2C358B1AAB4`

### Historical audited snapshot

0.3.31 remains a preserved historical baseline candidate:

`7443EA847975413F71270A71ED17777660E1CE66006502F3D7B12576638775A0`

It passed offline gates but was not live-proven. Its package also contained stale 0.3.12 `START-HERE.txt` text.

## Scope review of successors

### 0.3.31 → 0.3.32

Changed production files:

- `HumanNpcSettings.cs`
- `ManualTestPlugin.cs`

The party transaction change preserves a newly created actor while bounded appearance/follow initialization completes. However, the same candidate also changed:

- view angle 120° → 90°;
- random move range 0 → 3;
- random circle interval 1 → 2;
- avoid water false → true;
- intercept maximum 0 → 2 seconds;
- flee when hurt and target unreachable false → true.

These extra changes are not explained by the candidate name and must receive explicit contract justification and regression review. They are not automatically accepted.

### 0.3.32 → 0.3.33

Changed production files:

- `HumanCompanion.cs`
- version string in `ManualTestPlugin.cs`

The change removes an immediate target drop from the quarter-second acquisition scan and leaves disengagement to the longer authorization/retention contract. It cites an acquire/idle/acquire loop observed in a 2026-10-17 lab run. Classification: scoped source change with runtime motivation; full protected behavior remains unproven.

### 0.3.33 → 0.3.34

Changed:

- `ManualPartyLab.cs`
- version string in `ManualTestPlugin.cs`

The behavioral change is confined to the controlled test-wave utility: instead of requiring the tester's faced direction to contain one valid site, it searches dry, clear, reachable sites at 20–45 m around the player. It does not intentionally change companion runtime behavior. Classification: installed test-harness change, not launched.

## Current test decision

0.3.34 is the only current installed candidate. Before testing:

1. Treat the 0.3.32 setting bundle as an explicit review item.
2. Launch and confirm both startup lines report 0.3.34.
3. Confirm the profile DLL hash remains `8575846E…C3E6`.
4. Run the protected 14-step validation, stopping at the first companion failure.
5. Do not count automatic wave-site selection as proof of companion combat behavior.
6. Capture startup log, structured telemetry, and video.
7. Promote nothing until live evidence and the no-regression matrix agree.

## Evidence exchange

- Claude blocker review: `CLAUDE-OUTBOX/2026-10-17-baseline-blocker/RESPONSE.md`
- Codex machine resolution: `CODEX-OUTBOX/2026-10-17-baseline-resolution/RESPONSE.md`
- Full historical audit: `CODEX-OUTBOX/2026-10-17-full-project-audit/HANDOFF.md`

## Immediate rules

- Preserve accepted behavior and last known-good artifacts.
- Fix only an evidenced failing ownership boundary.
- Never call selection a successful native action.
- Utility/social systems may not reclaim hands or movement during an encounter.
- Postponed features remain postponed.
- Never overwrite prior submissions.
