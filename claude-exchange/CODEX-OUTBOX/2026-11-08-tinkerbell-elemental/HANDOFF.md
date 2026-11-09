# Current: Formidable Surtling Finalization22 built, live test blocked

Brian explicitly resumed all finalization work. Source checkpoint aed5ecb688cce13fbb3487ae71f7c47bfa0643e4 on scrufboy/lwc-valheim-workbench work/valheim-workbench; localcommit33fffb8. Read exchange/surtling-elemental/2026-11-07/recovery06/DELIVERY22.md and FINALIZATION22.md.

Built private0.1.12 Recovery22Package02 with:
- Pre-zero physical hot-window finisher gate, one immutable native drop result.
- Prolonged nativeWet no-reward dissipation; explicit custom exposedrain soaking (60sdefault, configurable; vanillaSurtlings do not gainrainWet).
-3s dangeroushotsteam using nativefireAoE; separate nativeSmoke attackhazards, bounded anchors/puffs/TTL.
- At mosttwo reignitions to25%-maxHPfloor, then missed-window exhaustion/onecoin. No postdeath revival or endless healing.
- NormalVolley/Crossfire/Pressure decisions independent of downing,6s recoveryrage as tacticalbias. Corrected style reentry, hoverdescent overshoot and upward avoidance heightcap.
- Existing acquired female visual/controller/fire/smoke/attack/death bundle and nativebodyflames preserved. Fullreward core1..2base/2..3level2+,AncientBark/Resin2..4, optionalnativevaluables withconfigchances. SoakgivesZERO reward. Missing a window permanentlyreduces laterfinisherreward toonecoin.

Runtimecompile against actual installed gameassemblies, ZIPCRC/manifest/payload equality passed. QA22helper0.2.1compiled with exact named disposable observer/worldguard; physicalfinisher, nativeWet injection, poisonnonlethality, immutableRewardreads and finiteexhaustion tests prepared. Neither runtime22 norQA22 was installed or liveexecuted.

Desktopinventory succeeded; r2modman launch failed exactly: GetCursorPos failed: Access is denied. (0x80070005). Restore interactiveWindows desktop access before authorized UIimport/liveQA. Do not bypass this through another UIautomation route.21version0.1.11 is still enabled in separateFlightTest; Toolkit3.0.5/ServerDev1.116/Jotunn2.30.2retained,QA20disabled. No profile/savechanged.

Package download: https://chatgpt.com/library?file_id=libfile_e16dcc551db48191921feea970da7f66&account_id=personal
SHA256d4748110d01ea8f18e6ce750357cfaab1c5193a8ee84a55d6cb52ac490ee4d14
Localoutput/FormidableSurtling-Recovery22/scrufboy-Formidable_Surtling_Female-0.1.12-Recovery22-Package02-PRIVATE.zip
Testhelperlocaltest-output/FormidableSurtling-Recovery22/QA22/LWC-ElementalCycleQA22-0.2.1.zip is separate and must never ship withcreature.

Not game-ready/released yet. Actualbomb/rain/immersion, physicaldropcounts, densecombat, cover, smokeexposure/obstruction, movement/faction/companion encounters and multiplayerremainacceptance. Elementalaudioaudition, rigidcarbonalternative/summon/skinsremainpending. DedicatedTinkerbellplayerowner retainsitsseparateprototype; no sourceownershiptransfer. Source21andallpriorcandidatespreserved. OrdinaryGit history vsAPIcheckpointsrequires later non-destructive reconciliation.

---

# Current reveal preview: Surtling21 Package04 + Tooler3.0.5

Brian corrected the boundary explicitly: the creature IS the Formidable Surtling/fire elemental. Explosive provides player Sorceress animations, not NPC AI. Our behavior parts are FormidableAI modules. Neutral HoverCombatModule/Profile now exposes the reusable combat-motion contract; future player Sorceress needs full controller input/animation/telemetry integration, not an assertion that the NPC API already supplies that.

Ground-oriented means hovering just ABOVE terrain with a visible buffer. Contact Land is deliberate quench behavior. Latest reward correction: maximum reward for finishing the brief peak-danger steaming window; thoroughly soaked/rain/drowned defeat gives NO reward, superseding earlier coin proposals. Finisher/steam-damage/reward gate is not implemented in this candidate; native drops/ordinary lethal damage remain.

Corrected source checkpoint 5e7a93ee23fec243df806b417a938063da06dd3d on work/valheim-workbench, scrufboy/lwc-valheim-workbench. Read recovery06/DELIVERY21.md and SORCERESS21.md. Local source commits4742fc7,00f18fe,cae3fef preserve implementation and evidence. All previous versions remain preserved. Package02/03 were integration failures (address, staged inactive parameter validation); superseded by04.

Actual Package04 installed in LWC Surtling Flight Test and live-loaded with acquired female geometry, visible native body flames, acquired trails/steam/ground/attacks/death retained.142 telemetry frames,24 casts including primary/searing shards, zero Error/Exception lines in the successful run. New visible-attack evasion logic compiled; no independent live evasion cue was observed in this capture. The saved flame image is real gameplay, not a render substitute.

Private Surtling0.1.11Package04: https://chatgpt.com/library?file_id=libfile_84dc7a90abc08191bf0efb33c8c1e49f&account_id=personal
SHA2566be6e86c3c6a5f20aedcec5fe4d0f3d8ca58f4b9b36478b3ed295244fcd32ef1

