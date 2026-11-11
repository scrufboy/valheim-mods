$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$director = Get-Content -LiteralPath (Join-Path $root 'src\LivingWorldRaidDirector.cs') -Raw
$panel = Get-Content -LiteralPath (Join-Path $root 'src\LivingWorldTrainerPanel.cs') -Raw
$config = Get-Content -LiteralPath (Join-Path $root 'config\custom_raids.supplemental.lwc_reveal_candidates.cfg') -Raw

$prefabs = @(
    'LWC_NekMynBrute', 'LWC_UndeadColossus_Test', 'LWC_UndeadColossus_Unarmed',
    'LWC_NeckMyn_Bowman_Recovery_QA', 'LWC_NekMyn_Spearman', 'RRRN_Neckman',
    'LWC_NekMyn_Shaman', 'Surtling', 'LWC_MarshMarauder_Prototype'
)
foreach ($prefab in $prefabs) {
    if (-not $director.Contains($prefab) -or -not $config.Contains("PrefabName = $prefab")) {
        throw "Reveal candidate identity missing or mismatched: $prefab"
    }
}
if (($config -split '\r?\n' | Where-Object { $_ -match '^Random\s*=\s*false$' }).Count -ne 9) {
    throw 'All nine reveal events must remain manual-only.'
}
if (($config -split '\r?\n' | Where-Object { $_ -match '^HuntPlayer\s*=\s*true$' }).Count -ne 9) {
    throw 'All nine reveal events must use native raid pursuit.'
}
foreach ($required in @('Chainloader.PluginInfos.ContainsKey', 'ZNetScene.instance.GetPrefab', 'RandEventSystem.instance.HaveEvent', 'slots.Count != 1', 'No substitute was spawned', 'SetRandomEventByName', 'requestedPrefab', 'actualPrefab', 'spawnPoint', 'failureReason')) {
    if (-not $director.Contains($required)) { throw "Reveal runtime contract missing: $required" }
}
foreach ($setting in @('raidDistanceMin', 'raidDistanceMax', 'raidApproachSeconds', 'raidCount', 'raidIntervalSeconds', 'raidWaves')) {
    if (-not $panel.Contains($setting)) { throw "Raid calculator setting is not exposed: $setting" }
}

[ordered]@{ Status='PASS'; Candidates=9; ExactRegistrationGuard=$true; NativeRaidScheduler=$true; Telemetry=$true; Installed=$false; GameTested=$false } | ConvertTo-Json
