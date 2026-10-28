# Current Living World C.O.R.E. project state — 2026-10-27

This file is the repository navigation record. It does not merge experimental or
approved runtime branches into `main`.

## Design authority

- `main` — current lore, framework boundaries, world companions,
  roster/progression, appearance and permanent-mortality design. The latest
  substantive design commit in this record is `54f8d7a`; later commits add this
  navigation documentation.
- `design/lwc-design-archive` @ `6b9efbd` — reconciled design archive,
  implementation map, traversal gate, work order and Claude source audit.

## Living World C.O.R.E. 1.0 runtime history

| Ref | Commit | Meaning |
| --- | --- | --- |
| `lwc-core-1.0/baseline-combat-voice-1.0.32.6` | `26fd295` | approved and locked combat-voice source |
| `combat-voice-approved-1.0.32.6` | peels to `26fd295` | annotated approval tag |
| `lwc-core-1.0/installed-1.0.32.7` | `9933763` | installed rename/module-boundary build; LIVE-PROVEN 2026-10-28 (one-launch check, see `claude-exchange/CLAUDE-OUTBOX/2026-10-28-launch-proof-1.0.32.7`) |
| `lwc-core-1.0/1.0.32.8b-party-spacing-test` | `33addad` | spacing review build; live-tested 2026-10-28 (contact failures, see CLAUDE-OUTBOX/2026-10-28-spacing-live-result) |
| `lwc-core-1.0/1.0.32.11-bombs` | `f69f217` | 1.0.32.9 contact + 1.0.32.10 gait + finite bombs; superseded before play |
| `lwc-core-1.0/1.0.32.12-souls-throw` | `1a7174e` | + Souls Pillager throw, cluster aim, endless test mode; superseded before play |
| `lwc-core-1.0/1.0.32.13-body-voice` | `ea13706` | + body-reaction voice addon (poison cough); INSTALLED 2026-10-28 (hash `0a44d570…`), superseded by 1.0.32.14 before play; DLL backed up in `BepInEx/LivingWorldBackups` |
| `lwc-core-1.0/1.0.32.14-body-states` | `3c7e0da` | + body states (burning, frost, long swim, exhaustion, recovery) from ChatGPT's handoff; INSTALLED 2026-10-28 (hash `206653b4…`), live test and clip audition pending |
| `lwc-core-1.0/parked-1.0.33` | `965a2a0` | parked hot-path experiment; not installed |

**Game build:** Brian's installed Valheim updated to **1.0.21** on 2026-10-28 (assembly `9f959c03…`); prior inspections used 1.0.20. 1.0.32.14 rebuilt against 1.0.21 is byte-identical and passes all gates (OFFLINE-TESTED).

These refs share their complete prerequisite history. They remain separate from
`main` until an explicit reconciliation and acceptance plan is approved.

## Immediate order

1. ~~Launch installed 1.0.32.7 once and verify its runtime evidence.~~ Done 2026-10-28.
2. Install/test 1.0.32.8b only after that proof, with spacing telemetry enabled.
3. Implement identity-preserving travel: cave/dungeon doors first, then portals with inventory persistence. Design: `claude-exchange/CLAUDE-OUTBOX/2026-10-28-portal-dungeon-gate-design`.
4. Reconcile the 1.0.x actor line with the Formidable framework boundary.
5. Player-parity interior layer (status, skills, aiming model, attack purpose, outputs), staged; design: `claude-exchange/CLAUDE-OUTBOX/2026-10-28-player-parity-layer-design`. Awaiting ChatGPT review.
6. Continue shared bomb/AOE, heavy weapon, class/role, needs, gear, progression and
   township work through modular gates.

## Deliberately excluded from GitHub

Compiled DLLs, licensed or derived audio, private configurations, logs, telemetry,
videos, profile backups, game assemblies, third-party packages and personal paths
are not source deliverables and are intentionally absent.