Tooler3.0.5 installed, ServerDev1.116retained. Nativefreefly/FOV, nearest-creature framing, local singleplayer freeze, F9 clean PNG and F10 restore work together. Actual clean1920x1080 capture:
https://chatgpt.com/library?file_id=libfile_37a2fd2fba8081918b5e839d126aa8ca&account_id=personal
Toolerpackage: https://chatgpt.com/library?file_id=libfile_5ba4685c760c81918fd6e43407afab89&account_id=personal
Toolkit local repo LWCToolerRebuild commits05b4435/8df3e29, no remote configured. Camera source/handoff under integration/src/LivingWorldCamera.cs and exchange/CAMERA-3.0.5-2026-11-08.md. No other actor AI changed.

Game left in separate LWCTinkerbellQA with the candidate framed and action frozen for Brian's review. F8resumes,F9captures,F10restores; DeleteopensTooler and resumes photo freeze. Disposable QAobserver died before enabling protection after load; no owner character/save used. No OBSrecording. PoisonQAhelperdisabled. Capture/UI proof is separate from full gameplay, audio, Arena, multiplayer, reward/finisher acceptance.

---

# Tinkerbell elemental - shared implementation handoff, November 8

Brian explicitly requested this handoff for Claude and the fire/audio work.

## Latest playtest feedback

Brian's direct feedback, relayed here from the Real Smoke and Fire chat: “What I witnessed was really cool behavior. I gotta see more of it. I want you to just let it know that I just saw amazing stuff in the last play. Oh my goodness.” Preserve the current effects/behavior candidate as the comparison baseline. This positive observation does not imply that every scenario, loot proposal or multiplayer feature is accepted.

## Current source and installed candidate

Canonical direction: https://github.com/scrufboy/lwc-valheim-workbench/blob/main/docs/project/TINKERBELL-ELEMENTAL-CONTRACT-2026-11-08.md

Corrected source checkpoint: work/valheim-workbench commit df384f0fb5f51430ebd03d30470e4d1eade79808, directory exchange/surtling-elemental/2026-11-07/recovery06. Read DELIVERY20.md and ELEMENTAL20-STATUS.md. Local source commit39f22dc. Ordinary pushes fail DNS; connector source checkpoints preserve remote files with non-forced leased updates. Local and remote API-created histories require later reconciliation; do not reset either side or claim all repositories fully synchronized.

Version0.1.10 Recovery20 Package02 is installed/enabled in **LWC Surtling Flight Test**. Toolkit3.0.3 and Server Devcommands1.116 remain enabled. QA-only poison injector is disabled after testing, absent from creature ZIP. Valheim exited after testing.

Actual controlled native damage-path poison testing completed three cycles:
Descend -> actual contact Land ->3s vulnerable Stunned/steam -> arms-out SpecialAttack1 Reignite -> Active, phases1/2/0.120HP reduced to106.5853 without healing. Actual Searing Shards, Cinder Fan and primary fireball releases logged.129 telemetry samples; capsule-foot terrain clearance0.080-0.089m at Land; zero Error/Exception lines. This was injected HitData, not an actual thrown bomb or multiplayer test. Package01's touchdown probe caused upward avoidance; Package02 caps that probe at landing support and completed all cycles without teleport/physics bypass.

Private ZIP: scrufboy-Formidable_Surtling_Female-0.1.10-Recovery20-Package02-PRIVATE.zip
SHA256 ebe48813c5073caaffc78246040a305fb1428f0d830577b68fc07642e632738e
Persistent download: https://chatgpt.com/library?file_id=libfile_07d89c7f0440819181c94c39fba5ca71&account_id=personal

## Shared behavior contract

Ground-oriented hovering fire elemental with its core as heart. Native MonsterAI sensing/hostility and actual ground combat opportunities remain. Low terrain-following buffer; independent facing/travel with forward/back/side/diagonal lean and synchronized turns. Brief tactical elevation only2-3 human heights, approximately4-6m. No scripted circles, timed climb/dive loops, high Drake flight or Deathsquito dive-bomb role. Reusable bounded environment probes; no trained machine-learning claim.

Poison resistance explicitly changes Immune -> Normal on this replacement. Frost remains weak. Native Wet/damage/duration persists, including ongoing damage after leaving water until native Wet expires. Reignition does not cure Wet. Quench cancels stale native attacks and uses an18s cooldown, no zero-HP revival. Acquired NatureManufacture body flame/steam/trailing smoke/near-ground scorch and existing Inferno/Distortion attacks/death effects are bound.

## Ownership and next work

Surtling Branch Recovery owns creature traversal, controller, combat, quenching, body/trail/recovery/death effects and its bounded fire lease. Real Smoke and Fire owns world-fire adapters. Audio work supplies auditioned clip roles/mix notes, not a second creature writer. Need elemental cast/burst, smolder/crackle, steam/hiss and recovery laugh candidates. Current female voice remains provisional.

Read the fire chat's local handoff exchange/real-smoke-fire/2026-11-08/SURTLING-OWNER-DIRECTION.md for the newer finisher/loot/recovery-boost proposals. They are pending reconciliation: the tested build still uses native drops, allows ordinary lethal damage and has no recovery healing. Do not claim the proposed kill-window/rare loot system is implemented. Acquired Elemental summon, rigid ash corpse and variant skins remain pending.

Arena integration is authorized but requires baseline backup, exact roster and coordinated telemetry. Prepared manual event names: lwc_elemental_observe, lwc_elemental_poison, lwc_elemental_frost, lwc_elemental_roster. The prepared CFG is source, not proof of installation or raid execution. No multiplayer or final owner appearance acceptance claimed.

Direct app thread enumeration failed again. This Git handoff is verified shared delivery; no direct message delivery to other chats is claimed. Preserve19, avoid competing source writers/Unity builders, and retain this Package02 baseline.
