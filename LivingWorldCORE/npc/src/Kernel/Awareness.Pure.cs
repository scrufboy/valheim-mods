// Living World C.O.R.E. - A1 shared party awareness (shadow). Pure, offline-testable, fixed capacity.
// Design: docs/development/FORMIDABLE-AI-AWARENESS-DESIGN.md (rev 2 + 8e89866). Brian 2026-10-30: awareness notices,
// interpretation reads alert/approach/commitment, arbitration chooses, the skill validates, native mechanics decide.
// A1 is read-only: nothing here drives phases, targets, movement, look, hands, combat, bombs, bows or voice.
// Companion-owned perception only: the leader's camera, facing and sight are never inputs.
using System;

namespace LivingWorld.Core
{
    public enum ContactState { None, Noticed, Alerted, Committed, Lost }
    public enum ContactSource { None, Sight, Hearing, Contact }
    public enum ShadowPosture { Calm, Observe, Readiness, HoldStealth, Combat }

    public static class AwarenessTuning
    {
        public const float ClearDayCeiling = 60f;            // first clear-day noticing maximum (test value)
        public const float OrdinaryDarkFactor = 35f / 60f;    // temporary ordinary-dark factor, tuned from video
        public const float NearRegard = 12f;                  // 360 deg within this (peripheral / hearing)
        public const float RegardHalfAngle = 100f;            // head and shoulder turn either side of facing
        public const float SalienceMin = .6f, SalienceMax = 1.2f;
        public const int NoticeSamples = 2;                   // perception samples needed to notice
        public const float NoticeSpacing = .25f;              // ... at least this far apart
        public const float LostAfter = 3f, DropAfter = 10f, DropCommittedAfter = 20f;
        public const float VelocitySmoothing = .35f;          // per sample blend; the 1.5 s window estimates motion
        public const float CommitClosingSpeed = 1f, CommitHeading = 35f, CommitSustain = 1.5f, CommitGrace = .5f;
        public const float DecommitAfter = 3f;
        public const float TargetFacing = 30f, ChargeSpeed = 1.5f, TargetEvidenceFresh = .6f;
        public const float HurtMemory = 8f;                   // existing kernel hurt memory
        public const int ContactCapacity = 32, GroupCapacity = 8, ObserverCapacity = 8;
        public const float GroupLink = 6f;
        public const float RaysPerSecond = 48f, RayBurst = 12f;
        public const float UrgentRefresh = .25f, FarRefresh = 1f, NearBand = 20f;
        public const float EnumerationInterval = .25f;        // 4 Hz candidate distance pass per party
        public const int LeaderTarget = 99;                   // TargetsWhom value for "the leader" (a party member, not a sensor)
    }

    public static class Visibility
    {
        public static float EnvironmentFactor(bool dark) { return dark ? AwarenessTuning.OrdinaryDarkFactor : 1f; }

        /// <summary>Target salience from size and motion, bounded. Radius in metres, speed in m/s.</summary>
        public static float Salience(float radius, float speed)
        {
            float s = 1f;
            if (radius < .4f) s -= .2f; else if (radius >= 1f) s += .1f;
            if (speed > 1f) s += .1f;
            return s < AwarenessTuning.SalienceMin ? AwarenessTuning.SalienceMin : s > AwarenessTuning.SalienceMax ? AwarenessTuning.SalienceMax : s;
        }

        public static float NoticeRange(float environment, float salience, float training)
        {
            return AwarenessTuning.ClearDayCeiling * environment * salience * training;
        }

        /// <summary>Within notice range and inside the observer's field of regard (360 close, +-100 deg beyond).</summary>
        public static bool InFieldOfRegard(P2 observer, P2 forward, P2 target, float range)
        {
            P2 to = target - observer;
            float d = to.Length;
            if (d > range) return false;
            if (d <= AwarenessTuning.NearRegard) return true;
            P2 f = forward.Normalized;
            if (f.Length < .5f) return true;
            float cos = P2.Dot(f, to * (1f / d));
            return cos >= (float)Math.Cos(AwarenessTuning.RegardHalfAngle * Math.PI / 180d);
        }
    }

