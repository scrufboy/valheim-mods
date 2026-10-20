using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LivingWorld.Studio
{
    /// <summary>
    /// Local photographic scenery only: never instantiates a complete vanilla prefab.
    /// Meshes/shaders remain runtime references; all materials are private copies.
    /// This prototype does not promise fog isolation or a seamless curved backdrop.
    /// A server must allocate the room position before a player can enter it.
    /// </summary>
    public sealed class StudioRoom : IDisposable
    {
        public const float Width = 32f, Height = 12f;
        public const int PropLimit = 32;
        static readonly HashSet<string> AllowedProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "Beech1", "Birch1", "FirTree", "PineTree", "Bush01", "Bush02",
            "RaspberryBush", "BlueberryBush", "wood_floor", "woodwall",
            "wood_door", "wood_pole2", "piece_workbench", "piece_chest_wood",
            "piece_chair", "piece_bench01", "piece_table", "bed", "sign"
        };
        readonly List<Material> materials = new List<Material>();
        readonly List<Material> backdropMaterials = new List<Material>();
        readonly List<GameObject> props = new List<GameObject>();
        readonly List<GameObject> markers = new List<GameObject>();
        GameObject root;
        MeshFilter floorSource;
        int controlCount;
        Vector3 origin;
        bool disposed;

        public Vector3 Landing { get { return origin + new Vector3(0f, 0.15f, -12f); } }

        public static StudioRoom Build(Vector3 origin)
        {
            RequireFinite(origin);
            if (ZNetScene.instance == null) throw new InvalidOperationException("Native prefab registry is unavailable.");
            var source = ZNetScene.instance.GetPrefab("wood_floor");
            if (source == null) throw new InvalidOperationException("Native wood_floor prefab is unavailable.");
            MeshFilter selected = null;
            float area = 0f;
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (mesh == null || renderer == null || renderer.sharedMaterials.Length == 0) continue;
                var size = mesh.bounds.size;
                if (size.x <= 0.001f || size.y <= 0.001f || size.z <= 0.001f) continue;
                float candidate = size.x * size.z;
                if (candidate > area) { selected = filter; area = candidate; }
            }
            if (selected == null) throw new InvalidOperationException("Native wood floor has no usable solid mesh.");
            var room = new StudioRoom();
            room.origin = origin;
            room.floorSource = selected;
            room.root = new GameObject("LWC_Studio_LocalRoom");
            room.root.transform.position = origin;
            try
            {
                room.Slab(selected, "Floor", new Vector3(0, -.2f, 0), new Vector3(32.8f, .4f, 32.8f));
                room.Slab(selected, "Ceiling", new Vector3(0, 12.2f, 0), new Vector3(32.8f, .4f, 32.8f));
                room.Slab(selected, "West", new Vector3(-16.2f, 6, 0), new Vector3(.4f, 12, 32.8f));
                room.Slab(selected, "East", new Vector3(16.2f, 6, 0), new Vector3(.4f, 12, 32.8f));
                room.Slab(selected, "Back", new Vector3(0, 6, 16.2f), new Vector3(32f, 12, .4f));
                room.Slab(selected, "Front", new Vector3(0, 6, -16.2f), new Vector3(32f, 12, .4f));
                room.Lamp(new Vector3(-8, 10, -6), 1.8f, 30);
                room.Lamp(new Vector3(8, 10, -6), 1.8f, 30);
                room.Lamp(new Vector3(0, 10, 8), 1.2f, 25);
                for (int row = 0; row < 4; row++)
                    for (int column = 0; column < 4; column++)
                    {
                        var pad = room.Slab(selected, "SubjectPad_" + (1 + row * 4 + column),
                            new Vector3(-4.5f + 3f * column, .01f, 1f + 2.5f * row),
                            new Vector3(.7f, .02f, .7f), false);
                        // Markers are visual guidance, not obstacles or independently owned pieces.
                        pad.GetComponent<BoxCollider>().enabled = false;
                        room.markers.Add(pad);
                    }
                room.SetPalette("green");
                return room;
            }
            catch { room.Dispose(); throw; }
        }

        GameObject Slab(MeshFilter source, string name, Vector3 at, Vector3 size, bool chroma = true)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = at;
            var box = holder.AddComponent<BoxCollider>();
            box.size = size;
            var meshObject = new GameObject("NativeWoodVisual");
            meshObject.transform.SetParent(holder.transform, false);
            var bounds = source.sharedMesh.bounds;
            var scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
            meshObject.transform.localScale = scale;
            meshObject.transform.localPosition = -Vector3.Scale(bounds.center, scale);
            meshObject.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            var renderer = meshObject.AddComponent<MeshRenderer>();
            var copies = CopyMaterials(source.GetComponent<MeshRenderer>().sharedMaterials, chroma);
            if (!chroma)
                foreach (var material in copies)
                    foreach (var property in new[] { "_Color", "_BaseColor" })
                        if (material.HasProperty(property)) material.SetColor(property, new Color(.65f, .65f, .65f, 1));
            renderer.sharedMaterials = copies;
            ConfigureRenderer(renderer);
            return holder;
        }

        /// <summary>Neutral timber console body; caller may add its own isolated interaction component.</summary>
        public GameObject CreateExitDoor()
        {
            CheckAlive();
            var source = ZNetScene.instance.GetPrefab("wood_door");
            if (source == null) throw new InvalidOperationException("Native timber door visual is unavailable.");
            var holder = new GameObject("StudioExitDoor");
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = new Vector3(0f, .02f, -15.5f);
            int count = CopyVisualTree(source.transform, holder.transform, true, new HashSet<Renderer>());
            if (count == 0) throw new InvalidOperationException("Native timber door has no supported visual.");
            var collider = holder.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.25f, 0f);
            collider.size = new Vector3(1.8f, 2.5f, .35f);
            return holder;
        }

        /// <summary>Neutral timber console body; caller may add its own isolated interaction component.</summary>
        public GameObject CreateControl(string name, Vector3 localPosition)
        {
            CheckAlive(); RequireFinite(localPosition);
            if (controlCount >= 16) throw new InvalidOperationException("Studio control budget reached.");
            if (String.IsNullOrEmpty(name) || name.Length > 80) throw new ArgumentException("Invalid control name.");
            if (Math.Abs(localPosition.x) > 15 || Math.Abs(localPosition.z) > 15 || localPosition.y < .5f || localPosition.y > 10.5f)
                throw new ArgumentException("Control must remain inside the studio.");
            var control = Slab(floorSource, name, localPosition, new Vector3(.65f, 1f, .65f), false);
            controlCount++;
            return control;
        }

        Material[] CopyMaterials(Material[] originals, bool chroma)
        {
            var copies = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                if (originals[i] == null) throw new InvalidOperationException("Native renderer has a missing material.");
                var copy = new Material(originals[i]);
                copy.name = "LWC_Studio_" + originals[i].name;
                materials.Add(copy); copies[i] = copy;
                if (!chroma) continue;
                backdropMaterials.Add(copy);
                foreach (var property in new[] { "_MainTex", "_BumpMap", "_MetallicGlossMap", "_EmissionMap", "_DetailAlbedoMap", "_DetailNormalMap", "_MossTex", "_NoiseTex" })
                    if (copy.HasProperty(property)) copy.SetTexture(property, null);
                foreach (var keyword in new[] { "_NORMALMAP", "_METALLICGLOSSMAP", "_DETAIL_MULX2", "_EMISSION" }) copy.DisableKeyword(keyword);
                foreach (var property in new[] { "_Metallic", "_Glossiness", "_GlossMapScale", "_BumpScale", "_Moss" })
                    if (copy.HasProperty(property)) copy.SetFloat(property, 0f);
                if (copy.HasProperty("_EmissionColor")) copy.SetColor("_EmissionColor", Color.black);
            }
            return copies;
        }

        public void SetPalette(string palette)
        {
            CheckAlive();
            Color color;
            switch (palette)
            {
                case "green": color = new Color(0, 1, 0, 1); break;
                case "blue": color = new Color(0, 0, 1, 1); break;
                case "neutral": color = new Color(.502f, .502f, .502f, 1); break;
                default: throw new ArgumentException("Palette must be green, blue or neutral.", "palette");
            }
            // Preflight all materials before applying a palette, so unsupported
            // native shader versions cannot leave a partly recolored room.
            foreach (var material in backdropMaterials)
            {
                if (!material.HasProperty("_Color") && !material.HasProperty("_BaseColor"))
                    throw new InvalidOperationException("Native wood shader has no supported color property.");
            }
            foreach (var material in backdropMaterials)
            {
                foreach (var property in new[] { "_Color", "_BaseColor" })
                    if (material.HasProperty(property)) material.SetColor(property, color);
            }
        }

        public void SetMarkersVisible(bool visible)
        {
            CheckAlive();
            foreach (var marker in markers) if (marker != null) marker.SetActive(visible);
        }

        void Lamp(Vector3 at, float intensity, float range)
        {
            var lamp = new GameObject("NeutralPhotoLight");
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = at;
            var light = lamp.AddComponent<Light>();
            lamp.AddComponent<StudioLightMarker>();
            light.type = LightType.Point; light.color = Color.white;
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
        }

        /// <summary>Decorative visual copy only: no harvest, inventory, animation, AI, collisions or network identity.</summary>
        public void PlaceProp(string prefabName, Vector3 localPosition, float yaw)
        { PlaceProp(prefabName, localPosition, yaw, 1f); }

        public void PlaceProp(string prefabName, Vector3 localPosition, float yaw, float scale)
        {
            CheckAlive(); RequireFinite(localPosition);
            if (Single.IsNaN(yaw) || Single.IsInfinity(yaw)) throw new ArgumentException("Invalid rotation.");
            if (scale != 1f && scale != .75f && scale != .5f && scale != .25f) throw new ArgumentException("Unsupported scenery scale.");
            if (!AllowedProps.Contains(prefabName)) throw new ArgumentException("This scenery prefab is not approved.");
            if (props.Count >= PropLimit) throw new InvalidOperationException("Studio scenery limit reached.");
            if (Math.Abs(localPosition.x) > 13 || Math.Abs(localPosition.z) > 13 || localPosition.y < 0 || localPosition.y > 8)
                throw new ArgumentException("Scenery anchor must stay within the studio work area.");
            if (ZNetScene.instance == null) throw new InvalidOperationException("Native registry is unavailable.");
            var source = ZNetScene.instance.GetPrefab(prefabName);
            if (source == null) throw new InvalidOperationException("Approved vanilla scenery is unavailable in this game: " + prefabName);
            var holder = new GameObject("LWC_PhotoProp_" + prefabName);
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = localPosition;
            holder.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            holder.transform.localScale = new Vector3(scale, scale, scale);
            int firstMaterial = materials.Count;
            try
            {
                var lowerLods = LowerLodRenderers(source);
                int count = CopyVisualTree(source.transform, holder.transform, true, lowerLods);
                if (count == 0 || count > 256) throw new InvalidOperationException("Scenery has no supported bounded mesh representation.");
                var renderers = holder.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var renderer in renderers)
                {
                    var bounds = renderer.bounds;
                    Vector3 low = bounds.min - origin, high = bounds.max - origin;
                    if (low.x < -15.5f || high.x > 15.5f || low.z < -15.5f || high.z > 15.5f || low.y < -.25f || high.y > 11.5f)
                        throw new InvalidOperationException("Scenery extends outside the studio; choose a smaller item or another anchor.");
                }
                props.Add(holder);
            }
            catch
            {
                UnityEngine.Object.Destroy(holder);
                for (int i = materials.Count - 1; i >= firstMaterial; --i) { UnityEngine.Object.Destroy(materials[i]); materials.RemoveAt(i); }
                throw;
            }
        }

        // Copy one high-quality representation of an LODGroup, never every LOD
        // simultaneously. The native LODGroup is not cloned into the photograph.
        static HashSet<Renderer> LowerLodRenderers(GameObject source)
        {
            var all = new HashSet<Renderer>();
            var high = new HashSet<Renderer>();
            foreach (var group in source.GetComponentsInChildren<LODGroup>(true))
            {
                bool found = false;
                foreach (var lod in group.GetLODs())
                {
                    bool nonempty = false;
                    foreach (var renderer in lod.renderers)
                    {
                        if (renderer == null) continue;
                        all.Add(renderer);
                        nonempty = true;
                    }
                    if (!found && nonempty)
                    {
                        foreach (var renderer in lod.renderers) if (renderer != null) high.Add(renderer);
                        found = true;
                    }
                }
            }
            all.ExceptWith(high);
            return all;
        }

        int CopyVisualTree(Transform source, Transform parent, bool top, HashSet<Renderer> lowerLods)
        {
            var node = new GameObject(source.name + "_visual");
            node.transform.SetParent(parent, false);
            if (!top)
            {
                node.transform.localPosition = source.localPosition;
                node.transform.localRotation = source.localRotation;
                node.transform.localScale = source.localScale;
            }
            else node.transform.localScale = source.localScale;
            var filter = source.GetComponent<MeshFilter>();
            var renderer = source.GetComponent<MeshRenderer>();
            int count = 0;
            if (filter != null && filter.sharedMesh != null && renderer != null && renderer.enabled && !lowerLods.Contains(renderer))
            {
                node.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var copy = node.AddComponent<MeshRenderer>();
                copy.sharedMaterials = CopyMaterials(renderer.sharedMaterials, false);
                ConfigureRenderer(copy); count++;
            }
            for (int i = 0; i < source.childCount; i++)
            {
                if (!source.GetChild(i).gameObject.activeSelf) continue;
                count += CopyVisualTree(source.GetChild(i), node.transform, false, lowerLods);
                if (count > 256) throw new InvalidOperationException("Scenery mesh budget exceeded.");
            }
            return count;
        }

        static void ConfigureRenderer(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        public void ClearProps()
        {
            CheckAlive();
            foreach (var prop in props)
            {
                if (prop == null) continue;
                foreach (var renderer in prop.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (material != null && materials.Remove(material)) UnityEngine.Object.Destroy(material);
                UnityEngine.Object.Destroy(prop);
            }
            props.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (root != null) UnityEngine.Object.Destroy(root);
            foreach (var material in materials) if (material != null) UnityEngine.Object.Destroy(material);
            materials.Clear(); backdropMaterials.Clear(); props.Clear(); markers.Clear(); root = null;
        }

        void CheckAlive() { if (disposed || root == null) throw new ObjectDisposedException("StudioRoom"); }
        static void RequireFinite(Vector3 value)
        {
            if (Single.IsNaN(value.x) || Single.IsInfinity(value.x) || Single.IsNaN(value.y) || Single.IsInfinity(value.y) || Single.IsNaN(value.z) || Single.IsInfinity(value.z))
                throw new ArgumentException("Studio coordinates must be finite.");
        }
    }
}
