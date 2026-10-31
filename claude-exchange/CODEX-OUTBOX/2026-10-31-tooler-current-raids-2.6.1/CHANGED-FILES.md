# Changed files

Branch `work/tooler-current-raids-2.6.1`, commits `6b8e2da` and `394e874`:

- `LivingWorldCORETooler/src/LivingWorldTrainerPanel.cs` — current raid, noon and stop buttons; obsolete trial buttons removed.
- `LivingWorldCORETooler/config/custom_raids.supplemental.lwc_formidable_souls.cfg` — exact current long-range raid definitions, now package-owned.
- `LivingWorldCORETooler/Test-FormidableRaidIntegration.ps1` — locks event names, six prefabs, fair `HuntPlayer=false`, random-pool exclusion and 70–95 m ring.
- `LivingWorldCORETooler/Test-InterfaceContract.ps1` — locks the two current Tooler commands.
- `LivingWorldCORETooler/Test-SiteSurveyContracts.ps1` — corrects the 2.6 merged Build-page ownership check without changing Site Survey.
- `LivingWorldCORETooler/Build-LivingWorldCORETooler.ps1` — builds/packages 2.6.1, packages the current raid config and executes the raid contract.
- Version, manifest, README and changelog files — 2.6.1 and current behavior.

No companion, FormidableAI, RRR compatibility, faction, raid-law, voice, movement, telemetry or game DLL source changed.