    /// <summary>What the runtime read about one candidate this pass (positions and visible behavior only).</summary>
    public struct ContactObservation
    {
        public int Id;
        public string Family;
        public bool Wildlife, Sleeping, Boss;
        public P2 Position; public float Height; public P2 Velocity; public float Radius;
        public bool NativeAlerted;          // its own alert state; counts only when sighted (visible stance/run)
        public int NativeTargetMember;      // party member it natively targets (0..7 companion, 99 leader), -1 none
        public bool TargetPerceptible;      // facing that member within 30 deg, charging it, or attacking
        public bool HurtParty;              // hurt a party member within the hurt memory
    }

    public sealed class ContactRecord
    {
        public bool Used;
        public int Id; public string Family; public bool Wildlife, Sleeping, Boss;
        public ContactState State, LostFrom;
        public ContactSource LastSource; public int LastObserver = -1;
        public float FirstSample, FirstNoticed, LastSeen, LastSampleAt, LastRayAt = float.NegativeInfinity;
        public int Samples;
        public P2 LastKnownPos; public float Height; public P2 Velocity; public float Radius;
        public float ClosingSpeed, TimeToContact = float.PositiveInfinity, Distance = float.PositiveInfinity;
        public bool AlertedObserved;
        public int TargetsWhomObserved = -1; public float TargetEvidenceAt = float.NegativeInfinity;
        public bool HurtParty;
        public float CommitEvidence, ApproachSeenAt = float.NegativeInfinity, LastCommitReasonAt = float.NegativeInfinity;
        public string CommitReason = "";
        public float Confidence;
        public int GroupId = -1;

        internal void Reset(int id, float now)
        {
            Used = true; Id = id; Family = ""; Wildlife = Sleeping = Boss = false;
            State = LostFrom = ContactState.None; LastSource = ContactSource.None; LastObserver = -1;
            FirstSample = now; FirstNoticed = LastSeen = LastSampleAt = now; LastRayAt = float.NegativeInfinity; Samples = 0;
            LastKnownPos = new P2(0f, 0f); Height = 0f; Velocity = new P2(0f, 0f); Radius = .5f;
            ClosingSpeed = 0f; TimeToContact = Distance = float.PositiveInfinity;
            AlertedObserved = false; TargetsWhomObserved = -1; TargetEvidenceAt = float.NegativeInfinity; HurtParty = false;
            CommitEvidence = 0f; ApproachSeenAt = LastCommitReasonAt = float.NegativeInfinity; CommitReason = "";
            Confidence = 0f; GroupId = -1;
        }
    }

    public struct ContactTransition
    {
        public long Sequence; public float Time;
        public int Id; public string Family;
        public ContactState From, To; public ContactSource Source; public int Observer;
        public float Distance, Closing, TimeToContact; public bool Alerted; public int TargetsWhom;
        public string Evidence;
    }

    /// <summary>Fixed-capacity transient contact memory and interpretation. Never persisted.</summary>
    public sealed class ContactBook
    {
        public readonly ContactRecord[] Records = new ContactRecord[AwarenessTuning.ContactCapacity];
        readonly ContactTransition[] ring = new ContactTransition[128];
        long sequence;
        public int Evicted { get; private set; }
        public int Refused { get; private set; }
        public long TransitionCount { get { return sequence; } }

        public ContactBook() { for (int i = 0; i < Records.Length; i++) Records[i] = new ContactRecord(); }

        public int Count { get { int n = 0; for (int i = 0; i < Records.Length; i++) if (Records[i].Used) n++; return n; } }

        public ContactRecord Find(int id)
        {
            for (int i = 0; i < Records.Length; i++) if (Records[i].Used && Records[i].Id == id) return Records[i];
            return null;
        }

