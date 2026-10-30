# FormidableAI shared awareness: design for review (no code yet)

Status: DESIGN, revision 2 (applies `a3ce8e0`). Nothing here is implemented. Sources: 1.0.x at
`work/bomb-ranged-acquisition` `17edec6` (combat behavior = `5f54db8`), the 0.6.4 state contract (`cbc0f0f`), the
1.0.4 engagement rules (`78bc786`), and Brian's decisions of 2026-10-30.

Order of authority:
**awareness notices → interpretation (alert, approach, commitment, group, what we know about it) → arbitration
chooses → the skill validates its own execution → native Valheim mechanics decide the outcome.**

Answers are derived from the situation, the character's capability and its knowledge. There are no global
fire/no-fire switches.

## 1. What exists today (SOURCE-INSPECTED)

Companion native senses (`NpcFactory`): `m_viewRange 30`, `m_viewAngle 90` (a 90° cone ahead),
`m_alertRange 20`, `m_hearRange 9999` (hearing is bounded by the target's own noise range).

Six separate world scanners and no shared contact memory:

| Scanner | Where | Range / gate | Cadence | Output |
|---|---|---|---|---|
| `TargetSelector.Build` | `FindEnemy` postfix | all characters ≤60 m, cap 16, `CanSenseTarget` (30 m/90° sight or noise) | native `MonsterAI.UpdateTarget` timer (IL constants 2 s / 6 s; INFERRED) | one native target |
| `NpcKernel.ScanHostiles` | kernel senses | ≤30 m, **only enemies already alerted** | kernel scan | nearest alerted hostile |
| `StepBomb` | native combat | ≤30 m, `CanSeeTarget`, cap 24 | 0.2 s in combat | throw plan |
| `ObserveThreats` | block policy | local | per tick in combat | block threats |
| `NpcBody.CountEnemies` | heavy attack | area radius | per heavy decision | counts |
| shadow `TargetsActor` | observer | target only | 2 Hz | fact |

Consequences: there is no "what's that?" stage, no reading of approach or commitment, and no groups. A contact
is forgotten once it leaves the 90° cone, and every module that needs contacts scans for itself. 0.6.5 removed a
failed rewrite of the native scan constants and added no replacement, so extended noticing never existed.

**Knowledge leak already live:** the 1.0.32.21 bomb eligibility (`BodyOf`) reads `GetDamageModifiers().m_poison`
directly. Under the 2026-10-30 decision this is tactical omniscience. §2.5 gives it a legitimate source.

## 2. One awareness service per party

`PartyAwareness`: one instance per leader, owned by the kernel layer, **no native writes, no domain**. It is the
only broad character scanner. Each companion contributes its own perception samples and reads the shared facts.

### 2.1 Fair sources (every sample is tagged)

| Source | Establishes | Never establishes |
|---|---|---|
| `Sight` (the companion's own line of sight, inside its field of regard) | existence, position, motion, visible behavior | n/a |
| `Hearing` (native `CanHearTarget`) | "something there", rough position | alert state, identity of intent |
| `Contact` (it hurt or touched a party member) | existence, hostility, commitment | n/a |
| `PartyReport` (another companion's sample) | that the contact exists and its **reported** last-known position | **any companion's attack lane**: whoever shoots or throws validates its own current line, arc and friend clearance |

Brian's sight is not a source or prerequisite. "If I can see them, they can see them" means a normally rendered,
unoccluded enemy is fairly observable by competent companions using their own perception. A future explicit
player command/mark could create a report, but ordinary awareness never depends on Brian looking first.

### 2.2 Contact record (fixed capacity, reused; transient, never persisted)

```
ContactRecord                       // capacity 32 per party; oldest non-committed evicted first
  int    Id; Character Actor        // Actor runtime only
  string Family                     // enemy family (prefab group) used to query knowledge, §2.5
  bool   Wildlife, Boss
  ContactState State                // §3
  Source LastSource; float FirstNoticed, LastSeen
  P2     LastKnownPos; float Height; P2 Velocity
  float  ClosingSpeed, TimeToContact   // toward the party centroid
  bool   AlertedObserved            // alert stance/run perceived (its BaseAI alert corroborated by visible behavior)
  int    TargetsWhomObserved        // party member it is perceptibly facing/charging/attacking, or -1 (§3 rule)
  bool   HurtParty                  // within the existing 8 s hurt memory
  float  CommitEvidence, Confidence // confidence decays with staleness and source quality
  float  Salience                   // target size x motion, feeds visibility
  int    GroupId
```

### 2.3 Group record

```
GroupRecord                         // capacity 8 per party
  int   Id, Members, AlertedObserved, Committed
  P2    Centroid, Velocity; float Radius, Distance, ClosingSpeed, TimeToContact
  bool  Committed
  KnowledgeView Susceptibility      // per damage type: Known/Unknown + confidence, from §2.5, never from native modifiers
  int   NearestMemberId
```

Grouping: single-link within 6 m among non-wildlife hostile contacts, recomputed when positions move more than
1 m. Fixed arrays.

### 2.4 Visibility, range and ray budget

**Notice range = clear-day ceiling × environment visibility × target salience.**

| Term | First test value | Notes |
|---|---|---|
| Clear-day ceiling | **60 m** | Shadow-test maximum. Noticing range, **not** attack range. |
| Environment visibility | ordinary dark = 35/60 (temporary); fog, weather, Mistlands obscurity and local light enter as factors once measured | Every Contact event records the environment (dark, fog/weather name, biome, nearby light) so video can tune it. |
| Target salience | 1.0 baseline; larger or moving targets higher, small or still ones lower | Bounded 0.6–1.2 for the first test. |
| Observer training | 1.0 for every companion now | Hook for biome origin and training (e.g. Mistlands-trained in mist), from the character profile. |
| Field of regard | facing ±100° (head and shoulder turn) to the notice range; 360° within 12 m | Look behaviors (ReadyWatch, guard sweep, AwarenessLook) widen coverage over time, which is why looking around matters. |

**Who spends the rays, and fairness.** The party has one scheduler and a budget of ≤ 48 line-of-sight rays per
second. Each tick it serves contacts in **urgency order**: committed and closing first, then by time to contact,
then contacts nearer than 20 m. Within each urgency band it rotates **round-robin across companion observers**,
so no companion is starved and an urgent contact is never queued behind a distant
idle one. A contact's ray is cast from the observer best placed to see it (nearest, with it inside that observer's
field of regard). The candidate distance pass (`GetAllCharacters`) runs at 4 Hz per party. Telemetry reports
rays spent per observer.

**Staleness:** not seen for 3 s → `Lost` (last-known position kept); dropped after 10 s, or 20 s if it was
committed. The 1.5 s evidence window estimates motion; it is not an action timer.

### 2.5 Knowledge is separate from contact memory

`KnowledgeBook` is the persistent learned knowledge (the 2026-10-30 learned-knowledge decision): compact fact ids
with provenance (training, biome doctrine, lore/compendium, observed combat), stored per person and per party.
Awareness interpretation **queries** it by enemy family and never writes contacts into it. Contacts stay
transient.

- `Susceptibility(family, damageType)` returns Known(value, confidence) or Unknown. **Unknown is a real
  uncertainty in arbitration**: a bomb proposal against an unknown family carries lower expected value, it is
  not forbidden.
- Observed combat updates knowledge legitimately. A companion who saw a cloud do nothing to Skeletons learns
  "skeleton family: poison immune, observed".
- Execution physics stay exact (native damage). Only the tactical expectation is knowledge-bound.
- **Migration for the live bomb rule:** replace `BodyOf`'s native-modifier read with the knowledge query. To keep
  today's liked behavior legitimately, companions would start with a small set of common-knowledge facts as
  biome training (e.g. undead families are poison-immune). Whether that starting set is right is a
  **character/lore choice for Brian** (§7).

## 3. Interpretation (evidence thresholds, not timers)

```
Unseen ──(2 Sight samples ≥0.25 s apart, a Contact, or a companion PartyReport)──▶ Noticed
Noticed ──(alert behavior perceived)──▶ Alerted
Noticed/Alerted ──(commitment evidence)──▶ Committed
any ──(not seen 3 s)──▶ Lost ──(seen again)──▶ previous state; dropped per §2.4
Committed ──(3 s with no perceived targeting/approach and no harm)──▶ Alerted
```

**Commitment evidence** (any one):
1. It hurt a party member (`Contact`).
2. Its native target is a party member **and** that is perceptible: it is facing that member within 30°, charging,
   or in an attack on them, or a contact event has occurred. A native target alone, with no visible behavior, is not
   evidence.
3. Sustained approach while alert: closing speed ≥ 1.0 m/s, heading within 35° of the party centroid, for ≥ 1.5 s.

Being visible, being alert at a distance, or being inside any weapon's range is **not** commitment.
Wildlife and sleeping creatures follow the 1.0.4 rules: they commit only by rules 1–2.

## 4. What each layer does with it

### 4.1 Phase (0.6.4 order preserved, with an Observe step added)

| Evidence | Phase/posture | Weapons |
|---|---|---|
| Noticed only, or wildlife, or unconfirmed | **Observe/Investigate**: look at it, track it, Pathfinder-type investigates | stay stowed |
| Credible threat: Alerted non-wildlife hostile, or a known-hostile family closing | **Readiness** (as with leader armed / native alert today) | ready, form up |
| Committed contact or group | **Combat** (0.6.4 `CommittedThreat → NativeCombat`) | per capability (§4.2) |

The leader's weapon still shapes posture, but it is not a prerequisite (decision 2026-10-30).

### 4.2 Responses are bounded by capability

| Capability | May act on a committed contact when | Otherwise awareness lets it |
|---|---|---|
| Bow (trained archer) | target inside the bow's effective envelope and the archer has its **own** clear lane | ready, take position, wait for the window |
| Bomb (9–20 m envelope) | a committed group is inside 9–20 m and the thrower has its own arc and friend clearance; susceptibility from knowledge | prepare, then throw when the group enters the envelope (intercept window) |
| Melee | inside the 30 m leash | hold the line, guard partner/leader, cover |
| Everyone | n/a | warn, form up, reposition, take cover, withdraw, hold |

### 4.3 Sneaking changes costs; it is not a veto

While the leader sneaks, initiating breaks stealth, so its cost is high:
- Noticed, alerted or suspicious contacts: companions observe and stay hidden.
- Committed (it targeted or hurt the party, or sustained closing makes interception unavoidable): stealth has
  failed. The arbiter picks the strongest survival response, which may be an opening bomb or bow shot.

### 4.4 Ownership map

| Consumer | Today | After migration |
|---|---|---|
| Phase arbiter | `LeaderArmed`/`NativeAlert` → Readiness; target/damage → Combat | §4.1 |
| `TargetSelector` (answers `FindEnemy`) | own 60 m scan | candidates from contacts; 1.0.4 / Sparta rules unchanged; `Committed` from §3 |
| `NpcKernel.ScanHostiles` | own 30 m alerted scan | removed; derived from contacts |
| Bomb (`StepBomb`) | own 30 m scan in combat | **no discovery**: validates a proposal against a committed group (own `CanSeeTarget`, arc, lead, friend clearance, cooldown) |
| Bow / native weapon choice | native | §4.2 envelope; earlier commitment helps |
| AwarenessLook / ReadyWatch / guard sweep | nearest alerted hostile | look at the most relevant contact (committed > alerted > noticed) |
| Combat voice (LOCKED) | target-change hook | unchanged; callouts are a new expression module consuming transitions |
| Block (`ObserveThreats`), heavy (`CountEnemies`) | local scans | stay local execution checks |
| FormidableAI shadow | target only | reads contact and group facts |

## 5. Migration (one change set per test)

1. **A1 shadow awareness:** read-only; `Contact`/`Group`/`AwarenessBudget` telemetry with environment fields. Graded
   against video: when did each companion notice each group, and was the budget held? No behavior change.
2. **A2 posture:** Observe vs Readiness from contacts (§4.1).
3. **A3 targeting:** `TargetSelector` candidates from contacts.
4. **A4 commitment → Combat.**
5. **A5 knowledge and responses:** `KnowledgeBook` replaces native-modifier reads; bomb and bow act per §4.2 and §4.3.

## 6. Telemetry

- `Contact`: `{id, family, state from→to, source, observer, dist, closing, ttc, alertedObserved, targetsWhomObserved, evidence, env{dark, weather, biome, light}}`
- `Group`: `{id, members, committed, dist, closing, ttc, susceptibility{known/unknown}}`
- `AwarenessBudget` every 5 s: `{contacts, groups, raysPerSec, raysByObserver, enumHz, dropped}`
- Action logs cite the contact/group id and the knowledge facts that justified them.

## 7. Character/lore choice for Brian (not a runtime switch)

Which enemy facts do companions start knowing as common biome training, versus having to learn by lore or by
watching? Example: "undead are immune to poison". Starting with it keeps the bomb behavior you liked and makes
it legitimate. Leaving it out means companions learn it the first time a cloud fails on Skeletons.
