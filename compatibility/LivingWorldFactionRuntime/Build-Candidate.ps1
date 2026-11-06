$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$profile = 'C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena'
$managed = 'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed'
$exportRoot = 'C:\Users\scruf\Documents\LWC Model Skin Studio\Exports'
$version = '0.1.0'
$destination = Join-Path $exportRoot ('LWC-Faction-Relations-' + $version + '-Candidate04')
$configStage = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'build\faction-correction-20261106-final'
if (!(Test-Path -LiteralPath $compiler)) { throw "Compiler missing: $compiler" }
if (Test-Path -LiteralPath $destination) { throw "Fresh candidate destination required: $destination" }
$references = @(
    (Join-Path $profile 'BepInEx\core\BepInEx.dll'),
    (Join-Path $profile 'BepInEx\core\0Harmony.dll'),
    (Join-Path $profile 'BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll'),
    (Join-Path $managed 'assembly_valheim.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'SoftReferenceableAssets.dll'),
    (Join-Path $managed 'netstandard.dll')
)
foreach ($reference in $references) { if (!(Test-Path -LiteralPath $reference)) { throw "Reference missing: $reference" } }
$source = Join-Path $PSScriptRoot 'src\LivingWorldFactionRuntimePlugin.cs'
$testSource = Join-Path $PSScriptRoot 'tests\RuntimeRegistryTests.cs'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('lwc-faction-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $dll = Join-Path $temp 'LivingWorld.FactionRelations.dll'
    $referenceArgs = $references | ForEach-Object { '/reference:' + $_ }
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:library "/out:$dll" $referenceArgs $source
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $LASTEXITCODE" }
    $testExe = Join-Path $temp 'RuntimeRegistryTests.exe'
    & $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$testExe" $referenceArgs $source $testSource
    if ($LASTEXITCODE -ne 0) { throw "Test compilation failed: $LASTEXITCODE" }
    foreach ($reference in $references) { Copy-Item -LiteralPath $reference -Destination $temp -Force }
    $testOutput = & $testExe
    if ($LASTEXITCODE -ne 0) { throw "Runtime registry tests failed: $LASTEXITCODE" }
    New-Item -ItemType Directory -Path $destination | Out-Null
    Copy-Item -LiteralPath $dll -Destination $destination
    Copy-Item -LiteralPath $source -Destination $destination
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LivingWorldFactionRuntime.cfg.candidate') -Destination $destination
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $destination
    Copy-Item -LiteralPath $testSource -Destination $destination
    $testOutput | Set-Content -LiteralPath (Join-Path $destination 'offline-tests.txt') -Encoding UTF8
    if (!(Test-Path -LiteralPath $configStage)) { throw "Reviewed compatibility config stage missing: $configStage" }
    Copy-Item -LiteralPath $configStage -Destination (Join-Path $destination 'compatibility-config') -Recurse
    $referenceManifest = foreach ($reference in $references) {
        $item = Get-Item -LiteralPath $reference
        [ordered]@{ path = $item.FullName; version = $item.VersionInfo.FileVersion; sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $reference).Hash.ToLowerInvariant() }
    }
    $files = Get-ChildItem -LiteralPath $destination -File -Recurse | ForEach-Object {
        [ordered]@{ name = $_.FullName.Substring($destination.Length + 1); length = $_.Length; sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLowerInvariant() }
    }
    [ordered]@{ plugin = 'LivingWorld.FactionRelations'; version = $version; installed = $false; gameLaunched = $false;
        compiler = $compiler; references = @($referenceManifest); files = @($files) } |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destination 'build-verification.json') -Encoding UTF8
    Write-Output $destination
} finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
