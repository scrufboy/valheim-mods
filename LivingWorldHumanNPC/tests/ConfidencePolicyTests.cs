using System;
using System.Globalization;
using LivingWorld.HumanNpc;

static class ConfidencePolicyTests
{
    static int checks;
    static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    static bool Near(double a, double b) { return Math.Abs(a-b) < .000001; }
    static void Tick(ConfidencePolicy state, int count, PartySupportObservation support, double pressure)
    { for (int n=0;n<count;n++) state.Advance(.5,support,pressure); }
    static void Main()
    {
        var together = new ConfidencePolicy(60,.5); var calm = new ConfidencePolicy(60,.5);
        Tick(together,80,PartySupportObservation.Perceived,1); Tick(calm,80,PartySupportObservation.Perceived,0);
        Check(Near(together.Current,calm.Current) && together.Current>60,"Overwhelming pressure NEVER erodes cohesive confidence");
        var apart = new ConfidencePolicy(60,.5); var apartCalm = new ConfidencePolicy(60,.5);
        Tick(apart,80,PartySupportObservation.Absent,1); Tick(apartCalm,80,PartySupportObservation.Absent,0);
        Check(apart.Current<apartCalm.Current && apartCalm.Current<60,"Same pressure adds erosion ONLY when apart");
        Check(apart.Cohesion==PartyCohesion.Separated,"Sustained absence confirms separation");
        double before=apart.Current; apart.Advance(.5,PartySupportObservation.Perceived,1);
        Check(apart.Current>before && apart.Current-before<1 && apart.Current<60,"Reunion gradual, no instant refill even under pressure");
        Check(apart.Cohesion==PartyCohesion.Together,"Perceived reunion returns together state");
        var blink = new ConfidencePolicy(60,.5); blink.Advance(.5,PartySupportObservation.Perceived,0); before=blink.Current;
        Tick(blink,5,PartySupportObservation.Absent,1);
        Check(blink.Current==before && blink.Cohesion==PartyCohesion.Together,"2.5s transient occlusion does not erode or switch cohesion");
        blink.Advance(.5,PartySupportObservation.Perceived,1);
        Check(blink.NoSupportSeconds==0 && blink.Current>before,"Reappearance resets absence grace");
        for(int n=0;n<40;n++){blink.Advance(.5,PartySupportObservation.Absent,1);blink.Advance(.5,PartySupportObservation.Perceived,1);}
        Check(blink.Cohesion==PartyCohesion.Together && blink.Current>before,"Alternating transient occlusion never oscillates into separation");
        before=apartCalm.Current; Tick(apartCalm,200,PartySupportObservation.Unknown,1);
        Check(apartCalm.Current==before && apartCalm.Cohesion==PartyCohesion.Unknown,"Unknown freezes; cannot invent support or separation");
        Tick(apartCalm,5,PartySupportObservation.Absent,1);
        Check(apartCalm.Current==before,"Confirmation restarts after an unknown sample");
        var brave=new ConfidencePolicy(60,1); var wary=new ConfidencePolicy(60,0);
        Tick(brave,20,PartySupportObservation.Absent,1); Tick(wary,20,PartySupportObservation.Absent,1);
        Check(brave.Current>wary.Current && brave.Current<60,"Personal courage buffers loss but cannot abolish separation loss");
        Tick(wary,1000,PartySupportObservation.Absent,1);Check(wary.Current==0,"Lower bound");
        Tick(brave,1000,PartySupportObservation.Perceived,1);Check(brave.Current==100,"Upper bound under strong pressure");
        foreach(double dt in new[]{-1,0,.50001,10,Double.NaN,Double.PositiveInfinity})
        {before=together.Current;Check(!together.Advance(dt,PartySupportObservation.Absent,1)&&together.Current==before,"Invalid/long elapsed time cannot mutate");}
        foreach(double pressure in new[]{-1,1.1,Double.NaN,Double.PositiveInfinity})
        {before=together.Current;Check(!together.Advance(.5,PartySupportObservation.Absent,pressure)&&together.Current==before,"Malformed pressure cannot mutate");}
        Check(!together.Advance(.5,(PartySupportObservation)7,0),"Invalid observation enum rejected");
        var originalCulture=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=new CultureInfo("fr-FR");
            var saved=new ConfidencePolicy(34.1234,.76);ConfidencePolicy loaded;
            Check(ConfidencePolicy.TryRestore(saved.Save(),out loaded)&&Near(loaded.Current,saved.Current)&&Near(loaded.Courage,saved.Courage),"Invariant saved personal resource roundtrip");
            Check(loaded.Cohesion==PartyCohesion.Unknown && loaded.NoSupportSeconds==0,"Reload never preserves sensing or offline elapsed time");
        }
        finally{CultureInfo.CurrentCulture=originalCulture;}
        foreach(string record in new[]{null,"","1|NaN|0.5","1|Infinity|0.5","1|-1|0.5","1|101|0.5","1|60|NaN","1|60|-0.1","1|60|1.1","2|60|0.5","1|60|0.5|extra",new String('x',129)})
        {ConfidencePolicy loaded;Check(!ConfidencePolicy.TryRestore(record,out loaded)&&loaded==null,"Malformed saved state refused, no replacement");}
        Check(ConfidencePolicy.ObservedPressure(2,0)==0,"Two local threats not overwhelming");
        Check(ConfidencePolicy.ObservedPressure(3,0)>0 && ConfidencePolicy.ObservedPressure(100,0)==1,"Observed pressure bounded");
        Check(ConfidencePolicy.ObservedPressure(3,3)==0,"Local support affects pressure estimate");
        Console.WriteLine("ConfidencePolicy PASS: "+checks+" checks; pressure-only-while-separated, grace, reunion, courage and persistence.");
    }
}
