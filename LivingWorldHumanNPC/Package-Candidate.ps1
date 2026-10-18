$ErrorActionPreference='Stop'
$verification=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build/build-verification.json') -Raw|ConvertFrom-Json
if($verification.Status -ne 'PASS' -or $verification.Version -ne '0.3.43'){throw 'Current candidate must pass the complete offline build before packaging.'}
if((Get-FileHash -LiteralPath $verification.DllPath -Algorithm SHA256).Hash -ne $verification.DllSha256){throw 'Verified binary changed.'}
foreach($source in $verification.Sources){if((Get-FileHash -LiteralPath $source.Path -Algorithm SHA256).Hash -ne $source.Sha256){throw "Verified source changed: $($source.RelativePath)"}}
$package=Join-Path $PSScriptRoot 'package'
$plugin=Join-Path $package 'plugins/LivingWorldHumanNpc/LivingWorld.HumanNpc.dll'
$null=New-Item -ItemType Directory -Force -Path (Split-Path $plugin -Parent)
Copy-Item -LiteralPath $verification.DllPath -Destination $plugin
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'START-HERE.txt') -Destination (Join-Path (Split-Path $plugin -Parent) 'START-HERE.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'build/build-verification.json') -Destination (Join-Path $package 'build-verification.json')
$files=@(Get-ChildItem -LiteralPath $package -Recurse -File|ForEach-Object {
    $relative=[IO.Path]::GetRelativePath($package,$_.FullName).Replace('\','/')
    [ordered]@{Source=$relative;Destination=$relative;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
})
$manifest=Join-Path $PSScriptRoot 'package-manifest.json'
[ordered]@{Schema=1;Files=$files}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $manifest -Encoding utf8
$archive=Join-Path $PSScriptRoot 'LivingWorld-HumanNPC-0.3.43-ATOMIC-COMBAT-HANDOFF-LAB.zip'
if(Test-Path -LiteralPath $archive){throw 'Archive already exists; preserve it rather than overwriting another candidate.'}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open($archive,[IO.Compression.ZipArchiveMode]::Create)
try {foreach($file in $files){$null=[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $package $file.Source),$file.Destination,[IO.Compression.CompressionLevel]::Optimal)}} finally {$zip.Dispose()}
$result=& (Join-Path $PSScriptRoot 'tests/Verify-BepInExArchive.ps1') -CandidateDirectory $package -ArchivePath $archive -ManifestPath $manifest
if(-not $result.Passed){throw 'Archive verification failed; do not install.'}
$result|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'archive-verification.json') -Encoding utf8
"Archive PASS: $archive"
"Archive SHA256: $((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash)"
'Not installed. No profile, world, or synced project source was edited.'

