// Living World C.O.R.E. - party spacing (1.0.32.8, corrections 1.0.32.9). Pure decision law, no Unity: offline-testable.
// Movement presentation only. The result is a point; native MonsterAI.Follow stays the sole movement writer.
using System;

namespace LivingWorld.Core
{
    /// <summary>A flat (x, z) point.</summary>
    public struct P2
    {
        public float X, Z;
        public P2(float x, float z) { X = x; Z = z; }
        public static P2 operator +(P2 a, P2 b) { return new P2(a.X + b.X, a.Z + b.Z); }
        public static P2 operator -(P2 a, P2 b) { return new P2(a.X - b.X, a.Z - b.Z); }
        public static P2 operator *(P2 a, float k) { return new P2(a.X * k, a.Z * k); }
        public float Length { get { return (float)Math.Sqrt(X * X + Z * Z); } }
        public static float Dot(P2 a, P2 b) { return a.X * b.X + a.Z * b.Z; }
        public static float Distance(P2 a, P2 b) { return (a - b).Length; }
        public P2 Normalized { get { float l = Length; return l < 1e-5f ? new P2(0f, 0f) : new P2(X / l, Z / l); } }
        /// <summary>90 degrees to the left of a travel direction (x right, z forward).</summary>
        public P2 Left { get { return new P2(-Z, X); } }
    }

    /// <summary>One loaded party member near this companion (the bound leader or a companion of the same leader).</summary>
    public struct SpacingNeighbor
    {
        public int Id;           // stable identity (instance id; the leader uses 0)
        public bool IsLeader;
        public P2 Position;
        public P2 Velocity;
        public int Slot;         // formation slot (-1 unknown); lower slot = right of way
    }

    /// <summary>Unity side: is a flat point standable and reachable in a straight line from here (ground, no water,
    /// no drop or climb, no wall)? Implemented with the guard module's ground and segment checks.</summary>
    public interface ISpacingTerrain
    {
        bool Clear(P2 from, P2 to);
    }

    public enum SpacingMode { Travel, Hold, Sidestep, Wait, BackOff }

    public struct SpacingResult
    {
        public SpacingMode Mode;
        public P2 Shift;           // Travel while releasing: lateral correction added to the aim; BackOff: unit direction away
        public P2 Point;           // Hold / Wait: stand here; BackOff: walk to here; Sidestep: pass point to walk at
        public int AvoidedId;      // -1 none
        public bool AvoidedLeader;
        public int Side;           // +1 left, -1 right, 0 none
        public float Offset;       // current lateral offset, metres
        public float RegionDistance; // distance outside the soft standing region (0 = inside)
        public string Event;       // "enter", "release", "wait", "blocked", "backoff" on the tick it happens; else null
    }

    /// <summary>
    /// Party spacing for calm movement (traversal, readiness, post-combat regroup, catch-up, guard approach).
    /// Hard rule (Brian, 2026-10-27): the leader is never pushed - no standing spot or short-cut is worth touching him;
    /// in a menu the clearance widens and a companion already inside it eases back out.
    /// Soft region: a standing companion near its spot (within HoldEnter) stays put until it is HoldExit away, its
    /// spacing is violated, or the leader moves - it does not chase the mathematical centre.
    /// Sidestep: a party member in the walking corridor shifts the follow aim sideways (bounded, smoothed, side locked
    /// for the whole episode, hysteresis on entry and release). If no lane is clear: wait (re-evaluating both lanes), never push and never hand back into the blockage.
    /// Never used in combat (traversal does not run there) and never a reason to regroup.
    /// </summary>
    public sealed class PartySpacing
    {
        public const float LeaderClear = 1.6f, MenuLeaderClear = 2.2f, MateClear = 1.3f;
        // 1.0.32.9: contact = bodies touching. Live 2026-10-28: pressed capsules sit at 0.95-0.98 m centre to centre, so the
        // old 0.9 m companion threshold never fired; contact is now 1.05 m for both.
        public const float ContactLeader = 1.05f, ContactMate = 1.05f;
        // 1.0.32.9: back off BEFORE touching. Leader: inside 1.3 m (LeaderClear less hysteresis); companion: inside 1.1 m.
        public const float BackOffHysteresis = .3f, MateBackOffEnter = 1.1f;
        public const float Lookahead = 5f, LookaheadSeconds = 1.5f, PassMargin = .4f, EnterPersist = .2f, ReleaseMargin = .5f, ReleaseBehind = .6f, ReleasePersist = .5f;
        public const float MinClosing = .3f, MinOffset = .6f, MaxOffset = 2.4f, RateIn = 2.5f, RateOut = 1.5f, HeadOn = .35f;
        public const float HoldEnter = 1f, HoldExit = 1.8f, HoldSpeed = .8f, WaitMax = 1.5f, BackOffMargin = .3f;
        public const float MinAhead = 1.5f, MenuBackOffHysteresis = .3f, RightOfWayStop = .4f, GiveWayRange = 2f, GiveWayStep = 1.5f;
        public const int MaxNeighbors = 8;

