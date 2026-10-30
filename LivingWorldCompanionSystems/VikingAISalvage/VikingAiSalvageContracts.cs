using System;

namespace LivingWorld.Companions.VikingAISalvage
{
    [Flags]
    public enum VikingAiFeature
    {
        None = 0,
        SensoryAdvisories = 1,
        SettlementAffordances = 2,
        EquipmentScoring = 4,
        UnloadedScheduleProjection = 8,
        PortalTransactions = 16
    }

    public enum RuntimeAuthority
    {
        LivingWorldMovementWriter,
        NativeMonsterAI,
        VikingAiAdvisory
    }

    public sealed class VikingAiAdapterOptions
    {
        public VikingAiFeature EnabledFeatures { get; private set; }

        public VikingAiAdapterOptions(VikingAiFeature enabledFeatures)
        {
            EnabledFeatures = enabledFeatures;
        }

        public bool IsEnabled(VikingAiFeature feature)
        {
            return feature != VikingAiFeature.None && (EnabledFeatures & feature) == feature;
        }

        public static VikingAiAdapterOptions SafeDefaults()
        {
            return new VikingAiAdapterOptions(VikingAiFeature.None);
        }
    }

    public static class OwnershipPolicy
    {
        public static RuntimeAuthority SelectAuthority(bool committedCombat)
        {
            return committedCombat
                ? RuntimeAuthority.NativeMonsterAI
                : RuntimeAuthority.LivingWorldMovementWriter;
        }

        public static bool MayVikingAiWriteMovement()
        {
            return false;
        }

        public static bool MayRunAdvisory(
            VikingAiAdapterOptions options,
            VikingAiFeature feature,
            bool committedCombat)
        {
            if (options == null) throw new ArgumentNullException("options");
            return !committedCombat && options.IsEnabled(feature);
        }
    }
}
