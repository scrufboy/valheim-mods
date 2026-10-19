param(
    [string]$AssemblyPath='D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll',
    [string]$CecilPath='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\independent-core\tools\Mono.Cecil.dll'
)
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $AssemblyPath).Path)
$checks=0
function Require([bool]$pass,[string]$reason){if(-not $pass){throw "Migration native contract FAIL: $reason"};$script:checks++}
function Body([string]$type,[string]$method){
    $definition=$assembly.MainModule.Types | Where-Object FullName -eq $type
    $methods=@($definition.Methods | Where-Object Name -eq $method)
    Require ($methods.Count -eq 1 -and $methods[0].HasBody) "$type.$method unique body"
    ($methods[0].Body.Instructions | ForEach-Object ToString) -join "`n"
}
try {
    Require ((Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash -eq '94998885B103D6E5F5480123EAEE0A093E82AF2DDEEEE37536C15BF555F24F3A') 'Reviewed native assembly changed; reinspection required'
    $create=Body ZNetScene CreateObject
    foreach($target in @('ZDO::GetPrefab()','ZDO::GetPosition()','ZDO::GetRotation()','ZNetView::m_initZDO','UnityEngine.Object::Instantiate')){
        Require ($create.Contains($target)) "CreateObject retains native route through $target"
    }
    $awake=Body ZNetView Awake
    Require ($awake.Contains('ldsfld ZDO ZNetView::m_initZDO') -and $awake.Contains('stfld ZDO ZNetView::m_zdo')) 'ZNetView consumes existing init ZDO'
    $follow=Body MonsterAI SetFollowTarget
    Require ($follow.Contains('stfld UnityEngine.GameObject MonsterAI::m_follow')) 'Follow is native runtime object field'
    Require (-not $follow.Contains('ZDO::')) 'Follow setter has no persistence (not evidence of restored following)'
    $patrol=Body BaseAI GetPatrolPoint
    Require ($patrol.Contains('ZDOVars::s_patrol') -and $patrol.Contains('ZDOVars::s_patrolPoint')) 'Native patrol reads saved fields'
    $health=Body Character GetHealth
    Require ($health.Contains('ZDOVars::s_health') -and $health.Contains('ZDO::GetFloat')) 'Native health reads saved field'
    $maxHealth=Body Character GetMaxHealth
    Require ($maxHealth.Contains('ZDOVars::s_maxHealth') -and $maxHealth.Contains('ZDO::GetFloat')) 'Native max health reads saved field'
    $start=Body Humanoid Start
    Require ($start.Contains('Character::IsPlayer()') -and $start.Contains('Humanoid::GiveDefaultItems()')) 'Native non-player defaults are not saved arbitrary inventory'
    "HumanMigrationContracts PASS: $checks read-only native IL checks; Unity saved actor load/migration NOT RUN."
} finally {$assembly.Dispose()}
