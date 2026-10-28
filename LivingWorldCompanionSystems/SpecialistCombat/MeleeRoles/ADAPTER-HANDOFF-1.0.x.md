# Melee role adapter handoff — Valheim 1.0.x

This folder is engine-free policy. It emits a request or refusal; it never moves,
faces, animates, retargets, applies hits, or debits resources. `MonsterAI` remains
the exclusive combat owner. `HoldLine` and `CloseRange` are preferences for that
owner, not commands and not a parallel attack controller.

## Encounter and identity boundary

These capabilities do not decide whether a person is friendly, hostile,
recruitable, temporarily travelling under an accepted aid agreement, or a party
member. The server-owned relationship/encounter layer supplies
`TargetAuthorized`; uncertain first sight never does. A single-claim authored
hostile event still enters through ordinary native combat authorization.

Attach the role and loadout to the stable `PersonId` equipment record. Aid,
travel, event resolution, later party invitation, release, and gear progression
must retain that same person and update their record; none of those transitions
spawns a stronger replacement or silently changes allegiance. The Black Forest
and Plains profiles describe capabilities and credible loadout families only.
They do not imply recruitment, encounter ownership, or faction policy.

## Adapter inputs

- Supply target motion from two recent authoritative observations, never future or
  remote-peer knowledge. Replicate the committed result; do not reroll per peer.
- Build `SpaceEvidence` from precommit navigation, cast and overlap evidence for
  the proposed native step/contact volume or heavy attack arc. Treat missing facts
  as unsafe. Include party members and protected neutrals.
- Expose a `NativeAttackCandidate` only for the actor's actually equipped item and
  current animation set. `LiveVerified` is a release gate, not configuration.
- Once a request is committed, native execution owns windup, root/step motion,
  contact, damage and recovery. The adapter must not teleport, steer, or retarget.

## Exact live proof required on the maintained 1.0.x build

Record build/mod versions, prefab and item IDs, actor body presentation, owner
peer, and a synchronized owner/client capture for every candidate attack.

1. **Animation events:** exact state/trigger and every event in order; identify
   the authoritative attack/contact event and prove cancel-before-event versus
   recovery-after-event. Do not infer an event from a visually convincing clip.
2. **Grip and equipment lifecycle:** right/left/two-hand grip, hand attachments,
   draw/stow and back attachment, shield conflict, weapon visibility, and both
   required body presentations through idle, windup, contact, recovery, stagger,
   death, unload and reload.
3. **Hit and damage ownership:** native contact volume/arc, minimum and maximum
   range, root/step displacement, target filtering, friendly/neutral behavior,
   one damage application, stagger/knockback, durability/stamina/resource debit,
   and proof that policy code never spawns or applies a hit.
4. **Recovery:** measured windup, active/contact window, recovery lock, interrupt
   behavior, miss behavior, weapon-switch behavior, and proof no second request is
   accepted while native recovery owns the actor.
5. **Network replication:** dedicated server plus owner and remote client observe
   one identical target lock, attack start, native displacement, contact/miss,
   damage/debit and recovery. Test latency, target dodge after commitment, owner
   transfer, disconnect during windup, unload/reload, and confirm no duplicate
   attack, peer-local prediction, snap, teleport, or postcommit retarget.
6. **Safety scenarios:** ally and protected neutral in step lane/contact volume;
   ally entering each atgeir/thrust/sweep/spin arc before commitment; obstacle,
   ledge, unreachable endpoint and tactical-bound edge. Every unsafe case must
   refuse before native commitment, while a target dodging afterward may cause a
   legitimate miss.

Knife candidates additionally require measured native windup, supplied step
distance and contact radius. Atgeir/heavy candidates are independently approved
per thrust, sweep, and spin; proof for one attack never enables the others.

Offline tests are policy evidence only. Until this checklist passes, role profiles
remain loadout/canon data and the adapter must keep `LiveVerified=false`.
