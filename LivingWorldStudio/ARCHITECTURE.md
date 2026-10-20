# Living World Studio — architecture and feasibility gate
Date: 2026-10-20. Owner: Brian / scrufboy.
Status: SOURCE-INSPECTED design; NOT IMPLEMENTED, NOT INSTALLED, NOT LIVE-PROVEN.
Baseline: scrufboy/valheim-mods main 91f80f29b7b86a78a61d1f364df539271ae733f2.

## Identity and isolation
Working utility name: **Living World Studio**. Directory LivingWorldStudio;
assembly LivingWorld.Studio.dll; proposed plugin ID
com.scrufboy.livingworldcore.studio; package scrufboy-LivingWorld_Studio.
These are working identifiers, not a finalized marketing identity.
Entrance display proposal: **Portrait Hall Door** (plain timber construction).
No assertion that this room exists in canon or grants a character new origins.
The FORMIDABLE lore branch was read at 110a3d1; its independent technical,
narrative and utility boundaries are preserved. This checked-in handoff coordinates
naming with story work; no message was sent to another chat and no approval is claimed.

One independently removable BepInEx/Jötunn utility. No Tooler, CameraMode,
RRRCore or companion-combat assembly dependency. Install on server and every
participating client with protocol/version agreement. No automatic install script.
No patches or edits to companion combat, faction targeting or normal orders.
A missing approved subject adapter disables real-companion selection; it must not
silently fall back to all nearby characters, followers or tamed creatures.

## Inspected evidence and limits
Local existing checkout was clean on design/resident-woodcutter-foundation at
7a810a3. A separate worktree starts from main 91f80f2 so resident work is preserved.
No AGENTS.md was listed in the inspected repository tree.
Read repository layout, project boundaries, exchange protocol/current handoff,
HumanCompanion.cs, AmbientAnimationContract.cs, ManualLabBuilder.cs,
ManualTestPlugin.cs and HUMAN-ANIMATION-HANDOFF.json.
HumanCompanion exposes temporary follow and EndTemporaryOrder, plus combat state,
but no public durable photo suspension/restore transaction.
Ambient posture has internal activity leases; these are not a public studio API.
Stopping combat, issuing hold or disabling MonsterAI would not establish exclusive
ownership over ambient, follow, equipment, traversal, recovery and networking.
The handoff's installed 0.3.34 record is historical, not current machine proof.

Game/Jötunn reference assemblies exist in a prior workspace (Jötunn 2.30.2).
They have not been signature-audited or proved to match Brian's current installation.
No game process, current installed DLLs or live game session is accessible here.
Current build target must be established from the actual target installation,
rather than assuming the old 1.0.17 or 1.0.20 references match it.

Primary API sources inspected:
- https://valheim-modding.github.io/Jotunn/tutorials/pieces.html
- https://valheim-modding.github.io/Jotunn/api/Jotunn.Entities.CustomPiece.html
- https://valheim-modding.github.io/Jotunn/data/pieces/piece-list.html
Jötunn documents cloning native pieces after OnVanillaPrefabsAvailable, registering
through PieceManager and unsubscribing the callback. The generated piece list
identifies itself as Valheim 1.0.12: it is candidate evidence, not current verification.

## Room and asset audit
First room: 32 m clear width/depth, 12 m clear height; 3 m rear cove envelope.
Reserve a 40 x 40 x 16 m allocation, including shell and safety vestibule.
Keep at least 8 m of camera runway in front of subject rows.
Initial budget: 16 subject pads; four rows of four at 2 m lateral / 2.5 m row spacing.
Reject overflow; provide compact/wide presets within the same bounded pad count.

