# Tinkerbell elemental - shared owner direction, November 8

Brian explicitly requested this handoff for Claude and the active fire/audio
work. It records engineering direction, not approval of a finished release.

Canonical full contract: repository scrufboy/lwc-valheim-workbench,
published on main (contract commit deb48793700c1f1f82c55aca50f269bc16da0625),
`docs/project/TINKERBELL-ELEMENTAL-CONTRACT-2026-11-08.md`.
The local checkout is the sibling `LWCModelSkinStudio` directory; local work/valheim-workbench commit ba9b608 contains the same contract. Ordinary Git pushes are DNS-blocked; the connector published this contract/handoff only, not the complete source history.

The Surtling replacement is now a fire elemental in character and presentation,
with its core as the internal heart/power source. **Ground-oriented hover is the
default.** Keep ground-combat decisions and native attack opportunities, with
independent facing and travel, backward/side/diagonal leaning and synchronized
authored turns. No constant scripted circle, scheduled climb loop, high Drake
flight or insect dive-bomb. Temporary elevation is only two to three human
heights, approximately4-6m, chosen for actual tactical/environmental reasons.

Poison/frost/native Wet must support a deliberate counterplay cycle:
quench -> controlled descent -> actual acquired Land at a real support surface
-> grounded vulnerable Stunned with steam -> arms-out SpecialAttack1 reignition
with a small telegraphed fire burst -> changed attack phase. Actual death still
uses the final large explosion. Cancel stale attacks, preserve native damage,
never revive zero HP, and do not heal repeatedly or stun-lock on every poison tick.
Native Surtling poison immunity must be explicitly changed on the replacement
for accepted poison bombs to drive this behavior; frost is already weak.

Surtling Branch Recovery owns creature traversal/combat/quench/body/trail/death
effects. Real Smoke and Fire owns world-fire adapters; current source is
LivingWorldRealSmokeFire and its Workbench checkpoint. Keep these owners separate
and avoid duplicate attached effect/sound writers. Audio work should provide
auditioned smolder/crackle, steam/hiss, recovery laugh and elemental cast/burst
candidates with exact archive/clip names and mix notes. New acquisitions are
authoritative in Unity My Assets; local absence does not prove non-ownership.

Candidate19 remains installed in **LWC Surtling Flight Test** with Toolkit3.0.3
and Server Devcommands1.116. A separate QA encounter recorded120HP,135 motion
samples,0.50-6.30m terrain height,36 casts,four flames,death/removal and no logged
exceptions. Brian did not see this encounter; it is not owner acceptance.
Candidate20 is under development in
`exchange/surtling-elemental/2026-11-07/recovery06` in the Workbench checkout.
Preserve19. Do not start competing source edits/builders/install transactions.

Brian now requests controlled Arena/raid scenarios with the acquired creature
roster and poison/frost interactions. Coordinate baseline backup, exact installed
versions, factions and telemetry ownership before touching that live profile.
The Surtling source owner will supply an installable candidate and status.

Cross-chat delivery through the app could not enumerate chats in this pass;
this file is the explicit shared handoff. Read it and the canonical contract
before continuing. A local commit is not proof of successful remote push.

