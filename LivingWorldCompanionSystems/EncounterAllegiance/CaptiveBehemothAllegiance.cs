using System;
using System.Collections.Generic;
using System.Globalization;

namespace LivingWorld.CompanionSystems.EncounterAllegiance
{
    public enum CaptiveBehemothMode
    {
        DormantBound,
        Controlled,
        ReleasedFrenzy,
        ReleasedUndead,
        Dead
    }

    public enum CaptiveBehemothAction
    {
        PhysicalDeployment,
        BindingCompleted,
        SpellBroken,
        TimeAdvanced,
        DeathConfirmed
    }

    public enum EncounterActorRole
    {
        Player,
        ControllerShaman,
        NeckCaptor,
        CaptorMinion,
        Undead,
        Other
    }

    public enum InstanceDisposition { Friendly, Hostile }

    public sealed class CaptiveBehemothRecord
    {
        public string EncounterId;
        public string BehemothId;
        public string ControllerId;
        public CaptiveBehemothMode Mode;
        public long Revision;
        public long BindingRevision;
        public long ReleaseEpoch;
        public float FrenzyUntil;
        public readonly HashSet<string> AppliedEventIds = new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class CaptiveBehemothEvent
    {
        public string EventId;
        public CaptiveBehemothAction Action;
        public bool IsAuthority;
        public float WorldTime;
        public float FrenzyDuration;
        public string ControllerId;
        public long BindingRevision;
        public bool VerifiedBindingReceipt;
        public bool VerifiedSpellBreak;
    }

    public sealed class CaptiveBehemothResult
    {
        public bool Accepted;
        public bool Replayed;
        public string Reason;
        public CaptiveBehemothRecord Record;
        public bool ClearControlPresentation;
        public bool StartFrenzyPresentation;
        public bool RestoreUndeadPresentation;
        public bool RestoreControlPresentation;
        public bool ClearAllPresentation;
    }

    public static class CaptiveBehemothAllegiancePolicy
    {
        public const string Schema = "lwc-captive-behemoth-v1";

        public static CaptiveBehemothRecord Create(string encounterId, string behemothId)
        {
            if (String.IsNullOrEmpty(encounterId) || String.IsNullOrEmpty(behemothId))
                throw new ArgumentException("Stable encounter and Behemoth IDs are required");
            return new CaptiveBehemothRecord { EncounterId = encounterId, BehemothId = behemothId,
                Mode = CaptiveBehemothMode.DormantBound, FrenzyUntil = -1f };
        }

