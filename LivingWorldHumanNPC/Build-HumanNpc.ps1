param(
    [string]$SharedProjectRoot='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage',
    [string]$ManagedDirectory='D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$Python='C:\Users\scruf\AppData\Local\Programs\Python\Python312\python.exe',
    [string]$UnityPyDirectory='C:\Users\scruf\Documents\Codex\2026-10-13\begin-now-dedicated-independent-valheim-living\work\human-npc\assets\python-libs',
    [string]$NativeBundlesDirectory=(Join-Path (Split-Path $ManagedDirectory -Parent) 'StreamingAssets\SoftRef\Bundles')
)
# Offline-only build. Native/runtime paths are read-only references. All products
# stay below this candidate's build directory; no game/profile installation occurs.
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Core'){
    throw 'Run Build-HumanNpc.ps1 with PowerShell 7/CoreCLR. Native container metadata cannot be loaded by Windows PowerShell/.NET Framework.'
}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$build=Join-Path $PSScriptRoot 'build'
$null=New-Item -ItemType Directory -Force -Path $build
[ordered]@{Version='0.3.68';Status='INCOMPLETE';Reason='Current build has not completed all checks';UnityRuntime='NOT RUN';ProfileInstalled=$false;GameLaunched=$false} |
    ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding utf8
$nativeBuild=Join-Path $SharedProjectRoot 'independent-core\build'
$runtimeCore=Join-Path $SharedProjectRoot 'runtime\BepInEx\core'
$cecil=Join-Path $SharedProjectRoot 'independent-core\tools\Mono.Cecil.dll'
$nativeAssembly=Join-Path $ManagedDirectory 'assembly_valheim.dll'
$suiteResults=[System.Collections.Generic.List[object]]::new()

function CheckOutput([string]$name,[object[]]$lines){
    $text=($lines | ForEach-Object ToString) -join "`n"
    Write-Output $text
    if($text -notmatch '\bPASS\b'){throw "$name did not report PASS."}
    $suiteResults.Add([ordered]@{Name=$name;Status='PASS';Output=$text})
}

