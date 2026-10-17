using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
class BuildWorkerCore
{
    static IEnumerable<TypeDefinition> All(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes)foreach(var c in All(n))yield return c;}
    static string Body(MethodDefinition m){if(!m.HasBody)return "";return string.Join("\n",m.Body.Instructions.Select(i=>i.OpCode+" "+Operand(m,i.Operand)).ToArray());}
    static string Operand(MethodDefinition m,object o){if(o is Instruction)return "#"+m.Body.Instructions.IndexOf((Instruction)o);if(o is Instruction[])return string.Join(",",((Instruction[])o).Select(i=>m.Body.Instructions.IndexOf(i).ToString()).ToArray());return o==null?"":o.ToString();}
    static void Main(string[] args)
    {
        var a=AssemblyDefinition.ReadAssembly(args[0]);var module=a.MainModule;
        var originals=module.Types.SelectMany(All).Where(t=>t.Name!="VillageWorker").SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName,Body);
        var core=AssemblyDefinition.ReadAssembly(args[1]);var api=core.MainModule.Types.First(t=>t.FullName=="LivingVillageCore.AreaWork");
        var worker=module.Types.First(t=>t.Name=="VillageWorker");
        var ctor=worker.Methods.First(m=>m.Name==".ctor");var baseCtor=(MethodReference)ctor.Body.Instructions.First(i=>i.OpCode==OpCodes.Call && i.Operand is MethodReference && ((MethodReference)i.Operand).Name==".ctor").Operand;
        foreach(var field in worker.Fields.ToArray())if(field.Name!="Home" && field.Name!="Report")worker.Fields.Remove(field);
        string[] retained={".ctor","Awake","WorkTick","ValidateNativeApis","ResetClaims","OnDisable"};
        foreach(var method in worker.Methods.ToArray())if(!retained.Contains(method.Name))worker.Methods.Remove(method);
        foreach(var method in worker.Methods)
        {
            method.Body=new MethodBody(method);var il=method.Body.GetILProcessor();
            if(method.Name==".ctor"){il.Append(Instruction.Create(OpCodes.Ldarg_0));il.Append(Instruction.Create(OpCodes.Call,baseCtor));}
            else if(method.Name=="WorkTick")
            {il.Append(Instruction.Create(OpCodes.Ldarg_0));il.Append(Instruction.Create(OpCodes.Ldarg_1));il.Append(Instruction.Create(OpCodes.Call,module.ImportReference(api.Methods.First(m=>m.Name=="Run"))));}
            else if(method.Name=="OnDisable")
            {il.Append(Instruction.Create(OpCodes.Ldarg_0));il.Append(Instruction.Create(OpCodes.Call,module.ImportReference(api.Methods.First(m=>m.Name=="Disable"))));}
            else if(method.Name=="ValidateNativeApis" || method.Name=="ResetClaims")
            {il.Append(Instruction.Create(OpCodes.Call,module.ImportReference(api.Methods.First(m=>m.Name==(method.Name=="ResetClaims"?"Reset":"Validate")))));}
            il.Append(Instruction.Create(OpCodes.Ret));
        }
        var plugin=module.Types.First(t=>t.Name=="LivingVillagePlugin");var attribute=plugin.CustomAttributes.First(c=>c.AttributeType.Name=="BepInPlugin");attribute.ConstructorArguments[2]=new CustomAttributeArgument(module.TypeSystem.String,"0.1.6.3");
        foreach(var type in module.Types.SelectMany(All).Where(t=>t.Name!="VillageWorker"))foreach(var m in type.Methods)if(originals[m.FullName]!=Body(m))throw new Exception("Unintended non-worker change: "+m.FullName);
        a.Write(args[2]);Console.WriteLine("Worker module replaced; "+originals.Count+" other method bodies unchanged. Old worker scan/distance/claim methods removed.");
    }
}