        /// <summary>A slot for a new contact: a free one, else evict the stalest non-committed; null if all are committed.</summary>
        ContactRecord Acquire(int id, float now)
        {
            ContactRecord free = null, stalest = null;
            for (int i = 0; i < Records.Length; i++)
            {
                ContactRecord r = Records[i];
                if (!r.Used) { if (free == null) free = r; continue; }
                bool committed = r.State == ContactState.Committed || (r.State == ContactState.Lost && r.LostFrom == ContactState.Committed);
                if (committed) continue;
                if (stalest == null || r.LastSeen < stalest.LastSeen) stalest = r;
            }
            ContactRecord slot = free;
            if (slot == null && stalest != null)
            {
                Evicted++;
                if (stalest.State != ContactState.None) Emit(stalest, stalest.State, ContactState.None, now, "evicted");
                slot = stalest;
            }
            if (slot == null) { Refused++; return null; }
            slot.Reset(id, now);
            return slot;
        }

        /// <summary>One perception sample. Sight and hearing need two samples to notice; a contact (it hurt us) notices at once.</summary>
        public ContactRecord Perceive(ContactObservation o, ContactSource source, int observer, float now)
        {
            ContactRecord r = Find(o.Id) ?? Acquire(o.Id, now);
            if (r == null) return null;
            r.Family = o.Family ?? ""; r.Wildlife = o.Wildlife; r.Sleeping = o.Sleeping; r.Boss = o.Boss; r.Radius = o.Radius;
            if (r.Samples > 0)
            {
                float k = AwarenessTuning.VelocitySmoothing;
                r.Velocity = r.Velocity * (1f - k) + o.Velocity * k;
            }
            else r.Velocity = o.Velocity;
            r.LastKnownPos = o.Position; r.Height = o.Height;
            r.LastSeen = now; r.LastSource = source; r.LastObserver = observer;
            if (r.Samples == 0 || now - r.LastSampleAt >= AwarenessTuning.NoticeSpacing) { r.Samples++; r.LastSampleAt = now; }
            if (source == ContactSource.Sight)
            {
                // Alert and targeting are read only while the companion actually sees the creature's behavior.
                r.AlertedObserved = o.NativeAlerted;
                if (o.NativeTargetMember >= 0 && o.TargetPerceptible) { r.TargetsWhomObserved = o.NativeTargetMember; r.TargetEvidenceAt = now; }
            }
            r.HurtParty = o.HurtParty;
            r.Confidence = source == ContactSource.Sight || source == ContactSource.Contact ? 1f : Math.Max(r.Confidence, .4f);
            if (r.State == ContactState.Lost) Emit(r, ContactState.Lost, r.State = r.LostFrom, now, "seen again");
            else if (r.State == ContactState.None && (source == ContactSource.Contact || r.Samples >= AwarenessTuning.NoticeSamples))
            {
                r.FirstNoticed = now;
                Emit(r, ContactState.None, r.State = ContactState.Noticed, now, source == ContactSource.Contact ? "contact" : r.Samples + " samples");
            }
            return r;
        }

