param(
    [string]$Managed = 'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$Profile = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena"
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$build = Join-Path $root 'build'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$null = New-Item -ItemType Directory -Force -Path $build
$output = Join-Path $build 'LivingWorld.RRRCoreCompat.dll'
Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue

$refs = @(
    (Join-Path $Managed 'mscorlib.dll'),
    (Join-Path $Managed 'System.dll'),
    (Join-Path $Managed 'System.Core.dll'),
    (Join-Path $Managed 'netstandard.dll'),
    (Join-Path $Managed 'assembly_valheim.dll'),
    (Join-Path $Managed 'MagicaClothV2.dll'),
    (Join-Path $Managed 'UnityEngine.dll'),
    (Join-Path $Managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $Managed 'UnityEngine.PhysicsModule.dll'),
    (Join-Path $Profile 'BepInEx\core\BepInEx.dll'),
    (Join-Path $Profile 'BepInEx\core\0Harmony.dll')
)
foreach ($ref in $refs) {
    if (-not (Test-Path -LiteralPath $ref -PathType Leaf)) { throw "Missing reference: $ref" }
}
$refArgs = $refs | ForEach-Object { '/r:' + $_ }
& $csc /noconfig /nologo /target:library /optimize+ /langversion:5 /nostdlib+ `
    @refArgs /out:$output (Join-Path $root 'Plugin.cs')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $output)) { throw 'Compatibility build failed.' }
Get-FileHash -LiteralPath $output -Algorithm SHA256 | Format-Table Hash,Path

