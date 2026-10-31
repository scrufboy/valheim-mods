// Metadata contract check for LivingWorld.RRRLifecycleDiag.
// Usage: mono ContractCheck.exe <assembly_valheim.dll> <LivingWorld.RRRLifecycleDiag.dll>
// Exit 0 = all checks passed.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

class ContractCheck
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        | BindingFlags.Static | BindingFlags.DeclaredOnly;
    static int failures;
    static int passes;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "PASS " : "FAIL ") + what);
        if (ok) passes++; else failures++;
    }

    static int Main(string[] args)
    {
        Assembly game = Assembly.LoadFrom(args[0]);
        Assembly diag = Assembly.LoadFrom(args[1]);
        Type plugin = diag.GetType("LivingWorld.RRRLifecycleDiag.Plugin", true);

        // Fixed tracked set.
        string[] trackedActors = (string[])plugin.GetField("TrackedActors").GetValue(null);
        Check(trackedActors.SequenceEqual(new[] { "RRRN_WoodsSoul", "RRRN_NinoBosque", "RRRN_OrcBrute" }),
            "TrackedActors == [RRRN_WoodsSoul, RRRN_NinoBosque, RRRN_OrcBrute]");

        // Native fields the plugin reads.
        Type znv = game.GetType("ZNetView"), zns = game.GetType("ZNetScene"), zdo = game.GetType("ZDO");
        FieldInfo mZdo = znv.GetField("m_zdo", All);
        Check(mZdo != null && mZdo.FieldType == zdo, "ZNetView.m_zdo : ZDO");
        FieldInfo mInst = zns.GetField("m_instances", All);
        Check(mInst != null && mInst.FieldType == typeof(Dictionary<,>).MakeGenericType(zdo, znv),
            "ZNetScene.m_instances : Dictionary<ZDO,ZNetView>");

        // Native methods used outside hooks.
        Type znet = game.GetType("ZNet");
        foreach (string m in new[] { "IsServer", "IsDedicated", "GetPeers" })
            Check(znet.GetMethod(m, All, null, Type.EmptyTypes, null) != null, "ZNet." + m + "()");
        Check(game.GetType("Player").GetMethod("GetAllPlayers", All) != null, "Player.GetAllPlayers()");

        // Hook table.
        string[] table = (string[])plugin.GetField("HookTable").GetValue(null);
        var handlerNames = new HashSet<string>();
        foreach (string row in table)
        {
            string[] p = row.Split('|');
            Type t = game.GetType(p[0]);
            MethodInfo[] targets = t == null ? new MethodInfo[0] : t.GetMethods(All).Where(x => x.Name == p[1]).ToArray();
            Check(targets.Length == 1, "target " + p[0] + "." + p[1] + " resolves to exactly one declared method (found " + targets.Length + ")");
            if (targets.Length != 1) continue;
            MethodInfo target = targets[0];
            for (int k = 2; k <= 4; ++k)
            {
                if (p[k] == "-") continue;
                string kind = k == 2 ? "prefix" : k == 3 ? "postfix" : "finalizer";
                handlerNames.Add(p[k]);
                MethodInfo h = plugin.GetMethod(p[k], All);
                Check(h != null && h.IsStatic, kind + " " + p[k] + " exists and is static");
                if (h == null) continue;
                CheckParameters(target, h, kind);
                CheckFailClosed(h, kind);
            }
        }

        // Non-hook entry points invoked by the game.
        foreach (string entry in new[] { "Command" })
        {
            MethodInfo h = plugin.GetMethod(entry, All);
            Check(h != null && HasCatch(h), "entry " + entry + " has a catch clause");
        }

        // Distance parsing.
        MethodInfo parse = plugin.GetMethod("ParseDistance", All);
        CheckDistance(parse, null, 12f, false);
        CheckDistance(parse, "15.5", 15.5f, false);
        CheckDistance(parse, "0", 8f, false);
        CheckDistance(parse, "-5", 8f, false);
        CheckDistance(parse, "100", 30f, false);
        CheckDistance(parse, "abc", 12f, true);
        CheckDistance(parse, "", 12f, true);
        CheckDistance(parse, "NaN", 12f, true);
        CheckDistance(parse, "1e40", 12f, true);
        CheckDistance(parse, "12,5", 12f, true);

        Console.WriteLine("contract: " + passes + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }

    static void CheckParameters(MethodInfo target, MethodInfo h, string kind)
    {
        ParameterInfo[] tp = target.GetParameters();
        foreach (ParameterInfo hp in h.GetParameters())
        {
            string n = hp.Name;
            bool ok;
            if (n == "__instance") ok = !target.IsStatic && hp.ParameterType.IsAssignableFrom(target.DeclaringType);
            else if (n == "__exception") ok = kind == "finalizer" && hp.ParameterType == typeof(Exception);
            else if (n == "__result") ok = kind == "postfix" && hp.ParameterType == target.ReturnType;
            else
            {
                ParameterInfo match = tp.FirstOrDefault(x => x.Name == n);
                ok = match != null && match.ParameterType == hp.ParameterType;
            }
            Check(ok, "  " + h.Name + " parameter '" + n + "' binds to " + target.DeclaringType.Name + "." + target.Name);
        }
        if (kind == "finalizer")
        {
            Check(h.ReturnType == typeof(Exception), "  " + h.Name + " returns Exception");
            Check(h.GetParameters().Any(x => x.Name == "__exception"), "  " + h.Name + " receives __exception");
        }
        else Check(h.ReturnType == typeof(void), "  " + h.Name + " returns void (observer, never skips original)");
    }

    static bool HasCatch(MethodInfo m)
    {
        MethodBody body = m.GetMethodBody();
        return body != null && body.ExceptionHandlingClauses.Any(c => c.Flags == ExceptionHandlingClauseOptions.Clause);
    }

    static void CheckFailClosed(MethodInfo h, string kind)
    {
        Check(HasCatch(h), "  " + h.Name + " wraps its body in try/catch");
        if (kind != "finalizer") return;
        byte[] il = h.GetMethodBody().GetILAsByteArray();
        int idx = Array.FindIndex(h.GetParameters(), x => x.Name == "__exception");
        bool tail = false;
        if (il.Length >= 2 && il[il.Length - 1] == 0x2A)
        {
            if (idx <= 3) tail = il[il.Length - 2] == (byte)(0x02 + idx);
            else tail = il.Length >= 3 && il[il.Length - 3] == 0x0E && il[il.Length - 2] == idx;
        }
        Check(tail, "  " + h.Name + " final instruction returns the incoming __exception (after its catch)");
        MethodBody body = h.GetMethodBody();
        int lastHandlerEnd = body.ExceptionHandlingClauses.Max(c => c.HandlerOffset + c.HandlerLength);
        Check(lastHandlerEnd <= il.Length - 2, "  " + h.Name + " returns outside every protected region");
    }

    static void CheckDistance(MethodInfo parse, string input, float expect, bool expectFallback)
    {
        object[] a = { input, false };
        float got = (float)parse.Invoke(null, a);
        bool fell = (bool)a[1];
        Check(Math.Abs(got - expect) < 1e-4f && fell == expectFallback,
            "ParseDistance(" + (input == null ? "null" : "\"" + input + "\"") + ") = " + got + " fallback=" + fell
            + " (expected " + expect + " fallback=" + expectFallback + ")");
    }
}
