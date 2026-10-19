using System;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Narrow humanoid defense adapter. MonsterAI has no shield-raise decision,
    // so this component supplies only that missing human action. It never picks
    // a combat target, moves, turns, switches weapons, attacks or queues a riposte.
    public sealed class HumanCombatDefense : MonoBehaviour
    {
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo Blocking = RequiredField(typeof(Character), "m_blocking");
        static readonly MethodInfo CurrentBlocker = RequiredMethod(typeof(Humanoid), "GetCurrentBlocker");

        ZNetView view;
        Humanoid body;
        MonsterAI ai;
        HumanCompanion companion;
        Character observedAttack;
        bool sawAttack;
        bool raised;
        float cueAt;
        float releaseAt;

        public string Describe()
        { return raised ? "shield raised for observed close strike" : "shield defense ready"; }

        static FieldInfo RequiredField(Type type, string name)
        {
            var field = type.GetField(name, Hidden);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }

        static MethodInfo RequiredMethod(Type type, string name)
        {
            var method = type.GetMethod(name, Hidden, null, Type.EmptyTypes, null);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            return method;
        }

        void Cache()
        {
            if (view == null) view = GetComponent<ZNetView>();
            if (body == null) body = GetComponent<Humanoid>();
            if (ai == null) ai = GetComponent<MonsterAI>();
            if (companion == null) companion = GetComponent<HumanCompanion>();
        }

        bool Owner()
        {
            Cache();
            return view != null && view.IsValid() && view.IsOwner() && body != null && ai != null &&
                companion != null && !body.IsDead();
        }

        bool ShieldReady()
        {
            var blocker = CurrentBlocker.Invoke(body, null) as ItemDrop.ItemData;
            return blocker != null && blocker.m_equipped && blocker.m_shared != null &&
                blocker.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
        }

        Character IncomingAttack()
        {
            Character nearest = null;
            float nearestDistance = Single.PositiveInfinity;
            foreach (var candidate in Character.GetAllCharacters())
            {
                if (candidate == null || candidate == body || candidate.IsDead() || !candidate.InAttack()) continue;
                var hostile = candidate.GetComponent<MonsterAI>();
                if (hostile == null || hostile.GetTargetCreature() != body) continue;
                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance > 5f || distance >= nearestDistance || !ai.CanSeeTarget(candidate)) continue;
                nearest = candidate; nearestDistance = distance;
            }
            return nearest;
        }

        void SetRaised(bool value)
        {
            if (body == null || raised == value) return;
            Blocking.SetValue(body, value);
            raised = value;
        }

        void Release()
        {
            SetRaised(false);
            observedAttack = null;
            sawAttack = false;
            cueAt = 0f;
            releaseAt = 0f;
        }

        void Update()
        {
            var cleric = GetComponent<HumanCleric>();
            if (!Owner() || !companion.CombatOwnsActor || cleric != null && cleric.Casting)
            { Release(); return; }

            Character threat = IncomingAttack();
            bool attacking = threat != null;
            if (threat != observedAttack)
            {
                SetRaised(false);
                observedAttack = threat;
                sawAttack = false;
            }
            if (attacking && !sawAttack) cueAt = Time.time;

            if (attacking && !raised)
            {
                Vector3 direction = threat.transform.position - transform.position;
                direction.y = 0f;
                Vector3 forward = transform.forward;
                forward.y = 0f;
                bool facing = direction.sqrMagnitude > .01f && forward.sqrMagnitude > .01f &&
                    Vector3.Dot(forward.normalized, direction.normalized) >= .35f;
                bool incapacitated = body.InAttack() || body.IsDrawingBow() || body.IsStaggering();
                if (PartyCombatPolicy.ReactToAttack(true, facing, ShieldReady(), body.HaveStamina(10f),
                    incapacitated, direction.magnitude, Time.time - cueAt, .06f))
                {
                    SetRaised(true);
                    releaseAt = Time.time + .8f;
                }
            }

            if (raised && (!attacking || Time.time >= releaseAt)) SetRaised(false);
            sawAttack = attacking;
        }

        void OnDisable() { Release(); }
        void OnDestroy() { Release(); }
    }
}
