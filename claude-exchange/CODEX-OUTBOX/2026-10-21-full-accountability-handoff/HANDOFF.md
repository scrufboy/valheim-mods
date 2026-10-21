# Living World C.O.R.E. — current accountability handoff to Claude

Date: 2026-10-21

## Read this before changing code

This document supersedes the 2026-10-17 handoff as a statement of the current
machine state. The older handoff remains evidence of the earlier regression
history, but its 0.3.31 candidate is no longer current.

The user's requirement has been repeatedly converted into narrower or opposite
implementations. Working companion behavior has repeatedly been disturbed while
unrelated fixes were attempted. The immediate request is not another optimistic
rewrite. It is an independent accountability audit, followed by a recovery that
preserves every accepted behavior and proves the result live.

## Current repository state

- Repository: `https://github.com/scrufboy/valheim-mods.git`
- Branch: `handoff-rimebound-emberworn`
- Current implementation commit: `b654162` — `Repair native facial hair commit before party readiness`
- Local checkout:
  `C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\github-sync-forest-children`
- Human NPC source:
  `LivingWorldHumanNPC`
- Governing state contract:
  `LivingWorldHumanNPC/BEHAVIOR-STATE-CONTRACT.md`
- Governing party-creation contract:
  `LivingWorldHumanNPC/PARTY-SPAWN-CONTRACT.md`

## Current installed state

- Profile:
  `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1`
- Installed plugin version: `0.3.66`
- Installed DLL SHA-256:
  `5FD74D100BC6BDB22D0761D52955DC10BD944995A94152DA43A1DD1ECA8B01D2`
- Package archive:
  `LivingWorldHumanNPC/LivingWorld-HumanNPC-0.3.66-NATIVE-HAIR-COMMIT-LAB.zip`
- Package SHA-256:
  `23954E6DF31D0F015FBDF38766EAB9B2265B4CBCDF429B58E4FEBA9331DAFD75`
- Status: **installed, offline-tested, not launched, not live-proven**.

The failed 0.3.64 installed binary was preserved at:

`C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1\BepInEx\LivingWorldBackups\HumanNpc\20261020-failed-0.3.64-single-bald-party`

Failed DLL SHA-256:

`73E5B6FECA1C79D0EE6A39CF7BDC6D7D077E54FB806ABD7C4346D2E601749749`

## Immediate live failure that produced 0.3.65

0.3.64 loaded but did not produce the promised party. The log recorded one
actor, no support actor, no follow, and:

`FAIL human initialization: 10-second initialization deadline: native hair hash 0 != 1043840427 for Hair9`

The actor was visibly bald. `lwparty` did not produce two complete companions.
This was a live failure, not a theoretical concern.

The identified cause was that the cloned Player `VisEquipment` graph retained
private request caches while the new NPC ZDO did not contain the equivalent
selection. Valheim's setter no-op contract then skipped writing the matching
selection, so the visible attachment was never built.

## 2026-10-21 startup evidence after installing 0.3.65

0.3.65 loaded successfully, but the world still contained the single failed
Hair9 actor left by the preceding run. Before a clean new two-person `lwparty`
transaction was logged, that saved actor again reported:

`FAIL human initialization: 10-second initialization deadline: native hair hash 0 != 1043840427 for Hair9`

The user had to use `killall` to remove the survivor. This is not evidence that
a clean 0.3.65 two-person creation failed, because no new `lwparty` spawn was
present in the log before this event. It is live evidence that 0.3.65 did not
automatically repair the saved broken actor. Recovery of a prior failed actor
must therefore be classified **failed**, not unknown or successful.

## Clean 0.3.65 two-person failure and 0.3.66 response

After `killall` removed the saved Hair9 actor, the user built new test structures
and ran the single `lwparty` command. The command created two fresh named actors.
The video shows both bald and inert. The log records two independent failures:

`native hair hash 0 != -74603963 for Hair25`

`native hair hash 0 != 1043840417 for Hair7`

The joint party transaction then refused readiness and follow. Telemetry emitted
no NPC behavior stream because neither actor passed appearance initialization.
This cleanly rejects 0.3.65.

The false offline proof was identified in `IdentityAdapterHarness`: its fake
`SetHairItem`/`SetBeardItem` methods immediately changed the current render hash
and created attachment objects. Valheim 1.0.20 does not do that. The real setters
update requested/ZDO state; `UpdateVisuals` later reads the ZDO and builds the
attachments. Version 0.3.66 now commits and reads back Valheim's native
`s_hairItem`/`s_beardItem` ZDO records before the native visual pass, and the
managed double models that real sequence. The complete offline suite passed and
0.3.66 is installed, but it is not yet live-proven.

The failed 0.3.65 DLL is preserved at:

`C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1\BepInEx\LivingWorldBackups\HumanNpc\20261021-failed-0.3.65-two-bald-inert-party`

