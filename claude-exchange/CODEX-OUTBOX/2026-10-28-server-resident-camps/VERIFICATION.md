# Verification: server-resident camp correction

- SOURCE-INSPECTED: Jotunn zone documentation confirms persistent network objects are instantiated on zone load; it does not make every NPC's AI continuously active merely because a dedicated server process runs.
- OFFLINE-TESTED: `CampLifecycle/Test-CampLifecycle.ps1` passes 61 checks, including selected no-player residency and failure gates.
- OFFLINE-TESTED: `RuntimeIntegration/Run-Tests.ps1` passes 36 checks after the policy extension.
- UNKNOWN: The maintained core has not been updated with a zone-retention adapter, away-safety enforcement, or an installed DLL for this change.
- UNKNOWN: No zero-player dedicated-server session or one/two-player return test has been run for this change.
