using System;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // The prefab begins as one native human, not as an already-running party
    // actor.  Companion systems are attached exactly once only after the
    // appearance adapter has proved the body, equipment, hair and beard.
    public sealed class HumanBehaviorInstaller : MonoBehaviour
    {
        float capacity, recovery, missingBonus, spendDelay;
        bool configured, attempted;

        public bool Installed { get; private set; }
        public string Status = "Waiting for a complete native human";

        internal void Configure(Player source)
        {
            if (source == null) throw new ArgumentNullException("source");
            capacity = source.m_baseStamina;
            recovery = source.m_staminaRegen;
            missingBonus = source.m_staminaRegenTimeMultiplier;
            spendDelay = source.m_staminaRegenDelay;
            configured = true;
        }

        void Update()
        {
            if (Installed || attempted) return;
            var appearance = GetComponent<HumanAppearance>();
            if (appearance == null || !appearance.Initialized) return;
            attempted = true;
            try { Install(); }
            catch (Exception error)
            {
                var ai = GetComponent<MonsterAI>();
                if (ai != null) ai.enabled = false;
                Status = "FAIL behavior installation: " + error.Message;
                Report(Status);
            }
        }

        void Install()
        {
            if (!configured) throw new InvalidOperationException("Native stamina configuration missing.");
            var ai = GetComponent<MonsterAI>();
            if (ai == null || ai.enabled) throw new InvalidOperationException("Dormant native AI gate is missing.");
            RequireAbsent<HumanCompanion>();
            RequireAbsent<HumanDoorTraversal>();
            RequireAbsent<HumanFollowPresence>();
            RequireAbsent<HumanCompanionPosture>();
            RequireAbsent<HumanResources>();
            RequireAbsent<HumanEnvironmentStatus>();
            RequireAbsent<HumanPartyConfidence>();
            RequireAbsent<HumanCompanionAmbient>();
            RequireAbsent<HumanEquipmentReadiness>();
            RequireAbsent<HumanCombatDefense>();
            RequireAbsent<HumanPartyEquipment>();
            RequireAbsent<HumanCleric>();
            RequireAbsent<HumanUtilityTorch>();

            gameObject.AddComponent<HumanCompanion>();
            gameObject.AddComponent<HumanDoorTraversal>();
            gameObject.AddComponent<HumanFollowPresence>();
            gameObject.AddComponent<HumanCompanionPosture>();
            gameObject.AddComponent<HumanResources>().Configure(capacity, recovery, missingBonus, spendDelay);
            gameObject.AddComponent<HumanEnvironmentStatus>();
            gameObject.AddComponent<HumanPartyConfidence>();
            gameObject.AddComponent<HumanCompanionAmbient>();
            gameObject.AddComponent<HumanEquipmentReadiness>();
            gameObject.AddComponent<HumanCombatDefense>();
            gameObject.AddComponent<HumanPartyEquipment>();
            gameObject.AddComponent<HumanCleric>();
            gameObject.AddComponent<HumanUtilityTorch>();

            Installed = true;
            Status = "Complete native human; companion behavior installed";
            ai.enabled = true; // Last: no AI or companion loop runs on an incomplete person.
            Report(Status);
        }

        void RequireAbsent<T>() where T : Component
        {
            if (GetComponent<T>() != null)
                throw new InvalidOperationException("Behavior component was attached before native-human approval: " + typeof(T).Name);
        }

        static void Report(string message)
        {
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report(message);
        }
    }
}
