using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Invoked after the native Awake/Start sequence. No custom update loop drives
    // locomotion, attacks, damage, animation time, inventory or remote ownership.
    public sealed class HumanAppearance : MonoBehaviour
    {
        const string RevisionKey = "livingworld_human_rig_revision";
        static readonly BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo HairInstance = typeof(VisEquipment).GetField("m_hairItemInstance", Native);
        static readonly FieldInfo BeardInstance = typeof(VisEquipment).GetField("m_beardItemInstance", Native);
        static readonly FieldInfo CurrentHairHash = typeof(VisEquipment).GetField("m_currentHairItemHash", Native);
        static readonly FieldInfo CurrentBeardHash = typeof(VisEquipment).GetField("m_currentBeardItemHash", Native);
        public string[] RequiredItems = new string[0];
        public string WeaponName = "";
        public string State = "Awaiting native initialization";
        public bool Initialized;
        ZNetView view;
        Humanoid humanoid;
        float deadline;
        bool finished;
        bool hadAuthority;

        void Start()
        {
            view = GetComponent<ZNetView>(); humanoid = GetComponent<Humanoid>();
            deadline = Time.realtimeSinceStartup + 10f;
        }

        void Update()
        {
            if (finished) return;
            try
            {
                if (view == null || !view.IsValid()) { WaitOrFail("network view not ready"); return; }
                var composition = GetComponent<HumanCompositionIdentity>();
                if (composition == null || !composition.IsCompositionReady)
                {
                    hadAuthority = false;
                    State = "Awaiting explicit compatible composition: " + (composition == null ? "identity missing" : composition.Status);
                    return;
                }
                var zdo = view.GetZDO();
                string saved = zdo.GetString(RevisionKey, "");
                HumanRigPolicy.Require(HumanRigPolicy.RevisionCompatible(saved), "Saved human revision conflicts; refusing reinterpretation.");
                HumanProfile profile = null;
                var identity = GetComponent<HumanIdentity>();
                if (identity != null)
                    HumanRigPolicy.Require(identity.Read(out profile) != HumanProfileState.Refused, identity.Status);
                else
                    HumanRigPolicy.Require(zdo.GetString(HumanProfile.RecordKey, "") == "" && zdo.GetString(HumanProfile.RequiredKey, "") == "",
                        "Saved/required human profile exists without its identity adapter.");
                if (!view.IsOwner()) { hadAuthority = false; State = "Remote visual replication; no local initialization writes"; return; }
                if (!hadAuthority) { hadAuthority = true; deadline = Time.realtimeSinceStartup + 10f; }
                var vis = GetComponent<VisEquipment>();
                HumanRigPolicy.Require(vis != null && HairInstance != null && BeardInstance != null &&
                    CurrentHairHash != null && CurrentBeardHash != null, "Native facial-hair presentation members changed.");
                bool pendingAppearanceCommit = HumanRigPolicy.MayInitializeAppearance(true, true, saved);
                if (pendingAppearanceCommit)
                {
                    if (profile == null)
                    {
                        // Unchanged legacy scout path. A load never generates an
                        // identity or reinterprets an existing visual revision.
                        vis.SetModel(0);
                        vis.SetSkinColor(new Vector3(0.90f, 0.78f, 0.66f));
                        vis.SetHairColor(new Vector3(0.18f, 0.10f, 0.055f));
                        // Legacy actors without a saved profile receive the
                        // native empty customization, never a guessed prefab.
                        vis.SetHairItem(0);
                        vis.SetBeardItem(0);
                    }
                    else
                    {
                        identity.ValidateAppearance(profile);
                        // Native setters replicate the profile only before the
                        // visual commit marker. Owner loss cannot commit it.
                        if (!view.IsOwner()) { hadAuthority = false; return; }
                        vis.SetModel((int)profile.Sex);
                        if (!view.IsOwner()) { hadAuthority = false; return; }
                        vis.SetSkinColor(new Vector3(profile.SkinR / 255f, profile.SkinG / 255f, profile.SkinB / 255f));
                        if (!view.IsOwner()) { hadAuthority = false; return; }
                        vis.SetHairColor(new Vector3(profile.HairR / 255f, profile.HairG / 255f, profile.HairB / 255f));
                        if (!view.IsOwner()) { hadAuthority = false; return; }
                        vis.SetHairItem(profile.Hair.GetStableHashCode());
                        if (!view.IsOwner()) { hadAuthority = false; return; }
                        vis.SetBeardItem(profile.Beard == "" ? 0 : profile.Beard.GetStableHashCode());
                    }
                }
                string facialHairReason;
                if (profile != null && !FacialHairAttached(vis, profile, out facialHairReason))
                { WaitOrFail(facialHairReason); return; }
                // The revision is a visual commit marker, not merely proof that
                // hashes were written. A fresh actor cannot become ready while
                // its selected hair/beard failed to instantiate on the rig.
                if (pendingAppearanceCommit)
                {
                    if (!view.IsOwner()) { hadAuthority = false; return; }
                    zdo.Set(RevisionKey, HumanRigPolicy.Revision);
                }
                var items = humanoid.GetInventory().GetAllItems();
                if (!RequiredItems.All(name => items.Any(item => item.m_shared.m_name == name && item.m_equipped)))
                {
                    // Only native selection, only before the bounded initialization completes.
                    humanoid.EquipBestWeapon(null, null, null, null);
                    WaitOrFail("native default clothing/weapon/shield not all equipped"); return;
                }
                var weapon = humanoid.GetCurrentWeapon();
                HumanRigPolicy.Require(weapon != null && HumanRigPolicy.EquipmentReady(items.Contains(weapon), weapon.m_equipped, WeaponName, weapon.m_shared.m_name),
                    "Expected weapon was not selected from the native inventory.");
                NativeHumanFactory.Validate(gameObject, true);
                HumanRigPolicy.Require(!humanoid.IsPlayer() && GetComponent<Player>() == null, "NPC unexpectedly has player semantics.");
                Initialized = true; finished = true;
                State = "Human graph, equipped inventory and facial hair initialized" +
                    (profile == null ? "; legacy appearance; gameplay NOT certified" :
                    "; hair=" + profile.Hair + "; beard=" + (profile.Beard == "" ? "clean-shaven" : profile.Beard) +
                    "; gameplay NOT certified");
                Report(State);
            }
            catch (Exception error)
            {
                finished = true; Initialized = false;
                State = "FAIL human initialization: " + error.Message;
                var ai = GetComponent<MonsterAI>(); if (ai != null) ai.enabled = false;
                Report(State);
            }
        }

        void WaitOrFail(string reason)
        {
            State = "Awaiting " + reason;
            if (Time.realtimeSinceStartup >= deadline) throw new InvalidOperationException("10-second initialization deadline: " + reason);
        }

        static bool FacialHairAttached(VisEquipment vis, HumanProfile profile, out string reason)
        {
            int hair = profile.Hair.GetStableHashCode();
            int beard = profile.Beard == "" ? 0 : profile.Beard.GetStableHashCode();
            var hairObject = HairInstance.GetValue(vis) as GameObject;
            var beardObject = BeardInstance.GetValue(vis) as GameObject;
            if ((int)CurrentHairHash.GetValue(vis) != hair || hairObject == null)
            { reason = "native hair visual attachment for " + profile.Hair; return false; }
            if ((int)CurrentBeardHash.GetValue(vis) != beard || (beard != 0 && beardObject == null) || (beard == 0 && beardObject != null))
            { reason = "native beard visual attachment for " + (profile.Beard == "" ? "clean-shaven" : profile.Beard); return false; }
            reason = ""; return true;
        }

        static void Report(string message)
        {
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report(message);
        }
    }
}
