using System;
using System.Collections.Generic;

namespace LivingWorld.HumanNpc
{
    // Pure checks shared by the prefab builder and the non-Unity regression suite.
    internal static class HumanRigPolicy
    {
        internal const string Revision = "native-human-v1";
        internal const string PrefabId = "LWC_HumanScout_v1";
        internal static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        internal static void ValidateBoneNames(IEnumerable<string> names)
        {
            Require(names != null, "Body bone array is missing.");
            var used = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            foreach (var name in names)
            {
                Require(!String.IsNullOrEmpty(name), "Body bone is null or unnamed.");
                Require(used.Add(name), "Duplicate body bone name: " + name);
                count++;
            }
            Require(count > 0, "Body has no bones.");
        }

        internal static bool MayInitializeAppearance(bool validView, bool owner, string savedRevision)
        {
            return validView && owner && String.IsNullOrEmpty(savedRevision);
        }

        internal static bool RevisionCompatible(string savedRevision)
        {
            return String.IsNullOrEmpty(savedRevision) || savedRevision == Revision;
        }

        internal static bool EquipmentReady(bool inInventory, bool equipped, string expected, string actual)
        {
            return inInventory && equipped && !String.IsNullOrEmpty(expected) && expected == actual;
        }
    }
}
