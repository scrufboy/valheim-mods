$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$panel = Get-Content -LiteralPath (Join-Path $root 'src\LivingWorldTrainerPanel.cs') -Raw
$configPath = Join-Path $root 'config\custom_raids.supplemental.lwc_formidable_trials.cfg'
$config = Get-Content -LiteralPath $configPath -Raw
$manifest = Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json

function Assert-Contains([string]$Text, [string]$Expected, [string]$Message) {
    if (-not $Text.Contains($Expected)) {
        throw $Message
    }
}

Assert-Contains $panel 'event lwc_souls_pillagers' 'The Tooler is missing the pillager RandomEvent button.'
Assert-Contains $panel 'event lwc_krauguk_legion' 'The Tooler is missing the legion RandomEvent button.'
Assert-Contains $config 'Name = lwc_souls_pillagers' 'The pillager RandomEvent is not configured.'
Assert-Contains $config 'PrefabName = RRRN_Pillager' 'The pillager event does not use the verified Souls prefab.'
Assert-Contains $config 'Name = lwc_krauguk_legion' 'The legion RandomEvent is not configured.'
Assert-Contains $config 'PrefabName = RRRN_OrcBrute' 'The legion event does not use the verified Souls prefab.'

if (($config -split '\r?\n' | Where-Object { $_ -match '^Random\s*=\s*false$' }).Count -ne 2) {
    throw 'Both formidable trials must be excluded from the random raid pool.'
}
if (($config -split '\r?\n' | Where-Object { $_ -match '^HuntPlayer\s*=\s*true$' }).Count -ne 2) {
    throw 'Both formidable trials must commit to the player/party.'
}
if ($config -notmatch '(?s)\[LWC_Souls_Pillagers\.0\].*?MaxSpawned\s*=\s*3.*?GroupSizeMin\s*=\s*2.*?GroupSizeMax\s*=\s*3') {
    throw 'The pillager matchup is not bounded to two-to-three combatants.'
}
if ($config -notmatch '(?s)\[LWC_Krauguk_Legion\.0\].*?MaxSpawned\s*=\s*2.*?GroupSizeMin\s*=\s*2.*?GroupSizeMax\s*=\s*2') {
    throw 'The legion matchup is not bounded to two combatants.'
}
if ($manifest.version_number -ne '2.4.0') {
    throw 'Unexpected Tooler package version.'
}
if ($manifest.dependencies -notcontains 'ASharpPen-Custom_Raids-1.8.2') {
    throw 'Custom Raids is not declared as a package dependency.'
}

[ordered]@{
    Status = 'PASS'
    PackageVersion = $manifest.version_number
    Events = @('lwc_souls_pillagers', 'lwc_krauguk_legion')
    RandomPool = $false
    PillagerGroup = '2-3'
    LegionGroup = '2'
    Config = $configPath
} | ConvertTo-Json -Depth 3
