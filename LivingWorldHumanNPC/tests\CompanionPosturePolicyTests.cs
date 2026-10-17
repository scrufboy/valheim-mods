using System;
using LivingWorld.HumanNpc;
static class CompanionPosturePolicyTests
{
    static int checks;
    static void Check(bool value, string reason) { ++checks; if (!value) throw new Exception(reason); }
    static void Main()
    {
        foreach (bool eligible in new[] { false, true })
        foreach (bool combat in new[] { false, true })
        foreach (bool safe in new[] { false, true })
        foreach (bool crouch in new[] { false, true })
        foreach (bool sit in new[] { false, true })
        foreach (bool arrived in new[] { false, true })
        foreach (bool clear in new[] { false, true })
        foreach (float quiet in new[] { Single.NaN, Single.PositiveInfinity, -1f, 0f, 1.99f, 2f, 10f })
        {
            var actual = CompanionPosturePolicy.Choose(eligible,combat,safe,crouch,sit,arrived,clear,quiet);
            var expected = CompanionPosture.Upright;
            if (eligible && !combat && safe)
            {
                if (crouch) expected = CompanionPosture.Crouch;
                else if (sit && arrived && clear && !Single.IsNaN(quiet) && !Single.IsInfinity(quiet) && quiet >= 2f) expected = CompanionPosture.Sit;
            }
            Check(actual == expected, "Posture precedence / posture interruption");
        }
        Console.WriteLine("CompanionPosturePolicy PASS: " + checks + " combinations. Animation itself NOT RUN.");
    }
}
