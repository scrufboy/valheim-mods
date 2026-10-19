using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LivingWorld.ManualTest
{
    // Disposable, manually requested native building fixtures. No service AI.
    internal static class ManualLabBuilder
    {
        internal const string LabTag = "livingworld_manual_lab_v1";
        internal const int ObjectBudget = 128;
        const float HalfWidth = 10f, HalfDepth = 8f, SiteMargin = 0.6f;

        sealed class Placement
        {
            internal string Prefab, Role, Text;
            internal Vector3 Position;
            internal float Yaw;
            internal bool Ground, Slope, AlignTop;
            internal Vector3 Rise;
            internal float DamageFraction;
        }

        internal static void ValidateSite(Vector3 origin)
        {
            if (ZoneSystem.instance == null || ZNetScene.instance == null || Player.m_localPlayer == null)
                throw new InvalidOperationException("Enter the disposable world before creating the lab.");
            if (!Finite(origin.x) || !Finite(origin.y) || !Finite(origin.z))
                throw new InvalidOperationException("Lab origin is not finite.");
            float low, high; SampleTerrain(origin, out low, out high);
            if (high - low > 1.2f)
                throw new InvalidOperationException("Lab needs a dry, reasonably flat 20 x 16 metre clearing (terrain spread must be at most 1.2 metres). Move to another site; terrain will not be edited.");
            if (low < ZoneSystem.instance.m_waterLevel + 1f)
                throw new InvalidOperationException("Lab site is too close to water. Choose dry ground at least one metre above the water level.");

            var center = new Vector3(origin.x, (low + high) * 0.5f + 3.5f, origin.z);
            var extent = new Vector3(HalfWidth + SiteMargin, (high - low) * 0.5f + 4f, HalfDepth + SiteMargin);
            foreach (var collider in Physics.OverlapBox(center, extent, Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
            {
                if (collider == null || !collider.enabled) continue;
                // Reject characters and existing structures even when their collider is a trigger.
                if (ManualAcceptance.BlocksLabFootprint(collider.GetComponentInParent<Character>() != null,
                    collider.GetComponentInParent<Piece>() != null))
                    throw new InvalidOperationException("The lab footprint contains a character or existing structure: " + collider.name + ". Stand outside the footprint and choose a clear site.");
                // Do not reject the user's prepared plot for an unidentified
                // generic collider such as 'visual'. Scenery and dropped items
                // remain untouched; real occupants and structures stay protected.
            }
            // Some objects have no active collider; inspect their roots as well.
            foreach (var piece in UnityEngine.Object.FindObjectsOfType<Piece>())
                if (Inside(piece.transform.position, origin)) throw new InvalidOperationException("An existing piece occupies the lab footprint: " + piece.name);
            foreach (var character in UnityEngine.Object.FindObjectsOfType<Character>())
                if (Inside(character.transform.position, origin)) throw new InvalidOperationException("A character occupies the lab footprint: " + character.name);
        }

        internal static List<GameObject> Build(Vector3 origin, Action<string> report)
        {
            if (report == null) throw new ArgumentNullException("report");
            ValidateSite(origin);
            float low, high; SampleTerrain(origin, out low, out high);
            origin.y = high + 0.15f;
            var plan = CreatePlan();
            if (plan.Count > ObjectBudget) throw new InvalidOperationException("Lab exceeds its bounded object budget.");
            var prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (var p in plan)
            {
                PreflightPrefab(prefabs, p.Prefab, true);
                var prefab = prefabs[p.Prefab];
                if (p.Text != null && prefab.GetComponent<Sign>() == null)
                    throw new InvalidOperationException("Native sign prefab has no Sign component.");
                if ((p.Role == "input" || p.Role == "output" || p.Role == "workshop-supplies") && prefab.GetComponent<Container>() == null)
                    throw new InvalidOperationException("Native chest prefab has no Container component.");
                if (p.Prefab == "fire_pit" && (prefab.GetComponent<Fireplace>() == null || prefab.GetComponent<Fireplace>().m_maxFuel < 1f))
                    throw new InvalidOperationException("Native fire prefab has no usable Fireplace component.");
                if (p.DamageFraction > 0f && (prefab.GetComponent<WearNTear>() == null || prefab.GetComponent<WearNTear>().m_health <= 0f))
                    throw new InvalidOperationException("Native repair prefab has no usable WearNTear component.");
                if (p.Prefab == "bed" && prefab.GetComponent<Bed>() == null)
                    throw new InvalidOperationException("Native bed prefab has no Bed component.");
                if ((p.Prefab == "piece_workbench" || p.Prefab == "piece_cauldron") && prefab.GetComponent<CraftingStation>() == null)
                    throw new InvalidOperationException("Native station prefab has no CraftingStation component: " + p.Prefab);
                if (p.Prefab == "piece_cookingstation" && prefab.GetComponent<CookingStation>() == null)
                    throw new InvalidOperationException("Native cooking prefab has no CookingStation component.");
            }
            foreach (var item in new[] { "Wood", "Stone", "Resin", "RawMeat", "NeckTail", "Mushroom", "Raspberry", "Carrot", "Hammer" })
                PreflightPrefab(prefabs, item, false);
            // Validate all structural snap geometry before spawning anything.
            foreach (var p in plan) PlacementTransform(prefabs[p.Prefab], p, origin, out low, true);

            var player = Player.m_localPlayer;
            var creator = player.GetPlayerID();
            var platform = Splatform.PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
            if (creator == 0) throw new InvalidOperationException("Local character has no valid creator identity.");
            var created = new List<GameObject>();
            try
            {
                foreach (var p in plan)
                {
                    float yaw;
                    var position = PlacementTransform(prefabs[p.Prefab], p, origin, out yaw, false);
                    var go = UnityEngine.Object.Instantiate(prefabs[p.Prefab], position, Quaternion.Euler(0f, yaw, 0f));
                    if (go == null) throw new InvalidOperationException("Native instantiation failed: " + p.Prefab);
                    created.Add(go); // Track before any operation which can fail.
                    var view = go.GetComponent<ZNetView>();
                    if (view == null || !view.IsValid() || !view.IsOwner() || view.GetZDO() == null)
                        throw new InvalidOperationException("Lab piece did not receive local owner authority: " + p.Prefab);
                    view.GetZDO().Set(LabTag, "piece");
                    view.GetZDO().Set("livingworld_manual_lab_role", p.Role);
                    var piece = go.GetComponent<Piece>();
                    piece.SetCreator(creator, platform);
                    if (piece.GetCreator() != creator) throw new InvalidOperationException("Lab piece creator assignment failed: " + p.Prefab);
                    if (p.Text != null)
                    {
                        var sign = go.GetComponent<Sign>();
                        if (sign == null) throw new InvalidOperationException("Native sign component missing.");
                        ((TextReceiver)sign).SetText(p.Text);
                    }
                    if (p.DamageFraction > 0f)
                    {
                        var wear = go.GetComponent<WearNTear>();
                        if (wear == null || wear.m_health <= 0f) throw new InvalidOperationException("Repair target lacks native health.");
                        wear.ApplyDamage(wear.m_health * p.DamageFraction, null);
                        if (wear.GetHealthPercentage() >= 0.99f) throw new InvalidOperationException("Repair target damage was not applied.");
                    }
                    var fire = go.GetComponent<Fireplace>();
                    if (fire != null) fire.SetFuel(Math.Min(5f, fire.m_maxFuel));
                    var container = go.GetComponent<Container>();
                    if (container != null && p.Role == "input")
                    {
                        AddSupplies(container, prefabs, "Wood", 50); AddSupplies(container, prefabs, "RawMeat", 10);
                        AddSupplies(container, prefabs, "NeckTail", 10); AddSupplies(container, prefabs, "Mushroom", 10);
                        AddSupplies(container, prefabs, "Raspberry", 10); AddSupplies(container, prefabs, "Carrot", 10);
                    }
                    if (container != null && p.Role == "workshop-supplies")
                    {
                        AddSupplies(container, prefabs, "Wood", 50); AddSupplies(container, prefabs, "Stone", 30);
                        AddSupplies(container, prefabs, "Resin", 10); AddSupplies(container, prefabs, "Hammer", 1);
                    }
                }
                report("LAB BUILT: " + created.Count + " native pieces. Origin " + Format(origin) + "; footprint 20m east-west x 16m north-south. No terrain edit; ordinary native stability and wear remain active.");
                report("HOUSE: west (-5,-2), roofed 4m x 4m room, south door, bed, outside bed fire on its east side. Claim the bed manually; sleep still requires native shelter, warmth, and safety.");
                report("WORKSHOP: southeast (3,-2), roofed 4m x 4m shed, workbench and supplies chest. REPAIR: east (6,0), lower wall at 55% health and upper wall at 65% health, supported by ordinary wooden foundations.");
                report("KITCHEN: northwest, cooking station (-6,2) and cauldron (-2,2) each over a separate ground fire; roofed storage behind with open sky above fires for smoke ventilation. INPUT (-5,5) contains actual ingredients and wood. OUTPUT (-2,5) starts empty.");
                report("GUARD: northeast (6,4), low platform and roof. Ground walking lane: rectangle corners (-9,-7), (9,-7), (9,7), (-9,7), relative to origin. Signs identify areas. NPC household, repair, cooking, and guard services are pending unless separately exercised by implemented commands.");
                report("OBSERVE: wait 30 seconds for native support/fire checks, then verify all roofs stand, both kitchen fires burn, workbench works, chest quantities persist, and bed reports native conditions. A collapsed piece or blocked station is a lab-layout failure; record it before retrying.");
                return created;
            }
            catch
            {
                Cleanup(created, report);
                throw;
            }
        }

        internal static void Cleanup(IList<GameObject> created, Action<string> report)
        {
            var failures = 0;
            for (var i = created.Count - 1; i >= 0; --i)
            {
                var go = created[i]; if (go == null) continue;
                try
                {
                    var view = go.GetComponent<ZNetView>();
                    if (view != null && view.IsValid())
                    {
                        if (!view.IsOwner() || ZNetScene.instance == null) { failures++; continue; }
                        ZNetScene.instance.Destroy(go);
                    }
                    else UnityEngine.Object.Destroy(go);
                }
                catch { failures++; }
            }
            if (failures != 0 && report != null) report("LAB CLEANUP INCOMPLETE: " + failures + " owned fixture objects could not be removed. Do not repeat setup until inspected; use the disposable world's reset procedure.");
        }

        static List<Placement> CreatePlan()
        {
            var p = new List<Placement>();
            Room(p, -7f, -4f, true, "house");
            Room(p, 1f, -4f, false, "workshop");
            Add(p, "bed", -4.2f, 0.03f, -2.8f, 180f, "bed");
            Add(p, "fire_pit", -2f, 0.02f, -2.8f, 0f, "bed-fire").Ground = true;
            Add(p, "piece_workbench", 3f, 0.02f, -3.1f, 180f, "workbench");
            Add(p, "piece_chest_wood", 4.1f, 0.03f, -1.1f, 270f, "workshop-supplies");

            // Two adjacent repair targets: ground floor and one storey above, both normally supported.
            Support(p, 6f, -1f, "repair-support"); Support(p, 6f, 1f, "repair-support");
            Add(p, "woodwall", 6f, 0f, 0f, 90f, "repair-ground").DamageFraction = 0.45f;
            Add(p, "woodwall", 6f, 2f, 0f, 90f, "repair-elevated").DamageFraction = 0.35f;

            Add(p, "fire_pit", -6f, 0.02f, 2f, 0f, "cooking-fire").Ground = true;
            Add(p, "piece_cookingstation", -6f, 0.02f, 2f, 0f, "cooking-station").Ground = true;
            Add(p, "fire_pit", -2f, 0.02f, 2f, 0f, "cauldron-fire").Ground = true;
            Add(p, "piece_cauldron", -2f, 0.02f, 2f, 0f, "cauldron").Ground = true;
            // Storage canopy has a completely open fire-side edge; no roof lies over either fire.
            for (int x = -6; x <= 0; x += 2)
            {
                Support(p, x, 4f, "kitchen-support"); Support(p, x, 6f, "kitchen-support");
                Add(p, "wood_pole2", x, 0f, 4f, 0f, "kitchen-post");
                Add(p, "wood_pole2", x, 2f, 4f, 0f, "kitchen-post");
                Add(p, "wood_pole2", x, 0f, 6f, 0f, "kitchen-post");
            }
            for (int x = -5; x <= -1; x += 2) Slope(p, "wood_roof_45", x, 2f, 6f, Vector3.back, false, "kitchen-roof");
            Add(p, "piece_chest_wood", -5f, 0.02f, 5f, 180f, "input").Ground = true;
            Add(p, "piece_chest_wood", -2f, 0.02f, 5f, 180f, "output").Ground = true;

            for (int x = 4; x <= 8; x += 2)
                for (int z = 3; z <= 5; z += 2) { Support(p, x, z, "guard-support"); Add(p, "wood_pole2", x, 0f, z, 0f, "guard-post"); }
            Add(p, "wood_floor", 5f, 0f, 4f, 0f, "guard-floor"); Add(p, "wood_floor", 7f, 0f, 4f, 0f, "guard-floor");
            Slope(p, "wood_roof_45", 4f, 2f, 4f, Vector3.right, false, "guard-roof");
            Slope(p, "wood_roof_45", 8f, 2f, 4f, Vector3.left, false, "guard-roof");
            Slope(p, "wood_stepladder", 5f, 0f, 3f, Vector3.forward, true, "guard-step");
            Sign(p, -7f, 0f, 0.6f, "HOUSE", "house-label");
            Sign(p, 1f, 0f, 0.6f, "WORKSHOP / REPAIR", "workshop-label");
            Sign(p, -5f, 0f, 3.5f, "INPUT", "input-label");
            Sign(p, -2f, 0f, 3.5f, "OUTPUT", "output-label");
            Sign(p, 7f, 0f, 2.5f, "GUARD", "guard-label");
            return p;
        }

        static void Room(List<Placement> p, float x, float z, bool door, string role)
        {
            for (int ix = 0; ix <= 4; ix += 2)
                for (int iz = 0; iz <= 4; iz += 2) Support(p, x + ix, z + iz, role + "-foundation");
            for (int ix = 1; ix <= 3; ix += 2)
                for (int iz = 1; iz <= 3; iz += 2) Add(p, "wood_floor", x + ix, 0f, z + iz, 0f, role + "-floor");
            for (int i = 1; i <= 3; i += 2)
            {
                Add(p, "woodwall", x, 0f, z + i, 90f, role + "-wall");
                Add(p, "woodwall", x + 4f, 0f, z + i, 90f, role + "-wall");
                Add(p, "woodwall", x + i, 0f, z + 4f, 0f, role + "-wall");
                if (door) Add(p, i == 1 ? "wood_door" : "woodwall", x + i, 0f, z, 0f, i == 1 ? "house-door" : role + "-wall");
                Slope(p, "wood_roof_45", x, 2f, z + i, Vector3.right, false, role + "-roof");
                Slope(p, "wood_roof_45", x + 4f, 2f, z + i, Vector3.left, false, role + "-roof");
            }
            foreach (var end in new[] { z, z + 4f })
            {
                Slope(p, "wood_wall_roof_45", x + 1f, 2f, end, Vector3.right, false, role + "-gable");
                Slope(p, "wood_wall_roof_45", x + 3f, 2f, end, Vector3.left, false, role + "-gable");
            }
            Slope(p, "wood_stepladder", x + 1f, 0f, z, Vector3.forward, true, role + "-step");
        }

        static Placement Add(List<Placement> p, string prefab, float x, float y, float z, float yaw, string role)
        {
            var item = new Placement { Prefab = prefab, Position = new Vector3(x, y, z), Yaw = yaw, Role = role };
            p.Add(item); return item;
        }
        static void Support(List<Placement> p, float x, float z, string role) { Add(p, "wood_pole2", x, 0f, z, 0f, role).AlignTop = true; }
        static void Slope(List<Placement> p, string name, float x, float y, float z, Vector3 rise, bool top, string role)
        {
            var item = Add(p, name, x, y, z, 0f, role); item.Slope = true; item.Rise = rise; item.AlignTop = top;
        }
        static void Sign(List<Placement> p, float x, float y, float z, string text, string role)
        {
            // Standalone sign is anchored by a normal pole, not a disabled support check.
            Support(p, x, z, role + "-support"); Add(p, "wood_pole", x, 0f, z, 0f, role + "-post");
            Add(p, "sign", x, 0.7f, z - 0.06f, 0f, role).Text = text;
        }

        static Vector3 PlacementTransform(GameObject prefab, Placement p, Vector3 origin, out float yaw, bool preflight)
        {
            yaw = p.Yaw;
            var desired = origin + p.Position;
            if (p.Ground) desired.y = Ground(desired) + p.Position.y;
            var structural = p.Prefab.StartsWith("wood", StringComparison.Ordinal);
            if (!structural) return desired;
            var points = new List<Transform>(); prefab.GetComponent<Piece>().GetSnapPoints(points);
            if (points.Count == 0) throw new InvalidOperationException("Native structural snap points unavailable: " + p.Prefab);
            var min = float.PositiveInfinity; var max = float.NegativeInfinity;
            var local = new List<Vector3>();
            foreach (var point in points)
            {
                var v = prefab.transform.InverseTransformPoint(point.position); local.Add(v);
                min = Math.Min(min, v.y); max = Math.Max(max, v.y);
            }
            Vector3 bottom = Vector3.zero, top = Vector3.zero; int nb = 0, nt = 0;
            foreach (var v in local)
            {
                if (Math.Abs(v.y - min) < 0.02f) { bottom += v; nb++; }
                if (Math.Abs(v.y - max) < 0.02f) { top += v; nt++; }
            }
            bottom /= nb; top /= nt;
            if (p.Slope)
            {
                var rise = top - bottom; rise.y = 0f;
                if (rise.sqrMagnitude < 0.05f || max - min < 0.5f)
                    throw new InvalidOperationException("Native sloped-piece geometry cannot be aligned: " + p.Prefab);
                yaw = Mathf.Atan2(p.Rise.x, p.Rise.z) * Mathf.Rad2Deg - Mathf.Atan2(rise.x, rise.z) * Mathf.Rad2Deg;
            }
            if (p.Prefab == "wood_pole2" && max - min < 1.8f)
                throw new InvalidOperationException("Native foundation pole is too short for the permitted site slope.");
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var result = desired - rotation * (p.AlignTop ? top : bottom);
            if (p.AlignTop && p.Prefab == "wood_pole2")
            {
                var ground = Ground(desired);
                if (desired.y - ground > (max - min) - 0.1f || ground > desired.y + 0.05f)
                    throw new InvalidOperationException("A foundation would be unsupported or buried. Choose a flatter clearing.");
            }
            if (p.Prefab == "wood_stepladder")
            {
                var foot = result + rotation * bottom;
                if (foot.y > Ground(foot) + 0.1f)
                    throw new InvalidOperationException("The entry step would float above the terrain. Choose a flatter clearing.");
            }
            return result;
        }

        static void PreflightPrefab(IDictionary<string, GameObject> prefabs, string name, bool piece)
        {
            if (prefabs.ContainsKey(name)) return;
            var prefab = ZNetScene.instance.GetPrefab(name);
            if (prefab == null || (piece && (prefab.GetComponent<Piece>() == null || prefab.GetComponent<ZNetView>() == null)) || (!piece && prefab.GetComponent<ItemDrop>() == null))
                throw new InvalidOperationException("Required native lab prefab is absent or incompatible: " + name + ". No lab pieces were created.");
            prefabs.Add(name, prefab);
        }
        static void AddSupplies(Container chest, IDictionary<string, GameObject> prefabs, string name, int amount)
        {
            var prefab = prefabs[name]; var max = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize;
            if (max < 1) throw new InvalidOperationException("Invalid native item stack size: " + name);
            while (amount > 0)
            {
                var count = Math.Min(max, amount);
                if (!chest.GetInventory().AddItem(prefab, count)) throw new InvalidOperationException("Lab supply chest rejected " + name);
                amount -= count;
            }
        }
        static void SampleTerrain(Vector3 origin, out float low, out float high)
        {
            low = float.PositiveInfinity; high = float.NegativeInfinity;
            for (int x = -10; x <= 10; x += 2)
                for (int z = -8; z <= 8; z += 2)
                {
                    var h = Ground(origin + new Vector3(x, 0f, z));
                    low = Math.Min(low, h); high = Math.Max(high, h);
                }
        }
        static float Ground(Vector3 point)
        {
            float h;
            if (!ZoneSystem.instance.GetGroundHeight(point, out h) || !Finite(h))
                throw new InvalidOperationException("Terrain is not fully loaded under the lab footprint. Wait for loading or choose another clearing.");
            return h;
        }
        static bool Inside(Vector3 point, Vector3 origin) { return Math.Abs(point.x - origin.x) <= HalfWidth + SiteMargin && Math.Abs(point.z - origin.z) <= HalfDepth + SiteMargin; }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        static string Format(Vector3 v) { return String.Format(CultureInfo.InvariantCulture, "({0:F1}, {1:F1}, {2:F1})", v.x, v.y, v.z); }
    }
}
