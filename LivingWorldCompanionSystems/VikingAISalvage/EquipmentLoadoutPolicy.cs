using System;
using System.Collections.Generic;

namespace LivingWorld.Companions.VikingAISalvage
{
    public enum EquipmentSlot
    {
        MainHand,
        OffHand,
        Head,
        Chest,
        Legs,
        Cape
    }

    public sealed class EquipmentCandidate
    {
        public string Id;
        public EquipmentSlot Slot;
        public float Power;
        public bool IsTwoHanded;
        public bool IsShield;
        public bool Allowed;
    }

    public sealed class EquipmentLoadout
    {
        private readonly Dictionary<EquipmentSlot, EquipmentCandidate> _items =
            new Dictionary<EquipmentSlot, EquipmentCandidate>();

        public IDictionary<EquipmentSlot, EquipmentCandidate> Items { get { return _items; } }
        public float Score { get; internal set; }
    }

    public static class EquipmentLoadoutPolicy
    {
        public static EquipmentLoadout SelectBest(IList<EquipmentCandidate> candidates)
        {
            if (candidates == null) throw new ArgumentNullException("candidates");

            EquipmentLoadout best = ScoreLoadout(candidates, null);
            for (int i = 0; i < candidates.Count; i++)
            {
                EquipmentCandidate main = candidates[i];
                if (!IsUsable(main) || main.Slot != EquipmentSlot.MainHand) continue;
                EquipmentLoadout proposed = ScoreLoadout(candidates, main);
                if (IsBetterLoadout(proposed, best)) best = proposed;
            }
            return best;
        }

        private static EquipmentLoadout ScoreLoadout(IList<EquipmentCandidate> candidates, EquipmentCandidate forcedMain)
        {
            EquipmentLoadout result = new EquipmentLoadout();
            if (forcedMain != null) result.Items[EquipmentSlot.MainHand] = forcedMain;

            for (int i = 0; i < candidates.Count; i++)
            {
                EquipmentCandidate item = candidates[i];
                if (!IsUsable(item)) continue;
                if (item.Slot == EquipmentSlot.MainHand) continue;
                if (item.Slot == EquipmentSlot.OffHand && forcedMain != null && forcedMain.IsTwoHanded) continue;

                EquipmentCandidate existing;
                if (!result.Items.TryGetValue(item.Slot, out existing) || IsBetter(item, existing))
                    result.Items[item.Slot] = item;
            }

            foreach (EquipmentCandidate item in result.Items.Values) result.Score += item.Power;
            return result;
        }

        private static bool IsUsable(EquipmentCandidate item)
        {
            return item != null && item.Allowed && !string.IsNullOrEmpty(item.Id);
        }

        private static bool IsBetter(EquipmentCandidate candidate, EquipmentCandidate existing)
        {
            if (candidate.Power != existing.Power) return candidate.Power > existing.Power;
            return string.CompareOrdinal(candidate.Id, existing.Id) < 0;
        }

        private static bool IsBetterLoadout(EquipmentLoadout candidate, EquipmentLoadout existing)
        {
            if (candidate.Score != existing.Score) return candidate.Score > existing.Score;
            EquipmentCandidate candidateMain;
            EquipmentCandidate existingMain;
            bool candidateHasMain = candidate.Items.TryGetValue(EquipmentSlot.MainHand, out candidateMain);
            bool existingHasMain = existing.Items.TryGetValue(EquipmentSlot.MainHand, out existingMain);
            if (candidateHasMain != existingHasMain) return candidateHasMain;
            return candidateHasMain && string.CompareOrdinal(candidateMain.Id, existingMain.Id) < 0;
        }
    }
}
