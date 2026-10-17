$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$fixtureRoot = Join-Path $PSScriptRoot ('fixtures-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixtureRoot
$candidate = Join-Path $fixtureRoot 'candidates'
$null = New-Item -ItemType Directory -Path $candidate
$dllBytes = [Text.Encoding]::UTF8.GetBytes('new candidate core DLL representative bytes')
[IO.File]::WriteAllBytes((Join-Path $candidate 'NewCore.dll'), $dllBytes)
$hash = (Get-FileHash -LiteralPath (Join-Path $candidate 'NewCore.dll') -Algorithm SHA256).Hash
$destination = 'plugins/NewCore/NewCore.dll'
$manifestPath = Join-Path $fixtureRoot 'manifest.json'
@{Schema=1; Files=@(@{Destination=$destination; Source='NewCore.dll'; Sha256=$hash})} |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
function Archive([string]$Name, [object[]]$Entries) {
    $path = Join-Path $fixtureRoot ($Name + '.zip')
    $zip = [IO.Compression.ZipFile]::Open($path,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($item in $Entries) {
            $entry = $zip.CreateEntry([string]$item.Path)
            $stream = $entry.Open()
            try { $stream.Write($item.Bytes,0,$item.Bytes.Length) } finally { $stream.Dispose() }
        }
    } finally { $zip.Dispose() }
    return $path
}
$passed=0
function Expect([string]$Name, [object[]]$Entries, [bool]$Accept) {
    $zipPath = Archive $Name $Entries
    $didAccept=$false
    try {
        $result = & (Join-Path $PSScriptRoot 'Verify-BepInExArchive.ps1') -CandidateDirectory $candidate -ArchivePath $zipPath -ManifestPath $manifestPath
        $didAccept = $result.Passed
    } catch {
        if ($Accept) { throw }
        Write-Output "$Name rejected: $($_.Exception.Message)"
    }
    if ($didAccept -ne $Accept) { throw "Fixture failed: $Name expected acceptance=$Accept" }
    $script:passed++
}
try {
    $good = @{Path=$destination; Bytes=$dllBytes}
    Expect 'valid-inside-BepInEx' @($good) $true
    Expect 'wrong-root' @(@{Path=('BepInEx/'+$destination); Bytes=$dllBytes}) $false
    Expect 'traversal' @(@{Path='../escape.dll'; Bytes=$dllBytes}) $false
    Expect 'backslash-traversal' @(@{Path='plugins\..\escape.dll'; Bytes=$dllBytes}) $false
    Expect 'duplicate-case' @($good,@{Path=$destination.ToUpperInvariant(); Bytes=$dllBytes}) $false
    Expect 'missing-binary' @(@{Path='plugins/'; Bytes=[byte[]]@()}) $false
    Expect 'unexpected-binary' @($good,@{Path='plugins/Unexpected.dll'; Bytes=$dllBytes}) $false
    Expect 'wrong-binary-bytes' @(@{Path=$destination; Bytes=[Text.Encoding]::UTF8.GetBytes('wrong version')}) $false
    Expect 'absolute-path' @(@{Path='C:/bad.dll'; Bytes=$dllBytes}) $false
    Expect 'trailing-dot-alias' @(@{Path='plugins/NewCore/NewCore.dll.'; Bytes=$dllBytes}) $false
    Expect 'file-directory-collision' @($good,@{Path='plugins/NewCore/NewCore.dll/'; Bytes=[byte[]]@()}) $false
    [IO.File]::WriteAllBytes((Join-Path $candidate 'Unmanifested.dll'),$dllBytes)
    Expect 'candidate-binary-missing-manifest' @($good) $false
    Write-Output "Release gate: $passed fixtures passed. No live profile accessed."
} finally {
    $resolved = [IO.Path]::GetFullPath($fixtureRoot)
    $prefix = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar + 'fixtures-'
    if ($resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
