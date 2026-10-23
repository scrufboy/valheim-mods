# Verification

## Current candidate

**INSTALLED:** `LivingWorld.HumanNpc.dll` identifies as Living World C.O.R.E. NPC
1.0.0 with plugin ID `local.livingworld.humannpc`. Its SHA-256 is
`71D0F72DB02E5614C9BEC3BC899D49A6DB045DDA308755B8FCB44B4AB8BAB37D`.

**INSTALLED:** `LivingWorld.Telemetry.dll` has SHA-256
`68A9A5E202DBF4B1CDC87004AA1241881B051EAF6E506599D9FE6E018520E90F`.

**SOURCE-INSPECTED:** The package `BUILD-INFO.json` identifies the source branch as
`claude/lwcore-npc-1.0.0` and parent as
`handoff-rimebound-emberworn@4c09a79`.

**OFFLINE-TESTED:** Claude's package records regression ledger 23/23, native gate
16 members, ownership gate 42 sites, and analyzer self-test PASS. Those results
were read from Claude's verification record and were not independently rerun here.

**UNKNOWN:** No 1.0.0 Unity runtime or live behavior result existed when this
handoff was written. The latest available game log predates the Claude install and
records the rejected 0.6.4 registration failure; it cannot judge 1.0.0.

## Handoff checks

**SOURCE-INSPECTED:** The specification was reconciled against Claude's current
1.0.0 source, the companion behavior contract, activity policy, population policy,
building-marker plan, lab affordance catalogue, and roadmap in the authorized
local project paths.

**SOURCE-INSPECTED:** The submission explicitly distinguishes ambient animation
from real work, town recognition from citizenship, Hold from dismissal, and loaded
migration from off-screen simulation.

**SOURCE-INSPECTED:** The submission includes the complete authorized path map and
does not request editing files under the read-only `sources` mirror.

**UNKNOWN:** Proposed interfaces, persistence schema, performance, multiplayer
authority, save migration, and runtime behavior remain unverified until Claude
returns a design and produces a gated candidate.

## Installation status

**INSTALLED:** No installation was performed for this handoff. The active Claude
1.0.0 candidate remains unchanged for the user's direct test.
