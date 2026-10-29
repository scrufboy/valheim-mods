$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $env:TEMP 'lwc-live-core-adapter-tests.exe'
$frameworkCompiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compiler = Get-Command csc.exe -ErrorAction SilentlyContinue
$compilerPath = if ($compiler) { $compiler.Source } elseif (Test-Path -LiteralPath $frameworkCompiler) { $frameworkCompiler } else { $null }
if ($compilerPath) {
  & $compilerPath /nologo /warnaserror+ /out:$out (Join-Path $here 'EncounterAdapterPolicy.cs') (Join-Path $here 'EncounterAdapterPolicyTests.cs')
  if ($LASTEXITCODE) { exit $LASTEXITCODE }
  & $out
  exit $LASTEXITCODE
}
$dotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
if (!$dotnet) { throw 'Neither csc.exe nor dotnet.exe is available.' }
$scratch = Join-Path $env:TEMP 'lwc-live-core-adapter-tests'
New-Item -ItemType Directory -Force $scratch | Out-Null
Copy-Item (Join-Path $here 'EncounterAdapterPolicy.cs') $scratch -Force
Copy-Item (Join-Path $here 'EncounterAdapterPolicyTests.cs') $scratch -Force
& $dotnet.Source new console --force --framework net8.0 --output $scratch | Out-Null
Remove-Item (Join-Path $scratch 'Program.cs')
& $dotnet.Source run --project $scratch
exit $LASTEXITCODE