        public static CaptiveBehemothResult Apply(CaptiveBehemothRecord current, CaptiveBehemothEvent fact)
        {
            if (!Valid(current) || fact == null || String.IsNullOrEmpty(fact.EventId) || !Finite(fact.WorldTime))
                return Denied(current, "invalid-input");
            if (!fact.IsAuthority) return Denied(current, "authority-required");
            if (current.Mode == CaptiveBehemothMode.Dead)
            {
                if (fact.Action == CaptiveBehemothAction.DeathConfirmed && current.AppliedEventIds.Contains(fact.EventId))
                    return Result(current, "already-applied", true, false, false, false, false, false);
                return Denied(current, "terminal-death");
            }
            if (current.AppliedEventIds.Contains(fact.EventId))
                return Result(current, "already-applied", true, false, false, false, false, false);

            CaptiveBehemothRecord next = Clone(current);
            bool clearControl = false, frenzy = false, undead = false, control = false, clearAll = false;
            switch (fact.Action)
            {
                case CaptiveBehemothAction.PhysicalDeployment:
                    if (next.Mode != CaptiveBehemothMode.Controlled)
                        return Denied(current, "deployment-requires-control");
                    // Physical chain deployment never means the spell broke.
                    break;
                case CaptiveBehemothAction.BindingCompleted:
                    if (!fact.VerifiedBindingReceipt || String.IsNullOrEmpty(fact.ControllerId) ||
                        fact.BindingRevision <= next.BindingRevision)
                        return Denied(current, "invalid-or-stale-binding-receipt");
                    next.ControllerId = fact.ControllerId;
                    next.BindingRevision = fact.BindingRevision;
                    next.Mode = CaptiveBehemothMode.Controlled;
                    next.FrenzyUntil = -1f;
                    control = true;
                    break;
                case CaptiveBehemothAction.SpellBroken:
                    if (!fact.VerifiedSpellBreak || next.Mode != CaptiveBehemothMode.Controlled ||
                        !Finite(fact.FrenzyDuration) || fact.FrenzyDuration <= 0f)
                        return Denied(current, "verified-controlled-break-and-bounded-duration-required");
                    next.Mode = CaptiveBehemothMode.ReleasedFrenzy;
                    next.ReleaseEpoch++;
                    next.FrenzyUntil = fact.WorldTime + fact.FrenzyDuration;
                    clearControl = true; frenzy = true;
                    break;
                case CaptiveBehemothAction.TimeAdvanced:
                    if (next.Mode != CaptiveBehemothMode.ReleasedFrenzy || fact.WorldTime < next.FrenzyUntil)
                        return Denied(current, "no-frenzy-expiry-due");
                    next.Mode = CaptiveBehemothMode.ReleasedUndead;
                    next.FrenzyUntil = -1f;
                    undead = true;
                    break;
                case CaptiveBehemothAction.DeathConfirmed:
                    if (next.Mode == CaptiveBehemothMode.Dead)
                        return Result(current, "already-dead", true, false, false, false, false, false);
                    next.Mode = CaptiveBehemothMode.Dead;
                    next.FrenzyUntil = -1f;
                    clearAll = true;
                    break;
                default:
                    return Denied(current, "unsupported-action");
            }
            next.AppliedEventIds.Add(fact.EventId);
            next.Revision++;
            return Result(next, "accepted", false, clearControl, frenzy, undead, control, clearAll);
        }

        public static bool TryResolve(CaptiveBehemothRecord record, string firstActorId, EncounterActorRole firstRole,
            string secondActorId, EncounterActorRole secondRole, out InstanceDisposition disposition)
        {
            disposition = InstanceDisposition.Friendly;
            if (!Valid(record) || record.Mode == CaptiveBehemothMode.Dead ||
                String.IsNullOrEmpty(firstActorId) || String.IsNullOrEmpty(secondActorId) || firstActorId == secondActorId)
                return false;
            bool firstBeast = firstActorId == record.BehemothId;
            bool secondBeast = secondActorId == record.BehemothId;
            if (firstBeast == secondBeast) return false;
            EncounterActorRole other = firstBeast ? secondRole : firstRole;
            switch (record.Mode)
            {
                case CaptiveBehemothMode.DormantBound:
                    if (Captor(other)) { disposition = InstanceDisposition.Friendly; return true; }
                    return false;
                case CaptiveBehemothMode.Controlled:
                    if (Captor(other)) { disposition = InstanceDisposition.Friendly; return true; }
                    if (other == EncounterActorRole.Player) { disposition = InstanceDisposition.Hostile; return true; }
                    return false;
                case CaptiveBehemothMode.ReleasedFrenzy:
                    disposition = InstanceDisposition.Hostile; return true;
                case CaptiveBehemothMode.ReleasedUndead:
                    if (other == EncounterActorRole.Undead) { disposition = InstanceDisposition.Friendly; return true; }
                    if (Captor(other) || other == EncounterActorRole.Player)
                    { disposition = InstanceDisposition.Hostile; return true; }
                    return false;
                default:
                    return false;
            }
        }

