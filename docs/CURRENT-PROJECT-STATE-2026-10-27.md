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
| `lwc-core-1.0/1.0.32.20-sneak-approach` | `f310b10` | 1.0.32.19 + far companions come up at pace and sneak only within 6 m of a crouching leader; INSTALLED 2026-10-29 (hash `53f7bd6d…`); LIVE 16:08: traversal sound (Brian: "sublime"), party clouds/escape/coughs proven, poison wasted on undead (see CLAUDE-OUTBOX/2026-10-29-live-result-1.0.32.20) |
| `lwc-core-1.0/1.0.32.21-bomb-eligibility` | `31f269d` | 1.0.32.20 + no poison cloud on poison-immune groups (native damage modifiers); INSTALLED 2026-10-29 (hash `7a1f4de7…`), superseded by .22 install before its live test; backup in `LivingWorldBackups` |
| `work/formidable-ai-1.0.32.21` (1.0.32.22) | `034c539` | 1.0.32.21 + FormidableAI core (ChatGPT) + read-only shadow observer (`NpcDomain.None`, 2 Hz, `FormidableShadow` telemetry, schema 12) + analyzer schema-12 support (Claude); all gates pass; INSTALLED 2026-10-30 (hash `e0d16428…`, telemetry `4b5f56f9…`) with observer on; LIVE-PROVEN wiring/safety 2026-10-30: 1,838 s, 6,003 comparisons across 10 spawned companions, zero sequence gaps, zero domain violations and zero kernel/behavior faults; native combat remained authoritative; comparison mix is plumbing evidence, not a quality score |
| `work/bomb-ranged-acquisition` (1.0.32.24) | `5f54db8` | 1.0.32.22 + 1.0.32.23 read-only character profile (torch excluded from weapon doctrine, `2a276f2`) + bomb ranged area control (throw 15 m/s, band 9-20 m; decision law otherwise unchanged); Claude canonical build: ledger 131/131, native 46, ownership 76 sites, voice LOCKED unchanged, movement-writer OK, analyzer OK; INSTALLED 2026-10-30 (hash `86fabe84…`, telemetry `01946edb…`); superseded by 1.0.32.26 before its bomb test, backup in `LivingWorldBackups`, .22 backed up; observer still on; test = bomb acquisition/release range with video + telemetry, not a FormidableAI quality score |
| `work/awareness-a1` (1.0.32.26) | `eb28f1c` | `17edec6` (bomb world acquisition removed; = `5f54db8` combat behavior, 9-20 m band) + A1 shared awareness SHADOW: tactical-group adapter (`AwarenessGroupId`, hostility from native `BaseAI.IsEnemy`), companion-owned sight/hearing/contact, Noticed/Alerted/Committed/Lost, groups, empty KnowledgeBook, independent initiative ledger (native target/action edges, upper bounds, flush at stop, gap records), schema 13. Approved `572b450`. Claude canonical build: ledger 155/155, native 46, ownership 76 (unchanged), voice LOCKED unchanged, movement-writer OK, analyzer OK. INSTALLED 2026-10-30 (core `c95d9ef7…`, telemetry `da82cdca…`, both verified on device) with `AwarenessShadow = true` and `ShadowObserver = true`; .24 backed up. Shadow-only: no gameplay control. Next: short video session per presentation §8/§10.5 |
| `work/awareness-a1` (1.0.32.27) | `7c22507` | A1 rev 5: 10 Hz interpretation/group step, urgent 0.25 s refresh for confirming/alerted contacts, corrected native-first analyzer classification. Gates 157/157; native 46; ownership 76; voice lock unchanged; movement/analyzer checks pass. INSTALLED 2026-10-31 in isolated Arena profile from reproducible current-game Mono build (core `979972eb…`, telemetry `42298a29…`). LIVE-PROVEN 2026-10-31: 1,442 s, zero awareness/kernel/ownership faults, median awareness cost 0.55 ms/s, complete initiative records, only three true awareness-late cases (0.42 s median). Shadow-only; no gameplay authority. See `CODEX-OUTBOX/2026-10-31-a1-rev5-live-raids-wolves`. |
| `work/native-hair-attachment-1.0.32.28` (1.0.32.28) | `af31a58` | 1.0.32.27 (A1 rev 5 `7c22507` + analyzer `7ff725e`) + restored native hair/beard attach transaction (0.3.66-0.3.68 parity), saved-presentation repair without reroll, appearance gate. Claude canonical Mono build: ledger 157/157, native 56, appearance gate OK, ownership 82 (new sites all in NpcAppearance), voice LOCKED unchanged (not re-locked), movement-writer OK, analyzer OK. INSTALLED 2026-10-31 in `LWC Formidable Arena` (core `f9f3ac20…`, telemetry `7f9d737a…`, verified on device); .27 (`979972eb…`, voice lock verified) backed up. LIVE FAIL 2026-10-31: Alf (Hair4/Beard19) READY, second male failed on rigid Beard1 (party not kept); superseded by .29. Live gate was: one `lwparty`, two companions APPEARANCE READY with native hair (male beard / female none), one complete party |
| `work/native-hair-attachment-1.0.32.29` (1.0.32.29) | `1ec0f18` | .28 + readiness accepts both native attachment roots (skinned `attach_skin` and rigid `attach` under `m_visual`, e.g. Beard1/Hair1/Hair2); detailed beard failure reason. No class/combat/Souls/raid/feature changes. Claude canonical Mono build: ledger 157/157, native 57, appearance gate OK, ownership 82, voice LOCKED unchanged, movement-writer OK, analyzer OK. INSTALLED 2026-10-31 in `LWC Formidable Arena` (core `d376d63b…`, telemetry `f127b7f4…`, verified on device); .28 backed up. Live gate: one `lwparty` |
| `lwc-core-1.0/parked-1.0.33` | `965a2a0` | parked hot-path experiment; not installed |

