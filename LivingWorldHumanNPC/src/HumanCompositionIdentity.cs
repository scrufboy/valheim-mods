using System;
using UnityEngine;
using LivingWorld.ManualTest;

namespace LivingWorld.HumanNpc
{
    // Read-only on load. No Awake/Start callback can bless an existing saved actor.
    // Commands may stamp only this mod's composition metadata; native save fields
    // and the unchanged visual revision are never overwritten by this adapter.
    public sealed class HumanCompositionIdentity : MonoBehaviour
    {
        string failure = "";

        public bool IsReady
        {
            get { return State == HumanMigrationState.Ready; }
        }

        public bool IsCompositionReady
        {
            get { var state = State; return state == HumanMigrationState.Ready || state == HumanMigrationState.CurrentAppearancePending; }
        }

        public string Status
        {
            get { return HumanMigrationPolicy.Describe(State) + (failure == "" ? "" : "; " + failure); }
        }

        internal HumanMigrationState State
        {
            get
            {
                var view = GetComponent<ZNetView>();
                if (view == null || !view.IsValid() || view.GetZDO().GetPrefab() != HumanMigrationPolicy.Identity.GetStableHashCode()) return HumanMigrationState.Refused;
                return HumanMigrationPolicy.Inspect(Read(view.GetZDO()));
            }
        }

        public bool TryMigrate(Player player)
        {
            try
            {
                var view = Guard(player);
                var companion = GetComponent<HumanCompanion>();
                if (companion == null) throw new InvalidOperationException("Human companion component is missing; no migration metadata written.");
                var plan = HumanMigrationPolicy.PlanMigration(Read(view.GetZDO()), true, view.IsOwner(), true);
                foreach (var pair in plan) view.GetZDO().Set(pair.Key, pair.Value);
                if (!IsReady) throw new InvalidOperationException("Composition commit could not be read back; actor remains gated.");
                if (!companion.InitializeOrders(player)) throw new InvalidOperationException("Composition committed, but companion order initialization was refused; existing order data is retained.");
                failure = "";
                return true;
            }
            catch (Exception error) { failure = "Migration refused: " + error.Message; return false; }
        }

        // Only called synchronously by the plugin's explicit spawn command with
        // the exact newly instantiated instance. No saved-actor discovery calls it.
        public void InitializeFresh(Player player)
        {
            var view = Guard(player);
            var companion = GetComponent<HumanCompanion>();
            if (companion == null) throw new InvalidOperationException("Human companion component missing; no fresh composition metadata written.");
            var plan = HumanMigrationPolicy.PlanFresh(Read(view.GetZDO()), true, true, view.IsOwner());
            foreach (var pair in plan) view.GetZDO().Set(pair.Key, pair.Value);
            if (!IsCompositionReady) throw new InvalidOperationException("Fresh composition commit could not be read back.");
            if (!companion.InitializeOrders(player)) throw new InvalidOperationException("Fresh composition committed, but initial companion order was refused.");
            failure = "";
        }

        ZNetView Guard(Player player)
        {
            ManualTestPlugin.GuardWorld();
            if (player == null || player != Player.m_localPlayer) throw new InvalidOperationException("Only the current local tester may initialize or migrate the scout.");
            var view = GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner()) throw new InvalidOperationException("A valid locally owned scout is required; ownership is never claimed.");
            if (view.GetZDO().GetPrefab() != HumanMigrationPolicy.Identity.GetStableHashCode()) throw new InvalidOperationException("Saved prefab identity does not match the human scout.");
            return view;
        }

        static HumanMigrationSnapshot Read(ZDO zdo)
        {
            return new HumanMigrationSnapshot(zdo.GetString(HumanMigrationPolicy.VisualKey, ""), zdo.GetString(HumanMigrationPolicy.CompositionKey, ""),
                zdo.GetString(HumanMigrationPolicy.OriginKey, ""), zdo.GetString(HumanMigrationPolicy.IdentityKey, ""), zdo.GetString(HumanMigrationPolicy.LabTagKey, ""));
        }
    }
}
