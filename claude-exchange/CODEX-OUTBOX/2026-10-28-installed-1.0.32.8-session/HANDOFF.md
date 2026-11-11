# Installed 1.0.32.8 video review: pushing remains, gait is later polish

Date: 2026-10-28. This is a new dated submission for draft PR #4. It does not
replace the earlier 1.0.32.7 voice-test correction or claim a new release.

## Owner's live observations

- LIVE-PROVEN (owner report): Brian saw **some pushing** in the installed
  1.0.32.8 session. The no-push acceptance criterion remains unmet. Do not
  call this fixed because the contact counter looks small.
- LIVE-PROVEN (owner report): Traversal through structures looked good.
- LIVE-PROVEN (owner report): During deliberate slow walking, companions have
  stop-and-surge jitter rather than his smooth gait. He considers this polish
  for later, though he wants a gait/pace module available.
- SOURCE-INSPECTED: He did not observe door interaction, independent
  chair/bench selection, or workstation choices. These were not promised by
  this combat and spacing build.

## Exact session and recording

- INSTALLED: The active `LWC Human Lab v1` profile loaded `Living World C.O.R.E.
  NPC 1.0.32.8`. Installed `LivingWorld.HumanNpc.dll` SHA-256:
  `7CC4647364281C878A2C74CDCB209268A7DA53B0B4C5822BE0B311B28EC8D039`.
  This is an installed hash, not yet a verified package/source match.
- LIVE-PROVEN: OBS source folder is **`Y:\OBS OUTPUT\OBS OUTPUT`**.
  `2026-10-28 12-55-49.mp4` runs 476.309 s, SHA-256
  `B37B2B2A0BC220BEE6D3E1C34C1AF1EB5518DAE721CEA0EA9EF7513AFCB4B10C`.
  `2026-10-28 13-03-46.mp4` runs 175.017 s, SHA-256
  `F0FF7F817DFFE0CE21127E38B4D04BF95A4FB8BD8F9D0C61FB97C9481232FFF0`.
  Both source videos remain at that local path; they are not embedded in Git.
- LIVE-PROVEN: The redacted log and raw telemetry in this submission preserve
  the 12:52:48–13:06:41 game session. The source log remains in the active
  BepInEx profile and in a local ignored snapshot. The Git copy redacts the
  player's Steam and PlayFab identifiers.

## What the evidence shows

- LIVE-PROVEN: The spacing log records 10 leader-contact episodes: Svanhild
  6 (4.2 seconds total), Bothild 4 (2.2 seconds total). The final summaries
  are near `t=820.23`. The contact tracker counts only distance below 1 m,
  so it cannot measure every felt shove or approach.
- LIVE-PROVEN: In the first video, loaded companions approach the player in
  forest/structure travel and bunch near him on the paved area. The second
  video shows them spread for `The forest is moving`, fight, and regroup.
  The video plus owner's direct report keeps pushing open; frames alone do not
  quantify the force on the player.
- LIVE-PROVEN: Telemetry records 38 companion voice events, including 2 raid
  warcries; 49 strikes all recorded as killing blows, 46 at or after the raid
  warcry. These are combat/balance observations, not a claim that every kill
  came from a raid-spawned enemy. No NPC death is recorded in this session.
- LIVE-PROVEN: In telemetry `t=460–480`, the player's horizontal movement had
  0 near-stop samples out of 39 at a 1.51 m/s mean. Svanhild had 2 near-stop
  samples and a 3.84 m/s peak; Bothild had 4 and the same peak. This is
  consistent with the owner's observed slow-walk stop-and-surge gait.

## Source diagnosis and work boundary

- SOURCE-INSPECTED: In maintained 1.0.x source commit
  `33addad57523d5d95df7eb2572403e4f76d1ea36`,
  `PartySpacing.Pure.cs` uses `ContactLeader = 1.0f` as normal backoff
  entry, while desired leader clearance is 1.6 m. `TraversalBehavior`
  changes only the native Follow anchor, and runs in Calm/Readiness/Regroup.
  No pairwise player/companion collision suppression appears in the source.
  These are plausible reasons steering does not guarantee no physical push;
  the exact impulse path remains UNKNOWN until native physics is inspected.
- SOURCE-INSPECTED: `NpcBody.SetPace` uses
  `Mathf.Max(baseSpeed, leaderSpeed + .15f)` when not running. That floor
  prevents matching a deliberate walk slower than `baseSpeed`. Native
  Follow's 3 m stop radius may also contribute to stop-and-surge. Keep gait
  work after the no-push repair, preserving run, catch-up, sneak, and combat.
- SOURCE-INSPECTED: The maintained README explicitly lists doors and
  workstation/crafting poses outside the initial core. The current
  `SitWithLeaderBehavior` mirrors the leader's sit; it does not reserve or
  choose a chair or bench. Design these as separate world-interaction/routine
  behaviors with owned claims and combat preemption, not combat attack code.

## Claude's next action

1. Reproduce the owner's no-push observation with the two exact OBS clips and
   the same installed build. Inspect the native player/NPC collider pair and
   movement phase, especially narrow travel and the paved/house scenes.
2. Prepare a narrow 1.0.x repair that prevents physical displacement of the
   leader while preserving companion/enemy collisions and the existing
   spacing presentation. A binding-scoped collision-pair rule is a candidate,
   not an approved fix; verify teardown on unload, death, rebinding, and each
   network peer before use. Do not merely widen or hide the contact counter.
3. Gate the candidate with an idle-player displacement test, the observed
   route, structure bottlenecks, combat, save/reload, and multiplayer/dedicated
   authority. Keep voice and combat baselines locked and record the installed
   package hash if a new candidate is promoted.
4. Queue a distinct gait policy that can reduce non-run movement speed during
   intentional slow walking when native animation supports it; compare
   continuous leader speed against companion stop/burst samples. Add doors,
   seat choice, and workstation choice as later living-routine modules.

FAILED: The installed 1.0.32.8 no-push acceptance criterion. UNKNOWN: the
exact native collider cause, perceived voice fit, and any new candidate's
in-game behavior. No mod DLL was changed by this review.
