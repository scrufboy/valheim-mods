using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Pose adapter only: never selects a destination or turns/moves an actor.
    // Native humanoid animation, crouch locomotion and noise handling are reused.
    public sealed class HumanCompanionPosture : MonoBehaviour
    {
        const BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo CurrentAttack = typeof(Humanoid).GetField("m_currentAttack", Native);
        static readonly FieldInfo LeftItem = typeof(Humanoid).GetField("m_leftItem", Native);
        static readonly FieldInfo RightItem = typeof(Humanoid).GetField("m_rightItem", Native);
        static readonly FieldInfo HiddenLeft = typeof(Humanoid).GetField("m_hiddenLeftItem", Native);
        static readonly FieldInfo HiddenRight = typeof(Humanoid).GetField("m_hiddenRightItem", Native);
        static readonly FieldInfo Attachments = typeof(AnimationEffect).GetField("m_attachments", Native);
        static readonly MethodInfo ShowHands = typeof(Humanoid).GetMethod("ShowHandItems", Native, null, new[] { typeof(bool), typeof(bool) }, null);
        static readonly object[] ShowArguments = { false, false };
        HumanCompanion companion;
        HumanDoorTraversal doors;
        Character body;
        Humanoid humanoid;
        Animator animator;
        AnimationEffect attachmentEvents;
        GameObject ownedCraftProp;
        ZNetView view;
        ZSyncAnimation sync;
        Player restingLeader;
        float settledAt, nextRefresh;
        bool hadAuthority, faulted, restRequested;
        bool ownsHands, restoreHands, clearCraftPending;
        object leasedLeft, leasedRight;
        int ambientCraft;
        CompanionPosture posture;
        public CompanionPosture Posture { get { return posture; } }
        internal bool OwnsAmbientHands { get { return ownsHands; } }
        internal bool HasAmbientPose { get { return ambientCraft != 0; } }

        internal static void InstallPatches(Harmony harmony)
        {
            foreach (string name in new[] { "IsCrouching", "IsSitting" })
            {
                var native = typeof(Character).GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (native == null || native.DeclaringType != typeof(Character)) throw new MissingMethodException("Native posture contract changed: " + name);
                harmony.Patch(native, postfix: new HarmonyMethod(typeof(HumanCompanionPosture).GetMethod(
                    name == "IsCrouching" ? "AfterCrouching" : "AfterSitting", BindingFlags.Static | BindingFlags.NonPublic)));
            }
            if (Attachments == null) throw new MissingFieldException("Native animation attachments changed.");
            foreach (string name in new[] { "Attach", "Effect" })
            {
                var method = typeof(AnimationEffect).GetMethod(name, Native, null, new[] { typeof(AnimationEvent) }, null);
                if (method == null) throw new MissingMethodException("Native crafting event changed: " + name);
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(HumanCompanionPosture).GetMethod("BeforeCraftEvent", BindingFlags.Static | BindingFlags.NonPublic)),
                    postfix: name == "Attach" ? new HarmonyMethod(typeof(HumanCompanionPosture).GetMethod("AfterCraftAttach", BindingFlags.Static | BindingFlags.NonPublic)) : null);
            }
        }
        void Cache()
        {
            if (companion == null) companion = GetComponent<HumanCompanion>();
            if (doors == null) doors = GetComponent<HumanDoorTraversal>();
            if (body == null) body = GetComponent<Character>();
            if (humanoid == null) humanoid = GetComponent<Humanoid>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (view == null) view = GetComponent<ZNetView>();
            if (sync == null) sync = GetComponent<ZSyncAnimation>();
        }
        bool Owner() { return view != null && view.IsValid() && view.IsOwner(); }
        internal void Refresh()
        {
            if (faulted || !isActiveAndEnabled) return;
            try
            {
                Cache();
                if (!Owner()) { posture = CompanionPosture.Upright; hadAuthority = false; restingLeader = null; restRequested = false; return; }
                if (clearCraftPending && sync != null) { sync.SetInt("crafting", 0); clearCraftPending = false; }
                RestoreAmbientHands();
                // Posture mimicry follows the persistent bound party identity,
                // not the peaceful-only gaze/follow view. Heightened awareness
                // must not erase the leader's stealth instruction; committed
                // combat still forces upright through the policy below.
                Player leader = companion == null ? null : companion.PartyLeader;
                bool safe = body != null && !body.IsDead() && body.IsOnGround() && !body.IsSwimming() &&
                    !body.IsFlying() && !body.InAttack() && !body.IsDrawingBow() && !body.IsBlocking();
                bool crouched = leader != null && leader.IsCrouching();
                bool sitting = false;
                if (leader != null)
                {
                    var leaderView = leader.GetComponent<ZNetView>();
                    sitting = leaderView != null && leaderView.IsValid() && leader.InEmote() &&
                        leaderView.GetZDO().GetString(ZDOVars.s_emote, "") == "sit";
                }
                bool arrived = leader != null && Vector3.Distance(transform.position, leader.transform.position) <= CompanionPresencePolicy.ResumeDistance &&
                    body.GetVelocity().sqrMagnitude < 0.0625f;
                bool passageClear = leader != null && doors != null && doors.MayRestNear(leader.gameObject);
                bool wantsRest = sitting && arrived && passageClear && safe;
                if (!wantsRest || !restRequested || leader != restingLeader)
                { settledAt = Time.time; restingLeader = leader; }
                restRequested = wantsRest;
                // Leader mimicry is peaceful-only. The single combat-owner
                // state keeps the fighter upright; animations do not vote.
                bool combat = companion != null && companion.CombatOwnsActor;
                var next = CompanionPosturePolicy.Choose(leader != null, combat,
                    safe, crouched, sitting, arrived, passageClear, Time.time - settledAt);
                if (leader == null || !safe || crouched || sitting || !passageClear) StopAmbient();
                Apply(next, !hadAuthority); hadAuthority = true;
            }
            catch (Exception error)
            {
                if (Owner()) Apply(CompanionPosture.Upright, true);
                faulted = true;
                var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
                if (plugin != null) plugin.Report("POSTURE FAIL: " + error.Message + "; native upright behavior retained");
            }
        }
        bool HandsSafe()
        {
            if (humanoid == null || !Owner() || humanoid.IsDead() || humanoid.InAttack() || humanoid.IsDrawingBow() || humanoid.IsBlocking() ||
                CurrentAttack == null || LeftItem == null || RightItem == null || HiddenLeft == null || HiddenRight == null || ShowHands == null) return false;
            var attack = CurrentAttack.GetValue(humanoid) as Attack;
            // InAttack alone may turn false before Attack.Update finishes cleanup.
            return attack == null || attack.IsDone();
        }
        internal bool BeginAmbientCraft(int kind)
        {
            Cache();
            if (ambientCraft == kind && kind != 0) return true;
            if ((kind != 1 && kind != 2) || faulted || !isActiveAndEnabled || posture != CompanionPosture.Upright ||
                sync == null || !HandsSafe() || ownsHands || companion == null || companion.PresenceLeader == null) return false;
            if (HiddenLeft.GetValue(humanoid) != null || HiddenRight.GetValue(humanoid) != null) return false;
            leasedLeft = LeftItem.GetValue(humanoid); leasedRight = RightItem.GetValue(humanoid);
            // Native hide preserves the actual existing items; never creates a
            // substitute weapon or interprets an attack as a crafting action.
            ownsHands = leasedLeft != null || leasedRight != null;
            if (ownsHands && !humanoid.HideHandItems(false, false))
            { ownsHands = false; leasedLeft = leasedRight = null; return false; }
            ambientCraft = kind; sync.SetInt("crafting", kind); return true;
        }
        internal void StopAmbient()
        {
            if (ambientCraft != 0) clearCraftPending = true;
            if (Owner() && sync != null && clearCraftPending) { sync.SetInt("crafting", 0); clearCraftPending = false; }
            ambientCraft = 0; restoreHands = ownsHands;
            ClearCraftProp();
            RestoreAmbientHands();
        }
        void ClearCraftProp()
        {
            if (ownedCraftProp == null) return;
            var list = attachmentEvents == null || Attachments == null ? null : Attachments.GetValue(attachmentEvents) as List<GameObject>;
            if (list != null) list.Remove(ownedCraftProp);
            // Only the inspected local cosmetic prop captured after our native
            // Forge Attach event. Never a network object or inventory item.
            UnityEngine.Object.Destroy(ownedCraftProp); ownedCraftProp = null; attachmentEvents = null;
        }
        static bool BeforeCraftEvent(AnimationEffect __instance, AnimationEvent __0)
        {
            var own = __instance.GetComponentInParent<HumanCompanionPosture>();
            if (own == null || __0.animatorClipInfo.clip == null || __0.animatorClipInfo.clip.name != "Forge") return true;
            own.Cache();
            if (!own.isActiveAndEnabled || own.animator == null || own.animator.GetInteger("crafting") != 2) return false;
            if (own.Owner() && own.ambientCraft != 2) return false;
            return __0.functionName == "Attach" ? AmbientAnimationContract.SafeEvent(__0, "Attach", "aniattach_forge_hammer") :
                AmbientAnimationContract.SafeEvent(__0, "Effect", "fx_forge_hammer");
        }
        static void AfterCraftAttach(AnimationEffect __instance, AnimationEvent __0)
        {
            if (__0.animatorClipInfo.clip == null || __0.animatorClipInfo.clip.name != "Forge" || !BeforeCraftEvent(__instance, __0)) return;
            var own = __instance.GetComponentInParent<HumanCompanionPosture>();
            if (own == null) return;
            var list = Attachments.GetValue(__instance) as List<GameObject>;
            if (list == null || list.Count > 16) return;
            foreach (var prop in list)
                if (prop != null && prop.name == "aniattach_forge_hammer(Clone)")
                { own.ownedCraftProp = prop; own.attachmentEvents = __instance; }
        }
        void RestoreAmbientHands()
        {
            if (!restoreHands || !HandsSafe()) return;
            // A newer equipment decision owns the hands if either active or
            // hidden references changed. Never take that decision back.
            bool same = LeftItem.GetValue(humanoid) == null && RightItem.GetValue(humanoid) == null &&
                ReferenceEquals(HiddenLeft.GetValue(humanoid), leasedLeft) && ReferenceEquals(HiddenRight.GetValue(humanoid), leasedRight);
            if (same) ShowHands.Invoke(humanoid, ShowArguments);
            ownsHands = restoreHands = false; leasedLeft = leasedRight = null;
        }
        void Apply(CompanionPosture next, bool force)
        {
            if (sync == null || !Owner() || (!force && next == posture)) return;
            if (force && ambientCraft == 0) sync.SetInt("crafting", 0);
            // These parameters are private to this explicitly composed NPC.
            // Stop a looping sit before asking native movement to resume.
            if (force || posture == CompanionPosture.Sit && next != CompanionPosture.Sit)
            { sync.SetBool("emote_sit", false); sync.SetTrigger("emote_stop"); }
            sync.SetBool("crouching", next == CompanionPosture.Crouch);
            if (next == CompanionPosture.Sit) sync.SetBool("emote_sit", true);
            if (next != posture)
            {
                var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
                if (plugin != null) plugin.Report("POSTURE " + posture + " -> " + next);
            }
            posture = next;
        }
        void Update()
        {
            if (Time.time < nextRefresh) return;
            nextRefresh = Time.time + 0.1f;
            Refresh();
        }
        void OnDisable() { StopAmbient(); if (Owner()) Apply(CompanionPosture.Upright, false); posture = CompanionPosture.Upright; }
        static void AfterCrouching(Character __instance, ref bool __result)
        {
            var own = __instance.GetComponent<HumanCompanionPosture>();
            if (own != null && own.isActiveAndEnabled && own.Owner()) __result = own.posture == CompanionPosture.Crouch;
        }
        static void AfterSitting(Character __instance, ref bool __result)
        {
            var own = __instance.GetComponent<HumanCompanionPosture>();
            if (own != null && own.isActiveAndEnabled && own.Owner()) __result = own.posture == CompanionPosture.Sit;
        }
    }
}

