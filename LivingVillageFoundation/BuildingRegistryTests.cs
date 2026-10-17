using System;
using LivingVillageCore;
public static class BuildingRegistryTests
{
    private static int count;
    private static void Check(bool result,string name) { if(!result)throw new Exception(name);count++; }
    public static void Main()
    {
        var r=new BuildingRegistry();var origin=new RegistryPosition(0,0,0);bool active=true;
        Check(r.RegisterSettlement("A",p=>active&&Math.Abs(p.X)<=80&&Math.Abs(p.Z)<=80),"register A");
        Check(r.RegisterSettlement("B",p=>Math.Abs(p.X)<=80&&Math.Abs(p.Z)<=80),"register B overlapping");
        Check(!r.RegisterSettlement("A",p=>true),"cannot silently replace bounds");
        Check(r.RegisterBuilding("A","house",origin),"building registration");
        Check(!r.RegisterBuilding("B","house",origin),"one owner building");
        Check(!r.RegisterBuilding("A","outside",new RegistryPosition(81,0,0)),"bounds");
        Check(!r.RegisterBuilding("A","nan",new RegistryPosition(double.NaN,0,0)),"finite");
        Check(!r.AssignMarker("A","missing","m",BuildingRole.Kitchen,origin),"no unknown buildings");
        Check(r.AssignMarker("A","house","m",BuildingRole.Kitchen,new RegistryPosition(5,0,0)),"kitchen marker");
        Check(!r.AssignMarker("B","house","m",BuildingRole.Kitchen,origin),"cross settlement rejected");
        Check(r.FindNearest("A",BuildingRole.Kitchen,origin,4)==null,"lookup range");
        Check(r.FindNearest("B",BuildingRole.Kitchen,origin,10)==null,"lookup cannot steal role");
        Check(r.FindNearest("A",BuildingRole.Kitchen,origin,5).MarkerId=="m","inclusive range");
        Check(r.AssignMarker("A","house","m",BuildingRole.Smithy,origin),"change role");
        Check(r.FindNearest("A",BuildingRole.Kitchen,origin,10)==null,"old role unassigned");
        Check(!r.RemoveMarker("B","m"),"foreign removal rejected");
        Check(r.RemoveMarker("A","m")&&r.GetBuilding("house")!=null,"removal preserves building");
        Check(r.AssignMarker("A","house","z",BuildingRole.Housing,origin)&&r.AssignMarker("A","house","a",BuildingRole.Housing,origin),"multiple markers");
        Check(r.FindNearest("A",BuildingRole.Housing,origin,0).MarkerId=="a","deterministic tie");
        active=false;Check(r.FindNearest("A",BuildingRole.Housing,origin,10)==null,"changed external bounds honored");active=true;
        Check(r.RemoveBuilding("A","house")&&r.FindNearest("A",BuildingRole.Housing,origin,10)==null,"forget building unassigns markers");
        Console.WriteLine("PASS: "+count+" building registry policy checks");
    }
}
