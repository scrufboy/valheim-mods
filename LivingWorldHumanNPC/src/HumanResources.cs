using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Resource provider, NOT another AI/movement controller. Only our composed
    // humans carry this component. Foreign NPCs and real Players stay native.
    public sealed class HumanResources : MonoBehaviour
    {
        internal const string RecordKey = "livingworld_human_stamina_v1";
        public float Capacity, BaseRecovery, MissingFractionBonus, SpendDelay;
        public string Status = "Waiting for human initialization";
        static bool patchesReady;
        ZNetView view;
        Humanoid body;
        HumanAppearance appearance;
        HumanCompositionIdentity composition;
        HumanResourcePool pool;
        bool owned, faulted, dirty;
        float pendingTime, sinceSaved;

        internal void Configure(Player nativeValues)
        {
            if (nativeValues == null) throw new ArgumentNullException("nativeValues");
            Configure(nativeValues.m_baseStamina, nativeValues.m_staminaRegen,
                nativeValues.m_staminaRegenTimeMultiplier, nativeValues.m_staminaRegenDelay);
        }

        internal void Configure(float capacity, float baseRecovery, float missingFractionBonus, float spendDelay)
        {
            Capacity = capacity;
            BaseRecovery = baseRecovery;
            MissingFractionBonus = missingFractionBonus;
            SpendDelay = spendDelay;
            if (!ConfigurationValid()) throw new InvalidOperationException("Invalid native stamina parameters.");
        }

        bool ConfigurationValid()
        {
            return HumanResourcePool.Finite(Capacity) && Capacity > 0 && Capacity <= 10000 &&
                HumanResourcePool.Finite(BaseRecovery) && BaseRecovery >= 0 &&
                HumanResourcePool.Finite(MissingFractionBonus) && MissingFractionBonus >= 0 &&
                HumanResourcePool.Finite(SpendDelay) && SpendDelay >= 0 && SpendDelay <= 60;
        }

        internal static void InstallPatches(Harmony harmony)
        {
            Patch(harmony, "GetMaxStamina", Type.EmptyTypes, "BeforeMaximum");
            Patch(harmony, "GetStaminaPercentage", Type.EmptyTypes, "BeforePercentage");
            Patch(harmony, "HaveStamina", new[] { typeof(float) }, "BeforeHave");
            Patch(harmony, "UseStamina", new[] { typeof(float) }, "BeforeUse");
            Patch(harmony, "AddStamina", new[] { typeof(float) }, "BeforeAdd");
            patchesReady = true;
        }

        static void Patch(Harmony harmony, string target, Type[] signature, string handler)
        {
            var method = typeof(Character).GetMethod(target, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null, signature, null);
            if (method == null) throw new MissingMethodException(typeof(Character).FullName, target);
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(HumanResources).GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Static)));
        }

        void Cache()
        {
            if (view == null) view = GetComponent<ZNetView>();
            if (body == null) body = GetComponent<Humanoid>();
            if (appearance == null) appearance = GetComponent<HumanAppearance>();
            if (composition == null) composition = GetComponent<HumanCompositionIdentity>();
        }

        void ForgetAuthority()
        {
            owned = false; pool = null; dirty = false; pendingTime = sinceSaved = 0;
        }

        bool ReadyOwner()
        {
            Cache();
            if (view == null || !view.IsValid() || !view.IsOwner())
            { ForgetAuthority(); return false; }
            if (faulted || !patchesReady || !isActiveAndEnabled || body == null || body is Player || body.IsDead() ||
                appearance == null || !appearance.Initialized || composition == null || !composition.IsReady) return false;
            if (!owned)
            {
                if (!ConfigurationValid()) { Fault("Invalid resource configuration"); return false; }
                string record = view.GetZDO().GetString(RecordKey, "");
                if (record.Length == 0)
                {
                    pool = new HumanResourcePool(Capacity);
                    // A new record is created once, only on a ready owned human.
                    // All subsequent acquisitions restore rather than refill.
                    owned = true; dirty = true; Persist();
                }
                else if (!HumanResourcePool.TryRestore(record, Capacity, out pool))
                { Fault("Unknown/malformed resource record; preserved without refilling"); return false; }
                else owned = true;
                pendingTime = sinceSaved = 0;
                Status = "Finite stamina active; native actions spend from the saved pool";
            }
            return true;
        }

        HumanResourcePool Read()
        {
            if (ReadyOwner()) return pool;
            if (faulted || !patchesReady || !isActiveAndEnabled || body == null || body is Player || body.IsDead() ||
                view == null || !view.IsValid() || view.IsOwner() || !ConfigurationValid()) return null;
            HumanResourcePool remote;
            return HumanResourcePool.TryRestore(view.GetZDO().GetString(RecordKey, ""), Capacity, out remote) ? remote : null;
        }

        void Update()
        {
            try
            {
                if (!ReadyOwner()) return;
                float dt = Time.deltaTime;
                if (!HumanResourcePool.Finite(dt) || dt <= 0) return;
                pendingTime = Math.Min(.25f, pendingTime + Math.Min(.25f, dt));
                if (pendingTime < .1f) return;
                float modifier = 1f;
                var effects = body.GetSEMan();
                if (effects != null) effects.ModifyStaminaRegen(ref modifier);
                bool permitted = !body.InAttack() && !body.InDodge() && !body.IsEncumbered() &&
                    !(body.IsSwimming() && !body.IsOnGround()) && !body.IsWallRunning();
                double scale = modifier * (double)Game.m_staminaRegenRate * (body.IsBlocking() ? .8 : 1);
                dirty |= pool.Advance(pendingTime, BaseRecovery, MissingFractionBonus, scale, permitted);
                sinceSaved += pendingTime; pendingTime = 0;
                if (sinceSaved >= 1f) Persist();
            }
            catch (Exception error) { Fault(error.Message); }
        }

        void Persist()
        {
            if (!dirty || pool == null || view == null || !view.IsValid() || !view.IsOwner()) return;
            view.GetZDO().Set(RecordKey, pool.Save());
            dirty = false; sinceSaved = 0;
        }

        void Use(float amount)
        {
            if (!ReadyOwner()) return;
            if (!pool.Use(amount, Game.m_staminaRate, SpendDelay)) return;
            pendingTime = 0; dirty = true; Persist();
        }

        void Add(float amount)
        {
            if (!ReadyOwner()) return;
            if (!pool.Add(amount)) return;
            pendingTime = 0; dirty = true; Persist();
        }

        void Fault(string reason)
        {
            faulted = true; Status = "Resource provider stopped: " + reason;
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report(Status);
        }

        public string Describe()
        {
            var state = Read();
            return state == null ? Status : String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Stamina {0:0.0}/{1:0.0}; recovery delay {2:0.00}s", state.Current, state.Capacity, state.RecoveryDelay);
        }

        void OnDisable() { try { Persist(); } finally { ForgetAuthority(); } }
        void OnDestroy() { try { Persist(); } finally { ForgetAuthority(); } }

        static HumanResources For(Character actor)
        { return actor == null || actor is Player ? null : actor.GetComponent<HumanResources>(); }

        static bool BeforeMaximum(Character __instance, ref float __result)
        {
            var provider = For(__instance); if (provider == null) return true;
            try { var state = provider.Read(); __result = state == null ? 0f : (float)state.Capacity; }
            catch (Exception error) { provider.Fault(error.Message); __result = 0f; }
            return false;
        }
        static bool BeforePercentage(Character __instance, ref float __result)
        {
            var provider = For(__instance); if (provider == null) return true;
            try { var state = provider.Read(); __result = state == null ? 0f : (float)(state.Current / state.Capacity); }
            catch (Exception error) { provider.Fault(error.Message); __result = 0f; }
            return false;
        }
        static bool BeforeHave(Character __instance, float __0, ref bool __result)
        {
            var provider = For(__instance); if (provider == null) return true;
            try { var state = provider.Read(); __result = state != null && state.Have(__0); }
            catch (Exception error) { provider.Fault(error.Message); __result = false; }
            return false;
        }
        static bool BeforeUse(Character __instance, float __0)
        {
            var provider = For(__instance); if (provider == null) return true;
            try { provider.Use(__0); } catch (Exception error) { provider.Fault(error.Message); }
            return false;
        }
        static bool BeforeAdd(Character __instance, float __0)
        {
            var provider = For(__instance); if (provider == null) return true;
            try { provider.Add(__0); } catch (Exception error) { provider.Fault(error.Message); }
            return false;
        }
    }
}
