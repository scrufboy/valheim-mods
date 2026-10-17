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
        Check(!PartyCombatPolicy.Torch(true,false,false,false,false,true,0.03f,false),"fog suppresses light");
        Check(!PartyCombatPolicy.Torch(true,false,false,false,false,true,0.001f,true),"local mist suppresses light");
        Check(!PartyCombatPolicy.Torch(true,false,true,false,false,true,0.001f,false),"stealth suppresses light");
        Check(!PartyCombatPolicy.Torch(true,false,false,true,false,true,0.001f,false),"combat frees hands");
        Check(!PartyCombatPolicy.Torch(true,true,false,false,false,true,0.001f,false),"existing light sufficient");
        Check(PartyCombatPolicy.Torch(true,false,false,false,false,true,0.001f,false),"clear dark night");
        Check(!PartyCombatPolicy.Torch(true,false,false,false,false,false,0,false),"unknown fog is not clear weather");
        Check(PartyCombatPolicy.HealEligible(true,true,true,true,5,20,100),"injured party member");
        Check(!PartyCombatPolicy.HealEligible(false,true,true,true,5,20,100),"enemy excluded");
        Check(!PartyCombatPolicy.HealEligible(true,false,true,true,5,0,100),"no resurrection");
        Check(!PartyCombatPolicy.HealEligible(true,true,false,true,5,20,100),"no foreign health writes");
        Check(!PartyCombatPolicy.HealEligible(true,true,true,false,5,20,100),"wall blocks healing");
        Check(PartyCombatPolicy.ReactToAttack(true,true,true,true,false,3,0f,0f),"observed close attack permits immediate shield response");
        Check(!PartyCombatPolicy.ReactToAttack(false,true,true,true,false,3,0f,0f),"unseen cue cannot trigger block");
        Check(!PartyCombatPolicy.ReactToAttack(true,true,true,true,false,3,1f,0f),"stale cue discarded");
        Check(!PartyCombatPolicy.ReactToAttack(true,true,true,true,true,3,0f,0f),"staggered or attacking defender cannot begin a block");
        Console.WriteLine("PartyCombatPolicy PASS: " + checks + " scenario checks. Runtime not tested.");
    }
}
