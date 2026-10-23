namespace LivingWorld.Core
{
    /// <summary>Leader crouches -> companions crouch, outside committed combat only.</summary>
    public sealed class StealthBehavior : NpcBehavior
    {
        public override string Name { get { return "stealth"; } }
        public override NpcDomain Claims { get { return NpcDomain.Posture; } }

        public override bool Wants(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            return s.HasLeader && s.LeaderCrouching && !s.Swimming &&
                (f.Phase == NpcPhase.Calm || f.Phase == NpcPhase.Readiness || f.Phase == NpcPhase.Regroup);
        }

        public override void Enter(NpcFrame f) { Body.SetCrouch(this, true); }

        public override void Tick(NpcFrame f)
        {
            Body.SetCrouch(this, true);
            Status = "crouched with leader";
        }

        public override void Exit(NpcFrame f, string reason)
        {
            Body.SetCrouch(this, false);
            Status = "released: " + reason;
        }
    }

    /// <summary>Leader sits -> settled companions sit. Uses native Character.IsSitting (0.3.54 lesson).</summary>
    public sealed class SitWithLeaderBehavior : NpcBehavior
    {
        const float SettleSeconds = 1f;
        float settledFor;

        public override string Name { get { return "sit"; } }
        public override NpcDomain Claims { get { return NpcDomain.Posture; } }

        public override bool Wants(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            bool eligible = f.Phase == NpcPhase.Calm && s.HasLeader && s.LeaderSitting && !s.LeaderMoving && !s.Swimming;
            if (!eligible) { settledFor = 0f; return false; }
            if (Active) return true;
            bool settled = s.OwnSpeed < .3f && f.Board.FormationDistance >= 0f && f.Board.FormationDistance <= 3.35f;
            settledFor = settled ? settledFor + f.Dt : 0f;
            return settledFor >= SettleSeconds;
        }

        public override void Enter(NpcFrame f) { Body.SetSit(this, true); }

        public override void Tick(NpcFrame f)
        {
            Body.SetSit(this, true);
            Status = "sitting with leader";
        }

        public override void Exit(NpcFrame f, string reason)
        {
            settledFor = 0f;
            Body.SetSit(this, false);
            Status = "released: " + reason;
        }
    }
}
