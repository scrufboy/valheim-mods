param([string]$Clone = (Join-Path $PSScriptRoot '..\..\..\lwc-core-norse-port'))
$ErrorActionPreference = 'Stop'
$commit = '9933763978fd7c29bfa9909e7ea8b6c47f86e421'
$checks = @(
  @('LivingWorldCORE/npc/src/CorePlugin.cs', 'ZNet.instance.IsServer()'),
  @('LivingWorldCORE/npc/src/CorePlugin.cs', 'GetAllZDOsWithPrefabIterative'),
  @('LivingWorldCORE/npc/src/CorePlugin.cs', 'ZNetScene.instance.FindInstance'),
  @('LivingWorldCORE/npc/src/Kernel/NpcKernel.cs', 'public static IEnumerable<NpcKernel> All'),
  @('LivingWorldCORE/npc/src/Kernel/NpcKernel.cs', 'public static event Action<NpcKernel, string> Removed'),
  @('LivingWorldCORE/npc/src/Kernel/NpcKernel.cs', 'public static event Action<NpcKernel, float, Character> Damaged'),
  @('LivingWorldCORE/npc/src/Kernel/NpcKernel.cs', 'public Character RecentAttacker(float seconds)'),
  @('LivingWorldCORE/npc/src/Kernel/NpcBody.cs', 'public bool IsOwner'),
  @('LivingWorldCORE/npc/src/Kernel/NpcBody.cs', 'public readonly ZNetView View')
)
foreach ($check in $checks) {
  $source = & git -C $Clone show "$commit`:$($check[0])"
  if ($LASTEXITCODE -or -not ($source | Select-String -SimpleMatch $check[1])) { throw "Missing proven signature: $($check[1])" }
}
Write-Host "Verified $($checks.Count) core signatures at $commit"
