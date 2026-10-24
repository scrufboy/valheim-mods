namespace LivingWorld.Core
{
    /// <summary>
    /// Sole owner of hands outside combat. One decision per tick:
    /// readiness draws, otherwise stow on the back, with a torch in hand when the
    /// torch policy says so. Combat takes this domain away; the exit transaction
    /// puts the torch away before native combat ever runs.
    /// </summary>
    public sealed class HandsBehavior : NpcBehavior
    {
        readonly TorchPolicy torch = new TorchPolicy();
        public TorchPolicy Torch { get { return torch; } }

        public override string Name { get { return "hands"; } }
        public override NpcDomain Claims { get { return NpcDomain.Hands; } }

        public override bool Wants(NpcFrame f)
        {
            return f.Phase == NpcPhase.Calm || f.Phase == NpcPhase.Readiness ||
                f.Phase == NpcPhase.Regroup || f.Phase == NpcPhase.AwaitingLeader;
        }

        float nextQuiverCheck;

        public override void Tick(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            bool wantTorch = torch.Step(f.Phase, s.Dark, s.NearBuiltLight, s.LeaderCrouching, s.Swimming,
                f.SecondsSinceEncounter, f.Dt);
            f.Board.TorchWanted = wantTorch;
            if (f.Now >= nextQuiverCheck) { nextQuiverCheck = f.Now + 5f; Body.TopUpQuiver(this); }

            if (f.Phase == NpcPhase.Readiness)
            {
                Body.Draw(this);
                f.Board.TorchLit = false;
                Status = "drawn " + NpcBody.ItemName(Body.LeftItem) + "/" + NpcBody.ItemName(Body.RightItem);
                return;
            }
            if (wantTorch)
            {
                bool lit = Body.LightTorch(this);
                f.Board.TorchLit = lit;
                if (lit)
                {
                    Status = "torch; back=" + NpcBody.ItemName(Body.BackLeft) + "/" + NpcBody.ItemName(Body.BackRight);
                    return;
                }
            }
            Body.Stow(this);
            f.Board.TorchLit = false;
            Status = "stowed back=" + NpcBody.ItemName(Body.BackLeft) + "/" + NpcBody.ItemName(Body.BackRight) +
                "; torch " + torch.Reason;
        }

        public override void Exit(NpcFrame f, string reason)
        {
            // Torch must never survive into combat or any other owner's tenure.
            Body.DouseTorch(this);
            f.Board.TorchLit = false;
            Status = "released: " + reason;
        }
    }
}
