using System;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Native graph is retained. Only these inspected, non-combat crafting clips
    // may be requested; changed clips/props fail closed before any equipment use.
    internal static class AmbientAnimationContract
    {
        internal static bool Supports(Animator animator, int kind)
        {
            if (animator == null || animator.runtimeAnimatorController == null || (kind != 1 && kind != 2)) return false;
            string name = kind == 1 ? "Workbench" : "Forge";
            bool found = false;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null || clip.name != name) continue;
                found = true;
                var events = clip.events;
                if (kind == 1) { if (events.Length != 0) return false; continue; }
                if (events.Length != 2 || !SafeEvent(events[0], "Attach", "aniattach_forge_hammer") ||
                    !SafeEvent(events[1], "Effect", "fx_forge_hammer")) return false;
            }
            return found;
        }

        internal static bool SafeEvent(AnimationEvent e, string function, string prefabName)
        {
            var prefab = e.objectReferenceParameter as GameObject;
            if (e.functionName != function || e.stringParameter != "RightHand_Attach" || e.intParameter != 0 ||
                prefab == null || prefab.name != prefabName) return false;
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null) return false;
                string type = component.GetType().FullName;
                if (type != "UnityEngine.Transform" && type != "UnityEngine.MeshFilter" && type != "UnityEngine.MeshRenderer" &&
                    type != "UnityEngine.ParticleSystem" && type != "UnityEngine.ParticleSystemRenderer" &&
                    type != "UnityEngine.AudioSource" && type != "TimedDestruction" && type != "ZSFX") return false;
            }
            return true;
        }
    }
}
