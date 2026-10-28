using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.EncounterVoice
{
    public enum CueApprovalMode { Normal, Audition }
    public enum PersonaGender { Female, Male }
    public enum CueSuppression
    {
        None, InvalidRequest, UnknownLine, CombatOwnsPresentation, AlertOwnsPresentation,
        ApprovedSurpriseRequiresAlertOwnership, OtherSpeakerOwnsPresentation, CampCooldown, SpeakerCooldown, IncidentVoiceAlreadyEmitted,
        ConversationalVoiceUnapproved
    }

    public sealed class EncounterCueDefinition
    {
        public string LineId = "";
        public string SafeEmote = "";
        public string FemaleCandidateCue = "";
        public string MaleCandidateCue = "";
        public bool UsesApprovedSurprisePool;
    }

    public sealed class EncounterCueRequest
    {
        public string LineId = "";
        public string SpeakerId = "";
        public string PersonaId = "";
        public PersonaGender Gender;
        public string ReadableText = "";
        public string IncidentId = "";
        public double Now;
        public bool NativeCombatOwnsPresentation;
        public bool AlertOwnsPresentation;
        public CueApprovalMode ApprovalMode;
    }

    public sealed class EncounterCueState
    {
        public string ActiveSpeakerId = "";
        public double SpeakerLeaseUntil;
        public double LastCampCueAt = Double.NegativeInfinity;
        public readonly Dictionary<string, double> LastSpeakerCueAt = new Dictionary<string, double>(StringComparer.Ordinal);
        public readonly HashSet<string> VoicedIncidentIds = new HashSet<string>(StringComparer.Ordinal);

        public EncounterCueState Copy()
        {
            var copy = new EncounterCueState { ActiveSpeakerId = ActiveSpeakerId,
                SpeakerLeaseUntil = SpeakerLeaseUntil, LastCampCueAt = LastCampCueAt };
            foreach (var item in LastSpeakerCueAt) copy.LastSpeakerCueAt.Add(item.Key, item.Value);
            foreach (var item in VoicedIncidentIds) copy.VoicedIncidentIds.Add(item);
            return copy;
        }
    }

    public sealed class EncounterCueResult
    {
        public EncounterCueState State;
        public string LineId;
        public string SpeakerId;
        public string PersonaId;
        public string ReadableText;
        public string SafeEmote;
        public string VoiceCue;
        public bool VoiceIsAuditionCandidate;
        public CueSuppression Suppression;
    }

    public static class EncounterCuePresentationPolicy
    {
        public const string ExistingSurprisedVoicePool = "lwc.voice.surprised.existing";
        public const double CampCooldownSeconds = 3.0;
        public const double SpeakerCooldownSeconds = 8.0;
        public const double SpeakerLeaseSeconds = 2.0;

        static readonly HashSet<string> SafeEmotes = new HashSet<string>(StringComparer.Ordinal)
        {
            "wave", "halt", "point", "comehere", "bow", "nonono", "relax", "open_hand_or_bow"
        };
        static readonly HashSet<string> ApprovedSurpriseLineIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "splitash.shock.edda_down", "plains.shock.ketil"
        };

        public static EncounterCueResult Present(EncounterCueState current, EncounterCueRequest request,
            IDictionary<string, EncounterCueDefinition> definitions)
        {
            var state = current ?? new EncounterCueState();
            if (!ValidRequest(request) || definitions == null)
                return Invalid(state, request, CueSuppression.InvalidRequest);

            EncounterCueDefinition definition;
            if (!definitions.TryGetValue(request.LineId, out definition) || definition == null ||
                definition.LineId != request.LineId || !SafeEmotes.Contains(definition.SafeEmote) ||
                (definition.UsesApprovedSurprisePool && !ApprovedSurpriseLineIds.Contains(request.LineId)))
                return TextOnly(state, request, CueSuppression.UnknownLine);

            // Readable authored text survives cue suppression. The policy never
            // synthesizes, edits, or infers spoken words from a nonverbal asset.
            if (request.NativeCombatOwnsPresentation)
                return TextOnly(state, request, CueSuppression.CombatOwnsPresentation);
            if (request.AlertOwnsPresentation && !definition.UsesApprovedSurprisePool)
                return TextOnly(state, request, CueSuppression.AlertOwnsPresentation);
            if (definition.UsesApprovedSurprisePool && !request.AlertOwnsPresentation)
                return TextOnly(state, request, CueSuppression.ApprovedSurpriseRequiresAlertOwnership);
            if (definition.UsesApprovedSurprisePool && String.IsNullOrEmpty(request.IncidentId))
                return TextOnly(state, request, CueSuppression.InvalidRequest);
            if (!String.IsNullOrEmpty(state.ActiveSpeakerId) && state.ActiveSpeakerId != request.SpeakerId &&
                request.Now < state.SpeakerLeaseUntil)
                return TextOnly(state, request, CueSuppression.OtherSpeakerOwnsPresentation);
            if (request.Now - state.LastCampCueAt < CampCooldownSeconds)
                return TextOnly(state, request, CueSuppression.CampCooldown);

            double lastSpeaker;
            if (state.LastSpeakerCueAt.TryGetValue(request.SpeakerId, out lastSpeaker) &&
                request.Now - lastSpeaker < SpeakerCooldownSeconds)
                return TextOnly(state, request, CueSuppression.SpeakerCooldown);

            var next = state.Copy();
            next.ActiveSpeakerId = request.SpeakerId;
            next.SpeakerLeaseUntil = request.Now + SpeakerLeaseSeconds;
            next.LastCampCueAt = request.Now;
            next.LastSpeakerCueAt[request.SpeakerId] = request.Now;

            if (definition.UsesApprovedSurprisePool)
            {
                if (state.VoicedIncidentIds.Contains(request.IncidentId))
                    return TextAndEmote(next, request, definition.SafeEmote, null, false,
                        CueSuppression.IncidentVoiceAlreadyEmitted);
                next.VoicedIncidentIds.Add(request.IncidentId);
                return TextAndEmote(next, request, definition.SafeEmote,
                    ExistingSurprisedVoicePool, false, CueSuppression.None);
            }

            if (request.ApprovalMode == CueApprovalMode.Normal)
                return TextAndEmote(next, request, definition.SafeEmote, null, false,
                    CueSuppression.ConversationalVoiceUnapproved);

            var candidate = request.Gender == PersonaGender.Female
                ? definition.FemaleCandidateCue : definition.MaleCandidateCue;
            return TextAndEmote(next, request, definition.SafeEmote,
                String.IsNullOrEmpty(candidate) ? null : candidate,
                !String.IsNullOrEmpty(candidate), String.IsNullOrEmpty(candidate)
                    ? CueSuppression.ConversationalVoiceUnapproved : CueSuppression.None);
        }

        static bool ValidRequest(EncounterCueRequest request)
        {
            return request != null && ValidId(request.LineId) && ValidId(request.SpeakerId) &&
                ValidId(request.PersonaId) && !String.IsNullOrWhiteSpace(request.ReadableText) &&
                request.ReadableText.Length <= 512 && ValidClock(request.Now) &&
                Enum.IsDefined(typeof(PersonaGender), request.Gender) &&
                Enum.IsDefined(typeof(CueApprovalMode), request.ApprovalMode);
        }

        static bool ValidId(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Length > 128) return false;
            for (var i = 0; i < value.Length; i++) if (Char.IsControl(value[i])) return false;
            return true;
        }

        static bool ValidClock(double value)
        { return !Double.IsNaN(value) && !Double.IsInfinity(value) && value >= 0; }

        static EncounterCueResult Invalid(EncounterCueState state, EncounterCueRequest request, CueSuppression why)
        {
            return new EncounterCueResult { State = state, LineId = request == null ? null : request.LineId,
                SpeakerId = request == null ? null : request.SpeakerId,
                PersonaId = request == null ? null : request.PersonaId,
                ReadableText = request == null ? null : request.ReadableText, Suppression = why };
        }

        static EncounterCueResult TextOnly(EncounterCueState state, EncounterCueRequest request, CueSuppression why)
        { return TextAndEmote(state, request, null, null, false, why); }

        static EncounterCueResult TextAndEmote(EncounterCueState state, EncounterCueRequest request,
            string emote, string voice, bool audition, CueSuppression why)
        {
            return new EncounterCueResult { State = state, LineId = request.LineId,
                SpeakerId = request.SpeakerId, PersonaId = request.PersonaId,
                ReadableText = request.ReadableText, SafeEmote = emote, VoiceCue = voice,
                VoiceIsAuditionCandidate = audition, Suppression = why };
        }
    }
}
