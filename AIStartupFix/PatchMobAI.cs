using System;using System.Linq;using Mono.Cecil;using Mono.Cecil.Cil;
class PatchMobAI {
 static System.Collections.Generic.IEnumerable<TypeDefinition> All(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes.SelectMany(All))yield return n;}
 static void Main(string[] a){
  var r=new DefaultAssemblyResolver();r.AddSearchDirectory("work/MobAILib-0.3.3");r.AddSearchDirectory("work/runtime-profile/BepInEx/core");r.AddSearchDirectory("D:/SteamLibrary/steamapps/common/Valheim/valheim_Data/Managed");
  var x=AssemblyDefinition.ReadAssembly(a[0],new ReaderParameters{AssemblyResolver=r});var ts=x.MainModule.Types.SelectMany(All).ToArray();
  var reverse=ts.Single(t=>t.Name=="BaseAI_UpdateAI_ReversePatch").Methods.Single(m=>m.Name=="UpdateAI");
  if(reverse.ReturnType.FullName=="System.Void"){
  reverse.ReturnType=x.MainModule.TypeSystem.Boolean;
  // Harmony replaces this stand-in with the genuine BaseAI body. Keep the
  // unpatched fallback valid IL too; native owner update returns success.
  foreach(var ret in reverse.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ret).ToArray())reverse.Body.GetILProcessor().InsertBefore(ret,Instruction.Create(OpCodes.Ldc_I4_1));
  int calls=0;
  foreach(var m in ts.SelectMany(t=>t.Methods).Where(m=>m.HasBody))foreach(var i in m.Body.Instructions.ToArray()){
   var mr=i.Operand as MethodReference;if(mr==null || mr.Name!="UpdateAI" || mr.DeclaringType.FullName!=reverse.DeclaringType.FullName)continue;
   i.Operand=reverse;m.Body.GetILProcessor().InsertAfter(i,Instruction.Create(OpCodes.Pop));calls++;
  }
  if(calls!=1)throw new Exception("Expected one reverse caller, found "+calls);
  }
  else if(reverse.ReturnType.FullName!="System.Boolean")throw new Exception("Unexpected reverse return type");
  // The optional unloaded-zone roaming implementation relies on removed
  // ZDOMan methods. Disabling the complete capability prevents partial
  // ownership/network behavior and does not alter loaded-area worker AI.
  int removed=0;
  foreach(var name in new[]{"ZoneSystem_Update_Patch","ZDOMan_SendZDOToPeers_Patch","ZDOMan_FindSectorObjects_Patch"}){
   var t=ts.Single(v=>v.Name==name);
   foreach(var attr in t.CustomAttributes.Where(v=>v.AttributeType.Name.StartsWith("Harmony")).ToArray()){t.CustomAttributes.Remove(attr);removed++;}
  }
  if(removed!=3)throw new Exception("Expected three roaming target attributes, found "+removed);
  x.Write(a[1]);Console.WriteLine("BaseAI reverse bool preserved; three unsupported optional roaming hook targets removed; loaded-area worker AI remains intact.");
 }
}
