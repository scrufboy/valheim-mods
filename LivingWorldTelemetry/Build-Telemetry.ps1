param(
    [string]$ManagedDirectory='D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$LivingWorldRoot=(Join-Path $PSScriptRoot '..\human-npc-0.3.45-guard-scan-coverage'),
    [string]$CoreRoot=(Join-Path $PSScriptRoot '..\independent-core')
)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Core'){throw 'Run with PowerShell 7.'}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$build=Join-Path $PSScriptRoot 'build'
$null=New-Item -ItemType Directory -Force -Path $build
$source=Join-Path $PSScriptRoot 'src\LivingWorldTelemetryPlugin.cs'
$main=Join-Path $LivingWorldRoot 'build\LivingWorld.HumanNpc.dll'
$bep=Join-Path $CoreRoot 'build\BepInEx.dll'
$jotunn=Join-Path $CoreRoot 'build\Jotunn.dll'
$harmony=Join-Path (Join-Path $LivingWorldRoot '..\runtime\BepInEx\core') '0Harmony.dll'
$cecil=Join-Path $CoreRoot 'tools\Mono.Cecil.dll'
foreach($path in @($source,$main,$bep,$jotunn,$harmony,$cecil)){if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing build input: $path"}}
$refs=@($bep,$harmony,$main)
$refs+=@('netstandard','assembly_valheim','assembly_utils','Splatform','SoftReferenceableAssets','MagicaClothV2','UnityEngine','UnityEngine.CoreModule','UnityEngine.AnimationModule','UnityEngine.PhysicsModule','UnityEngine.TerrainPhysicsModule') | ForEach-Object {Join-Path $ManagedDirectory ($_+'.dll')}
foreach($path in $refs){if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing reference: $path"}}
$dll=Join-Path $build 'LivingWorld.Telemetry.dll'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:library "/out:$dll" ($refs|ForEach-Object{"/reference:$_"}) $source
if($LASTEXITCODE -ne 0){throw 'Telemetry compilation failed.'}

Add-Type -Path $cecil
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
try {
    $types=@($assembly.MainModule.Types)
    $plugin=@($types|Where-Object FullName -eq 'LivingWorld.Telemetry.LivingWorldTelemetryPlugin')
    if($plugin.Count -ne 1){throw 'Telemetry plugin type missing.'}
    $methods=@($plugin[0].Methods|Where-Object HasBody)
    $calls=@($methods|ForEach-Object {$_.Body.Instructions}|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]}|ForEach-Object {$_.Operand.FullName})
    if(@($calls|Where-Object {$_.Contains('System.IO.TextWriter::WriteLine')}).Count -eq 0){throw 'Bounded writer call absent.'}
    $strings=@($methods|ForEach-Object {$_.Body.Instructions}|Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr}|ForEach-Object Operand)
    foreach($required in @('SelectBestAttack','Cancel','RPC_Damage','Heal','StartAttack','OnAttackTrigger','BlockAttack')){
        if($required -notin $strings){throw "Required reflected observation boundary absent: $required"}
    }
    if('lwtelemetry' -in $strings -or @($calls|Where-Object {$_ -match 'Terminal/ConsoleCommand::.ctor'}).Count){
        throw 'Manual telemetry console workflow is forbidden.'
    }
    $update=@($methods|Where-Object Name -eq 'Update')
    $updateCalls=@($update.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]}|ForEach-Object {$_.Operand.Name})
    if('StartSession' -notin $updateCalls -or 'StopSession' -notin $updateCalls){throw 'Automatic world lifecycle recording is not wired.'}
    if(@($calls|Where-Object {$_ -match 'ZDO::Set|ClaimOwnership|SetTarget|MoveTo|EquipItem|Character::SetHealth|Character::Damage\(|Character::Heal\('}).Count -gt 0){
        throw 'Telemetry contains a forbidden gameplay mutation call.'
    }
    $refsFound=@($assembly.MainModule.AssemblyReferences|ForEach-Object Name)
    if(@($refsFound|Where-Object {$_ -match '(?i)rrr|souls'}).Count){throw 'Forbidden external gameplay dependency found.'}
    $hash=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    [ordered]@{
        Version='0.2.2';Status='PASS';DllPath=$dll;DllSha256=$hash;
        MainNpcSha256=(Get-FileHash -LiteralPath $main -Algorithm SHA256).Hash;
        Scope='Automatic read-only lab observation plus bounded rotating local JSONL output; no commands; Unity runtime not executed';
        RecordsPerSecondCap=50;RecordCap=200000;ByteCap=67108864;SampleSeconds=0.5;
        AssemblyReferences=$refsFound
    }|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding utf8
    "Telemetry 0.2.2 offline build PASS. DLL SHA256: $hash"
    'No game/profile/world was launched or changed.'
} finally {$assembly.Dispose()}
