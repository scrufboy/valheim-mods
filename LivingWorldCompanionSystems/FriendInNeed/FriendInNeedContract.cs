using System;
using System.Globalization;

namespace LivingWorld.CompanionSystems.FriendInNeed
{
    public enum FriendInNeedPhase { Dormant, Alerting, YieldedToCombat, Resolved }
    public enum DangerSubject { Camp, KnownFriend }
    public enum ResolutionKind { None, Safe, Defeated, Lost, Expired }

    [Flags]
    public enum ThreatEvidence
    {
        None = 0,
        VisibleHostile = 1,
        HostileTargetingProtectedActor = 2,
        AttributedDamage = 4,
        CampDamage = 8,
        ProtectedActorIncapacitated = 16
    }

    public enum FriendInNeedCommandKind
    {
        None,
        PlayExistingSurprisedVoice,
        ShowDangerSignal,
        ShowFollowUpSignal,
        ClearDangerSignal,
        YieldToNativeCombat,
        Persist
    }

    public sealed class FriendInNeedCommand
    {
        public FriendInNeedCommandKind Kind;
        public string EventId;
        public string SubjectId;
        public string VoicePool;
        public ThreatEvidence Evidence;
    }

    public sealed class FriendInNeedState
    {
        public const string Schema = "friend-in-need-v1";
        public string EventId = "";
        public string SubjectId = "";
        public DangerSubject Subject;
        public FriendInNeedPhase Phase;
        public ThreatEvidence Evidence;
        public int FollowUpsEmitted;
        public double LastSignalAt;
        public bool InitialVoiceEmitted;
        public ResolutionKind Resolution;

        public FriendInNeedState Copy()
        {
            return (FriendInNeedState)MemberwiseClone();
        }
    }

    public sealed class FriendInNeedResult
    {
        public FriendInNeedState State;
        public FriendInNeedCommand[] Commands;
    }

    public static class FriendInNeedContract
    {
        public const string ExistingSurprisedVoicePool = "lwc.voice.surprised.existing";
        public const int MaxFollowUps = 1;
        public const double FollowUpIntervalSeconds = 8.0;
        public const double AlertLifetimeSeconds = 45.0;
        const int MaxRecordLength = 512;

        public static FriendInNeedResult Witness(
            FriendInNeedState current, string eventId, DangerSubject subject, string subjectId,
            ThreatEvidence evidence, double now, bool nativeCombatCommitted)
        {
            if (!ValidClock(now) || !ValidId(eventId) || !ValidId(subjectId) ||
                !Enum.IsDefined(typeof(DangerSubject), subject) || !ValidEvidence(evidence))
                return NoChange(current);

            // Visibility alone is peaceful awareness. This alert begins only for
            // committed danger or a concrete harmful event.
            if (current == null && !IsCommittedOrMaterial(evidence)) return NoChange(current);

            if (current != null && current.EventId == eventId)
            {
                if (current.Phase == FriendInNeedPhase.Resolved) return NoChange(current);
                if (nativeCombatCommitted) return CommitCombat(current, evidence, now);
                FriendInNeedState refreshed = current.Copy();
                refreshed.Evidence |= evidence;
                return Changed(refreshed, Command(FriendInNeedCommandKind.Persist, refreshed));
            }

            // A single owner never replaces an unresolved incident with a new one.
            if (current != null && (current.Phase == FriendInNeedPhase.Alerting ||
                current.Phase == FriendInNeedPhase.YieldedToCombat)) return NoChange(current);

            FriendInNeedState next = new FriendInNeedState
            {
                EventId = eventId,
                Subject = subject,
                SubjectId = subjectId,
                Phase = nativeCombatCommitted ? FriendInNeedPhase.YieldedToCombat : FriendInNeedPhase.Alerting,
                Evidence = evidence,
                LastSignalAt = now,
                InitialVoiceEmitted = true
            };

            if (nativeCombatCommitted)
                return Changed(next,
                    Command(FriendInNeedCommandKind.ShowDangerSignal, next),
                    Command(FriendInNeedCommandKind.YieldToNativeCombat, next),
                    Command(FriendInNeedCommandKind.Persist, next));

            return Changed(next,
                Command(FriendInNeedCommandKind.PlayExistingSurprisedVoice, next),
                Command(FriendInNeedCommandKind.ShowDangerSignal, next),
                Command(FriendInNeedCommandKind.Persist, next));
        }

