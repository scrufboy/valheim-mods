using System;
using System.Globalization;

namespace LivingWorld.HumanNpc
{
    public enum PartySupportObservation { Unknown, Perceived, Absent }
    public enum PartyCohesion { Unknown, Together, Separated }
    public enum ConfidenceLeaderState { Unknown, LoadedAlive, LoadedDead, Unloaded }

    // Independent, bounded resource policy. No actor, village, movement, damage,
    // equipment or native Rested dependency. Courage is a saved personal trait.
    public sealed class ConfidencePolicy
    {
        public const double Capacity = 100;
        public const double InitialConfidence = 60;
        public const double ObservationInterval = .5;
        public const double SeparationGrace = 3;
        public const double SeparationLossPerSecond = .35;
        public const double PressureLossPerSecond = 1.05;
        public const double ReunionGainPerSecond = .8;
        public const float SupportEnterRange = 12f;
        public const float SupportRemainRange = 16f;
        public const float SenseRange = 24f;
        public const int ColliderLimit = 64;
        public const int ActorLimit = 16;

        public double Current { get; private set; }
        public double Courage { get; private set; }
        public PartyCohesion Cohesion { get; private set; }
        public double NoSupportSeconds { get; private set; }

        public ConfidencePolicy(double confidence, double courage)
        {
            if (!Finite(confidence) || confidence < 0 || confidence > Capacity)
                throw new ArgumentOutOfRangeException("confidence");
            if (!Finite(courage) || courage < 0 || courage > 1)
                throw new ArgumentOutOfRangeException("courage");
            Current = confidence; Courage = courage; Cohesion = PartyCohesion.Unknown;
        }

        public static bool Finite(double value)
        { return !Double.IsNaN(value) && !Double.IsInfinity(value); }

        public bool Advance(double seconds, PartySupportObservation support, double overwhelmingPressure)
        {
            // No long-frame or unloaded catch-up; no malformed input mutation.
            if (!Finite(seconds) || seconds <= 0 || seconds > ObservationInterval ||
                !Finite(overwhelmingPressure) || overwhelmingPressure < 0 || overwhelmingPressure > 1 ||
                (support != PartySupportObservation.Unknown && support != PartySupportObservation.Perceived && support != PartySupportObservation.Absent))
                return false;
            double previous = Current;
            if (support == PartySupportObservation.Unknown)
            {
                // A failed/saturated sample is not evidence of separation.
                Cohesion = PartyCohesion.Unknown; NoSupportSeconds = 0;
                return false;
            }
            if (support == PartySupportObservation.Perceived)
            {
                Cohesion = PartyCohesion.Together; NoSupportSeconds = 0;
                Current = Math.Min(Capacity, Current + seconds * ReunionGainPerSecond * (.75 + .5 * Courage));
            }
            else
            {
                double erosionTime = Math.Max(0, NoSupportSeconds + seconds - SeparationGrace);
                NoSupportSeconds = Math.Min(SeparationGrace, NoSupportSeconds + seconds);
                if (NoSupportSeconds >= SeparationGrace) Cohesion = PartyCohesion.Separated;
                // THE PRESSURE RULE: pressure adds erosion ONLY in this absent,
                // confirmed-separated branch. Together/unknown never use it.
                if (Cohesion == PartyCohesion.Separated)
                    Current = Math.Max(0, Current - erosionTime * (1 - .6 * Courage) *
                        (SeparationLossPerSecond + overwhelmingPressure * PressureLossPerSecond));
            }
            return Current != previous;
        }

        public static double ObservedPressure(int threats, int supportingActors)
        {
            if (threats < 3 || threats <= supportingActors + 1) return 0;
            return Math.Min(1, (threats - supportingActors - 1) / 4.0);
        }

        public string Save()
        {
            // Transient sensing and elapsed grace are deliberately not saved.
            return "1|" + Current.ToString("R", CultureInfo.InvariantCulture) + "|" + Courage.ToString("R", CultureInfo.InvariantCulture);
        }

        public static bool TryRestore(string record, out ConfidencePolicy state)
        {
            state = null;
            if (String.IsNullOrEmpty(record) || record.Length > 128) return false;
            string[] parts = record.Split('|'); double current, courage;
            if (parts.Length != 3 || parts[0] != "1" ||
                !Double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out current) ||
                !Double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out courage) ||
                !Finite(current) || current < 0 || current > Capacity || !Finite(courage) || courage < 0 || courage > 1) return false;
            state = new ConfidencePolicy(current, courage); return true;
        }
    }

    public struct ConfidenceSnapshot
    {
        public readonly double Confidence01, Courage01;
        public readonly PartyCohesion Cohesion;
        public readonly ConfidenceLeaderState Leader;
        public readonly int PerceivedSupport, PerceivedThreats;
        public readonly double OverwhelmingPressure01;
        public ConfidenceSnapshot(ConfidencePolicy state, ConfidenceLeaderState leader, int support, int threats, double pressure)
        {
            Confidence01 = state.Current / ConfidencePolicy.Capacity; Courage01 = state.Courage;
            Cohesion = state.Cohesion; Leader = leader;
            PerceivedSupport = support; PerceivedThreats = threats; OverwhelmingPressure01 = pressure;
        }
    }
}
