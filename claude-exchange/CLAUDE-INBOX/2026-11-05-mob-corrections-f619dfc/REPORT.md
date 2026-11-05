Source pushed: scrufboy/lwc-valheim-workbench, branch work/valheim-workbench, commit f619dfc. Fetch before editing. This report supersedes earlier candidate/audio/agent status. No claim that Claude has already read or accepted it.

# Mob correction report for Claude

## Failures and their consequences

The installed Þurs0.2.1 is the rejected fitted Native14 payload, not a corrected
own-animation build. Earlier rotation bridges/native motion conversions and
mesh fitting did not meet the owner's source-shape requirement. Long private
physics/pose gates delayed complete candidates and did not establish that the
installed creature was fixed. The claim-by-claim history is in
TROLL-CLAIMS-AND-RAID-AUDIT-2026-11-05.md; it must not be rewritten as success.

The later own-clip candidates loaded, but independent review found real bugs:

- Troll and Bear attack states lacked the native `attack` tag; stagger states
  lacked `stagger`. Native movement/attack/stagger logic uses these tags. A clean
  load did not prove working combat. Both Editor controller builders now set
  the tags and produce fresh OwnClips002 derivatives.
- Both corpse collider bakes used BakeMesh(true) followed by TransformPoint,
  applying renderer scale twice. Both now use BakeMesh(false) with TransformPoint.
- Troll/Bear corpse appearance transfer only ran on the owner. Both now publish
  a bounded one-time ZDO own-hierarchy pose/scale/renderer snapshot, with a remote
  receiver and visible fallback. Actual multiplayer behavior remains unproven.
- Marauder08 still put the decorative back tool through collider/per-vertex
  paths. Candidate09 excludes that tool from collision data and surface/bounds
  comparisons. It stays rendered on body/corpse. Body11/joints10 remain unchanged;
  colliders18 became17. Whether this contributed to the player's hop/pop is unknown.
- Marauder08 had no remote handoff and could leave remote bodies invisible.
  Candidate09 adds a bounded ZDO pose/visibility/held-template snapshot and visible
  configured fallback for missing data. Multiplayer and material-property-block
  behavior have not been scenario-validated.
- Root checked only the audio cache, instead of Unity My Assets, then asked Brian
  to identify an already-owned pack. After his screenshot, the Heavy Zombie pack
  was downloaded and extracted. Stopping at its inventory was also incomplete.
- Earlier child agents reported quota failures. Fresh review agents in this pass
  ran successfully; treating old failures as a blanket collaboration block was
  insufficient. Three reviews completed; two reviewers implemented corrections.

## Current built candidates

All paths start at `C:/Users/scruf/Documents/LWC Model Skin Studio/Exports/`:

| Package | Current complete ZIP |
|---|---|
| Þurs0.3.0 | Thurs-0.3.0-OwnClips-Candidate04/LivingWorldCORE-Thurs-0.3.0.zip |
| Bjorn0.2.0 | Bjorn-Reborn-0.2.0-OwnClips-Candidate03/LivingWorldCORE-Bjorn_Reborn-0.2.0.zip |
| Marauder0.3.1 | Sunken-Marauder-0.3.1-Candidate09/LivingWorldCORE-Marsh_Marauder_Prototype-0.3.1.zip |
| Colossus0.1.4 | Undead-Colossus-0.1.4-Audio-Candidate01/LivingWorldCORE-Undead_Colossus_Prototype-0.1.4.zip |

Exact candidate IDs also appear in each ZIP's LWC-CANDIDATE.json and manager
description. The old Troll03/Bear02/Marauder08 and Colossus0.1.3 folders are marked
superseded. No images of superseded builds are used as current combat proof.

The four current candidates loaded TOGETHER in the existing private saved world
with no logged error/exception lines. All four ready/registration messages were
present. Evidence:
`C:/Users/scruf/Documents/LWC Studio QA/Connected-0.5.0-ac2ed85cdedf4f82a318a32ab916ea43/AppearanceChecks/ee384c294437487fbee7c060396e1013`.
This does not establish combat, death, player knockback, multiplayer or raid acceptance.
No new pose/metric suites, fresh map or sky drop were used.

## Colossus audio is integrated now

Resonant Mesh's Heavy Zombie Vocal Pack supplied63 original96kHz stereo24-bit
WAVs:41 vocals,16 impacts,6 extras. Eighteen selected working copies are included
as mono48kHz PCM16, three each for idle/alert/attack/hurt/death/impact. Original
masters remain untouched/private. audio-bank.json records source files and the
provisional duration/energy/envelope-based selection; no semantic audition is
claimed. An owner audio-review HTML offers manual-play controls for all18 cues.

Runtime loads the bank into positional ZSFX templates. Attack vocal duplication
from baseline alerted effects is removed; acquired attacks play at start and
impacts at hit. Native Draugr/Brute layers remain. Actual game logs confirm all
six roles with three clips each and no errors. Publisher attribution is included.
The approved Colossus DLL, source bundle, rig, animation and movement are unchanged.
Dark skin,12m/10s poison death cloud and zombie-raid start hook remain content-only.
Their damage/raid/audible suitability still require Brian's scenario review.

## Installed / next

No Arena package was replaced in this pass. Read-back still finds Þurs0.2.1,
Bjorn0.1.1, Marauder0.2.2 and Colossus0.1.2. Owner saves, frozen desktop app,
raid configuration files and unrelated packages are untouched.

Brian installs/tests the exact current candidates with video per the handoff.
Root fixes from that video. Troll trophy/scarified art follows accepted source
shape/motion. Bear has only four supplied clips; death/stagger/swim/sleep/jump/
consume use documented fallbacks, and sleeping/spiritcaller prefab variants and
its trophy remain unchanged. No universal arbitrary-mod corpse compatibility,
dedicated-server support or public-release readiness is claimed.

Claude/Cursor: fetch the current Workbench branch before edits, read this report
and DELIVERY-STATE, and coordinate ownership. Do not resume native retargeting,
fitting, back-equipment systems or metric gates. Do not call a clean load a
gameplay pass or an uninstalled candidate an installed fix.

