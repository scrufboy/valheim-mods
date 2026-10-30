# A1 review: make the implementation C.O.R.E.-shaped and measure initiative

Claude built A1 and the actor-intent follow-up. The read-only boundary, bounded storage, fair perception,
knowledge uncertainty and offline gates are good. Two corrections are required before installation/live A1.

## 1. The implementation is not yet genuinely keyed by side

`PartyAwareness` says it is keyed by side, but the runtime key and membership model remain companion-specific:

- static dictionary key is `LeaderId`;
- construction requires `NpcLeaderBinding`;
- observers come only from `NpcKernel.All` with the same leader;
- `Step` takes a `Player leader`;
- centroid/member targeting has a special leader slot;
- the only tick source is a companion kernel.

Comments promising future camps do not make that core reusable. Before A2, introduce an actual tactical-side
boundary. It may retain the companion party as the first adapter, but the awareness core must accept:

- stable `SideKey` independent of a player id;
- bounded member/observer facts supplied by a side adapter;
- relationship/hostility classification supplied by the side/faction context;
- member positions and eligible observers without a required `Player`;
- the same contact/intent/group policy for companion parties, friendly camps and hostile camps.

The companion adapter may include Brian's body as a protected member/centroid participant while excluding him
as a sensor. A camp adapter can be wired later, but it must not require rewriting awareness policy.

Also document the deliberate A1 limitation if players are excluded as contacts; do not make `c.IsPlayer()` a
permanent core rule because hostile player relationships are a side/faction question.

## 2. A1 must measure the full initiative path

Brian identified the governing issue: our smarter checks must not cost initiative. A slightly earlier fair
perception envelope may compensate for irreducible interpretation latency, but only by the measured amount.

Add a bounded read-only initiative trace for each relevant contact/group:

- first inside candidate/render-relevant envelope;
- first fair evidence (sight, sound or contact);
- Noticed;
- Alerted;
- Committed;
- first native target acquisition by a side member;
- first response-ready proposal (shadow only; no control);
- first native attack/shot/throw initiation where observable;
- distance and time-to-contact at every milestone;
- deltas between milestones and total evidence-to-action latency.

Independent checks should update concurrently. Telemetry must reveal duplicated/serial delay rather than
assuming all latency should be compensated by range. The calibration rule is:

`additional perception lead = closing speed × irreducible added decision latency`

First remove redundant latency; then test the smallest range/sound lead that restores or improves the old/native
action timing while keeping C.O.R.E.'s rules of engagement.

## Scope

Keep A1 read-only, disabled by default and gameplay-neutral. Do not wire A2 control yet. Re-run every existing
gate and present the revised side boundary, initiative fields, sample trace, and exact live/video acceptance
criteria. Push the corrected branch and update the presentation.
