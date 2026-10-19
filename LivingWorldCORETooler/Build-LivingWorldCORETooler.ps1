param(
    [string]$ManagedDirectory = 'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$BepInExDirectory = 'C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\runtime\BepInEx\core',
    [string]$CompatibilityForkDirectory = 'C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\ValheimToolerOwnership\BepInEx\plugins\ValheimToolerCompatibilityFork'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$build = Join-Path $root 'build'
$package = Join-Path $root 'package'
$plugin = Join-Path $package 'plugins\LivingWorldCORETooler'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $root 'src\LivingWorldToolerExtension.cs'
$output = Join-Path $build 'LivingWorld.Tooler.Extension.dll'
$icon = Join-Path $root 'icon.png'
$iconBase64 = Join-Path $root 'icon.png.base64'

New-Item -ItemType Directory -Force -Path $build, $plugin | Out-Null

if (-not (Test-Path -LiteralPath $icon -PathType Leaf) -and (Test-Path -LiteralPath $iconBase64 -PathType Leaf)) {
    [IO.File]::WriteAllBytes($icon, [Convert]::FromBase64String((Get-Content -LiteralPath $iconBase64 -Raw).Trim()))
}

$references = @(
    (Join-Path $BepInExDirectory 'BepInEx.dll'),
    (Join-Path $BepInExDirectory '0Harmony.dll'),
    (Join-Path $ManagedDirectory 'netstandard.dll'),
    (Join-Path $ManagedDirectory 'assembly_valheim.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.CoreModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $CompatibilityForkDirectory 'ValheimTooler.dll')
)

foreach ($path in @($compiler, $source, $icon) + $references) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing build input: $path"
    }
}

& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:library "/out:$output" ($references | ForEach-Object { "/reference:$_" }) $source
if ($LASTEXITCODE -ne 0) {
    throw 'Living World Tooler compilation failed.'
}

Copy-Item -LiteralPath $output -Destination $plugin -Force
Copy-Item -LiteralPath (Join-Path $CompatibilityForkDirectory 'ValheimTooler.dll'), (Join-Path $CompatibilityForkDirectory 'ValheimToolerMod.dll'), (Join-Path $CompatibilityForkDirectory 'SharpConfig.dll') -Destination $plugin -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md'), (Join-Path $root 'CHANGELOG.md'), (Join-Path $root 'manifest.json'), (Join-Path $root 'LICENSE'), $icon -Destination $package -Force

[ordered]@{
    Product = "Living World C.O.R.E. Tooler"
    PackageVersion = "2.1.0"
    ExtensionVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($output).FileVersion
    Status = "PASS"
    Scope = "Offline compilation and original-interface integration verification"
    Runtime = "NOT RUN"
    ExtensionSha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
    OriginalInterfaceSha256 = (Get-FileHash -LiteralPath (Join-Path $CompatibilityForkDirectory 'ValheimTooler.dll') -Algorithm SHA256).Hash
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding UTF8

Get-Content -LiteralPath (Join-Path $build 'build-verification.json')
