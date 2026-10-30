using System;
using LivingWorld.FormidableAI.Profile;
using LivingWorld.Companions.FormidableAI.Classes;
public static class CharacterCombatProfileTests
{
    static int passed;
    static void Check(bool v,string n){if(!v)throw new Exception("FAIL: "+n);passed++;Console.WriteLine("ok - "+n);}
    static CharacterRollupFacts Facts(string r){return new CharacterRollupFacts{PersonIdentity="person-1",SavedRole=r,Origin=OriginBiome.Unknown,Training=TrainingBand.Trained};}
    public static void Main(){
        var gf=Facts("guard");gf.HasOneHanded=true;gf.HasShield=true;var g=CharacterCombatProfileFactory.Create(gf);
        Check(g.Job==CombatJob.Guard&&g.FormidableClass==FormidableClass.Warden,"guard maps explicitly to warden");
        Check(g.Has(WeaponDoctrine.Shield)&&g.Has(SurvivalDoctrine.BlockObservedStrike),"eligible shield loadout authorizes block doctrine");
        var af=Facts("archer");af.HasBow=true;var a=CharacterCombatProfileFactory.Create(af);
        Check(a.Job==CombatJob.Archer&&a.FormidableClass==FormidableClass.Huntress,"archer maps explicitly to huntress");
        Check(a.Has(SurvivalDoctrine.MaintainFiringLane)&&a.Has(SurvivalDoctrine.KitePressure),"bow authorizes ranged survival doctrine");
        var sf=Facts("scout");sf.HasBow=true;sf.HasSpear=true;var s=CharacterCombatProfileFactory.Create(sf);
        Check(s.Job==CombatJob.Scout&&s.FormidableClass==FormidableClass.Pathfinder,"scout maps explicitly to pathfinder");
        Check(s.Has(WeaponDoctrine.Throwing)&&s.Has(SurvivalDoctrine.InvestigateEvidence),"scout authorizes throw and investigation");
        var bad=Facts("guard");bad.HasShield=true;var b=CharacterCombatProfileFactory.Create(bad);
        Check(!b.Has(WeaponDoctrine.Shield)&&!b.Has(SurvivalDoctrine.BlockObservedStrike),"shield alone cannot claim shield doctrine");
        Check(g.EndlessUtilityTorch,"utility torch is explicit profile invariant");
        Console.WriteLine("PASS: "+passed+" character combat profile tests");}
}
