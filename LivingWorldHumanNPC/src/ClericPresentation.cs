using System;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    internal static class ClericPresentation
    {
        // Verified native Point clip has no gameplay animation events. Runtime
        // parameter validation is still required for the actual loaded controller.
        internal static void Begin(GameObject actor)
        {
            var animator = actor.GetComponentInChildren<Animator>();
            var sync = actor.GetComponent<ZSyncAnimation>();
            if (animator == null || sync == null) throw new InvalidOperationException("Human animation runtime unavailable");
            bool found = false;
            foreach (var parameter in animator.parameters)
                if (parameter.name == "emote_point" && parameter.type == AnimatorControllerParameterType.Trigger) found = true;
            if (!found) throw new InvalidOperationException("Loaded human controller lacks verified emote_point trigger");
            sync.SetTrigger("emote_point");
            // The native point animation is the readable wind-up. Repeated
            // actor-centred particles looked like an unexplained hip spark and
            // obscured combat posture, so only the delivered heal is marked.
        }
        internal static void End(GameObject actor)
        {
            var sync = actor.GetComponent<ZSyncAnimation>();
            if (sync != null) sync.SetTrigger("emote_stop");
        }

        internal static void Delivered(Vector3 at)
        {
            Pulse(at + Vector3.up * .5f, new Color(1f, .7f, .15f, .8f), 2.5f);
        }

        static void Pulse(Vector3 at, Color tint, float lifetime)
        {
            var source = ZNetScene.instance.GetPrefab("shaman_heal_aoe");
            if (source == null) throw new InvalidOperationException("Native healing visual unavailable");
            // Never instantiate the AOE root, its collider or its network scripts.
            // Only checked particle children are copied beneath an inactive root.
            var root = new GameObject("LWC_Local_Cleric_Gold"); root.SetActive(false);
            try
            {
                foreach (string name in new[] { "flames", "flames_world" })
                {
                    var child = source.transform.Find(name);
                    if (child == null || child.GetComponent<ParticleSystem>() == null) throw new InvalidOperationException("Native particle child unavailable: " + name);
                    foreach (var component in child.GetComponentsInChildren<Component>(true))
                        if (!(component is Transform) && !(component is ParticleSystem) && !(component is ParticleSystemRenderer))
                            throw new InvalidOperationException("Unexpected component in visual-only child: " + component.GetType().Name);
                    var copy = UnityEngine.Object.Instantiate(child.gameObject, root.transform, false);
                    foreach (var system in copy.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var main = system.main; main.loop = false; main.startColor = tint;
                        var over = system.colorOverLifetime; over.enabled = false;
                    }
                    foreach (var renderer in copy.GetComponentsInChildren<ParticleSystemRenderer>(true))
                        foreach (var material in renderer.materials)
                        {
                            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
                            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", tint);
                            UnityEngine.Object.Destroy(material, lifetime + .5f);
                        }
                }
                root.transform.position = at;
                root.SetActive(true);
                foreach (var system in root.GetComponentsInChildren<ParticleSystem>()) system.Play();
                UnityEngine.Object.Destroy(root, lifetime);
            }
            catch { UnityEngine.Object.Destroy(root); throw; }
        }
    }
}
