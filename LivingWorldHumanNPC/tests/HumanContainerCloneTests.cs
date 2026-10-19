using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

internal static class HumanContainerCloneTests
{
    static int checks;
    static void Check(bool value, string label)
    {
        checks++;
        if (!value) throw new Exception(label);
    }

    static T Wrapper<T>()
    {
        // Managed identity token only; no Unity constructor, asset access,
        // overloaded Unity equality or other native-engine call is made.
        return (T)FormatterServices.GetUninitializedObject(typeof(T));
    }

    static void Reject(MethodInfo method, object input, Type expected, string label)
    {
        bool rejected = false;
        try { method.Invoke(null, new object[] { input }); }
        catch (TargetInvocationException error) { rejected = error.InnerException.GetType() == expected; }
        Check(rejected, label);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run(string dll)
    {
        var factory = Assembly.LoadFrom(dll).GetType("LivingWorld.HumanNpc.NativeHumanFactory", true);
        var modelsMethod = factory.GetMethod("CloneModels", BindingFlags.Static | BindingFlags.NonPublic);
        var effectsMethod = factory.GetMethod("CloneFootEffects", BindingFlags.Static | BindingFlags.NonPublic);
        Check(modelsMethod != null, "compiled model helper missing");
        Check(effectsMethod != null, "compiled footstep helper missing");

        var meshA = Wrapper<UnityEngine.Mesh>();
        var meshB = Wrapper<UnityEngine.Mesh>();
        var materialA = Wrapper<UnityEngine.Material>();
        var materialB = Wrapper<UnityEngine.Material>();
        var sharedRecord = new VisEquipment.PlayerModel { m_mesh = meshA, m_baseMaterial = materialA };
        var sourceModels = new[] { sharedRecord, sharedRecord, new VisEquipment.PlayerModel { m_mesh = meshB, m_baseMaterial = materialB } };
        var clonedModels = (VisEquipment.PlayerModel[])modelsMethod.Invoke(null, new object[] { sourceModels });
        Check(!Object.ReferenceEquals(sourceModels, clonedModels), "model array must detach");
        Check(clonedModels.Length == 3, "model count/order changed");
        Check(!Object.ReferenceEquals(sourceModels[0], clonedModels[0]), "first model record must detach");
        Check(!Object.ReferenceEquals(sourceModels[1], clonedModels[1]), "aliased source model record must detach");
        Check(!Object.ReferenceEquals(clonedModels[0], clonedModels[1]), "clone records must not alias one another");
        Check(!Object.ReferenceEquals(sourceModels[2], clonedModels[2]), "last model record must detach");
        Check(Object.ReferenceEquals(meshA, clonedModels[0].m_mesh), "native mesh asset identity changed");
        Check(Object.ReferenceEquals(materialA, clonedModels[0].m_baseMaterial), "native material asset identity changed");
        Check(Object.ReferenceEquals(meshB, clonedModels[2].m_mesh) && Object.ReferenceEquals(materialB, clonedModels[2].m_baseMaterial), "second asset pair/order changed");
        clonedModels[0].m_mesh = meshB;
        clonedModels[0].m_baseMaterial = materialB;
        Check(Object.ReferenceEquals(sourceModels[0].m_mesh, meshA) && Object.ReferenceEquals(sourceModels[0].m_baseMaterial, materialA), "editing clone model changed donor");
        Check(Object.ReferenceEquals(clonedModels[1].m_mesh, meshA), "editing clone model changed sibling");
        sourceModels[2].m_mesh = meshA;
        Check(Object.ReferenceEquals(clonedModels[2].m_mesh, meshB), "editing donor model changed clone");
        clonedModels[1] = null;
        Check(Object.ReferenceEquals(sourceModels[1], sharedRecord), "editing clone array changed donor");
        sourceModels[2] = null;
        Check(clonedModels[2] != null, "editing donor array changed clone");
        var noModels = new VisEquipment.PlayerModel[0];
        var noModelsCopy = (VisEquipment.PlayerModel[])modelsMethod.Invoke(null, new object[] { noModels });
        Check(noModelsCopy.Length == 0 && !Object.ReferenceEquals(noModels, noModelsCopy), "empty model array must be independent");
        var nullAssets = (VisEquipment.PlayerModel[])modelsMethod.Invoke(null, new object[] { new[] { new VisEquipment.PlayerModel() } });
        Check(Object.ReferenceEquals(nullAssets[0].m_mesh, null) && Object.ReferenceEquals(nullAssets[0].m_baseMaterial, null), "clone helper must not fabricate native assets");
        Reject(modelsMethod, null, typeof(ArgumentNullException), "null model array must fail explicitly");
        Reject(modelsMethod, new VisEquipment.PlayerModel[] { null }, typeof(InvalidOperationException), "null model record must fail closed");

        var prefabA = Wrapper<UnityEngine.GameObject>();
        var prefabB = Wrapper<UnityEngine.GameObject>();
        var sharedPrefabs = new[] { prefabA, null, prefabB };
        var sharedEffect = new FootStep.StepEffect {
            m_name = "native-ground",
            m_motionType = FootStep.MotionType.Run,
            m_material = FootStep.GroundMaterial.Stone,
            m_effectPrefabs = sharedPrefabs
        };
        var sourceEffects = new List<FootStep.StepEffect> { sharedEffect, sharedEffect, new FootStep.StepEffect { m_name = "null-array", m_effectPrefabs = null } };
        var clonedEffects = (List<FootStep.StepEffect>)effectsMethod.Invoke(null, new object[] { sourceEffects });
        Check(!Object.ReferenceEquals(sourceEffects, clonedEffects), "footstep list must detach");
        Check(clonedEffects.Count == 3, "footstep count/order changed");
        Check(!Object.ReferenceEquals(sourceEffects[0], clonedEffects[0]), "footstep record must detach");
        Check(!Object.ReferenceEquals(sourceEffects[1], clonedEffects[1]), "aliased source footstep must detach");
        Check(!Object.ReferenceEquals(clonedEffects[0], clonedEffects[1]), "clone footstep records must not alias one another");
        Check(!Object.ReferenceEquals(sharedPrefabs, clonedEffects[0].m_effectPrefabs), "footstep prefab array must detach");
        Check(!Object.ReferenceEquals(clonedEffects[0].m_effectPrefabs, clonedEffects[1].m_effectPrefabs), "clone footstep arrays must not alias one another");
        Check(clonedEffects[0].m_name == "native-ground" && clonedEffects[0].m_motionType == FootStep.MotionType.Run && clonedEffects[0].m_material == FootStep.GroundMaterial.Stone, "footstep scalars changed");
        Check(clonedEffects[0].m_effectPrefabs.Length == 3, "footstep effect slot count changed");
        Check(Object.ReferenceEquals(clonedEffects[0].m_effectPrefabs[0], prefabA) && Object.ReferenceEquals(clonedEffects[0].m_effectPrefabs[2], prefabB), "native effect prefab identities/order changed");
        Check(Object.ReferenceEquals(clonedEffects[0].m_effectPrefabs[1], null), "empty effect slot must stay empty");
        Check(clonedEffects[2].m_effectPrefabs.Length == 0 && sourceEffects[2].m_effectPrefabs == null, "null array normalization changed donor");
        clonedEffects[0].m_name = "changed";
        clonedEffects[0].m_motionType = FootStep.MotionType.Walk;
        clonedEffects[0].m_material = FootStep.GroundMaterial.Wood;
        clonedEffects[0].m_effectPrefabs[0] = prefabB;
        Check(sourceEffects[0].m_name == "native-ground" && sourceEffects[0].m_motionType == FootStep.MotionType.Run && sourceEffects[0].m_material == FootStep.GroundMaterial.Stone, "editing clone footstep scalars changed donor");
        Check(Object.ReferenceEquals(sourceEffects[0].m_effectPrefabs[0], prefabA), "editing clone prefab slot changed donor");
        Check(Object.ReferenceEquals(clonedEffects[1].m_effectPrefabs[0], prefabA), "editing clone prefab slot changed sibling");
        sharedPrefabs[2] = prefabA;
        sourceEffects[0].m_name = "donor-changed";
        Check(Object.ReferenceEquals(clonedEffects[1].m_effectPrefabs[2], prefabB), "editing donor prefab slot changed clone");
        Check(clonedEffects[1].m_name == "native-ground", "editing donor scalar changed clone");
        clonedEffects.RemoveAt(0);
        Check(sourceEffects.Count == 3, "editing clone list changed donor list");
        sourceEffects.Clear();
        Check(clonedEffects.Count == 2, "editing donor list changed clone list");
        var emptyEffects = new List<FootStep.StepEffect>();
        var emptyCopy = (List<FootStep.StepEffect>)effectsMethod.Invoke(null, new object[] { emptyEffects });
        Check(emptyCopy.Count == 0 && !Object.ReferenceEquals(emptyEffects, emptyCopy), "empty footstep list must be independent");
        Reject(effectsMethod, null, typeof(ArgumentNullException), "null footstep list must fail explicitly");
        Reject(effectsMethod, new List<FootStep.StepEffect> { null }, typeof(InvalidOperationException), "null footstep record must fail closed");
    }

    static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2) throw new ArgumentException("Usage: HumanContainerCloneTests.exe <LivingWorld.HumanNpc.dll> <native-managed-directory> [dependency-directory ...]");
            var dependencyDirectories = new List<string>();
            for (int i = 1; i < args.Length; i++) dependencyDirectories.Add(Path.GetFullPath(args[i]));
            dependencyDirectories.Add(Path.GetDirectoryName(Path.GetFullPath(args[0])));
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs missing) {
                string fileName = new AssemblyName(missing.Name).Name + ".dll";
                foreach (string folder in dependencyDirectories)
                {
                    string path = Path.Combine(folder, fileName);
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            Run(Path.GetFullPath(args[0]));
            System.Console.WriteLine("HumanContainerCloneTests PASS: " + checks + " checks; managed model/footstep container isolation and unchanged shared asset references only. No Unity scene or gameplay executed.");
            return 0;
        }
        catch (Exception error) { System.Console.Error.WriteLine(error); return 1; }
    }
}
