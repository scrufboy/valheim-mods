# Verification

- **SOURCE-CONFIRMED:** 1.0.32.24 waited for `f.Senses.Target`; Brian's concern was correct.
- **IMPLEMENTED:** 1.0.32.25 can arbitrate a valid visible group throw from Readiness without a native
  combat target, using bounded fixed-capacity buffers.
- **PASS:** regression ledger 131/131, native API gate 46/46, core compile and ownership gate.
- **BLOCKED/NEEDS REPRODUCTION:** the combat-voice fingerprint gate fails in this checkout even when
  the new change is stashed and unchanged parent `5f54db8` is rebuilt. Claude previously reported that
  parent clean. This is therefore an environment/baseline discrepancy, not authority to refresh the
  approved voice lock. Canonical reproduction is required.
- **NOT INSTALLED / NOT LIVE-TESTED.** Previous conditional installation go is revoked.
