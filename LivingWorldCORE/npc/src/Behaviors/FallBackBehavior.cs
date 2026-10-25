using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Owns the actor while FallBackPolicy says to fall back: no target, run away from the nearest
    /// engaged enemy and toward the leader. The policy alone decides when it ends; then native
    /// combat resumes and the game's own weapon choice gives the bow at range.
    /// </summary>
    public sealed class FallBackBehavior : NpcBehavior
    {
        const float StepDistance = 6f;
        GameObject anchor;

        public override string Name { get { return "fall-back"; } }
        public override NpcDomain Claims { get { return NpcDomain.All; } }
        public override bool Wants(NpcFrame f) { return f.FallingBack; }

        public override void Enter(NpcFrame f)
        {
            if (anchor == null)
            {
                anchor = new GameObject("LWC fall-back anchor");
                anchor.hideFlags = HideFlags.HideAndDontSave;
            }
            Body.ReleaseEncounter(this);
        }

        public override void Tick(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            if (s.Target != null || s.HasStaticTarget) Body.ReleaseEncounter(this);
            Vector3 me = Body.Root.transform.position;
            Vector3 away = me - f.FallBackFrom; away.y = 0f;
            away = away.sqrMagnitude < .01f ? -Body.Root.transform.forward : away.normalized;
            Vector3 toward = Vector3.zero;
            if (s.Leader != null)
            {
                toward = s.Leader.transform.position - me; toward.y = 0f;
                // Never run toward the leader *through* the enemy: only blend when the leader is not behind it.
                toward = toward.sqrMagnitude < .01f || Vector3.Dot(toward.normalized, away) < -.2f ? Vector3.zero : toward.normalized;
            }
            Vector3 direction = (away + .7f * toward).normalized;
            anchor.transform.position = me + direction * StepDistance;
            Body.Follow(this, anchor);
            Body.SetPace(this, true, true, s.LeaderSpeed, false);
            Body.SetRun(this, true);
            Body.PublishObserved(this);
            Status = "falling back from enemy at " + Vector3.Distance(me, f.FallBackFrom).ToString("0.0") + "m";
        }

        public override void Exit(NpcFrame f, string reason)
        {
            try
            {
                if (anchor != null && Body.Ai != null && Body.Ai.GetFollowTarget() == anchor) Body.Follow(this, null);
                Body.RestorePace(this);
            }
            finally
            {
                if (anchor != null) Object.Destroy(anchor);
                anchor = null;
            }
            Status = "released: " + reason;
        }
    }
}
