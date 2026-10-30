using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LivingWorld.Companions.VikingAISalvage
{
    internal static class Benchmark
    {
        public static int Main()
        {
            List<EquipmentCandidate> items = new List<EquipmentCandidate> {
                new EquipmentCandidate { Id = "axe", Slot = EquipmentSlot.MainHand, Power = 12, Allowed = true, IsTwoHanded = true },
                new EquipmentCandidate { Id = "sword", Slot = EquipmentSlot.MainHand, Power = 9, Allowed = true },
                new EquipmentCandidate { Id = "shield", Slot = EquipmentSlot.OffHand, Power = 7, Allowed = true, IsShield = true },
                new EquipmentCandidate { Id = "head", Slot = EquipmentSlot.Head, Power = 4, Allowed = true },
                new EquipmentCandidate { Id = "chest", Slot = EquipmentSlot.Chest, Power = 5, Allowed = true },
                new EquipmentCandidate { Id = "legs", Slot = EquipmentSlot.Legs, Power = 4, Allowed = true },
                new EquipmentCandidate { Id = "cape", Slot = EquipmentSlot.Cape, Power = 2, Allowed = true }
            };
            const int iterations = 100000;
            GC.Collect();
            long before = GC.GetTotalMemory(true);
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++) EquipmentLoadoutPolicy.SelectBest(items);
            watch.Stop();
            long after = GC.GetTotalMemory(false);
            Console.WriteLine("equipment iterations=" + iterations + " elapsed_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3") +
                              " retained_bytes_delta=" + (after - before));
            return 0;
        }
    }
}
