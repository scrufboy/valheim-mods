param(
    [Parameter(Mandatory=$true)][string]$ProfileBepInEx,
    [string]$ManagedDirectory='D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$managed = $ManagedDirectory
$output = Join-Path $root 'build\LivingWorldLoreNameOverlay.dll'
New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force | Out-Null
$bep = Join-Path $ProfileBepInEx 'core\BepInEx.dll'
$unity = Join-Path $managed 'UnityEngine.dll'
$unityCore = Join-Path $managed 'UnityEngine.CoreModule.dll'
$valheim = Join-Path $managed 'assembly_valheim.dll'
$netstandard = Join-Path $managed 'netstandard.dll'
& $compiler /nologo /target:library /optimize+ "/out:$output" "/reference:$bep" "/reference:$netstandard" "/reference:$unity" "/reference:$unityCore" "/reference:$valheim" (Join-Path $root 'src\LivingWorldLoreNameOverlay.cs')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $output)) { throw 'Lore overlay compilation failed' }
Get-FileHash -Algorithm SHA256 -LiteralPath $output
