$ErrorActionPreference='Stop'
$verification=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build\build-verification.json') -Raw|ConvertFrom-Json
if($verification.Status -ne 'PASS' -or $verification.Version -ne '0.2.0'){throw 'Verified telemetry build required.'}
if((Get-FileHash -LiteralPath $verification.DllPath -Algorithm SHA256).Hash -ne $verification.DllSha256){throw 'Verified DLL changed.'}
$package=Join-Path $PSScriptRoot 'package'
$plugin=Join-Path $package 'plugins\LivingWorldTelemetry'
$null=New-Item -ItemType Directory -Force -Path $plugin
Copy-Item -LiteralPath $verification.DllPath -Destination (Join-Path $plugin 'LivingWorld.Telemetry.dll')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'START-HERE.txt') -Destination (Join-Path $plugin 'START-HERE.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'TELEMETRY-SCHEMA.txt') -Destination (Join-Path $plugin 'TELEMETRY-SCHEMA.txt')
$archive=Join-Path $PSScriptRoot 'LivingWorld-Telemetry-0.2.0-AUTOMATIC-LAB.zip'
if(Test-Path -LiteralPath $archive){throw 'Archive exists; refusing overwrite.'}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($package,$archive,[IO.Compression.CompressionLevel]::Optimal,$false)
$zip=[IO.Compression.ZipFile]::OpenRead($archive)
try {
    $entries=@($zip.Entries)
    if($entries.Count -ne 3 -or @($entries|Where-Object {$_.FullName -eq 'plugins/LivingWorldTelemetry/LivingWorld.Telemetry.dll'}).Count -ne 1){throw 'Archive shape invalid.'}
} finally {$zip.Dispose()}
"Archive PASS: $archive"
"Archive SHA256: $((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash)"
'Not installed.'
