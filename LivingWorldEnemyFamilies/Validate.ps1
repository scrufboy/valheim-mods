param([Parameter(Mandatory=$true)][string]$ProfileBepInEx)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$config = Join-Path $root 'config\Living World Regional Peoples'
$definitions = @(Get-ChildItem -LiteralPath $config -Filter 'rrrnpcs.custom.*.json')
if ($definitions.Count -ne 2) { throw "Expected exactly two regional definitions; found $($definitions.Count)" }
$expected = @('LWC_Rimebound','LWC_Emberworn')
$seen = @()
foreach ($file in $definitions) {
    $raw = Get-Content -Raw -LiteralPath $file.FullName
    $doc = $raw | ConvertFrom-Json
    if ($doc.sOriginalPrefabName -ne 'RRR_NPC') { throw "$($file.Name): wrong base" }
    if ($expected -notcontains $doc.sNewPrefabName) { throw "$($file.Name): unexpected ID" }
    if ($seen -contains $doc.sNewPrefabName) { throw "$($file.Name): duplicate ID" }
    $seen += $doc.sNewPrefabName
    foreach ($axis in @('fX','fY','fZ')) { if ($doc.Category_Appearance.vScale.$axis -ne 1.0) { throw "$($file.Name): $axis must remain adult scale 1.0" } }
    if ($doc.Category_Special.bCanTame -or $doc.Category_Special.bAlwaysTame -or $doc.Category_Special.bCommandableWhenTame) { throw "$($file.Name): must not be tameable or commandable" }
    $attacks = @($doc.Category_Humanoid.aAdvancedCustomAttacks)
    if ($attacks.Count -ne 1) { throw "$($file.Name): exactly one special required" }
    if ($attacks[0].fAIAttackInterval -lt 10) { throw "$($file.Name): special cooldown too short" }
    $d = $attacks[0].dtAttackDamageOverride
    $total = $d.fDamage+$d.fBlunt+$d.fSlash+$d.fPierce+$d.fChop+$d.fPickaxe+$d.fFire+$d.fFrost+$d.fLightning+$d.fPoison+$d.fSpirit
    if ($total -gt 40) { throw "$($file.Name): special damage exceeds approval bound" }
    foreach ($forbidden in @('WoodsSoul','NinoBosque','ForestChild','NorthernForestChild','SouthernForestChild')) { if ($raw -match [regex]::Escape($forbidden)) { throw "$($file.Name): forbidden lineage label: $forbidden" } }
}
$spawn = Get-Content -Raw -LiteralPath (Join-Path $config 'spawn_that.world_spawners.livingworld_regional_peoples.cfg')
if (($spawn | Select-String -Pattern 'Enabled=True' -AllMatches).Matches.Count -ne 0) { throw 'Approval spawns must be disabled' }
foreach ($id in @('RRRN_LWC_Rimebound','RRRN_LWC_Emberworn')) { if ($spawn -notmatch [regex]::Escape($id)) { throw "Missing spawn ID $id" } }
$required = @(
  'plugins\Unknown-RRRCore-3.1.5-DIAGNOSTIC-6-Empty-Visual-Hashes.zip\RRRCore.dll',
  'plugins\ASharpPen-Spawn_That\Valheim.SpawnThat.dll',
  'plugins\OdinPlus-MadHatter\MadHatter.dll',
  'plugins\Unknown-RunicPack-1.1.6-DIAGNOSTIC-1-Valheim-1.0.17.zip\RunicPack.dll'
)
foreach ($relative in $required) { if (-not (Test-Path -LiteralPath (Join-Path $ProfileBepInEx $relative))) { throw "Missing dependency: $relative" } }
$souls = Join-Path $ProfileBepInEx 'config\Souls Of The Land - Enemy Pack'
foreach ($name in @('rrrnpcs.custom.ForestChild1.json','rrrnpcs.custom.NinoBosque.json')) { if (-not (Test-Path -LiteralPath (Join-Path $souls $name))) { throw "Missing technical reference: $name" } }
$binaryChecks = @{
  (Join-Path $ProfileBepInEx 'plugins\OdinPlus-MadHatter\MadHatter.dll') = @('GrimAgeHornedHelm','Dragon_Headdress')
  (Join-Path $ProfileBepInEx 'plugins\Unknown-RunicPack-1.1.6-DIAGNOSTIC-1-Valheim-1.0.17.zip\RunicPack.dll') = @('RunicAxeBlue','RunicAxeRed')
}
foreach ($binary in $binaryChecks.Keys) {
    $bytes = [IO.File]::ReadAllBytes($binary)
    $ascii = [Text.Encoding]::ASCII.GetString($bytes)
    $unicode = [Text.Encoding]::Unicode.GetString($bytes)
    foreach ($prefab in $binaryChecks[$binary]) { if (-not ($ascii.Contains($prefab) -or $unicode.Contains($prefab))) { throw "Installed provider does not expose: $prefab" } }
}
$overlaySource = Get-Content -Raw -LiteralPath (Join-Path $root 'src\LivingWorldLoreNameOverlay.cs')
foreach ($mapping in @('RRRN_Neckman','Fen Draugr','RRRN_Servant','Myrkvidr Thrall','RRRN_OrcBrute','Eitrherjar Legionary')) {
    if ($overlaySource -notmatch [regex]::Escape($mapping)) { throw "Lore overlay mapping missing: $mapping" }
}
foreach ($excluded in @('RRRN_NinoBosque','RRRN_WoodsSoul','RRRN_Pillager')) {
    if ($overlaySource -match [regex]::Escape($excluded)) { throw "Excluded prefab entered lore overlay: $excluded" }
}
Write-Host 'PASS: two unique adult peoples, exact IDs, bounded specials, no taming, disabled spawns, dependencies, provider assets, and references verified.'