        // episode state
        bool holding;
        int pendingId = -1; float pendingSince;
        int activeId = -1; bool activeLeader; int side; float offset; float clearSince = -1f;
        bool releasing; float waitSince = -1f; bool backingOff; int mateBackId = -1;
        /// <summary>This companion's stable identity (instance id) for right-of-way ties.</summary>
        public int SelfId;

        public bool Holding { get { return holding; } }
        public bool Avoiding { get { return activeId != -1 || releasing; } }

        public static float Clearance(bool leader, bool leaderInMenu)
        {
            return leader ? (leaderInMenu ? MenuLeaderClear : LeaderClear) : MateClear;
        }

        /// <summary>Hard rule: a standing spot never lies inside the leader's clearance; it moves straight out to the edge.</summary>
        public static P2 KeepGoalOffLeader(P2 goal, P2 leader, bool leaderInMenu)
        {
            float clear = Clearance(true, leaderInMenu) + .2f;
            P2 out_ = goal - leader;
            float d = out_.Length;
            if (d >= clear) return goal;
            P2 dir = d < 1e-3f ? new P2(0f, -1f) : out_ * (1f / d);
            return leader + dir * clear;
        }

        /// <summary>Crowding is a local condition, never separation: regroup is decided only from leader / party
        /// distances under the cohesion rules, which this module never feeds.</summary>
        public static bool CrowdingCallsRegroup() { return false; }

        public void Reset()
        {
            holding = false; pendingId = -1; activeId = -1; side = 0; offset = 0f; clearSince = -1f;
            releasing = false; waitSince = -1f; backingOff = false; mateBackId = -1;
        }

        /// <param name="self">this companion</param>
        /// <param name="ownSpeed">its ground speed</param>
        /// <param name="aim">where native Follow would walk this tick without spacing</param>
        /// <param name="goal">the standing spot (slot or guard point); used for the soft region when standing</param>
        /// <param name="standing">leader standing and this is a formation spot (soft region applies)</param>
        /// <param name="slotSide">+1 left formation side, -1 right, 0 centre</param>
        /// <param name="mySlot">own slot (-1 unknown)</param>
        public SpacingResult Step(float now, float dt, P2 self, float ownSpeed, P2 aim, P2 goal, bool standing,
            bool leaderInMenu, int slotSide, int mySlot, SpacingNeighbor[] near, int count, ISpacingTerrain terrain)
        {
            // 1.0.32.9: a standing spot never lies against a companion that has right of way over this one; it moves
            // out to that companion's clearance (the yielder stands beside it instead of pressing into it).
            P2 goalShift = new P2(0f, 0f);
            if (standing)
            {
                int n = Math.Min(count, MaxNeighbors);
                for (int i = 0; i < n; i++)
                {
                    if (near[i].IsLeader || !HasRightOfWay(near[i].Slot, near[i].Id, SelfOf(mySlot, SelfId))) continue;
                    P2 moved = KeepGoalOff(goal + goalShift, near[i].Position, MateClear + .2f);
                    goalShift = moved - goal;
                }
            }
            SpacingResult r = StepCore(now, dt, self, ownSpeed, aim + goalShift, goal + goalShift, standing, leaderInMenu,
                slotSide, mySlot, near, count, terrain);
            if (r.Mode == SpacingMode.Travel) r.Shift = r.Shift + goalShift;
            return r;
        }

