param(
    [string]$ManagedDirectory='D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$ReferenceRoot='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage'
)
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=Split-Path $PSScriptRoot -Parent
$build=Join-Path $PSScriptRoot 'build'
$null=New-Item -ItemType Directory -Force -Path $build
$sources=@(
    'HumanIdentityRecord.cs',
    'CompanionStateLatch.cs',
    'NativeHumanFactoryV2.cs',
    'HumanAppearanceV2.cs',
    'HumanCompanionV2.cs',
    'HumanNpcPlugin.cs'
) | ForEach-Object { Join-Path $PSScriptRoot $_ }
foreach($source in $sources){if(-not (Test-Path -LiteralPath $source -PathType Leaf)){throw "Missing rebuild source: $source"}}
$contractTest=Join-Path $PSScriptRoot 'FoundationContractTests.cs'
if(-not(Test-Path -LiteralPath $contractTest -PathType Leaf)){throw "Missing rebuild contract test: $contractTest"}

$allText=($sources | ForEach-Object {Get-Content -LiteralPath $_ -Raw}) -join "`n"
if($allText -match '(?i)RRRCore|HarmonyPatch|HumanCompanion\b|HumanAppearance\b|ManualTestPlugin'){
    throw 'The rebuild source set contains an old implementation or external combat dependency.'
}
if($allText -notmatch 'CopySerializedHumanState\(player, human\)' -or
   $allText -notmatch 'GetTargetCreature\(\)' -or
   $allText -notmatch 'GetAllZDOsWithPrefabIterative'){
    throw 'The rebuild is missing its native-copy, native-combat, or global-cleanup boundary.'
}
if($allText -match 'Items\([^\)]*"HelmetIron"'){
    throw 'The visible-identity acceptance loadout cannot contain a hair-hiding iron helmet.'
}

$nativeBuild=Join-Path $ReferenceRoot 'independent-core\build'
$runtimeCore=Join-Path $ReferenceRoot 'runtime\BepInEx\core'
$referencePaths=@(
    (Join-Path $nativeBuild 'BepInEx.dll'),
    (Join-Path $nativeBuild 'Jotunn.dll')
)
$referencePaths+=@('netstandard','assembly_valheim','assembly_utils','Splatform','SoftReferenceableAssets','MagicaClothV2','UnityEngine','UnityEngine.CoreModule','UnityEngine.AnimationModule','UnityEngine.TerrainPhysicsModule','UnityEngine.PhysicsModule','UnityEngine.AudioModule') |
    ForEach-Object {Join-Path $ManagedDirectory ($_+'.dll')}
foreach($reference in $referencePaths){if(-not (Test-Path -LiteralPath $reference -PathType Leaf)){throw "Missing read-only reference: $reference"}}
$references=@($referencePaths | ForEach-Object {"/reference:$_"})
$dll=Join-Path $build 'LivingWorld.HumanNpc.dll'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:library "/out:$dll" $references $sources
if($LASTEXITCODE -ne 0){throw 'Living World Human NPC 0.4.1 compilation failed.'}

$testExe=Join-Path $build 'FoundationContractTests.exe'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$testExe" `
    (Join-Path $PSScriptRoot 'HumanIdentityRecord.cs') (Join-Path $PSScriptRoot 'CompanionStateLatch.cs') $contractTest
if($LASTEXITCODE -ne 0){throw 'Foundation contract test compilation failed.'}
& $testExe
if($LASTEXITCODE -ne 0){throw 'Foundation contract tests failed.'}

$manifest=[ordered]@{
    Version='0.4.1'
    Status='PASS'
    Dll=$dll
    Sha256=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    Sources=@($sources | ForEach-Object {[ordered]@{Path=$_;Sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}})
    Contracts=@(
        'Complete serialized Humanoid/Character state copied before Player removal',
        'No RRRCore or Harmony combat dependency',
        'Native MonsterAI retains target, facing, pursuit and attack ownership',
        'lwparty globally removes legacy/current ZDO records before exactly two spawns',
        'Hair is mandatory; every male receives a native beard; females receive none',
        'Acceptance loadouts contain no helmet that can suppress visible facial hair'
    )
    LiveUnity='NOT RUN'
    Installed=$false
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding utf8
Write-Output "HumanNpc 0.4.1 rebuild PASS. DLL SHA256: $($manifest.Sha256)"
Write-Output 'Live Unity appearance, party replacement, follow, stow and combat remain to be tested.'
