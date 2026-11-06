param([string]$Compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe')
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$output = Join-Path ([System.IO.Path]::GetTempPath()) ('FriendInNeedContractTests-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & $Compiler /nologo /warnaserror+ /out:$output (Join-Path $here 'FriendInNeedContract.cs') (Join-Path $here 'FriendInNeedContractTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Friend in Need contract compilation failed' }
    & $output
    if ($LASTEXITCODE -ne 0) { throw 'Friend in Need contract tests failed' }
}
finally {
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Force }
}