        static SpacingNeighbor SelfOf(int slot, int id) { return new SpacingNeighbor { Id = id, Slot = slot }; }

        /// <summary>A point inside `clear` of `center` moves straight out to that distance.</summary>
        public static P2 KeepGoalOff(P2 goal, P2 center, float clear)
        {
            P2 out_ = goal - center;
            float d = out_.Length;
            if (d >= clear) return goal;
            P2 dir = d < 1e-3f ? new P2(0f, -1f) : out_ * (1f / d);
            return center + dir * clear;
        }

        /// <summary>1.0.32.9: the clear point to back off to. Straight away first; if a wall or drop blocks it, the
        /// nearest clear direction at +-45 then +-90 degrees (a companion against a wall indoors still separates).</summary>
        static bool AwayPoint(ISpacingTerrain terrain, P2 self, P2 center, P2 away, float radius, out P2 to, out P2 dir)
        {
            float[] turns = { 0f, .785398f, -.785398f, 1.570796f, -1.570796f };
            for (int k = 0; k < turns.Length; k++)
            {
                float c = (float)Math.Cos(turns[k]), s = (float)Math.Sin(turns[k]);
                dir = new P2(away.X * c - away.Z * s, away.X * s + away.Z * c);
                to = center + dir * radius;
                if (terrain == null || terrain.Clear(self, to)) return true;
            }
            to = self; dir = away; return false;
        }

