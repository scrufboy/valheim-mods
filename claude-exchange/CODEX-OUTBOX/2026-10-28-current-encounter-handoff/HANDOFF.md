# Current Claude handoff: living encounters, class pairs, and combat support

Date: 2026-10-28. Branch: `work/companion-encounter-world`. Draft PR:
https://github.com/scrufboy/valheim-mods/pull/4. Base:
`work/norse-human-names`. This submission is additive under the exchange
protocol; prior dated submissions remain intact.

## Owner intent and fixed rules

- DESIGN-AUTHORITY: The owner goal is to make the best NPCs Valheim can have:
  one modular, cohesive living world of capable friendly and hostile people,
  biome-shaped identity and equipment, authored adventures, working routines,
  earned affinity, and telemetry-refined combat. Read
  `LivingWorldCORE/PROJECT-BOUNDARIES.md` under **Owner goal** before treating
  any later correction as a change to project scope.
- DESIGN-AUTHORITY: A narrow correction changes its stated subject only. Keep
  the existing research, implementation direction, and accepted contracts
  unless the owner explicitly changes them. Clarify an ambiguous reversal
  before undoing work.
- SOURCE-INSPECTED: Brian wants people encountered in a living world, with their
  own home, kit, knowledge, needs, consent, and history. A person may ask for
  food, help against a nearby threat, or help reclaiming a village. The player
  may agree and perform witnessed aid; the person may reciprocate and travel
  for a specific purpose. Shared action may deepen affinity. A later, separate
  invitation and answer establish party membership; friendship and family-like
  bonds continue to grow through lived history.
- SOURCE-INSPECTED: The established roster canon caps active field positions at
  two, including temporary help. One useful companion is preferred early; a
  third ordinary fighter is not a default. A full field party may know or bond
  with more people without marching as an army.
- SOURCE-INSPECTED: Each accepted aid request, site event claim, temporary
  journey, safe return, witnessed affinity change, later invitation, and final
  assignment is a separate server-owned fact. Zone unload cannot reset or pay
  the request.
- SOURCE-INSPECTED: A long red-haired Plains spear woman with biome-credible
  armor is a candidate blueprint, not an automatic recruit. Her Norse name and
  parent lineage must be selected/validated through the naming catalog. Her
  owned spear, fallback weapon, finite throw/recovery lifecycle, voice, local
  knowledge, and relationship need to describe the same persistent person.

## Code now in this PR

- CODE-TESTED: `EncounterPopulation` selects and replays a biome-shaped
  one-to-four-person cast after a site claim. It has 99 abstract role packages,
  server-only selection and combat requests, per-move proof flags, personal
  spear/bomb/fallback choices, and a narrow client display view. It does not
  instantiate Valheim objects or assert exact item IDs.
- CODE-TESTED: `EncounterContinuity` projects quest/relationship flags from
  existing owner ledgers. It retains accepted aid, event, bond, and party facts
  across unload while pausing unavailable scene actions. It recognizes the
  target site's loaded state separately from the issuer's camp.
- CODE-TESTED: `CombatSupport` reserves ward/heal/missile candidates against
  finite resource, commits cost/cooldown only on an observed native cast event,
  and restores resource only through an idempotent verified rest/supply event
  away from combat. Its sample costs are test fixtures, not balance settings.
- SOURCE-INSPECTED: `CHARACTER-BLUEPRINT-AND-COMBAT-TELEMETRY.md` gives the
  authoring fields, class-pair event stream, timing decisions, and balance
  criteria. The canonical arc keeps a person's request, witnessed aid,
  reciprocal temporary travel, safe return, and later invitation as distinct
  moments in one continuing life.
- CODE-TESTED: The earlier Discovery, Allegiance, CampLifecycle,
  NeedsAndAffinity, QuestBroker, EncounterActivation, RescueRelease,
  EncounterLoadouts, specialist combat, voice, and integration modules remain
  on the branch. The first 1.0.x adapter slice compiles with 1.0.32.8 source,
  but the maintained runtime is not wired or installed from these submissions.

## Installed-session evidence and its correction

- LIVE-PROVEN (limited): The preserved 1.0.32.7 startup log and telemetry are in
  `../2026-10-28-installed-1.0.32.7-session/`. Its installed DLL SHA-256 is
  `89B5BEAF24550E79978CF8C7A6E69EC2A2B54BBEE6ACDADC01FD45EE408A1E6F`.
  The log SHA-256 is
  `991A848E359CF60EF222CD7355233547AE09455172B4E2C6B1EFF8BA0893EA73`;
  telemetry SHA-256 is
  `58EB40B95222231F2E380D1E636A3026857128D0987449E0F2EF9DAAC6EAD4E4`.
- LIVE-PROVEN (limited): The later installed 1.0.32.8 session is documented in
  `../2026-10-28-installed-1.0.32.8-session/HANDOFF.md`, with redacted log, raw
  telemetry, and exact OBS clip paths. Its installed DLL SHA-256 is
  `7CC4647364281C878A2C74CDCB209268A7DA53B0B4C5822BE0B311B28EC8D039`.
  Brian observed some physical pushing, so the no-push gate is still open.
  This installed hash has not been proven to match the reviewed source commit.
