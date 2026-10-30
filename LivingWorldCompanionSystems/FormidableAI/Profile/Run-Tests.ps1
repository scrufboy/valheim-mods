$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$out=Join-Path $root 'profile-tests.exe'
$csc='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /langversion:5 /out:$out (Join-Path $root 'CharacterCombatProfile.cs') (Join-Path $root '..\Classes\ClassCapabilities.cs') (Join-Path $root 'CharacterCombatProfileTests.cs')
if($LASTEXITCODE-ne 0){throw 'profile tests compile failed'}
try{& $out;if($LASTEXITCODE-ne 0){throw 'profile tests failed'}}finally{Remove-Item -Force -ErrorAction SilentlyContinue $out}