        SpacingResult StepCore(float now, float dt, P2 self, float ownSpeed, P2 aim, P2 goal, bool standing,
            bool leaderInMenu, int slotSide, int mySlot, SpacingNeighbor[] near, int count, ISpacingTerrain terrain)
        {
            var r = new SpacingResult { Mode = SpacingMode.Travel, AvoidedId = -1 };
            if (count > MaxNeighbors) count = MaxNeighbors;

            // 1. Hard rule: never inside the leader's clearance in a menu, never touching him at all.
            for (int i = 0; i < count; i++)
            {
                if (!near[i].IsLeader) continue;
                float d = P2.Distance(self, near[i].Position);
                // enter inside the contact distance (or the menu clearance less hysteresis); leave once back at the clearance
                // 1.0.32.9: enter before touching (1.3 m, or the menu clearance less hysteresis); leave at the clearance
                float enter = leaderInMenu ? MenuLeaderClear - MenuBackOffHysteresis : LeaderClear - BackOffHysteresis;
                float leave = Clearance(true, leaderInMenu);
                backingOff = backingOff ? d < leave : d < enter;
                if (backingOff)
                {
                    P2 away = d < 1e-3f ? new P2(0f, -1f) : (self - near[i].Position) * (1f / d);
                    P2 to, dir;
                    holding = false; pendingId = -1;
                    r.AvoidedId = near[i].Id; r.AvoidedLeader = true;
                    if (AwayPoint(terrain, self, near[i].Position, away, Clearance(true, leaderInMenu) + BackOffMargin, out to, out dir))
                    { r.Mode = SpacingMode.BackOff; r.Point = to; r.Shift = dir; r.Event = "backoff"; }
                    else { r.Mode = SpacingMode.Wait; r.Point = self; r.Event = "blocked"; }
                    return r;
                }
            }

            // 1b. 1.0.32.9: never stay pressed against another companion (live 2026-10-28: 17 s shoulder to shoulder
            //     indoors). Only the companion without right of way steps out, so the two never both move.
            int pressed = -1; float pressedD = 0f;
            for (int i = 0; i < count; i++)
            {
                if (near[i].IsLeader || HasRightOfWay(mySlot, SelfId, near[i])) continue;
                float d = P2.Distance(self, near[i].Position);
                float enter = mateBackId == near[i].Id ? MateClear : MateBackOffEnter;
                if (d < enter && (pressed < 0 || d < pressedD)) { pressed = i; pressedD = d; }
            }
            if (pressed >= 0)
            {
                mateBackId = near[pressed].Id;
                P2 away = pressedD < 1e-3f ? new P2(0f, -1f) : (self - near[pressed].Position) * (1f / pressedD);
                P2 to, dir;
                holding = false; pendingId = -1;
                r.AvoidedId = near[pressed].Id; r.AvoidedLeader = false;
                if (AwayPoint(terrain, self, near[pressed].Position, away, MateClear + BackOffMargin, out to, out dir))
                { r.Mode = SpacingMode.BackOff; r.Point = to; r.Shift = dir; r.Event = "giveway"; }
                else { r.Mode = SpacingMode.Wait; r.Point = self; r.Event = "blocked"; }
                return r;
            }
            mateBackId = -1;

            // 2. Soft region: a standing companion near its spot, well spaced, stays where it is.
            float fromGoal = P2.Distance(self, goal);
            r.RegionDistance = Math.Max(0f, fromGoal - HoldExit);
            if (standing && activeId == -1 && !releasing)
            {
                bool spaced = Spaced(self, near, count, leaderInMenu, mySlot);
                if (holding) holding = fromGoal <= HoldExit && spaced;
                else holding = fromGoal <= HoldEnter && ownSpeed <= HoldSpeed && spaced;
                if (holding) { r.Mode = SpacingMode.Hold; r.Point = self; return r; }
            }
            else holding = false;

            // 3. Corridor: the nearest party member in the way along the walk to the aim.
            P2 toAim = aim - self;
            float length = toAim.Length;
            P2 u = length > 1e-3f ? toAim * (1f / length) : new P2(0f, 1f);
            P2 left = u.Left;
            float look = Math.Max(Lookahead, ownSpeed * LookaheadSeconds);   // a running companion looks further ahead
            float rateIn = Math.Max(RateIn, ownSpeed * 1.2f);                 // and sidesteps as briskly as it moves
            int hit = -1; float hitT = float.PositiveInfinity, hitLat = 0f;
            for (int i = 0; i < count; i++)
            {
                float clear = Clearance(near[i].IsLeader, leaderInMenu);
                P2 rel = near[i].Position - self;
                float t = P2.Dot(rel, u), lat = P2.Dot(rel, left);
                // only someone we are closing on: a party member walking ahead at our pace is not in the way
                bool closing = ownSpeed - P2.Dot(near[i].Velocity, u) > MinClosing;
                bool ahead = closing && t > 0f && t < Math.Min(look, length + clear) && Math.Abs(lat) < clear;
                bool touching = closing && rel.Length < clear && t > -.3f;
                if ((ahead || touching) && t < hitT) { hit = i; hitT = t; hitLat = lat; }
            }

            if (activeId == -1 && !releasing)
            {
                if (hit < 0) { pendingId = -1; return r; }
                int id = near[hit].Id;
                if (pendingId != id) { pendingId = id; pendingSince = now; return r; }
                if (now - pendingSince < EnterPersist) return r;
                // start an episode, side locked until it ends
                activeId = id; activeLeader = near[hit].IsLeader; clearSince = -1f; waitSince = -1f;
                if (Math.Abs(hitLat) > HeadOn) side = hitLat > 0f ? -1 : 1;
                else if (!near[hit].IsLeader && P2.Dot(near[hit].Velocity, u) < -.3f) side = -1;   // oncoming: keep right
                else side = slotSide != 0 ? slotSide : -1;
                r.Event = "enter";
            }

            // 4. Active episode: walk to a pass point beside the party member, on the locked side.
            //    Offset = how far that point sits from the straight walk (smoothed in, smoothed out).
            float desired = 0f, epClear = 0f, epLat = 0f, epT = 0f;
            int idx = activeId == -1 ? -1 : Find(near, count, activeId);
            P2 pass = aim;
            if (idx >= 0 && !releasing)
            {
                epClear = Clearance(near[idx].IsLeader, leaderInMenu);
                P2 rel = near[idx].Position - self;
                epT = P2.Dot(rel, u); epLat = P2.Dot(rel, left);
                // 1.0.32.8b: release is geometric. "Not closing" only counts while we are actually walking (moving on
                // together); a stopped companion never treats a party member still standing in its corridor as clear.
                bool intrudes = (epT > 0f && epT < Math.Min(look, length + epClear) && Math.Abs(epLat) < epClear) ||
                    (rel.Length < epClear && epT > -.3f);
                bool movingOnTogether = ownSpeed > HoldSpeed && ownSpeed - P2.Dot(near[idx].Velocity, u) <= MinClosing;
                bool cleared = epT < -ReleaseBehind || side * -epLat >= epClear + ReleaseMargin || !intrudes || movingOnTogether;
                if (cleared) { if (clearSince < 0f) clearSince = now; }
                else clearSince = -1f;
                if (clearSince >= 0f && now - clearSince >= ReleasePersist) { releasing = true; r.Event = "release"; }
                else desired = Desired(side, epLat, epClear);
            }
            else if (idx < 0 && activeId != -1 && !releasing) { releasing = true; r.Event = "release"; }

            float rate = desired > offset ? rateIn : RateOut;
            if (releasing || Lane(terrain, self, u, left, length, side, desired))
            {
                if (!releasing && idx >= 0)
                    pass = self + u * Math.Max(epT, MinAhead) + left * (side * Math.Max(offset, Approach(offset, desired, rateIn * dt)));
                offset = Approach(offset, desired, rate * dt);
                waitSince = -1f;
                r.AvoidedId = activeId; r.AvoidedLeader = activeLeader; r.Side = side; r.Offset = offset;
                if (releasing)
                {
                    if (offset <= .05f) { Reset(); r.Offset = 0f; r.Side = 0; return r; }
                    r.Mode = SpacingMode.Travel;                  // easing back onto the walk
                    r.Shift = left * (side * offset);
                    return r;
                }
                r.Mode = SpacingMode.Sidestep;
                r.Point = pass;
                r.Shift = pass - aim;
                return r;
            }

            // 5. 1.0.32.8b: no lane on the locked side while the party member still blocks the corridor.
            //    Never hand back into the known collision. Wait first; then re-evaluate both lanes; otherwise stay put.
            r.AvoidedId = activeId; r.AvoidedLeader = activeLeader; r.Side = side; r.Offset = offset;
            if (waitSince < 0f) { waitSince = now; r.Event = "blocked"; }
            if (now - waitSince >= WaitMax && Lane(terrain, self, u, left, length, -side, Desired(-side, epLat, epClear)))
            {
                side = -side;                                         // the other lane opened: take it
                offset = 0f; waitSince = -1f; r.Side = side; r.Event = "switch";
                r.Mode = SpacingMode.Sidestep;
                r.Point = self + u * Math.Max(epT, MinAhead) + left * (side * Math.Min(Desired(side, epLat, epClear), rateIn * dt));
                r.Shift = r.Point - aim;
                return r;
            }
            bool rightOfWay = idx >= 0 && HasRightOfWay(mySlot, SelfId, near[idx]);
            if (rightOfWay)
            {
                // the other companion gives way; keep walking but stop short of contact rather than push
                if (epT > 0f && epT < ContactMate + RightOfWayStop) { r.Mode = SpacingMode.Wait; r.Point = self; return r; }
                r.Mode = SpacingMode.Travel;
                return r;
            }
            if (idx >= 0 && !near[idx].IsLeader)
            {
                // yielding to a companion that is walking at us: back out of its way if there is room behind
                P2 rel = near[idx].Position - self;
                float d = rel.Length;
                P2 away = d < 1e-3f ? u * -1f : rel * (-1f / d);
                bool comingAtUs = P2.Dot(near[idx].Velocity, away) > MinClosing;
                if (comingAtUs && d < epClear + GiveWayRange && (terrain == null || terrain.Clear(self, self + away * GiveWayStep)))
                {
                    r.Mode = SpacingMode.BackOff; r.Point = self + away * GiveWayStep; r.Shift = away;
                    if (r.Event == null) r.Event = "giveway";
                    return r;
                }
            }
            r.Mode = SpacingMode.Wait;                                // leader, or nowhere to go: remain stopped on this side
            r.Point = self;
            return r;
        }