        public static FriendInNeedResult Tick(FriendInNeedState current, double now, bool evidenceStillFresh,
            ThreatEvidence newMaterialEvidence, bool nativeCombatCommitted)
        {
            if (current == null || !ValidClock(now) || current.Phase != FriendInNeedPhase.Alerting)
                return NoChange(current);
            if (nativeCombatCommitted) return CommitCombat(current, ThreatEvidence.None, now);
            if (!evidenceStillFresh || now - current.LastSignalAt >= AlertLifetimeSeconds)
                return Resolve(current, ResolutionKind.Expired, now);
            if (newMaterialEvidence != ThreatEvidence.None &&
                (!ValidEvidence(newMaterialEvidence) || !IsMaterialFollowUp(newMaterialEvidence)))
                return NoChange(current);
            if (now < current.LastSignalAt || current.FollowUpsEmitted >= MaxFollowUps ||
                now - current.LastSignalAt < FollowUpIntervalSeconds ||
                newMaterialEvidence == ThreatEvidence.None) return NoChange(current);

            FriendInNeedState next = current.Copy();
            next.FollowUpsEmitted++;
            next.LastSignalAt = now;
            next.Evidence |= newMaterialEvidence;
            return Changed(next,
                Command(FriendInNeedCommandKind.ShowFollowUpSignal, next),
                Command(FriendInNeedCommandKind.Persist, next));
        }

        public static FriendInNeedResult Resolve(FriendInNeedState current, ResolutionKind resolution, double now)
        {
            if (current == null || !ValidClock(now) || resolution == ResolutionKind.None ||
                !Enum.IsDefined(typeof(ResolutionKind), resolution) || current.Phase == FriendInNeedPhase.Resolved)
                return NoChange(current);
            FriendInNeedState next = current.Copy();
            next.Phase = FriendInNeedPhase.Resolved;
            next.Resolution = resolution;
            next.LastSignalAt = now;
            return Changed(next,
                Command(FriendInNeedCommandKind.ClearDangerSignal, next),
                Command(FriendInNeedCommandKind.Persist, next));
        }

        static FriendInNeedResult CommitCombat(FriendInNeedState current, ThreatEvidence evidence, double now)
        {
            if (current.Phase == FriendInNeedPhase.YieldedToCombat) return NoChange(current);
            FriendInNeedState next = current.Copy();
            next.Phase = FriendInNeedPhase.YieldedToCombat;
            next.Evidence |= evidence;
            next.LastSignalAt = now;
            return Changed(next,
                Command(FriendInNeedCommandKind.YieldToNativeCombat, next),
                Command(FriendInNeedCommandKind.Persist, next));
        }

        public static string Write(FriendInNeedState state)
        {
            if (!Valid(state)) throw new ArgumentException("Invalid Friend in Need state", "state");
            return String.Join("|", FriendInNeedState.Schema, Escape(state.EventId),
                ((int)state.Subject).ToString(CultureInfo.InvariantCulture), Escape(state.SubjectId),
                ((int)state.Phase).ToString(CultureInfo.InvariantCulture),
                ((int)state.Evidence).ToString(CultureInfo.InvariantCulture),
                state.FollowUpsEmitted.ToString(CultureInfo.InvariantCulture),
                state.LastSignalAt.ToString("R", CultureInfo.InvariantCulture),
                state.InitialVoiceEmitted ? "1" : "0",
                ((int)state.Resolution).ToString(CultureInfo.InvariantCulture));
        }

