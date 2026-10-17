using System;
using System.Globalization;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Sensing is read-only. The only writes are this actor's confidence record.
    // This queryable input does not yet modify CCD, stamina, armor, health,
    // movement, equipment, food or native Rested/comfort duration.
    public sealed class HumanPartyConfidence : MonoBehaviour
    {
        internal const string RecordKey = "livingworld_human_party_confidence_v1";
        public string Status = "Confidence awaiting initialized owned human";
        static readonly string[] CharacterLayers = { "character", "character_net", "character_ghost", "character_noenv" };
        readonly Collider[] nearby = new Collider[ConfidencePolicy.ColliderLimit];
        readonly Character[] actors = new Character[ConfidencePolicy.ActorLimit];
        ZNetView view;
        Humanoid body;
        MonsterAI ai;
        HumanAppearance appearance;
        HumanCompositionIdentity identity;
        HumanIdentity personalIdentity;
        HumanCompanion companion;
        ConfidencePolicy state;
        bool owned, dirty, faulted;
        float pending, sinceSaved, nextObservation;
        long party;
        int supportCount, threatCount;
        double pressure;
        ConfidenceLeaderState leaderState;
        bool layersChecked;
        int characterMask;

        void Cache()
        {
            if (view == null) view = GetComponent<ZNetView>();
            if (body == null) body = GetComponent<Humanoid>();
            if (ai == null) ai = GetComponent<MonsterAI>();
            if (appearance == null) appearance = GetComponent<HumanAppearance>();
            if (identity == null) identity = GetComponent<HumanCompositionIdentity>();
            if (personalIdentity == null) personalIdentity = GetComponent<HumanIdentity>();
            if (companion == null) companion = GetComponent<HumanCompanion>();
        }

        void ForgetAuthority()
        {
            owned = dirty = false; state = null; pending = sinceSaved = 0;
            nextObservation = 0; party = 0; supportCount = threatCount = 0; pressure = 0;
            leaderState = ConfidenceLeaderState.Unknown;
            Array.Clear(nearby, 0, nearby.Length); Array.Clear(actors, 0, actors.Length);
        }

        bool ReadyOwner()
        {
            Cache();
            if (view == null || !view.IsValid() || !view.IsOwner())
            { ForgetAuthority(); return false; }
            if (faulted || !isActiveAndEnabled || body == null || body is Player || body.IsDead() || ai == null ||
                identity == null || !identity.IsReady || appearance == null || !appearance.Initialized || companion == null)
            { pending = 0; return false; }
            HumanProfile profile;
            HumanProfileState profileState = ReadProfile(out profile);
            if (profileState == HumanProfileState.Refused)
            { pending = 0; Status = "Confidence gated by unknown/malformed personal identity"; return false; }
            if (!owned)
            {
                string record = view.GetZDO().GetString(RecordKey, "");
                if (record.Length == 0)
                {
                    // Immutable profile seeds the trait once. Existing saved
                    // confidence always retains its own courage on reacquisition.
                    double initialCourage = profileState == HumanProfileState.Ready ? profile.InitialCourage / 100.0 : .5;
                    state = new ConfidencePolicy(ConfidencePolicy.InitialConfidence, initialCourage);
                    owned = true; dirty = true; Persist();
                }
                else if (!ConfidencePolicy.TryRestore(record, out state))
                { Fault("Unknown/malformed saved confidence preserved without replacing it"); return false; }
                else owned = true;
                pending = sinceSaved = 0;
                nextObservation = Time.time + (float)ConfidencePolicy.ObservationInterval;
            }
            return true;
        }

        HumanProfileState ReadProfile(out HumanProfile profile)
        {
            profile = null;
            if (personalIdentity != null) return personalIdentity.Read(out profile);
            return view.GetZDO().GetString(HumanProfile.RecordKey, "").Length == 0 ? HumanProfileState.Absent : HumanProfileState.Refused;
        }

        bool SenseLayersReady()
        {
            if (layersChecked) return characterMask != 0;
            layersChecked = true;
            // These four character layers were verified in native Character.Awake
            // and Player.Awake. Environment colliders must never fill this buffer.
            int mask = 0;
            foreach (string name in CharacterLayers)
            {
                int layer = LayerMask.NameToLayer(name);
                if (layer < 0 || layer > 31) { characterMask = 0; return false; }
                mask |= 1 << layer;
            }
            characterMask = mask; return characterMask != 0;
        }

        void Update()
        {
            try
            {
                if (!ReadyOwner()) return;
                float dt = Time.deltaTime;
                if (!ConfidencePolicy.Finite(dt) || dt <= 0 || !ConfidencePolicy.Finite(Time.time)) return;
                pending = Math.Min((float)ConfidencePolicy.ObservationInterval, pending + Math.Min(.25f, dt));
                if (Time.time < nextObservation) return;
                nextObservation = Time.time + (float)ConfidencePolicy.ObservationInterval;
                var observation = Observe();
                dirty |= state.Advance(pending, observation, pressure);
                sinceSaved += pending; pending = 0;
                Status = String.Format(CultureInfo.InvariantCulture,
                    "Confidence {0:0.0}/100; {1}; leader {2}; perceived party {3}, threats {4}; input only, no native Rested effect",
                    state.Current, state.Cohesion, leaderState, supportCount, threatCount);
                if (sinceSaved >= 1f) Persist();
            }
            catch (Exception error) { Fault(error.Message); }
        }

        PartySupportObservation Observe()
        {
            supportCount = threatCount = 0; pressure = 0; leaderState = ConfidenceLeaderState.Unknown;
            if (!SenseLayersReady()) return PartySupportObservation.Unknown;
            long observedParty; Player leader;
            // Parent contract: Ready + stable order.OwnerId + loaded Player.GetPlayer.
            // Includes combat/Hold and dead leader; never use PresenceLeader here.
            if (!companion.TryGetConfidenceParty(out observedParty, out leader) || observedParty == 0)
            { party = 0; return PartySupportObservation.Unknown; }
            if (party != 0 && party != observedParty)
                state.Advance(ConfidencePolicy.ObservationInterval, PartySupportObservation.Unknown, 0);
            party = observedParty;
            leaderState = leader == null ? ConfidenceLeaderState.Unloaded :
                (leader.IsDead() ? ConfidenceLeaderState.LoadedDead : ConfidenceLeaderState.LoadedAlive);
            var leaderView = leader == null ? null : leader.GetComponent<ZNetView>();
            if (leader != null && (leaderView == null || !leaderView.IsValid())) leaderState = ConfidenceLeaderState.Unknown;
            float supportRange = state.Cohesion == PartyCohesion.Together ? ConfidencePolicy.SupportRemainRange : ConfidencePolicy.SupportEnterRange;
            bool leaderSupported = leader != null && !leader.IsDead() && Perceive(leader, supportRange);
            if (leaderSupported) supportCount++;
            int count = Physics.OverlapSphereNonAlloc(transform.position, ConfidencePolicy.SenseRange,
                nearby, characterMask, QueryTriggerInteraction.Ignore);
            bool complete = count < nearby.Length;
            int actorCount = 0;
            try
            {
                for (int i = 0; i < count && i < nearby.Length; i++)
                {
                    Character candidate = nearby[i] == null ? null : nearby[i].GetComponentInParent<Character>();
                    if (candidate == null || candidate == body || candidate == leader || candidate.IsDead()) continue;
                    bool duplicate = false;
                    for (int n = 0; n < actorCount; n++) if (actors[n] == candidate) { duplicate = true; break; }
                    if (duplicate) continue;
                    if (actorCount >= actors.Length) { complete = false; continue; }
                    actors[actorCount++] = candidate;
                    if (SameParty(candidate))
                    {
                        if (Perceive(candidate, supportRange)) supportCount++;
                    }
                    else
                    {
                        var hostile = candidate.GetComponent<MonsterAI>();
                        if (hostile == null || !hostile.IsAlerted() || candidate.IsPlayer() || candidate.IsTamed()) continue;
                        Character victim = hostile.GetTargetCreature();
                        if ((victim == body || SameParty(victim)) && Perceive(candidate, ConfidencePolicy.SenseRange)) threatCount++;
                    }
                }
            }
            finally { Array.Clear(nearby, 0, nearby.Length); Array.Clear(actors, 0, actors.Length); }
            pressure = ConfidencePolicy.ObservedPressure(threatCount, supportCount);
            if (supportCount > 0) return PartySupportObservation.Perceived;
            // Unloaded/unknown leader or incomplete local search cannot certify
            // separation. A dead loaded leader supplies no living support.
            return complete && (leaderState == ConfidenceLeaderState.LoadedAlive || leaderState == ConfidenceLeaderState.LoadedDead)
                ? PartySupportObservation.Absent : PartySupportObservation.Unknown;
        }

        bool Perceive(Character candidate, float range)
        {
            var candidateView = candidate.GetComponent<ZNetView>();
            if (candidateView == null || !candidateView.IsValid() || candidate.IsDead()) return false;
            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            return ConfidencePolicy.Finite(distance) && distance <= range &&
                (ai.CanSeeTarget(candidate) || ai.CanHearTarget(candidate));
        }

        bool SameParty(Character candidate)
        {
            if (candidate == null || candidate.IsDead()) return false;
            var candidateView = candidate.GetComponent<ZNetView>();
            if (candidateView == null || !candidateView.IsValid()) return false;
            var player = candidate as Player;
            if (player != null) return player.GetPlayerID() == party;
            // Saved identity is readable on remote peers; never require their
            // local owner-only initialization flag or claim their authority.
            var candidateIdentity = candidate.GetComponent<HumanCompositionIdentity>();
            if (candidate.GetComponent<HumanCompanion>() == null || candidateIdentity == null || !candidateIdentity.IsReady) return false;
            var candidatePersonal = candidate.GetComponent<HumanIdentity>(); HumanProfile candidateProfile;
            if (candidatePersonal != null)
            { if (candidatePersonal.Read(out candidateProfile) == HumanProfileState.Refused) return false; }
            else if (candidateView.GetZDO().GetString(HumanProfile.RecordKey, "").Length != 0) return false;
            CompanionOrderRecord record;
            return CompanionPolicy.TryRead(candidateView.GetZDO().GetString(CompanionPolicy.RecordKey, ""), out record) && record.OwnerId == party;
        }

        void Persist()
        {
            if (!dirty || state == null || view == null || !view.IsValid() || !view.IsOwner()) return;
            HumanProfile profile;
            if (ReadProfile(out profile) == HumanProfileState.Refused) return;
            view.GetZDO().Set(RecordKey, state.Save()); dirty = false; sinceSaved = 0;
        }

        public bool TryGetSnapshot(out ConfidenceSnapshot snapshot)
        {
            snapshot = new ConfidenceSnapshot(); Cache();
            if (faulted || !isActiveAndEnabled || view == null || !view.IsValid() || body == null || body is Player ||
                body.IsDead() || identity == null || !identity.IsReady) return false;
            HumanProfile profile;
            if (ReadProfile(out profile) == HumanProfileState.Refused) return false;
            if (view.IsOwner())
            {
                if (!owned || state == null || appearance == null || !appearance.Initialized) return false;
                snapshot = new ConfidenceSnapshot(state, leaderState, supportCount, threatCount, pressure); return true;
            }
            // Reading a snapshot never initializes, refills, scans or writes.
            ForgetAuthority(); ConfidencePolicy remote;
            if (!ConfidencePolicy.TryRestore(view.GetZDO().GetString(RecordKey, ""), out remote)) return false;
            snapshot = new ConfidenceSnapshot(remote, ConfidenceLeaderState.Unknown, 0, 0, 0); return true;
        }

        public string Describe()
        {
            ConfidenceSnapshot snapshot;
            return TryGetSnapshot(out snapshot) ? String.Format(CultureInfo.InvariantCulture,
                "Confidence {0:0.0}/100; courage {1:0.00}; {2}; local support {3}; perceived pressure {4:0.00}; native Rested unchanged",
                snapshot.Confidence01 * 100, snapshot.Courage01, snapshot.Cohesion, snapshot.PerceivedSupport, snapshot.OverwhelmingPressure01) : Status;
        }

        void Fault(string reason)
        {
            faulted = true; Status = "Confidence stopped: " + reason;
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report(Status);
        }
        void OnDisable() { try { Persist(); } finally { ForgetAuthority(); } }
        void OnDestroy() { try { Persist(); } finally { ForgetAuthority(); } }
    }
}