        /// <summary>Interpretation and staleness. centroid = party centroid (companions and the leader's body).</summary>
        public void Step(float now, float dt, P2 centroid)
        {
            for (int i = 0; i < Records.Length; i++)
            {
                ContactRecord r = Records[i];
                if (!r.Used) continue;
                float since = now - r.LastSeen;
                r.Confidence = since <= 0f ? r.Confidence : Math.Max(0f, r.Confidence * (1f - dt / AwarenessTuning.DropAfter));
                P2 to = centroid - r.LastKnownPos;
                r.Distance = to.Length;
                P2 dir = to.Normalized;
                r.ClosingSpeed = P2.Dot(r.Velocity, dir);
                r.TimeToContact = r.ClosingSpeed > .2f ? r.Distance / r.ClosingSpeed : float.PositiveInfinity;

                if (r.State == ContactState.None)
                {
                    if (since >= AwarenessTuning.LostAfter) r.Used = false;      // never noticed: forget quietly
                    continue;
                }
                if (r.State == ContactState.Lost)
                {
                    float keep = r.LostFrom == ContactState.Committed ? AwarenessTuning.DropCommittedAfter : AwarenessTuning.DropAfter;
                    if (since >= keep) { Emit(r, ContactState.Lost, ContactState.None, now, "dropped"); r.Used = false; }
                    continue;
                }
                if (since >= AwarenessTuning.LostAfter) { r.LostFrom = r.State; Emit(r, r.State, r.State = ContactState.Lost, now, "not seen " + AwarenessTuning.LostAfter + "s"); continue; }

                // Commitment evidence, each a visible behavior: it hurt us; it perceptibly targets one of us; sustained approach while alert.
                bool fresh = since < AwarenessTuning.TargetEvidenceFresh;
                bool targets = r.TargetsWhomObserved >= 0 && now - r.TargetEvidenceAt < AwarenessTuning.TargetEvidenceFresh;
                if (!targets) r.TargetsWhomObserved = -1;
                float speed = r.Velocity.Length;
                bool heading = speed > .1f && P2.Dot(r.Velocity * (1f / speed), dir) >= (float)Math.Cos(AwarenessTuning.CommitHeading * Math.PI / 180d);
                bool approach = fresh && r.AlertedObserved && r.ClosingSpeed >= AwarenessTuning.CommitClosingSpeed && heading;
                if (approach) { r.CommitEvidence += dt; r.ApproachSeenAt = now; }
                else if (now - r.ApproachSeenAt > AwarenessTuning.CommitGrace) r.CommitEvidence = 0f;
                bool sustained = !r.Wildlife && !r.Sleeping && r.CommitEvidence >= AwarenessTuning.CommitSustain;
                bool committedNow = r.HurtParty || targets || sustained;
                if (committedNow) r.LastCommitReasonAt = now;

                if (committedNow && r.State != ContactState.Committed)
                {
                    // The reason string is built only on the transition (no per-frame allocation).
                    r.CommitReason = r.HurtParty ? "hurt party" : targets ? "targets " + (r.TargetsWhomObserved == AwarenessTuning.LeaderTarget ? "leader" : "companion " + r.TargetsWhomObserved)
                        : "closing " + r.ClosingSpeed.ToString("0.0") + " m/s for " + r.CommitEvidence.ToString("0.0") + " s";
                    Emit(r, r.State, r.State = ContactState.Committed, now, r.CommitReason);
                }
                else if (r.State == ContactState.Committed && !committedNow && now - r.LastCommitReasonAt >= AwarenessTuning.DecommitAfter)
                {
                    ContactState to2 = r.AlertedObserved ? ContactState.Alerted : ContactState.Noticed;
                    Emit(r, ContactState.Committed, r.State = to2, now, "no targeting, approach or harm for " + AwarenessTuning.DecommitAfter + "s");
                }
                else if (r.State == ContactState.Noticed && r.AlertedObserved)
                    Emit(r, ContactState.Noticed, r.State = ContactState.Alerted, now, "alert behavior seen");
            }
        }

        void Emit(ContactRecord r, ContactState from, ContactState to, float now, string evidence)
        {
            ContactTransition t = new ContactTransition
            {
                Sequence = sequence, Time = now, Id = r.Id, Family = r.Family, From = from, To = to, Source = r.LastSource,
                Observer = r.LastObserver, Distance = r.Distance, Closing = r.ClosingSpeed, TimeToContact = r.TimeToContact,
                Alerted = r.AlertedObserved, TargetsWhom = r.TargetsWhomObserved, Evidence = evidence
            };
            ring[sequence % ring.Length] = t;
            sequence++;
        }

        /// <summary>Transitions with Sequence greater than after, oldest first, into out (bounded). Returns count.</summary>
        public int TransitionsAfter(long after, ContactTransition[] output)
        {
            long start = Math.Max(after + 1, sequence - ring.Length);
            int n = 0;
            for (long s = start; s < sequence && n < output.Length; s++) output[n++] = ring[s % ring.Length];
            return n;
        }
    }

    public sealed class GroupRecord
    {
        public int Id, Members, Alerted, Committed, Closing;
        public P2 Centroid, Velocity; public float Radius, Distance, ClosingSpeed, TimeToContact;
        public bool IsCommitted;
        public int NearestMemberId;
        public int SusceptibleKnown, ImmuneKnown, Unknown;   // poison, from KnowledgeBook only
    }

