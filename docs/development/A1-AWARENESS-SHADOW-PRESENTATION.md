# A1 shared awareness (shadow): implementation and verification

**Branch** `work/awareness-a1` · **commit** `d56c2c0` + actor-agnostic intent (see §9) · **version** 1.0.32.26 · base `17edec6` (combat behavior = `5f54db8`)
**Status:** OFFLINE-TESTED. Not installed. Gameplay unchanged. A2 not started.

## 1. What runs

```
 every frame, once per party (first companion kernel to tick drives it; owner only)
 ┌──────────────────────────────────────────────────────────────────────────────┐
 │ SENSE      4 Hz candidate pass: GetAllCharacters → hostile, not party,      │
 │            within 72 m (60 m × max salience) of any companion               │
 │            ├─ Contact: it hurt a companion (kernel hurt memory, 8 s)        │
 │            ├─ Hearing: companion BaseAI.CanHearTarget (no alert/intent)     │
 │            └─ Sight:   scheduled Linecast eye → centre, then → head,        │
 │                        view-blocking layers only, from a companion whose    │
 │                        field of regard holds it                             │
 ├──────────────────────────────────────────────────────────────────────────────┤
 │ CONTACT    ContactBook[32]: position, smoothed velocity, last seen, source, │
 │            observer, confidence; alert and targeting read only on Sight     │
 ├──────────────────────────────────────────────────────────────────────────────┤
 │ INTERPRET  None → Noticed → Alerted → Committed;  any → Lost → dropped      │
 │            closing speed, time to contact toward the party centroid         │
 ├──────────────────────────────────────────────────────────────────────────────┤
 │ GROUP      GroupBook[8]: 6 m single-link; members, alerted, committed,      │
 │            closing, time to contact, poison knowledge (Known/Unknown)       │
 ├──────────────────────────────────────────────────────────────────────────────┤
 │ POSTURE    StealthDoctrine → Calm/Observe/Readiness/HoldStealth/Combat      │
 │            (shadow fact only, A2 will consume it)                           │
 ├──────────────────────────────────────────────────────────────────────────────┤
 │ TELEMETRY  Contact (each transition) · Group (on change, 2 s while held) ·  │
 │            AwarenessBudget (every 5 s)          → nothing else reads it     │
 └──────────────────────────────────────────────────────────────────────────────┘
```

Files: `src/Kernel/Awareness.Pure.cs` (pure, ledger-tested), `src/Kernel/PartyAwareness.cs` (Unity reads),
one line in `NpcKernel.Update`, config in `CorePlugin`, schema 13 in `CoreTelemetry`, `tools/analyze_session.py`.

## 2. Implemented values

| Parameter | Value |
|---|---|
| Clear-day notice ceiling | 60 m |
| Environment factor | ordinary dark (night or not daylight) 35/60 (= 35 m); weather, biome and near light are **recorded, not yet applied** |
| Target salience | 1.0; −0.2 radius < 0.4 m; +0.1 radius ≥ 1 m; +0.1 moving > 1 m/s; bounded 0.6–1.2 |
| Observer training | 1.0 for everyone (hook only) |
| Field of regard | facing ±100°; 360° within 12 m |
| Candidate pass | 4 Hz per party, ≤ 48 candidates (overflow counted) |
| Ray budget | 48 rays/s per party, burst 12; ≤ 2 rays per sample (centre, then head) |
| Refresh | committed/closing and anything ≤ 20 m: 0.25 s; others 1 s |
| Scheduling | urgency band → time to contact → distance; observer = fewest rays this second, nearest on ties |
| Noticing | 2 perception samples ≥ 0.25 s apart, or 1 Contact |
| Commitment | hurt party; or perceptible targeting (native target + facing ≤ 30°, charging ≥ 1.5 m/s or attacking, while sighted, fresh 0.6 s); or alert approach ≥ 1.0 m/s, heading ≤ 35°, sustained 1.5 s (grace 0.5 s) |
| Decommit | 3 s without targeting, approach or harm → Alerted (Noticed if no alert seen) |
| Staleness | Lost at 3 s unseen; dropped at 10 s (committed 20 s); never-noticed samples forgotten at 3 s |
| Capacity | 32 contacts (stalest non-committed evicted; committed never; refusals counted), 8 groups (overflow counted), 8 observers, 8 parties |
| Wildlife / sleeping | never commit by approach; only by harm or perceptible targeting; never in hostile groups |

