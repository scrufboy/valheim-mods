$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$panel = Get-Content -LiteralPath (Join-Path $root 'src\LivingWorldTrainerPanel.cs') -Raw
$configPath = Join-Path $root 'config\custom_raids.supplemental.lwc_formidable_souls.cfg'
$config = Get-Content -LiteralPath $configPath -Raw
$manifest = Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json

function Assert-Contains([string]$Text, [string]$Expected, [string]$Message) {
    if (-not $Text.Contains($Expected)) {
        throw $Message
    }
}

Assert-Contains $panel 'event lwc_formidable_arena' 'The Tooler is missing the current Formidable Arena button.'
Assert-Contains $panel 'event lwc_formidable_souls_swamp' 'The Tooler is missing the current Formidable Souls Swamp button.'
Assert-Contains $config 'Name = lwc_formidable_arena' 'The Formidable Arena event is not configured.'
Assert-Contains $config 'Name = lwc_formidable_souls_swamp' 'The Formidable Souls Swamp event is not configured.'
foreach ($prefab in @('RRRN_Pillager', 'RRRN_WoodsSoul', 'RRRN_NinoBosque', 'RRRN_Neckman', 'RRRN_Servant', 'RRRN_OrcBrute')) {
    Assert-Contains $config ("PrefabName = " + $prefab) ("The current raid definitions are missing " + $prefab + ".")
}

if (($config -split '\r?\n' | Where-Object { $_ -match '^Random\s*=\s*false$' }).Count -ne 2) {
    throw 'Both formidable trials must be excluded from the random raid pool.'
}
if (($config -split '\r?\n' | Where-Object { $_ -match '^HuntPlayer\s*=\s*true$' }).Count -ne 8 -or
    ($config -split '\r?\n' | Where-Object { $_ -match '^HuntPlayer\s*=\s*false$' }).Count -ne 0) {
    throw 'Every current FormidableAI raid slot must perform normal hostile raid pursuit.'
}
if (($config -split '\r?\n' | Where-Object { $_ -match '^SpawnRadiusMin\s*=\s*70$' }).Count -ne 8 -or
    ($config -split '\r?\n' | Where-Object { $_ -match '^SpawnRadiusMax\s*=\s*95$' }).Count -ne 8) {
    throw 'Every current FormidableAI raid slot must use the 70-95 m initiative ring.'
}
if ($manifest.version_number -ne '2.6.2') {
    throw 'Unexpected Tooler package version.'
}
if ($manifest.dependencies -notcontains 'ASharpPen-Custom_Raids-1.8.2') {
    throw 'Custom Raids is not declared as a package dependency.'
}

[ordered]@{
    Status = 'PASS'
    PackageVersion = $manifest.version_number
    Events = @('lwc_formidable_arena', 'lwc_formidable_souls_swamp')
    RandomPool = $false
    HuntPlayer = $true
    SpawnRing = '70-95 m'
    Config = $configPath
} | ConvertTo-Json -Depth 3