    /// <summary>Single-link groups of non-wildlife hostile contacts. Fixed arrays.</summary>
    public sealed class GroupBook
    {
        public readonly GroupRecord[] Groups = new GroupRecord[AwarenessTuning.GroupCapacity];
        readonly int[] parent = new int[AwarenessTuning.ContactCapacity];
        readonly int[] slotOf = new int[AwarenessTuning.ContactCapacity];
        public int Count { get; private set; }
        public int Overflow { get; private set; }

        public GroupBook() { for (int i = 0; i < Groups.Length; i++) Groups[i] = new GroupRecord(); }

        int Root(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }

        static bool Member(ContactRecord r)
        {
            return r.Used && !r.Wildlife && (r.State == ContactState.Noticed || r.State == ContactState.Alerted || r.State == ContactState.Committed);
        }

        public void Build(ContactBook book, P2 centroid, KnowledgeBook knowledge)
        {
            ContactRecord[] rs = book.Records;
            int n = rs.Length;
            for (int i = 0; i < n; i++) { parent[i] = i; rs[i].GroupId = -1; slotOf[i] = -1; }
            for (int i = 0; i < n; i++)
            {
                if (!Member(rs[i])) continue;
                for (int j = i + 1; j < n; j++)
                {
                    if (!Member(rs[j])) continue;
                    if (P2.Distance(rs[i].LastKnownPos, rs[j].LastKnownPos) <= AwarenessTuning.GroupLink)
                    { int a = Root(i), b = Root(j); if (a != b) parent[b] = a; }
                }
            }
            Count = 0;
            int overflow = 0;
            for (int i = 0; i < n; i++)
            {
                if (!Member(rs[i])) continue;
                int root = Root(i);
                if (slotOf[root] < 0)
                {
                    if (Count >= Groups.Length) { overflow++; continue; }
                    slotOf[root] = Count;
                    GroupRecord g0 = Groups[Count++];
                    g0.Id = int.MaxValue; g0.Members = g0.Alerted = g0.Committed = g0.Closing = 0;
                    g0.Centroid = g0.Velocity = new P2(0f, 0f); g0.Radius = 0f; g0.NearestMemberId = -1; g0.Distance = float.PositiveInfinity;
                    g0.SusceptibleKnown = g0.ImmuneKnown = g0.Unknown = 0;
                }
                GroupRecord g = Groups[slotOf[root]];
                ContactRecord r = rs[i];
                g.Members++;
                g.Id = Math.Min(g.Id, r.Id);
                g.Centroid = g.Centroid + r.LastKnownPos; g.Velocity = g.Velocity + r.Velocity;
                if (r.State == ContactState.Alerted || r.State == ContactState.Committed) g.Alerted++;
                if (r.State == ContactState.Committed) g.Committed++;
                if (r.ClosingSpeed >= AwarenessTuning.CommitClosingSpeed) g.Closing++;
                if (r.Distance < g.Distance) { g.Distance = r.Distance; g.NearestMemberId = r.Id; }
                KnowledgeView k = knowledge == null ? KnowledgeView.UnknownView : knowledge.Query(r.Family, DamageKind.Poison);
                if (!k.Known) g.Unknown++; else if (k.Value == Susceptibility.Immune) g.ImmuneKnown++; else g.SusceptibleKnown++;
            }
            Overflow = overflow;
            for (int gi = 0; gi < Count; gi++)
            {
                GroupRecord g = Groups[gi];
                float inv = 1f / g.Members;
                g.Centroid = g.Centroid * inv; g.Velocity = g.Velocity * inv;
                g.Radius = 0f;
                for (int i = 0; i < n; i++)
                    if (Member(rs[i]) && slotOf[Root(i)] == gi)
                    {
                        rs[i].GroupId = g.Id;
                        g.Radius = Math.Max(g.Radius, P2.Distance(rs[i].LastKnownPos, g.Centroid));
                    }
                P2 to = centroid - g.Centroid;
                float d = to.Length;
                g.ClosingSpeed = P2.Dot(g.Velocity, to.Normalized);
                g.TimeToContact = g.ClosingSpeed > .2f ? Math.Max(0f, d - g.Radius) / g.ClosingSpeed : float.PositiveInfinity;
                g.IsCommitted = g.Committed > 0 || g.Closing * 2 >= g.Members && g.Alerted * 2 >= g.Members && g.ClosingSpeed >= AwarenessTuning.CommitClosingSpeed;
            }
        }
    }

