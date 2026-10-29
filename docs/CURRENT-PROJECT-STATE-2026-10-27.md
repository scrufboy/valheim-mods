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
| `lwc-core-1.0/1.0.32.14-body-states` | `3c7e0da` | + body states (burning, frost, long swim, exhaustion, recovery); live overload test 2026-10-28 (see CLAUDE-OUTBOX/2026-10-28-live-result-1.0.32.14 + 2026-10-29 poison correction); superseded by 1.0.32.15; DLL backed up |
| `lwc-core-1.0/1.0.32.15-party-clouds` | `7b68ef2` | + reduced own/party bomb-cloud exposure (player and companions, 0.35) and companion hazard awareness/escape; Brian-approved 2026-10-29; INSTALLED 2026-10-29 (hash `ef9b56a0…`); live 14:32 session showed the 1.0.32.10 gait regression (standing leader -> 0.7 m/s); superseded |
| `lwc-core-1.0/1.0.32.16-gait-standing` | `42fb9cb` | 1.0.32.15 + gait matching only while the leader walks (standing leader = native pace); INSTALLED 2026-10-29 (hash `53de430c…`); live 15:14: walking speed fixed (3-4 m/s), but armed standing still parked behind the leader; superseded |
| `lwc-core-1.0/1.0.32.17-standing-restore` | `1af8029` | 1.0.32.16 + 1.0.32.7 standing anchor restored (spacing keeps only contact back-off); INSTALLED 2026-10-29 (hash `352a03fd…`); superseded before play |
| `lwc-core-1.0/1.0.32.18-ready-watch` | `a8bafe2` | 1.0.32.17 + ReadyWatch behavior (weapon drawn, leader standing: watch own outside half, turn/sweep, short footing shifts); Brian-approved; INSTALLED 2026-10-29 (hash `c2511673…`); live: companions stopped dead wherever the leader stopped (trigger bug); superseded |
| `lwc-core-1.0/1.0.32.19-ready-watch-trigger` | `bd1d7da` | ready watch only after re-acquiring the leased spot near the leader; INSTALLED 2026-10-29 (hash `b6f60615…`); superseded before play |
| `lwc-core-1.0/1.0.32.20-sneak-approach` | `f310b10` | 1.0.32.19 + far companions come up at pace and sneak only within 6 m of a crouching leader; INSTALLED 2026-10-29 (hash `53f7bd6d…`), live test pending (movement + poison/body states) |
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
