using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// A1 shared party awareness, SHADOW ONLY (Brian 2026-10-30, build order 45f18c4). One per leader. It is the only
    /// broad character scanner in the awareness design, but in A1 nothing consumes it: no phase, target, movement, look,
    /// hands, combat, bomb, bow or voice reads it. It writes no native state and owns no domain; it only reads positions,
    /// visible behavior and line-of-sight rays, and publishes telemetry.
    ///
    /// Perception is companion-owned: observers are the party's companions. The leader's body position only enters the
    /// party centroid (where "toward the party" points); the leader's camera, facing and sight are never inputs.
    ///
    /// Keyed by SIDE, not by player: a side is a set of actors that watch together. Today the only wired side is a
    /// player's companion party (key = leader id). Camps of our own hostile or friendly FormidableAI NPCs use the same
    /// core, the same fairness rules and the same intent readers (ActorIntent) with their own side key; actors of
    /// another side - including other FormidableAI actors - are ordinary contacts when hostile.
    /// </summary>
    public sealed class PartyAwareness
    {
        const int CandidateCapacity = 48;
        static readonly Dictionary<long, PartyAwareness> parties = new Dictionary<long, PartyAwareness>();
        static int viewMask;
        public static IEnumerable<PartyAwareness> All { get { return parties.Values; } }

        public readonly long LeaderId;
        public readonly ContactBook Book = new ContactBook();
        public readonly GroupBook Groups = new GroupBook();
        public readonly KnowledgeBook Knowledge = new KnowledgeBook();   // A1: empty (no legitimate source wired yet)
        readonly RayScheduler rays = new RayScheduler();

        readonly NpcKernel[] observers = new NpcKernel[AwarenessTuning.ObserverCapacity];
        readonly P2[] obsPos = new P2[AwarenessTuning.ObserverCapacity];
        readonly P2[] obsFwd = new P2[AwarenessTuning.ObserverCapacity];
        readonly bool[] eligible = new bool[AwarenessTuning.ObserverCapacity];
        readonly float[] obsDist = new float[AwarenessTuning.ObserverCapacity];
        int observerCount;

        readonly Character[] candidates = new Character[CandidateCapacity];
        readonly ContactObservation[] observed = new ContactObservation[CandidateCapacity];
        readonly float[] lastRay = new float[CandidateCapacity];
        readonly int[] lastRayId = new int[CandidateCapacity];
        readonly int[] order = new int[CandidateCapacity];
        readonly int[] band = new int[CandidateCapacity];
        readonly float[] ttc = new float[CandidateCapacity];
        readonly float[] dist = new float[CandidateCapacity];
        int candidateCount;
        public int CandidatesDropped { get; private set; }

        int lastFrame = -1;
        float lastStep, nextEnumeration, nextBudget;
        P2 centroid;
        public float NoticeRange { get; private set; }
        public bool Dark { get; private set; }
        public string Weather { get; private set; }
        public string Biome { get; private set; }
        public ShadowPosture Posture { get; private set; }
        public int Faults { get; private set; }

        // Cost, measured in game: CPU of this service and managed-heap growth across its steps (coarse; GC can hide it).
        readonly Stopwatch watch = new Stopwatch();
        long ticksWindow; long allocWindow; int stepsWindow;
        public float CpuMsPerSecond { get; private set; }
        public float AllocKbPerSecond { get; private set; }
        public int StepsPerSecond { get; private set; }
        public int RaysPerSecond { get { return rays.RaysLastWindow; } }
        public int RaysDeferred { get { return rays.Deferred; } }
        public int RaysBy(int observer) { return rays.SpentLast(observer); }
        public string ObserverName(int i) { return i >= 0 && i < observerCount && observers[i] != null && observers[i].Body != null && observers[i].Body.Human != null ? observers[i].Body.Human.m_name : ""; }
        public int ObserverCount { get { return observerCount; } }

        // Bounded event queues read by the telemetry plugin (sequence numbers, never growing).
        public readonly GroupSnapshot[] GroupRing = new GroupSnapshot[64];
        public long GroupSequence { get; private set; }
        public long BudgetSequence { get; private set; }

        PartyAwareness(long leaderId) { LeaderId = leaderId; Weather = ""; Biome = ""; }

        /// <summary>Called by each companion kernel's Update on the owner. One step per frame per party.</summary>
        internal static void Tick(NpcKernel kernel)
        {
            if (!CorePlugin.AwarenessShadow || kernel == null) return;
            NpcLeaderBinding binding = kernel.GetComponent<NpcLeaderBinding>();
            if (binding == null || binding.LeaderId == 0L) return;
            PartyAwareness party;
            if (!parties.TryGetValue(binding.LeaderId, out party))
            {
                if (parties.Count >= 8) return;     // bounded: one per leader
                party = new PartyAwareness(binding.LeaderId);
                parties[binding.LeaderId] = party;
            }
            if (party.lastFrame == Time.frameCount) return;
            party.lastFrame = Time.frameCount;
            try { party.Step(kernel.Frame.Senses.Leader); }
            catch (Exception error)
            {
                party.Faults++;
                if (party.Faults <= 5 || party.Faults % 100 == 0) CorePlugin.Warn("awareness shadow fault (read-only, no effect on play): " + error.Message);
            }
        }

        void Step(Player leader)
        {
            float now = Time.time;
            float dt = lastStep <= 0f ? 0f : Mathf.Min(now - lastStep, .5f);
            lastStep = now;
            long mem0 = GC.GetTotalMemory(false);
            watch.Reset(); watch.Start();

            rays.Refill(now, dt);
            if (now >= nextEnumeration)
            {
                nextEnumeration = now + AwarenessTuning.EnumerationInterval;
                Enumerate(leader, now);
            }
            CastDue(now);
            Book.Step(now, dt, centroid);
            Groups.Build(Book, centroid, Knowledge);
            UpdatePosture(leader);
            RecordGroups(now);

            watch.Stop();
            long grew = GC.GetTotalMemory(false) - mem0;
            ticksWindow += watch.ElapsedTicks; if (grew > 0) allocWindow += grew; stepsWindow++;
            if (now >= nextBudget)
            {
                float span = nextBudget <= 0f ? 1f : 5f;
                CpuMsPerSecond = (float)(ticksWindow * 1000.0 / Stopwatch.Frequency) / span;
                AllocKbPerSecond = allocWindow / 1024f / span;
                StepsPerSecond = (int)(stepsWindow / span);
                ticksWindow = allocWindow = 0; stepsWindow = 0;
                nextBudget = now + 5f;
                BudgetSequence++;
            }
        }

        void Enumerate(Player leader, float now)
        {
            observerCount = 0;
            P2 sum = new P2(0f, 0f); int n = 0;
            foreach (NpcKernel k in NpcKernel.All)
            {
                if (k == null || k.Body == null || k.Body.Human == null || k.Body.Human.IsDead() || !k.Body.IsOwner) continue;
                NpcLeaderBinding b = k.GetComponent<NpcLeaderBinding>();
                if (b == null || b.LeaderId != LeaderId) continue;
                Vector3 p = k.Body.Human.transform.position;
                sum = sum + new P2(p.x, p.z); n++;
                if (observerCount >= observers.Length) continue;
                Vector3 f = k.Body.Human.transform.forward;
                observers[observerCount] = k; obsPos[observerCount] = new P2(p.x, p.z); obsFwd[observerCount] = new P2(f.x, f.z);
                observerCount++;
            }
            if (leader != null) { Vector3 lp = leader.transform.position; sum = sum + new P2(lp.x, lp.z); n++; }
            centroid = n == 0 ? new P2(0f, 0f) : sum * (1f / n);
            if (observerCount == 0) { candidateCount = 0; return; }

            Vector3 at = observers[0].Body.Human.transform.position;
            Dark = EnvMan.instance != null && (EnvMan.IsNight() || !EnvMan.IsDaylight());
            Weather = EnvMan.instance == null || EnvMan.instance.GetCurrentEnvironment() == null ? "" : EnvMan.instance.GetCurrentEnvironment().m_name;
            Biome = Heightmap.FindBiome(at).ToString();
            NoticeRange = Visibility.NoticeRange(Visibility.EnvironmentFactor(Dark), 1f, 1f);
            float reach = NoticeRange * AwarenessTuning.SalienceMax;

            int count = 0;
            Humanoid first = observers[0].Body.Human;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c.IsDead() || c.IsPlayer() || OwnSide(c)) continue;
                if (!BaseAI.IsEnemy(first, c)) continue;
                Vector3 cp = c.transform.position;
                P2 c2 = new P2(cp.x, cp.z);
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < observerCount; i++) nearest = Mathf.Min(nearest, P2.Distance(obsPos[i], c2));
                if (nearest > reach) continue;
                if (count >= CandidateCapacity) { CandidatesDropped++; continue; }
                candidates[count] = c;
                observed[count] = Observe(c, leader, now);
                count++;
            }
            candidateCount = count;

            // Hearing and contact (no rays): native hearing reads only "something there"; harm is direct evidence.
            for (int i = 0; i < candidateCount; i++)
            {
                if (observed[i].HurtParty) { Book.Perceive(observed[i], ContactSource.Contact, HurtBy(candidates[i]), now); continue; }
                for (int o = 0; o < observerCount; o++)
                {
                    BaseAI ai = observers[o].Body.Ai;
                    if (ai != null && ai.CanHearTarget(candidates[i])) { Book.Perceive(observed[i], ContactSource.Hearing, o, now); break; }
                }
            }
        }

        bool OwnSide(Character c)
        {
            NpcKernel k = c.GetComponent<NpcKernel>();
            if (k == null) return false;
            NpcLeaderBinding b = k.GetComponent<NpcLeaderBinding>();
            return b != null && b.LeaderId == LeaderId;
        }

        int HurtBy(Character c)
        {
            for (int o = 0; o < observerCount; o++)
                if (ReferenceEquals(observers[o].RecentAttacker(AwarenessTuning.HurtMemory), c)) return o;
            return -1;
        }

        // Members a contact may target (companions and the leader's body). The leader is a member, never a sensor.
        readonly P2[] memberPos = new P2[AwarenessTuning.ObserverCapacity + 1];
        public int ReaderFormidable { get; private set; }
        public int ReaderMonster { get; private set; }
        public int ReaderBaseAI { get; private set; }
        public int ReaderVisible { get; private set; }

        ContactObservation Observe(Character c, Player leader, float now)
        {
            Vector3 p = c.transform.position, v = c.GetVelocity();
            BaseAI ai = c.GetBaseAI();
            Character target; bool alerted;
            IntentReader reader = ActorIntent.Read(c, out target, out alerted);
            if (reader == IntentReader.FormidableActor) ReaderFormidable++;
            else if (reader == IntentReader.NativeMonsterAI) ReaderMonster++;
            else if (reader == IntentReader.NativeBaseAI) ReaderBaseAI++;
            else ReaderVisible++;
            P2 pos = new P2(p.x, p.z), vel = new P2(v.x, v.z);
            Vector3 fw = c.transform.forward; P2 fwd = new P2(fw.x, fw.z);
            int member = -1;
            if (target != null)
            {
                if (leader != null && ReferenceEquals(target, leader)) member = AwarenessTuning.LeaderTarget;
                else for (int o = 0; o < observerCount; o++) if (ReferenceEquals(observers[o].Body.Human, target)) { member = o; break; }
            }
            bool perceptible = false;
            if (member >= 0)
            {
                Vector3 to = target.transform.position - p; to.y = 0f;
                Vector3 f = fw; f.y = 0f;
                bool facing = to.sqrMagnitude > .01f && f.sqrMagnitude > .01f && Vector3.Angle(f, to) <= AwarenessTuning.TargetFacing;
                Vector3 hv = new Vector3(v.x, 0f, v.z);
                bool charging = to.sqrMagnitude > .01f && Vector3.Dot(hv, to.normalized) >= AwarenessTuning.ChargeSpeed;
                perceptible = facing || charging || c.InAttack();
            }
            else
            {
                // No reader names a party target: judge by what is visible, the same cues a player reads.
                int n = 0;
                for (int o = 0; o < observerCount; o++) memberPos[n++] = obsPos[o];
                int leaderSlot = -1;
                if (leader != null) { Vector3 lp = leader.transform.position; leaderSlot = n; memberPos[n++] = new P2(lp.x, lp.z); }
                int seen = VisibleIntent.Target(pos, fwd, vel, c.InAttack(), memberPos, n);
                if (seen >= 0) { member = seen == leaderSlot ? AwarenessTuning.LeaderTarget : seen; perceptible = true; }
                if (reader == IntentReader.VisibleOnly || reader == IntentReader.NativeBaseAI) alerted = alerted || VisibleIntent.Alerted(vel, seen);
            }
            return new ContactObservation
            {
                Id = c.GetInstanceID(), Family = c.m_name,
                Wildlife = c.GetFaction() == Character.Faction.AnimalsVeg, Sleeping = ai != null && ai.IsSleeping(), Boss = c.IsBoss(),
                Position = pos, Height = p.y, Velocity = vel, Radius = c.GetRadius(),
                NativeAlerted = alerted,
                NativeTargetMember = member, TargetPerceptible = perceptible,
                HurtParty = HurtBy(c) >= 0
            };
        }

        void CastDue(float now)
        {
            if (observerCount == 0 || candidateCount == 0) return;
            if (viewMask == 0) viewMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "viewblock", "vehicle");
            int due = 0;
            for (int i = 0; i < candidateCount; i++)
            {
                Character c = candidates[i];
                if (c == null || c.IsDead()) continue;
                int id = observed[i].Id;
                ContactRecord r = Book.Find(id);
                P2 pos = observed[i].Position;
                float d = P2.Distance(centroid, pos);
                bool urgent = r != null && (r.State == ContactState.Committed || (!float.IsInfinity(r.TimeToContact) && r.ClosingSpeed >= AwarenessTuning.CommitClosingSpeed));
                int b = RayScheduler.Band(urgent, d);
                float last = r != null ? Mathf.Max(r.LastRayAt, lastRayId[i] == id ? lastRay[i] : float.NegativeInfinity) : lastRayId[i] == id ? lastRay[i] : float.NegativeInfinity;
                if (now - last < RayScheduler.RefreshInterval(b)) continue;
                order[due] = i; band[i] = b; ttc[i] = r == null ? float.PositiveInfinity : r.TimeToContact; dist[i] = d;
                due++;
            }
            RayScheduler.Order(order, due, band, ttc, dist);
            for (int k = 0; k < due; k++)
            {
                int i = order[k];
                Character c = candidates[i];
                P2 pos = observed[i].Position;
                float range = Visibility.NoticeRange(Visibility.EnvironmentFactor(Dark), Visibility.Salience(observed[i].Radius, observed[i].Velocity.Length), 1f);
                bool any = false;
                for (int o = 0; o < observerCount; o++)
                {
                    eligible[o] = Visibility.InFieldOfRegard(obsPos[o], obsFwd[o], pos, range);
                    obsDist[o] = P2.Distance(obsPos[o], pos);
                    any |= eligible[o];
                }
                lastRayId[i] = observed[i].Id; lastRay[i] = now;
                ContactRecord rec = Book.Find(observed[i].Id); if (rec != null) rec.LastRayAt = now;
                if (!any) continue;                                  // nobody is placed to see it: no ray spent
                int who = rays.ChooseObserver(eligible, obsDist, observerCount);
                Humanoid eyeOf = observers[who].Body.Human;
                Vector3 eye = eyeOf.m_eye != null ? eyeOf.m_eye.position : eyeOf.transform.position + Vector3.up * 1.6f;
                if (!rays.TryTake(who)) { lastRay[i] = float.NegativeInfinity; if (rec != null) rec.LastRayAt = float.NegativeInfinity; break; }   // budget spent: retry next frame, urgent first
                bool seen = !Physics.Linecast(eye, c.GetCenterPoint(), viewMask, QueryTriggerInteraction.Ignore);
                if (!seen && rays.TryTake(who))
                {
                    Vector3 top = c.m_eye != null ? c.m_eye.position : c.GetCenterPoint() + Vector3.up * .6f;
                    seen = !Physics.Linecast(eye, top, viewMask, QueryTriggerInteraction.Ignore);
                }
                if (seen) Book.Perceive(observed[i], ContactSource.Sight, who, now);
            }
        }

        void UpdatePosture(Player leader)
        {
            ContactState strongest = ContactState.None; bool anyNoticed = false;
            foreach (ContactRecord r in Book.Records)
            {
                if (!r.Used || r.State == ContactState.None || r.State == ContactState.Lost) continue;
                anyNoticed = true;
                if (r.Wildlife && r.State != ContactState.Committed) continue;     // wildlife never readies the party
                if (r.State > strongest) strongest = r.State;
            }
            Posture = StealthDoctrine.Posture(strongest, anyNoticed, leader != null && leader.IsCrouching());
        }

        void RecordGroups(float now)
        {
            for (int g = 0; g < Groups.Count; g++)
            {
                GroupRecord gr = Groups.Groups[g];
                GroupSnapshot last = FindLastGroup(gr.Id);
                bool changed = last.Sequence < 0 || last.Members != gr.Members || last.Committed != gr.IsCommitted || now - last.Time >= 2f;
                if (!changed) continue;
                GroupRing[GroupSequence % GroupRing.Length] = new GroupSnapshot
                {
                    Sequence = GroupSequence, Time = now, Id = gr.Id, Members = gr.Members, Alerted = gr.Alerted, CommittedMembers = gr.Committed,
                    Committed = gr.IsCommitted, Distance = gr.Distance, Closing = gr.ClosingSpeed, TimeToContact = gr.TimeToContact, Radius = gr.Radius,
                    SusceptibleKnown = gr.SusceptibleKnown, ImmuneKnown = gr.ImmuneKnown, Unknown = gr.Unknown
                };
                GroupSequence++;
            }
        }

        GroupSnapshot FindLastGroup(int id)
        {
            long start = Math.Max(0L, GroupSequence - GroupRing.Length);
            for (long s = GroupSequence - 1; s >= start; s--)
                if (GroupRing[s % GroupRing.Length].Id == id) return GroupRing[s % GroupRing.Length];
            return new GroupSnapshot { Sequence = -1 };
        }
    }

    public struct GroupSnapshot
    {
        public long Sequence; public float Time;
        public int Id, Members, Alerted, CommittedMembers; public bool Committed;
        public float Distance, Closing, TimeToContact, Radius;
        public int SusceptibleKnown, ImmuneKnown, Unknown;
    }
}