    public enum DamageKind { Poison, Fire, Frost, Lightning, Spirit }
    public enum Susceptibility { Unknown, Immune, Resistant, Neutral, Weak }
    public enum KnowledgeProvenance { None, Training, BiomeDoctrine, Lore, ObservedCombat }

    public struct KnowledgeView
    {
        public bool Known; public Susceptibility Value; public float Confidence; public KnowledgeProvenance Provenance;
        public static readonly KnowledgeView UnknownView = new KnowledgeView();
    }

    /// <summary>
    /// Learned tactical knowledge (separate from transient contacts). A1 ships it EMPTY: nothing is known until a
    /// legitimate source (training, biome doctrine, lore, observed combat) adds a fact. Native damage modifiers are
    /// never read into it. Unknown is a real answer.
    /// </summary>
    public sealed class KnowledgeBook
    {
        struct Fact { public string Family; public DamageKind Kind; public Susceptibility Value; public float Confidence; public KnowledgeProvenance From; }
        readonly Fact[] facts = new Fact[64];
        int count;
        public int Count { get { return count; } }

        public bool Learn(string family, DamageKind kind, Susceptibility value, float confidence, KnowledgeProvenance from)
        {
            if (string.IsNullOrEmpty(family) || from == KnowledgeProvenance.None || value == Susceptibility.Unknown) return false;
            for (int i = 0; i < count; i++)
                if (facts[i].Family == family && facts[i].Kind == kind)
                {
                    if (confidence >= facts[i].Confidence) facts[i] = new Fact { Family = family, Kind = kind, Value = value, Confidence = confidence, From = from };
                    return true;
                }
            if (count >= facts.Length) return false;
            facts[count++] = new Fact { Family = family, Kind = kind, Value = value, Confidence = confidence, From = from };
            return true;
        }

        public KnowledgeView Query(string family, DamageKind kind)
        {
            for (int i = 0; i < count; i++)
                if (facts[i].Family == family && facts[i].Kind == kind)
                    return new KnowledgeView { Known = true, Value = facts[i].Value, Confidence = facts[i].Confidence, Provenance = facts[i].From };
            return KnowledgeView.UnknownView;
        }
    }

    /// <summary>
    /// Fair line-of-sight scheduling for one party. Budget: RaysPerSecond tokens (burst RayBurst). Due contacts are
    /// served in urgency order (band 0 committed/closing, band 1 within 20 m, band 2 the rest; then time to contact,
    /// then distance). Each is cast from an eligible observer chosen round-robin by fewest rays spent this second,
    /// nearest on ties, so no companion is starved and urgent contacts never wait behind idle distant ones.
    /// </summary>
    public sealed class RayScheduler
    {
        float tokens = AwarenessTuning.RayBurst, windowStart;
        readonly int[] spent = new int[AwarenessTuning.ObserverCapacity];
        readonly int[] spentLast = new int[AwarenessTuning.ObserverCapacity];
        public int RaysThisWindow { get; private set; }
        public int RaysLastWindow { get; private set; }
        public int Deferred { get; private set; }
        public int SpentLast(int observer) { return observer >= 0 && observer < spentLast.Length ? spentLast[observer] : 0; }

        public void Refill(float now, float dt)
        {
            tokens = Math.Min(AwarenessTuning.RayBurst, tokens + AwarenessTuning.RaysPerSecond * dt);
            if (now - windowStart >= 1f)
            {
                RaysLastWindow = RaysThisWindow; RaysThisWindow = 0;
                for (int i = 0; i < spent.Length; i++) { spentLast[i] = spent[i]; spent[i] = 0; }
                windowStart = now;
            }
        }

        public bool TryTake(int observer)
        {
            if (tokens < 1f) { Deferred++; return false; }
            tokens -= 1f; RaysThisWindow++;
            if (observer >= 0 && observer < spent.Length) spent[observer]++;
            return true;
        }

