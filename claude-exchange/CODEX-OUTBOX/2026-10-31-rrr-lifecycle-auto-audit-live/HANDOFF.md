# RRR lifecycle diagnostic 0.2.0: automatic audit live result

Brian completed the authorized audit-only throwaway-world launch. No companions, commands, fixtures, combat or raids were used.

## Runtime gate

- **LIVE-PROVEN:** BepInEx loaded `LWC RRR Lifecycle Diagnostic 0.2.0`.
- **LIVE-PROVEN:** `RRR-LIFE armed 14/14 hooks` with fixture commands disabled.
- **LIVE-PROVEN:** automatic audits ran for WoodsSoul, NinoBosque and OrcBrute.
- **LIVE-PROVEN:** zero `RRR-LIFE DIAG-FAULT` lines.
- **LIVE-PROVEN:** zero game error/exception lines in this launch.
- **LIVE-PROVEN:** clean world teardown reset (`clearedViews=0`).

## Audit findings

All three registered prefabs are active and have exactly one root `ZNetView`, zero nested network views and zero missing scripts.

- WoodsSoul: every concrete default/random-weapon item exists in ObjectDB with no child ZNetViews or missing scripts. Its existing `randomArmor[0] NULL-ITEM` is present on the known-working reference, so that condition does not explain the suspect actors.
- NinoBosque: EphemeralHelm and RunicAxeBlue both exist in ObjectDB; every listed item reports zero child ZNetViews and zero missing scripts.
- OrcBrute: both GrimAge-named armor entries exist in ObjectDB despite no separately loaded GrimAge mod; all five weapons, two armor entries and two shields exist and report zero child ZNetViews/missing scripts.

The static prefab audit did not identify a defect. If NinoBosque or OrcBrute causes the historical removal burst, the failing evidence begins during instance construction, runtime gear selection/attachment, AI startup, or removal—not in static registration or a missing item reference.

## Preserved evidence

- Profile backup: `BepInEx/LivingWorldBackups/sessions/2026-10-31-rrr-lifecycle-auto-audit/`.
- `LogOutput.log` SHA-256 `D1B4735C8AFBB6305AD4870412CE2F7F85E1DC2DD6216F64B4F12465A5897A6D`.
- Captured config SHA-256 `23C7A1F21173529B791CE993ED0EA23CDA1BF864C08C707DDF224BD4CAB330BE`.

## Next boundary

The fixture stage is now justified. Claude may change only `Fixture.EnableFixtureCommands=true`; keep the plugin, Tooler and actor containment unchanged. Read the config back from device and record its hash. Brian then uses the same throwaway world, no companions, at 30 m and seed 1337, one actor at a time in this order:

1. `lwc_rrrdiag fixture RRRN_WoodsSoul 30 1337`; wait 10 seconds; `lwc_rrrdiag remove`.
2. `lwc_rrrdiag fixture RRRN_NinoBosque 30 1337`; wait 10 seconds; `lwc_rrrdiag remove`.
3. `lwc_rrrdiag fixture RRRN_OrcBrute 30 1337`; wait 10 seconds; `lwc_rrrdiag remove`.

Stop immediately if a fixture command is refused, a `DIAG-FAULT` appears, or the game begins a repeating error burst. Return the complete log. No video is required.

