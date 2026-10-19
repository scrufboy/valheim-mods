using System;
using LivingWorld.HumanNpc;
class PartyCombatPolicyTests
{
    static int checks;
    static void Check(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
    static void Main()
    {
        Check(PartyCombatPolicy.BowReleaseBridge(true,true,true,true,true,true), "native attack request permits scoped bow release bridge");
        Check(!PartyCombatPolicy.BowReleaseBridge(false,true,true,true,true,true), "unready archer cannot bridge release");
        Check(!PartyCombatPolicy.BowReleaseBridge(true,false,true,true,true,true), "unauthorized target cannot bridge release");
        Check(!PartyCombatPolicy.BowReleaseBridge(true,true,false,true,true,true), "non-bow attack remains untouched");
        Check(!PartyCombatPolicy.BowReleaseBridge(true,true,true,false,true,true), "partial draw cannot release");
        Check(!PartyCombatPolicy.BowReleaseBridge(true,true,true,true,false,true), "blocked firing lane cannot release");
        Check(!PartyCombatPolicy.BowReleaseBridge(true,true,true,true,true,false), "empty bow cannot release");
        Check(PartyCombatPolicy.Weapon(false,true,true,false,20,4,2)==PartyWeapon.Bow,"open approach allows complete shot");
        Check(PartyCombatPolicy.Weapon(false,true,true,false,20,8,2)==PartyWeapon.Bow,"archer role survives a fast open approach");
        Check(PartyCombatPolicy.Weapon(false,true,false,false,30,2,2)==PartyWeapon.SwordShield,"ally in lane overrides range");
        Check(PartyCombatPolicy.Weapon(false,false,true,false,30,2,2)==PartyWeapon.SwordShield,"no ammunition");
        Check(PartyCombatPolicy.Weapon(true,true,true,true,30,2,2)==PartyWeapon.SwordShield,"nearby second threat overrides distant target");
        Check(PartyCombatPolicy.Weapon(false,true,true,false,7,1,2)==PartyWeapon.SwordShield,"entry hysteresis");
        Check(PartyCombatPolicy.Weapon(true,true,true,false,11,1,2)==PartyWeapon.Bow,"retain safe ranged commitment");
        Check(!PartyCombatPolicy.Torch(true,false,true,false,false),"stealth suppresses light");
        Check(!PartyCombatPolicy.Torch(true,false,false,true,false),"combat frees hands");
        Check(!PartyCombatPolicy.Torch(true,false,false,false,true),"occupied hands suppress light");
        Check(!PartyCombatPolicy.Torch(true,true,false,false,false),"nearby built light suppresses carried torch");
        Check(PartyCombatPolicy.Torch(true,false,false,false,false),"vanilla night peaceful companion uses light");
        Check(!PartyCombatPolicy.Torch(false,false,false,false,false),"vanilla day does not use utility light");
        Check(PartyCombatPolicy.BuiltLightNearby(false,6f),"six metres enters built-light shelter");
        Check(PartyCombatPolicy.BuiltLightNearby(true,9.9f),"light shelter persists through hysteresis band");
        Check(!PartyCombatPolicy.BuiltLightNearby(false,6.1f),"outside six metres does not newly suppress torch");
        Check(!PartyCombatPolicy.BuiltLightNearby(true,10f),"ten metres restores torch eligibility");
        Check(!PartyCombatPolicy.BuiltLightNearby(true,Single.PositiveInfinity),"no built light restores torch eligibility");
        Check(PartyCombatPolicy.PerceivedLight(0f,0f,.6f,1f) < .1f,"blue night is perceived as dark");
        Check(PartyCombatPolicy.PerceivedLight(.6f,.6f,.6f,1f) > .5f,"neutral daylight is perceived as bright");
        Check(PartyCombatPolicy.HealEligible(true,true,true,true,5,20,100),"injured party member");
        Check(!PartyCombatPolicy.HealEligible(false,true,true,true,5,20,100),"enemy excluded");
        Check(!PartyCombatPolicy.HealEligible(true,false,true,true,5,0,100),"no resurrection");
        Check(!PartyCombatPolicy.HealEligible(true,true,false,true,5,20,100),"no foreign health writes");
        Check(!PartyCombatPolicy.HealEligible(true,true,true,false,5,20,100),"wall blocks healing");
        Check(PartyCombatPolicy.ClericSupport(true,false,false,true,true),"safe combat window permits class support");
        Check(!PartyCombatPolicy.ClericSupport(true,true,false,true,true),"personal threat keeps native combat in control");
        Check(!PartyCombatPolicy.ClericSupport(true,false,true,true,true),"busy body cannot begin a cast");
        Check(PartyCombatPolicy.ClericSupport(false,false,false,true,true),"peaceful recovery remains available");
        Check(PartyCombatPolicy.ReactToAttack(true,true,true,true,false,3,0f,0f),"observed close attack permits immediate shield response");
        Check(!PartyCombatPolicy.ReactToAttack(false,true,true,true,false,3,0f,0f),"unseen cue cannot trigger block");
        Check(!PartyCombatPolicy.ReactToAttack(true,true,true,true,false,3,1f,0f),"stale cue discarded");
        Check(!PartyCombatPolicy.ReactToAttack(true,true,true,true,true,3,0f,0f),"staggered or attacking defender cannot begin a block");
        Console.WriteLine("PartyCombatPolicy PASS: " + checks + " scenario checks. Runtime not tested.");
    }
}
