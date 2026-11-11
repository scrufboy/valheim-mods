// Intended compile location: LivingWorldCORE/npc/src/Integration/CoreEncounterAdapter.cs
// Proven source baseline: 9933763978fd7c29bfa9909e7ea8b6c47f86e421 (1.0.32.7).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LivingWorld.CompanionSystems.LiveCoreAdapter;

namespace LivingWorld.Core
{
    /// <summary>
    /// First maintained encounter slice: server-only creation, durable identity tags,
    /// persistent/process duplicate refusal, unload-safe claim release, and observation
    /// of witnessed first contact. It never sets a target or starts an attack.
    /// </summary>
    internal sealed class CoreEncounterAdapter
    {
        internal const string SchemaKey = "livingworld.encounter.schema";
        internal const string Schema = "lwc-encounter-actor-v1";
        internal const string PersonKey = "livingworld.person.id";
        internal const string EncounterKey = "livingworld.encounter.id";
        internal const string SiteKey = "livingworld.site.id";
        internal const string CampKey = "livingworld.camp.id";
        internal const string IncidentKey = "livingworld.incident.id";

        readonly PersonSpawnClaims processClaims = new PersonSpawnClaims();
        readonly Dictionary<NpcKernel, string> loaded = new Dictionary<NpcKernel, string>();

        internal CoreEncounterAdapter() { NpcKernel.Removed += OnRemoved; }
        internal void Dispose() { NpcKernel.Removed -= OnRemoved; }

        // Must be driven by CorePlugin.StartCoroutine. callback is invoked exactly once.
        internal IEnumerator CreateOnce(string prefabId, Vector3 position, Quaternion rotation,
            EncounterActorIds ids, Action<NpcKernel, string> callback)
        {
            string reason = "invalid_ids";
            if (callback == null) yield break;
            if (ids == null || !ids.IsValid(out reason)) { AdapterCompletion.Deliver(callback, null, reason); yield break; }
            if (!EncounterAdapterPolicy.IsSupportedPrefab(prefabId)) { AdapterCompletion.Deliver(callback, null, "unsupported_prefab"); yield break; }
            if (ZNet.instance == null || !ZNet.instance.IsServer()) { AdapterCompletion.Deliver(callback, null, "server_authority_required"); yield break; }
            if (!CorePlugin.Registered || ZDOMan.instance == null || ZNetScene.instance == null)
            { AdapterCompletion.Deliver(callback, null, "world_not_ready"); yield break; }

            // Persistent duplicate scan precedes the process claim. This closes reload and
            // unloaded-zone duplication; the process claim closes same-process re-entry.
            foreach (string candidate in EncounterAdapterPolicy.SupportedPrefabIds)
            {
                var records = new List<ZDO>(); int index = 0;
                while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(candidate, records, ref index)) yield return null;
                foreach (ZDO zdo in records)
                    if (zdo != null && zdo.IsValid() && zdo.GetString(PersonKey, "") == ids.PersonId)
                    { AdapterCompletion.Deliver(callback, null, "persistent_person_already_exists"); yield break; }
            }
            if (!processClaims.TryClaim(ids.PersonId)) { AdapterCompletion.Deliver(callback, null, "loaded_person_already_claimed"); yield break; }

            GameObject actor = null;
            NpcKernel created = null;
            string completion = "created";
            try
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(prefabId);
                if (prefab == null) throw new InvalidOperationException("registered prefab not found");
                actor = UnityEngine.Object.Instantiate(prefab, position, rotation);
                ZNetView view = actor.GetComponent<ZNetView>();
                if (view == null || !view.IsValid() || !view.IsOwner()) throw new InvalidOperationException("new actor is not locally owned");
                NpcFactory.Validate(actor);
                ZDO zdo = view.GetZDO();
                zdo.Set(SchemaKey, Schema); zdo.Set(PersonKey, ids.PersonId);
                zdo.Set(EncounterKey, ids.EncounterId); zdo.Set(SiteKey, ids.SiteId);
                zdo.Set(CampKey, ids.CampId); zdo.Set(IncidentKey, ids.IncidentId);
                created = actor.GetComponent<NpcKernel>();
                loaded.Add(created, ids.PersonId);
            }
            catch (Exception error)
            {
                processClaims.Release(ids.PersonId);
                if (actor != null)
                {
                    ZNetView view = actor.GetComponent<ZNetView>();
                    if (view != null && view.IsValid() && view.IsOwner()) ZNetScene.instance.Destroy(actor);
                    else UnityEngine.Object.Destroy(actor);
                }
                created = null;
                completion = "creation_failed:" + error.Message;
            }
            // Outside the creation try: consumer failure cannot roll back a valid actor or trigger a retry.
            AdapterCompletion.Deliver(callback, created, completion);
        }

        internal ContactDirective ObserveFirstContact(NpcKernel kernel, bool playerWitnessed,
            bool attributedDamage, bool hostileTargetingActor, out string reason)
        {
            EncounterActorIds ids;
            if (!TryRead(kernel, out ids)) { reason = "missing_or_corrupt_encounter_identity"; return ContactDirective.None; }
            NpcPhase phase = kernel.Frame.Phase;
            bool nativeCombat = phase == NpcPhase.Combat || phase == NpcPhase.CombatClear || phase == NpcPhase.CombatRegroup;
            return EncounterAdapterPolicy.ObserveFirstContact(new FirstContactFacts {
                Ids=ids, IsServer=ZNet.instance != null && ZNet.instance.IsServer(),
                IsOwner=kernel.Body != null && kernel.Body.IsOwner, ActorLoaded=true,
                PlayerWitnessed=playerWitnessed, AttributedDamage=attributedDamage,
                HostileTargetingActor=hostileTargetingActor, NativeCombatCommitted=nativeCombat
            }, out reason);
        }

        static bool TryRead(NpcKernel kernel, out EncounterActorIds ids)
        {
            ids = null;
            if (kernel == null || kernel.Body == null || kernel.Body.View == null || !kernel.Body.View.IsValid()) return false;
            ZDO zdo = kernel.Body.View.GetZDO();
            if (zdo == null || zdo.GetString(SchemaKey, "") != Schema) return false;
            var value = new EncounterActorIds { PersonId=zdo.GetString(PersonKey, ""), EncounterId=zdo.GetString(EncounterKey, ""),
                SiteId=zdo.GetString(SiteKey, ""), CampId=zdo.GetString(CampKey, ""), IncidentId=zdo.GetString(IncidentKey, "") };
            string reason; if (!value.IsValid(out reason)) return false;
            ids = value; return true;
        }

        void OnRemoved(NpcKernel kernel, string reason)
        {
            string personId;
            if (!loaded.TryGetValue(kernel, out personId)) return;
            loaded.Remove(kernel);
            processClaims.Release(personId); // unload/death releases memory only; the ZDO remains authoritative.
        }
    }
}
