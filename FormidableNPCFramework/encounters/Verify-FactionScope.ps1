param(
    [Parameter(Mandatory=$true)][string]$ManagedDirectory,
    [Parameter(Mandatory=$true)][string]$BepInExCoreDirectory,
    [Parameter(Mandatory=$true)][string]$CustomRaidsDll
)
$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$candidate = Join-Path $env:TEMP 'LWC-SwampFactionScope-Source.dll'
$checks = Join-Path $env:TEMP 'LWC-SwampFactionScope-Tests.exe'
$arguments = @('/nologo','/noconfig','/nostdlib+','/target:library','/langversion:5','/warn:4','/warnaserror+',"/out:$candidate")
foreach ($reference in @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','assembly_valheim.dll','assembly_utils.dll','UnityEngine.CoreModule.dll')) {
    $arguments += '/r:' + (Join-Path $ManagedDirectory $reference)
}
foreach ($reference in @('0Harmony.dll','BepInEx.dll')) {
    $arguments += '/r:' + (Join-Path $BepInExCoreDirectory $reference)
}
$arguments += '/r:' + $CustomRaidsDll
$arguments += Join-Path $PSScriptRoot 'SwampRaidFactionScope.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Installed SDK faction adapter compile failed' }
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /define:LWC_FACTION_POLICY_TEST /target:exe "/out:$checks" (Join-Path $PSScriptRoot 'SwampRaidFactionScope.cs') (Join-Path $PSScriptRoot 'test_faction_scope.cs')
if ($LASTEXITCODE -ne 0) { throw 'Faction scope policy compile failed' }
& $checks
if ($LASTEXITCODE -ne 0) { throw 'Faction scope policy checks failed' }
[ordered]@{
    Status = 'SOURCE_COMPILED_POLICY_TESTED'
    Installed = $false
    GameLaunched = $false
    CustomRaidsSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $CustomRaidsDll).Hash
    Candidate = $candidate
} | ConvertTo-Json
