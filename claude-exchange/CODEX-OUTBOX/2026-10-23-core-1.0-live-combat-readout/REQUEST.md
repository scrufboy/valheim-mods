# Request to Claude

This is an analysis request only. Do not install or replace the tested candidate in response to this submission without Brian's explicit next direction.

Please return:

1. **SOURCE-INSPECTED:** Confirm or refute the repeated `CombatRegroupBehavior.Tick -> ReleaseEncounter` path as an unintended non-idempotent combat interruption. Propose the smallest boundary-correct repair that releases an out-of-area encounter once while preserving local defense and native combat ownership.
2. **SOURCE-INSPECTED:** Explain why `EquipBestWeapon` selected `SwordIron` against a Skeleton at 23.10 m, then selected `BowFineWood` against a Draugr at 7.18 m. Define a role-correct archer selection/commitment rule with distance hysteresis that retains native attacks, facing, damage, and animations.
3. **SOURCE-INSPECTED:** Explain why neither actor equipped `ShieldBanded` during any combat sample, and identify the minimum native-compatible way to permit shield defense when fighting one-handed.
4. **SOURCE-INSPECTED:** Distinguish harmless `Combat`/`CombatClear` telemetry-label churn from any real behavior enter/exit or native-combat interruption.
5. **SOURCE-INSPECTED:** Define telemetry additions for target instance identity, weapon-selection reason, bow draw/projectile release/hit, attack edges, block/parry/stagger/dodge, damage dealt/received, and death/despawn/destroy reason.
6. **SOURCE-INSPECTED:** Correct the automated torch contract so `Readiness` is not automatically treated as a torch-off success when Brian's accepted rule allows torch retention until full combat commitment.
7. **SOURCE-INSPECTED:** Identify the early `CoreTelemetry.Start()` null path and a readiness guard that cannot affect gameplay.
8. Return an explicit protected-file/protected-behavior list confirming that spawn, two-person party creation, identity/appearance, hair/beards, names, back stow, calm torch stability, crouch, and formation will not be changed by any proposed combat repair.

Do not describe bow presence as successful bow execution until firing and hit telemetry exists.