- SOURCE-INSPECTED: `../2026-10-28-voice-test-interpretation-correction/HANDOFF.md`
  supersedes the interpretation and next-decision wording in
  `../2026-10-28-1.0.32.7-session-analysis/HANDOFF.md`. The user ran the
  superior Berserker encounter to test voices. Two NPC deaths were intended
  test conditions, not evidence of an AI/code defect. The log records 68
  ordinary/warcry/hit/exert/gloat voice plays and two death-voice events.
  The analyzer's four flagged checks are diagnostics, not four established
  defects or a reason to repeat that fight. Preserve the old report as a
  historical record, but use the correction for interpretation.
- SOURCE-INSPECTED: Claude's dated
  `../../CLAUDE-OUTBOX/2026-10-27-norse-names-install-hold/HANDOFF.md`
  records the 0.3.58 naming DLL installation hold. It must not be installed
  into the maintained 1.0.x profile.

## Exact client/server split

| Server owns | Client receives |
| --- | --- |
| Site/cast/person claims, world seed and saved kit | Server-produced silhouette, visible kit, approved voice/text |
| Allegiance, noticed/known facts, need and quest ledgers | Only the faction/identity the server has revealed |
| Affinity, aid agreement, invitation, active slot and release | Interaction options authorized by current projected flags |
| Combat target, movement, item, ammo/eitr debit, native event | Replicated animation, projectile, status and result |
| Server-resident zone/actor authority and save/reload | Presentation of nearby actors and cues |

No client may reroll a cast, create a person, grant a quest flag, infer hidden
friendship, choose a target, apply damage, refill magic, or assign a third field
fighter.

## Builder sequence on maintained 1.0.x

1. Preserve the 1.0.32.7 source commit
   `9933763978fd7c29bfa9909e7ea8b6c47f86e421` and rollback hash, plus
   the installed 1.0.32.8 session evidence. Treat reviewed 1.0.32.8 source
   `33addad57523d5d95df7eb2572403e4f76d1ea36` as source/compile evidence
   until its relationship to the installed DLL is proven. Repair the observed
   physical pushing before declaring the movement gate met.
2. Repair/review the voice and movement lock comparison using
   `LiveCoreAdapter/GateCompatibility/`; a clean rebuild of approved source
   also fails the old token-sensitive hashes. Do not mute voices or blindly
   regenerate locks.
3. Bind the selected cast to an atomic server world record before actor
   creation. Use one stable person, name/lineage, history, kit, and camp/route
   identity. Map abstract kit labels to verified registered native items,
   armor visuals, grips, and animations.
4. Feed live load/unload, target arrival, witnessed help, and party facts from
   the owning ledgers into `EncounterFlagProjection`. Verify save/restart,
   two-player arrival, target unload, issuer unload, and selected resident
   camps. Do not create a second quest ledger.
5. Prove bomb and spear paths separately. A spear throw must have one native
   release and debit; while airborne/lost, use the different fallback. Recovery
   requires the same owned projectile and a safe route. Knife lunge and each
   atgeir move need separate native animation/contact/ally-safety proof.
6. Inspect current Fuling shaman prefab, attack, shield/status, and projectile
   data as candidate support behavior. Do not assume its presentation proves a
   heal path. Bind ward, heal, and missile individually to real native events;
   verify costs, expiration, target legality, friendly fire, and restoration.
7. Use development telemetry per class pair and one-/two-companion raids to
   tune timing, position, resource use, and player agency. A companion dying in
   superior combat is an outcome; report a code failure only for a violated
   behavior, state, ownership, or resource contract.
8. Install only after all maintained gates pass. Record exact package/installed
   DLL hashes, startup logs, voice and combat telemetry, zero-/one-/two-player
   dedicated-server observations, save/reload, zone return, and rollback.

## Agent status and limits

- SOURCE-INSPECTED: On 2026-10-28, the project sidebar showed 21 related chats.
  Each latest turn ended with a final answer; none was actively writing. Sixteen
  were unread to Brian. In his review workflow, those submissions still await
  his reading and approval. A completed chat, passing check, or GitHub push is
  not owner approval. The local naming subagent also completed its assigned
  implementation and tests.
- UNKNOWN: None of the new biome cast, support magic, spear recovery, class-pair
  telemetry, or server-resident paths is installed or live-proven in Valheim.
  Voice candidates still require approval. The maintained adapter and game
  testing are Claude's next integration gate, not something these code checks
  can replace.

## Claude's immediate build direction

Claude can inspect PR #4 and build reviewable candidate code now. First repair
the 1.0.32.8 physical pushing observed in the owner's video, preserving native
combat and voice behavior; use the reviewed semantic gate proposal to check
that those locked behaviors are unchanged. In parallel or next, port the first
maintained 1.0.x encounter slice: server-owned stable person/site identity,
durable claim before actor creation, no duplicate actor on zone return, and
witnessed first contact. Connect the existing Needs, QuestBroker,
EncounterActivation, RescueRelease, and affinity policies as separate owners:
specific request -> witnessed help -> optional reciprocal temporary travel ->
observed outcome and safe return -> later independent invitation and answer.
Do not collapse those steps into one follower toggle. Present the compiled
candidate, gates, and exact changed files for Brian's review. Treat the unread
chat submissions as awaiting his approval even while candidate integration
work proceeds.
