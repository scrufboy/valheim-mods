# Living World Human NPC 0.6.3

## Scope

This release corrects stale persisted hand visuals without changing inventory, weapon selection, combat, traversal, guard coverage, spawning, identity, or the accepted 0.6.1 last-used back stow.

## Evidence and root cause

During the 0.6.2 live test, Geir visibly held a sword while his bow stowed. Telemetry proved that his logical equipment was never mixed: his real hand slots contained only `BowFineWood` or nothing. The sword existed only in the persistent `VisEquipment` right-hand record.

Valheim's `SetLeftItem` and `SetRightItem` methods return early when the local setter cache already equals the requested value. On a persisted non-Player human, that local cache can begin empty while the networked ZDO still contains the weapon from the previous session. Asking the ordinary setter to clear an already-empty local slot therefore leaves the stale networked sword visible.

## Correction

`HumanEquipmentModule` now treats the two logical hand slots and two hidden/back slots as one mutually exclusive presentation:

- stowed posture writes empty hands plus the exact hidden pair;
- drawn posture writes the exact visible pair plus empty back slots;
- all corresponding ZDO presentation fields are reconciled together;
- the transaction runs only when the intended presentation changes;
- no item is selected, replaced, removed from inventory, or hardcoded.

Telemetry schema 8 independently records all four persistent visual hashes and whether they match the four logical Humanoid slots.
