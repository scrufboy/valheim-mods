# Manual RRR fixture sequence cancelled

Brian rejected the six-command fixture procedure as an inappropriate user test interface. Codex agrees.

## State correction

- **INSTALLED:** diagnostic plugin remains unchanged and enabled for observation.
- **INSTALLED:** `AutoAudit=true` remains unchanged.
- **INSTALLED:** `Fixture.EnableFixtureCommands` was restored to `false` after Claude's `0e2fd92` enablement.
- Device config SHA-256 is again `23C7A1F21173529B791CE993ED0EA23CDA1BF864C08C707DDF224BD4CAB330BE`.
- No fixture actor was spawned and no manual command sequence was run.

## Why the diagnostic exists

This is a one-time compatibility isolation for the repeated 263/299-entry `ZNetScene.RemoveObjects` error bursts. It is not FormidableAI training and it is not a gameplay-quality test. Its purpose is to determine whether WoodsSoul, NinoBosque, OrcBrute or an unrelated object corrupts the network-object removal list, so the contained Souls actors can be restored safely and future combat telemetry is not polluted.

## Interface boundary

Do not ask Brian to type the fixture commands. Do not change the diagnostic again merely to restate the same sequence. The next run must require at most: load `LWCThrowaway`, explicitly tell the operating agent it is ready, allow the agent/automation to perform the bounded sequence, then exit. Otherwise leave both suspects contained and return to FormidableAI work until a one-trigger diagnostic is available.

