param(
    [string]$CoreRoot = (Join-Path $PSScriptRoot '..\..\..\lwc-core-norse-port\LivingWorldCORE\npc'),
    [string]$Managed = 'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$Profile = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\LWC Human Lab v1"
)
$ErrorActionPreference = 'Stop'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot 'build-1.0.32.8'
New-Item -ItemType Directory -Force -Path $output | Out-Null

$names = 'assembly_valheim','assembly_utils','assembly_guiutils','Splatform','SoftReferenceableAssets','MagicaClothV2',
         'netstandard','UnityEngine','UnityEngine.CoreModule','UnityEngine.AnimationModule','UnityEngine.PhysicsModule',
         'UnityEngine.TerrainPhysicsModule','UnityEngine.AudioModule','UnityEngine.AIModule','UnityEngine.ParticleSystemModule',
         'UnityEngine.IMGUIModule','UnityEngine.UI','UnityEngine.TextRenderingModule','Unity.TextMeshPro','UnityEngine.InputLegacyModule'
$refs = @($names | ForEach-Object { Join-Path $Managed ($_ + '.dll') })
$refs += Join-Path $Profile 'BepInEx\core\BepInEx.dll'
$refs += Join-Path $Profile 'BepInEx\core\0Harmony.dll'
$refs += Join-Path $Profile 'BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll'
foreach ($reference in $refs) { if (!(Test-Path -LiteralPath $reference)) { throw "Missing reference: $reference" } }
$refArgs = $refs | ForEach-Object { '/r:' + $_ }
$sources = Get-ChildItem -Recurse -Filter *.cs (Join-Path $CoreRoot 'src') |
    Where-Object { $_.FullName -notmatch '\\Telemetry\\' } | ForEach-Object FullName
$sources += Join-Path $PSScriptRoot 'EncounterAdapterPolicy.cs'
$sources += Join-Path $PSScriptRoot 'CoreEncounterAdapter.cs'
$dll = Join-Path $output 'LivingWorld.HumanNpc.dll'

& $csc /nologo /noconfig /target:library /optimize+ /langversion:5 /nostdlib+ `
    /r:"$Managed\mscorlib.dll" /r:"$Managed\System.dll" /r:"$Managed\System.Core.dll" `
    @refArgs /out:$dll @sources
if ($LASTEXITCODE) { throw '1.0.32.8 core + encounter adapter compilation failed' }
Get-FileHash -Algorithm SHA256 $dll
