param(
    [string]$JotunnPath='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\independent-core\build\Jotunn.dll',
    [string]$CecilPath='C:\Users\scruf\.codex\.chatgpt-projects\g-p-6acbfeacfc1c81918b6bf81f40fe6336\work\LivingVillage\independent-core\tools\Mono.Cecil.dll'
)
# Read-only dependency IL audit, not Unity registration execution.
$ErrorActionPreference='Stop'
Add-Type -Path $CecilPath
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $JotunnPath).Path)
$checks=0
function Require([bool]$pass,[string]$reason){if(-not $pass){throw "JotunnRegistrationLifecycle FAIL: $reason"};$script:checks++}
function Method([string]$typeName,[string]$name,[string]$parameters){
    $type=$assembly.MainModule.Types | Where-Object FullName -eq $typeName
    $methods=@($type.Methods | Where-Object {$_.Name -eq $name -and (($_.Parameters.ParameterType.FullName -join ',') -eq $parameters)})
    Require ($methods.Count -eq 1 -and $methods[0].HasBody) "unique reviewed $typeName.$name($parameters)"
    $methods[0]
}
function Calls($method){@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference]})}
try {
    Require ((Get-FileHash -LiteralPath $JotunnPath -Algorithm SHA256).Hash -eq '9509221046842A70F94B98BAB4CB41005DEE026823A7A79345829ED46A30F796') 'Reviewed Jotunn binary unchanged; reinspection needed after updates'
    $ctor=Method 'Jotunn.Entities.CustomPrefab' '.ctor' 'UnityEngine.GameObject,System.Boolean'
    $add=Method 'Jotunn.Managers.PrefabManager' 'AddPrefab' 'Jotunn.Entities.CustomPrefab'
    $init=Method 'Jotunn.Managers.PrefabManager' 'Jotunn.IManager.Init' ''
    $register=Method 'Jotunn.Managers.PrefabManager' 'RegisterToZNetScene' 'UnityEngine.GameObject'
    $asset=Method 'Jotunn.Managers.AssetManager' 'AddAsset' 'UnityEngine.Object,UnityEngine.Object'
    $loader=Method 'Jotunn.Managers.AssetManager' 'AddAssetToBundleLoader' 'SoftReferenceableAssets.AssetBundleLoader,SoftReferenceableAssets.AssetID,Jotunn.Managers.AssetManager/AssetRef'
    $generate=Method 'Jotunn.Managers.AssetManager' 'GenerateAssetID' 'UnityEngine.Object'
    $valid=Method 'Jotunn.Entities.CustomPrefab' 'IsValid' ''
    $getter=Method 'Jotunn.Managers.PrefabManager' 'get_PrefabContainer' ''
    $createClone=Method 'Jotunn.Managers.PrefabManager' 'CreateClonedPrefab' 'System.String,UnityEngine.GameObject'
    $cloneAsset=Method 'Jotunn.Managers.AssetManager' 'ClonePrefab' 'UnityEngine.GameObject,System.String,UnityEngine.Transform'
    Require ($getter.IsAssembly) 'PrefabContainer getter internal; external guard must use reviewed reflection'
    $initInstructions=@($init.Body.Instructions)
    $deactivate=@($initInstructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.FullName -eq 'System.Void UnityEngine.GameObject::SetActive(System.Boolean)'})
    Require ($deactivate.Count -eq 1 -and $deactivate[0].Previous.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldc_I4_0) 'Jotunn initializes PrefabContainer inactive'
    Require (@(Calls $add | Where-Object {$_.Operand.FullName -eq 'System.Void UnityEngine.Transform::SetParent(UnityEngine.Transform,System.Boolean)'}).Count -eq 1) 'AddPrefab reparents only to configured prefab holder'
    Require (@(Calls $add | Where-Object {$_.Operand.Name -eq 'get_PrefabContainer'}).Count -eq 1) 'AddPrefab resolves native inactive PrefabContainer'
    Require (@(Calls $createClone | Where-Object {$_.Operand.Name -eq 'get_PrefabContainer'}).Count -eq 1) 'CreateClonedPrefab resolves the same inactive PrefabContainer before cloning'
    Require (@(Calls $createClone | Where-Object {$_.Operand.FullName -eq 'UnityEngine.GameObject Jotunn.Managers.AssetManager::ClonePrefab(UnityEngine.GameObject,System.String,UnityEngine.Transform)'}).Count -eq 1) 'CreateClonedPrefab delegates one clone with the inactive container parent'
    Require (@(Calls $cloneAsset | Where-Object {$_.Operand.FullName -eq '!!0 UnityEngine.Object::Instantiate<UnityEngine.GameObject>(!!0,UnityEngine.Transform)'}).Count -eq 1) 'AssetManager clones directly under the supplied parent instead of creating a live scene root'
    Require (@(Calls $cloneAsset | Where-Object {$_.Operand.Name -eq 'SetActive'}).Count -eq 0) 'AssetManager does not activate the staged clone'
    foreach($method in @($ctor,$add,$register,$asset,$loader,$generate,$valid)){
        Require (@(Calls $method | Where-Object {$_.Operand.Name -in @('SetActive','Instantiate','Awake','Start','SendMessage')}).Count -eq 0) "$($method.FullName) does not activate/instantiate/invoke lifecycle"
    }
    $registerBody=($register.Body.Instructions | ForEach-Object ToString) -join "`n"
    Require ($registerBody.Contains('ZNetScene::m_namedPrefabs') -and $registerBody.Contains('ZNetScene::m_prefabs')) 'Scene registration adds prefab references, not scene instances'
    Require (@(Calls $generate | Where-Object {$_.Operand.Name -eq 'GetStableHashCode'}).Count -eq 1) 'Fresh asset identity generated from own name'
    Write-Output "JotunnRegistrationLifecycle PASS: $checks read-only registration/lifecycle checks. Actual Unity registration NOT RUN."
} finally {$assembly.Dispose()}

