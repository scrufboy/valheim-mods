param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [string]$CecilPath='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\independent-core\tools\Mono.Cecil.dll'
)
# Independent read-only IL audit: no Unity code loaded or invoked.
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $AssemblyPath).Path)
$checks=0
function Require([bool]$pass,[string]$reason){if(-not $pass){throw "Human composition binary FAIL: $reason"};$script:checks++}
function WalkType($type){$type;foreach($nested in $type.NestedTypes){WalkType $nested}}
function Calls($method){@($method.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] })}
function FindMethod([string]$typeName,[string]$methodName){
    $type=@($types | Where-Object FullName -eq $typeName)
    Require ($type.Count -eq 1) "type $typeName"
    $method=@($type[0].Methods | Where-Object Name -eq $methodName)
    Require ($method.Count -eq 1 -and $method[0].HasBody) "method $typeName.$methodName"
    $method[0]
}
try {
    $types=@($assembly.MainModule.Types | ForEach-Object {WalkType $_})
    $methods=@($types.Methods | Where-Object HasBody)
    $allInstructions=@($methods | ForEach-Object {$_.Body.Instructions})
    $allCalls=@($allInstructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]})
    $strings=@($allInstructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr} | ForEach-Object {$_.Operand})
    Require (@($strings | Where-Object {$_ -match '(?i)dvergr|dverger'}).Count -eq 0) 'no rejected template identifier anywhere in compiled strings'
    $cloneSites=@($methods | Where-Object { @(Calls $_ | Where-Object {$_.Operand.Name -eq 'CreateClonedPrefab'}).Count -gt 0 })
    Require ($cloneSites.Count -eq 1 -and $cloneSites[0].DeclaringType.FullName -eq 'LivingWorld.ManualTest.ManualTestPlugin' -and
        $cloneSites[0].Name -eq 'Register' -and @(Calls $cloneSites[0] | Where-Object {$_.Operand.Name -eq 'CreateClonedPrefab'}).Count -eq 1) 'only the private alert effect uses Jotunn inactive-container cloning'
    Require (@($allCalls | Where-Object {$_.Operand.FullName -match 'AddComponent<Player>|System.Void Player::.ctor'}).Count -eq 0) 'no constructed Player component/lifecycle'
    Require (@($allCalls | Where-Object {$_.Operand.FullName -match 'System.IO.|System.Net.|System.Diagnostics.Process|ZNet::SetServer|Game::SetProfile|ZNetView::ClaimOwnership|ZNetView::ResetZDO'}).Count -eq 0) 'no file/network/profile launch/reset/ownership mutation path'
    Require ($assembly.MainModule.Resources.Count -eq 0) 'no bundled game/legacy resources'
    $policy=@($types | Where-Object FullName -eq LivingWorld.HumanNpc.HumanRigPolicy)[0]
    $migration=@($types | Where-Object FullName -eq LivingWorld.HumanNpc.HumanMigrationPolicy)[0]
    $acceptance=@($types | Where-Object FullName -eq LivingWorld.ManualTest.ManualAcceptance)[0]
    $plugin=@($types | Where-Object FullName -eq LivingWorld.ManualTest.ManualTestPlugin)[0]
    Require (($policy.Fields | Where-Object Name -eq PrefabId).Constant -eq 'LWC_HumanScout_v1' -and ($migration.Fields | Where-Object Name -eq Identity).Constant -eq 'LWC_HumanScout_v1' -and ($plugin.Fields | Where-Object Name -eq NpcId).Constant -eq 'LWC_HumanScout_v1') 'stable saved prefab identity agrees across registration and migration'
    Require (($policy.Fields | Where-Object Name -eq Revision).Constant -eq 'native-human-v1' -and ($migration.Fields | Where-Object Name -eq VisualRevision).Constant -eq 'native-human-v1') 'old human visual revision is unchanged and agrees with migration gate'
    Require (($acceptance.Fields | Where-Object Name -eq WorldName).Constant -eq 'LWC_HumanLab_v1' -and ($acceptance.Fields | Where-Object Name -eq ProfileName).Constant -eq 'LWC_Tester') 'same existing world and short character name retained'
    $create=FindMethod LivingWorld.HumanNpc.NativeHumanFactory Create
    $createCalls=@(Calls $create)
    Require (@($createCalls | Where-Object {$_.Operand.FullName -eq 'System.Void UnityEngine.GameObject::.ctor(System.String)'}).Count -eq 2) 'definition container plus fresh named GameObject root constructed explicitly'
    Require (@($createCalls | Where-Object {$_.Operand.Name -match 'Instantiate|GetPrefab|CreateClonedPrefab'}).Count -eq 0) 'fresh-root factory does not obtain or instantiate character/template prefab'
    foreach($component in @('UnityEngine.CapsuleCollider','UnityEngine.Rigidbody','ZNetView','ZSyncTransform','ZSyncAnimation','Humanoid','MonsterAI','LivingWorld.HumanNpc.HumanCompositionIdentity','LivingWorld.HumanNpc.HumanCompanion','LivingWorld.HumanNpc.HumanEquipmentReadiness','LivingWorld.HumanNpc.HumanCombatDefense','LivingWorld.HumanNpc.HumanEnvironmentStatus')){
        Require (@($createCalls | Where-Object {$_.Operand.FullName.Contains('AddComponent<'+$component+'>')}).Count -eq 1) "fresh explicit component $component"
    }
    $environment=FindMethod LivingWorld.HumanNpc.HumanEnvironmentStatus Update
    $environmentCalls=@(Calls $environment)
    Require (@($environmentCalls | Where-Object {$_.Operand.FullName -eq 'System.Boolean EnvMan::IsWet()'}).Count -eq 1) 'environment adapter reads native rain state'
    Require (@($environmentCalls | Where-Object {$_.Operand.FullName -match 'Cover::GetCoverForPoint'}).Count -eq 1) 'environment adapter reads native roof cover'
    Require (@($environmentCalls | Where-Object {$_.Operand.FullName -match 'ShieldGenerator::IsInsideShield'}).Count -eq 1) 'environment adapter reads native shield protection'
    Require (@($environmentCalls | Where-Object {$_.Operand.FullName -match 'SEMan::AddStatusEffect'}).Count -eq 1) 'environment adapter applies native Wet through SEMan once'
    Require (@($environmentCalls | Where-Object {$_.Operand.FullName -match 'SEMan::RemoveStatusEffect'}).Count -eq 0) 'environment adapter leaves native drying and water Wet untouched'
    $environmentType=@($types | Where-Object FullName -eq LivingWorld.HumanNpc.HumanEnvironmentStatus)[0]
    Require (@($environmentType.Fields | Where-Object {$_.Name -eq 'TriggerStartEffects' -and $_.FieldType.FullName -eq 'System.Reflection.MethodInfo'}).Count -eq 1) 'wet presentation caches the exact native start-effect method contract'
    Require (@($environment.Body.Instructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Stfld -and $_.Operand.FullName -match 'StatusEffect::m_effectsOnlyOnPlayer'}).Count -eq 1) 'wet presentation changes only the actor runtime status player-only gate'
    Require (@($environmentCalls | Where-Object {$_.Operand.FullName -match 'System.Reflection.MethodBase::Invoke'}).Count -eq 1) 'wet presentation retriggers the native networked start effect once'
    $apply=FindMethod LivingWorld.HumanNpc.NativeHumanFactory ApplyVisual
    $applyCalls=@(Calls $apply)
    Require (@($applyCalls | Where-Object {$_.Operand.Name -eq 'Instantiate'}).Count -eq 1) 'one human visual subtree instantiate in visual adapter'
    Require (@($applyCalls | Where-Object {$_.Operand.Name -match 'GetPrefab|CreateClonedPrefab'}).Count -eq 0) 'visual adapter does not fetch alternate native templates'
    Require (@($apply.Body.Instructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr -and $_.Operand -eq 'Visual'}).Count -ge 1) 'visual adapter explicitly selects native Visual subtree'
    $catalog=FindMethod LivingWorld.ManualTest.ManualTestPlugin NativeAppearanceChoices
    $catalogCalls=@(Calls $catalog)
    $catalogFields=@($catalog.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.FieldReference]} | ForEach-Object {$_.Operand.Name})
    Require (@($catalogCalls | Where-Object {$_.Operand.Name -eq 'GetAllItems'}).Count -eq 0) 'fresh identity never searches every customization item as a selectable base style'
    foreach($field in @('m_hairPrefabPrefix','m_hairPrefabCount','m_beardPrefabPrefix','m_beardPrefabCount')){
        Require ($catalogFields -notcontains $field) "catalog does not invent Player style metadata from NPC-random field $field"
    }
    Require (@($catalogCalls | Where-Object {$_.Operand.Name -eq 'GetItemPrefab'}).Count -eq 1) 'each native Player facial-hair entry is resolved through ObjectDB'
    $catalogStrings=@($catalog.Body.Instructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr} | ForEach-Object {$_.Operand})
    Require ($catalogStrings -contains 'attach' -and $catalogStrings -contains 'attach_skin') 'catalog requires a native visual attachment before identity generation'
    $facial=FindMethod LivingWorld.HumanNpc.HumanAppearance FacialHairAttached
    $facialFields=@($facial.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.FieldReference]} | ForEach-Object {$_.Operand.Name})
    foreach($field in @('HairInstance','BeardInstance','CurrentHairHash','CurrentBeardHash')){
        Require ($facialFields -contains $field) "appearance readiness verifies $field"
    }
    $refresh=FindMethod LivingWorld.HumanNpc.HumanAppearance RefreshNativeAppearance
    $refreshCalls=@(Calls $refresh | Where-Object {$_.Operand.Name -eq 'Invoke'})
    Require ($refreshCalls.Count -eq 2) 'appearance performs a construction pass and a material pass before commit'
    $render=FindMethod LivingWorld.HumanNpc.HumanAppearance AttachmentRenders
    $renderCalls=@(Calls $render)
    Require (@($renderCalls | Where-Object {$_.Operand.Name -eq 'GetComponentsInChildren'}).Count -eq 1) 'appearance readiness inspects the selected attachment renderers'
    Require (@($renderCalls | Where-Object {$_.Operand.Name -eq 'GetLODs'}).Count -ge 1) 'appearance readiness requires the selected attachment in the human LOD'
    Require (@($render.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'get_activeInHierarchy'}).Count -ge 2) 'appearance readiness rejects inactive attachment objects/renderers'
    $identity=@($types | Where-Object FullName -eq LivingWorld.HumanNpc.HumanCompositionIdentity)[0]
    Require (@($identity.Methods | Where-Object {$_.Name -in @('Awake','Start','Update','FixedUpdate','OnEnable')}).Count -eq 0) 'no automatic identity/migration callback'
    $identityCalls=@($identity.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
    Require (@($identityCalls | Where-Object {$_.Operand.Name -match 'Destroy|SetPrefab|SetPosition|SetRotation|SetHealth|SetLevel|ResetZDO|RemoveGlobalKey|Clear|Teleport'}).Count -eq 0) 'identity adapter never rewrites native identity/position/health/world or destroys actor'
    foreach($pair in @(@('InitializeFresh','Spawn','PrepareParty'),@('TryMigrate','OnCommand'))){
        $sites=@($methods | Where-Object { @(Calls $_ | Where-Object {$_.Operand.DeclaringType.FullName -eq 'LivingWorld.HumanNpc.HumanCompositionIdentity' -and $_.Operand.Name -eq $pair[0]}).Count -gt 0 })
        $allowed=@($pair | Select-Object -Skip 1)
        Require ($sites.Count -eq $allowed.Count -and @($sites | Where-Object {$_.DeclaringType.FullName -ne 'LivingWorld.ManualTest.ManualTestPlugin' -or $_.Name -notin $allowed}).Count -eq 0) "$($pair[0]) only called from explicitly checked scout/party commands"
    }
    $writes=FindMethod LivingWorld.HumanNpc.HumanMigrationPolicy PlanMigration
    $writeStrings=@($writes.Body.Instructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr} | ForEach-Object {$_.Operand})
    Require ($writeStrings -contains 'livingworld_human_composition_origin' -and $writeStrings -contains 'livingworld_human_composition_revision') 'migration plan writes separate origin/composition keys'
    Require (-not ($writeStrings -contains 'livingworld_human_rig_revision')) 'migration never relabels old visual revision'
    "HumanCompositionBinary PASS: $checks independent IL checks. Binary SHA256: $((Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash)"
    'Unity load, migration, pathfinding, attacks and save/reload NOT RUN by this audit.'
} finally {$assembly.Dispose()}