        public static bool TryRead(string record, out FriendInNeedState state)
        {
            state = null;
            if (String.IsNullOrEmpty(record) || record.Length > MaxRecordLength) return false;
            string[] p = record.Split('|');
            if (p.Length != 10 || p[0] != FriendInNeedState.Schema) return false;
            int subject, phase, evidence, followUps, resolution;
            double lastSignal;
            if (!Int32.TryParse(p[2], NumberStyles.None, CultureInfo.InvariantCulture, out subject) ||
                !Int32.TryParse(p[4], NumberStyles.None, CultureInfo.InvariantCulture, out phase) ||
                !Int32.TryParse(p[5], NumberStyles.None, CultureInfo.InvariantCulture, out evidence) ||
                !Int32.TryParse(p[6], NumberStyles.None, CultureInfo.InvariantCulture, out followUps) ||
                !Double.TryParse(p[7], NumberStyles.Float, CultureInfo.InvariantCulture, out lastSignal) ||
                !Int32.TryParse(p[9], NumberStyles.None, CultureInfo.InvariantCulture, out resolution) ||
                (p[8] != "0" && p[8] != "1")) return false;
            FriendInNeedState candidate = new FriendInNeedState
            {
                EventId = Unescape(p[1]), Subject = (DangerSubject)subject, SubjectId = Unescape(p[3]),
                Phase = (FriendInNeedPhase)phase, Evidence = (ThreatEvidence)evidence,
                FollowUpsEmitted = followUps, LastSignalAt = lastSignal,
                InitialVoiceEmitted = p[8] == "1", Resolution = (ResolutionKind)resolution
            };
            if (!Valid(candidate)) return false;
            state = candidate;
            return true;
        }

        public static bool Valid(FriendInNeedState state)
        {
            if (state == null || !ValidId(state.EventId) || !ValidId(state.SubjectId) ||
                !Enum.IsDefined(typeof(DangerSubject), state.Subject) ||
                !Enum.IsDefined(typeof(FriendInNeedPhase), state.Phase) || !ValidEvidence(state.Evidence) ||
                !Enum.IsDefined(typeof(ResolutionKind), state.Resolution) || !ValidClock(state.LastSignalAt) ||
                state.FollowUpsEmitted < 0 || state.FollowUpsEmitted > MaxFollowUps) return false;
            if (!state.InitialVoiceEmitted || state.Phase == FriendInNeedPhase.Dormant) return false;
            return state.Phase == FriendInNeedPhase.Resolved
                ? state.Resolution != ResolutionKind.None
                : state.Resolution == ResolutionKind.None;
        }

        static bool ValidEvidence(ThreatEvidence evidence)
        {
            const ThreatEvidence all = ThreatEvidence.VisibleHostile |
                ThreatEvidence.HostileTargetingProtectedActor | ThreatEvidence.AttributedDamage |
                ThreatEvidence.CampDamage | ThreatEvidence.ProtectedActorIncapacitated;
            return evidence != ThreatEvidence.None && (evidence & ~all) == 0;
        }

        static bool IsCommittedOrMaterial(ThreatEvidence evidence)
        {
            return (evidence & (ThreatEvidence.HostileTargetingProtectedActor |
                ThreatEvidence.AttributedDamage | ThreatEvidence.CampDamage |
                ThreatEvidence.ProtectedActorIncapacitated)) != 0;
        }

        static bool IsMaterialFollowUp(ThreatEvidence evidence)
        {
            return (evidence & (ThreatEvidence.AttributedDamage | ThreatEvidence.CampDamage |
                ThreatEvidence.ProtectedActorIncapacitated)) != 0;
        }

        static bool ValidClock(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value) && value >= 0; }
        static bool ValidId(string value)
        {
            if (String.IsNullOrEmpty(value) || value.Length > 96) return false;
            for (int i = 0; i < value.Length; i++)
                if (Char.IsControl(value[i]) || value[i] == '|') return false;
            return true;
        }
        static string Escape(string value) { return Uri.EscapeDataString(value); }
        static string Unescape(string value) { try { return Uri.UnescapeDataString(value); } catch { return ""; } }
        static FriendInNeedCommand Command(FriendInNeedCommandKind kind, FriendInNeedState state)
        {
            return new FriendInNeedCommand { Kind = kind, EventId = state.EventId, SubjectId = state.SubjectId,
                Evidence = state.Evidence, VoicePool = kind == FriendInNeedCommandKind.PlayExistingSurprisedVoice
                    ? ExistingSurprisedVoicePool : null };
        }
        static FriendInNeedResult Changed(FriendInNeedState state, params FriendInNeedCommand[] commands)
        { return new FriendInNeedResult { State = state, Commands = commands }; }
        static FriendInNeedResult NoChange(FriendInNeedState state)
        { return new FriendInNeedResult { State = state, Commands = new FriendInNeedCommand[0] }; }
    }
}
