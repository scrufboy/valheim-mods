using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>One member of a tactical awareness group, as supplied by its adapter.</summary>
    public struct GroupMember
    {
        public Character Actor;     // body: position, protection, centroid
        public bool Sensor;         // perceives for the group (eyes, ears); a protected member need not be one
        public BaseAI Ai;           // native hearing for sensors (may be null)
        public NpcKernel Kernel;    // our FormidableAI actor (hurt memory, own target/action), or null
        public string Name;
    }

    /// <summary>
    /// The boundary between the awareness core and whoever is being made aware (90b08e2, Brian 2026-10-30).
    /// AwarenessGroupId is a TACTICAL GROUP - who shares one encounter picture - not a faction and not an allegiance.
    /// Hostility comes from the adapter's relationship context: Valheim's native faction rules (BaseAI.IsEnemy) now,
    /// approved Living World allegiance/reputation later. Awareness keeps no friend/enemy table of its own.
    /// </summary>
    public interface IAwarenessGroupAdapter
    {
        long AwarenessGroupId { get; }
        string Kind { get; }
        int Members(GroupMember[] into);          // bounded by into.Length
        bool IsMember(Character c);
        bool IsHostile(Character c);              // relationship context, never invented by awareness
        bool StealthIntent { get; }               // the group is trying to stay hidden (changes costs, never a veto)
    }

    /// <summary>
    /// First adapter: a player's companion party. The player's id only maps to this party's group id here; the core never
    /// sees a Player. The player's body is a protected member (centroid, can be targeted) but never a sensor.
    /// </summary>
    public sealed class CompanionPartyAdapter : IAwarenessGroupAdapter
    {
        static readonly Dictionary<long, CompanionPartyAdapter> byLeader = new Dictionary<long, CompanionPartyAdapter>();
        readonly long leaderId;
        Player leader;
        Character relationBody;   // a member body whose native faction answers hostility (set in Members)

        CompanionPartyAdapter(long leaderId) { this.leaderId = leaderId; }

        internal static CompanionPartyAdapter For(NpcKernel kernel)
        {
            NpcLeaderBinding b = kernel == null ? null : kernel.GetComponent<NpcLeaderBinding>();
            if (b == null || b.LeaderId == 0L) return null;
            CompanionPartyAdapter a;
            if (!byLeader.TryGetValue(b.LeaderId, out a))
            {
                if (byLeader.Count >= 8) return null;
                a = new CompanionPartyAdapter(b.LeaderId);
                byLeader[b.LeaderId] = a;
            }
            if (kernel.Frame != null && kernel.Frame.Senses.Leader != null) a.leader = kernel.Frame.Senses.Leader;
            return a;
        }

        public long AwarenessGroupId { get { return unchecked((long)0x4C57435000000000L ^ leaderId); } }   // "LWCP" namespace
        public string Kind { get { return "companion-party"; } }
        public bool StealthIntent { get { return leader != null && leader.IsCrouching(); } }

        public int Members(GroupMember[] into)
        {
            int n = 0;
            relationBody = null;
            foreach (NpcKernel k in NpcKernel.All)
            {
                if (n >= into.Length) break;
                if (k == null || k.Body == null || k.Body.Human == null || k.Body.Human.IsDead() || !k.Body.IsOwner) continue;
                NpcLeaderBinding b = k.GetComponent<NpcLeaderBinding>();
                if (b == null || b.LeaderId != leaderId) continue;
                into[n++] = new GroupMember { Actor = k.Body.Human, Sensor = true, Ai = k.Body.Ai, Kernel = k, Name = k.Body.Human.m_name };
                if (relationBody == null) relationBody = k.Body.Human;
            }
            if (leader != null && !leader.IsDead() && n < into.Length)
                into[n++] = new GroupMember { Actor = leader, Sensor = false, Ai = null, Kernel = null, Name = "leader" };
            return n;
        }

        public bool IsMember(Character c)
        {
            if (c == null) return false;
            if (leader != null && ReferenceEquals(c, leader)) return true;
            NpcLeaderBinding b = c.GetComponent<NpcLeaderBinding>();
            return b != null && b.LeaderId == leaderId;
        }

        // Native Valheim faction relationship (BaseAI.IsEnemy), judged from a member body; later refined by allegiance.
        public bool IsHostile(Character c) { return relationBody != null && BaseAI.IsEnemy(relationBody, c); }
    }

    /// <summary>
    /// A1 tactical-group awareness, SHADOW ONLY. Group-agnostic core: everything it knows about who is aware comes from an
    /// IAwarenessGroupAdapter. Nothing in play reads it; it writes no native state and owns no domain. Perception is the
    /// group's own sensors (sight rays, native hearing, harm); non-sensor members are protected, never borrowed eyes.
    /// Players are not excluded as contacts by rule: whether any actor is hostile is the adapter's relationship call.
    /// </summary>
    public sealed class AwarenessGroup
    {
        const int CandidateCapacity = 48;
        static readonly Dictionary<long, AwarenessGroup> groups = new Dictionary<long, AwarenessGroup>();
        static int viewMask;
        public static IEnumerable<AwarenessGroup> All { get { return groups.Values; } }

        public readonly long AwarenessGroupId;
        public readonly string Kind;
        public readonly ContactBook Book = new ContactBook();
        public readonly GroupBook Groups = new GroupBook();
        public readonly KnowledgeBook Knowledge = new KnowledgeBook();   // A1: empty (no legitimate source wired yet)
        readonly RayScheduler rays = new RayScheduler();
        IAwarenessGroupAdapter adapter;

        readonly GroupMember[] members = new GroupMember[AwarenessTuning.MemberCapacity];
        readonly P2[] memberPos = new P2[AwarenessTuning.MemberCapacity];
        int memberCount;
        // Sensor view (index into members) for the ray scheduler.
        readonly int[] sensorOf = new int[AwarenessTuning.ObserverCapacity];
        readonly P2[] obsPos = new P2[AwarenessTuning.ObserverCapacity];
        readonly P2[] obsFwd = new P2[AwarenessTuning.ObserverCapacity];
        readonly bool[] eligible = new bool[AwarenessTuning.ObserverCapacity];
        readonly float[] obsDist = new float[AwarenessTuning.ObserverCapacity];
        int sensorCount;

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

        // First time each actor entered the candidate envelope (initiative milestone InEnvelope). Fixed ring.
        readonly int[] envId = new int[64]; readonly float[] envAt = new float[64]; readonly float[] envDist = new float[64]; int envNext;

        int lastFrame = -1;
        float lastStep, nextEnumeration, nextBudget;
        P2 centroid;
        public float NoticeRange { get; private set; }
        public bool Dark { get; private set; }
        public string Weather { get; private set; }
        public string Biome { get; private set; }
        public ShadowPosture Posture { get; private set; }
        public int Faults { get; private set; }
        public int ReaderFormidable { get; private set; }
        public int ReaderMonster { get; private set; }
        public int ReaderBaseAI { get; private set; }
        public int ReaderVisible { get; private set; }

        readonly Stopwatch watch = new Stopwatch();
        long ticksWindow; long allocWindow; int stepsWindow;
        public float CpuMsPerSecond { get; private set; }
        public float AllocKbPerSecond { get; private set; }
        public int StepsPerSecond { get; private set; }
        public int RaysPerSecond { get { return rays.RaysLastWindow; } }
        public int RaysDeferred { get { return rays.Deferred; } }
        public int RaysBy(int sensor) { return rays.SpentLast(sensor); }
        public int SensorCount { get { return sensorCount; } }
        public int MemberCount { get { return memberCount; } }
        public string SensorName(int s) { return s >= 0 && s < sensorCount ? members[sensorOf[s]].Name ?? "" : ""; }
        public string MemberName(int m) { return m >= 0 && m < memberCount ? members[m].Name ?? "" : ""; }

        public readonly GroupSnapshot[] GroupRing = new GroupSnapshot[64];
        public long GroupSequence { get; private set; }
        public long BudgetSequence { get; private set; }

        AwarenessGroup(IAwarenessGroupAdapter a) { AwarenessGroupId = a.AwarenessGroupId; Kind = a.Kind; Weather = ""; Biome = ""; }

        /// <summary>Drive one step per frame for this adapter's group (any member may call it; owner-side only).</summary>
        internal static void Tick(IAwarenessGroupAdapter a)
        {
            if (!CorePlugin.AwarenessShadow || a == null) return;
            AwarenessGroup g;
            if (!groups.TryGetValue(a.AwarenessGroupId, out g))
            {
                if (groups.Count >= 8) return;
                g = new AwarenessGroup(a);
                groups[a.AwarenessGroupId] = g;
            }
            if (g.lastFrame == Time.frameCount) return;
            g.lastFrame = Time.frameCount;
            g.adapter = a;
            try { g.Step(); }
            catch (Exception error)
            {
                g.Faults++;
                if (g.Faults <= 5 || g.Faults % 100 == 0) CorePlugin.Warn("awareness shadow fault (read-only, no effect on play): " + error.Message);
            }
        }

        void Step()
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
                Enumerate(now);
            }
            CastDue(now);
            Book.Step(now, dt, centroid);
            Groups.Build(Book, centroid, Knowledge);
            ObserveMemberActions(now);
            UpdatePosture();
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

        void Enumerate(float now)
        {
            memberCount = adapter.Members(members);
            sensorCount = 0;
            P2 sum = new P2(0f, 0f);
            for (int m = 0; m < memberCount; m++)
            {
                Vector3 p = members[m].Actor.transform.position;
                memberPos[m] = new P2(p.x, p.z);
                sum = sum + memberPos[m];
                if (members[m].Sensor && sensorCount < sensorOf.Length)
                {
                    Vector3 f = members[m].Actor.transform.forward;
                    sensorOf[sensorCount] = m; obsPos[sensorCount] = memberPos[m]; obsFwd[sensorCount] = new P2(f.x, f.z);
                    sensorCount++;
                }
            }
            centroid = memberCount == 0 ? new P2(0f, 0f) : sum * (1f / memberCount);
            Book.SetCentroid(centroid);
            if (sensorCount == 0) { candidateCount = 0; return; }

            Vector3 at = members[sensorOf[0]].Actor.transform.position;
            Dark = EnvMan.instance != null && (EnvMan.IsNight() || !EnvMan.IsDaylight());
            Weather = EnvMan.instance == null || EnvMan.instance.GetCurrentEnvironment() == null ? "" : EnvMan.instance.GetCurrentEnvironment().m_name;
            Biome = Heightmap.FindBiome(at).ToString();
            NoticeRange = Visibility.NoticeRange(Visibility.EnvironmentFactor(Dark), 1f, 1f);
            float reach = NoticeRange * AwarenessTuning.SalienceMax;

            int count = 0;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c.IsDead() || adapter.IsMember(c) || !adapter.IsHostile(c)) continue;
                Vector3 cp = c.transform.position;
                P2 c2 = new P2(cp.x, cp.z);
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < sensorCount; i++) nearest = Mathf.Min(nearest, P2.Distance(obsPos[i], c2));
                if (nearest > reach) continue;
                if (count >= CandidateCapacity) { CandidatesDropped++; continue; }
                candidates[count] = c;
                observed[count] = Observe(c, now, P2.Distance(centroid, c2));
                count++;
            }
            candidateCount = count;

            for (int i = 0; i < candidateCount; i++)
            {
                if (observed[i].HurtParty) { Book.Perceive(observed[i], ContactSource.Contact, SensorIndexOfMember(HurtMember(candidates[i])), now); continue; }
                for (int s = 0; s < sensorCount; s++)
                {
                    BaseAI ai = members[sensorOf[s]].Ai;
                    if (ai != null && ai.CanHearTarget(candidates[i])) { Book.Perceive(observed[i], ContactSource.Hearing, s, now); break; }
                }
            }
        }

        int SensorIndexOfMember(int m) { for (int s = 0; s < sensorCount; s++) if (sensorOf[s] == m) return s; return -1; }

        int HurtMember(Character c)
        {
            for (int m = 0; m < memberCount; m++)
            {
                NpcKernel k = members[m].Kernel;
                if (k != null && ReferenceEquals(k.RecentAttacker(AwarenessTuning.HurtMemory), c)) return m;
            }
            return -1;
        }

        void EnvelopeFirst(int id, float now, float distance, out float since, out float at)
        {
            for (int i = 0; i < envId.Length; i++) if (envId[i] == id) { since = envAt[i]; at = envDist[i]; return; }
            envId[envNext] = id; envAt[envNext] = now; envDist[envNext] = distance; envNext = (envNext + 1) % envId.Length;
            since = now; at = distance;
        }

        ContactObservation Observe(Character c, float now, float centroidDistance)
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
            if (target != null) for (int m = 0; m < memberCount; m++) if (ReferenceEquals(members[m].Actor, target)) { member = m; break; }
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
                int seen = VisibleIntent.Target(pos, fwd, vel, c.InAttack(), memberPos, memberCount);
                if (seen >= 0) { member = seen; perceptible = true; }
                if (reader == IntentReader.VisibleOnly || reader == IntentReader.NativeBaseAI) alerted = alerted || VisibleIntent.Alerted(vel, seen);
            }
            int id = c.GetInstanceID();
            float since, at;
            EnvelopeFirst(id, now, centroidDistance, out since, out at);
            return new ContactObservation
            {
                Id = id, Family = c.m_name,
                Wildlife = c.GetFaction() == Character.Faction.AnimalsVeg, Sleeping = ai != null && ai.IsSleeping(), Boss = c.IsBoss(),
                Position = pos, Height = p.y, Velocity = vel, Radius = c.GetRadius(),
                NativeAlerted = alerted, NativeTargetMember = member, TargetPerceptible = perceptible,
                HurtParty = HurtMember(c) >= 0,
                EnvelopeSince = since, EnvelopeDistance = at
            };
        }

        void CastDue(float now)
        {
            if (sensorCount == 0 || candidateCount == 0) return;
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
                float mine = lastRayId[i] == id ? lastRay[i] : float.NegativeInfinity;
                float last = r != null ? Mathf.Max(r.LastRayAt, mine) : mine;
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
                for (int s = 0; s < sensorCount; s++)
                {
                    eligible[s] = Visibility.InFieldOfRegard(obsPos[s], obsFwd[s], pos, range);
                    obsDist[s] = P2.Distance(obsPos[s], pos);
                    any |= eligible[s];
                }
                lastRayId[i] = observed[i].Id; lastRay[i] = now;
                ContactRecord rec = Book.Find(observed[i].Id); if (rec != null) rec.LastRayAt = now;
                if (!any) continue;
                int who = rays.ChooseObserver(eligible, obsDist, sensorCount);
                Character eyeOf = members[sensorOf[who]].Actor;
                Vector3 eye = eyeOf.m_eye != null ? eyeOf.m_eye.position : eyeOf.transform.position + Vector3.up * 1.6f;
                if (!rays.TryTake(who)) { lastRay[i] = float.NegativeInfinity; if (rec != null) rec.LastRayAt = float.NegativeInfinity; break; }
                bool seen = !Physics.Linecast(eye, c.GetCenterPoint(), viewMask, QueryTriggerInteraction.Ignore);
                if (!seen && rays.TryTake(who))
                {
                    Vector3 top = c.m_eye != null ? c.m_eye.position : c.GetCenterPoint() + Vector3.up * .6f;
                    seen = !Physics.Linecast(eye, top, viewMask, QueryTriggerInteraction.Ignore);
                }
                if (seen) Book.Perceive(observed[i], ContactSource.Sight, who, now);
            }
        }

        /// <summary>Initiative: each member's own native target and attack/shot/throw on a contact, as observable facts.</summary>
        void ObserveMemberActions(float now)
        {
            for (int m = 0; m < memberCount; m++)
            {
                NpcKernel k = members[m].Kernel;
                if (k == null || k.Body == null || k.Body.Human == null) continue;
                MonsterAI ai = k.Body.Ai as MonsterAI;
                Character t = ai == null ? null : ai.GetTargetCreature();
                if (t == null) continue;
                int id = t.GetInstanceID();
                Book.MarkMember(id, Milestone.MemberTarget, now);
                Humanoid h = k.Body.Human;
                if (h.InAttack() || h.IsDrawingBow() || k.Body.Throwing) Book.MarkMember(id, Milestone.MemberAction, now);
            }
        }

        void UpdatePosture()
        {
            ContactState strongest = ContactState.None; bool anyNoticed = false;
            foreach (ContactRecord r in Book.Records)
            {
                if (!r.Used || r.State == ContactState.None || r.State == ContactState.Lost) continue;
                anyNoticed = true;
                if (r.Wildlife && r.State != ContactState.Committed) continue;
                if (r.State > strongest) strongest = r.State;
            }
            Posture = StealthDoctrine.Posture(strongest, anyNoticed, adapter.StealthIntent);
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