# Explicit allowlist: no linked upstream implementation, rejected creature-root
# code, or legacy graph/probe helper can enter this binary by recursive discovery.
$relativeSources=@(
    'src\ManualAcceptance.cs',
    'src\ManualTestPlugin.cs',
    'src\ManualLabBuilder.cs',
    'src\HumanRigPolicy.cs',
    'src\NativeHumanFactory.cs',
    'src\HumanNpcSettings.cs',
    'src\HumanAppearance.cs',
    'src\HumanCompositionIdentity.cs',
    'src\HumanBehaviorInstaller.cs',
    'src\CompanionPolicy.cs',
    'src\HumanCompanion.cs',
    'src\CompanionPresencePolicy.cs',
    'src\HumanFollowPresence.cs',
    'src\HumanAwarenessHud.cs',
    'src\CompanionPosturePolicy.cs',
    'src\HumanCompanionPosture.cs',
    'src\HumanResourcePool.cs',
    'src\HumanResources.cs',
    'src\HumanEnvironmentStatus.cs',
    'src\HumanProfile.cs',
    'src\HumanIdentity.cs',
    'src\ConfidencePolicy.cs',
    'src\HumanPartyConfidence.cs',
    'src\CompanionActivities.cs',
    'src\GuardCoveragePolicy.cs',
    'src\HumanCompanionAmbient.cs',
    'src\HumanEquipmentReadiness.cs',
    'src\HumanCombatDefense.cs',
    'src\EquipmentPresentationPolicy.cs',
    'src\AmbientAnimationContract.cs',
    'src\DoorTravelPolicy.cs',
    'src\HumanDoorTraversal.cs',
    'src\PartyCombatPolicy.cs',
    'src\HumanPartyEquipment.cs',
    'src\HumanCleric.cs',
    'src\ClericPresentation.cs',
    'src\HumanUtilityTorch.cs',
    'src\ManualPartyLab.cs',
    'migration\HumanMigrationPolicy.cs'
)
$sources=@($relativeSources | ForEach-Object {Join-Path $PSScriptRoot $_})
foreach($source in $sources){if(-not (Test-Path -LiteralPath $source -PathType Leaf)){throw "Allowlisted source missing: $source"}}
$actualSources=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File | ForEach-Object FullName)
if(@($actualSources | Where-Object {$sources -notcontains $_}).Count){throw 'Unreviewed C# source found outside build allowlist.'}
foreach($source in $sources){
    $content=Get-Content -LiteralPath $source -Raw
    if($content -match '(?i)\bdvergr\b|\bdverger\b|\bNativeNpcProbe\b|using\s+LivingWorld\.(Core|Valheim)\s*;'){
        throw "Rejected template or old implementation dependency in active source: $source"
    }
}
$pluginSource=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\ManualTestPlugin.cs') -Raw
if($pluginSource -notmatch '\[BepInPlugin\("local\.livingworld\.humannpc",\s*"[^"]+",\s*"0\.3\.68"\)\]'){
    throw 'Expected new 0.3.68 human NPC plugin declaration; never reuse frozen version.'
}
$sourceManifest=@($sources | ForEach-Object {
    [ordered]@{Path=$_;RelativePath=[IO.Path]::GetRelativePath($PSScriptRoot,$_).Replace('\','/');Sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}
})
CheckOutput SourceAllowlist @("SourceAllowlist PASS: $($sources.Count) explicit fresh-composition sources; no old template/probe/core source links.")
CheckOutput HumanRigHandoff @(& (Join-Path $PSScriptRoot 'contracts\Export-HumanRigHandoff.ps1'))

CheckOutput FreshHumanLifecycle @(& (Join-Path $PSScriptRoot 'contracts\Test-FreshHumanLifecycle.ps1') -AssemblyPath $nativeAssembly -CecilPath $cecil)
CheckOutput HumanMigrationContracts @(& (Join-Path $PSScriptRoot 'contracts\Test-HumanMigrationContracts.ps1') -AssemblyPath $nativeAssembly -CecilPath $cecil)
CheckOutput JotunnRegistrationLifecycle @(& (Join-Path $PSScriptRoot 'contracts\Test-JotunnRegistrationLifecycle.ps1') -JotunnPath (Join-Path $nativeBuild 'Jotunn.dll') -CecilPath $cecil)
$scriptOrderOutput=@(& $Python (Join-Path $PSScriptRoot 'contracts\inspect_script_order.py') --unitypy-dir $UnityPyDirectory --bundles-dir $NativeBundlesDirectory)
if($LASTEXITCODE -ne 0){throw 'Native MonoScript order check failed.'}
CheckOutput NativeScriptOrder $scriptOrderOutput
$nativeSwordOutput=@(& $Python (Join-Path $PSScriptRoot 'contracts\inspect_human_sword.py') --unitypy-dir $UnityPyDirectory --bundles-dir $NativeBundlesDirectory)
if($LASTEXITCODE -ne 0){throw 'Native human sword asset check failed.'}
CheckOutput NativeHumanSword $nativeSwordOutput
$nativeAmbientOutput=@(& $Python (Join-Path $PSScriptRoot 'tests\inspect_ambient_assets.py') --unitypy-dir $UnityPyDirectory --bundles-dir $NativeBundlesDirectory)
if($LASTEXITCODE -ne 0){throw 'Native ambient animation asset check failed.'}
CheckOutput NativeAmbientAssets $nativeAmbientOutput
$releaseOutput=@(& (Join-Path $PSScriptRoot 'tests\Test-ReleaseGate.ps1'))
if(($releaseOutput -join "`n") -notmatch 'Release gate: (\d+) fixtures passed\.') {throw 'Release gate fixture suite did not finish successfully.'}
CheckOutput ReleaseGate (@("ReleaseGate PASS: $($Matches[1]) archive fixtures.") + $releaseOutput)

$pureSuites=@(
    @{Name='ManualAcceptance';Source='src\ManualAcceptance.cs'},
    @{Name='HumanRigPolicy';Source='src\HumanRigPolicy.cs'},
    @{Name='CompanionPolicy';Source='src\CompanionPolicy.cs'},
    @{Name='CompanionPresencePolicy';Source='src\CompanionPresencePolicy.cs'},
    @{Name='CompanionPosturePolicy';Source='src\CompanionPosturePolicy.cs'},
    @{Name='HumanResourcePool';Source='src\HumanResourcePool.cs'},
    @{Name='HumanProfile';Source='src\HumanProfile.cs'},
    @{Name='ConfidencePolicy';Source='src\ConfidencePolicy.cs'},
    @{Name='CompanionActivities';Source='src\CompanionActivities.cs';Test='tests\ActivityTests.cs'},
    @{Name='GuardCoveragePolicy';Source='src\GuardCoveragePolicy.cs'},
    @{Name='DoorTravelPolicy';Source='src\DoorTravelPolicy.cs'},
    @{Name='PartyCombatPolicy';Source='src\PartyCombatPolicy.cs'},
    @{Name='EquipmentPresentationPolicy';Source='src\EquipmentPresentationPolicy.cs'},
    @{Name='HumanMigrationPolicy';Source='migration\HumanMigrationPolicy.cs'}
)
foreach($suite in $pureSuites){
    $test=Join-Path $build ($suite.Name+'Tests.exe')
    $testSource=if($suite.ContainsKey('Test')){$suite.Test}else{'tests\'+$suite.Name+'Tests.cs'}
    & $compiler /nologo /optimize+ /target:exe "/out:$test" (Join-Path $PSScriptRoot $suite.Source) (Join-Path $PSScriptRoot $testSource)
    if($LASTEXITCODE -ne 0){throw "$($suite.Name) test compilation failed."}
    $testOutput=@(& $test)
    if($LASTEXITCODE -ne 0){throw "$($suite.Name) checks failed."}
    CheckOutput $suite.Name $testOutput
}
$independentTest=Join-Path $build 'AcceptanceRegressionTests.exe'
$resourceTest=Join-Path $build 'ResourceAdapterHarness.exe'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$resourceTest" (Join-Path $PSScriptRoot 'src\HumanResourcePool.cs') (Join-Path $PSScriptRoot 'src\HumanResources.cs') (Join-Path $PSScriptRoot 'tests\ResourceAdapterHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Resource adapter harness compilation failed.'}
$resourceOutput=@(& $resourceTest)
if($LASTEXITCODE -ne 0){throw 'Resource adapter harness failed.'}
CheckOutput ResourceAdapterHarness $resourceOutput
$confidenceTest=Join-Path $build 'ConfidenceAdapterHarness.exe'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$confidenceTest" (Join-Path $PSScriptRoot 'src\ConfidencePolicy.cs') (Join-Path $PSScriptRoot 'src\HumanPartyConfidence.cs') (Join-Path $PSScriptRoot 'src\CompanionPolicy.cs') (Join-Path $PSScriptRoot 'tests\ConfidenceAdapterHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Confidence adapter harness compilation failed.'}
$confidenceOutput=@(& $confidenceTest)
if($LASTEXITCODE -ne 0){throw 'Confidence adapter harness failed.'}
CheckOutput ConfidenceAdapterHarness $confidenceOutput
$identityTest=Join-Path $build 'IdentityAdapterHarness.exe'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$identityTest" (Join-Path $PSScriptRoot 'src\HumanProfile.cs') (Join-Path $PSScriptRoot 'src\HumanIdentity.cs') (Join-Path $PSScriptRoot 'src\HumanAppearance.cs') (Join-Path $PSScriptRoot 'src\HumanRigPolicy.cs') (Join-Path $PSScriptRoot 'migration\HumanMigrationPolicy.cs') (Join-Path $PSScriptRoot 'tests\IdentityAdapterHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Identity adapter harness compilation failed.'}
$identityOutput=@(& $identityTest)
if($LASTEXITCODE -ne 0){throw 'Identity adapter harness failed.'}
CheckOutput IdentityAdapterHarness $identityOutput
$installerTest=Join-Path $build 'BehaviorInstallerHarness.exe'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$installerTest" (Join-Path $PSScriptRoot 'src\HumanBehaviorInstaller.cs') (Join-Path $PSScriptRoot 'tests\BehaviorInstallerHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Behavior installer harness compilation failed.'}
$installerOutput=@(& $installerTest)
if($LASTEXITCODE -ne 0){throw 'Behavior installer harness failed.'}
CheckOutput BehaviorInstallerHarness $installerOutput
CheckOutput FreshSpawnTransaction @(& (Join-Path $PSScriptRoot 'tests\Test-FreshSpawnTransaction.ps1'))
CheckOutput PartySpawnTransaction @(& (Join-Path $PSScriptRoot 'tests\Test-PartySpawnTransaction.ps1'))
$ambientHarnessSources=@(
    'src\CompanionActivities.cs', 'src\CompanionPresencePolicy.cs',
    'src\CompanionPosturePolicy.cs', 'src\AmbientAnimationContract.cs',
    'src\GuardCoveragePolicy.cs',
    'src\HumanCompanionAmbient.cs', 'src\HumanFollowPresence.cs',
    'src\HumanCompanionPosture.cs', 'tests\PresenceAdapterHarness.cs',
    'tests\AmbientAdapterDoubles.cs', 'tests\AmbientAdapterHarness.cs'
) | ForEach-Object {Join-Path $PSScriptRoot $_}
foreach($harness in @('PresenceAdapterHarness','AmbientAdapterHarness')){
    $adapterTest=Join-Path $build ($harness+'.exe')
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/main:$harness" "/out:$adapterTest" $ambientHarnessSources
    if($LASTEXITCODE -ne 0){throw "$harness compilation failed."}
    $adapterOutput=@(& $adapterTest)
    if($LASTEXITCODE -ne 0){throw "$harness failed."}
    CheckOutput $harness $adapterOutput
}
& $compiler /nologo /optimize+ /target:exe "/out:$independentTest" (Join-Path $PSScriptRoot 'src\CompanionPolicy.cs') (Join-Path $PSScriptRoot 'migration\HumanMigrationPolicy.cs') (Join-Path $PSScriptRoot 'tests\AcceptanceRegressionTests.cs')
if($LASTEXITCODE -ne 0){throw 'Independent acceptance regression compilation failed.'}
$independentOutput=@(& $independentTest)
if($LASTEXITCODE -ne 0){throw 'Independent acceptance regression checks failed.'}
if(($independentOutput -join "`n") -notmatch 'Independent acceptance: (\d+) passed; 0 failed\.') {throw 'Independent acceptance suite did not finish without failures.'}
CheckOutput IndependentAcceptance (@("IndependentAcceptance PASS: $($Matches[1]) product checks.") + $independentOutput)

$referencePaths=@(
    (Join-Path $nativeBuild 'BepInEx.dll'),
    (Join-Path $nativeBuild 'Jotunn.dll'),
    (Join-Path $runtimeCore '0Harmony.dll')
)
$referencePaths+=@('netstandard','assembly_valheim','assembly_utils','Splatform','SoftReferenceableAssets','MagicaClothV2','UnityEngine','UnityEngine.CoreModule','UnityEngine.AnimationModule','UnityEngine.TerrainPhysicsModule','UnityEngine.PhysicsModule','UnityEngine.ParticleSystemModule','UnityEngine.AudioModule') | ForEach-Object {Join-Path $ManagedDirectory ($_+'.dll')}
foreach($referencePath in $referencePaths){if(-not (Test-Path -LiteralPath $referencePath -PathType Leaf)){throw "Read-only reference missing: $referencePath"}}
$references=@($referencePaths | ForEach-Object {"/reference:$_"})
$dll=Join-Path $build 'LivingWorld.HumanNpc.dll'
& $compiler /nologo /langversion:5 /warn:4 /optimize+ /target:library "/out:$dll" $references $sources
if($LASTEXITCODE -ne 0){throw 'Fresh human NPC compilation failed.'}

$containerTest=Join-Path $build 'HumanContainerCloneTests.exe'
& $compiler /nologo /optimize+ /target:exe "/out:$containerTest" $references (Join-Path $PSScriptRoot 'tests\HumanContainerCloneTests.cs')
if($LASTEXITCODE -ne 0){throw 'Human container test compilation failed.'}
# Capture managed Console output without rerunning or pretending to execute any
# Unity scene. The existing CoreCLR-only runner rejects a nonzero test return.
$priorConsoleOut=[Console]::Out
$containerCapture=[IO.StringWriter]::new()
try {
    [Console]::SetOut($containerCapture)
    & (Join-Path $PSScriptRoot 'contracts\Run-HumanContainerCloneTests.ps1') -TestExecutable $containerTest -HumanDll $dll -ManagedDirectory $ManagedDirectory -DependencyDirectories @($nativeBuild,$runtimeCore)
} finally {[Console]::SetOut($priorConsoleOut)}
CheckOutput HumanContainerClone @($containerCapture.ToString())
$containerCapture.Dispose()
CheckOutput HumanCompositionBinary @(& (Join-Path $PSScriptRoot 'tests\Verify-HumanCompositionBinary.ps1') -AssemblyPath $dll -CecilPath $cecil)
CheckOutput PartyIntegration @(& (Join-Path $PSScriptRoot 'tests\Verify-PartyIntegration.ps1') -AssemblyPath $dll -NativeAssemblyPath $nativeAssembly -CecilPath $cecil)
CheckOutput ResourceIntegration @(& (Join-Path $PSScriptRoot 'contracts\Test-ResourceIntegration.ps1') -AssemblyPath $dll -NativeAssemblyPath $nativeAssembly -CecilPath $cecil)
CheckOutput ConfidenceIntegration @(& (Join-Path $PSScriptRoot 'tests\Verify-ConfidenceIntegration.ps1') -AssemblyPath $dll -CecilPath $cecil)
CheckOutput HumanCompanionBinary @(& (Join-Path $PSScriptRoot 'tests\Verify-HumanCompanionBinary.ps1') -AssemblyPath $dll -CecilPath $cecil)
CheckOutput DoorIntegration @(& (Join-Path $PSScriptRoot 'tests\Verify-DoorIntegration.ps1') -AssemblyPath $dll -NativeAssemblyPath $nativeAssembly -CecilPath $cecil -BaselinePath (Join-Path $SharedProjectRoot 'human-npc-0.3.0-native\build\LivingWorld.HumanNpc.dll'))
CheckOutput PresenceIntegration @(& (Join-Path $PSScriptRoot 'tests\Verify-PresenceIntegration.ps1') -AssemblyPath $dll -NativeAssemblyPath $nativeAssembly -CecilPath $cecil -BaselinePath (Join-Path $SharedProjectRoot 'human-npc-0.3.2-navigation\build\LivingWorld.HumanNpc.dll'))
CheckOutput AmbientIntegration @(& (Join-Path $PSScriptRoot 'tests\Verify-AmbientIntegration.ps1') -AssemblyPath $dll -NativeAssemblyPath $nativeAssembly -CecilPath $cecil -BaselinePath (Join-Path $SharedProjectRoot 'human-npc-0.3.3-companion\build\LivingWorld.HumanNpc.dll'))

foreach($entry in $sourceManifest){
    if((Get-FileHash -LiteralPath $entry.Path -Algorithm SHA256).Hash -ne $entry.Sha256){
        throw "Source changed during build/checks; rerun after edits finish: $($entry.Path)"
    }
}
$sourceManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $build 'source-manifest.json') -Encoding utf8
$referenceManifest=@($referencePaths | ForEach-Object {[ordered]@{Path=$_;Sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}})
$referenceManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $build 'reference-manifest.json') -Encoding utf8
$dllHash=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
[ordered]@{
    Version='0.3.68'; Status='PASS'; DllPath=$dll; DllSha256=$dllHash;
    Scope='Offline compilation, pure managed policies, container isolation and read-only native IL/asset checks only';
    UnityRuntime='NOT RUN'; ProfileInstalled=$false; GameLaunched=$false;
    Sources=$sourceManifest; Suites=@($suiteResults.ToArray())
} | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding utf8
Write-Output "HumanNpc 0.3.68 offline build PASS. DLL SHA256: $dllHash"
Write-Output 'Unity load, migration, animation, movement/combat and save/reload NOT RUN. No game or live profile touched.'
