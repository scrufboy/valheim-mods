param(
    [string]$Profile = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena"
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$bep = Join-Path $Profile 'BepInEx'
$built = Join-Path $root 'build\LivingWorld.RRRCoreCompat.dll'
$installed = Join-Path $bep 'plugins\LivingWorldRRRCoreCompat\LivingWorld.RRRCoreCompat.dll'
$raid = Join-Path $bep 'config\custom_raids.supplemental.lwc_formidable_souls.cfg'
$natural = Join-Path $bep 'config\Souls Of The Land - Enemy Pack\spawn_that.world_spawners.souls_of_the_wood.cfg'
$spawnThat = Join-Path $bep 'plugins\ASharpPen-Spawn_That\Valheim.SpawnThat.dll'

function RequireFile([string]$path) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing file: $path" }
}
function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}
function Section([string]$text, [string]$name) {
    $match = [regex]::Match($text, '(?ms)^\[' + [regex]::Escape($name) + '\]\s*(.*?)(?=^\[|\z)')
    if (-not $match.Success) { throw "Missing section [$name]" }
    return $match.Groups[1].Value
}
function Value([string]$section, [string]$key) {
    $match = [regex]::Match($section, '(?m)^' + [regex]::Escape($key) + '\s*=\s*(.*?)\s*$')
    if (-not $match.Success) { throw "Missing $key" }
    return $match.Groups[1].Value
}

foreach ($file in @($built,$installed,$raid,$natural,$spawnThat)) { RequireFile $file }
$buildHash = (Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash
Assert ($buildHash -eq $installedHash) 'Installed compatibility DLL differs from the verified build.'

$text = Get-Content -LiteralPath $raid -Raw
$events = @('LWC_Formidable_Souls_Forest','LWC_Formidable_Souls_Swamp')
foreach ($event in $events) {
    $eventSection = Section $text $event
    Assert ((Value $eventSection 'Duration') -eq '120') "$event is not Elder duration."
    Assert ((Value $eventSection 'NearBaseOnly') -eq 'false') "$event cannot run in the open arena."
    for ($i=0; $i -lt 4; ++$i) {
        $spawn = Section $text ($event + '.' + $i)
        Assert ((Value $spawn 'HuntPlayer') -eq 'false') "$event.$i forces Brian as a target."
        Assert ((Value $spawn 'SpawnInterval') -eq '5000') "$event.$i is not Elder paced."
        Assert ((Value $spawn 'SpawnRadiusMin') -eq '70') "$event.$i inner radius changed."
        Assert ((Value $spawn 'SpawnRadiusMax') -eq '95') "$event.$i outer radius changed."
    }
    $ordinary = Section $text ($event + '.0')
    Assert ((Value $ordinary 'MaxSpawned') -eq '6') "$event common cap changed."
    Assert ((Value $ordinary 'GroupSizeMin') -eq '1' -and (Value $ordinary 'GroupSizeMax') -eq '3') "$event common grouping changed."
    $pairs = Section $text ($event + '.1')
    Assert ((Value $pairs 'MaxSpawned') -eq '4') "$event secondary cap changed."
    Assert ((Value $pairs 'GroupSizeMin') -eq '1' -and (Value $pairs 'GroupSizeMax') -eq '3') "$event secondary grouping changed."
    foreach ($i in 2,3) {
        $single = Section $text ($event + '.' + $i)
        Assert ((Value $single 'MaxSpawned') -eq '1') "$event.$i is not one-at-a-time."
        Assert ((Value $single 'GroupSizeMin') -eq '1' -and (Value $single 'GroupSizeMax') -eq '1') "$event.$i is not a single specialist."
        Assert ((Value $single 'SpawnDistance') -eq '10') "$event.$i lost Elder specialist suppression."
    }
    Assert ((Value (Section $text ($event + '.3')) 'PrefabName') -eq 'RRRN_OrcBrute') "$event heavy slot is not the Legionnaire."
}

$naturalText = Get-Content -LiteralPath $natural -Raw
foreach ($prefab in @('RRRN_Pillager','RRRN_OrcBrute','RRRN_WoodsSoul','RRRN_Neckman','RRRN_NinoBosque','RRRN_Servant')) {
    Assert ($naturalText.Contains('PrefabName=' + $prefab)) "Natural spawn config lacks $prefab."
}

Write-Output 'OFFLINE VERIFY PASS'
Write-Output "Compatibility DLL: $installedHash"
Write-Output 'Raid events: lwc_formidable_arena, lwc_formidable_souls_swamp'
Write-Output 'Natural Souls roster: 6/6 configured through Spawn That'

