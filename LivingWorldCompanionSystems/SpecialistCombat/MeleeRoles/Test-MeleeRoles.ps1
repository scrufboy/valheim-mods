$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$out=Join-Path $root 'MeleeRolePolicyTests.exe'
$compiler=(Get-Command csc.exe -ErrorAction SilentlyContinue).Source
if(-not $compiler){$compiler=Get-ChildItem 'C:\Windows\Microsoft.NET\Framework*\v4*\csc.exe' -ErrorAction SilentlyContinue|Sort-Object FullName -Descending|Select-Object -First 1 -ExpandProperty FullName}
if(-not $compiler){throw 'C# compiler not found.'}
try{
  & $compiler /nologo /warn:4 /warnaserror+ /optimize+ /target:exe "/out:$out" (Join-Path $root 'MeleeRolePolicy.cs') (Join-Path $root 'MeleeRoleProfiles.cs') (Join-Path $root 'MeleeRolePolicyTests.cs')
  if($LASTEXITCODE -ne 0){throw 'Melee role policy compilation failed.'}
  & $out
  if($LASTEXITCODE -ne 0){throw 'Melee role policy tests failed.'}
}finally{Remove-Item -LiteralPath $out -Force -ErrorAction SilentlyContinue}
