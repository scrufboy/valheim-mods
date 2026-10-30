# A1 revision 4: native initiative independent of awareness state (answers cebfcd0)

Commit `eb28f1c` on `work/awareness-a1` (parent `d989514`). Not installed.

Changed files:
- `LivingWorldCORE/npc/src/Kernel/Awareness.Pure.cs`: `InitiativeLedger`, `MemberEdges`, `MilestoneMark.UpperBound/By`;
  `ContactBook` forwards awareness milestones to the ledger and no longer owns traces; `TransitionsAfter(..., out lost)`.
- `LivingWorldCORE/npc/src/Kernel/AwarenessGroup.cs`: native observation via edges into the ledger, independent of
  contact state; envelope and presence marks; retirement; `FlushAll`.
- `LivingWorldCORE/npc/src/Telemetry/CoreTelemetry.cs`: `ub*`/`by*` fields, `seq`; `InitiativeGap`/`ContactGap`;
  flush and drain at RunStop/world exit; ledger counters in `AwarenessBudget`.
- `LivingWorldCORE/npc/tests/RegressionLedgerTests.cs`: biased test replaced; 8 new ordering/flush/gap tests.
- `LivingWorldCORE/npc/tools/analyze_session.py`: before/after/absent ordering counts, native-only and upper-bound counts,
  signed latencies with negatives, true medians, loss FAIL, lead calibration.

Gates: ledger 155/155; native 46; ownership 76 (unchanged); voice LOCKED unchanged; movement-writer OK; analyzer OK.
Hashes: core `c95d9ef7…a61a8e5`, telemetry `da82cdca…b08c03da`.
Presentation §10.5 retracts "never-noticed publish nothing" and the Noticed-before-target acceptance line.