## What 0.3.65 changed

0.3.65:

1. invalidates exactly the five private native appearance-request caches before
   the first fresh native setter sequence;
2. requires visible selected head hair and any selected male beard before
   appearance readiness;
3. creates both missing base humans during one `lwparty` command pass;
4. lets their appearance adapters progress concurrently;
5. attaches the existing companion behavior suite only after each native human
   passes appearance readiness;
6. enables native `MonsterAI` after behavior attachment;
7. reports party success only after both actors initialize orders and follow.

Offline checks passed. A managed transaction harness verified that one
`lwparty` pass allocates exactly two actors, begins two independent profile
transactions, refuses partial success, does not duplicate either actor on the
internal retry, and follows both only at the joint ready boundary.

An IL comparison found no changed existing method bodies between 0.3.64 and
0.3.65 in these protected implementation types:

- `HumanCompanion`
- `HumanDoorTraversal`
- `HumanFollowPresence`
- `HumanCompanionPosture`
- `HumanEnvironmentStatus`
- `HumanPartyConfidence`
- `HumanCompanionAmbient`
- `HumanEquipmentReadiness`
- `HumanCombatDefense`
- `HumanPartyEquipment`
- `HumanCleric`
- `HumanUtilityTorch`
- `ManualPartyLab`
- `HumanNpcSettings`

That is structural/offline evidence only. Moving the attachment boundary can
still change Unity lifecycle timing even when the module method bodies are
unchanged.

## Accountability and unresolved risks

| Boundary | Evidence | Current judgment |
|---|---|---|
| One-command complete party | clean 0.3.65 live run | Failed: two fresh actors, neither ready/following |
| Visible hair/beard | clean 0.3.65 live run and video | Failed: both fresh actors bald |
| 0.3.65 cache repair | clean 0.3.65 live run | Failed for Hair25 and Hair7 |
| 0.3.66 native ZDO commit repair | source + corrected managed harness + native IL checks | Offline-tested and installed; live proof pending |
| 0.3.65 two-person allocation | clean live log/video | Passed allocation only; readiness/follow failed |
| Combat/follow/torch/stow/wet/door modules | unchanged-source boundary + full suite | Preserved structurally, not live-proven in 0.3.66 |
| Runtime `AddComponent` staging | no Unity run | Unknown; lifecycle ordering must be observed |
| Existing failed 0.3.64 actor recovery | 0.3.65 startup log on 2026-10-21 | Failed: saved Hair9 actor repeated the same initialization deadline and required `killall` |
| Remote/multiplayer ownership | source concern | Unknown and potentially incomplete: remote appearance readiness and behavior installation need independent review |
| Failure rollback of the full two-actor transaction | managed tests cover no duplication and partial refusal | Review whether all second-actor allocation failures should roll back both actors atomically |
| Xbox/crossplay | no real Xbox client proof | Unknown; do not claim compatibility |

The repeated process failure was patch-forward development without maintaining
one continuously enforced live champion. Offline suites prevented many direct
method regressions, but they did not prevent Unity lifecycle, presentation, or
transaction failures. Versions 0.3.59 through 0.3.65 repeatedly changed the
appearance boundary without producing a live-proven complete party. This must
not be minimized.

## Protected requirements Claude must retain

- One `lwparty` command creates or recovers the complete party. Internal retries
  are never user commands.
- Both people are genuine random humans: male or female, native skin/hair color,
  concrete head hair, and beard only when a male profile selects one.
- Combat is exclusive native `MonsterAI` combat with Living World authorization
  and bounds. Follow, formation, gaze, ambient work, posture, torches, and
  peaceful door behavior cannot compete with it.
- Formidable means capable and believable, not invincible or omniscient.
- Visible weapon stowing, bow execution/facing, wetness, torches, doors,
  formation, guard-sector coverage, workstation use, and command discovery
  remain protected behavior.
- A compile, unit test, selection, intent, or log message is not live proof.
- No new feature may be used as justification to remove an accepted one.

## Required independent review before another patch

1. Read every development note from 0.3.42 through 0.3.66 and compare its claim
   to the associated code, tests, live log, telemetry, and video.
2. Inspect Git history and retained binaries rather than trusting version prose.
3. Reconstruct the complete 0.3.59–0.3.66 appearance/spawn causal chain.
4. Review 0.3.66's runtime staging for Unity lifecycle and network ownership,
   including remote clients.
5. Do not change gameplay code until the current 0.3.66 live result is captured,
   unless static review proves it unsafe to launch.
6. If 0.3.66 is safe to launch, the first gate is only `lwparty`: two visible,
   complete humans and two following companions. Stop on failure.

## Evidence locations

See `ACCESS-REQUIREMENTS.md` in this folder. The local files are not magically
available to Claude merely because they are named here. They must be mounted,
uploaded, or otherwise shared explicitly.
