# Specialist combat capability plan

Status: implementation handoff after the 0.3.58 moving-anchor defense line.

## Non-negotiable combat boundary

`MonsterAI` remains the exclusive owner of target approach, locomotion, facing,
ordinary weapon selection, attack cadence, attack animation, projectile creation,
damage, stamina/resource debit, and hit resolution whenever a valid native attack
can express the role.

Living World may prepare actor-local equipment, authorize targets, define tactical
bounds, reject unsafe attacks, supply prediction input, and request a bounded
special action through the same exclusive combat owner. It may not run a parallel
general combat controller.

## Capability modules, not one-off classes

Classes are data compositions over reusable capabilities:

- `CombatLoadoutProfile` — allowed weapons, preferred ranges, fallback weapon,
  shield pairing, armor presentation, finite ammunition and resources.
- `ThrownProjectileCapability` — spear, javelin, bomb, flask, or future thrown
  object; owns prediction and safe-release gates, not ordinary pursuit.
- `CommittedLungeCapability` — knife or short-weapon step attack with predicted
  contact and abort rules.
- `HeavyPressureCapability` — slow high-commitment swings, bash/stagger preference,
  recovery windows, and friendly-space checks.
- `MagicCastCapability` — reserve, cooldown, target/area validity, cast anchor,
  hand/staff VFX lease, and interruption.
- `CombatPresentationProfile` — native animation names, hand/back attachments,
  VFX/SFX anchors, readiness stance, and raid assembly cue.

The Plains heavy profile can later drive a shorter, stockier Black Forest or
Mountain axe fighter by changing body presentation, equipment, damage, reach,
and attack set without duplicating decision code.

## Predictive aim service

All ranged and committed movement attacks use one service. It predicts without
granting impossible knowledge.

Inputs:

- target position and a short, recent velocity sample;
- projectile origin, speed, gravity, fuse/arming time, blast radius, and maximum
  flight time;
- actor aim error, target visibility age, obstruction result, ally positions,
  and authorized encounter bounds;
- for lunges: attack wind-up, root/step distance supplied by the attack, target
  radius, and reachable ground at the proposed end point.

For a constant-velocity target, solve interception iteratively because flight time
depends on the eventual aim point. Apply gravity compensation to ballistic throws.
Bomb prediction targets the future ground-impact area at detonation time, not the
target's current chest. A spear may target center mass for a direct hit or a safe
ground point for retrieval, according to the native projectile contract.

Fairness rules:

- velocity comes only from recently sensed motion;
- prediction confidence decays with visibility age and direction changes;
- skill/class spread adds deterministic bounded error;
- no throw through terrain, buildings, party members, or protected neutrals;
- no throw when minimum range, fuse, blast radius, or escape-time safety fails;
- uncertainty may delay or cancel; it never snaps to an unseen future position;
- prediction is calculated by the network owner and the committed result is
  replicated, not independently rolled on every peer.

## Bomb capability: next shared infrastructure

Bombs are not a new top-level class by themselves. `Sapper` is a role profile that
uses the reusable thrown-projectile capability frequently; other classes may be
granted limited bombs.

Release gates require authorized combat, owned actor, available finite bomb,
valid native attack state, clear ballistic arc, safe ally radius, reachable escape
or adequate stand-off, and a predicted impact area that remains tactically useful.
Native item/attack execution owns the release, animation event, projectile,
inventory debit, fuse, area effect, and damage.

Acceptance scenarios:

1. stationary target at short and medium legal range;
2. target moving laterally at constant speed;
3. target approaching and departing;
4. target changes direction after release and may evade;
5. low wall blocks the arc;
6. ally crosses the impact area before commitment;
7. cluster enters a useful area and one bomb is committed;
8. target leaves encounter bounds and the throw is cancelled;
9. finite inventory reaches zero without refill or phantom projectile;
10. dedicated server and remote client see one identical release and impact.

## Plains spear hunter

Role record: `plains_spear_hunter`.

- Primary weapon: black-metal-tier spear or an actor-local equivalent whose
  visuals and damage are appropriate to the Plains.
- Reserve: finite throwing spears/javelins if the native spear does not return.
- Armor: mobile Plains-capable padded/black-metal presentation, not heavy tanking.
- Opening: predicted spear throw when lane, range, ammunition, and recovery are
  valid.
- Close work: native spear thrusts; knife fallback only after the separate lunge
  capability is accepted.
- Movement identity: fast reposition and dodge eligibility within party cohesion;
  no teleport, forced transform, or pursuit beyond the moving anchor.
