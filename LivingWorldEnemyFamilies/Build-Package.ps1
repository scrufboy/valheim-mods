param([Parameter(Mandatory=$true)][string]$ProfileBepInEx)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $root 'Build-Overlay.ps1') -ProfileBepInEx $ProfileBepInEx
$dist = Join-Path $root 'dist'
$packageName = 'LivingWorld_Rimebound_and_Emberworn_0.1.0'
$stage = Join-Path $dist $packageName
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$packageConfig = Join-Path $stage 'BepInEx\config\Living World Regional Peoples'
New-Item -ItemType Directory -Path $packageConfig -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stage 'BepInEx\plugins\LivingWorld-LoreNameOverlay') -Force | Out-Null
$sourceConfig = Join-Path $root 'config\Living World Regional Peoples'
foreach ($name in @('rrrnpcs.custom.LWC_Rimebound.json','rrrnpcs.custom.LWC_Emberworn.json','spawn_that.world_spawners.livingworld_regional_peoples.cfg')) { Copy-Item -LiteralPath (Join-Path $sourceConfig $name) -Destination $packageConfig }
foreach ($name in @('manifest.json','README.md','DEPENDENCIES.md','APPROVAL-CHECKLIST.md','ASSET-AND-TUNING-REPORT.md','INSTALL-ROLLBACK.md')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $stage }
Copy-Item -LiteralPath (Join-Path $root 'build\LivingWorldLoreNameOverlay.dll') -Destination (Join-Path $stage 'BepInEx\plugins\LivingWorld-LoreNameOverlay')
$zip = Join-Path $dist "$packageName.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
Get-FileHash -Algorithm SHA256 -LiteralPath $zip
