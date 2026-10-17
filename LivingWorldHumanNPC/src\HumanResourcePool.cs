using System;
using System.Globalization;

namespace LivingWorld.HumanNpc
{
    // One bounded ledger. Native actions decide when to debit; this class never
    // invents another attack charge, movement decision, item, or offline job.
    public sealed class HumanResourcePool
    {
        public double Capacity { get; private set; }
        public double Current { get; private set; }
        public double RecoveryDelay { get; private set; }

        public HumanResourcePool(double capacity)
        {
            if (!Finite(capacity) || capacity <= 0 || capacity > 10000)
                throw new ArgumentOutOfRangeException("capacity");
            Capacity = Current = capacity;
        }

        public static bool Finite(double number)
        { return !double.IsNaN(number) && !double.IsInfinity(number); }

        // Native Player.HaveStamina uses strictly greater, including cost zero.
        public bool Have(double cost)
        { return Finite(cost) && cost >= 0 && Current > cost; }

        public bool Use(double amount, double worldCostScale, double delay)
        {
            if (!Finite(amount) || !Finite(worldCostScale) || worldCostScale < 0 ||
                !Finite(delay) || delay < 0 || delay > 60) return false;
            double scaled = amount * worldCostScale;
            if (!Finite(scaled) || scaled == 0) return false;
            Current = Math.Max(0, Math.Min(Capacity, Current - scaled));
            RecoveryDelay = delay;
            return true;
        }

        public bool Add(double amount)
        {
            if (!Finite(amount) || amount == 0) return false;
            Current = Math.Max(0, Math.Min(Capacity, Current + amount));
            return true;
        }

        public bool Advance(double seconds, double baseRate, double missingFractionBonus,
            double recoveryScale, bool permitted)
        {
            // No catch-up for unload, pause or a long frame. Recovery is observed
            // loaded time only; regeneration never turns a frame hitch into rest.
            if (!Finite(seconds) || seconds < 0 || seconds > .25 ||
                !Finite(baseRate) || baseRate < 0 || !Finite(missingFractionBonus) || missingFractionBonus < 0 ||
                !Finite(recoveryScale) || recoveryScale < 0) return false;
            double activeTime = Math.Max(0, seconds - RecoveryDelay);
            double nextDelay = Math.Max(0, RecoveryDelay - seconds);
            double rate = baseRate * (1 + (1 - Current / Capacity) * missingFractionBonus) * recoveryScale;
            if (!Finite(rate)) return false;
            double next = permitted ? Math.Min(Capacity, Current + Math.Min(Capacity - Current, rate * activeTime)) : Current;
            bool changed = next != Current || nextDelay != RecoveryDelay;
            Current = next; RecoveryDelay = nextDelay;
            return changed;
        }

        public string Save()
        {
            return "1|" + Capacity.ToString("R", CultureInfo.InvariantCulture) + "|" +
                Current.ToString("R", CultureInfo.InvariantCulture) + "|" + RecoveryDelay.ToString("R", CultureInfo.InvariantCulture);
        }

        public static bool TryRestore(string record, double expectedCapacity, out HumanResourcePool result)
        {
            result = null;
            if (String.IsNullOrEmpty(record) || record.Length > 192) return false;
            var parts = record.Split('|');
            double capacity, current, delay;
            if (parts.Length != 4 || parts[0] != "1" ||
                !Double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out capacity) ||
                !Double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out current) ||
                !Double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out delay) ||
                !Finite(capacity) || capacity <= 0 || capacity > 10000 || capacity != expectedCapacity ||
                !Finite(current) || current < 0 || current > capacity || !Finite(delay) || delay < 0 || delay > 60)
                return false;
            result = new HumanResourcePool(capacity) { Current = current, RecoveryDelay = delay };
            return true;
        }
    }
}