- Target preference: immediate threats, exposed ranged enemies, and targets whose
  motion provides a safe throw—not species-based cheating.
- Counterplay: throws can miss after a direction change; close pressure interrupts
  the throw; finite spears matter.

The first live trial pairs this actor with the Plains heavy fighter against a
bounded Fuling-equivalent wave. Success means readable complementary behavior,
not guaranteed victory.

## Heavy pressure fighter

Role record: `plains_heavy_reaver`.

- Candidate native sets: two-handed axe, atgeir, heavy club, or approved actor-
  local attack items after runtime animation and hit-event verification.
- Responsibility: occupy close threats, exploit stagger, break clustered pressure,
  and protect the spear hunter's release window.
- Safety: no swing is requested when an ally occupies the attack's unsafe arc;
  native attack and recovery timing remain authoritative.
- Reuse: presentation and tuning variants become the Black Forest stocky axe
  breaker and Mountain rime axe guardian.

Do not call an attack valid because an animation looks correct. Verify attack
events, damage window, movement, recovery, network replication, equipment grip,
and both body presentations.

## Knife lunge

The lunge is a committed attack, not ordinary locomotion. The capability predicts
whether the target will remain inside the native contact volume at the attack's
hit time. It checks reachable ground and an unobstructed path for the committed
step. Once native attack execution begins, the adapter does not move the actor or
retarget the animation.

Cancel before commitment when the target is already too close, beyond achievable
contact, behind an obstacle, outside authorized bounds, or when the endpoint is
unsafe. After commitment the target may dodge; a miss is legitimate.

## Magic capability and presentation

Magic starts as a capability audit, not an unrestricted class. Candidate sources
are native player staves, validated creature attacks, approved project assets, and
actor-local status/projectile definitions. Every candidate must identify:

- resource type and finite/regenerating reserve;
- target or area rules, range, cooldown, and interruption;
- existing native animation/attack state and its animation events;
- hand, finger, weapon, or ground VFX anchor;
- projectile/area/status owner and friendly-fire policy;
- network ownership and replicated start/stop state;
- what happens if VFX exists but the gameplay cast is cancelled.

Presentation tiers:

1. **Staff-native:** use a validated staff attack and native hand/weapon lifecycle.
2. **Hand-origin:** lease one or both hand attachment points, play a verified cast
   clip, spawn a networked effect only from the authoritative cast event, and
   release the lease on interrupt.
3. **Gesture support:** use a safe native gesture only when it has no death/hit or
   unrelated gameplay event; layer VFX without pretending the emote causes damage.

The current metadata proves the human rig has left/right hand attachment bones and
safe social triggers such as point, challenge, roar, kneel, and others. It does
not prove any of them is a combat cast. `KneelDeath` is forbidden because it owns
a `Die` event. New cast use requires runtime graph, event, cancellation, held-item,
root-motion, and replication verification.

Initial magic profiles should be narrow:

- `rune_warder`: timed party ward or resistance area;
- `battle_cleric`: existing bounded heal with improved presentation;
- `mist_caster`: one validated projectile or control effect;
- `elemental_sapper`: thrown or placed area effect using the same safety service.

## Raid animation sequence

Ambient emotes are cancellable leases. Raid alarm immediately requests their
release; no new ambient pose starts while alarm/readiness/combat owns the actor.
Use native transitions for standing, drawing, attacks, stagger, and death. Custom
presentation may add a short alarm gesture or class-ready cue only after proving
it cannot delay combat or leave the body/hands stuck.

Required telemetry timestamps: raid sensed, ambient lease released, equipment
ready, target authorized, first native attack, special requested, special
committed/cancelled, combat clear, regroup complete, ambient eligible.

## Implementation sequence and gates

1. Accept the traversal/settled-player collision fix without combat regression.
2. Add pure prediction policy tests and recorded ballistic cases.
3. Wire one finite native bomb through safe prediction and native execution.
4. Prove multiplayer single-release and inventory debit.
5. Generalize to thrown spear and prepare `plains_spear_hunter`.
6. Validate one heavy native attack set and prepare `plains_heavy_reaver`.
7. Run paired Plains wave, camp raid, regroup, and persistence acceptance.
8. Add knife lunge policy and runtime proof.
9. Audit staff/hand magic candidates; ship only the first fully verified profile.

Every stage preserves the current voices, shouts, stagger, death, formation,
moving-anchor defense, doors, torch, cleric, persistence, and native combat proof.
Offline policy tests are evidence, not substitutes for live Valheim acceptance.

