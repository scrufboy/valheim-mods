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
        bool ambientDark;
        bool fault;
        static readonly MethodInfo Copy = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

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
                var posture = GetComponent<HumanCompanionPosture>();
                if (posture != null && (posture.OwnsAmbientHands || posture.HasAmbientPose)) { eligibleSince = -1; return; }
                EnsureTorch();
                long owner; Player leader;
                if (!companion.TryGetConfidenceParty(out owner, out leader) || leader == null) { Extinguish(); return; }
                var readiness = GetComponent<HumanEquipmentReadiness>();
                if (companion.CombatOwnsActor)
                { lastCombat = Time.time; ReleaseForWeapons(); return; }
                // A peaceful leader-readiness signal can reserve the hands, but
                // it is not combat and cannot mutate the combat-clear timer.
                if (readiness != null && readiness.WantsWeapons())
                { ReleaseForWeapons(); return; }
                if (!companion.AreaClearVerified) { ReleaseForWeapons(); return; }
                // Torches are companion ambience, never combat equipment. A
                // full five quiet seconds must follow the last combat signal so
                // a transient target/alert gap cannot put one back in a hand.
                if (Time.time - lastCombat < 5f) { Extinguish(); return; }
                bool busy = body.InAttack() || body.IsStaggering() || body.IsSwimming() || body.IsCrouching();
                var cleric = GetComponent<HumanCleric>();
                busy |= cleric != null && cleric.Casting;
                // A bow or raised shield always wins. Peaceful sword/shield can stow
                // the shield temporarily; the equipment adapter restores it in combat.
                var weapon = body.GetCurrentWeapon();
                busy |= weapon != null && weapon.m_shared.m_attack.m_bowDraw;
                var environment = EnvMan.instance == null ? null : EnvMan.instance.GetCurrentEnvironment();
                bool known = environment != null;
                float fog = known ? Mathf.Max(environment.m_fogDensityNight, environment.m_fogDensityMorning,
                    environment.m_fogDensityDay, environment.m_fogDensityEvening) : 1f;
                bool dark = AmbientDark(known);
                bool allowed = PartyCombatPolicy.Torch(dark, NearbyLight(), leader.IsCrouching(), companion.CombatOwnsActor,
                    busy, known, fog, Mister.InsideMister(transform.position, 1f));
                if (!allowed) { Extinguish(); return; }
                if (eligibleSince < 0) eligibleSince = Time.time;
                if (Time.time - eligibleSince < 3f || (torch != null && torch.m_equipped)) return;
                EnsureTorch();
                if (body.EquipItem(torch, false)) Debug.Log("[LWC torch] utility light on: sustained darkness, clear local mist check");
            }
            catch (Exception error) { fault = true; Extinguish(); Debug.Log("[LWC torch] disabled: " + error.Message); }
        }

        bool AmbientDark(bool environmentKnown)
        {
            if (!environmentKnown) return false;
            Color ambient = RenderSettings.ambientLight;
            float level = Mathf.Max(ambient.r, Mathf.Max(ambient.g, ambient.b)) * RenderSettings.ambientIntensity;
            // Hysteresis prevents dawn, storms and environment transitions from
            // snapping every torch off at a time-of-day boundary. Current rendered
            // ambient light—not the clock—decides, with five seconds of sustained
            // brightness required to extinguish an already-needed torch.
            if (!ambientDark)
            {
                if (level < .30f) ambientDark = true;
                return ambientDark;
            }
            if (level <= .42f) { brightSince = -1f; return true; }
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

        void Extinguish()
        {
            eligibleSince = -1;
            if (torch == null || !torch.m_equipped || view == null || !view.IsValid() || !view.IsOwner()) return;
            body.UnequipItem(torch, false);
            var readiness = GetComponent<HumanEquipmentReadiness>();
            if (readiness != null) readiness.RecoverFromUtility();
            Debug.Log("[LWC torch] stowed: visibility, stealth, combat or equipment priority");
        }
        internal void ReleaseForWeapons()
        {
            if (body == null) body = GetComponent<Humanoid>();
            if (view == null) view = GetComponent<ZNetView>();
            Extinguish();
        }
        void OnDisable() { Extinguish(); }
    }
}

