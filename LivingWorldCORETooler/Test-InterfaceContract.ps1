param(
    [string]$ProjectRoot = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
$extensionPath = Join-Path $ProjectRoot 'src\LivingWorldToolerExtension.cs'
$panelPath = Join-Path $ProjectRoot 'src\LivingWorldTrainerPanel.cs'
$modulesPath = Join-Path $ProjectRoot 'src\LivingWorldToolerModules.cs'

$extension = Get-Content -LiteralPath $extensionPath -Raw
$panel = Get-Content -LiteralPath $panelPath -Raw
$modules = Get-Content -LiteralPath $modulesPath -Raw

$requiredOriginalSurface = @(
    '"$vt_toolbar_player"',
    '"$vt_toolbar_entities"',
    '"$vt_toolbar_terrain_shaper"',
    '"$vt_toolbar_misc"',
    '"Living World"',
    'PlayerHacks.DisplayGUI();',
    'EntitiesItemsHacks.DisplayGUI();',
    'TerrainShaper.DisplayGUI();',
    'MiscHacks.DisplayGUI();'
)

$requiredLivingWorldPages = @(
    '"Companions"',
    '"Raids"',
    '"Player"',
    '"Spawn"',
    '"World"',
    '"Build"',
    '"Admin"'
)

$requiredCapabilities = @(
    'lwparty',
    'lwparty new',
    'lwparty clean',
    'lwstatus',
    'lwheal',
    'lwsummon',
    'lwgoto',
    'event army_moder',
    'event lwc_souls_pillagers',
    'event lwc_krauguk_legion',
    'stopevent',
    'tod 0',
    'env Rain',
    'terrain_level',
    'terrain_reset',
    'exploremap',
    'genloc',
    'killall',
    'removedrops',
    'killme',
    'SiteSurveyPanel.DisplayGUI'
)

foreach ($fragment in $requiredOriginalSurface) {
    if (-not $extension.Contains($fragment)) {
        throw "Stable original Tooler surface is missing: $fragment"
    }
}

foreach ($fragment in $requiredLivingWorldPages) {
    if (-not $modules.Contains($fragment)) {
        throw "Stable Living World page is missing: $fragment"
    }
}

foreach ($fragment in $requiredCapabilities) {
    if (-not $panel.Contains($fragment)) {
        throw "Stable Tooler capability is missing: $fragment"
    }
}

$pageCount = ([regex]::Matches($modules, 'new ToolerPage\(')).Count
if ($pageCount -ne $requiredLivingWorldPages.Count) {
    throw "Expected $($requiredLivingWorldPages.Count) registered Living World pages; found $pageCount."
}

[ordered]@{
    Status = 'PASS'
    OriginalToolbarPages = 4
    LivingWorldPages = $pageCount
    InterfacePolicy = 'Existing pages, commands, layout, and original feature surface are build-enforced.'
} | ConvertTo-Json
