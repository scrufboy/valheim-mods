# Human NPC 0.3.48 registered alert voice recovery

## Proven 0.3.47 runtime failure

The 2026-10-18 18:47 lab run failed when a hostile triggered combat alert. The private alert sound had been created as an unregistered `DontDestroyOnLoad` object and became Unity-null before `EffectList.Create` used it. `BaseAI.SetAlerted(true)` threw `ArgumentException: The Object you want to instantiate is null`.

The exception occurred inside the companion-to-combat transition. The controller correctly failed closed, which then made both companions not ready, removed their transient targets, stopped movement, cleared follow steering, and caused their weapons to return to the peaceful back slots. Repeated hostile hits retriggered the same native alert exception. Telemetry consequently recorded no durable authorized target while Bren Reed fell from 198 health to 17.6 and both companions were largely stationary.

## Correction

- The private alert sound is now cloned during prefab registration, tuned to the verified Souls of the Land human NPC pitch range of 0.80–0.90, and registered as its own Jotunn/ZNetScene prefab.
- The companion NPC's alert effect references that registered, inactive prefab definition.
- The failed unregistered `DontDestroyOnLoad` lifecycle is removed.
- Registration cleanup never destroys a prefab after Jotunn has accepted it.
- Binary tests require exactly two Jotunn prefab registrations: the private alert sound and the independent NPC.

## Preserved work

No combat selection, movement, weapon, formation, wetness, stowing, or door logic changed in this recovery. The 0.3.47 torch-hand ownership correction remains intact: peaceful torch ownership yields the hands until the single combat owner begins a real transition.

## Runtime status

Offline checks can prove composition and registration structure, but the repaired `SetAlerted` lifecycle still requires one in-game hostile alert test before it is accepted.

