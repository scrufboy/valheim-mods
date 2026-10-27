using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// 1.0.32.8 party spacing, Unity side. READ-ONLY: gathers the bound leader and that leader's loaded companions
    /// (no scene scan), answers the terrain question for a sidestep lane, detects the leader's menus, and writes the
    /// SPACE telemetry. It never moves, steers, rotates or pushes anything and never sets a follow target: the
    /// traversal anchor is the only thing the result changes, and native Follow stays the sole movement writer.
    /// </summary>
    public sealed class PartySpacingProbe : ISpacingTerrain
    {
        const float NeighborRadius = 8f, LaneProbe = 3f, MaxStep = 1f;
        public readonly SpacingNeighbor[] Near = new SpacingNeighbor[PartySpacing.MaxNeighbors];
        readonly string[] names = new string[PartySpacing.MaxNeighbors];
        readonly RaycastHit[] hits = new RaycastHit[8];
        public int Count;
        float selfHeight;

        /// <summary>The local leader has a menu / map / inventory / chest / text box open (he is not steering).</summary>
        public static bool LeaderInMenu(Player leader)
        {
            if (leader == null || leader != Player.m_localPlayer) return false;
            return InventoryGui.IsVisible() || StoreGui.IsVisible() || Minimap.IsOpen() || Menu.IsVisible() ||
                TextInput.IsVisible() || global::Console.IsVisible() || Game.IsPaused() || leader.GetCurrentCraftingStation() != null;
        }

        public static int SlotSide(int slot) { return FormationLeases.LocalX(slot) < 0f ? 1 : FormationLeases.LocalX(slot) > 0f ? -1 : 0; }

        public static P2 Flat(Vector3 v) { return new P2(v.x, v.z); }

        /// <summary>Leader + loaded, living companions bound to the same leader, within 8 m. Bounded, allocation-free.</summary>
        public void Gather(NpcKernel self, Player leader)
        {
            Count = 0;
            if (self == null || leader == null) return;
            Vector3 me = self.transform.position;
            selfHeight = me.y;
            Add(0, true, leader.transform.position, leader.GetVelocity(), -1, "leader", me);
            NpcLeaderBinding mine = self.GetComponent<NpcLeaderBinding>();
            long leaderId = mine == null ? 0L : mine.LeaderId;
            foreach (NpcKernel k in NpcKernel.LiveSet)
            {
                if (Count >= Near.Length) break;
                if (k == null || k == self || k.Body == null || k.Body.Human == null || k.Body.Human.IsDead()) continue;
                NpcLeaderBinding b = k.GetComponent<NpcLeaderBinding>();
                if (b == null || leaderId == 0L || b.LeaderId != leaderId) continue;
                Add(k.GetInstanceID(), false, k.transform.position, k.Body.Human.GetVelocity(), k.Frame.Board.FormationSlot,
                    k.Body.Human.GetHoverName(), me);
            }
        }

        void Add(int id, bool leader, Vector3 position, Vector3 velocity, int slot, string name, Vector3 me)
        {
            if (Count >= Near.Length) return;
            Vector3 d = position - me;
            d.y = 0f;
            if (d.magnitude > NeighborRadius) return;
            Near[Count] = new SpacingNeighbor { Id = id, IsLeader = leader, Position = Flat(position), Velocity = Flat(velocity), Slot = slot };
            names[Count] = name;
            Count++;
        }

        public string NameOf(int id)
        {
            for (int i = 0; i < Count; i++) if (Near[i].Id == id) return names[i];
            return "?";
        }

        /// <summary>The first metres of a sidestep lane: standable ground (no steep slope, drop/climb over 1 m, water)
        /// and a clear straight line at chest height (characters ignored).</summary>
        public bool Clear(P2 from, P2 to)
        {
            Vector3 a = new Vector3(from.X, selfHeight, from.Z);
            Vector3 dir = new Vector3(to.X - from.X, 0f, to.Z - from.Z);
            float length = Mathf.Min(dir.magnitude, LaneProbe);
            if (length < .05f) return true;
            dir.Normalize();
            Vector3 end = a + dir * length;
            float ground;
            if (!Ground(end, out ground) || Mathf.Abs(ground - selfHeight) > MaxStep) return false;
            if (ZoneSystem.instance != null && ground < ZoneSystem.instance.m_waterLevel - .3f) return false;
            int count = Physics.RaycastNonAlloc(a + Vector3.up * .8f, dir, hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (hits[i].collider != null && hits[i].collider.GetComponentInParent<Character>() == null) return false;
            return true;
        }

        bool Ground(Vector3 point, out float height)
        {
            height = point.y;
            int count = Physics.RaycastNonAlloc(point + Vector3.up * 1.5f, Vector3.down, hits, 3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            float closest = Single.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.collider.GetComponentInParent<Character>() != null || hit.normal.y < .7f ||
                    hit.distance >= closest) continue;
                closest = hit.distance;
                height = hit.point.y;
                found = true;
            }
            return found;
        }
    }

    /// <summary>SPACE telemetry (DevSpacingLog): episodes, waits, back-offs, close contacts, one summary a minute.</summary>
    public sealed class PartySpacingLog
    {
        readonly ContactTracker contacts = new ContactTracker();
        float episodeStart, nextSummary;
        int episodes;

        public void Write(NpcKernel self, PartySpacingProbe probe, SpacingResult r, int slot, float now)
        {
            if (!CorePlugin.DevSpacingLog || self == null || self.Body == null || self.Body.Human == null) return;
            string who = self.Body.Human.GetHoverName();
            string avoided = r.AvoidedId < 0 ? "-" : r.AvoidedLeader ? "leader" : probe.NameOf(r.AvoidedId) + "#" + r.AvoidedId;
            string t = " t=" + now.ToString("0.00");
            if (r.Event == "enter") { episodes++; episodeStart = now; }
            if (r.Event != null)
                CorePlugin.Report("SPACE-" + r.Event.ToUpperInvariant() + " " + who + " slot=" + slot + " mode=" + r.Mode +
                    " avoid=" + avoided + " side=" + (r.Side > 0 ? "left" : r.Side < 0 ? "right" : "-") +
                    " offset=" + r.Offset.ToString("0.00") + " region=" + r.RegionDistance.ToString("0.00") +
                    (r.Event == "release" ? " lasted=" + (now - episodeStart).ToString("0.00") : "") + t);
            float ended; int started;
            int endedId = contacts.Step(now, PartySpacingProbe.Flat(self.transform.position), probe.Near, probe.Count, out ended, out started);
            if (started >= 0) CorePlugin.Report("SPACE-CONTACT " + who + " with=" + (started == 0 ? "leader" : probe.NameOf(started)) + " begin" + t);
            if (endedId >= 0) CorePlugin.Report("SPACE-CONTACT " + who + " with=" + (endedId == 0 ? "leader" : "#" + endedId) +
                " end lasted=" + ended.ToString("0.00") + t);
            if (now >= nextSummary)
            {
                if (nextSummary > 0f)
                    CorePlugin.Report("SPACE-SUMMARY " + who + " episodes=" + episodes + " contacts=" + contacts.Incidents +
                        " contactSeconds=" + contacts.TotalSeconds.ToString("0.0") + t);
                nextSummary = now + 60f;
            }
        }
    }
}