        float Desired(int s, float lat, float clear) { return Clamp(s * lat + clear + PassMargin, MinOffset, MaxOffset); }

        static bool Lane(ISpacingTerrain terrain, P2 self, P2 u, P2 left, float length, int s, float offsetWanted)
        {
            return terrain == null || terrain.Clear(self, self + left * (s * offsetWanted) + u * Math.Max(1.5f, Math.Min(length, 3f)));
        }

        /// <summary>Deterministic right of way between companions: the lower formation slot; if either slot is unknown
        /// or they are equal, the lower identity. The leader always has right of way.</summary>
        public static bool HasRightOfWay(int mySlot, int myId, SpacingNeighbor other)
        {
            if (other.IsLeader) return false;
            if (mySlot >= 0 && other.Slot >= 0 && mySlot != other.Slot) return mySlot < other.Slot;
            return myId < other.Id;
        }

        static bool Spaced(P2 self, SpacingNeighbor[] near, int count, bool leaderInMenu, int mySlot)
        {
            for (int i = 0; i < count; i++)
            {
                float d = P2.Distance(self, near[i].Position);
                if (near[i].IsLeader) { if (d < Clearance(true, leaderInMenu)) return false; continue; }
                // companions: the one with the higher slot gives way, so two crowded companions never both move
                if (d < MateClear && (mySlot < 0 || near[i].Slot < 0 || near[i].Slot < mySlot)) return false;
            }
            return true;
        }

