$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$director = Get-Content -LiteralPath (Join-Path $root 'src\LivingWorldRaidDirector.cs') -Raw
$showcase = Get-Content -LiteralPath (Join-Path $root 'src\LivingWorldShowcaseDirector.cs') -Raw
$plugin = Get-Content -LiteralPath (Join-Path $root 'src\LivingWorldToolerExtension.cs') -Raw
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
foreach ($required in @('LivingWorldShowcaseMarker', 'FindSpawnPoint', 'no-nearby-spawnable-area', 'cohort_queued', 'nativeEvent', 'streamDeckAction', 'reinforcement_recall', 'showcase-stop', 'LWC_ShowcaseCompanion_Test', 'ZNetScene.instance.Destroy')) {
    if (-not $showcase.Contains($required)) { throw "Showcase Director contract missing: $required" }
}
foreach ($required in @('KeyboardShortcut', 'Debounce seconds', 'No HUD mode', 'streamdeck:cohort:', 'streamdeck:stop', 'streamdeck:next-wave', 'streamdeck:reinforce', 'streamdeck:recall')) {
    if (-not $plugin.Contains($required)) { throw "Headless Stream Deck contract missing: $required" }
}
if ($showcase.Contains('SetFaction(')) { throw 'Showcase Director must never rewrite creature factions.' }
if ($showcase.Contains('SetRandomEventByName')) { throw 'Showcase cohorts must not consume or fake a second native raid slot.' }

[ordered]@{ Status='PASS'; Candidates=9; ExactRegistrationGuard=$true; NativeRaidScheduler=$true; ShowcaseDirector=$true; HeadlessHotkeys=$true; Debounce=$true; DedicatedTestCompanionGate=$true; Telemetry=$true; Installed=$false; GameTested=$false } | ConvertTo-Json
