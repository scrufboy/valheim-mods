# Living World C.O.R.E. — companion life cycle, party rotation and Fallout-style return home
**Design response, 2026-10-29. STATUS: PROPOSED IMPLEMENTATION CONTRACT, NOT BUILT OR OWNER-SIGNED-OFF CODE.**
**Source anchors:** `LivingWorldCORE/COMPANION-PROGRESSION-ROSTER-AND-RELEASE.md`, `LivingWorldCORE/PROJECT-BOUNDARIES.md`, `claude-exchange/DECISIONS.md`, October 28 player-parity handoffs, October 29 biome-locked bomb response. This note is append-only; it does **not** replace prior approved rules.

## The design pivot
Brian: NPC types, class loadouts and earned abilities need to be chosen for the target threat *within their actual biome and progression*. A player needs to level trusted companions, rotate the expedition party, and send a companion back to their own lawful home (Fallout-style), not delete, reroll, or make them wander indefinitely. Without this, recruiting and growing distinct people has no purpose. This is the point of integration of identity, role, class, skill, origin, home authority, lifecycle, tactical selection, and return travel.

**Core law:** Leaving the field-party **changes assignment**, not identity, equipment, loyalty, skill, or existence. The individual persists outside the loaded NPC GameObject. Identity and actual home are authoritative; no released or inactive companion is ever ownerless.

## 1. Who: distinct populations, explicit ceilings
The existing governing roster document balances campaign around **two active expedition slots**, unlocked after **Eikthyr (1st)** and **Elder (2nd)**, and **six living bonded companions total**, active members included. These are default campaign values, not a claim that the debug laboratory limits were enforced in the October 28 16-spawn stress test.
- **World people:** camp, faction, settlement, roaming actors with their own home/territory and community; not player assets.
- **Known people:** have recognized the player and remember prior meetings; not automatically bonded.
- **Bonded roster:** ongoing personal relationship; includes active companion, at-home individual, valid guard or resident.
- **Active expedition party:** at most the unlocked field slots, the only group attached to the player for normal follow/combat; distant residents do not inflate raid scaling.
- **Permanent death:** living identity is closed; memorial persists; no free resurrection or new copy.

Each actor has biome, origin, faction, class/archetype, authorized equipment/skills and legal progression ceiling. An Ooze Bomb user does **not** learn Bile Bombs because skeletons are poison-immune. Threat evaluation chooses **among that person's real abilities and carried gear**, not among every ability in Valheim. Future specialized anti-undead classes are separate authored class definitions, not arbitrary cross-biome loadout patches.

## 2. What: three commands with different effects (nomenclature correction)
The approved 2026-10-27 roster document already uses **"Release from Party" to mean the ordinary end of expedition assignment**, preserving the ongoing relationship. Do **not** silently change that command into a permanent breakup.
- **Return Home / Release from Party (ordinary):** leave active party, keep bonded roster seat; set valid home destination; person returns to camp/village/post and becomes `AtHome/Available` or its valid existing job state. Their skills, history, kit, scars, friendship and future recruitment eligibility remain. No new encounter roll or five-day recruitment cooldown required to rejoin an existing bond.
- **Rejoin Expedition:** at a lawful location, with an open unlocked field slot, healthy and available; reverse the assignment in a single transaction, no duplicate NPC instantiated.
- **Part Ways / Sever Bond (distinct deliberate action):** after explicit confirmation at a safe handoff, end bonded relationship and free **one of the six** bonded seats. Person persists independently with a camp/home/faction authority and their full identity/experience/gear. They do **not** despawn into oblivion or instantly turn hostile. Existing five-in-game-day new-lead cadence applies if this constitutes a genuine abandonment/recruitment change; exact re-bonding policy is an owner decision rather than an instant menu exploit.

The earlier **camp-of-one** concept remains: a former companion returned to independent wilderness life can establish or regain a camp, plausibly relocate within the original region, remember the player and potentially later be found with another person. The deterministic newcomer design, lead counting and timing are still **UNRESOLVED**; do not automatically populate new actors.

Separately, **Assign Post / Residence** is a legal *assignment* for an at-home bonded individual when charter/home/job contracts exist. Changing guard/resident roles is not severing the bond. No universal command teleports people across regions.

