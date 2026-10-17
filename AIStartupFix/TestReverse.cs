using System;using System.IO;using System.Reflection;using HarmonyLib;
class TestReverse {
 static void Main(string[] a){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{foreach(var d in new[]{"D:/SteamLibrary/steamapps/common/Valheim/valheim_Data/Managed","work/runtime-profile/BepInEx/core","work/MobAILib-0.3.3","work/ai-startup-fix"}){var p=Path.Combine(d,new AssemblyName(e.Name).Name+".dll");if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
  var game=Assembly.LoadFrom("D:/SteamLibrary/steamapps/common/Valheim/valheim_Data/Managed/assembly_valheim.dll");var lib=Assembly.LoadFrom(a[0]);
  var orig=game.GetType("BaseAI").GetMethod("UpdateAI",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
  var dest=lib.GetType("RagnarsRokare.SlaveGreylings.MobAILib+BaseAI_UpdateAI_ReversePatch").GetMethod("UpdateAI",BindingFlags.Public|BindingFlags.Static);
  Console.WriteLine("Source="+orig+" Standin="+dest);
  new Harmony("local.popworkers.reverse-test").CreateReversePatcher(orig,new HarmonyMethod(dest)).Patch();
  Console.WriteLine("PASS: Harmony reverse patch generated executable method without invalid IL.");
  if(a.Length>1 && a[1]=="all"){
   new Harmony("local.popworkers.all-patches-test").PatchAll(lib);
   Console.WriteLine("PASS: Every MobAILib Harmony patch compiled against current Valheim.");
  }
 }
}
