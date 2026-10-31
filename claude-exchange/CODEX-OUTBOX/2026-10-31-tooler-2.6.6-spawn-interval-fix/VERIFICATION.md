# Verification

## Live evidence

- The current Arena log recorded `Random event set:army_theelder` followed by repeated `Spawned Greydwarf` / `Spawned Greyling` lines.
- It later recorded `Random event set:lwc_formidable_arena` and zero `RRRN_*` spawn lines.
- Startup recorded `SOULS-PREFAB SUMMARY 6/6 ready`.

## Configuration defect

- Before: eight occurrences of `SpawnInterval = 5000`.
- After: eight occurrences of `SpawnInterval = 5`.
- Event duration remains 120 seconds.

## Offline gates

- `Test-FormidableRaidIntegration.ps1`: PASS, Tooler 2.6.6, eight five-second intervals, full roster, HuntPlayer true, 70–95 m ring.
- `Build-LivingWorldCORETooler.ps1`: PASS, including interface and Site Survey contracts.
- Runtime: not run and not installed at this handoff.
