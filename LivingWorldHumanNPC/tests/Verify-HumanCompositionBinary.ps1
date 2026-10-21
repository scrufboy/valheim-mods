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
    Require ($cloneSites.Count -eq 2 -and
        @($cloneSites | Where-Object {$_.DeclaringType.FullName -eq 'LivingWorld.ManualTest.ManualTestPlugin' -and $_.Name -eq 'Register'}).Count -eq 1 -and
        @($cloneSites | Where-Object {$_.DeclaringType.FullName -eq 'LivingWorld.HumanNpc.NativeHumanFactory' -and $_.Name -eq 'Create'}).Count -eq 1) 'only the alert effect and native Player human graph use Jotunn inactive-container cloning'
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
    Require (@($createCalls | Where-Object {$_.Operand.Name -eq 'CreateClonedPrefab'}).Count -eq 1) 'factory clones one complete native Player human graph inside the inactive prefab container'
    Require (@($createCalls | Where-Object {$_.Operand.Name -eq 'Instantiate'}).Count -eq 0) 'factory does not separately instantiate or rebuild a visual subtree'
    foreach($component in @('Humanoid','MonsterAI','LivingWorld.HumanNpc.HumanCompositionIdentity','LivingWorld.HumanNpc.HumanIdentity','LivingWorld.HumanNpc.HumanBehaviorInstaller')){
        Require (@($createCalls | Where-Object {$_.Operand.FullName.Contains('AddComponent<'+$component+'>')}).Count -eq 1) "explicit NPC component $component"
    }
    foreach($component in @('LivingWorld.HumanNpc.HumanCompanion','LivingWorld.HumanNpc.HumanDoorTraversal','LivingWorld.HumanNpc.HumanFollowPresence','LivingWorld.HumanNpc.HumanCompanionPosture','LivingWorld.HumanNpc.HumanResources','LivingWorld.HumanNpc.HumanEnvironmentStatus','LivingWorld.HumanNpc.HumanPartyConfidence','LivingWorld.HumanNpc.HumanCompanionAmbient','LivingWorld.HumanNpc.HumanEquipmentReadiness','LivingWorld.HumanNpc.HumanCombatDefense','LivingWorld.HumanNpc.HumanPartyEquipment','LivingWorld.HumanNpc.HumanCleric','LivingWorld.HumanNpc.HumanUtilityTorch')){
        Require (@($createCalls | Where-Object {$_.Operand.FullName.Contains('AddComponent<'+$component+'>')}).Count -eq 0) "factory does not pre-attach behavior $component"
    }
    $install=FindMethod LivingWorld.HumanNpc.HumanBehaviorInstaller Install
    $installCalls=@(Calls $install)
    $installerType=@($types | Where-Object FullName -eq 'LivingWorld.HumanNpc.HumanBehaviorInstaller')[0]
    foreach($fieldName in @('capacity','recovery','missingBonus','spendDelay','configured')){
        $field=@($installerType.Fields | Where-Object Name -eq $fieldName)
        Require ($field.Count -eq 1 -and @($field[0].CustomAttributes | Where-Object {$_.AttributeType.FullName -eq 'UnityEngine.SerializeField'}).Count -eq 1) "prefab-configured installer field $fieldName is Unity-serialized"
    }
    foreach($component in @('LivingWorld.HumanNpc.HumanCompanion','LivingWorld.HumanNpc.HumanDoorTraversal','LivingWorld.HumanNpc.HumanFollowPresence','LivingWorld.HumanNpc.HumanCompanionPosture','LivingWorld.HumanNpc.HumanResources','LivingWorld.HumanNpc.HumanEnvironmentStatus','LivingWorld.HumanNpc.HumanPartyConfidence','LivingWorld.HumanNpc.HumanCompanionAmbient','LivingWorld.HumanNpc.HumanEquipmentReadiness','LivingWorld.HumanNpc.HumanCombatDefense','LivingWorld.HumanNpc.HumanPartyEquipment','LivingWorld.HumanNpc.HumanCleric','LivingWorld.HumanNpc.HumanUtilityTorch')){
        Require (@($installCalls | Where-Object {$_.Operand.FullName.Contains('AddComponent<'+$component+'>')}).Count -eq 1) "post-appearance installer attaches $component exactly once"
    }
    $lastAdd=($installCalls | Where-Object {$_.Operand.Name -eq 'AddComponent'} | Sort-Object Offset | Select-Object -Last 1).Offset
    $enable=@($install.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'set_enabled'} | Sort-Object Offset | Select-Object -Last 1)
    Require ($enable.Count -eq 1 -and $enable[0].Offset -gt $lastAdd) 'native AI is enabled only after the complete behavior set is attached'
    foreach($component in @('UnityEngine.CapsuleCollider','UnityEngine.Rigidbody','ZNetView','ZSyncTransform','ZSyncAnimation','VisEquipment','FootStep')){
        Require (@($createCalls | Where-Object {$_.Operand.FullName.Contains('AddComponent<'+$component+'>')}).Count -eq 0) "native graph retains existing $component instead of duplicating it"
    }
    Require (@($createCalls | Where-Object {$_.Operand.FullName -match 'NativeHumanFactory::Remove<PlayerController>|NativeHumanFactory::Remove<Talker>|NativeHumanFactory::Remove<Skills>'}).Count -eq 3) 'Player-only controller, talk and skill components are removed before lifecycle'
    Require (@($createCalls | Where-Object {$_.Operand.FullName -match 'DestroyImmediate\(UnityEngine.Object\)'}).Count -ge 1) 'Player component is destroyed while the prefab remains inactive'
    Require (@($createCalls | Where-Object {$_.Operand.FullName -match 'System.Reflection|FieldInfo::SetValue'}).Count -eq 0) 'factory does not broadly copy serialized Player fields into the NPC'
    $createFields=@($create.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.FieldReference]} | ForEach-Object {$_.Operand.FullName})
    Require (@($createFields | Where-Object {$_ -eq 'UnityEngine.Transform Character::m_eye'}).Count -eq 2) 'factory transfers only the established native eye reference'
    Require (@($createFields | Where-Object {$_ -eq 'ItemDrop Humanoid::m_unarmedWeapon'}).Count -eq 2) 'factory transfers only the established native unarmed weapon reference'
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
    $apply=FindMethod LivingWorld.HumanNpc.NativeHumanFactory ConfigureNativeVisual
    $applyCalls=@(Calls $apply)
    Require (@($applyCalls | Where-Object {$_.Operand.Name -match 'Instantiate|GetPrefab|CreateClonedPrefab'}).Count -eq 0) 'visual adapter retains the already-cloned native graph'
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
    $stage=FindMethod LivingWorld.HumanNpc.HumanAppearance StageFreshProfile
    $stageCalls=@(Calls $stage)
    foreach($setter in @('SetModel','SetSkinColor','SetHairColor','SetHairItem','SetBeardItem')){
        Require (@($stageCalls | Where-Object {$_.Operand.DeclaringType.FullName -eq 'VisEquipment' -and $_.Operand.Name -eq $setter}).Count -eq 1) "fresh native startup stages $setter exactly once"
    }
    Require (@($stageCalls | Where-Object {$_.Operand.DeclaringType.FullName -eq 'ZDO' -and $_.Operand.Name -eq 'GetInt'}).Count -eq 2) 'fresh native startup reads both persisted facial-hair selections back'
    Require (@($stageCalls | Where-Object {$_.Operand.Name -eq 'Invoke' -or $_.Operand.Name -match '^Set(Hair|Beard)Equipped$|^UpdateVisuals$|^UpdateLodgroup$|^UpdateColors$'}).Count -eq 0) 'fresh staging never forces private rendering or attachment methods'
    $appearanceUpdate=FindMethod LivingWorld.HumanNpc.HumanAppearance Update
    $appearanceUpdateCalls=@(Calls $appearanceUpdate)
    Require (@($appearanceUpdateCalls | Where-Object {$_.Operand.DeclaringType.FullName -eq 'VisEquipment' -and $_.Operand.Name -match '^Set(Model|SkinColor|HairColor|HairItem|BeardItem)$'}).Count -eq 5) 'only the unchanged unprofiled legacy branch writes late native presentation defaults'
    Require (@($appearanceUpdateCalls | Where-Object {$_.Operand.Name -eq 'Invoke' -or $_.Operand.Name -match '^Set(Hair|Beard)Equipped$|^UpdateVisuals$|^UpdateLodgroup$|^UpdateColors$'}).Count -eq 0) 'appearance validation never forces private native rendering after startup'
    $stageSites=@($methods | Where-Object { @(Calls $_ | Where-Object {$_.Operand.DeclaringType.FullName -eq 'LivingWorld.HumanNpc.HumanAppearance' -and $_.Operand.Name -eq 'StageFreshProfile'}).Count -gt 0 })
    Require ($stageSites.Count -eq 2 -and @($stageSites | Where-Object {$_.DeclaringType.FullName -ne 'LivingWorld.ManualTest.ManualTestPlugin' -or $_.Name -notin @('Spawn','PrepareParty')}).Count -eq 0) 'only both explicit fresh party allocation paths stage native appearance'
    $render=FindMethod LivingWorld.HumanNpc.HumanAppearance AttachmentRenders
    $renderCalls=@(Calls $render)
    Require (@($renderCalls | Where-Object {$_.Operand.Name -eq 'GetComponentsInChildren'}).Count -eq 1) 'appearance readiness inspects the selected attachment renderers'
    Require (@($renderCalls | Where-Object {$_.Operand.Name -eq 'GetLODs'}).Count -ge 1) 'appearance readiness requires the selected attachment in the human LOD'
    Require (@($render.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'get_activeInHierarchy'}).Count -ge 2) 'appearance readiness rejects inactive attachment objects/renderers'
    $identity=@($types | Where-Object FullName -eq LivingWorld.HumanNpc.HumanCompositionIdentity)[0]
    Require (@($identity.Methods | Where-Object {$_.Name -in @('Awake','Start','Update','FixedUpdate','OnEnable')}).Count -eq 0) 'no automatic identity/migration callback'
    $identityCalls=@($identity.Methods | Where-Object HasBody | ForEach-Object {Calls $_})
    Require (@($identityCalls | Where-Object {$_.Operand.Name -match 'Destroy|SetPrefab|SetPosition|SetRotation|SetHealth|SetLevel|ResetZDO|RemoveGlobalKey|Clear|Teleport'}).Count -eq 0) 'identity adapter never rewrites native identity/position/health/world or destroys actor'
    foreach($pair in @(@('InitializeFreshBase','Spawn','PrepareParty','InitializeFresh'),@('InitializeOrders','RequirePartyMemberReady'),@('TryMigrate','OnCommand'))){
        $sites=@($methods | Where-Object { @(Calls $_ | Where-Object {$_.Operand.DeclaringType.FullName -eq 'LivingWorld.HumanNpc.HumanCompositionIdentity' -and $_.Operand.Name -eq $pair[0]}).Count -gt 0 })
        $allowed=@($pair | Select-Object -Skip 1)
        Require ($sites.Count -eq $allowed.Count -and @($sites | Where-Object {($_.DeclaringType.FullName -ne 'LivingWorld.ManualTest.ManualTestPlugin' -and $_.DeclaringType.FullName -ne 'LivingWorld.HumanNpc.HumanCompositionIdentity') -or $_.Name -notin $allowed}).Count -eq 0) "$($pair[0]) only called from explicitly checked scout/party commands"
    }
    $writes=FindMethod LivingWorld.HumanNpc.HumanMigrationPolicy PlanMigration
    $writeStrings=@($writes.Body.Instructions | Where-Object {$_.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr} | ForEach-Object {$_.Operand})
    Require ($writeStrings -contains 'livingworld_human_composition_origin' -and $writeStrings -contains 'livingworld_human_composition_revision') 'migration plan writes separate origin/composition keys'
    Require (-not ($writeStrings -contains 'livingworld_human_rig_revision')) 'migration never relabels old visual revision'
    "HumanCompositionBinary PASS: $checks independent IL checks. Binary SHA256: $((Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash)"
    'Unity load, migration, pathfinding, attacks and save/reload NOT RUN by this audit.'
} finally {$assembly.Dispose()}
