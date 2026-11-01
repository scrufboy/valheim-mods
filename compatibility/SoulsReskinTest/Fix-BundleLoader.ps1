param(
    [Parameter(Mandatory=$true)][string]$SourceDll,
    [Parameter(Mandatory=$true)][string]$OutputDll,
    [Parameter(Mandatory=$true)][string]$CecilPath
)
$ErrorActionPreference = 'Stop'
Add-Type -Path $CecilPath
if ([IO.Path]::GetFullPath($SourceDll) -eq [IO.Path]::GetFullPath($OutputDll)) { throw 'Stage output separately.' }
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($SourceDll)
function Get-Types($ts) { foreach ($t in $ts) { $t; Get-Types $t.NestedTypes } }
function Get-MethodText($m) { if ($m.HasBody) { ($m.Body.Instructions | ForEach-Object ToString) -join "`n" } }
try {
    $module = $a.MainModule
    $methods = @(Get-Types $module.Types | ForEach-Object { $_.Methods })
    $target = @($methods | Where-Object Name -eq 'GetAssetBundleFromResources')
    if ($target.Count -ne 1) { throw 'Expected one embedded bundle loader.' }
    $loader = $target[0]
    $calls = @($loader.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'LoadFromStream' })
    if ($calls.Count -ne 1) { throw 'Expected one LoadFromStream call.' }
    $oldCall = $calls[0].Operand
    $baseline = @{}
    foreach ($m in $methods) { if ($m -ne $loader) { $baseline[$m.FullName] = Get-MethodText $m } }
    $streamType = $oldCall.Parameters[0].ParameterType
    $memoryType = [Mono.Cecil.TypeReference]::new('System.IO','MemoryStream',$module,$module.TypeSystem.Object.Scope)
    $byteArrayType = [Mono.Cecil.ArrayType]::new($module.TypeSystem.Byte)
    $ctor = [Mono.Cecil.MethodReference]::new('.ctor',$module.TypeSystem.Void,$memoryType)
    $ctor.HasThis = $true
    $copy = [Mono.Cecil.MethodReference]::new('CopyTo',$module.TypeSystem.Void,$streamType)
    $copy.HasThis = $true
    $copy.Parameters.Add([Mono.Cecil.ParameterDefinition]::new($streamType))
    $toArray = [Mono.Cecil.MethodReference]::new('ToArray',$byteArrayType,$memoryType)
    $toArray.HasThis = $true
    $dispose = [Mono.Cecil.MethodReference]::new('Dispose',$module.TypeSystem.Void,$streamType)
    $dispose.HasThis = $true
    $loadMemory = [Mono.Cecil.MethodReference]::new('LoadFromMemory',$oldCall.ReturnType,$oldCall.DeclaringType)
    $loadMemory.Parameters.Add([Mono.Cecil.ParameterDefinition]::new($byteArrayType))
    $helper = [Mono.Cecil.MethodDefinition]::new('LoadEmbeddedBundleFromMemory',
        ([Mono.Cecil.MethodAttributes]::Private -bor [Mono.Cecil.MethodAttributes]::Static),$oldCall.ReturnType)
    $helper.Parameters.Add([Mono.Cecil.ParameterDefinition]::new('source',[Mono.Cecil.ParameterAttributes]::None,$streamType))
    $helper.Body.InitLocals = $true
    $helper.Body.Variables.Add([Mono.Cecil.Cil.VariableDefinition]::new($memoryType))
    $helper.Body.Variables.Add([Mono.Cecil.Cil.VariableDefinition]::new($byteArrayType))
    $il = $helper.Body.GetILProcessor()
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Newobj,$ctor))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Stloc_0))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_0))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$copy))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_0))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$toArray))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Stloc_1))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_0))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$dispose))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_1))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Call,$loadMemory))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
    $loader.DeclaringType.Methods.Add($helper)
    $calls[0].Operand = $helper
    $attrs = @(Get-Types $module.Types | ForEach-Object { $_.CustomAttributes } | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' })
    $attrs[0].ConstructorArguments[2] = [Mono.Cecil.CustomAttributeArgument]::new($attrs[0].ConstructorArguments[2].Type,'0.1.3')
    $null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputDll)
    $a.Write($OutputDll)
} finally { $a.Dispose() }
$b = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($OutputDll)
try {
    $all = @(Get-Types $b.MainModule.Types | ForEach-Object { $_.Methods })
    foreach ($m in $all) {
        if ($baseline.ContainsKey($m.FullName) -and (Get-MethodText $m) -ne $baseline[$m.FullName]) { throw "Unrelated method changed: $($m.FullName)" }
    }
    $legacy = @($all | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'LoadFromStream' })
    if ($legacy.Count -ne 0) { throw 'A stream-backed bundle load remains.' }
    Write-Output 'PASS: memory-backed bundle loader; all unrelated method instructions unchanged; version 0.1.3.'
} finally { $b.Dispose() }
