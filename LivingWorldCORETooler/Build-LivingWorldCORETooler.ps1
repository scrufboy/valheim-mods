param(
    [string]$ManagedDirectory = 'D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$BepInExDirectory = 'C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\runtime\BepInEx\core',
    [string]$CompatibilityForkDirectory = 'C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\ValheimToolerOwnership\BepInEx\plugins\ValheimToolerCompatibilityFork',
    [string]$CecilPath = 'C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\Mono.Cecil.dll'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$build = Join-Path $root 'build'
$package = Join-Path $root 'package'
$dist = Join-Path $root 'dist'
$plugin = Join-Path $package 'plugins\LivingWorldCORETooler'
$packageConfig = Join-Path $package 'config'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources = @(
    (Join-Path $root 'src\LivingWorldToolerExtension.cs'),
    (Join-Path $root 'src\LivingWorldTrainerPanel.cs'),
    (Join-Path $root 'src\LivingWorldToolerModules.cs'),
    (Join-Path $root 'src\LivingWorldSiteSurvey.cs')
)
$coreVersionSource = Join-Path $root 'src\CoreVersionInfo.cs'
$coreInput = Join-Path $CompatibilityForkDirectory 'ValheimTooler.dll'
$coreOutput = Join-Path $build 'LivingWorld.CORE.Tooler.Core.dll'
$versionCarrierDirectory = Join-Path $build 'version-carrier'
$versionCarrier = Join-Path $versionCarrierDirectory 'LivingWorld.CORE.Tooler.Core.dll'
$output = Join-Path $build 'LivingWorld.CORE.Tooler.dll'
$archive = Join-Path $dist 'scrufboy-LivingWorldCORETooler-2.6.3.zip'
$icon = Join-Path $root 'icon.png'
$iconBase64 = Join-Path $root 'icon.png.base64'
$raidConfig = Join-Path $root 'config\custom_raids.supplemental.lwc_formidable_souls.cfg'

if (Test-Path -LiteralPath $build) {
    $resolvedBuild = (Resolve-Path -LiteralPath $build).Path
    if (-not $resolvedBuild.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean build outside project root: $resolvedBuild"
    }
    Remove-Item -LiteralPath $resolvedBuild -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $build | Out-Null
if (Test-Path -LiteralPath $package) {
    $resolvedPackage = (Resolve-Path -LiteralPath $package).Path
    if (-not $resolvedPackage.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean package outside project root: $resolvedPackage"
    }
    Remove-Item -LiteralPath $resolvedPackage -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $plugin | Out-Null
New-Item -ItemType Directory -Force -Path $packageConfig | Out-Null
if (Test-Path -LiteralPath $dist) {
    $resolvedDist = (Resolve-Path -LiteralPath $dist).Path
    if (-not $resolvedDist.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean dist outside project root: $resolvedDist"
    }
    Remove-Item -LiteralPath $resolvedDist -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $dist | Out-Null

if (-not (Test-Path -LiteralPath $icon -PathType Leaf) -and (Test-Path -LiteralPath $iconBase64 -PathType Leaf)) {
    [IO.File]::WriteAllBytes($icon, [Convert]::FromBase64String((Get-Content -LiteralPath $iconBase64 -Raw).Trim()))
}

foreach ($path in @($compiler) + $sources + @($coreVersionSource, $icon, $raidConfig, $CecilPath, $coreInput, (Join-Path $CompatibilityForkDirectory 'SharpConfig.dll'))) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing build input: $path"
    }
}

if (-not ('LivingWorld.Build.NativeVersionResource' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LivingWorld.Build
{
    public static class NativeVersionResource
    {
        private const uint LoadLibraryAsDataFile = 0x00000002;
        private static readonly IntPtr VersionResourceType = new IntPtr(16);
        private static readonly IntPtr VersionResourceName = new IntPtr(1);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string fileName, IntPtr file, uint flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SizeofResource(IntPtr module, IntPtr resource);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadResource(IntPtr module, IntPtr resource);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LockResource(IntPtr resourceData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr module);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr BeginUpdateResource(string fileName, bool deleteExistingResources);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool UpdateResource(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] data, uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool EndUpdateResource(IntPtr update, bool discard);

        public static void ReplaceVersionResource(string targetFile, string versionCarrierFile)
        {
            byte[] versionData = ReadVersionResource(versionCarrierFile);
            IntPtr update = BeginUpdateResource(targetFile, true);
            if (update == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "BeginUpdateResource failed.");

            bool committed = false;
            try
            {
                if (!UpdateResource(update, VersionResourceType, VersionResourceName, 0, versionData, (uint)versionData.Length))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateResource failed.");
                if (!EndUpdateResource(update, false))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "EndUpdateResource failed.");
                committed = true;
            }
            finally
            {
                if (!committed)
                    EndUpdateResource(update, true);
            }
        }

        private static byte[] ReadVersionResource(string fileName)
        {
            IntPtr module = LoadLibraryEx(fileName, IntPtr.Zero, LoadLibraryAsDataFile);
            if (module == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadLibraryEx failed.");
            try
            {
                IntPtr resource = FindResource(module, VersionResourceName, VersionResourceType);
                if (resource == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "VERSION resource was not found.");
                uint size = SizeofResource(module, resource);
                IntPtr loaded = LoadResource(module, resource);
                IntPtr bytes = LockResource(loaded);
                if (size == 0 || loaded == IntPtr.Zero || bytes == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "VERSION resource could not be loaded.");
                byte[] result = new byte[size];
                Marshal.Copy(bytes, result, 0, checked((int)size));
                return result;
            }
            finally
            {
                FreeLibrary(module);
            }
        }
    }
}
'@
}

New-Item -ItemType Directory -Force -Path $versionCarrierDirectory | Out-Null
& $compiler /nologo /target:library "/out:$versionCarrier" $coreVersionSource
if ($LASTEXITCODE -ne 0) {
    throw 'Living World C.O.R.E. Tooler version resource compilation failed.'
}

Add-Type -Path $CecilPath
$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($coreInput)
try {
    $core.Name.Name = 'LivingWorld.CORE.Tooler.Core'
    $core.Name.Version = [Version]'2.6.3.0'
    $core.MainModule.Name = 'LivingWorld.CORE.Tooler.Core.dll'

    $metadata = @{
        'System.Reflection.AssemblyTitleAttribute' = 'Living World C.O.R.E. Tooler Core'
        'System.Reflection.AssemblyDescriptionAttribute' = 'Living World C.O.R.E. Tooler feature and interface core'
        'System.Reflection.AssemblyCompanyAttribute' = 'Brian / scrufboy'
        'System.Reflection.AssemblyProductAttribute' = 'Living World C.O.R.E. Tooler'
        'System.Reflection.AssemblyCopyrightAttribute' = 'Living World integration Copyright Brian / scrufboy'
        'System.Reflection.AssemblyVersionAttribute' = '2.6.3.0'
        'System.Reflection.AssemblyFileVersionAttribute' = '2.6.3.0'
    }
    foreach ($attribute in $core.CustomAttributes) {
        if ($metadata.ContainsKey($attribute.AttributeType.FullName) -and $attribute.ConstructorArguments.Count -gt 0) {
            $argumentType = $attribute.ConstructorArguments[0].Type
            $attribute.ConstructorArguments[0] = [Mono.Cecil.CustomAttributeArgument]::new($argumentType, $metadata[$attribute.AttributeType.FullName])
        }
    }

    $stringReplacements = @{
        'config_vt.path' = 'config_lwc_tooler.path'
        'valheimtooler_settings.cfg' = 'livingworldcore_tooler.cfg'
        'valheimtooler_internal.cfg' = 'livingworldcore_tooler_internal.cfg'
        'ValheimTooler' = 'LivingWorld.CORE.Tooler.Core'
    }
    $types = New-Object System.Collections.Generic.Stack[Mono.Cecil.TypeDefinition]
    foreach ($type in $core.MainModule.Types) { $types.Push($type) }
    while ($types.Count -gt 0) {
        $type = $types.Pop()
        foreach ($nested in $type.NestedTypes) { $types.Push($nested) }
        foreach ($method in $type.Methods) {
            if (-not $method.HasBody) { continue }
            foreach ($instruction in $method.Body.Instructions) {
                if ($instruction.OpCode.Code -ne [Mono.Cecil.Cil.Code]::Ldstr) { continue }
                $value = [string]$instruction.Operand
                if ($stringReplacements.ContainsKey($value)) {
                    $instruction.Operand = $stringReplacements[$value]
                }
                elseif ($value.StartsWith('[ValheimTooler')) {
                    $instruction.Operand = $value.Replace('[ValheimTooler', '[Living World C.O.R.E. Tooler')
                }
            }
        }
    }
    $core.Write($coreOutput)
}
finally {
    $core.Dispose()
}

[LivingWorld.Build.NativeVersionResource]::ReplaceVersionResource($coreOutput, $versionCarrier)
$coreVersionInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($coreOutput)
if ($coreVersionInfo.CompanyName -ne 'Brian / scrufboy' -or
    $coreVersionInfo.ProductName -ne 'Living World C.O.R.E. Tooler' -or
    $coreVersionInfo.FileVersion -ne '2.6.3.0') {
    throw 'The rebranded core still contains incorrect Windows file metadata.'
}

$references = @(
    (Join-Path $BepInExDirectory 'BepInEx.dll'),
    (Join-Path $BepInExDirectory '0Harmony.dll'),
    (Join-Path $ManagedDirectory 'netstandard.dll'),
    (Join-Path $ManagedDirectory 'assembly_valheim.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.CoreModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $ManagedDirectory 'UnityEngine.PhysicsModule.dll'),
    $coreOutput
)
foreach ($path in $references) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing reference: $path"
    }
}

& $compiler /nologo /langversion:5 /warn:4 /warnaserror+ /optimize+ /target:library "/out:$output" ($references | ForEach-Object { "/reference:$_" }) $sources
if ($LASTEXITCODE -ne 0) {
    throw 'Living World C.O.R.E. Tooler compilation failed.'
}

& (Join-Path $root 'Test-InterfaceContract.ps1') -ProjectRoot $root
if ($LASTEXITCODE -ne 0) {
    throw 'Living World C.O.R.E. Tooler interface contract failed.'
}

& (Join-Path $root 'Test-SiteSurveyContracts.ps1') -ProjectRoot $root
if ($LASTEXITCODE -ne 0) {
    throw 'Living World Site Survey contract failed.'
}

& (Join-Path $root 'Test-FormidableRaidIntegration.ps1')
if ($LASTEXITCODE -ne 0) {
    throw 'Living World Formidable raid integration contract failed.'
}

Copy-Item -LiteralPath $output, $coreOutput, (Join-Path $CompatibilityForkDirectory 'SharpConfig.dll') -Destination $plugin -Force
Copy-Item -LiteralPath $raidConfig -Destination $packageConfig -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md'), (Join-Path $root 'CHANGELOG.md'), (Join-Path $root 'manifest.json'), (Join-Path $root 'LICENSE'), $icon -Destination $package -Force

$builtCore = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($coreOutput)
$builtPlugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($output)
try {
    $pluginTypes = @($builtPlugin.MainModule.Types | Where-Object {
        $_.CustomAttributes.AttributeType.FullName -contains 'BepInEx.BepInPlugin'
    })
    if ($pluginTypes.Count -ne 1) { throw "Expected one Living World plugin identity; found $($pluginTypes.Count)." }
    if ($builtCore.Name.Name -ne 'LivingWorld.CORE.Tooler.Core') { throw 'Core assembly was not rebranded.' }
    if (Test-Path -LiteralPath (Join-Path $plugin 'ValheimToolerMod.dll')) { throw 'Legacy ValheimTooler plugin identity leaked into the package.' }
    if ($coreVersionInfo.CompanyName -eq 'Astropilot' -or $coreVersionInfo.ProductName -eq 'ValheimTooler') { throw 'Legacy Tooler branding leaked into the core file metadata.' }
}
finally {
    $builtCore.Dispose()
    $builtPlugin.Dispose()
}

Compress-Archive -Path (Join-Path $package '*') -DestinationPath $archive -CompressionLevel Optimal

[ordered]@{
    Product = 'Living World C.O.R.E. Tooler — Brian / scrufboy'
    PackageVersion = '2.6.3'
    PluginGuid = 'com.scrufboy.livingworldcore.tooler'
    Status = 'PASS'
    Scope = 'Offline compilation, owned plugin identity, rebranded core, original feature-surface, and isolated Site Survey verification'
    Runtime = 'NOT RUN'
    PluginSha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
    CoreSha256 = (Get-FileHash -LiteralPath $coreOutput -Algorithm SHA256).Hash
    PackageSha256 = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
    Package = $archive
    LegacyPluginIncluded = $false
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $build 'build-verification.json') -Encoding UTF8

Get-Content -LiteralPath (Join-Path $build 'build-verification.json')
