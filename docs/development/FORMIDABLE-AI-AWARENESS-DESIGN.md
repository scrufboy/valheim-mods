# FormidableAI shared awareness: design for review (no code yet)

Status: DESIGN. Nothing here is implemented. Written 2026-10-30 by Claude against 1.0.x source at
`work/bomb-ranged-acquisition` `17edec6` (combat behavior = `5f54db8`), the 0.6.4 state contract
(`cbc0f0f`), the 1.0.4 engagement rules (`78bc786`) and Brian's decisions of 2026-10-30.

Order of authority this design implements:
**awareness notices → interpretation (alert, approach, commitment, group) → arbitration chooses → the skill
validates execution → native Valheim mechanics decide the outcome.**

## 1. What exists today (SOURCE-INSPECTED)

Companion native senses (`NpcFactory`): `m_viewRange 30`, `m_viewAngle 90` (a 90° cone ahead),
`m_alertRange 20`, `m_hearRange 9999` (hearing is bounded by the target's own noise range).

There are six separate world scanners and no shared contact memory:

| Scanner | Where | Range / gate | Cadence | Output |
|---|---|---|---|---|
| `TargetSelector.Build` | `FindEnemy` postfix | all characters ≤60 m, cap 16, `CanSenseTarget` (30 m/90° sight or noise) | native `MonsterAI.UpdateTarget` timer (IL constants 2 s / 6 s; INFERRED, not measured) | one native target |
| `NpcKernel.ScanHostiles` | kernel senses | ≤30 m, **only enemies already alerted** | kernel scan | nearest alerted hostile |
| `StepBomb` | native combat | ≤30 m, `CanSeeTarget`, cap 24 | 0.2 s in combat | throw plan |
| `ObserveThreats` | block policy | local | per tick in combat | block threats |
| `NpcBody.CountEnemies` | heavy attack | area radius | per heavy decision | counts |
| shadow `TargetsActor` | observer | target only | 2 Hz | fact |

Consequences: no "what's that?" stage, no approach/commitment reading, no groups, no memory of a contact
once it leaves the 90° cone, and every module that needs contacts scans for itself. The 0.6.5 rebuild removed a
failed rewrite of the native scan constants and added no replacement loop, so extended noticing never existed.

## 2. One awareness service per party

`PartyAwareness`: one instance per leader (party), owned by the kernel layer, with **no native writes and no
domain**. It is the only broad character scanner. Every companion's kernel contributes its own perception
samples to it and reads facts from it. Party sharing is a fair source: companions call out what they see.

### 2.1 Fair sources (each sample is tagged with one)

| Source | Rule |
|---|---|
| `Sight` | A line-of-sight ray from the companion's eye to the contact's centre (and one to its head) is unoccluded (`viewblock`, terrain, static, piece layers), the contact is within the notice range, and it is inside the companion's **field of regard** (§2.4). |
| `Hearing` | Native `CanHearTarget` (the target's own noise range). Gives position and "something there", never alert state. |
| `Contact` | The contact hurt or touched a party member (damage event). |
| `Party` | Another companion's `Sight`/`Hearing`/`Contact` sample (a callout). |
| `Leader` *(Brian to confirm)* | Line of sight from the leader's position. Brian: "if I can see them, they can see them." |

Never a source: a contact behind walls with no sound, private state with no visible consequence, future outcomes.

### 2.2 Contact record (fixed capacity, reused)

```
ContactRecord                       // capacity 32 per party; oldest non-committed evicted first
  int    Id                         // Character instance id (also the ZDOID string for telemetry)
  Character Actor                   // runtime only, never persisted
  string Kind                       // prefab, for enemy-family knowledge
  bool   Wildlife, Boss
  ContactState State                // see §3
  Source LastSource                 // which fair source last confirmed it
  float  FirstNoticed, LastSeen     // Time.time
  P2     LastKnownPos; float Height
  P2     Velocity                   // smoothed over the evidence window
  float  ClosingSpeed               // + toward the party centroid, m/s
  float  TimeToContact              // distance / closing speed when closing, +inf otherwise
  bool   Alerted                    // its own BaseAI.IsAlerted(); visible as alert stance/run
  int    TargetsWhom                // -1 none, else party member id (native target; visible as it turning/charging)
  bool   HurtParty                  // within hurt memory (8 s, existing)
  float  CommitEvidence             // seconds of sustained closing inside the window
  float  Confidence                 // 0..1, decays with staleness
  int    GroupId
```

### 2.3 Group record

```
GroupRecord                         // capacity 8 per party
  int   Id; int Members; int Alerted; int Committed; int Poisonable (from native modifiers)
  P2    Centroid; P2 Velocity; float Radius
  float Distance, ClosingSpeed, TimeToContact     // to the party centroid
  bool  Committed                   // any member TargetsWhom party or HurtParty, or ≥ half the members closing (§3)
  int   NearestMemberId
```

Grouping: single-link clustering of non-wildlife hostile contacts within **6 m** of another member,
recomputed when membership-relevant positions change by more than 1 m. Fixed arrays, no allocation per frame.

### 2.4 Range, field of regard and budget (proposed numbers; to be tested, not assumed)

| Parameter | Proposed | Why |
|---|---|---|
| Notice range, daylight | **60 m** | Equals the existing `PartnerReach`. Double today's 30 m. Covers the 9–20 m bomb band with time to react. |
| Notice range, dark (`EnvMan` night/not daylight) | **35 m** | Fair: the player also sees less at night. |
| Field of regard | facing **±100°** (head and shoulder turn) out to the notice range; **360° within 12 m** (peripheral/hearing) | Companions turn their heads (ReadyWatch, guard sweep). Without a field of regard they would see like a 360° camera. |
| Candidate enumeration | `GetAllCharacters` distance filter, **4 Hz per party** | Cheap distance pass. One per party, not per companion. |
| Line-of-sight raycasts | **≤ 48 per party per second**, round-robin; contacts closer than 20 m or committed refreshed every 0.25 s, others every 1 s | A bounded, measurable budget. Telemetry reports the real ray count. |
| Staleness | not seen 3 s → `Lost` (last known position kept); 10 s → dropped; committed 20 s | Memory survives brief occlusion without becoming omniscience. |
| Evidence window | 1.5 s of motion samples | Estimates velocity/closing. It is not an action timer. |

## 3. Transitions (evidence thresholds, not timers)

```
Unseen ──(2 Sight samples ≥0.25 s apart, or 1 Contact)──▶ Noticed
Noticed ──(Alerted)──▶ Alerted
Noticed/Alerted ──(commitment evidence)──▶ Committed
any ──(not seen 3 s)──▶ Lost ──(seen again)──▶ previous state
Lost ──(10 s; 20 s if it was committed)──▶ dropped
Committed ──(no target on party, not closing, not hurt party for 3 s)──▶ Alerted
```

Commitment evidence. Any one of these is enough; each is a visible behavior:
1. its native target is a party member (0.6.4 definition: `GetTargetCreature()`);
2. it hurt a party member (existing hurt memory);
3. sustained approach: `ClosingSpeed ≥ 1.0 m/s` with heading within 35° of the party centroid, for ≥ 1.5 s inside
   the window, while `Alerted`.

Merely existing, being visible, or being inside bomb range is **not** commitment (b1d2d63 #5).

Wildlife and sleeping creatures follow the 1.0.4 rules: never committed unless they target or hurt the party.
While the leader sneaks, only rule 1 and rule 2 commit (existing `TargetPolicy.Engaged` sneak rule).

## 4. Ownership map and what each consumer reads

| Consumer | Today | After migration |
|---|---|---|
| Phase arbiter (`Kernel.Pure` phase machine) | `LeaderArmed`/`NativeAlert` → Readiness; target/damage → Combat | **0.6.4 order preserved**: leader armed **or an alerted/noticed contact within notice range** → Readiness; **committed contact/group** → Combat (0.6.4 `CommittedThreat → NativeCombat`). Clear verification unchanged. |
| `TargetSelector` (answers `FindEnemy`) | own 60 m scan | candidates come from the contact book; `TargetPolicy` engagement rules (1.0.4, Sparta partner defense) unchanged, with `Committed` taken from the contact record. |
| `NpcKernel.ScanHostiles` | own 30 m alerted scan | removed; `NearestAlertedHostile` derived from contacts. |
| Bomb (`StepBomb`) | own 30 m scan in combat | **no discovery**. Candidates are committed groups from the book. It keeps only execution validation: own `CanSeeTarget` on the aim member, trajectory, lead, friend clearance, cooldown. |
| Bow / native weapon choice | native | unchanged; it benefits because commitment arrives earlier. |
| AwarenessLook / ReadyWatch / guard sweep | nearest alerted hostile | look at the most relevant contact (committed > alerted > noticed); this also widens the field of regard. |
| Combat voice (LOCKED) | target-change hook | **unchanged**. Any callout ("over there!") is a new expression module consuming transitions; the locked module is not edited. |
| Block (`ObserveThreats`), heavy (`CountEnemies`) | local scans | stay **local execution checks** (a few metres), not discovery. |
| FormidableAI shadow | target only | reads contact and group facts for its frame. |

The opening bomb is not a bomb decision. A **committed group** in the 9–20 m band moves the phase to Combat
by the 0.6.4 rule. The arbiter then lets native combat take the action, and the bomb skill validates the throw
against the committed group. Nothing scans "for something to bomb".

**Open point (Brian):** `TargetPolicy.MaxRange` is 30 m. A committed group at 40–60 m is known but not
engageable. Proposal: committed contacts are engageable at range for **ranged responses only** (bow, bomb); the
melee chase leash stays 30 m.

## 5. Migration (one change set per test, per Brian's cadence)

1. **A1 shadow awareness.** `PartyAwareness` runs read-only. Telemetry only: `Contact` and `Group` transition
   events with source, distance, closing speed and evidence. Graded against video: when did each companion
   notice each group? Is the ray budget held? Does nothing change in behavior?
2. **A2 senses.** `NearestAlertedHostile` and the Readiness rule come from contacts (0.6.4 order).
3. **A3 targeting.** `TargetSelector` candidates come from contacts; engagement rules unchanged.
4. **A4 commitment.** Committed contact/group → Combat.
5. **A5 opening area control.** The bomb validates against committed groups; ranged engagement of committed
   contacts beyond 30 m if Brian accepts the open point.

Each step is its own build: ledger tests for the pure policy (transitions, grouping, budget), the existing
gates, and a live test.

## 6. Telemetry

- `Contact`: `{id, kind, state from→to, source, dist, closing, ttc, alerted, targetsWhom, evidence, companion}`
- `Group`: `{id, members, committed, dist, closing, ttc, poisonable}`
- `AwarenessBudget` every 5 s: `{contacts, groups, raysPerSec, enumHz, dropped}`
- Bomb and target logs reference the contact/group id that justified the action.

## 7. Questions for Brian

1. Is leader line of sight a companion source ("if I can see them, they can see them")?
2. Notice ranges 60 m day / 35 m dark: right starting values to test?
3. Ranged-only engagement of committed contacts beyond 30 m?
4. While you sneak, should companions still open with ranged area control on a committed group, or hold?