## 3. When and where: lawful transitions
- Reject Return Home while actively engaged in combat, committed attack, falling, dead/downed, on a ship underway, crossing an instance boundary, inside an unsafe dungeon, in deep water or on hazardous lava/terrain. Instead expose **Pending Return** or “Return when safe”; do not release combat ownership in the middle of a swing.
- A safe handoff requires an **existing** lawful home: initial proof is the character's persisted origin/nomadic camp (as the 2026-10-28 decision says). Later registered choices may include a real dwelling/bed, chartered reclaimed village, allied camp or assigned guard post. Never pick a fictitious unbuilt home or closest random world point.
- If multiple authorized homes exist, let the player choose; otherwise use origin camp if still valid. If none is valid, **remain with the party or Hold at a verified safe fallback**, while the transition stays incomplete. No ownerless idle wander, no straight-line pathing into unloaded wilderness.
- When a companion departs, show an acknowledgement, stow/draw appropriate travel equipment and leave locally by normal navigation while the world is loaded. When safely outside observation and simulated path bounds, an **authoritative background transit record** may advance travel toward the home; do not simulate thousands of meters of continuously loaded actors. Reconstruct the **same person** near their lawful home upon arrival, after checking ground/safety and that no other instance is alive.
- **No unexplained instant teleport.** Return/transit can use the existing zone/instance transfer contract only after it is live-proven. Do not claim a route through a portal that the character cannot use. Long-distance transit timing/route risk are tuning and owner decisions. No random offscreen death until explicitly designed and auditable; NPCs must not die invisibly due to absent path simulation.
- Changing a party slot becomes effective **only on authoritative departure/assignment commit**; before that, the leaving companion still counts toward the active limit. After committing, a companion in `ReturningHome` cannot fight for the field party or be called back remotely to shortcut travel. Exchange requires an authorized safe rendezvous, not one button during a boss encounter.
- If a client disconnects or the server restarts during transfer, recover the saved transition idempotently: old assignment OR new assignment, **never two actor copies, equipment copies, lost person, or phantom field slot**.

## 4. Why: gameplay reasons to invest
- Two field slots make tactical tradeoffs meaningful. A trained bow/spear specialist, shield defender, blunt heavy, healer, poison thrower, and potentially later magic/area specialist are **different people with different permitted skills**. One general-purpose NPC should not acquire every weakness counter.
- The player might choose blunt against Skeletons/slimes, poison control against poison-susceptible living groups, a bow/spear huntress against moving/flying targets, and suitable protective gear against drake frost. A class should consider observed enemies' resistances, **but only select from its true carried weapon/ability set**; no omniscient resistance lookup or unrealistically guaranteed hits.
- Earned levels belong to the stable individual, not the party slot, spawned class template, or player roster entry. Skill XP comes from real actions with the native progression curve; seed strengths account for class/biome **and** boss keys, days survived and legitimate gear/material progression (owner decisions 2026-10-28 18:14). No passive XP farming while unloaded or at home unless a later measured approved job-training law exists.
- Existing party members can rest, heal and continue settlement life; waiting does not make them vanish, grant infinite supplies, or magically repair equipment. The player must visit/reunite at a valid location to bring them back.
- Two companions should not automatically defeat same-tier bosses while the player stands idle. Tactical switching offers options, not a substitute for player agency.

## 5. How: authoritative per-person contract
Keep one **persistent person record** keyed by a stable immutable `PersonId` (not Unity `GetInstanceID` or the transient GameObject) with:
`PersonId, OriginCampId, CurrentHomeAnchorId, FactionId, ClassProfileId, Identity/VisualRecipe, RelationshipState, BondOwner, ActivePartySlot?, AssignmentState, CombatOverlay?, TravelTransactionId?, TravelPhase, SourceAnchorId, DestinationAnchorId, LastSafeAnchorId, WorldPosition, Availability/Recovery, Skills[skillId→level+fractionalXP], EarnedAbilities, EquipmentOwned/Loaned, ConsumableStock, Injuries, History, Alive/DeathRecord, Version`.
- **Formidable NPC Framework** owns reusable identity, authoritative persistence, faction/home/anchor/transfer and attack capability APIs. It must not hardcode Living World lore, party size, bonds or encounters.
- **Living World Formidable NPCs** owns roster size, slot unlocks, relationship/assignment rules, safe return path, person progression, class/legal ability binding, party decisions and home-job choice.
- **Village Systems** owns charter/resident/dwelling/bed/job/guard eligibility; it must accept/reject a home assignment through a contract and never silently conscript someone.
- **Voice/animation/appearance** consume status/assignment events; no competing life-cycle authority. Native `MonsterAI` retains committed combat execution.

Recommended **mutually exclusive primary assignment**: `WorldIndependent`, `KnownAtCamp`, `BondedAtHome`, `ActiveParty`, `PendingReturn`, `ReturningHome`, `AssignedGuard`, `SettlementResident`, `InjuredRecovering`, `Dead`. `Combat` is an overlay/temporary domain ownership state, **not a replacement** for the home/bond/territory underlying assignment. A record always has a valid jurisdiction/home/rally/fallback.