        static int Find(SpacingNeighbor[] near, int count, int id)
        {
            for (int i = 0; i < count; i++) if (near[i].Id == id) return i;
            return -1;
        }

        static float Clamp(float v, float lo, float hi) { return v < lo ? lo : v > hi ? hi : v; }
        static float Approach(float v, float target, float step)
        {
            if (v < target) return Math.Min(target, v + step);
            return Math.Max(target, v - step);
        }
    }

    /// <summary>Close-contact incidents (telemetry): closer than ContactLeader / ContactMate, with duration.</summary>
    public sealed class ContactTracker
    {
        readonly int[] ids = new int[PartySpacing.MaxNeighbors];
        readonly float[] since = new float[PartySpacing.MaxNeighbors];
        int open;
        public int Incidents { get; private set; }
        public float TotalSeconds { get; private set; }

        /// <summary>Returns the id of an incident that just ended (and its duration), or -1.</summary>
        public int Step(float now, P2 self, SpacingNeighbor[] near, int count, out float endedSeconds, out int started)
        {
            endedSeconds = 0f; started = -1;
            int ended = -1;
            // close finished incidents
            for (int k = open - 1; k >= 0; k--)
            {
                int i = IndexOf(near, count, ids[k]);
                bool still = i >= 0 && P2.Distance(self, near[i].Position) < (near[i].IsLeader ? PartySpacing.ContactLeader : PartySpacing.ContactMate);
                if (still) continue;
                endedSeconds = now - since[k]; ended = ids[k]; TotalSeconds += endedSeconds;
                open--; ids[k] = ids[open]; since[k] = since[open];
            }
            for (int i = 0; i < count && open < ids.Length; i++)
            {
                float d = P2.Distance(self, near[i].Position);
                if (d >= (near[i].IsLeader ? PartySpacing.ContactLeader : PartySpacing.ContactMate)) continue;
                bool known = false;
                for (int k = 0; k < open; k++) if (ids[k] == near[i].Id) { known = true; break; }
                if (known) continue;
                ids[open] = near[i].Id; since[open] = now; open++; Incidents++; started = near[i].Id;
            }
            return ended;
        }

        static int IndexOf(SpacingNeighbor[] near, int count, int id)
        {
            for (int i = 0; i < count; i++) if (near[i].Id == id) return i;
            return -1;
        }
    }
}
