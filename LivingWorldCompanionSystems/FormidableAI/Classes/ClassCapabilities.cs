using System;

namespace LivingWorld.Companions.FormidableAI.Classes
{
    [Flags]
    public enum ClassCapability
    {
        None = 0,
        ShieldDefense = 1,
        ProtectAlly = 2,
        RangedPressure = 4,
        FiringLane = 8,
        HealSupport = 16,
        CastSupport = 32,
        ReachControl = 64,
        BoundedThrow = 128,
        ThreatAnchor = 256,
        CommandSupport = 512,
        Investigation = 1024,
        ThreatCommunication = 2048
    }

    public enum FormidableClass
    {
        Warden,
        Huntress,
        Mystic,
        Skirmisher,
        Commander,
        Pathfinder
    }

    public sealed class ClassProfile
    {
        public FormidableClass Class { get; private set; }
        public ClassCapability Capabilities { get; private set; }

        public ClassProfile(FormidableClass role, ClassCapability capabilities)
        {
            Class = role;
            Capabilities = capabilities;
        }

        public bool Has(ClassCapability capability)
        {
            return (Capabilities & capability) == capability;
        }

        public static ClassProfile Create(FormidableClass role)
        {
            switch (role)
            {
                case FormidableClass.Warden:
                    return new ClassProfile(role, ClassCapability.ShieldDefense | ClassCapability.ProtectAlly);
                case FormidableClass.Huntress:
                    return new ClassProfile(role, ClassCapability.RangedPressure | ClassCapability.FiringLane);
                case FormidableClass.Mystic:
                    return new ClassProfile(role, ClassCapability.HealSupport | ClassCapability.CastSupport);
                case FormidableClass.Skirmisher:
                    return new ClassProfile(role, ClassCapability.ReachControl | ClassCapability.BoundedThrow);
                case FormidableClass.Commander:
                    return new ClassProfile(role, ClassCapability.ThreatAnchor | ClassCapability.CommandSupport | ClassCapability.ProtectAlly);
                case FormidableClass.Pathfinder:
                    return new ClassProfile(role, ClassCapability.Investigation | ClassCapability.ThreatCommunication | ClassCapability.RangedPressure);
                default:
                    throw new ArgumentOutOfRangeException("role");
            }
        }
    }
}
