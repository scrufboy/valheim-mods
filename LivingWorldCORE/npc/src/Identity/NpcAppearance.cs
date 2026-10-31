using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.Core
{
    public sealed class NpcAppearance : MonoBehaviour
    {
        internal const string MarkerKey = "lwc_human_v2";
        internal const string NameKey = "lwc_human_v2_name";
        internal const string FemaleKey = "lwc_human_v2_female";
        internal const string HairNameKey = "lwc_human_v2_hair";
        internal const string BeardNameKey = "lwc_human_v2_beard";
        internal const string RoleKey = "lwc_human_v2_role";

        public string Role = "guard";
        public bool Ready { get; private set; }
        public string Status { get; private set; }
        public string DisplayName { get; private set; }
        public string HairName { get; private set; }
        public string BeardName { get; private set; }
        public bool Female { get; private set; }

        ZNetView view;
        VisEquipment visuals;
        Humanoid human;

        void Awake()
        {
            Status = "initializing native human";
            view = GetComponent<ZNetView>();
            visuals = GetComponent<VisEquipment>();
            human = GetComponent<Humanoid>();
            if (view == null || visuals == null || human == null || !view.IsValid())
            {
                Status = "invalid native human graph";
                return;
            }

            if (view.IsOwner() && !view.GetZDO().GetBool(MarkerKey, false)) CreateIdentity();
            ReadIdentity();
            if (!String.IsNullOrEmpty(DisplayName)) human.m_name = DisplayName;
        }

        void CreateIdentity()
        {
            int hairCount = CountContiguous("Hair");
            int beardCount = CountContiguous("Beard");
            int seed = unchecked((int)(view.GetZDO().m_uid.UserID ^ view.GetZDO().m_uid.ID ^ (uint)Guid.NewGuid().GetHashCode()));
            NpcIdentityRecord identity = NpcIdentityRecord.Create(new System.Random(seed), hairCount, beardCount);
            Vector3 skin = SkinColor(seed);
            Vector3 hairColor = HairColor(seed);

            // The cloned Player graph can carry request-cache values that do not
            // belong to its new ZDO. Make the first native setter observably dirty
            // without touching the rendered/current hashes.
            PrepareFreshNativeSelection(identity);

            visuals.SetModel(identity.Female ? 1 : 0);
            visuals.SetSkinColor(skin);
            visuals.SetHairColor(hairColor);
            visuals.SetHairItem(identity.Hair.GetStableHashCode());
            visuals.SetBeardItem(identity.Beard.Length == 0 ? 0 : identity.Beard.GetStableHashCode());

            ZDO zdo = view.GetZDO();
            zdo.Set(NameKey, identity.Name);
            zdo.Set(FemaleKey, identity.Female);
            zdo.Set(HairNameKey, identity.Hair);
            zdo.Set(BeardNameKey, identity.Beard);
            zdo.Set(RoleKey, Role);
            CommitNativeFacialHair(zdo, identity.Hair, identity.Beard);
            // MarkerKey means the identity is immutable. Presentation can be
            // repaired from it after a load; it must never be rerolled.
            zdo.Set(MarkerKey, true);
            InvokeNative(NativeApi.UpdateVisuals);
            AttachNativeFacialHair(identity.Hair, identity.Beard);
        }

        void ReadIdentity()
        {
            ZDO zdo = view.GetZDO();
            DisplayName = zdo.GetString(NameKey, "Living World Companion");
            Female = zdo.GetBool(FemaleKey, false);
            HairName = zdo.GetString(HairNameKey, "");
            BeardName = zdo.GetString(BeardNameKey, "");
            Role = zdo.GetString(RoleKey, Role);
        }

        IEnumerator Start()
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            float nextRepair = 0f;
            yield return null;
            while (Time.realtimeSinceStartup < deadline)
            {
                ReadIdentity();
                string reason;
                if (view.IsOwner() && Time.realtimeSinceStartup >= nextRepair && !VisibleFacialHair(out reason))
                {
                    // Older 1.0.x records can contain the immutable identity but
                    // no runtime attachment. Recommit only its exact native
                    // presentation and attach it; never reroll or rewrite identity.
                    RepairNativeFacialHair();
                    nextRepair = Time.realtimeSinceStartup + .25f;
                }
                if (VisibleFacialHair(out reason))
                {
                    human.m_name = DisplayName;
                    Ready = true;
                    Status = DisplayName + ": " + (Female ? "female" : "male") + ", hair=" + HairName +
                        ", beard=" + (BeardName.Length == 0 ? "none" : BeardName) + ", role=" + Role;
                    CorePlugin.Report("APPEARANCE READY " + Status);
                    yield break;
                }
                Status = reason;
                yield return null;
            }
            Ready = false;
            Status = "appearance failed: " + Status;
            MonsterAI ai = GetComponent<MonsterAI>();
            if (ai != null) ai.enabled = false;
            CorePlugin.Report("APPEARANCE FAIL " + Status);
        }

        void PrepareFreshNativeSelection(NpcIdentityRecord identity)
        {
            int hair = identity.Hair.GetStableHashCode();
            int beard = identity.Beard.Length == 0 ? 0 : identity.Beard.GetStableHashCode();
            NativeApi.RequestedModel.SetValue(visuals, identity.Female ? 0 : 1);
            NativeApi.RequestedSkinColor.SetValue(visuals, new Vector3(-1f, -1f, -1f));
            NativeApi.RequestedHairColor.SetValue(visuals, new Vector3(-1f, -1f, -1f));
            NativeApi.RequestedHairHash.SetValue(visuals, hair ^ Int32.MinValue);
            NativeApi.RequestedBeardHash.SetValue(visuals, beard ^ Int32.MinValue);
        }

        void RepairNativeFacialHair()
        {
            if (String.IsNullOrEmpty(HairName)) return;
            CommitNativeFacialHair(view.GetZDO(), HairName, BeardName);
            InvokeNative(NativeApi.UpdateVisuals);
            AttachNativeFacialHair(HairName, BeardName);
        }

        static void CommitNativeFacialHair(ZDO zdo, string hairName, string beardName)
        {
            if (zdo == null) throw new InvalidOperationException("Facial-hair selection has no native ZDO.");
            int hair = hairName.GetStableHashCode();
            int beard = String.IsNullOrEmpty(beardName) ? 0 : beardName.GetStableHashCode();
            zdo.Set(ZDOVars.s_hairItem, hair, false);
            zdo.Set(ZDOVars.s_beardItem, beard, false);
            if (zdo.GetInt(ZDOVars.s_hairItem, 0) != hair || zdo.GetInt(ZDOVars.s_beardItem, 0) != beard)
                throw new InvalidOperationException("Facial-hair selection did not persist to the native ZDO.");
        }

        void AttachNativeFacialHair(string hairName, string beardName)
        {
            int hair = hairName.GetStableHashCode();
            int beard = String.IsNullOrEmpty(beardName) ? 0 : beardName.GetStableHashCode();
            InvokeNative(NativeApi.SetHairEquipped, hair);
            InvokeNative(NativeApi.SetBeardEquipped, beard);
            InvokeNative(NativeApi.UpdateLodgroup);
            InvokeNative(NativeApi.UpdateColors);
        }

        object InvokeNative(MethodInfo method, params object[] arguments)
        {
            if (method == null) throw new MissingMethodException("Valheim facial-hair presentation member changed.");
            try { return method.Invoke(visuals, arguments); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }

        bool VisibleFacialHair(out string reason)
        {
            if (String.IsNullOrEmpty(HairName)) { reason = "saved hair is missing"; return false; }
            if (NativeApi.HairInstance == null || NativeApi.BeardInstance == null || NativeApi.CurrentHairHash == null || NativeApi.CurrentBeardHash == null)
            { reason = "Valheim facial-hair fields changed"; return false; }
            int expectedHair = HairName.GetStableHashCode();
            int expectedBeard = BeardName.Length == 0 ? 0 : BeardName.GetStableHashCode();
            int currentHair = (int)NativeApi.CurrentHairHash.GetValue(visuals);
            int currentBeard = (int)NativeApi.CurrentBeardHash.GetValue(visuals);
            GameObject hairObject = NativeApi.HairInstance.GetValue(visuals) as GameObject;
            GameObject beardObject = NativeApi.BeardInstance.GetValue(visuals) as GameObject;
            string renderReason = "missing";
            if (currentHair != expectedHair || hairObject == null || !AttachmentRenders(hairObject, out renderReason))
            {
                reason = "waiting for visible " + HairName + " (selected=" + expectedHair +
                    ", rendered=" + currentHair + ", object=" + (hairObject == null ? "missing" : renderReason) + ")";
                return false;
            }
            if (currentBeard != expectedBeard)
            {
                reason = "waiting for beard selection " + (BeardName.Length == 0 ? "none" : BeardName) +
                    " (selected=" + expectedBeard + ", rendered=" + currentBeard + ")";
                return false;
            }
            if (Female && beardObject != null)
            { reason = "female actor retained facial hair"; return false; }
            if (!Female && (BeardName.Length == 0 || beardObject == null || !AttachmentRenders(beardObject, out renderReason)))
            { reason = "waiting for visible male beard " + BeardName; return false; }
            reason = "";
            return true;
        }

        bool AttachmentRenders(GameObject attachment, out string reason)
        {
            if (!attachment.activeInHierarchy) { reason = "inactive"; return false; }
            Transform visualRoot = visuals.m_bodyModel == null ? null : visuals.m_bodyModel.transform.parent;
            if (visualRoot == null || (!ReferenceEquals(attachment.transform, visualRoot) && !attachment.transform.IsChildOf(visualRoot)))
            { reason = "outside native rig"; return false; }
            Renderer[] renderers = attachment.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) { reason = "no renderer"; return false; }
            LODGroup lod = visuals.m_bodyModel.GetComponentInParent<LODGroup>();
            Renderer[] lodRenderers = lod == null || lod.GetLODs().Length == 0 ? null : lod.GetLODs()[0].renderers;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0 ||
                    renderer.sharedMaterials.All(material => material == null)) continue;
                SkinnedMeshRenderer skin = renderer as SkinnedMeshRenderer;
                if (skin != null && (skin.sharedMesh == null || skin.rootBone == null ||
                    (!ReferenceEquals(skin.rootBone, visualRoot) && !skin.rootBone.IsChildOf(visualRoot)))) continue;
                if (lodRenderers != null && !lodRenderers.Contains(renderer)) continue;
                reason = "visible";
                return true;
            }
            reason = "no active materialized renderer in native LOD";
            return false;
        }

        static int CountContiguous(string prefix)
        {
            if (ObjectDB.instance == null) return 0;
            int count = 0;
            while (ObjectDB.instance.GetItemPrefab(prefix + (count + 1)) != null) count++;
            return count;
        }

        static Vector3 SkinColor(int seed)
        {
            Vector3[] colors = {
                new Vector3(.92f, .80f, .71f), new Vector3(.83f, .68f, .55f),
                new Vector3(.69f, .52f, .38f), new Vector3(.56f, .39f, .25f),
                new Vector3(.41f, .28f, .20f), new Vector3(.31f, .20f, .15f)
            };
            return colors[(seed & Int32.MaxValue) % colors.Length];
        }

        static Vector3 HairColor(int seed)
        {
            Vector3[] colors = {
                new Vector3(.12f, .09f, .07f), new Vector3(.25f, .16f, .09f),
                new Vector3(.44f, .28f, .14f), new Vector3(.70f, .54f, .26f),
                new Vector3(.47f, .19f, .10f), new Vector3(.61f, .59f, .55f)
            };
            return colors[((seed >> 5) & Int32.MaxValue) % colors.Length];
        }
    }
}
