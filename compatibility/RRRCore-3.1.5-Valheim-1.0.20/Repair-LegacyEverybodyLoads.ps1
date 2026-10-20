param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$CecilPath,
    [Parameter(Mandatory=$true)][string]$BackupPath,
    [Parameter(Mandatory=$true)][string]$ExpectedOriginalSha256,
    [Parameter(Mandatory=$true)][int]$ExpectedReferenceCount
)

$ErrorActionPreference='Stop'

foreach($path in @($AssemblyPath,$CecilPath)){
    if(-not (Test-Path -LiteralPath $path -PathType Leaf)){
        throw "Required file is missing: $path"
    }
}

$assembly=[IO.Path]::GetFullPath($AssemblyPath)
$backup=[IO.Path]::GetFullPath($BackupPath)
if($assembly -eq $backup){throw 'Backup must not overwrite the active assembly.'}

Add-Type -Path $CecilPath

function Get-AllTypes([Mono.Cecil.TypeDefinition]$type){
    $type
    foreach($nested in $type.NestedTypes){Get-AllTypes $nested}
}

function Get-LegacyLoads([Mono.Cecil.AssemblyDefinition]$definition){
    @($definition.MainModule.Types |
        ForEach-Object {Get-AllTypes $_} |
        ForEach-Object {$_.Methods} |
        Where-Object {$_.HasBody} |
        ForEach-Object {$_.Body.Instructions} |
        Where-Object {
            $_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldsfld -and
            $_.Operand -is [Mono.Cecil.FieldReference] -and
            $_.Operand.DeclaringType.FullName -eq 'ZRoutedRpc' -and
            $_.Operand.Name -eq 'Everybody'
        })
}

$originalHash=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash
$temporary="$assembly.lwc-legacy-everybody.tmp"
if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}

$definition=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($assembly)
try{
    $legacy=@(Get-LegacyLoads $definition)
    if($legacy.Count -eq 0){
        Write-Output "Assembly has no legacy ZRoutedRpc.Everybody loads: $assembly"
        exit 0
    }
    if($originalHash -ne $ExpectedOriginalSha256){
        throw "Refusing an unknown assembly. Expected $ExpectedOriginalSha256; found $originalHash."
    }
    if($legacy.Count -ne $ExpectedReferenceCount){
        throw "Expected $ExpectedReferenceCount legacy loads; found $($legacy.Count)."
    }

    $backupDirectory=Split-Path $backup -Parent
    $null=New-Item -ItemType Directory -Force -Path $backupDirectory
    if(Test-Path -LiteralPath $backup){
        $backupHash=(Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash
        if($backupHash -ne $originalHash){
            throw "Existing backup does not match the input; refusing to overwrite it: $backup"
        }
    } else {
        Copy-Item -LiteralPath $assembly -Destination $backup
    }

    foreach($instruction in $legacy){
        $instruction.OpCode=[Mono.Cecil.Cil.OpCodes]::Ldc_I8
        $instruction.Operand=[Int64]0
    }
    $definition.Write($temporary)
} catch {
    if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}
    throw
} finally {
    $definition.Dispose()
}

try{
    Move-Item -LiteralPath $temporary -Destination $assembly -Force
} finally {
    if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}
}

$verified=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($assembly)
try{
    $remaining=@(Get-LegacyLoads $verified)
    if($remaining.Count -ne 0){
        throw "Post-write verification failed: $($remaining.Count) legacy loads remain."
    }
} finally {
    $verified.Dispose()
}

$newHash=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash
Write-Output "Legacy ZRoutedRpc.Everybody repair PASS: $assembly"
Write-Output "Replaced instructions: $ExpectedReferenceCount"
Write-Output "Original SHA256: $originalHash"
Write-Output "Repaired SHA256: $newHash"
Write-Output "Backup: $backup"
