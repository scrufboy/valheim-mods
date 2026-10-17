using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class PatchWorkerController
{
    static System.Collections.Generic.IEnumerable<TypeDefinition> All(TypeDefinition t)
    {
        yield return t;
        foreach (var n in t.NestedTypes.SelectMany(All)) yield return n;
    }
    static void Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("Usage: PatchWorkerController input.dll output.dll assembly_valheim.dll");
        var asm = AssemblyDefinition.ReadAssembly(args[0]);
        var game = AssemblyDefinition.ReadAssembly(args[2]);
        var types = asm.MainModule.Types.SelectMany(All).ToArray();
        int prefixes = 0, effects = 0;
        foreach (var name in new[] { "Tameable_GetHoverName_Patch", "Tameable_Interact_Patch" })
        {
            var type = types.SingleOrDefault(t => t.Name == name);
            if (type == null) continue;
            var method = type.Methods.Single(m => m.Name == "Prefix");
            if (method.ReturnType.FullName != "System.Boolean") throw new InvalidOperationException("Unexpected Harmony prefix signature");
            // Returning true leaves the current game's controller-aware hover text,
            // petting, naming, and follow/stay interaction in control.
            method.Body = new MethodBody(method);
            method.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
            prefixes++;
        }
        var create = game.MainModule.Types.Single(t => t.Name == "EffectList").Methods.Single(m => m.Name == "Create" && m.Parameters.Count == 6);
        if (create.Parameters[5].ParameterType.FullName != "ZDOID") throw new InvalidOperationException("Unexpected EffectList.Create signature");
        foreach (var method in types.SelectMany(t => t.Methods).Where(m => m.HasBody))
        foreach (var instruction in method.Body.Instructions.ToArray())
        {
            var reference = instruction.Operand as MethodReference;
            if (reference == null || reference.DeclaringType.Name != "EffectList" || reference.Name != "Create" || reference.Parameters.Count != 5) continue;
            var id = new VariableDefinition(asm.MainModule.ImportReference(create.Parameters[5].ParameterType));
            method.Body.Variables.Add(id);
            method.Body.InitLocals = true;
            var il = method.Body.GetILProcessor();
            // A zero ZDOID preserves the old call's unrestricted effect audience.
            var load = Instruction.Create(OpCodes.Ldloc, id);
            il.InsertBefore(instruction, load);
            foreach (var branch in method.Body.Instructions)
            {
                if (branch.Operand == instruction) branch.Operand = load;
                var targets = branch.Operand as Instruction[];
                if (targets != null) for (int k = 0; k < targets.Length; k++) if (targets[k] == instruction) targets[k] = load;
            }
            instruction.Operand = asm.MainModule.ImportReference(create);
            method.Body.MaxStackSize = Math.Max(8, method.Body.MaxStackSize + 1);
            effects++;
        }
        // Added effect arguments can move a destination beyond short-branch range.
        foreach (var instruction in types.SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions))
        {
            if (instruction.OpCode.OperandType != OperandType.ShortInlineBrTarget) continue;
            var longName = instruction.OpCode.Name.Substring(0, instruction.OpCode.Name.Length - 2);
            instruction.OpCode = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).Single(o => o.Name == longName);
        }
        asm.Write(args[1]);
        var verify = AssemblyDefinition.ReadAssembly(args[1]);
        if (verify.MainModule.Types.SelectMany(All).SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Any(i => i.Operand is MethodReference && ((MethodReference)i.Operand).DeclaringType.Name == "EffectList" && ((MethodReference)i.Operand).Name == "Create" && ((MethodReference)i.Operand).Parameters.Count == 5)) throw new InvalidOperationException("Obsolete effect API remains");
        Console.WriteLine("Native Tameable prefixes=" + prefixes + "; migrated effect calls=" + effects + "; obsolete calls remaining=0");
    }
}