**Transition transaction** (idempotent, network-owner/server authority):
1. Validate identity, living status, real bond, allowed action, field/roster capacity, destination/home charter and available safety.
2. Reserve one transition token and snapshot personal gear/loans; disallow concurrent recruit/release/portal/death transactions.
3. At safe handoff, end follow and native party combat entitlement; persist `ReturningHome` and destination; free the active slot **once** when departure commits.
4. Perform local believable departure; background transit when unloaded only under approved route/timing rules; materialize same person at the anchor, preserving skills/gear and any assigned activity.
5. Confirm `AtHome`, `Guard`, `Resident` or `IndependentCamp`; on path/anchor failure persist `HoldSafe` or `NeedsRecovery`, never reset personality/identity. Make same command retry safely.
6. Rejoin validates same `PersonId`, real available home position, health, lawful role handoff, equipment and empty field seat. No spawn-control creation and no automatic new-lead timer.

## 6. Non-negotiable integration gates
- **Debug vs campaign:** tool-spawned laboratory NPCs can test return states but never become canonical discovered/bonded campaign people. Spawning 16 for load tests is not legal recruitment.
- **World authority:** no active third follower when two slots full; no seventh living bonded recruit when six seats full. A returned person still counts among six; a parted-ways person remains in-world but does not count as bonded.
- **Traversal before release implementation:** comply with existing mandatory cave/dungeon, portal and save/load identity-preservation work. Return Home must consume that solution; do not add a duplicate teleport or actor registry.
- **Safety/mortality:** no disappearing person or offscreen lethal pathfinding; no silent revival after death. Equipment recovery is separate from person recovery. No loot duplication; versioned transaction ledger.
- **Control domains:** native combat is native; a homecoming command cannot add competing movement, hands, damage or attack logic while combat owns the actor.
- **Network:** verify owner switch, remote clients, disconnect, restart, zone unload/reload, duplicate RPC, competing transactions, inventory transfers, path failure and location resettlement.
- **Gameplay:** change party at lawful site, persist two companions' distinct skills/gear before and after swap, beat/lose realistic same-tier encounter, demonstrate each class's tactical strengths without cross-biome ability unlock.
- **Mortality:** a deceased person's roster entry closes permanently and leaves a memorial; cannot be chosen in swap list. Odin restoration remains a separately gated future lore matter.
- **Telemetry:** emit `AssignmentRequested`, `AssignmentRejected(reason)`, `DepartureCommitted`, `TransitStarted`, `ArrivedHome`, `HomeFallback`, `RejoinCommitted`, `BondSevered` with stable person ID, transaction ID, source/destination and final party/roster counts. Do not falsely log requested as completed.

## 7. The minimum meaningful vertical slice
Before further class/combat expansion, **design and acceptance proof** should demonstrate:
A. Two campaign-eligible people recruited through legitimate encounters; real persistent PersonIds.
B. One leaves party at an existing origin camp and travels/returns lawfully, freeing exactly one field slot without losing a bonded seat.
C. The other field slot is exchanged with a **previously bonded** resident/person (no new 5-day recruit lead).
D. Save/reload and zone transfer recover both correctly; the former companion is found by name, same hair/voice/loadout/skills/equipment and history.
E. A separate explicit `Part Ways` transition frees one bonded seat, persists an independent person at lawful home/camp, without deleting or making an NPC turn into a generic recruit.
F. Rejections while fighting/boating/dungeoneering or without a lawful home preserve authority and prevent duplicate actors.
G. No regressions to 1.0.32.6 combat voice, 1.0.32.14 body voice, bomb prediction, native combat, movement writer, and established traversal gates.

**Sequence:** draft/approve this contract now; no gameplay edits authorized by this note. The currently installed 1.0.32.14 tests and traversal/portal gate remain engineering prerequisites. Once authorized, build the smallest persistent party↔home transition independently of new special attack classes, then integrate class/skill selection with that roster.

## Open decisions for Brian (do NOT silently resolve)
- Whether a return-home companion is simulated traveling for world-time or becomes available upon safe remote arrival, and which legal transit routes are allowed.
- What home choices appear first: initial origin camp only or already chartered allied village/guard post when implemented.
- Whether “Part Ways” is irreversible or allows later reconciliation through a new encounter (and how the 5-day lead cadence applies).
- Whether an at-home bonded person can train from approved settlement work or only maintain existing XP.
- UI naming and presentation: the existing **Release from Party** action is ordinary roster-preserving departure. Suggested clearer visible labels **Return Home** and **Part Ways**; neither is a generic delete button.