        public static int Band(bool committedOrClosing, float distance)
        {
            return committedOrClosing ? 0 : distance <= AwarenessTuning.NearBand ? 1 : 2;
        }

        public static float RefreshInterval(int band) { return band <= 1 ? AwarenessTuning.UrgentRefresh : AwarenessTuning.FarRefresh; }

        /// <summary>Index into eligible[] (observers that can see the spot), fewest rays this window, nearest on ties; -1 if none.</summary>
        public int ChooseObserver(bool[] eligible, float[] distance, int observers)
        {
            int best = -1;
            for (int i = 0; i < observers && i < spent.Length; i++)
            {
                if (!eligible[i]) continue;
                if (best < 0 || spent[i] < spent[best] || spent[i] == spent[best] && distance[i] < distance[best]) best = i;
            }
            return best;
        }

        /// <summary>Insertion sort of due work by (band, time to contact, distance). Fixed arrays, n small.</summary>
        public static void Order(int[] index, int n, int[] band, float[] ttc, float[] distance)
        {
            for (int i = 1; i < n; i++)
            {
                int v = index[i], j = i - 1;
                while (j >= 0 && Before(v, index[j], band, ttc, distance)) { index[j + 1] = index[j]; j--; }
                index[j + 1] = v;
            }
        }

        static bool Before(int a, int b, int[] band, float[] ttc, float[] distance)
        {
            if (band[a] != band[b]) return band[a] < band[b];
            if (ttc[a] != ttc[b]) return ttc[a] < ttc[b];
            return distance[a] < distance[b];
        }
    }

    /// <summary>
    /// Visible-behavior intent, for actors whose AI names no target or sets no alert (other mods' characters, any
    /// reader gap). A player would read the same cues: it is facing a member and closing on it, or attacking one close by.
    /// </summary>
    public static class VisibleIntent
    {
        public const float AttackReach = 4f, AlertRunSpeed = 3f;

        /// <returns>index into members of the visibly targeted member, or -1.</returns>
        public static int Target(P2 position, P2 forward, P2 velocity, bool attacking, P2[] members, int count)
        {
            int best = -1; float bestD = float.PositiveInfinity;
            P2 f = forward.Normalized;
            double cosFacing = Math.Cos(AwarenessTuning.TargetFacing * Math.PI / 180d);
            for (int i = 0; i < count; i++)
            {
                P2 to = members[i] - position;
                float d = to.Length;
                if (d < 1e-3f) continue;
                P2 dir = to * (1f / d);
                bool facing = f.Length > .5f && P2.Dot(f, dir) >= cosFacing;
                bool charging = P2.Dot(velocity, dir) >= AwarenessTuning.ChargeSpeed;
                bool striking = attacking && d <= AttackReach && facing;
                if (!(striking || facing && charging)) continue;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>Visible alert: running, or visibly targeting a member.</summary>
        public static bool Alerted(P2 velocity, int visibleTarget) { return visibleTarget >= 0 || velocity.Length >= AlertRunSpeed; }
    }

    /// <summary>
    /// The posture A2 would adopt (computed in A1 as a shadow fact only). Noticing produces observation; credible threat
    /// produces readiness; commitment produces combat. Sneaking is a cost, not a veto: merely noticed/alerted contacts
    /// keep the party hidden; a committed contact means stealth has failed.
    /// </summary>
    public static class StealthDoctrine
    {
        public static ShadowPosture Posture(ContactState strongestHostile, bool anyNoticed, bool leaderSneaking)
        {
            if (strongestHostile == ContactState.Committed) return ShadowPosture.Combat;
            if (strongestHostile == ContactState.Alerted) return leaderSneaking ? ShadowPosture.HoldStealth : ShadowPosture.Readiness;
            if (anyNoticed) return leaderSneaking ? ShadowPosture.HoldStealth : ShadowPosture.Observe;
            return ShadowPosture.Calm;
        }

        /// <summary>May the party initiate on this contact? Only once it has committed, sneaking or not.</summary>
        public static bool MayInitiate(ContactState state) { return state == ContactState.Committed; }
    }
}
