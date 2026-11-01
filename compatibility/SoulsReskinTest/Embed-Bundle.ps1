param(
    [Parameter(Mandatory=$true)][string]$SourceDll,
    [Parameter(Mandatory=$true)][string]$Bundle,
    [Parameter(Mandatory=$true)][string]$OutputDll,
    [Parameter(Mandatory=$true)][string]$CecilPath
)
$ErrorActionPreference = 'Stop'
if ([IO.Path]::GetFullPath($SourceDll) -eq [IO.Path]::GetFullPath($OutputDll)) {
    throw 'Output must be a staged copy, never the installed source.'
}
Add-Type -Path $CecilPath
function Get-Types($Types) {
    foreach ($t in $Types) { $t; Get-Types $t.NestedTypes }
}
function Get-CodeDigest($Assembly) {
    $lines = foreach ($t in (Get-Types $Assembly.MainModule.Types)) {
        foreach ($m in $t.Methods) {
            $m.FullName
            if ($m.HasBody) {
                foreach ($i in $m.Body.Instructions) { $i.ToString() }
                foreach ($v in $m.Body.Variables) { $v.ToString() }
                foreach ($e in $m.Body.ExceptionHandlers) {
                    '{0}|{1}|{2}|{3}|{4}|{5}|{6}' -f $e.HandlerType,$e.CatchType,$e.TryStart,$e.TryEnd,$e.HandlerStart,$e.HandlerEnd,$e.FilterStart
                }
            }
        }
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $hash = [Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($hash.ComputeHash($bytes)).Replace('-','') }
    finally { $hash.Dispose() }
}
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($SourceDll)
try {
    $before = Get-CodeDigest $a
    $resources = @($a.MainModule.Resources | Where-Object Name -eq 'SagesVault.sagerobes')
    if ($resources.Count -ne 1) { throw 'Expected one embedded SagesVault.sagerobes resource.' }
    $old = $resources[0]
    $index = $a.MainModule.Resources.IndexOf($old)
    $replacement = [Mono.Cecil.EmbeddedResource]::new($old.Name, $old.Attributes, [IO.File]::ReadAllBytes($Bundle))
    $a.MainModule.Resources[$index] = $replacement
    $null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputDll)
    $a.Write($OutputDll)
} finally { $a.Dispose() }
$check = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($OutputDll)
try {
    $after = Get-CodeDigest $check
    if ($before -ne $after) { throw 'Registration code changed unexpectedly.' }
    $actual = @($check.MainModule.Resources | Where-Object Name -eq 'SagesVault.sagerobes')[0].GetResourceData()
    $hash = [Security.Cryptography.SHA256]::Create()
    try {
        $resourceHash = [BitConverter]::ToString($hash.ComputeHash($actual)).Replace('-','')
    } finally { $hash.Dispose() }
    if ($resourceHash -ne (Get-FileHash -LiteralPath $Bundle -Algorithm SHA256).Hash) { throw 'Embedded bundle differs.' }
    [ordered]@{
        sourceDllSha256 = (Get-FileHash -LiteralPath $SourceDll -Algorithm SHA256).Hash
        outputDllSha256 = (Get-FileHash -LiteralPath $OutputDll -Algorithm SHA256).Hash
        embeddedBundleSha256 = $resourceHash
        methodCodeSha256 = $after
        methodCodeUnchanged = $true
        pluginIdentityPreserved = $true
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path (Split-Path -Parent $Bundle) 'assembly-verification.json') -Encoding UTF8
    Write-Output 'PASS: embedded reskinned bundle verified; all method instructions unchanged.'
} finally { $check.Dispose() }