| Resource | Evidence | Plan and remaining gate |
| --- | --- | --- |
| wood_door | Used by existing ManualLabBuilder | Runtime clone into unique build piece; replace native door interaction only on clone; retain lore-compatible timber appearance |
| wood_floor / woodwall | Used by existing lab builder | Copy only mesh/render data into studio-owned static geometry; no native wear, loot, support or destruction scripts |
| wood_roof_45 / wood_wall_roof_45 | Existing lab construction | Optional shell candidates; not proof of seamless photographic surfaces |
| Native wood material/shader | Runtime resource candidate | Private Material copies; inspect supported properties and fog/shadow behavior on actual target |
| Continuous cove | No verified native curved wood mesh | Prototype deforming a private readable clone of wood-derived mesh; prove normals, topology and smooth joins; do not promise a seam-free result yet |
| Native animation graph | Metadata handoff and ambient validator | Inspect per-rig states, parameters, clip events and props; names alone are insufficient |
| Dungeon/environment isolation | No verified API integration in repo | Compile and live-prove actual target APIs before enabling transfer |

Never distribute game meshes, textures, shaders, animation clips, controllers or
reference DLLs. Runtime resource availability is technical evidence, not a legal
opinion; preserve game/mod terms and original-source attribution.
Never instantiate a complete native building prefab and hope disabling one
component removes all its side effects. Whitelist visual and collider components.
Use shared native meshes read-only unless a private mesh copy is required.
Clone each material privately; destroy studio-owned materials/meshes at teardown.
Never alter sharedMaterials entries in place or write to vanilla shader globals.

Palette: green #00FF00, blue #0000FF, neutral #808080 as configurable sRGB targets.
These are source colors, not a promise of exact screenshot pixel values.
Remove texture/normal/emission noise through verified material properties.
Backdrop must not contribute green/blue indirect light or reflections to subjects.
Prefer independent flat/shaded backdrop and neutral subject-light layers.
A small controlled brightness gradient may show form, but no cast shadows on
chroma surfaces. Compare samples under all camera presets and target graphics modes.
Do not substitute arbitrary shader names or add bundled shaders without revisiting
the vanilla-only requirement. If no native shader isolates fog/lighting cleanly,
the chroma acceptance gate fails; hiding the failure behind extreme emission is forbidden.

Room is a reserved interior volume in the same Valheim world, not an actual
separate Unity scene or a claim of engine-level private instances.
Server allocates non-overlapping slots only in a surveyed safe interior region.
No hard-coded altitude or global map quadrant until dungeon/zone tests prove it safe.
Persist allocations with world UID; check loaded geometry and saved allocations.
A slot is private to one player session and its explicit roster; no casual walk-in.
Unauthorized entry triggers safe rejection/recovery, never deletion of the entrant.
Other mods/admin teleports can bypass boundaries; this is isolation for cooperative
play, not a security sandbox.

## Components and authority
- StudioPlugin: dependency/protocol check, registration and teardown.
- StudioEntrance: unique network ID, creator, access check, studio selection.
- StudioCoordinator: single server session/slot/roster authority.
- StudioJournal: durable write-ahead records and schema/version validation.
- StudioTransfer: owner-executed movement with server acknowledgements.
- StudioRoom: bounded geometry, pads, collisions and exit vestibule.
- StudioPresentation: local environment/camera-scoped lighting and reversible UI.
- StudioSubjectAdapter: optional approved subject authority integration.
- StudioPanel: controller-first views; sends intents, never authoritative transforms.

Do not add an adapter to companion production code as part of this utility stage.
The approved actor owner must eventually expose acquire/renew/release/recover
photo authority, including denial during encounters and restoration after restart.
No reflection into companion private state and no second movement/equipment writer.
A photo token expires safely; stale packets cannot reacquire it.
If only render proxies are supported later, label them explicitly and never claim
they are the actual selected companions.

## Durable transfer contract
Persist schema, world UID, session UUID, server identity, player character ID,
authenticated peer mapping, entrance ZDOID, slot, revision, phase, explicit roster,
palette and per-participant return/movement/pose acknowledgements.
For each participant store stable actor identity + ZDOID where applicable,
original world/position/full rotation, original authority and opaque restore token.
The player and each companion retain separate exact origins, not one group point.

State sequence:
Draft -> Reserved -> Prepared -> Entering -> Active -> Returning -> Restored -> Closed.
Any partial or ambiguous phase -> RecoveryPending. No implicit success.
Persist and flush the complete return journal before moving the first participant.
Mirror player return information into durable character-associated storage and
retain server/actor records so client/server world-save timing cannot erase it.
ZDO replication is not a disk-save guarantee; establish the durable store explicitly.
Use atomic replace plus previous valid record/checksum for journal writes.
Reject unknown schema, world mismatch, NaN/infinite coordinates and stale revisions.

