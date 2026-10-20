using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LivingWorld.Studio
{
    // Built-in camera render scope only. Never changes world time, biome or shared materials.
    // SRP/camera mods need separate live validation. Always restore after this camera renders.
    public sealed class StudioRenderScope : MonoBehaviour
    {
        public bool Active;
        public Color ChromaColor = Color.green;
        bool applied, fog;
        Camera studioCamera;
        CameraClearFlags clearFlags;
        Color background;
        float farClip;
        AmbientMode ambientMode;
        Color ambient, sky, equator, ground;
        float intensity;
        Material skybox;
        readonly List<Light> suppressed = new List<Light>();

        void OnPreCull()
        {
            Restore();
            if (!Active) return;
            fog = RenderSettings.fog; ambientMode = RenderSettings.ambientMode;
            ambient = RenderSettings.ambientLight; sky = RenderSettings.ambientSkyColor;
            equator = RenderSettings.ambientEquatorColor; ground = RenderSettings.ambientGroundColor;
            intensity = RenderSettings.ambientIntensity; skybox = RenderSettings.skybox;
            applied = true;
            try
            {
                studioCamera = GetComponent<Camera>();
                if (studioCamera != null)
                {
                    clearFlags = studioCamera.clearFlags; background = studioCamera.backgroundColor;
                    farClip = studioCamera.farClipPlane;
                    // Native wood meshes may have bevels/gaps. Never expose a sky or
                    // distant world through them in a photographic capture.
                    studioCamera.clearFlags = CameraClearFlags.SolidColor;
                    studioCamera.backgroundColor = ChromaColor;
                    studioCamera.farClipPlane = 96f;
                }
                RenderSettings.fog = false; RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.65f, .65f, .65f);
                RenderSettings.ambientIntensity = 1f;
                foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>())
                {
                    if (!light.enabled || light.GetComponentInParent<StudioLightMarker>() != null) continue;
                    suppressed.Add(light); light.enabled = false;
                }
            }
            catch { Restore(); throw; }
        }
        void OnPostRender() { Restore(); }
        void OnDisable() { Restore(); }
        void OnDestroy() { Restore(); }
        public void Restore()
        {
            if (!applied) return;
            applied = false;
            foreach (var light in suppressed) if (light != null) light.enabled = true;
            suppressed.Clear();
            if (studioCamera != null)
            {
                studioCamera.clearFlags = clearFlags; studioCamera.backgroundColor = background;
                studioCamera.farClipPlane = farClip; studioCamera = null;
            }
            RenderSettings.fog = fog; RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambient; RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground;
            RenderSettings.ambientIntensity = intensity; RenderSettings.skybox = skybox;
        }
    }
    public sealed class StudioLightMarker : MonoBehaviour { }
}
