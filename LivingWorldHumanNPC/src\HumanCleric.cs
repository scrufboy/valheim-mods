using System;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Local lab support, deliberately no custom network healing RPC or AOE.
    public sealed class HumanCleric : MonoBehaviour
    {
        internal const string RoleKey = "lwc_cleric_v1";
        const string ReserveKey = "lwc_cleric_reserve_v1";
        const string CooldownKey = "lwc_cleric_cooldown_v1";
        ZNetView view;
        Humanoid body;
        MonsterAI ai;
        HumanCompanion companion;
        HumanResourcePool reserve;
        Character patient;
        float castingUntil, cooldown, nextScan, saveAt;
        bool fault;
        static readonly MethodInfo Look = typeof(BaseAI).GetMethod("LookAt", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(Vector3) }, null);
        public bool Casting { get { return patient != null; } }
        public string Describe() { return "cleric=" + IsCleric(gameObject) + "; reserve=" + (reserve == null ? "pending" : reserve.Current.ToString("F1")) + "; cooldown=" + cooldown.ToString("F1") + "; casting=" + Casting + "; fault=" + fault; }

        internal static bool IsCleric(GameObject actor)
        {
            var v = actor == null ? null : actor.GetComponent<ZNetView>();
            return v != null && v.IsValid() && v.GetZDO().GetString(RoleKey, "") == "1";
        }

        void Update()
        {
            try
            {
                if (view == null) view = GetComponent<ZNetView>();
                if (body == null) body = GetComponent<Humanoid>();
                if (ai == null) ai = GetComponent<MonsterAI>();
                if (companion == null) companion = GetComponent<HumanCompanion>();
                if (!IsCleric(gameObject) || fault) return;
                if (!view.IsOwner()) { patient = null; reserve = null; return; }
                if (body.IsDead() || !GetComponent<HumanAppearance>().Initialized) { patient = null; return; }
                if (reserve == null)
                {
                    string saved = view.GetZDO().GetString(ReserveKey, "");
                    if (saved == "") reserve = new HumanResourcePool(100);
                    else if (!HumanResourcePool.TryRestore(saved, 100, out reserve)) throw new InvalidOperationException("Invalid cleric reserve preserved");
                    cooldown = Mathf.Clamp(view.GetZDO().GetFloat(CooldownKey, 0), 0, 12);
                }
                float dt = Mathf.Clamp(Time.deltaTime, 0, .1f);
                cooldown = Mathf.Max(0, cooldown - dt);
                reserve.Advance(dt, 2, 0, 1, !Casting);
                if (Casting)
                {
                    if (companion.HasAuthorizedCombat || body.IsStaggering() || !Eligible(patient))
                    { Cancel("cast interrupted by personal combat; no heal delivered"); return; }
                    ai.StopMoving();
                    if (Look == null) throw new MissingMethodException("BaseAI.LookAt");
                    Look.Invoke(ai, new object[] { patient.GetCenterPoint() });
                    if (Time.time >= castingUntil)
                    {
                        var recipient = patient; patient = null;
                        ClericPresentation.End(gameObject);
                        float before = recipient.GetHealth();
                        recipient.Heal(Mathf.Min(35f, recipient.GetMaxHealth() - before), true);
                        Log("heal recipient=" + recipient.GetZDOID() + "; health=" + before + " -> " + recipient.GetHealth());
                        if (recipient.GetHealth() > before)
                            try { ClericPresentation.Delivered(recipient.transform.position); }
                            catch (Exception visual) { Log("Heal delivered; cosmetic unavailable: " + visual.Message); }
                        Save();
                    }
                }
                else if (!companion.HasAuthorizedCombat && Time.time >= nextScan && cooldown <= 0 && reserve.Have(25) &&
                    !body.InAttack() && !body.IsStaggering())
                {
                    nextScan = Time.time + .5f;
                    Character best = null; float severity = 1;
                    foreach (var candidate in Character.GetAllCharacters())
                    {
                        if (!Eligible(candidate)) continue;
                        float fraction = candidate.GetHealth() / candidate.GetMaxHealth();
                        if (fraction < severity) { best = candidate; severity = fraction; }
                    }
                    if (best != null)
                    {
                        var ambient = GetComponent<HumanCompanionAmbient>();
                        if (ambient != null) ambient.Cancel("cleric support");
                        patient = best;
                        var posture = GetComponent<HumanCompanionPosture>();
                        if (posture != null) posture.Refresh();
                        ClericPresentation.Begin(gameObject);
                        castingUntil = Time.time + 1.5f; cooldown = 8f;
                        reserve.Use(25, 1, 3); Save();
                        Log("cast begun; recipient=" + best.GetZDOID() + "; reserve=" + reserve.Current);
                    }
                }
                if (Time.time >= saveAt) { saveAt = Time.time + 1; Save(); }
            }
            catch (Exception error) { fault = true; patient = null; Log("cleric fail-closed: " + error.Message); }
        }

        bool Eligible(Character target)
        {
            if (target == null) return false;
            long party; Player leader;
            if (!companion.TryGetConfidenceParty(out party, out leader)) return false;
            bool ally = target == body || target == leader;
            var other = target.GetComponent<HumanCompanion>();
            long otherParty; Player otherLeader;
            if (other != null && other.TryGetConfidenceParty(out otherParty, out otherLeader)) ally |= party == otherParty;
            var targetView = target.GetComponent<ZNetView>();
            bool visible = target == body || !Physics.Linecast(body.GetCenterPoint(), target.GetCenterPoint(),
                LayerMask.GetMask("Default", "static_solid", "piece", "terrain"), QueryTriggerInteraction.Ignore);
            return PartyCombatPolicy.HealEligible(ally, !target.IsDead(), targetView != null && targetView.IsValid() && targetView.IsOwner(),
                visible, Vector3.Distance(transform.position, target.transform.position), target.GetHealth(), target.GetMaxHealth());
        }
        void Cancel(string why) { patient = null; ClericPresentation.End(gameObject); Save(); Log(why); }
        void Save()
        {
            if (reserve == null || view == null || !view.IsValid() || !view.IsOwner()) return;
            view.GetZDO().Set(ReserveKey, reserve.Save()); view.GetZDO().Set(CooldownKey, cooldown);
        }
        void OnDisable() { if (patient != null) ClericPresentation.End(gameObject); patient = null; Save(); }
        void Log(string message) { Debug.Log("[LWC cleric] " + message); }
    }
}

