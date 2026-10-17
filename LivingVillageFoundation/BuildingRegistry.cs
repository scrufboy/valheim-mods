using System;
using System.Collections.Generic;

namespace LivingVillageCore
{
    public enum BuildingRole { Kitchen, Smithy, Workshop, Barracks, Healing, GreatHall, Housing }
    public struct RegistryPosition
    {
        public readonly double X, Y, Z;
        public RegistryPosition(double x, double y, double z) { X=x; Y=y; Z=z; }
        public bool IsFinite { get { return !(Double.IsNaN(X)||Double.IsNaN(Y)||Double.IsNaN(Z)||Double.IsInfinity(X)||Double.IsInfinity(Y)||Double.IsInfinity(Z)); } }
        public double DistanceSquared(RegistryPosition other) { double x=X-other.X,y=Y-other.Y,z=Z-other.Z; return x*x+y*y+z*z; }
    }
    // These are identity/location records, not blueprints or instructions to destroy buildings.
    public sealed class BuildingRecord
    {
        public readonly string SettlementId, BuildingId;
        public readonly RegistryPosition Position;
        public BuildingRecord(string settlementId,string buildingId,RegistryPosition position) { SettlementId=settlementId;BuildingId=buildingId;Position=position; }
    }
    public sealed class BuildingMarker
    {
        public readonly string SettlementId, BuildingId, MarkerId;
        public readonly BuildingRole Role;
        public readonly RegistryPosition Position;
        public BuildingMarker(string settlementId,string buildingId,string markerId,BuildingRole role,RegistryPosition position)
        { SettlementId=settlementId;BuildingId=buildingId;MarkerId=markerId;Role=role;Position=position; }
    }
    // Game-thread only. Adapter supplies the authoritative maypole/outpost bounds.
    // Caller must validate network ownership before mutation and persist accepted changes.
    public sealed class BuildingRegistry
    {
        private readonly Dictionary<string,Func<RegistryPosition,bool>> bounds = new Dictionary<string,Func<RegistryPosition,bool>>(StringComparer.Ordinal);
        private readonly Dictionary<string,BuildingRecord> buildings = new Dictionary<string,BuildingRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string,BuildingMarker> markers = new Dictionary<string,BuildingMarker>(StringComparer.Ordinal);
        private static bool ValidId(string id) { return !String.IsNullOrWhiteSpace(id); }
        private bool Contains(string settlementId,RegistryPosition point)
        { Func<RegistryPosition,bool> test;return ValidId(settlementId)&&point.IsFinite&&bounds.TryGetValue(settlementId,out test)&&test(point); }
        public bool RegisterSettlement(string settlementId,Func<RegistryPosition,bool> contains)
        {
            if(!ValidId(settlementId)||contains==null||bounds.ContainsKey(settlementId))return false;
            bounds.Add(settlementId,contains);return true;
        }
        public bool RegisterBuilding(string settlementId,string buildingId,RegistryPosition position)
        {
            if(!ValidId(buildingId)||!Contains(settlementId,position))return false;
            BuildingRecord existing;
            if(buildings.TryGetValue(buildingId,out existing)&&existing.SettlementId!=settlementId)return false;
            buildings[buildingId]=new BuildingRecord(settlementId,buildingId,position);return true;
        }
        public bool AssignMarker(string settlementId,string buildingId,string markerId,BuildingRole role,RegistryPosition position)
        {
            if(!ValidId(markerId)||!ValidId(buildingId)||!Enum.IsDefined(typeof(BuildingRole),role)||!Contains(settlementId,position))return false;
            BuildingRecord building;
            if(!buildings.TryGetValue(buildingId,out building)||building.SettlementId!=settlementId||!Contains(settlementId,building.Position))return false;
            BuildingMarker existing;
            if(markers.TryGetValue(markerId,out existing)&&existing.SettlementId!=settlementId)return false;
            markers[markerId]=new BuildingMarker(settlementId,buildingId,markerId,role,position);return true;
        }
        public bool RemoveMarker(string settlementId,string markerId)
        { BuildingMarker marker;if(!ValidId(markerId)||!markers.TryGetValue(markerId,out marker)||marker.SettlementId!=settlementId)return false;return markers.Remove(markerId); }
        public bool RemoveBuilding(string settlementId,string buildingId)
        {
            BuildingRecord building;if(!ValidId(buildingId)||!buildings.TryGetValue(buildingId,out building)||building.SettlementId!=settlementId)return false;
            var remove=new List<string>();foreach(var pair in markers)if(pair.Value.BuildingId==buildingId)remove.Add(pair.Key);
            foreach(string id in remove)markers.Remove(id);return buildings.Remove(buildingId);
        }
        public BuildingRecord GetBuilding(string buildingId)
        { BuildingRecord building;return ValidId(buildingId)&&buildings.TryGetValue(buildingId,out building)?building:null; }
        public BuildingMarker FindNearest(string settlementId,BuildingRole role,RegistryPosition origin,double maxDistance)
        {
            if(!Contains(settlementId,origin)||!Enum.IsDefined(typeof(BuildingRole),role)||Double.IsNaN(maxDistance)||Double.IsInfinity(maxDistance)||maxDistance<0)return null;
            BuildingMarker nearest=null;double best=maxDistance*maxDistance;
            foreach(BuildingMarker marker in markers.Values)
            {
                BuildingRecord building;
                if(marker.SettlementId!=settlementId||marker.Role!=role||!Contains(settlementId,marker.Position)||!buildings.TryGetValue(marker.BuildingId,out building)||!Contains(settlementId,building.Position))continue;
                double distance=origin.DistanceSquared(marker.Position);
                if(distance<best||(distance==best&&(nearest==null||String.CompareOrdinal(marker.MarkerId,nearest.MarkerId)<0))) { nearest=marker;best=distance; }
            }
            return nearest;
        }
    }
}
