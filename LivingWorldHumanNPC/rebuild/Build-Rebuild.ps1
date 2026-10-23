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
    'CombatCohesionLatch.cs',
    'HumanBehaviorStateMachine.cs',
    'NativeHumanFactoryV2.cs',
    'HumanAppearanceV2.cs',
    'HumanCompanionV2.cs',
    'GuardCoverageV2.cs',
    'HumanTraversalModule.cs',
    'HumanEquipmentModule.cs',
    'HumanSettledGuardModule.cs',
    'HumanBehaviorController.cs',
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
   $allText -notmatch 'GetAllZDOsWithPrefabIterative' -or
   $allText -notmatch 'HumanBehaviorState\.NativeCombat' -or
   $allText -notmatch 'HumanEquipmentModule'){
    throw 'The rebuild is missing its native-copy, state-owned combat, equipment, or global-cleanup boundary.'
}
if($allText -match 'Items\([^\)]*"HelmetIron"'){
    throw 'The visible-identity acceptance loadout cannot contain a hair-hiding iron helmet.'
}
$traversalText=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'HumanTraversalModule.cs') -Raw
$stateText=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'HumanBehaviorStateMachine.cs') -Raw
$companionText=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'HumanCompanionV2.cs') -Raw
$guardText=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'HumanSettledGuardModule.cs') -Raw
$equipmentText=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'HumanEquipmentModule.cs') -Raw
if($traversalText -match 'MoveTo\.Invoke|GetMethod\("MoveTo"'){
    throw 'Traversal cannot add a second movement writer beside native MonsterAI.Follow.'
}
if($traversalText -notmatch 'GetVelocity\(\)' -or $traversalText -notmatch 'native leader tether' -or
   $stateText -notmatch 'signal\.NativeAlert \|\| signal\.LeaderArmed' -or
   $companionText -match 'SetTamed\(true\)' -or $companionText -notmatch 'SetTamed\(false\)'){
    throw 'The governed movement/readiness contract or one-way legacy tame migration is missing.'
}
$controllerText=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'HumanBehaviorController.cs') -Raw
if($controllerText -match 'leader\.GetCurrentWeapon\(\)' -or
   $controllerText -notmatch 'VisibleRight\.GetValue' -or $controllerText -notmatch 'VisibleLeft\.GetValue' -or
   $controllerText -notmatch 'CombatCohesionLatch' -or $controllerText -match 'target\.transform\.position, leader\.transform\.position'){
    throw 'Readiness must use visible hands and combat cohesion must measure actor separation, never enemy range.'
}
if($controllerText -match 'GetMethod\("SetTarget"' -or $controllerText -notmatch 'ReleaseNativeEncounter' -or
   $controllerText -notmatch 'm_targetCreature' -or $controllerText -notmatch 'm_targetStatic' -or
   $controllerText -notmatch 'm_updateTargetTimer' -or $controllerText -notmatch 'SetAlerted'){
    throw 'The combat boundary must use Valheim''s complete native encounter-release transaction; SetTarget(null) is a no-op.'
}
if($traversalText -notmatch 'MovingAnchorLead' -or $traversalText -notmatch 'FollowAnchorPosition'){
    throw 'Native follow arrival-radius compensation is missing.'
}
if($guardText -match 'SetFollowTarget|MoveTo|SetMoveDir|SetRun|EquipItem|UnequipItem|SetTarget|DoAttack|StartAttack' -or
   $guardText -notmatch 'GuardCoverageV2' -or $guardText -notmatch 'SetSettledDestination'){
    throw 'Settled guard must use coverage leases and request the sole traversal writer without controlling combat or equipment.'
}
if($equipmentText -notmatch 'SyncPresentation' -or $equipmentText -notmatch 'SetLeftItem' -or
   $equipmentText -notmatch 'SetRightItem' -or $equipmentText -notmatch 'SetLeftBackItem' -or
   $equipmentText -notmatch 'SetRightBackItem' -or $equipmentText -notmatch 'ZDOVars\.s_rightItem' -or
   $equipmentText -notmatch 'Humanoid\.SetupVisEquipment intentionally updates hidden/back'){
    throw 'The non-Player human atomic hand/back visual synchronization contract is missing.'
}
if($equipmentText -notmatch 'EquipmentPosture\.NativeOwned' -or
   $equipmentText -match 'GetTargetCreature\(\) != null \?\s*EquipmentPosture\.Drawn'){
    throw 'Full native combat must own logical hand selection; presentation cannot redraw the last stowed pair.'
}
$factoryText=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'NativeHumanFactoryV2.cs') -Raw
if($factoryText -notmatch 'm_maxChaseDistance = 0f' -or $factoryText -notmatch 'm_alertRange = 20f'){
    throw 'The travelling companion must use the moving native tamed tether, not a fixed spawn-point chase limit.'
}
if($factoryText -notmatch 'm_updateTargetIntervalNear' -or $factoryText -notmatch 'm_updateTargetIntervalFar' -or
   $factoryText -notmatch 'TargetIntervalNear\.SetValue\(ai, \.5f\)'){
    throw 'The native target scan must use the centralized companion-tempo schedule.'
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
if($LASTEXITCODE -ne 0){throw 'Living World Human NPC 0.6.4 compilation failed.'}

$testExe=Join-Path $build 'FoundationContractTests.exe'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$testExe" `
    (Join-Path $PSScriptRoot 'HumanIdentityRecord.cs') (Join-Path $PSScriptRoot 'CompanionStateLatch.cs') `
    (Join-Path $PSScriptRoot 'CombatCohesionLatch.cs') (Join-Path $PSScriptRoot 'HumanBehaviorStateMachine.cs') `
    (Join-Path $PSScriptRoot 'GuardCoverageV2.cs') $contractTest
if($LASTEXITCODE -ne 0){throw 'Foundation contract test compilation failed.'}
& $testExe
if($LASTEXITCODE -ne 0){throw 'Foundation contract tests failed.'}

$manifest=[ordered]@{
    Version='0.6.4'
    Status='PASS'
    Dll=$dll
    Sha256=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    Sources=@($sources | ForEach-Object {[ordered]@{Path=$_;Sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}})
    Contracts=@(
        'Complete serialized Humanoid/Character state copied before Player removal',
        'No RRRCore or Harmony combat dependency',
        'One state machine is the sole behavior-state writer and retains exact state on invalid input',
        'Native MonsterAI exclusively owns targeting, facing, pursuit and attacks from commit through verified clear',
        'Native Follow is the sole calm/readiness movement writer; formation supplies only a stable destination and pace',
        'Formation orientation follows actual travel and remains fixed while the leader is stationary',
        'Native alert without a creature target selects readiness rather than committed combat',
        'Native combat releases the formation lease while retaining a leader follow tether inside a sustained actor-cohesion boundary',
        'Enemy range never retires a valid ranged target while the actor remains with the party',
        'Out-of-bound pursuit enters one hysteretic latched combat return and suppresses reacquisition until the inner party area',
        'Every newly committed target receives one immediate native EquipBestWeapon handoff without a Living World weapon choice',
        'Native FindEnemy and UpdateTarget retain ownership while their centralized companion scan intervals improve response time',
        'Previously saved 0.5.1 actors migrate only the obsolete tame bit to false',
        'Settled guard is an IdleGuard-only module with temporary opposing coverage leases and completed field-of-view scans',
        'Settled guard requests a destination from the sole traversal writer and cannot target, attack, equip, or issue movement',
        'Readiness observes only visible hand weapons and never Valheim unarmed fallback data',
        'Native Follow remains the only path writer; a moving destination lead compensates its native arrival radius',
        'Equipment posture stows and redraws the native last-equipped pair outside committed combat without hardcoded substitution',
        'Committed combat owns the logical hands while presentation mirrors all native hand and back slots without mutation',
        'Non-Player native humans explicitly synchronize their hidden pair to VisEquipment back slots',
        'The logical hand/back pairs are atomically reconciled to their persistent VisEquipment ZDO slots only when presentation changes',
        'lwparty is explicitly a laboratory transaction that removes stale records before exactly two spawns',
        'Hair is mandatory; every male receives a native beard; females receive none',
        'Acceptance loadouts contain no helmet that can suppress visible facial hair'
    )
    LiveUnity='NOT RUN'
    Installed=$false
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding utf8
Write-Output "HumanNpc 0.6.4 rebuild PASS. DLL SHA256: $($manifest.Sha256)"
Write-Output 'Live Unity identity preservation, pace matching, visible stow/draw, and native-combat handoff remain to be tested.'