Prepare checks: authenticated requester controls entrance and each subject;
no encounter/death/teleport conflict; all required peers support protocol;
all selected owners acknowledge a valid photo token; room built and probed;
origins and durable journal confirmed. Then transfer explicit subjects and player
with bounded deadlines, verified arrival and ownership acknowledgement.
Active is reached only after the full roster arrives. Partial arrival initiates
idempotent return of every moved participant; pending/unloaded actors stay journaled.
No client-supplied arbitrary destination or actor IDs without server authorization.
Token includes session, actor, world, revision and monotonically fenced ownership epoch.
Messages use authenticated RPC sender, not a player ID trusted from the payload.
Session quota: one active session per player, 16 subjects and one reserved slot.
Duplicate use/replayed RPC returns the existing transaction result.

Exit restores each participant's own recorded position AND rotation.
Preflight original landing for collision/terrain; exact return is conditional on
the location remaining safe. Never embed someone in a newly built wall to claim
exactness. If obstructed, retain exact origin, move only to a verified nearby safe
landing with explicit notice, and keep retry/recovery available. If no safe landing
exists, keep the safe room and journal; do not destroy the last refuge.
Deleting an entrance does not delete the journal or require that door to return.
Copied/duplicate entrances get independent ZDO IDs and cannot alias active sessions.

Reconnect/reload: reconcile journal and actual position/actor existence before
normal orders run; recover to origin by default. Server can recover offline
companions when their authoritative owner is available; offline players recover
on their next authenticated join. Host loss, server crash and ownership migration
all invalidate old epochs and enter reconciliation; never spawn replacement actors.
Return writes intent before movement, records verified arrival, releases photo
authority, confirms normal-owner acknowledgement, then marks restored.
Cleanup only after every participant is restored; retain unresolved session records.
Recovery command works without a surviving entrance. An emergency safe cell can
be reconstructed from a valid slot record before restoring normal AI.

## Posing and damage boundary
Real companion admission remains disabled until its existing owner grants exclusive
photo authority. Snapshot persistent order/home/role/inventory identity without
rewriting it; restore through that same owner. An encounter blocks admission.
A threat during preparation cancels transfer and leaves combat to its existing owner.
An intruder inside the studio aborts posing and initiates recovery; studio adds no
target choice, attack or defense decisions.
Photo hold is not combat hold and must not call StopCombat as an ownership shortcut.

Each admitted subject uses a bounded pad; face-camera takes the requesting player's
actual gameplay camera pose at command time. Face-target uses a selected approved
pad/marker. Rotate is incremental yaw; all requests go to the subject authority.
Freeze must preserve a sampled safe pose without globally pausing simulation or
changing timeScale; don't freeze gameplay Animator event execution blindly.
Release restores the owning actor's graph/equipment/movement authority.
Native weapon drawn/stowed is presentation through the approved owner; do not
grant items, consume arrows, fire projectiles or execute attack events.
Idle first; guard, bow-ready, kneel/yield, sit and cheer only after rig-specific
event/root-motion/attachment validation. Unsupported choices are disabled visibly.
Animation source names are candidates until verified, not invented callable API.

No new hostile spawns or living enemies in first runtime milestone.
Explicit future enemy subjects require a separate safe spawn/pose/cleanup adapter,
zero loot/death rewards, exact session tagging and bounded counts.
Never change faction globally, invulnerability globally, spawn rules globally,
or suppress damage for arbitrary characters outside the admitted roster/volume.
Prevent projectiles, damage, status, consumption, weather effects and ambient AI
for admitted photo actors through approved scoped facilities; missing facilities
fail admission. Floor/walls must not rely on ordinary wood stability or wear.
Test raids, water/fog, indoor status, fire/poison and area damage crossing boundaries.

