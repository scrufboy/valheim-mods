param(
    [string]$ManagedDirectory='D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$BepInExDirectory='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\runtime\BepInEx\core'
)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$build=Join-Path $root 'build'
$package=Join-Path $root 'package'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force -Path $build | Out-Null
if(Test-Path -LiteralPath $package){
    $resolvedPackage=(Resolve-Path -LiteralPath $package).Path
    if(-not $resolvedPackage.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw "Refusing to clean package outside project root: $resolvedPackage"}
    Remove-Item -LiteralPath $resolvedPackage -Recurse -Force
}
New-Item -ItemType Directory -Force -Path (Join-Path $package 'plugins\LivingWorldCORETooler') | Out-Null
$source=Join-Path $root 'src\LivingWorldLabToolerPlugin.cs'
$iconSource=Join-Path (Split-Path $root -Parent) 'ValheimToolerOwnership\icon.png'
$refs=@(
    (Join-Path $BepInExDirectory 'BepInEx.dll'),
    (Join-Path $ManagedDirectory 'netstandard.dll'),
    (Join-Path $ManagedDirectory 'assembly_valheim.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.CoreModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.InputLegacyModule.dll')
)
foreach($path in @($source,$iconSource)+$refs){if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing build input: $path"}}
$dll=Join-Path $build 'LivingWorld.CORE.Tooler.dll'
& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:library "/out:$dll" ($refs|ForEach-Object{"/reference:$_"}) $source
if($LASTEXITCODE -ne 0){throw 'Lab Tooler compilation failed.'}
$text=Get-Content -LiteralPath $source -Raw
$required=@('lwcommands','lwparty','lwtest setup','lwtest spawn','lwtest migrate','lwtest follow','lwtest hold','lwtest move','lwtest combat','lwtest stop','lwtest status','lwtest cleanup','lwtest party prepare','lwtest party status','lwtest party locate','lwtest party recover','lwtest wave boarneck','lwtest wave greydwarf','lwtest wave go','lwtest wave status','lwtest wave stop','randomevent','stopevent','god','ghost','fly','nocost','heal','spawn','tod','env','resetenv','skiptime','save','genloc','terrain_level','terrain_smooth','terrain_raise','terrain_lower','terrain_reset','exploremap','resetmap','goto','tame','killall','removedrops')
$missing=@($required|Where-Object{$text.IndexOf($_,[StringComparison]::Ordinal) -lt 0})
if($missing.Count){throw "Command catalog incomplete: $($missing -join ', ')"}
Copy-Item -LiteralPath $dll -Destination (Join-Path $package 'plugins\LivingWorldCORETooler\LivingWorld.CORE.Tooler.dll') -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md'),(Join-Path $root 'CHANGELOG.md'),(Join-Path $root 'manifest.json') -Destination $package -Force
Copy-Item -LiteralPath $iconSource -Destination (Join-Path $package 'icon.png') -Force
$zip=Join-Path $root 'scrufboy-LivingWorldCORETooler-2.0.0.zip'
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal -Force
$verification=[ordered]@{Product='Living World C.O.R.E. Tooler';Package='scrufboy-LivingWorldCORETooler';Version='2.0.0';Status='PASS';Scope='Offline compilation and package/catalog verification only';Runtime='NOT RUN';Commands=$required;DllSha256=(Get-FileHash $dll -Algorithm SHA256).Hash;ZipSha256=(Get-FileHash $zip -Algorithm SHA256).Hash}
$verification|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding utf8
$verification|ConvertTo-Json -Depth 4