        static bool Captor(EncounterActorRole role)
        { return role == EncounterActorRole.ControllerShaman || role == EncounterActorRole.NeckCaptor || role == EncounterActorRole.CaptorMinion; }
        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }
        static bool Valid(CaptiveBehemothRecord value)
        { return value != null && !String.IsNullOrEmpty(value.EncounterId) && !String.IsNullOrEmpty(value.BehemothId) && value.Revision >= 0; }
        static CaptiveBehemothRecord Clone(CaptiveBehemothRecord value)
        {
            CaptiveBehemothRecord copy = new CaptiveBehemothRecord { EncounterId = value.EncounterId,
                BehemothId = value.BehemothId, ControllerId = value.ControllerId, Mode = value.Mode,
                Revision = value.Revision, BindingRevision = value.BindingRevision,
                ReleaseEpoch = value.ReleaseEpoch, FrenzyUntil = value.FrenzyUntil };
            foreach (string id in value.AppliedEventIds) copy.AppliedEventIds.Add(id);
            return copy;
        }
        static CaptiveBehemothResult Denied(CaptiveBehemothRecord record, string reason)
        { return new CaptiveBehemothResult { Accepted = false, Reason = reason, Record = record }; }
        static CaptiveBehemothResult Result(CaptiveBehemothRecord record, string reason, bool replayed,
            bool clearControl, bool frenzy, bool undead, bool control, bool clearAll)
        { return new CaptiveBehemothResult { Accepted = true, Replayed = replayed, Reason = reason, Record = record,
            ClearControlPresentation = clearControl, StartFrenzyPresentation = frenzy,
            RestoreUndeadPresentation = undead, RestoreControlPresentation = control,
            ClearAllPresentation = clearAll }; }
    }

    public static class CaptiveBehemothPersistence
    {
        public const string RecordKeyPrefix = "lwc_captive_behemoth_v1_";

        public static string Write(CaptiveBehemothRecord value)
        {
            if (value == null || String.IsNullOrEmpty(value.EncounterId) || String.IsNullOrEmpty(value.BehemothId))
                throw new ArgumentException("Invalid captive Behemoth record", "value");
            List<string> ids = new List<string>(value.AppliedEventIds); ids.Sort(StringComparer.Ordinal);
            for (int i = 0; i < ids.Count; i++) ids[i] = Uri.EscapeDataString(ids[i]);
            return String.Join("|", new[] { CaptiveBehemothAllegiancePolicy.Schema,
                Uri.EscapeDataString(value.EncounterId), Uri.EscapeDataString(value.BehemothId),
                Uri.EscapeDataString(value.ControllerId ?? ""), value.Mode.ToString(),
                value.Revision.ToString(CultureInfo.InvariantCulture), value.BindingRevision.ToString(CultureInfo.InvariantCulture),
                value.ReleaseEpoch.ToString(CultureInfo.InvariantCulture), value.FrenzyUntil.ToString("R", CultureInfo.InvariantCulture),
                String.Join(",", ids.ToArray()) });
        }

        public static bool TryRead(string text, out CaptiveBehemothRecord value)
        {
            value = null;
            if (String.IsNullOrEmpty(text) || text.Length > 8192) return false;
            try
            {
                string[] p = text.Split('|'); long revision, binding, epoch; float until; CaptiveBehemothMode mode;
                if (p.Length != 10 || p[0] != CaptiveBehemothAllegiancePolicy.Schema ||
                    !Enum.TryParse(p[4], false, out mode) ||
                    !Int64.TryParse(p[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out revision) ||
                    !Int64.TryParse(p[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out binding) ||
                    !Int64.TryParse(p[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out epoch) ||
                    !Single.TryParse(p[8], NumberStyles.Float, CultureInfo.InvariantCulture, out until) || !Finite(until)) return false;
                CaptiveBehemothRecord parsed = new CaptiveBehemothRecord { EncounterId = Uri.UnescapeDataString(p[1]),
                    BehemothId = Uri.UnescapeDataString(p[2]), ControllerId = Uri.UnescapeDataString(p[3]), Mode = mode,
                    Revision = revision, BindingRevision = binding, ReleaseEpoch = epoch, FrenzyUntil = until };
                if (String.IsNullOrEmpty(parsed.EncounterId) || String.IsNullOrEmpty(parsed.BehemothId) || revision < 0 || binding < 0 || epoch < 0)
                    return false;
                if (p[9].Length > 0) foreach (string encoded in p[9].Split(','))
                { string id = Uri.UnescapeDataString(encoded); if (String.IsNullOrEmpty(id) || !parsed.AppliedEventIds.Add(id)) return false; }
                value = parsed; return true;
            }
            catch (UriFormatException) { return false; }
        }

        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }
    }
}
