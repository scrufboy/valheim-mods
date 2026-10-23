param(
    [string]$ProfileRoot='C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1'
)
$ErrorActionPreference='Stop'
if(Get-Process -Name valheim,valheim_server -ErrorAction SilentlyContinue){throw 'Valheim must be closed before installing the rebuilt human foundation.'}
$profile=(Resolve-Path -LiteralPath $ProfileRoot).Path
$plugins=Join-Path $profile 'BepInEx\plugins'
$backupRoot=Join-Path $profile ('BepInEx\LivingWorldBackups\Rebuild\'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-pre-0.6.4')
$humanSource=Join-Path $PSScriptRoot 'build\LivingWorld.HumanNpc.dll'
$telemetrySource=Join-Path $PSScriptRoot '..\..\LivingWorldTelemetry\rebuild\build\LivingWorld.Telemetry.dll'
$humanTarget=Join-Path $plugins 'LivingWorldHumanNpc\LivingWorld.HumanNpc.dll'
$telemetryTarget=Join-Path $plugins 'LivingWorldTelemetry\LivingWorld.Telemetry.dll'
$nestedTelemetryBackup=Join-Path $plugins 'LivingWorldTelemetry\backups\LivingWorld.Telemetry-0.2.1-pre-native-selection.dll'
foreach($path in @($humanSource,$telemetrySource)){if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing verified rebuild artifact: $path"}}
foreach($target in @($humanTarget,$telemetryTarget)){
    $parent=Split-Path $target -Parent
    $resolvedParent=(Resolve-Path -LiteralPath $parent).Path
    if(-not $resolvedParent.StartsWith($plugins,[StringComparison]::OrdinalIgnoreCase)){throw "Install target escaped the selected profile: $target"}
}
$null=New-Item -ItemType Directory -Force -Path $backupRoot
foreach($target in @($humanTarget,$telemetryTarget)){
    if(Test-Path -LiteralPath $target -PathType Leaf){Copy-Item -LiteralPath $target -Destination (Join-Path $backupRoot (Split-Path $target -Leaf))}
}
if(Test-Path -LiteralPath $nestedTelemetryBackup -PathType Leaf){
    Move-Item -LiteralPath $nestedTelemetryBackup -Destination (Join-Path $backupRoot (Split-Path $nestedTelemetryBackup -Leaf))
}
Copy-Item -LiteralPath $humanSource -Destination $humanTarget -Force
Copy-Item -LiteralPath $telemetrySource -Destination $telemetryTarget -Force

$humanHash=(Get-FileHash -LiteralPath $humanTarget -Algorithm SHA256).Hash
$telemetryHash=(Get-FileHash -LiteralPath $telemetryTarget -Algorithm SHA256).Hash
if($humanHash -ne (Get-FileHash -LiteralPath $humanSource -Algorithm SHA256).Hash -or
   $telemetryHash -ne (Get-FileHash -LiteralPath $telemetrySource -Algorithm SHA256).Hash){
    throw 'Installed DLL verification failed.'
}
$duplicates=@(Get-ChildItem -LiteralPath $plugins -File -Recurse -Filter '*.dll' | Where-Object {
    ($_.Name -eq 'LivingWorld.HumanNpc.dll' -and $_.FullName -ne $humanTarget) -or
    ($_.Name -like 'LivingWorld.Telemetry*.dll' -and $_.FullName -ne $telemetryTarget)
})
if($duplicates.Count){throw 'A duplicate human or telemetry DLL remains under BepInEx plugins: '+(($duplicates|ForEach-Object FullName)-join '; ')}

[ordered]@{
    InstalledAt=(Get-Date).ToString('o');Profile=$profile;Backup=$backupRoot;
    HumanNpc=[ordered]@{Version='0.6.4';Path=$humanTarget;Sha256=$humanHash};
    Telemetry=[ordered]@{Version='0.6.4';Path=$telemetryTarget;Sha256=$telemetryHash};
    DuplicateDlls=0
}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $backupRoot 'install-verification.json') -Encoding utf8
Write-Output "Installed HumanNpc 0.6.4: $humanHash"
Write-Output "Installed Telemetry 0.6.4: $telemetryHash"
Write-Output "Previous active DLLs and the nested telemetry backup were preserved at: $backupRoot"
