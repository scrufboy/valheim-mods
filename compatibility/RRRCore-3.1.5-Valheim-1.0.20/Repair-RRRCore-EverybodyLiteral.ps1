param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$CecilPath,
    [Parameter(Mandatory=$true)][string]$BackupPath,
    [string]$ExpectedOriginalSha256='13EA7364B76D8291C5FC79CDBF489F9D4BE0533E05A5CE6C4FCD0EE896054CBE'
)

$ErrorActionPreference='Stop'

foreach($path in @($AssemblyPath,$CecilPath)){
    if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw "Required file is missing: $path"}
}
$assembly=[IO.Path]::GetFullPath($AssemblyPath)
$backup=[IO.Path]::GetFullPath($BackupPath)
if($assembly -eq $backup){throw 'Backup must not overwrite the active assembly.'}

Add-Type -Path $CecilPath

function Get-AllTypes([Mono.Cecil.TypeDefinition]$type){
    $type
    foreach($nested in $type.NestedTypes){Get-AllTypes $nested}
}

function Get-SendMethod([Mono.Cecil.AssemblyDefinition]$definition){
    $types=@($definition.MainModule.Types | ForEach-Object {Get-AllTypes $_})
    $matches=@($types | Where-Object {$_.FullName -eq 'ServerSync.ConfigSync'} |
        ForEach-Object {$_.Methods} | Where-Object {
            $_.Name -eq 'sendZPackage' -and $_.HasBody -and $_.Parameters.Count -eq 2 -and
            $_.Parameters[0].ParameterType.FullName -eq 'System.Int64' -and
            $_.Parameters[1].ParameterType.FullName -eq 'ZPackage'
        })
    if($matches.Count -ne 1){throw "Expected exactly one ServerSync.ConfigSync.sendZPackage(long,ZPackage) body; found $($matches.Count)."}
    return $matches[0]
}

$originalHash=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash
$temporary="$assembly.lwc-repair.tmp"
if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}
$definition=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($assembly)
try{
    $method=Get-SendMethod $definition
    $legacy=@($method.Body.Instructions | Where-Object {
        $_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldsfld -and
        $_.Operand -is [Mono.Cecil.FieldReference] -and
        $_.Operand.DeclaringType.FullName -eq 'ZRoutedRpc' -and
        $_.Operand.Name -eq 'Everybody'
    })
    $repaired=@($method.Body.Instructions | Where-Object {
        $_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldc_I8 -and [Int64]$_.Operand -eq 0
    })
    if($legacy.Count -eq 0 -and $repaired.Count -eq 1){
        Write-Output "RRRCore compatibility instruction already repaired. SHA256: $originalHash"
        exit 0
    }
    if($legacy.Count -ne 1){throw "Expected exactly one ZRoutedRpc.Everybody load; found $($legacy.Count)."}
    if($ExpectedOriginalSha256 -and $originalHash -ne $ExpectedOriginalSha256){
        throw "Refusing an unknown RRRCore binary. Expected $ExpectedOriginalSha256; found $originalHash."
    }
    $backupDirectory=Split-Path $backup -Parent
    $null=New-Item -ItemType Directory -Force -Path $backupDirectory
    if(Test-Path -LiteralPath $backup){
        $backupHash=(Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash
        if($backupHash -ne $originalHash){throw "Existing backup does not match the original input; refusing to overwrite it: $backup"}
    } else {
        Copy-Item -LiteralPath $assembly -Destination $backup
    }

    $legacy[0].OpCode=[Mono.Cecil.Cil.OpCodes]::Ldc_I8
    $legacy[0].Operand=[Int64]0
    $definition.Write($temporary)
} catch {
    if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}
    throw
} finally {
    $definition.Dispose()
}
try {
    [IO.File]::Move($temporary,$assembly,$true)
} finally {
    if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}
}

$verified=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($assembly)
try{
    $method=Get-SendMethod $verified
    $legacy=@($method.Body.Instructions | Where-Object {
        $_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldsfld -and
        $_.Operand -is [Mono.Cecil.FieldReference] -and
        $_.Operand.DeclaringType.FullName -eq 'ZRoutedRpc' -and $_.Operand.Name -eq 'Everybody'
    })
    $repaired=@($method.Body.Instructions | Where-Object {
        $_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldc_I8 -and [Int64]$_.Operand -eq 0
    })
    if($legacy.Count -ne 0 -or $repaired.Count -ne 1){
        throw "Post-write verification failed: legacy=$($legacy.Count), repaired=$($repaired.Count)."
    }
} finally {
    $verified.Dispose()
}

$newHash=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash
Write-Output "RRRCore local Valheim 1.0.20 repair PASS. Original SHA256: $originalHash"
Write-Output "Repaired SHA256: $newHash"
Write-Output "Backup: $backup"
