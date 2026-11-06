# Verification: encounter/combat wave 2

Date: 2026-10-28. No active game profile was modified or installed by this
submission. Counts below are pure offline policy checks, not Valheim play tests.

| Check | Evidence |
| --- | --- |
| Discovery and scene selection | OFFLINE-TESTED: 25 + 14 checks pass |
| Camp lifecycle | OFFLINE-TESTED: 54 checks pass |
| Friend in Need | OFFLINE-TESTED: 31 checks pass |
| Rescue, release, temporary travel | OFFLINE-TESTED: 37 checks pass |
| Needs and affinity | OFFLINE-TESTED: 34 checks pass |
| Quest broker | OFFLINE-TESTED: 35 checks pass |
| Encounter activation | OFFLINE-TESTED: 13 checks pass |
| Encounter allegiance | OFFLINE-TESTED: 43 checks pass |
| Encounter loadouts | OFFLINE-TESTED: 19 checks pass |
| Spear/bomb prediction | OFFLINE-TESTED: 14 checks pass |
| Knife/atgeir roles | OFFLINE-TESTED: 21 checks pass |
| Runtime integration | OFFLINE-TESTED: 36 checks pass |
| Encounter cue policy | OFFLINE-TESTED: 27 checks pass; line bindings resolve |
| LiveCoreAdapter policy and pinned signatures | OFFLINE-TESTED: policy runner pass; nine 1.0.32.7 source signatures pass |
| Combined core + adapter 1.0.32.8 compilation | OFFLINE-TESTED: pass, local SHA-256 `B353CBF5DF58F8874C77FEF762BC118638AE9B940FBB87EBDC99154A3BF77D84` after known fixes |
| Native ownership gate on combined DLL | OFFLINE-TESTED: pass, 61 approved native write sites |
| Combat voice and movement lock gates | FAILED: legacy locks reject the combined build and even a clean rebuild of approved 1.0.32.7 source; see GateCompatibility README |
| Semantic gate comparison prototype | OFFLINE-TESTED: approved/current core and core-only/adapter equivalence pass; deliberate voice-priority and movement-predicate mutations are rejected |
| Installed profile/live game/dedicated server | UNKNOWN: not changed or run for this encounter wave |

The combined DLL is a local proof artifact and is excluded from GitHub. It is
not an install candidate until all maintained gates and live checks pass.