## Controller and commands
Normal hammer build menu places the entrance. Ordinary mapped Interact opens panel.
No secret keyboard chord or hard-coded Xbox glyph is required.
Use mapped UI navigation/Submit/Cancel, visible focus and scrollable long lists.
Do not consume gameplay inputs when the panel is closed.
Pause camera/player look only while the panel owns focus; restore on cancel/exit/error.

| Panel | Controller action |
| --- | --- |
| Entrance | Choose Enter alone / Select subjects / Recovery / Close |
| Roster | Named identity, ownership, checkbox, unavailable reason; all unselected initially |
| Transfer | Confirm explicit roster and origin; Cancel before movement |
| Studio | Subject/Group, pad spacing, face camera/target, rotate, pose, freeze/release |
| Look | Green/Blue/Neutral, bounded neutral light intensity; reset |
| Screenshot | Hide panel and pad markers; reopen with normal interaction at console/exit |
| Exit | Return all; report pending subjects individually |

Equivalent diagnostic command proposal: lwstudio status, recover, exit, palette
green|blue|neutral, select <approved identity>, clear, pad <1..16>, face camera,
face target <pad>, rotate <-180..180>, pose <verified name>, freeze, release.
Commands use the same authorization and transaction path as controller intents.
No arbitrary spawn, teleport coordinates, animator hashes or component toggles.
Normal first/third-person view is mandatory acceptance. Optional camera mods may
move only the local camera; no camera-mod dependency and no compatibility claim
until tested. Existing CameraMode remains untouched.

## Risk and implementation sequence
| Risk | Severity | Gate |
| --- | --- | --- |
| Missing durable actor photo authority | Critical | No real companion movement/posing until approved owner contract exists |
| Return data lost across save order/crash | Critical | Journal fault injection before/after every write, move and acknowledgement |
| Slot intersects native dungeon/world | High | Actual target zone/interior survey and multiplayer allocation test |
| Material fog, seams, texture grain or green spill | High | Render test on target graphics modes; no marketing claim before evidence |
| Native animation attack events/root motion | High | Per-rig whitelist and zero side-effect test |
| Unauthorized/replayed ownership RPC | High | Sender/epoch/revision tests with two clients and ownership change |
| Removal with occupants | Critical | Drain/recover first; never claim DLL deletion alone safely evacuates a room |
| Build references differ from installed game | High | Pin versions/hashes and compile against actual target |

1. Commit this design and naming handoff independently. No production AI edits.
2. Signature-audit actual game/Jötunn assemblies; record versions and hashes.
3. Implement geometry/material probe with no live transfers; prove clean chroma,
   collider shell, cove feasibility and scoped light/environment restoration.
4. Implement journal/coordinator and fault-injection tests independent of Unity.
5. Implement player-only entrance/exit and recovery on disposable world; do not
   package it as complete companion support. Prove two-client/server behavior.
6. Obtain approved owner-level subject contract in a separate controlled integration
   stage; then implement utility-side adapter without combat decisions.
7. Validate poses individually, then roster/group/controller operation.
8. Build separate installable candidate, manifest, checksums, license, changelog
   and install/remove instructions. Package only original DLL/docs/metadata.
9. With game/server stopped, install only studio DLL in disposable profile; no
   automated replacement of other DLLs. Live acceptance precedes release.

Required tests: two simultaneous sessions; duplicate door use; foreign owner;
stale/replayed RPC; unselected companion stays; encounter refusal; disconnect at
each phase; crash at each journal boundary; reload with player/subjects inside;
entrance removed; invalid/missing slot; full roster partial transfer; subject
unloaded/dead; authority migration; obstructed origin; cancelled pose; plugin removal
after drain; 30-minute stationary screenshot session; no drops/damage/wandering;
normal camera; controller without keyboard; baseline companion behavior after exit.
No release until all requested behaviors have evidence or explicit scope labels.

## Current decision
Design is concrete enough for a geometry probe, but a complete live-transfer mod
is not safe to implement from the currently exposed companion APIs.
No installable DLL is produced or claimed by this design submission.
The next safe implementation is the isolated rendering probe plus journal tests;
real companion integration remains gated on authoritative restoration support.