## 3. Source of every fact

| Fact | Source | Fair basis |
|---|---|---|
| Position, velocity, size | `transform`, `GetVelocity`, `GetRadius` | visible body, sampled only when sighted/heard |
| Seen | `Physics.Linecast` from the companion's `m_eye` on Default, static_solid, Default_small, piece, terrain, viewblock, vehicle | its own eyes; characters don't block |
| Heard | companion `BaseAI.CanHearTarget` | native hearing (target noise range) |
| Alerted | creature `BaseAI.IsAlerted`, **only on a Sight sample** | alert stance and run are visible |
| Targets a party member | `MonsterAI.GetTargetCreature`, **only with** facing ≤ 30°, charging, or `InAttack`, while sighted | the behavior is visible |
| Hurt the party | companion kernel `RecentAttacker(8 s)` | it was hit |
| Wildlife / sleeping / boss | `GetFaction`, `IsSleeping`, `IsBoss` | visible kind/state |
| Family | `Character.m_name` token | identity of the creature type |
| Party centroid | companions' bodies + **leader's body position only** | where "toward us" points; the leader's camera, facing and sight are never inputs |
| Environment | `EnvMan` night/daylight, current environment name, `Heightmap.FindBiome` | recorded for tuning |
| Poison susceptibility | `KnowledgeBook`, **empty in A1** → Unknown | native modifiers are never read into it |

**Remaining unknowns:**
- weather, fog and Mistlands factors (recorded, not applied);
- observer training;
- the knowledge starting set;
- precise attack tells (only `InAttack`);
- cover and terrain beyond line of sight;
- live cost.

The live bomb rule still reads native poison modifiers (unchanged gameplay); A5 replaces it.

## 4. Verification

| Gate | Result |
|---|---|
| Regression ledger | **144/144** (131 + 13 A1 tests) |
| Native gate | 46 members verified |
| Ownership gate | **76 native write sites, unchanged**: A1 adds none |
| Combat-voice lock | LOCKED, unchanged |
| Movement-writer gate | 13 writer sites, unchanged |
| Analyzer self-test | OK |
| DLLs | `LivingWorld.HumanNpc.dll` `e30ebee7c00c151f5c819bbf936d73f4aebcb9f690ec6f4f6e80ed14d5a4f1b6` · `LivingWorld.Telemetry.dll` `209888db7404c8de055d7bddc644af9b731de4e31ac3e44661018fbb557b6765` |

**The 13 A1 tests cover:**
- notice and alert;
- commitment evidence;
- hidden target is not evidence;
- decommit;
- Lost and drop (10 s and 20 s);
- field of regard and occlusion;
- notice range;
- grouping;
- capacity and eviction;
- fair scheduling and budget;
- sneak doctrine;
- unknown resistance;
- wildlife.

## 5. Cost

Not measurable offline. The first session measures, every 5 s per party (`AwarenessBudget`):
- `cpuMsPerSec`: Stopwatch around the whole service step;
- `allocKbPerSec`: managed-heap growth across steps (coarse; a GC can hide it);
- `raysPerSec` and `rays_<observer>`: rays per companion;
- `raysDeferred`, `stepsPerSec`, `contacts`, `groups`, `evicted`, `refused`, `candidatesDropped`, `groupOverflow`, `faults`.

Per-frame allocation was removed by design: fixed arrays, and strings built only on transitions. The per-transition and
per-5 s telemetry strings are the remaining allocations. The analyzer FAILS the session on any awareness fault or a
ray peak above 60/s.

## 6. Sample: one contact through the states

**SIMULATED** with the pure core, not live: a Greydwarf idles at 55 m, alerts at 4 s, charges companion 1 from 5 s,
hits at 16 s, flees out of sight at 19.5 s.

```
{"type":"Contact","gameT":0.5,"family":"$enemy_greydwarf","from":"None","to":"Noticed","source":"Sight","observer":"Stig Leifsson","dist":55.6,"closing":0.1,"ttc":null,"alertedObserved":false,"targetsWhom":-1,"evidence":"2 samples"}
{"type":"Contact","gameT":4.5,"from":"Noticed","to":"Alerted","source":"Sight","dist":55.3,"alertedObserved":true,"evidence":"alert behavior seen"}
{"type":"Contact","gameT":5.5,"from":"Alerted","to":"Committed","source":"Sight","dist":53.3,"closing":1.4,"ttc":37.7,"targetsWhom":1,"evidence":"targets companion 1"}
{"type":"Contact","gameT":21.0,"from":"Committed","to":"Alerted","source":"Sight","observer":"Tove Halfdansdottir","dist":18.3,"closing":-4.3,"evidence":"no targeting, approach or harm for 3s"}
{"type":"Contact","gameT":22.0,"from":"Alerted","to":"Lost","dist":18.3,"evidence":"not seen 3s"}
```