## Latest live evidence — 2026-10-31

- 1.0.32.27 A1 revision 5 ordinary-raid/mountain session: **LIVE-PROVEN** safe and
  complete at two active observers. Awareness cost fell to 0.55 ms/s median;
  sight notice median was 49 m, commitment median was 36 m, and native member
  targeting followed commitment by 6.28 s median. This accepts the revision-5
  scheduling change and provides the live basis for A2 posture; A1 remained
  read-only.
- Bomb ranged release is **LIVE-PROVEN improved**: 25 throws, 9.1-20.0 m,
  median 15.7 m; 14 at 15 m or farther and none inside the 9 m floor. Video
  shows poison established ahead of the party. This does not claim FormidableAI
  bomb initiative because native combat still owned acquisition.
- Souls arena setup **FAILED** before any Souls actor could exist. RRRCore 3.1.5
  null-dereferenced in `Npc.DesignNpcBase`; all six requested `RRRN_*` prefabs
  were then missing from Custom Raids. The earlier plugin-stage launch gate was
  incomplete; world/ObjectDB initialization exposed the second incompatibility.
- The wolf raid killed the two functioning companions. Four actors requested
  afterward never left `Initializing`: two party transactions failed because
  selected hair objects never rendered, and no partial party was kept. The
  analyzer's “6 companions present” headline counts these transient samples and
  is not an operational party count.
- Full evidence and requested next actions:
  `claude-exchange/CODEX-OUTBOX/2026-10-31-a1-rev5-live-raids-wolves/`.

## Souls registration and Elder-pattern encounters — 2026-10-31

- **LIVE-PROVEN:** RRRCore 3.1.5 current-Player prefab compatibility on branch
  `work/rrrcore-souls-1.0.21` at `15d49d3`. Installed compatibility DLL hash
  `dd3073fa…` in the isolated Arena profile. One `LWC_HumanLab_v1` world load on
  Valheim 1.0.21 and C.O.R.E. 1.0.32.27 registered all six Souls prefabs as
  humanoids with MonsterAI and emitted `SOULS-PREFAB SUMMARY 6/6 ready`.
- The original natural Souls Spawn That roster remains configured for all six
  prefabs. The old packed arena/trial configs are backed up and replaced by two
  `army_theelder`-pattern events: `lwc_formidable_arena` and
  `lwc_formidable_souls_swamp`. Both use an explicit 70-95 m random ring and put
  the Legionnaire only in the one-at-a-time heavy slot.
- **LIVE-PROVEN registration gate:** Spawn That 1.2.20, Custom Raids 1.8.2,
  RRRCore 3.1.5 and the compatibility plug-in all loaded. The complete log has
  zero error lines, no RRRCore prefab exception and no missing Souls warning.
  Natural-spawn frequency and the two raid compositions/pacing are the next
  video/telemetry evidence. See
  `CODEX-OUTBOX/2026-10-31-souls-prefab-elder-raid/`.

- 1.0.32.26 A1 second session: **LIVE-PROVEN** for the narrow compatibility claim that a
  MonsterDB-created hostile retaining native `MonsterAI` is discovered and tracked by A1.
  Session `LWC-20261031-000022-LWC_HumanLab_v1-part00.jsonl` ran 965 s with two companions,
  zero awareness faults, zero ownership violations and no lost initiative/contact records.
- MonsterDB 0.4.0 cloned `Greydwarf` as `LWC_A1_Hostile`. Two clone instances produced complete
  Contact/Group/Initiative records, were selected by both companions and were killed normally.
- This is **not** long-range or tactical-quality proof: the clones entered the awareness envelope at
  only 5.7 m and 3.6 m and died quickly. It exercises the native `MonsterAI` reader, not the
  `VisibleOnly` fallback for actors with non-native custom AI. See
  `claude-exchange/CODEX-OUTBOX/2026-10-31-monsterdb-a1-live-result/`.

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
