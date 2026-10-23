using System;
using System.Collections;
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
            zdo.Set(MarkerKey, true);
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
            yield return null;
            while (Time.realtimeSinceStartup < deadline)
            {
                ReadIdentity();
                string reason;
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
            if (currentHair != expectedHair || hairObject == null || hairObject.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                reason = "waiting for visible " + HairName + " (selected=" + expectedHair +
                    ", rendered=" + currentHair + ", object=" + (hairObject == null ? "missing" : "present") + ")";
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
            if (!Female && (BeardName.Length == 0 || beardObject == null || beardObject.GetComponentsInChildren<Renderer>(true).Length == 0))
            { reason = "waiting for visible male beard " + BeardName; return false; }
            reason = "";
            return true;
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
