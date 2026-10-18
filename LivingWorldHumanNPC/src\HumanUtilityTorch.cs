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
        float nextCheck, eligibleSince = -1, brightSince = -1, lastCombat = Single.NegativeInfinity;
        float observedLight = Single.PositiveInfinity, observedFog;
        bool ambientDark;
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
                var readiness = GetComponent<HumanEquipmentReadiness>();
                if (companion.CombatOwnsActor)
                { lastCombat = Time.time; Disable("combat owns actor"); return; }
                // A peaceful leader-readiness signal can reserve the hands, but
                // it is not combat and cannot mutate the combat-clear timer.
                if (readiness != null && readiness.WantsWeapons())
                { Disable("leader weapon readiness"); return; }
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
                var environment = EnvMan.instance == null ? null : EnvMan.instance.GetCurrentEnvironment();
                bool known = environment != null;
                bool dark = AmbientDark(known);
                observedFog = RenderSettings.fog ? RenderSettings.fogDensity : 0f;
                bool nearbyLight = NearbyLight();
                bool stealth = leader.IsCrouching();
                bool allowed = PartyCombatPolicy.Torch(dark, nearbyLight, stealth, companion.CombatOwnsActor, busy);
                if (!allowed)
                {
                    Disable(!dark ? "ambient light sufficient" : nearbyLight ? "nearby built light sufficient" :
                        stealth ? "leader stealth" : busy ? "hands/body busy" : "utility light denied");
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

        bool AmbientDark(bool environmentKnown)
        {
            if (!environmentKnown) return false;
            Color ambient = RenderSettings.ambientLight;
            float level = PartyCombatPolicy.PerceivedLight(ambient.r, ambient.g, ambient.b, RenderSettings.ambientIntensity);
            observedLight = level;
            // Hysteresis prevents dawn, storms and environment transitions from
            // snapping every torch off at a time-of-day boundary. Current rendered
            // ambient light—not the clock—decides, with five seconds of sustained
            // brightness required to extinguish an already-needed torch.
            if (!ambientDark)
            {
                if (level < .22f) ambientDark = true;
                return ambientDark;
            }
            if (level <= .32f) { brightSince = -1f; return true; }
            if (brightSince < 0f) brightSince = Time.time;
            if (Time.time - brightSince >= 5f) { ambientDark = false; brightSince = -1f; }
            return ambientDark;
        }

        bool NearbyLight()
        {
            // Only twice per second while opted in; no full-scene Light search.
            foreach (var collider in Physics.OverlapSphere(transform.position, 8f))
            {
                var piece = collider.GetComponentInParent<Piece>();
                if (piece == null) continue;
                foreach (var light in piece.GetComponentsInChildren<Light>())
                    if (light.enabled && light.gameObject.activeInHierarchy && light.intensity > .25f &&
                        Vector3.Distance(light.transform.position, body.GetCenterPoint()) <= Mathf.Min(8, light.range) &&
                        !Physics.Linecast(body.GetCenterPoint(), light.transform.position,
                            LayerMask.GetMask("Default", "static_solid", "piece", "terrain"), QueryTriggerInteraction.Ignore)) return true;
            }
            return false;
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
                "; renderedFog=" + observedFog.ToString("F3"));
        }
        internal string Describe() { return state + "; perceivedLight=" +
            (Single.IsInfinity(observedLight) ? "unknown" : observedLight.ToString("F3")) +
            "; renderedFog=" + observedFog.ToString("F3"); }
        internal void ReleaseForWeapons()
        {
            if (body == null) body = GetComponent<Humanoid>();
            if (view == null) view = GetComponent<ZNetView>();
            Disable("weapon priority");
        }
        void OnDisable() { Disable("component disabled"); }
    }
}