Live records also carry `dark`, `weather`, `biome`, `noticeRange` and `posture`.

## 7. Gameplay is unchanged

- `[FormidableAI] AwarenessShadow` defaults to **false**. Off, the service returns at its first line.
- On, nothing reads it: no phase, target, movement, look, hands, combat, bomb, bow or voice code references
  `PartyAwareness`. It has no domain and no native write (ownership gate unchanged at 76 sites). Its only outputs are
  telemetry records.
- Faults inside it are caught and counted in its own telemetry; they cannot reach the kernel's decision loop.
- Bomb behavior is the corrected `17edec6` (= `5f54db8`): throws only in real combat, 9–20 m band.

## 8. Video test (install 1.0.32.26 with `AwarenessShadow = true`)

Record OBS for the whole session. Say or type the segment name at its start (chat or `lwstatus`) so I can align video
and telemetry.

1. **Clear day, open ground, party standing still.** Let creatures wander into view at 40–60 m. Note when *you* first
   see each one.
2. **Distant aggro.** Draw a group's attention from 30 m or more and let it charge the party.
3. **Occlusion.** Keep a creature behind a hill or rock while it moves near the party, then let it step into view.
4. **Night.** One encounter after dark.
5. **Sneak.** Crouch near an alerted creature that hasn't targeted you; then let one charge you while you're crouched.
6. **Wildlife.** A deer or boar running past or away.

**Accept A1 if:**
- zero awareness faults and a ray peak of 60/s or less (analyzer PASS);
- companions behave exactly as in .24 (no new movement, look, weapon or bomb behavior; the analyzer's gameplay checks
  don't regress);
- `Noticed` lands within about 2 s of a creature becoming visible in the video, on day encounters 40–60 m out;
- `Committed` appears only when the video shows a hit, a charge, facing and attacking, or a sustained approach;
  never for idle, passing or wildlife creatures;
- occluded creatures are not `Sight`-noticed until visible (`Hearing` may notice them);
- `posture` reads Observe → Readiness → Combat in step with the video, and HoldStealth while sneaking until a
  commitment;
- `cpuMsPerSec` median below 1 ms per second, and heap growth near zero between transitions.

**Reject or revise if:**
- any awareness fault;
- a ray peak above 60/s;
- any gameplay difference from .24;
- Sight noticing through solid terrain or structures;
- commitment without visible evidence;
- consistent noticing more than 5 s after a creature is plainly visible in clear day (range or regard too
  narrow);
- CPU above 2 ms per second.

## 9. Update: aggro is aggro (Brian, after presentation)

Intent is now read the same way whoever generates the aggro, and the service is keyed by **side**:

| Actor | Reader | Target / alert from |
|---|---|---|
| Our FormidableAI actors (companions now; camp NPCs later) | `FormidableActor` | its kernel's target and phase |
| Vanilla creatures and mod creatures using `MonsterAI` | `NativeMonsterAI` | native target, native alert |
| Other `BaseAI` actors | `NativeBaseAI` | native alert; target by visible behavior |
| Anything without a reader | `VisibleOnly` | **visible behavior only**: facing a member and charging ≥ 1.5 m/s, or attacking one within 4 m facing it; running ≥ 3 m/s reads as alert |

A reported target still counts as evidence only when perceptible, so no reader grants hidden knowledge. Hostile actors of
another side, including other FormidableAI actors, are ordinary contacts; only this side's own members are skipped.
Today the only wired side is a player's companion party. Camps of our hostile and friendly NPCs use the same core,
rules and readers under their own side key. `AwarenessBudget` reports how many judgments each reader made.

Ledger 145/145 (+1 visible-intent test). Gates unchanged (ownership 76, voice LOCKED, movement-writer OK).
DLLs: core `6310a7f76e3dd4dc69ebcfcc765bb19b8772dd18598341eaccd75186390cf59e`, telemetry `1632867e90184efb78c7d63fe01bfc6a96ad3484ee1ded65a0132b5eedcdec91`.
