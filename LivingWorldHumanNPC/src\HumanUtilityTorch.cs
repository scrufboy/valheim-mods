using System;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    public sealed class HumanUtilityTorch : MonoBehaviour
    {
        Humanoid body;
        ZNetView view;
        HumanCompanion companion;
        ItemDrop.ItemData torch;
        float nextCheck, eligibleSince = -1, lastCombat = Single.NegativeInfinity;
        float observedLight = Single.PositiveInfinity, observedFog, nearestBuiltLight = Single.PositiveInfinity;
        bool nativeNight, nativeDaylight, nearBuiltLight;
        bool fault;
        string state = "starting";
        static readonly MethodInfo Copy = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo LeftItem = typeof(Humanoid).GetField("m_leftItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        static readonly FieldInfo RightItem = typeof(Humanoid).GetField("m_rightItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        void Update()
        {
            if (Time.time < nextCheck || fault) return;
            nextCheck = Time.time + .5f;
            try
            {
                if (body == null) body = GetComponent<Humanoid>();
                if (view == null) view = GetComponent<ZNetView>();
                if (companion == null) companion = GetComponent<HumanCompanion>();
                if (view == null || !view.IsValid() || !view.IsOwner() || body == null || body.IsDead() || !GetComponent<HumanAppearance>().Initialized) return;
                // Test feature is explicit opt-in, never changes an unprepared scout.
                if (view.GetZDO().GetString(HumanPartyEquipment.RecordKey, "") != "archer" && !HumanCleric.IsCleric(gameObject)) return;
                EnsureTorch();
                long owner; Player leader;
                if (!companion.TryGetConfidenceParty(out owner, out leader) || leader == null) { Disable("no bound party leader"); return; }
                if (companion.CombatOwnsActor)
                { lastCombat = Time.time; Disable("combat owns actor"); return; }
                if (!companion.AreaClearVerified) { Disable("area clear not verified"); return; }
                // Torches are companion ambience, never combat equipment. A
                // full five quiet seconds must follow the last combat signal so
                // a transient target/alert gap cannot put one back in a hand.
                if (Time.time - lastCombat < 5f) { Disable("five-second combat clear"); return; }
                bool busy = body.InAttack() || body.IsStaggering() || body.IsSwimming() || body.IsCrouching();
                var cleric = GetComponent<HumanCleric>();
                busy |= cleric != null && cleric.Casting;
                // A bow or raised shield always wins. Peaceful sword/shield can stow
                // the shield temporarily; the equipment adapter restores it in combat.
                var weapon = body.GetCurrentWeapon();
                busy |= weapon != null && weapon.m_shared.m_attack.m_bowDraw;
                bool dark = AmbientDark();
                observedFog = RenderSettings.fog ? RenderSettings.fogDensity : 0f;
                nearBuiltLight = NearbyBuiltLight(nearBuiltLight);
                bool stealth = leader.IsCrouching();
                bool allowed = PartyCombatPolicy.Torch(dark, nearBuiltLight, stealth, companion.CombatOwnsActor, busy);
                if (!allowed)
                {
                    Disable(!dark ? "vanilla day" : nearBuiltLight ? "within six-metre built-light shelter" : stealth ? "leader stealth" :
                        busy ? "hands/body busy" : "utility light denied");
                    return;
                }
                // Only a real crafting pose owns the peaceful hands. Guard and
                // observe movement remain compatible with carrying a torch.
                var posture = GetComponent<HumanCompanionPosture>();
                if (posture != null && (posture.OwnsAmbientHands || posture.HasAmbientPose))
                { Disable("ambient crafting owns hands"); return; }
                if (eligibleSince < 0) eligibleSince = Time.time;
                if (Time.time - eligibleSince < 3f) { SetState("waiting for sustained darkness"); return; }
                if (TorchVisible()) { SetState("on"); return; }
                EnsureTorch();
                // Saved inventories can retain m_equipped while the native hand
                // slots contain no torch after load. Never treat that metadata
                // flag as visible equipment: clear it, then run the real native
                // equip transaction so the hand model and light are restored.
                if (torch.m_equipped && !TorchVisible()) body.UnequipItem(torch, false);
                if (body.EquipItem(torch, false) && TorchVisible()) SetState("on");
                else Disable("native equip refused");
            }
            catch (Exception error) { fault = true; Disable("fault: " + error.Message); }
        }

        bool AmbientDark()
        {
            var manager = EnvMan.instance;
            Color ambient = RenderSettings.ambientLight;
            observedLight = PartyCombatPolicy.PerceivedLight(ambient.r, ambient.g, ambient.b, RenderSettings.ambientIntensity);
            if (manager == null) { nativeNight = false; nativeDaylight = false; return false; }
            nativeNight = EnvMan.IsNight();
            nativeDaylight = EnvMan.IsDaylight();
            // IsNight follows Valheim's own quarter-day boundaries. The
            // IsDaylight complement also admits always-dark environments.
            return nativeNight || !nativeDaylight;
        }

        bool NearbyBuiltLight(bool previous)
        {
            float nearest = Single.PositiveInfinity;
            foreach (var collider in Physics.OverlapSphere(transform.position, 10f))
            {
                var piece = collider.GetComponentInParent<Piece>();
                if (piece == null) continue;
                foreach (var light in piece.GetComponentsInChildren<Light>())
                {
                    if (!light.enabled || !light.gameObject.activeInHierarchy || light.intensity <= .25f) continue;
                    float distance = Vector3.Distance(light.transform.position, body.GetCenterPoint());
                    if (distance < nearest) nearest = distance;
                }
            }
            nearestBuiltLight = nearest;
            return PartyCombatPolicy.BuiltLightNearby(previous, nearest);
        }

        void EnsureTorch()
        {
            if (LeftItem == null || RightItem == null) throw new MissingFieldException("Native humanoid hand slots changed");
            if (torch != null && body.GetInventory().GetAllItems().Contains(torch)) return;
            var source = ObjectDB.instance.GetItemPrefab("Torch");
            if (source == null || source.GetComponent<ItemDrop>() == null) throw new InvalidOperationException("Runtime Torch item unavailable");
            foreach (var item in body.GetInventory().GetAllItems())
                if (item.m_dropPrefab != null && item.m_dropPrefab.name == "Torch") { torch = item; break; }
            if (torch == null)
            {
                torch = source.GetComponent<ItemDrop>().m_itemData.Clone(); torch.m_dropPrefab = source;
                if (!body.GetInventory().AddItem(torch)) { torch = null; throw new InvalidOperationException("No utility item slot available"); }
            }
            torch.m_shared = (ItemDrop.ItemData.SharedData)Copy.Invoke(torch.m_shared, null);
            torch.m_shared.m_useDurability = false;
        }

        bool TorchVisible()
        {
            return torch != null && torch.m_equipped && body != null && LeftItem != null && RightItem != null &&
                (ReferenceEquals(LeftItem.GetValue(body), torch) || ReferenceEquals(RightItem.GetValue(body), torch));
        }

        void Disable(string reason)
        {
            eligibleSince = -1;
            if (torch != null && torch.m_equipped && view != null && view.IsValid() && view.IsOwner())
            {
                body.UnequipItem(torch, false);
                var readiness = GetComponent<HumanEquipmentReadiness>();
                if (readiness != null) readiness.RecoverFromUtility();
            }
            SetState("off: " + reason);
        }
        void SetState(string next)
        {
            if (state == next) return;
            state = next;
            string actor = body == null || String.IsNullOrEmpty(body.m_name) ? gameObject.name : body.m_name;
            Debug.Log("[LWC torch] " + actor + ": " + state + "; perceivedLight=" +
                (Single.IsInfinity(observedLight) ? "unknown" : observedLight.ToString("F3")) +
                "; renderedFog=" + observedFog.ToString("F3") + "; nativeNight=" + nativeNight +
                "; nativeDaylight=" + nativeDaylight + "; builtLightDistance=" +
                (Single.IsInfinity(nearestBuiltLight) ? "none" : nearestBuiltLight.ToString("F2")));
        }
        internal string Describe() { return state + "; perceivedLight=" +
            (Single.IsInfinity(observedLight) ? "unknown" : observedLight.ToString("F3")) +
            "; renderedFog=" + observedFog.ToString("F3") + "; nativeNight=" + nativeNight +
            "; nativeDaylight=" + nativeDaylight + "; builtLightDistance=" +
            (Single.IsInfinity(nearestBuiltLight) ? "none" : nearestBuiltLight.ToString("F2")); }
        internal bool ClaimsPeacefulHands()
        {
            if (companion == null) companion = GetComponent<HumanCompanion>();
            if (body == null) body = GetComponent<Humanoid>();
            if (companion == null || body == null || companion.CombatOwnsActor || !companion.AreaClearVerified ||
                Time.time - lastCombat < 5f) return false;
            long owner; Player leader;
            if (!companion.TryGetConfidenceParty(out owner, out leader) || leader == null || leader.IsCrouching()) return false;
            bool busy = body.InAttack() || body.IsStaggering() || body.IsSwimming() || body.IsCrouching();
            var cleric = GetComponent<HumanCleric>();
            busy |= cleric != null && cleric.Casting;
            var weapon = body.GetCurrentWeapon();
            busy |= weapon != null && weapon.m_shared.m_attack.m_bowDraw;
            var posture = GetComponent<HumanCompanionPosture>();
            if (busy || (posture != null && (posture.OwnsAmbientHands || posture.HasAmbientPose))) return false;
            // nearBuiltLight is the cached result of the bounded half-second
            // physics scan in Update.  Readiness may consult this method ten
            // times per second but must never start another world scan.
            return AmbientDark() && !nearBuiltLight;
        }
        internal void ReleaseForWeapons()
        {
            if (body == null) body = GetComponent<Humanoid>();
            if (view == null) view = GetComponent<ZNetView>();
            Disable("weapon priority");
        }
        void OnDisable() { Disable("component disabled"); }
    }
}
