param(
    [Parameter(Mandatory=$true)][string]$SourceDll,
    [Parameter(Mandatory=$true)][string]$OutputDll,
    [Parameter(Mandatory=$true)][string]$CecilPath
)
$ErrorActionPreference = 'Stop'
if ([IO.Path]::GetFullPath($SourceDll) -eq [IO.Path]::GetFullPath($OutputDll)) { throw 'Stage the labelled DLL separately.' }
Add-Type -Path $CecilPath
function Get-Types($Types) { foreach ($t in $Types) { $t; Get-Types $t.NestedTypes } }
function Get-Code($a) {
    $lines = foreach ($t in (Get-Types $a.MainModule.Types)) {
        foreach ($m in $t.Methods) {
            $m.FullName
            if ($m.HasBody) { foreach ($i in $m.Body.Instructions) { $i.ToString() } }
        }
    }
    $lines -join "`n"
}
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($SourceDll)
try {
    $before = Get-Code $a
    $plugins = @(Get-Types $a.MainModule.Types | ForEach-Object { $_.CustomAttributes } | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' })
    if ($plugins.Count -ne 1) { throw 'Expected exactly one plugin identity.' }
    $attr = $plugins[0]
    if ($attr.ConstructorArguments[0].Value -ne 'fantu.sagesvault') { throw 'Unexpected compatibility GUID.' }
    $attr.ConstructorArguments[1] = [Mono.Cecil.CustomAttributeArgument]::new($attr.ConstructorArguments[1].Type, 'LWC Souls Garment03 Art Test')
    $attr.ConstructorArguments[2] = [Mono.Cecil.CustomAttributeArgument]::new($attr.ConstructorArguments[2].Type, '0.1.4')
    $a.MainModule.Name = 'LWC.SoulsArtTest.dll'
    $null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputDll)
    $a.Write($OutputDll)
} finally { $a.Dispose() }
$b = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($OutputDll)
try {
    if ((Get-Code $b) -ne $before) { throw 'Method instructions changed during labelling.' }
    $plugins = @(Get-Types $b.MainModule.Types | ForEach-Object { $_.CustomAttributes } | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' })
    if ($plugins[0].ConstructorArguments[1].Value -ne 'LWC Souls Garment03 Art Test' -or $plugins[0].ConstructorArguments[2].Value -ne '0.1.4') { throw 'Plugin label verification failed.' }
    $hash = [Security.Cryptography.SHA256]::Create()
    try { $codeHash = [BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes((Get-Code $b)))).Replace('-', '') } finally { $hash.Dispose() }
    [ordered]@{ name='LWC Souls Garment03 Art Test'; version='0.1.4'; guid='fantu.sagesvault'; sourceDllSha256=(Get-FileHash -LiteralPath $SourceDll -Algorithm SHA256).Hash; outputDllSha256=(Get-FileHash -LiteralPath $OutputDll -Algorithm SHA256).Hash; methodInstructionsSha256=$codeHash; methodInstructionsUnchanged=$true; installed=$false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path (Split-Path -Parent $OutputDll) 'label-verification.json') -Encoding UTF8
    Write-Output 'PASS: startup label LWC Souls Garment03 Art Test 0.1.4; method instructions unchanged.'
} finally { $b.Dispose() }
