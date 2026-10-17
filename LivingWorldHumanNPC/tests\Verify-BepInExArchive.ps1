param(
    [Parameter(Mandatory=$true)][string]$CandidateDirectory,
    [Parameter(Mandatory=$true)][string]$ArchivePath,
    [Parameter(Mandatory=$true)][string]$ManifestPath
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Safe-RelativePath([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { throw 'Empty package path.' }
    $normalized = $Value.Replace('\','/')
    if ($normalized.StartsWith('/') -or $normalized.Contains(':') -or $normalized.Contains([char]0)) { throw "Absolute/invalid package path: $Value" }
    $parts = $normalized.Split('/')
    foreach ($part in $parts) {
        if ([string]::IsNullOrWhiteSpace($part) -or $part -eq '.' -or $part -eq '..' -or
            $part.EndsWith('.') -or $part.EndsWith(' ') -or $part -match '[<>"|?*\x00-\x1f]' -or
            $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)') { throw "Unsafe package path: $Value" }
    }
    if ($parts[0] -ieq 'BepInEx') { throw "Archive must extract INSIDE BepInEx; leading BepInEx/ is forbidden: $Value" }
    return $normalized
}
function Under-Root([string]$Root, [string]$Relative) {
    $resolved = [IO.Path]::GetFullPath([IO.Path]::Combine($Root, $Relative.Replace('/',[IO.Path]::DirectorySeparatorChar)))
    $prefix = $Root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw "Escaped root: $Relative" }
    return $resolved
}
function Hash-Bytes([byte[]]$Bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-','') } finally { $sha.Dispose() }
}

$candidateRoot = (Resolve-Path -LiteralPath $CandidateDirectory).Path
$archiveFile = (Resolve-Path -LiteralPath $ArchivePath).Path
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($manifest.Schema -ne 1 -or @($manifest.Files).Count -eq 0) { throw 'Manifest requires Schema=1 and nonempty Files.' }
$expected = @{}
$sources = @{}
foreach ($item in $manifest.Files) {
    $destination = Safe-RelativePath ([string]$item.Destination)
    $source = Safe-RelativePath ([string]$item.Source)
    if ($expected.ContainsKey($destination)) { throw "Duplicate manifest destination: $destination" }
    $sourcePath = Under-Root $candidateRoot $source
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw "Missing candidate: $source" }
    $declaredHash = [string]$item.Sha256
    if ($declaredHash -notmatch '^[A-Fa-f0-9]{64}$') { throw "Invalid manifest SHA256: $destination" }
    $actual = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    if ($actual -ine $declaredHash) { throw "Candidate/manifest hash mismatch: $destination" }
    $expected[$destination] = @{ Source=$sourcePath; Hash=$actual }
    $sources[$sourcePath] = $true
}
foreach ($binary in Get-ChildItem -LiteralPath $candidateRoot -Recurse -File -Filter '*.dll') {
    if (-not $sources.ContainsKey($binary.FullName)) { throw "Candidate binary absent from manifest: $($binary.FullName)" }
}

# The only extraction target is a fresh experiment-local sandbox, never an
# argument supplied by the caller and never the installed game/profile.
$sandboxRoot = Join-Path $PSScriptRoot ('sandbox-' + [Guid]::NewGuid().ToString('N'))
$destinationRoot = Join-Path $sandboxRoot 'BepInEx'
$null = New-Item -ItemType Directory -Path $destinationRoot
$installedHashes = @{}
try {
    foreach ($destination in $expected.Keys) {
        if ([IO.Path]::GetExtension($destination) -ieq '.dll') {
            $installed = Under-Root $destinationRoot $destination
            $null = New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($installed)) -Force
            [IO.File]::WriteAllBytes($installed, [Text.Encoding]::UTF8.GetBytes('representative-old-installed-dll:' + $destination))
            $installedHashes[$destination] = (Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash
        }
    }
    $unrelatedRelative = 'plugins/ReleaseGateUnrelated/Sentinel.dll'
    if ($expected.ContainsKey($unrelatedRelative)) { throw 'Reserved fixture sentinel path used by manifest.' }
    $unrelated = Under-Root $destinationRoot $unrelatedRelative
    $null = New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($unrelated)) -Force
    [IO.File]::WriteAllBytes($unrelated, [Text.Encoding]::UTF8.GetBytes('unrelated installed binary must survive'))
    $unrelatedHash = (Get-FileHash -LiteralPath $unrelated -Algorithm SHA256).Hash

    $zip = [IO.Compression.ZipFile]::OpenRead($archiveFile)
    try {
        $seen = @{}
        $files = @{}
        foreach ($entry in $zip.Entries) {
            $directory = $entry.FullName.EndsWith('/') -or $entry.FullName.EndsWith('\')
            $entryPath = if ($directory) { $entry.FullName.TrimEnd('/','\') } else { $entry.FullName }
            $relative = Safe-RelativePath $entryPath
            if ($seen.ContainsKey($relative)) { throw "Duplicate archive destination: $relative" }
            $seen[$relative] = $directory
            # Unix symlink archives must not create redirecting extraction paths.
            $unixType = ($entry.ExternalAttributes -shr 16) -band 61440
            if ($unixType -eq 40960) { throw "Archive symlink is forbidden: $relative" }
            if (-not $directory) {
                if (-not $expected.ContainsKey($relative)) { throw "Unexpected archive file/binary: $relative" }
                $files[$relative] = $entry
            }
        }
        foreach ($relative in $seen.Keys) {
            $parts = $relative.Split('/')
            for ($i=1; $i -lt $parts.Length; $i++) {
                $ancestor = ($parts[0..($i-1)] -join '/')
                if ($seen.ContainsKey($ancestor) -and -not $seen[$ancestor]) { throw "File/directory collision: $relative" }
            }
        }
        foreach ($relative in $expected.Keys) {
            if (-not $files.ContainsKey($relative)) { throw "Missing archive file/binary: $relative" }
        }
        foreach ($relative in $files.Keys) {
            $entry = $files[$relative]
            $stream = $entry.Open()
            $memory = New-Object IO.MemoryStream
            try { $stream.CopyTo($memory); $bytes = $memory.ToArray() } finally { $stream.Dispose(); $memory.Dispose() }
            if ((Hash-Bytes $bytes) -ine $expected[$relative].Hash) { throw "Archive/candidate hash mismatch: $relative" }
            $target = Under-Root $destinationRoot $relative
            $null = New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force
            [IO.File]::WriteAllBytes($target,$bytes)
            if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ine $expected[$relative].Hash) { throw "Extracted candidate mismatch: $relative" }
            if ($installedHashes.ContainsKey($relative) -and $installedHashes[$relative] -eq $expected[$relative].Hash) { throw "Fixture did not demonstrate overwrite: $relative" }
        }
        if ((Get-FileHash -LiteralPath $unrelated -Algorithm SHA256).Hash -ine $unrelatedHash) { throw 'Unrelated binary modified.' }
        [pscustomobject]@{ Passed=$true; Archive=$archiveFile; FilesVerified=$files.Count; BinariesOverwritten=$installedHashes.Count; UnrelatedBinaryPreserved=$true }
    } finally { $zip.Dispose() }
} finally {
    $resolvedSandbox = [IO.Path]::GetFullPath($sandboxRoot)
    $allowedPrefix = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar + 'sandbox-'
    if ($resolvedSandbox.StartsWith($allowedPrefix,[StringComparison]::OrdinalIgnoreCase)) { Remove-Item -LiteralPath $resolvedSandbox -Recurse -Force }
}
