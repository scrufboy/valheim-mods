using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.EncounterVoice;

class EncounterCuePresentationPolicyTests
{
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception("FAIL " + label); }

    static Dictionary<string, EncounterCueDefinition> Definitions()
    {
        return new Dictionary<string, EncounterCueDefinition>(StringComparer.Ordinal)
        {
            { "splitash.request.meal_offer", new EncounterCueDefinition { LineId = "splitash.request.meal_offer",
                SafeEmote = "comehere", FemaleCandidateCue = "uf_asking_thinking05", MaleCandidateCue = "gm_mb_asking_sigh09" } },
            { "splitash.invitation.npc", new EncounterCueDefinition { LineId = "splitash.invitation.npc",
                SafeEmote = "open_hand_or_bow", FemaleCandidateCue = "uf_offer_confirm02", MaleCandidateCue = "gm_mb_offer_release03" } },
            { "splitash.shock.edda_down", new EncounterCueDefinition { LineId = "splitash.shock.edda_down",
                SafeEmote = "point", UsesApprovedSurprisePool = true } },
            { "contact.friendly.banked_fire", new EncounterCueDefinition { LineId = "contact.friendly.banked_fire",
                SafeEmote = "halt", FemaleCandidateCue = "uf_suspicion_investigate01", MaleCandidateCue = "gm_mb_suspicion_snort01" } },
            { "contact.travel.offer", new EncounterCueDefinition { LineId = "contact.travel.offer",
                SafeEmote = "open_hand_or_bow", FemaleCandidateCue = "uf_offer_confirm02", MaleCandidateCue = "gm_mb_offer_release03" } }
        };
    }

    static EncounterCueRequest Request(string line, string speaker, double now)
    {
        return new EncounterCueRequest { LineId = line, SpeakerId = speaker, PersonaId = speaker + ".persona",
            Gender = PersonaGender.Female, ReadableText = line == "splitash.invitation.npc"
                ? "Edda is safe. Your road is one I would choose. Will you have me?"
                : "We have fire for one meal and food for half. Will you help even that?",
            Now = now, ApprovalMode = CueApprovalMode.Normal };
    }

    static void Main()
    {
        var definitions = Definitions();
        var normal = EncounterCuePresentationPolicy.Present(null,
            Request("splitash.request.meal_offer", "edda", 10), definitions);
        Check(normal.ReadableText.Contains("Will you help"), "normal mode preserves readable authored text");
        Check(normal.SafeEmote == "comehere", "normal mode returns one safe emote");
        Check(normal.VoiceCue == null && normal.Suppression == CueSuppression.ConversationalVoiceUnapproved,
            "normal mode fails closed for unapproved conversation voice");
        Check(!normal.VoiceIsAuditionCandidate, "normal mode never labels a candidate playable");
        var cautious = Request("contact.friendly.banked_fire", "friendly-watcher", 15);
        cautious.ReadableText = "Easy there. We saw you. The fire is ours, but the ground need not be.";
        var cautiousNormal = EncounterCuePresentationPolicy.Present(normal.State, cautious, definitions);
        Check(cautiousNormal.VoiceCue == null && cautiousNormal.SafeEmote == "halt" &&
            cautiousNormal.Suppression == CueSuppression.ConversationalVoiceUnapproved,
            "new cautious contact remains silent in normal mode");

        var auditionRequest = Request("splitash.invitation.npc", "runa", 20);
        auditionRequest.ApprovalMode = CueApprovalMode.Audition;
        var audition = EncounterCuePresentationPolicy.Present(normal.State, auditionRequest, definitions);
        Check(audition.VoiceCue == "uf_offer_confirm02" && audition.VoiceIsAuditionCandidate,
            "audition mode exposes female candidate explicitly");
        Check(audition.ReadableText.EndsWith("Will you have me?"), "audition does not replace exact readable choice");
        var travelRequest = Request("contact.travel.offer", "cautious-survivor", 25);
        travelRequest.ReadableText = "Our road runs with yours as far as the old stones. Walk it together?";
        travelRequest.ApprovalMode = CueApprovalMode.Audition;
        var travel = EncounterCuePresentationPolicy.Present(audition.State, travelRequest, definitions);
        Check(travel.VoiceCue == "uf_offer_confirm02" && travel.VoiceIsAuditionCandidate &&
            travel.ReadableText.Contains("old stones"), "temporary travel offer is audition-only and keeps exact text");

        var maleRequest = Request("splitash.request.meal_offer", "ketil", 30);
        maleRequest.Gender = PersonaGender.Male; maleRequest.ApprovalMode = CueApprovalMode.Audition;
        var male = EncounterCuePresentationPolicy.Present(audition.State, maleRequest, definitions);
        Check(male.VoiceCue == "gm_mb_asking_sigh09" && male.VoiceIsAuditionCandidate,
            "audition resolves candidate by persona gender");

        var combatRequest = Request("splitash.request.meal_offer", "edda", 40);
        combatRequest.NativeCombatOwnsPresentation = true; combatRequest.ApprovalMode = CueApprovalMode.Audition;
        var combat = EncounterCuePresentationPolicy.Present(male.State, combatRequest, definitions);
        Check(combat.VoiceCue == null && combat.SafeEmote == null &&
            combat.Suppression == CueSuppression.CombatOwnsPresentation, "combat suppresses encounter cue and emote");
        Check(combat.ReadableText == combatRequest.ReadableText, "combat suppression does not rewrite text");

        var alertRequest = Request("splitash.request.meal_offer", "edda", 50);
        alertRequest.AlertOwnsPresentation = true; alertRequest.ApprovalMode = CueApprovalMode.Audition;
        var alert = EncounterCuePresentationPolicy.Present(male.State, alertRequest, definitions);
        Check(alert.VoiceCue == null && alert.SafeEmote == null &&
            alert.Suppression == CueSuppression.AlertOwnsPresentation, "alert owner suppresses conversation presentation");

        var shockRequest = Request("splitash.shock.edda_down", "runa", 60);
        shockRequest.ReadableText = "Edda—down!"; shockRequest.IncidentId = "splitash-incident-44";
        shockRequest.AlertOwnsPresentation = true;
        var shock = EncounterCuePresentationPolicy.Present(male.State, shockRequest, definitions);
        Check(shock.VoiceCue == EncounterCuePresentationPolicy.ExistingSurprisedVoicePool,
            "surprise resolves exact approved symbolic pool");
        Check(!shock.VoiceIsAuditionCandidate && shock.SafeEmote == "point", "approved surprise is not a candidate asset");
        var ownerlessShock = Request("splitash.shock.edda_down", "runa", 65);
        ownerlessShock.ReadableText = "Edda—down!"; ownerlessShock.IncidentId = "splitash-incident-ownerless";
        var ownerless = EncounterCuePresentationPolicy.Present(null, ownerlessShock, definitions);
        Check(ownerless.VoiceCue == null && ownerless.SafeEmote == null &&
            ownerless.Suppression == CueSuppression.ApprovedSurpriseRequiresAlertOwnership,
            "approved surprise cannot bypass authoritative alert ownership");
        var missingIncident = Request("splitash.shock.edda_down", "runa", 66);
        missingIncident.ReadableText = "Edda—down!"; missingIncident.AlertOwnsPresentation = true;
        var missingIncidentResult = EncounterCuePresentationPolicy.Present(null, missingIncident, definitions);
        Check(missingIncidentResult.VoiceCue == null && missingIncidentResult.SafeEmote == null &&
            missingIncidentResult.Suppression == CueSuppression.InvalidRequest,
            "approved surprise fails closed without stable incident ID");
        var repeatedShock = EncounterCuePresentationPolicy.Present(shock.State, shockRequest, definitions);
        Check(repeatedShock.VoiceCue == null, "same incident never voices twice even before cooldown checks");

        var repeatedLater = Request("splitash.shock.edda_down", "runa", 70);
        repeatedLater.ReadableText = "Edda—down!"; repeatedLater.IncidentId = "splitash-incident-44";
        repeatedLater.AlertOwnsPresentation = true;
        var repeatedAfterCooldown = EncounterCuePresentationPolicy.Present(shock.State, repeatedLater, definitions);
        Check(repeatedAfterCooldown.VoiceCue == null &&
            repeatedAfterCooldown.Suppression == CueSuppression.IncidentVoiceAlreadyEmitted,
            "incident dedupe persists after all cooldowns");

        var otherSpeaker = Request("splitash.invitation.npc", "runa", 80);
        otherSpeaker.ApprovalMode = CueApprovalMode.Audition;
        var owner = EncounterCuePresentationPolicy.Present(null, otherSpeaker, definitions);
        var collision = Request("splitash.request.meal_offer", "edda", 81);
        collision.ApprovalMode = CueApprovalMode.Audition;
        var blockedOther = EncounterCuePresentationPolicy.Present(owner.State, collision, definitions);
        Check(blockedOther.Suppression == CueSuppression.OtherSpeakerOwnsPresentation && blockedOther.SafeEmote == null,
            "one speaker owns the short presentation lease");

        var campCooldown = Request("splitash.request.meal_offer", "edda", 82.5);
        campCooldown.ApprovalMode = CueApprovalMode.Audition;
        var campBlocked = EncounterCuePresentationPolicy.Present(owner.State, campCooldown, definitions);
        Check(campBlocked.Suppression == CueSuppression.CampCooldown, "camp-wide cooldown applies after lease ends");

        var sameSpeaker = Request("splitash.invitation.npc", "runa", 84);
        sameSpeaker.ApprovalMode = CueApprovalMode.Audition;
        var speakerBlocked = EncounterCuePresentationPolicy.Present(owner.State, sameSpeaker, definitions);
        Check(speakerBlocked.Suppression == CueSuppression.SpeakerCooldown, "per-speaker cooldown prevents spam");

        var afterCooldown = Request("splitash.invitation.npc", "runa", 88);
        afterCooldown.ApprovalMode = CueApprovalMode.Audition;
        var allowedAgain = EncounterCuePresentationPolicy.Present(owner.State, afterCooldown, definitions);
        Check(allowedAgain.VoiceIsAuditionCandidate, "speaker can present after both cooldowns");

        var unknown = EncounterCuePresentationPolicy.Present(null, Request("missing.line", "runa", 90), definitions);
        Check(unknown.ReadableText != null && unknown.VoiceCue == null && unknown.SafeEmote == null &&
            unknown.Suppression == CueSuppression.UnknownLine, "unknown line fails closed but retains supplied readable text");

        var unsafeDefinitions = Definitions();
        unsafeDefinitions["splitash.request.meal_offer"].SafeEmote = "emote_with_root_motion";
        var unsafeResult = EncounterCuePresentationPolicy.Present(null,
            Request("splitash.request.meal_offer", "edda", 100), unsafeDefinitions);
        Check(unsafeResult.Suppression == CueSuppression.UnknownLine && unsafeResult.SafeEmote == null,
            "unapproved emote fails closed");
        var forgedDefinitions = Definitions();
        forgedDefinitions["splitash.request.meal_offer"].UsesApprovedSurprisePool = true;
        var forged = Request("splitash.request.meal_offer", "edda", 105);
        forged.AlertOwnsPresentation = true; forged.IncidentId = "forged-incident";
        var forgedResult = EncounterCuePresentationPolicy.Present(null, forged, forgedDefinitions);
        Check(forgedResult.Suppression == CueSuppression.UnknownLine && forgedResult.VoiceCue == null,
            "non-shock line cannot be relabeled as the approved surprise pool");

        var invalid = Request("splitash.request.meal_offer", "edda", 110); invalid.ReadableText = "";
        var invalidResult = EncounterCuePresentationPolicy.Present(null, invalid, definitions);
        Check(invalidResult.Suppression == CueSuppression.InvalidRequest && invalidResult.VoiceCue == null,
            "missing readable text is invalid");
        Check(normal.State.LastSpeakerCueAt.ContainsKey("edda") && !new EncounterCueState().LastSpeakerCueAt.ContainsKey("edda"),
            "policy state is explicit and isolated");

        Console.WriteLine("EncounterCuePresentationPolicyTests PASS: " + checks + " checks");
    }
}
