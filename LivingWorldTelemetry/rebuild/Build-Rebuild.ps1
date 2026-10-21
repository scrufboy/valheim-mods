param(
    [string]$ManagedDirectory='D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$ReferenceRoot='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage',
    [string]$HumanNpcAssembly=(Join-Path $PSScriptRoot '..\..\LivingWorldHumanNPC\rebuild\build\LivingWorld.HumanNpc.dll')
)
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source=Join-Path $PSScriptRoot 'TelemetryV2.cs'
$build=Join-Path $PSScriptRoot 'build'
$null=New-Item -ItemType Directory -Force -Path $build
$nativeBuild=Join-Path $ReferenceRoot 'independent-core\build'
$references=@(
    (Join-Path $nativeBuild 'BepInEx.dll'),
    $HumanNpcAssembly
)
$references+=@('netstandard','assembly_valheim','assembly_utils','Splatform','SoftReferenceableAssets','MagicaClothV2','UnityEngine','UnityEngine.CoreModule','UnityEngine.PhysicsModule','UnityEngine.TerrainPhysicsModule') |
    ForEach-Object {Join-Path $ManagedDirectory ($_+'.dll')}
foreach($path in @($source)+$references){if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing telemetry rebuild input: $path"}}

$text=Get-Content -LiteralPath $source -Raw
if($text -match 'Harmony|LivingWorld\.HumanNpc;' -or $text -notmatch 'HumanCompanionV2'){
    throw 'Telemetry must observe the 0.5 boundary without patching gameplay methods.'
}
$dll=Join-Path $build 'LivingWorld.Telemetry.dll'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:library "/out:$dll" ($references|ForEach-Object{"/reference:$_"}) $source
if($LASTEXITCODE -ne 0){throw 'Living World Telemetry 0.6.0 compilation failed.'}

$manifest=[ordered]@{
    Version='0.6.0';Status='PASS';Dll=$dll;
    Sha256=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash;
    HumanNpcSha256=(Get-FileHash -LiteralPath $HumanNpcAssembly -Algorithm SHA256).Hash;
    Contracts=@(
        'No Harmony or gameplay-method patches',
        'Observes only HumanCompanionV2 actors',
        'Records identity, exclusive behavior state, alert-versus-target evidence, formation frame, traversal, equipment posture, settled guard phase/sector, combat-boundary releases, health and position',
        'Records the leader position, velocity, travel speed, facing, posture, visible hands and current attack item every sample',
        'Bounded local JSONL output with automatic world lifecycle'
    );Installed=$false
}
$manifest|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding utf8
Write-Output "Telemetry 0.6.0 rebuild PASS. DLL SHA256: $($manifest.Sha256)"
