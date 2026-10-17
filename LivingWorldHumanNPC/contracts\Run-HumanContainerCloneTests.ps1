param(
    [Parameter(Mandatory=$true)][string]$TestExecutable,
    [Parameter(Mandatory=$true)][string]$HumanDll,
    [Parameter(Mandatory=$true)][string]$ManagedDirectory,
    [string[]]$DependencyDirectories=@()
)
$ErrorActionPreference='Stop'
# The installed game metadata uses default interface implementations, which the
# classic .NET Framework runner cannot load. Use PowerShell 7's CoreCLR to execute
# this managed-only test. This does not load UnityPlayer or start the game.
if($PSVersionTable.PSEdition -ne 'Core'){
    throw 'Human container checks require PowerShell 7 (pwsh/CoreCLR), not Windows PowerShell/.NET Framework. No check has run.'
}
$executablePath=(Resolve-Path -LiteralPath $TestExecutable).Path
$dllPath=(Resolve-Path -LiteralPath $HumanDll).Path
$managedPath=(Resolve-Path -LiteralPath $ManagedDirectory).Path
$arguments=[System.Collections.Generic.List[string]]::new()
$arguments.Add($dllPath)
$arguments.Add($managedPath)
foreach($dependencyDirectory in $DependencyDirectories){$arguments.Add((Resolve-Path -LiteralPath $dependencyDirectory).Path)}
$testAssembly=[System.Reflection.Assembly]::LoadFrom($executablePath)
if($null -eq $testAssembly.EntryPoint){throw 'Container test executable has no managed entrypoint.'}
$result=$testAssembly.EntryPoint.Invoke($null,(,$arguments.ToArray()))
if($result -ne 0){throw "Human container clone tests failed with exit code $result"}
