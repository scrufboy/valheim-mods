using System;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    public enum HumanProfileState { Absent, Ready, Refused }

    // Identity storage/representation only. No class/role assignment, stat
    // modifiers, equipment changes, world discovery or automatic generation.
    public sealed class HumanIdentity : MonoBehaviour
    {
        string observed;
        string displayed;
        HumanProfile cached;
        ZNetView network;
        Humanoid body;
        HumanCompositionIdentity composition;
        HumanAppearance appearance;
        float nextHydration;
        bool observedOwner, hasSeenProfile;
        public string Status { get; private set; }

        void Cache()
        {
            if (network == null) network = GetComponent<ZNetView>();
            if (body == null) body = GetComponent<Humanoid>();
            if (composition == null) composition = GetComponent<HumanCompositionIdentity>();
            if (appearance == null) appearance = GetComponent<HumanAppearance>();
        }

        bool TryView(out ZNetView view)
        {
            Cache(); view = network;
            return view != null && view.IsValid() && view.GetZDO() != null &&
                view.GetZDO().GetPrefab() == HumanMigrationPolicy.Identity.GetStableHashCode() &&
                GetComponent<Player>() == null && body != null &&
                composition != null && composition.IsCompositionReady;
        }

        public HumanProfileState Read(out HumanProfile profile)
        {
            profile = null;
            ZNetView view;
            if (!TryView(out view)) { Status = "Identity awaiting compatible human composition"; return HumanProfileState.Refused; }
            string required = view.GetZDO().GetString(HumanProfile.RequiredKey, "");
            if (required != "" && required != "1")
            { Status = "Unknown identity requirement preserved; human remains gated"; return HumanProfileState.Refused; }
            string record = view.GetZDO().GetString(HumanProfile.RecordKey, "");
            if (record.Length == 0)
            {
                observed = record; cached = null;
                bool refused = hasSeenProfile || required == "1";
                Status = refused ? "Required/previously observed identity is missing; human remains gated" : "Legacy human: no generated identity";
                return refused ? HumanProfileState.Refused : HumanProfileState.Absent;
            }
            hasSeenProfile = true;
            if (observed != record)
            {
                observed = record;
                HumanProfile.TryRead(record, out cached);
            }
            if (cached == null) { Status = "Unknown or malformed identity preserved; human remains gated"; return HumanProfileState.Refused; }
            profile = cached; Status = "Persisted identity: " + profile.Name;
            return HumanProfileState.Ready;
        }

        // Call synchronously for the exact instance returned by the explicit
        // fresh spawn command, AFTER composition.InitializeFresh and BEFORE the
        // first Update. Saved-instance discovery and lifecycle callbacks must
        // never call this method. Existing/unknown bytes are never overwritten.
        public void InitializeFresh(string seed, string[] availableHair, string[] availableBeards)
        {
            ZNetView view;
            if (!TryView(out view) || !view.IsOwner()) throw new InvalidOperationException("Identity initialization requires a compatible locally owned human.");
            string required = view.GetZDO().GetString(HumanProfile.RequiredKey, "");
            if (required != "" && required != "1") throw new InvalidOperationException("Unknown identity requirement preserved; initialization refused.");
            string existing = view.GetZDO().GetString(HumanProfile.RecordKey, "");
            if (existing.Length != 0)
            {
                HumanProfile prior;
                if (!HumanProfile.TryRead(existing, out prior)) throw new InvalidOperationException("Unknown/malformed identity preserved; initialization refused.");
                Status = "Existing human identity preserved: " + prior.Name;
                return; // Idempotent, even after appearance commit; never reroll.
            }
            view = FreshView();
            var profile = HumanProfile.Generate(seed, availableHair, availableBeards);
            string record = profile.Save();
            ValidateAppearance(profile);
            // Recheck the complete gate immediately at the only persistence
            // write boundary, including after all asset validation.
            if (FreshView() != view) throw new InvalidOperationException("Human view changed during identity initialization.");
            view.GetZDO().Set(HumanProfile.RecordKey, record);
            if (view.GetZDO().GetString(HumanProfile.RecordKey, "") != record)
                throw new InvalidOperationException("Fresh identity could not be read back; no retry or reroll performed.");
            observed = null; cached = null; nextHydration = 0;
            Status = "Fresh human identity persisted";
        }


        ZNetView FreshView()
        {
            ZNetView view;
            if (!TryView(out view) || !view.IsOwner()) throw new InvalidOperationException("Identity creation requires a compatible locally owned human; authority is never claimed.");
            var zdo = view.GetZDO();
            if (zdo.GetString(HumanProfile.RequiredKey, "") != "1" || zdo.GetString(HumanProfile.RecordKey, "") != "" || zdo.GetString(HumanMigrationPolicy.VisualKey, "") != "" ||
                zdo.GetString(HumanMigrationPolicy.OriginKey, "") != HumanMigrationPolicy.FreshOrigin ||
                zdo.GetString(HumanMigrationPolicy.CompositionKey, "") != HumanMigrationPolicy.CompositionRevision)
                throw new InvalidOperationException("Only an explicitly initialized fresh human without identity/appearance may receive a generated profile.");
            return view;
        }

        internal void ValidateAppearance(HumanProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            var vis = GetComponent<VisEquipment>();
            int model = (int)profile.Sex;
            if (vis == null || !vis.m_isPlayer || vis.m_playerComponent != null || vis.m_models == null ||
                model < 0 || model >= vis.m_models.Length || vis.m_models[model] == null ||
                vis.m_models[model].m_mesh == null || vis.m_models[model].m_baseMaterial == null)
                throw new InvalidOperationException("Selected native human body model is unavailable.");
            ValidateCustomization(profile.Hair, "hair");
            if (profile.Beard != "") ValidateCustomization(profile.Beard, "beard");
        }

        static void ValidateCustomization(string name, string kind)
        {
            var prefab = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(name);
            var drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            bool hasAttachment = prefab != null &&
                (prefab.transform.Find("attach") != null || prefab.transform.Find("attach_skin") != null);
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null ||
                drop.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Customization || !hasAttachment)
                throw new InvalidOperationException("Selected native human " + kind + " asset cannot attach to the human rig; saved identity is preserved: " + name);
        }

        void Update()
        {
            Cache();
            bool ownerNow = network != null && network.IsValid() && network.IsOwner();
            // Rehydrate at most twice per second, plus immediately on authority
            // changes. This adapter has no simulation or per-frame name write.
            if (Time.realtimeSinceStartup < nextHydration && ownerNow == observedOwner) return;
            nextHydration = Time.realtimeSinceStartup + .5f; observedOwner = ownerNow;
            HumanProfile profile;
            var state = Read(out profile);
            if (state == HumanProfileState.Ready)
            {
                // m_name is a local display field, not a network write. Every
                // peer hydrates it from the same immutable persisted record.
                if (displayed != observed || body.m_name != profile.Name) { body.m_name = profile.Name; displayed = observed; }
            }
            else if (state == HumanProfileState.Refused)
            {
                if (appearance != null) appearance.Initialized = false;
            }
        }
    }
}
