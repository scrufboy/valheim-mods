using System;
using System.Reflection;
using System.Reflection.Emit;

namespace LivingWorld.Core
{
    /// <summary>Compiled field readers (one ldfld), for per-frame paths where reflection GetValue would box and allocate.</summary>
    internal static class FastField
    {
        internal static Func<TOwner, TValue> Getter<TOwner, TValue>(FieldInfo field)
        {
            var method = new DynamicMethod("get_" + field.Name, typeof(TValue), new[] { typeof(TOwner) }, typeof(FastField).Module, true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<TOwner, TValue>)method.CreateDelegate(typeof(Func<TOwner, TValue>));
        }
    }
}
