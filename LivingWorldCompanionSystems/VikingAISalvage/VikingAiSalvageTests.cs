using System;
using System.Collections.Generic;

namespace LivingWorld.Companions.VikingAISalvage
{
    internal static class VikingAiSalvageTests
    {
        private static int _passed;

        public static int Main()
        {
            Run("safe defaults disable every feature", SafeDefaultsDisableFeatures);
            Run("committed combat preserves native MonsterAI", CombatPreservesNativeOwnership);
            Run("VikingAI never becomes movement writer", VikingAiNeverWritesMovement);
            Run("equipment produces one coherent loadout", EquipmentIsCoherent);
            Run("equipment ties ignore inventory order", EquipmentTieIsDeterministic);
            Run("portal rejects duplicates and illegal transfers", PortalRejectsUnsafeRequests);
            Run("portal requires readiness before one commit", PortalRequiresReadiness);
            Console.WriteLine("PASS: " + _passed + " VikingAI salvage contract tests");
            return 0;
        }

        private static void SafeDefaultsDisableFeatures()
        {
            VikingAiAdapterOptions options = VikingAiAdapterOptions.SafeDefaults();
            Assert(!options.IsEnabled(VikingAiFeature.SensoryAdvisories), "sensory must be opt-in");
            Assert(!OwnershipPolicy.MayRunAdvisory(options, VikingAiFeature.SensoryAdvisories, false), "disabled advisory ran");
        }

        private static void CombatPreservesNativeOwnership()
        {
            Assert(OwnershipPolicy.SelectAuthority(true) == RuntimeAuthority.NativeMonsterAI, "native combat lost ownership");
            VikingAiAdapterOptions options = new VikingAiAdapterOptions(VikingAiFeature.SensoryAdvisories);
            Assert(!OwnershipPolicy.MayRunAdvisory(options, VikingAiFeature.SensoryAdvisories, true), "advisory ran during combat");
        }

        private static void VikingAiNeverWritesMovement()
        {
            Assert(!OwnershipPolicy.MayVikingAiWriteMovement(), "VikingAI became a movement writer");
            Assert(OwnershipPolicy.SelectAuthority(false) == RuntimeAuthority.LivingWorldMovementWriter, "LWC lost peaceful movement ownership");
        }

        private static void EquipmentIsCoherent()
        {
            List<EquipmentCandidate> items = new List<EquipmentCandidate> {
                Item("two-hand", EquipmentSlot.MainHand, 15, true, false),
                Item("sword", EquipmentSlot.MainHand, 10, false, false),
                Item("shield", EquipmentSlot.OffHand, 8, false, true),
                Item("weak-shield", EquipmentSlot.OffHand, 2, false, true),
                Item("helm", EquipmentSlot.Head, 3, false, false),
                Item("forbidden", EquipmentSlot.Chest, 100, false, false, false)
            };
            EquipmentLoadout loadout = EquipmentLoadoutPolicy.SelectBest(items);
            Assert(loadout.Items[EquipmentSlot.MainHand].Id == "sword", "two-hand weapon incorrectly beat sword+shield");
            Assert(loadout.Items[EquipmentSlot.OffHand].Id == "shield", "best shield not selected");
            Assert(!loadout.Items.ContainsKey(EquipmentSlot.Chest), "forbidden item selected");
            Assert(loadout.Items.Count == 3, "more than one item per slot or missing armor");
        }

        private static void PortalRejectsUnsafeRequests()
        {
            PortalTransactionGate gate = new PortalTransactionGate();
            PortalTransferRequest request = Request("token-a");
            Assert(gate.Begin(request, false).Failure == "legal-restriction", "illegal transfer accepted");
            Assert(gate.Begin(request, true).State == PortalTransferState.DestinationValidated, "legal transfer rejected");
            Assert(gate.Begin(request, true).Failure == "duplicate-request", "duplicate transfer accepted");
        }

        private static void EquipmentTieIsDeterministic()
        {
            List<EquipmentCandidate> first = new List<EquipmentCandidate> {
                Item("z-sword", EquipmentSlot.MainHand, 10, false, false),
                Item("a-sword", EquipmentSlot.MainHand, 10, false, false)
            };
            List<EquipmentCandidate> second = new List<EquipmentCandidate> { first[1], first[0] };
            Assert(EquipmentLoadoutPolicy.SelectBest(first).Items[EquipmentSlot.MainHand].Id == "a-sword", "tie was not stable");
            Assert(EquipmentLoadoutPolicy.SelectBest(second).Items[EquipmentSlot.MainHand].Id == "a-sword", "inventory order changed tie");
        }

        private static void PortalRequiresReadiness()
        {
            PortalTransactionGate gate = new PortalTransactionGate();
            PortalTransfer transfer = gate.Begin(Request("token-b"), true);
            Assert(!gate.Commit(transfer), "committed before readiness");
            Assert(!gate.MarkDestinationReady(transfer, true, false), "accepted unloaded instance");

            PortalTransfer ready = gate.Begin(Request("token-c"), true);
            Assert(gate.MarkDestinationReady(ready, true, true), "ready destination rejected");
            Assert(gate.Commit(ready), "commit failed");
            Assert(!gate.Commit(ready), "duplicate commit succeeded");
            Assert(gate.Complete(ready), "completion failed");
            Assert(!gate.RollBack(ready), "completed transfer rolled back");
        }

        private static EquipmentCandidate Item(string id, EquipmentSlot slot, float power, bool twoHanded, bool shield, bool allowed = true)
        {
            return new EquipmentCandidate { Id = id, Slot = slot, Power = power, IsTwoHanded = twoHanded, IsShield = shield, Allowed = allowed };
        }

        private static PortalTransferRequest Request(string token)
        {
            return new PortalTransferRequest { Identity = "npc-42", SourcePortal = "portal-a", DestinationPortal = "portal-b", RequestToken = token };
        }

        private static void Run(string name, Action test)
        {
            test();
            _passed++;
            Console.WriteLine("ok - " + name);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
