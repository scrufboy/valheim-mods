using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Owns the actor while FallBackPolicy says to fall back: no target, run away from the nearest
    /// engaged enemy and toward the leader. The policy alone decides when it ends; then native
    /// combat resumes and the game's own weapon choice gives the bow at range.
    /// A2.4 (3e5ac3c): with FallbackLanes on, a viable lane is chosen BEFORE moving (terrain, water, obstacles, never past an
    /// enemy; toward the partner or leader as cover) and re-checked every half second; no lane ends the fall-back in a stand.
    /// A2.3: also runs a defense side-step (Evading) - the same mover, so no new movement writer exists.
    /// </summary>
    public sealed class FallBackBehavior : NpcBehavior
    {
        const float StepDistance = 6f, LaneProbe = 4f, LaneReplan = .5f, LaneKeep = 1.5f, ThreatRadius = 15f;
        GameObject anchor;
        int lane = -1;
        float nextLane, laneScore;
        readonly bool[] blocked = new bool[LanePlanner.Directions];
        readonly P2[] threats = new P2[8];
        readonly P2[] threatVel = new P2[8];

        public override string Name { get { return "fall-back"; } }
        public override NpcDomain Claims { get { return NpcDomain.All; } }
        public override bool Wants(NpcFrame f) { return f.FallingBack || f.Evading; }

        public override void Enter(NpcFrame f)
        {
            if (anchor == null)
            {
                anchor = new GameObject("LWC fall-back anchor");
                anchor.hideFlags = HideFlags.HideAndDontSave;
            }
            lane = -1;
            nextLane = 0f;
            standing = false;
            // Native MonsterAI walks to its target, ignoring the follow anchor: the target must be released to move at all.
            Body.ReleaseEncounter(this);
        }

        bool standing;

        public override void Tick(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            if (s.Target != null || s.HasStaticTarget) Body.ReleaseEncounter(this);
            Vector3 me = Body.Root.transform.position;
            if (f.Evading && !f.FallingBack)
            {
                // Lead the anchor 6 m along the side-step each tick: native Follow stops inside ~3 m and walks under 10 m.
                anchor.transform.position = me + f.EvadeDir * StepDistance;
                Body.Follow(this, anchor);
                Body.SetPace(this, true, true, s.LeaderSpeed, false);
                Body.SetRun(this, true);
                Body.PublishObserved(this);
                Status = "evading strike";
                return;
            }
            Vector3 direction;
            if (standing) { Body.Follow(this, null); Body.PublishObserved(this); Status = "no lane: standing"; return; }
            // A hazard escape keeps the 1.0.32.15 straight-away flight from the cloud (lanes judge enemies, not clouds).
            if (CorePlugin.FormidableLanes && !f.FallBackHazard && PlanLane(f, me, out direction)) { }
            else if (standing) { Body.Follow(this, null); Body.PublishObserved(this); Status = "no lane: standing"; return; }
            else
            {
                Vector3 away = me - f.FallBackFrom; away.y = 0f;
                away = away.sqrMagnitude < .01f ? -Body.Root.transform.forward : away.normalized;
                Vector3 toward = Vector3.zero;
                if (s.Leader != null)
                {
                    toward = s.Leader.transform.position - me; toward.y = 0f;
                    // Never run toward the leader *through* the enemy: only blend when the leader is not behind it.
                    toward = toward.sqrMagnitude < .01f || Vector3.Dot(toward.normalized, away) < -.2f ? Vector3.zero : toward.normalized;
                }
                direction = (away + .7f * toward).normalized;
            }
            anchor.transform.position = me + direction * StepDistance;
            Body.Follow(this, anchor);
            Body.SetPace(this, true, true, s.LeaderSpeed, false);
            Body.SetRun(this, true);
            Body.PublishObserved(this);
            Status = "falling back from enemy at " + Vector3.Distance(me, f.FallBackFrom).ToString("0.0") + "m" + (lane >= 0 ? " lane " + lane : "");
        }

        /// <summary>A2.4 lane choice. False = legacy direction (lanes off or not planned yet). Ends the fall-back when no lane exists.</summary>
        bool PlanLane(NpcFrame f, Vector3 me, out Vector3 direction)
        {
            direction = Vector3.zero;
            NpcKernel kernel = Body.Root.GetComponent<NpcKernel>();
            if (kernel == null) return false;
            if (f.Now >= nextLane || lane < 0)
            {
                nextLane = f.Now + LaneReplan;
                Humanoid self = Body.Human;
                int n = 0;
                foreach (Character c in Character.GetAllCharacters())
                {
                    if (n >= threats.Length) break;
                    if (c == null || c == self || c.IsDead() || !BaseAI.IsEnemy(self, c)) continue;
                    Vector3 p = c.transform.position;
                    if (Vector3.Distance(p, me) > ThreatRadius) continue;
                    Vector3 v = c.GetVelocity();
                    threats[n] = new P2(p.x, p.z); threatVel[n] = new P2(v.x, v.z); n++;
                }
                for (int i = 0; i < blocked.Length; i++) blocked[i] = !CombatLanes.Clear(me, LanePlanner.Direction(i), LaneProbe);
                Vector3 cover;
                bool hasCover = CoverPoint(kernel, f.Senses, me, out cover);
                float score; string why;
                int pick = LanePlanner.Choose(new P2(me.x, me.z), threats, threatVel, n, blocked, hasCover, new P2(cover.x, cover.z),
                    kernel.Doctrine.CoverWeight, out score, out why);
                if (pick < 0)
                {
                    kernel.FallBack.Lane = "none";
                    kernel.FallBack.Abort(f.Now, "no lane: stand and fight");
                    standing = true;
                    return false;
                }
                // Keep the current lane unless it is now excluded (blocked or passing an enemy) or clearly worse (no zig-zag).
                float current = lane < 0 ? float.NaN : LanePlanner.Score(lane, new P2(me.x, me.z), threats, threatVel, n, blocked, hasCover,
                    new P2(cover.x, cover.z), kernel.Doctrine.CoverWeight);
                if (float.IsNaN(current) || score > current + LaneKeep) { lane = pick; laneScore = score; }
                else laneScore = current;
                kernel.FallBack.Lane = lane + " " + why;
            }
            P2 d = LanePlanner.Direction(lane);
            direction = new Vector3(d.X, 0f, d.Z);
            return true;
        }

        /// <summary>The partner companion (else the leader) as the cover point for the lane.</summary>
        bool CoverPoint(NpcKernel self, NpcSenses s, Vector3 me, out Vector3 point)
        {
            point = Vector3.zero;
            NpcLeaderBinding mine = self.GetComponent<NpcLeaderBinding>();
            float best = float.PositiveInfinity;
            bool found = false;
            if (mine != null && mine.LeaderId != 0L)
                foreach (NpcKernel k in NpcKernel.LiveSet)
                {
                    if (k == null || k == self || k.Body == null || k.Body.Human == null || k.Body.Human.IsDead()) continue;
                    NpcLeaderBinding b = k.GetComponent<NpcLeaderBinding>();
                    if (b == null || b.LeaderId != mine.LeaderId) continue;
                    float d = Vector3.Distance(k.transform.position, me);
                    if (d < best) { best = d; point = k.transform.position; found = true; }
                }
            if (!found && s.Leader != null) { point = s.Leader.transform.position; found = true; }
            return found;
        }

        public override void Exit(NpcFrame f, string reason)
        {
            try
            {
                if (anchor != null && Body.Ai != null && Body.Ai.GetFollowTarget() == anchor) Body.Follow(this, null);
                Body.RestorePace(this);
                // The fall-back ends in a decision: fight now, not after the game's next re-pick (combat re-engages at once).
                Body.DropTarget(this);
            }
            finally
            {
                if (anchor != null) Object.Destroy(anchor);
                anchor = null;
                lane = -1;
                standing = false;
                NpcKernel k = Body.Root.GetComponent<NpcKernel>();
                if (k != null) k.FallBack.Lane = "";
            }
            Status = "released: " + reason;
        }
    }
}
