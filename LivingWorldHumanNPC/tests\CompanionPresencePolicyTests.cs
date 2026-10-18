using System;
using LivingWorld.HumanNpc;
static class CompanionPresencePolicyTests
{
    static int checks;
    static void Check(bool value, string reason) { ++checks; if (!value) throw new Exception(reason); }
    static void Pace(CompanionPace before, float distance, bool cross, CompanionPace expected, string reason)
    { Check(CompanionPresencePolicy.NextPace(before, distance, cross) == expected, reason); }
    static void Main()
    {
        foreach (CompanionPace p in Enum.GetValues(typeof(CompanionPace)))
        {
            foreach (float invalid in new[] { Single.NaN, Single.PositiveInfinity, Single.NegativeInfinity, -1f })
                foreach (bool cross in new[] { false, true }) Pace(p, invalid, cross, CompanionPace.Stand, "Invalid distance fails stationary");
            Pace(p, 0, false, CompanionPace.Stand, "No overlap pursuit");
            Pace(p, 2.2f, false, CompanionPace.Stand, "Arrival from all paces");
            Pace(p, 1f, true, CompanionPace.Walk, "Door separation overrides arrival");
            Pace(p, 5f, false, CompanionPace.Run, "Catch up at five metres");
        }
        Pace(CompanionPace.Stand, 2.8f, false, CompanionPace.Stand, "No arrival jitter");
        Pace(CompanionPace.Stand, 2.9f, false, CompanionPace.Walk, "Resume boundary");
        Pace(CompanionPace.Walk, 2.8f, false, CompanionPace.Walk, "Continue inside hysteresis band");
        Pace(CompanionPace.Walk, 4.9f, false, CompanionPace.Walk, "Below catch up boundary");
        Pace(CompanionPace.Run, 4f, false, CompanionPace.Run, "Catch up does not flutter at five metres");
        Pace(CompanionPace.Run, 3.5f, false, CompanionPace.Walk, "Ease to walking when near");
        var pace = CompanionPace.Stand;
        foreach (float noise in new[] {2.2f, 2.3f, 2.6f, 2.3f, 2.2f, 2.5f})
        { pace = CompanionPresencePolicy.NextPace(pace, noise, false); Check(pace == CompanionPace.Stand, "Standing noise remains still"); }
        pace = CompanionPace.Run;
        foreach (float noise in new[] {5f, 4.9f, 5.1f, 4.7f, 5f})
        { pace = CompanionPresencePolicy.NextPace(pace, noise, false); Check(pace == CompanionPace.Run, "Running noise remains running"); }
        foreach (bool moving in new[] {false, true})
        {
            foreach (float quiet in new[] {0f, 11.9f, 12f, 13f, 100f})
            {
                Check(!CompanionPresencePolicy.AttendLeader(false, false, moving, quiet), "Remote/dead/unbound/hold gets no leader look");
                Check(!CompanionPresencePolicy.AttendLeader(true, true, moving, quiet), "Combat immediately owns attention");
                Check(CompanionPresencePolicy.AttendLeader(true, false, moving, quiet) == (moving || quiet < 12), "Idle delay versus moving attention");
            }
            foreach (float invalid in new[] {Single.NaN, Single.PositiveInfinity, -1f})
                Check(!CompanionPresencePolicy.AttendLeader(true, false, moving, invalid), "Invalid time releases attention");
        }
        Check(CompanionPresencePolicy.StopDistance < CompanionPresencePolicy.ResumeDistance &&
            CompanionPresencePolicy.ResumeDistance < CompanionPresencePolicy.WalkAgainDistance &&
            CompanionPresencePolicy.WalkAgainDistance < CompanionPresencePolicy.RunDistance, "Ordered hysteresis bands");
        Check(CompanionPresencePolicy.FormationSideOffset(true, false, 0) < 0, "Archer owns stable left lane");
        Check(CompanionPresencePolicy.FormationSideOffset(false, true, 0) > 0, "Cleric owns stable right lane");
        Check(CompanionPresencePolicy.FormationSideOffset(false, false, 0) !=
            CompanionPresencePolicy.FormationSideOffset(false, false, 1), "Other companions alternate stable lanes");
        Check(CompanionPresencePolicy.NextFormationPace(CompanionPace.Walk, .8f) == CompanionPace.Stand,
            "Formation settles tightly on its leased position");
        Check(CompanionPresencePolicy.NextFormationPace(CompanionPace.Stand, 1.5f) == CompanionPace.Walk,
            "Formation resumes when its leased position opens");
        Console.WriteLine("CompanionPresencePolicy PASS: " + checks + " checks.");
    }
}

