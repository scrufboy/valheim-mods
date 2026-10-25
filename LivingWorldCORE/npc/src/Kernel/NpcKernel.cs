using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// One per NPC. Samples the world once per decision tick, asks the phase arbiter
    /// what the NPC is doing, grants domains to behaviors in priority order, and runs
    /// exit transactions for anything that lost its domains before new owners act.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class NpcKernel : MonoBehaviour
    {
        static readonly HashSet<NpcKernel> Live = new HashSet<NpcKernel>();
        public static IEnumerable<NpcKernel> All { get { return Live; } }

        readonly NpcFrame frame = new NpcFrame();
        readonly PhaseArbiter arbiter = new PhaseArbiter();
        readonly List<NpcBehavior> behaviors = new List<NpcBehavior>();
        readonly Dictionary<NpcDomain, NpcBehavior> owners = new Dictionary<NpcDomain, NpcBehavior>();
        bool[] wants, granted, wasGranted;
        NpcDomain[] claims;

        NpcBody body;
        NpcAppearance appearance;
        NpcLeaderBinding binding;
        float nextDecision, nextHostileScan, nextLightScan, nextFaultLog, lastDamageTime = float.NegativeInfinity;
        Character lastAttacker;
        Character trackedTarget;
        NpcPhase lastPhase = NpcPhase.Initializing;
        bool wasOwner;

        public NpcBody Body { get { return body; } }
        public NpcFrame Frame { get { return frame; } }
        public TargetSelector Targeting { get; private set; }

        /// <summary>The enemy that hurt this NPC within the window, if still alive.</summary>
        public Character RecentAttacker(float seconds)
        {
            if (lastAttacker == null || lastAttacker.IsDead() || Time.time - lastDamageTime > seconds) return null;
            return lastAttacker;
        }

        public void ReportFault(string where, Exception error)
        {
            Faults++;
            CorePlugin.Warn(where + " fault: " + error.Message);
        }
        public PhaseArbiter Arbiter { get { return arbiter; } }
        public IList<NpcBehavior> Behaviors { get { return behaviors; } }
        public int DomainViolations { get; private set; }
        public int Faults { get; private set; }
        public int PhaseChanges { get; private set; }
        public string LastViolation { get; private set; }
        public bool Died { get; private set; }
        public string LastAttackerName { get; private set; }

        /// <summary>Raised once when a companion leaves the world: "death" or "unloaded/destroyed".</summary>
        public static event Action<NpcKernel, string> Removed;
        public static event Action<NpcKernel, float, Character> Damaged;
        public float LastHealth { get; private set; }

        void Awake()
        {
            string missing = NativeApi.MissingMembers();
            if (missing.Length > 0)
                throw new MissingMemberException("Valheim changed members Living World depends on: " + missing);
            body = new NpcBody(this, gameObject);
            Targeting = new TargetSelector(this);
            appearance = GetComponent<NpcAppearance>();
            binding = GetComponent<NpcLeaderBinding>();

            // Priority order. Earlier entries win contested domains.
            Add(new FallBackBehavior());
            Add(new NativeCombatBehavior());
            Add(new CombatRegroupBehavior());
            Add(new StealthBehavior());
            Add(new SitWithLeaderBehavior());
            Add(new SettledGuardBehavior());
            Add(new AwarenessLookBehavior());
            Add(new TraversalBehavior());
            Add(new HandsBehavior());

            wants = new bool[behaviors.Count];
            granted = new bool[behaviors.Count];
            wasGranted = new bool[behaviors.Count];
            claims = new NpcDomain[behaviors.Count];
            for (int i = 0; i < behaviors.Count; i++) claims[i] = behaviors[i].Claims;

            if (body.Human != null)
            {
                body.Human.m_onDamaged += OnDamaged;
                body.Human.m_onDeath += OnDeath;
            }
        }

        void OnDeath()
        {
            Died = true;
        }

        void Add(NpcBehavior behavior)
        {
            behavior.Attach(body);
            behaviors.Add(behavior);
        }

        void OnEnable() { Live.Add(this); }
        void OnDisable() { ReleaseEverything("disabled"); Live.Remove(this); }
        void OnDestroy()
        {
            ReleaseEverything("destroyed");
            Live.Remove(this);
            bool dead = Died || (body != null && body.Human != null && body.Human.IsDead());
            Action<NpcKernel, string> removed = Removed;
            if (removed != null)
            {
                try { removed(this, dead ? "death" : "unloaded/destroyed"); }
                catch (Exception error) { CorePlugin.Warn("removal observer failed: " + error.Message); }
            }
            if (body != null && body.Human != null)
            {
                body.Human.m_onDamaged -= OnDamaged;
                body.Human.m_onDeath -= OnDeath;
            }
        }

        void OnDamaged(float damage, Character attacker)
        {
            lastDamageTime = Time.time;
            try { NpcVoice.OnHurt(this, damage); } catch (Exception error) { CorePlugin.Warn("voice hurt: " + error.Message); }
            if (body.Human != null) LastHealth = body.Human.GetHealth();
            Action<NpcKernel, float, Character> damaged = Damaged;
            if (damaged != null)
            {
                try { damaged(this, damage, attacker); }
                catch (Exception error) { CorePlugin.Warn("damage observer failed: " + error.Message); }
            }
            if (attacker != null)
            {
                lastAttacker = attacker;
                LastAttackerName = attacker.GetHoverName();
            }
        }

        internal bool Holds(NpcBehavior behavior, NpcDomain domain)
        {
            NpcBehavior owner;
            return behavior != null && owners.TryGetValue(domain, out owner) && ReferenceEquals(owner, behavior);
        }

        internal void RecordViolation(NpcBehavior behavior, NpcDomain domain)
        {
            DomainViolations++;
            LastViolation = (behavior == null ? "unknown" : behavior.Name) + " -> " + domain + " at " + frame.Phase;
        }

        public string OwnerName(NpcDomain domain)
        {
            NpcBehavior owner;
            return owners.TryGetValue(domain, out owner) ? owner.Name : (frame.Phase == NpcPhase.Initializing ? "-" : "native");
        }

        void Update()
        {
            if (body == null || body.Human == null || body.Ai == null) return;
            bool owner = body.IsOwner;
            if (!owner)
            {
                if (wasOwner) ReleaseEverything("lost network ownership");
                wasOwner = false;
                return;
            }
            wasOwner = true;
            if (Time.time < nextDecision) return;
            float dt = nextDecision <= 0f ? Tuning.DecisionInterval : Tuning.DecisionInterval + (Time.time - nextDecision);
            nextDecision = Time.time + Tuning.DecisionInterval;
            try
            {
                Decide(Mathf.Min(dt, .5f));
            }
            catch (Exception error)
            {
                Faults++;
                if (Time.time >= nextFaultLog)
                {
                    nextFaultLog = Time.time + 5f;
                    CorePlugin.Warn(name + " kernel fault (state retained): " + error.Message);
                }
            }
        }

        void LateUpdate()
        {
            if (body == null || body.Human == null || !body.IsOwner) return;
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (!wasGranted[i]) continue;
                try { behaviors[i].LateFrame(frame); }
                catch (Exception error)
                {
                    Faults++;
                    if (Time.time >= nextFaultLog)
                    {
                        nextFaultLog = Time.time + 5f;
                        CorePlugin.Warn(name + " " + behaviors[i].Name + " frame fault: " + error.Message);
                    }
                }
            }
        }

        void Decide(float dt)
        {
            frame.Now = Time.time;
            frame.Dt = dt;
            Sense(frame.Senses);

            NpcSenses s = frame.Senses;
            NpcPhase phase = arbiter.Step(new PhaseInputs
            {
                AppearanceReady = appearance != null && appearance.Ready,
                HasLeader = s.HasLeader,
                LeaderArmed = s.LeaderArmed,
                NativeAlert = s.NativeAlert,
                HasTarget = s.Target != null || s.HasStaticTarget,
                DamagedRecently = s.SecondsSinceDamaged <= Tuning.DamageMemorySeconds && AttackerStillDangerous(),
                AlertedHostileNear = s.NearestAlertedHostile != null,
                LocalThreat = LocalThreat(s),
                LeaderDistance = s.LeaderDistance,
                NearestPartyDistance = s.NearestPartyDistance,
                FormationDistance = frame.Board.FormationDistance
            }, dt);
            frame.Phase = phase;
            NpcVoice.Tick(this);
            frame.SecondsSinceEncounter = arbiter.SecondsSinceEncounter;
            bool fighting = phase == NpcPhase.Combat || phase == NpcPhase.CombatClear;
            frame.FallingBack = fallBack.Step(Time.time, fighting, body.Human.GetHealthPercentage(), nearestEngagedDistance);
            frame.FallBackFrom = nearestEngagedPosition;
            if (phase != lastPhase)
            {
                PhaseChanges++;
                lastPhase = phase;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                bool want = false;
                try { want = behaviors[i].Wants(frame); }
                catch (Exception error) { Faults++; behaviors[i].Status = "wants fault: " + error.Message; CorePlugin.Warn(behaviors[i].Name + " wants fault: " + error); }
                wants[i] = want;
            }
            DomainScheduler.Resolve(wants, claims, granted);

            // Exit transactions first, lowest priority first, so a releasing
            // owner finishes cleanly before anyone else touches its domain.
            for (int i = behaviors.Count - 1; i >= 0; i--)
            {
                if (!wasGranted[i] || granted[i]) continue;
                RunExit(i, "phase " + phase);
            }
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (!granted[i] || wasGranted[i]) continue;
                Grant(i);
                try { behaviors[i].Enter(frame); }
                catch (Exception error) { Faults++; behaviors[i].Status = "enter fault: " + error.Message; CorePlugin.Warn(behaviors[i].Name + " enter fault: " + error); }
            }
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (!granted[i]) continue;
                try { behaviors[i].Tick(frame); }
                catch (Exception error) { Faults++; behaviors[i].Status = "tick fault: " + error.Message; CorePlugin.Warn(behaviors[i].Name + " tick fault: " + error); }
            }
        }

        void Grant(int i)
        {
            wasGranted[i] = true;
            behaviors[i].Active = true;
            foreach (NpcDomain domain in Domains(claims[i])) owners[domain] = behaviors[i];
        }

        void RunExit(int i, string reason)
        {
            // Not the network owner any more (died, unloaded, or the game handed control elsewhere):
            // there is nothing of ours to tidy, and every native write would be refused.
            if (body != null && body.IsOwner)
                try { behaviors[i].Exit(frame, reason); }
                catch (Exception error) { Faults++; behaviors[i].Status = "exit fault: " + error.Message; CorePlugin.Warn(behaviors[i].Name + " exit fault: " + error); }
            else behaviors[i].Status = "released (not owner): " + reason;
            foreach (NpcDomain domain in Domains(claims[i]))
            {
                NpcBehavior owner;
                if (owners.TryGetValue(domain, out owner) && ReferenceEquals(owner, behaviors[i])) owners.Remove(domain);
            }
            wasGranted[i] = false;
            behaviors[i].Active = false;
        }

        void ReleaseEverything(string reason)
        {
            if (behaviors.Count == 0 || wasGranted == null) return;
            for (int i = behaviors.Count - 1; i >= 0; i--) if (wasGranted[i]) RunExit(i, reason);
            if (body != null) body.ReleaseAllUnchecked();
        }

        static IEnumerable<NpcDomain> Domains(NpcDomain set)
        {
            if ((set & NpcDomain.Movement) != 0) yield return NpcDomain.Movement;
            if ((set & NpcDomain.Hands) != 0) yield return NpcDomain.Hands;
            if ((set & NpcDomain.Look) != 0) yield return NpcDomain.Look;
            if ((set & NpcDomain.Posture) != 0) yield return NpcDomain.Posture;
        }

        // Only a living enemy counts. Friendly fire from the leader never starts a fight.
        bool HostileAttacker(float radius)
        {
            return lastAttacker != null && !lastAttacker.IsDead() && BaseAI.IsEnemy(body.Human, lastAttacker) &&
                Vector3.Distance(lastAttacker.transform.position, transform.position) <= radius;
        }

        bool AttackerStillDangerous() { return HostileAttacker(Tuning.HostileScanRadius); }

        float nextPinCheck;
        float nearestEngagedDistance = float.PositiveInfinity;
        Vector3 nearestEngagedPosition;
        readonly FallBackPolicy fallBack = new FallBackPolicy();
        public FallBackPolicy FallBack { get { return fallBack; } }
        public float NearestEngagedDistance { get { return nearestEngagedDistance; } }

        void ScanEngaged()
        {
            if (Time.time < nextPinCheck) return;
            nextPinCheck = Time.time + .25f;
            float d = float.PositiveInfinity;
            Character c = Targeting == null ? null : Targeting.NearestEngaged(out d);
            nearestEngagedDistance = c == null ? float.PositiveInfinity : d;
            if (c != null) nearestEngagedPosition = c.transform.position;
        }

        /// <summary>An engaged enemy (same rule as target choice) within the pin radius.</summary>
        bool LocalThreat(NpcSenses s)
        {
            ScanEngaged();
            return nearestEngagedDistance <= Tuning.EngagedPinRadius;
        }

        void Sense(NpcSenses s)
        {
            Player leader = null;
            s.HasLeader = binding != null && binding.TryResolveLeader(out leader) && leader != null;
            if (!s.HasLeader) leader = null;
            s.Leader = leader;
            Humanoid human = body.Human;
            if (leader != null)
            {
                Vector3 v = leader.GetVelocity();
                s.LeaderSpeed = new Vector2(v.x, v.z).magnitude;
                s.LeaderMoving = s.LeaderSpeed > .35f;
                s.LeaderRunning = s.LeaderMoving && leader.IsRunning();
                s.LeaderCrouching = leader.IsCrouching();
                s.LeaderSitting = leader.IsSitting();
                s.LeaderArmed = VisibleWeapon(NativeApi.RightItem.GetValue(leader) as ItemDrop.ItemData) ||
                    VisibleWeapon(NativeApi.LeftItem.GetValue(leader) as ItemDrop.ItemData);
                s.LeaderBusy = leader.GetCurrentCraftingStation() != null || leader.InPlaceMode() ||
                    InventoryGui.IsVisible() || StoreGui.IsVisible() || Minimap.IsOpen();
                s.LeaderDistance = Vector3.Distance(transform.position, leader.transform.position);
            }
            else
            {
                s.LeaderSpeed = 0f; s.LeaderMoving = s.LeaderRunning = s.LeaderCrouching = s.LeaderSitting = false;
                s.LeaderArmed = s.LeaderBusy = false; s.LeaderDistance = -1f;
            }

            Character target = body.Ai.GetTargetCreature();
            s.Target = target;
            s.HasStaticTarget = NativeApi.TargetStatic.GetValue(body.Ai) as StaticTarget != null;
            s.NativeAlert = body.Ai.IsAlerted();
            s.SecondsSinceDamaged = Time.time - lastDamageTime;
            s.LastAttacker = lastAttacker;
            s.Swimming = human.IsSwimming();
            Vector3 own = human.GetVelocity();
            s.OwnSpeed = new Vector2(own.x, own.z).magnitude;
            s.NearestPartyDistance = NearestPartyDistance(leader);

            if (Time.time >= nextHostileScan)
            {
                nextHostileScan = Time.time + Tuning.HostileScanInterval;
                ScanHostiles(s);
            }
            if (Time.time >= nextLightScan)
            {
                nextLightScan = Time.time + 1f;
                s.Dark = EnvMan.instance != null && (EnvMan.IsNight() || !EnvMan.IsDaylight());
                s.NearestBuiltLight = NearestBuiltLight();
                s.NearBuiltLight = BuiltLight.Near(s.NearBuiltLight, s.NearestBuiltLight);
            }
            if (target != trackedTarget) trackedTarget = target;
        }

        void ScanHostiles(NpcSenses s)
        {
            Character nearest = null;
            float best = Tuning.HostileScanRadius;
            Vector3 position = transform.position;
            foreach (Character other in Character.GetAllCharacters())
            {
                if (other == null || other == body.Human || other.IsDead()) continue;
                float distance = Vector3.Distance(position, other.transform.position);
                if (distance > best) continue;
                if (!BaseAI.IsEnemy(body.Human, other)) continue;
                BaseAI ai = other.GetBaseAI();
                if (ai == null || !ai.IsAlerted()) continue;
                best = distance;
                nearest = other;
            }
            s.NearestAlertedHostile = nearest;
            s.NearestAlertedHostileDistance = nearest == null ? float.PositiveInfinity : best;
        }

        readonly Collider[] lightProbe = new Collider[96];
        float NearestBuiltLight()
        {
            float nearest = float.PositiveInfinity;
            int count = Physics.OverlapSphereNonAlloc(transform.position, Tuning.BuiltLightExit, lightProbe);
            for (int i = 0; i < count; i++)
            {
                Collider collider = lightProbe[i];
                lightProbe[i] = null;
                if (collider == null) continue;
                Piece piece = collider.GetComponentInParent<Piece>();
                if (piece == null) continue;
                foreach (Light light in piece.GetComponentsInChildren<Light>())
                {
                    if (!light.enabled || !light.gameObject.activeInHierarchy || light.intensity <= .25f) continue;
                    float distance = Vector3.Distance(light.transform.position, transform.position);
                    if (distance < nearest) nearest = distance;
                }
            }
            return nearest;
        }

        float NearestPartyDistance(Player leader)
        {
            if (leader == null) return -1f;
            float nearest = float.PositiveInfinity;
            long leaderId = leader.GetPlayerID();
            foreach (NpcKernel other in Live)
            {
                if (other == null || other == this || !other.isActiveAndEnabled || other.binding == null) continue;
                if (other.binding.LeaderId != leaderId) continue;
                nearest = Mathf.Min(nearest, Vector3.Distance(transform.position, other.transform.position));
            }
            return float.IsPositiveInfinity(nearest) ? -1f : nearest;
        }

        static bool VisibleWeapon(ItemDrop.ItemData item)
        {
            // GetCurrentWeapon falls back to invisible fists; only a real hand item counts.
            return item != null && item.m_dropPrefab != null && item.IsWeapon() &&
                item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch;
        }

        public string Describe()
        {
            var parts = new List<string>();
            parts.Add(frame.Phase + " (" + arbiter.Reason + ")");
            parts.Add("move=" + OwnerName(NpcDomain.Movement) + " hands=" + OwnerName(NpcDomain.Hands) +
                " look=" + OwnerName(NpcDomain.Look) + " pose=" + OwnerName(NpcDomain.Posture));
            foreach (NpcBehavior behavior in behaviors)
                if (behavior.Active) parts.Add(behavior.Name + ": " + behavior.Status);
            if (DomainViolations > 0) parts.Add("VIOLATIONS=" + DomainViolations + " last=" + LastViolation);
            if (Faults > 0) parts.Add("faults=" + Faults);
            return String.Join("; ", parts.ToArray());
        }
    }
}
