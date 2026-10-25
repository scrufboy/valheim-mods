param([string]$ProjectRoot = $PSScriptRoot)
$ErrorActionPreference='Stop'
$plugin=Get-Content -LiteralPath (Join-Path $ProjectRoot 'src\LivingWorldTrainerPanel.cs') -Raw
$modules=Get-Content -LiteralPath (Join-Path $ProjectRoot 'src\LivingWorldToolerModules.cs') -Raw
$survey=Get-Content -LiteralPath (Join-Path $ProjectRoot 'src\LivingWorldSiteSurvey.cs') -Raw
$required=@(
  'Site Survey','Begin Survey','Mark Corner','Calculate','Preview Campus','Export Plan','Clear Preview',
  'GetWorldUID','GetSeed','GetGroundHeight','Physics.Raycast','Heightmap.FindBiome','MaxSamples = 4096',
  'liveAlteredTerrainAuthoritative','seedTerrainInferred','SiteSurveys','LivingWorldSurveyPreviewMarker',
  'JoystickButton0','JoystickButton1','JoystickButton2','JoystickButton3','Apply Terrain — LOCKED',
  'world-backup receipt','GetPeers','hostile combatant','cancellation','ToolerDuty.SiteSurvey'
)
$all=$plugin+"`n"+$modules+"`n"+$survey
$missing=@($required|Where-Object{$all.IndexOf($_,[StringComparison]::Ordinal) -lt 0})
if($missing.Count){throw "Site Survey contract missing: $($missing -join ', ')"}
$forbidden=@('new TerrainModifier','Instantiate(TerrainModifier','terrain_raise ','terrain_lower ','terrain_level ')
$present=@($forbidden|Where-Object{$survey.IndexOf($_,[StringComparison]::OrdinalIgnoreCase) -ge 0})
if($present.Count){throw "Site Survey must remain non-mutating: $($present -join ', ')"}
[ordered]@{Status='PASS';Module='SiteSurveyPanel';Controller=$true;LiveTerrain=$true;MaxSamples=4096;TerrainApplication='LOCKED';Install='HELD'} | ConvertTo-Json
