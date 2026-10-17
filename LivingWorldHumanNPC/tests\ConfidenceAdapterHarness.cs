// Actual adapter execution against managed doubles, not Unity/physics certification.
using System;
using System.Collections.Generic;
using System.Reflection;
using LivingWorld.HumanNpc;
using UnityEngine;
namespace UnityEngine
{
    public class GameObject
    {
        public Transform transform=new Transform(); public readonly List<MonoBehaviour> components=new List<MonoBehaviour>();
        public T Add<T>() where T:MonoBehaviour,new(){var value=new T();value.gameObject=this;components.Add(value);return value;}
        public T Get<T>() where T:class{foreach(var value in components)if(value is T)return value as T;return null;}
    }
    public class MonoBehaviour
    {
        public GameObject gameObject;public bool isActiveAndEnabled=true;
        public Transform transform{get{return gameObject.transform;}}
        public T GetComponent<T>()where T:class{return gameObject.Get<T>();}
        public T GetComponentInParent<T>()where T:class{return GetComponent<T>();}
    }
    public class Transform{public Vector3 position;}
    public struct Vector3
    {
        public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public static float Distance(Vector3 a,Vector3 b){return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));}
    }
    public class Collider:MonoBehaviour{}
    public static class Time{public static float time,deltaTime;}
    public enum QueryTriggerInteraction{Ignore}
    public static class LayerMask
    {
        public static bool missing;
        public static int NameToLayer(string name)
        {if(missing)return -1;switch(name){case "character":return 1;case "character_net":return 2;case "character_ghost":return 3;case "character_noenv":return 4;default:return -1;}}
    }
    public static class Physics
    {
        public static int queries,lastMask;public static readonly List<Collider> scene=new List<Collider>();
        public static int OverlapSphereNonAlloc(Vector3 at,float range,Collider[] result,int layers,QueryTriggerInteraction triggers)
        {queries++;lastMask=layers;int count=Math.Min(result.Length,scene.Count);for(int i=0;i<count;i++)result[i]=scene[i];return count;}
    }
}
public class ZDO
{
    public readonly Dictionary<string,string> data=new Dictionary<string,string>();public int writes;
    public string GetString(string key,string fallback){string value;return data.TryGetValue(key,out value)?value:fallback;}
    public void Set(string key,string value){data[key]=value;writes++;}
}
public class ZNetView:MonoBehaviour
{
    public bool valid=true,owner=true;public readonly ZDO data=new ZDO();
    public bool IsValid(){return valid;}public bool IsOwner(){return owner;}public ZDO GetZDO(){return data;}
}
public class Character:MonoBehaviour
{
    public bool dead,tame,visible=true,audible;
    public bool IsDead(){return dead;}public bool IsTamed(){return tame;}public bool IsPlayer(){return this is Player;}
}
public class Humanoid:Character{}
public class Player:Humanoid{public long id=7;public long GetPlayerID(){return id;}}
public class MonsterAI:MonoBehaviour
{
    public bool alerted;public Character target;public int senses;
    public bool IsAlerted(){return alerted;}public Character GetTargetCreature(){return target;}
    public bool CanSeeTarget(Character candidate){senses++;return candidate.visible;}
    public bool CanHearTarget(Character candidate){senses++;return candidate.audible;}
}
namespace LivingWorld.HumanNpc
{
    public class HumanAppearance:MonoBehaviour{public bool Initialized=true;}
    public class HumanCompositionIdentity:MonoBehaviour{public bool IsReady=true;}
    public enum HumanProfileState{Absent,Ready,Refused}
    public class HumanProfile{public const string RecordKey="livingworld_human_identity";public byte InitialCourage;}
    public class HumanIdentity:MonoBehaviour
    {
        public HumanProfile profile;public HumanProfileState state=HumanProfileState.Absent;
        public HumanProfileState Read(out HumanProfile result){result=profile;return state;}
    }
    public class HumanCompanion:MonoBehaviour
    {
        public Player leader;public long party=7;public bool ready=true,combat;
        internal Player PresenceLeader{get{return combat?null:leader;}}
        public bool CombatReadinessActive{get{return combat;}}
        internal bool TryGetConfidenceParty(out long partyId,out Player bound){partyId=party;bound=leader;return ready;}
    }
}
namespace LivingWorld.ManualTest{public class ManualTestPlugin{public static ManualTestPlugin Instance;public void Report(string message){}}}
static class ConfidenceAdapterHarness
{
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Call(object obj,string name){obj.GetType().GetMethod(name,Hidden).Invoke(obj,null);}
    static void Step(HumanPartyConfidence state,int frames,float dt)
    {for(int n=0;n<frames;n++){Time.deltaTime=dt;Time.time+=dt;Call(state,"Update");}}
    static ConfidenceSnapshot Read(HumanPartyConfidence state){ConfidenceSnapshot result;Check(state.TryGetSnapshot(out result),"Snapshot available");return result;}
    static GameObject Human()
    {
        var actor=new GameObject();actor.Add<ZNetView>();actor.Add<Humanoid>();actor.Add<MonsterAI>();actor.Add<HumanAppearance>();actor.Add<HumanCompositionIdentity>();actor.Add<HumanCompanion>();return actor;
    }
    static Collider Threat(Humanoid victim)
    {
        var actor=new GameObject();actor.transform.position=new Vector3(1,0,0);actor.Add<ZNetView>();actor.Add<Character>();
        var ai=actor.Add<MonsterAI>();ai.alerted=true;ai.target=victim;return actor.Add<Collider>();
    }
    static void Main()
    {
        var npc=Human();var view=npc.Get<ZNetView>();var body=npc.Get<Humanoid>();var ai=npc.Get<MonsterAI>();var orders=npc.Get<HumanCompanion>();
        var person=new GameObject();var leader=person.Add<Player>();person.Add<ZNetView>();person.transform.position=new Vector3(2,0,0);orders.leader=leader;
        var confidence=npc.Add<HumanPartyConfidence>();
        ConfidenceSnapshot unused;Check(!confidence.TryGetSnapshot(out unused)&&view.data.writes==0,"Read does not initialize or write");
        for(int n=0;n<5;n++)Physics.scene.Add(Threat(body));
        orders.combat=true;Step(confidence,42,.25f);var snapshot=Read(confidence);
        Check(orders.PresenceLeader==null && snapshot.Cohesion==PartyCohesion.Together,"Actual bound leader retains support during combat");
        Check(snapshot.OverwhelmingPressure01>0 && snapshot.Confidence01>.6,"Observed heavy pressure together does not erode confidence");
        Check(snapshot.PerceivedThreats==5 && snapshot.PerceivedSupport==1,"Bounded sensed actors feed confidence");
        Check(Physics.lastMask==30,"Only all four validated native character layers are queried");
        Check(snapshot.Courage01==.5,"Legacy absent profile receives neutral courage");
        double before=snapshot.Confidence01;person.transform.position=new Vector3(30,0,0);Step(confidence,32,.25f);snapshot=Read(confidence);
        Check(snapshot.Cohesion==PartyCohesion.Separated && snapshot.Confidence01<before,"Same pressure separated erodes after grace");
        before=snapshot.Confidence01;person.transform.position=new Vector3(2,0,0);Step(confidence,2,.25f);snapshot=Read(confidence);
        Check(snapshot.Confidence01>before&&snapshot.Confidence01-before<.01,"Reunion gradual through actual adapter");
        before=snapshot.Confidence01;leader.visible=false;Step(confidence,8,.25f);snapshot=Read(confidence);
        Check(snapshot.Confidence01==before&&snapshot.Cohesion==PartyCohesion.Together,"Brief sight occlusion protected by grace");
        leader.audible=true;Step(confidence,2,.25f);Check(Read(confidence).PerceivedSupport==1,"Hearing sustains support without sight");
        leader.visible=true;leader.audible=false;person.transform.position=new Vector3(15,0,0);Step(confidence,2,.25f);
        Check(Read(confidence).Cohesion==PartyCohesion.Together,"Support exit hysteresis retains 15m contact");
        person.transform.position=new Vector3(30,0,0);Step(confidence,20,.25f);person.transform.position=new Vector3(15,0,0);Step(confidence,2,.25f);
        Check(Read(confidence).Cohesion==PartyCohesion.Separated,"15m does not instantly reenter separated contact");
        orders.leader=null;before=Read(confidence).Confidence01;Step(confidence,24,.25f);snapshot=Read(confidence);
        Check(snapshot.Confidence01==before && snapshot.Leader==ConfidenceLeaderState.Unloaded&&snapshot.Cohesion==PartyCohesion.Unknown,"Unloaded is unknown, no invented separation or offline drain");
        orders.leader=leader;leader.dead=true;Step(confidence,20,.25f);snapshot=Read(confidence);
        Check(snapshot.Leader==ConfidenceLeaderState.LoadedDead&&snapshot.Cohesion==PartyCohesion.Separated,"Dead loaded leader distinct from unloaded, cannot provide support");
        // A same-party, remote-owned peer supports the actor even with no leader loaded.
        orders.leader=null;var peer=Human();peer.Get<ZNetView>().owner=false;peer.Get<HumanAppearance>().Initialized=false;
        peer.Get<ZNetView>().data.data[CompanionPolicy.RecordKey]=CompanionPolicy.Write(new CompanionOrderRecord{OwnerId=7,Order=CompanionOrder.Hold});
        peer.transform.position=new Vector3(2,0,0);var peerCollider=peer.Add<Collider>();Physics.scene.Add(peerCollider);Step(confidence,2,.25f);
        Check(Read(confidence).Cohesion==PartyCohesion.Together,"Loaded perceivable same-party remote peer supports without leader");
        peer.Get<ZNetView>().data.data[CompanionPolicy.RecordKey]=CompanionPolicy.Write(new CompanionOrderRecord{OwnerId=99,Order=CompanionOrder.Hold});Step(confidence,2,.25f);
        Check(Read(confidence).PerceivedSupport==0,"Foreign-party human never supplies support");
        // Buffer saturation is incomplete evidence, even when no support is returned.
        Physics.scene.Clear();for(int n=0;n<ConfidencePolicy.ColliderLimit;n++)Physics.scene.Add(new GameObject().Add<Collider>());
        orders.leader=leader;leader.dead=false;person.transform.position=new Vector3(30,0,0);before=Read(confidence).Confidence01;Step(confidence,30,.25f);
        Check(Read(confidence).Confidence01==before&&Read(confidence).Cohesion==PartyCohesion.Unknown,"Saturated scan freezes confidence");
        // All queries and native perception work have fixed per-observation bounds.
        Physics.scene.Clear();for(int n=0;n<30;n++)Physics.scene.Add(Threat(body));int q=Physics.queries,senses=ai.senses;Step(confidence,2,.25f);
        Check(Physics.queries-q==1&&ai.senses-senses<=ConfidencePolicy.ActorLimit*2,"One bounded overlap and at most 16 actor perception checks");
        Check(Read(confidence).Cohesion==PartyCohesion.Unknown,"Actor cap is unknown, never false separation");
        q=Physics.queries;Step(confidence,100,.01f);Check(Physics.queries-q<=2,"At most two scans per second");
        q=Physics.queries;Step(confidence,1,100f);Check(Physics.queries-q<=1,"Long stall never triggers scan catch-up burst");
        int writes=view.data.writes;q=Physics.queries;view.owner=false;Step(confidence,40,.25f);
        Check(view.data.writes==writes&&Physics.queries==q,"Authority loss prevents confidence writes and scans");
        snapshot=Read(confidence);Check(snapshot.Cohesion==PartyCohesion.Unknown,"Remote snapshot never exposes stale local cohesion");
        view.data.data[HumanPartyConfidence.RecordKey]="1|23|0.8";view.owner=true;Physics.scene.Clear();orders.leader=null;Step(confidence,3,.25f);snapshot=Read(confidence);
        Check(Math.Abs(snapshot.Confidence01-.23)<.000001&&Math.Abs(snapshot.Courage01-.8)<.000001,"Authority reacquisition restores saved resource/trait, never refills");
        q=Physics.queries;writes=view.data.writes;npc.Get<HumanCompositionIdentity>().IsReady=false;Step(confidence,20,.25f);
        Check(Physics.queries==q&&view.data.writes==writes&&!confidence.TryGetSnapshot(out unused),"Composition identity gates all sampling and reads");
        var corrupt=Human();var broken=corrupt.Add<HumanPartyConfidence>();corrupt.Get<ZNetView>().data.data[HumanPartyConfidence.RecordKey]="1|NaN|.5";q=Physics.queries;Step(broken,40,.25f);
        Check(corrupt.Get<ZNetView>().data.writes==0&&corrupt.Get<ZNetView>().data.GetString(HumanPartyConfidence.RecordKey,"")=="1|NaN|.5"&&Physics.queries==q&&!broken.TryGetSnapshot(out unused),"Malformed save preserved, service fails closed without scans/refill");
        LayerMask.missing=true;var missingLayers=Human();var missingSensor=missingLayers.Add<HumanPartyConfidence>();q=Physics.queries;Step(missingSensor,20,.25f);
        Check(Physics.queries==q&&Read(missingSensor).Cohesion==PartyCohesion.Unknown,"Missing native layer fails to unknown with no broad-layer fallback");LayerMask.missing=false;
        var named=Human();var profileIdentity=named.Add<HumanIdentity>();profileIdentity.state=HumanProfileState.Ready;profileIdentity.profile=new HumanProfile{InitialCourage=83};
        var namedConfidence=named.Add<HumanPartyConfidence>();Step(namedConfidence,3,.25f);snapshot=Read(namedConfidence);
        Check(snapshot.Courage01==.83,"First confidence record seeds courage from saved identity percentage");
        profileIdentity.profile.InitialCourage=10;Call(namedConfidence,"OnDisable");Step(namedConfidence,3,.25f);
        Check(Read(namedConfidence).Courage01==.83,"Existing courage stays saved rather than reseeded on reacquisition");
        profileIdentity.state=HumanProfileState.Refused;q=Physics.queries;writes=named.Get<ZNetView>().data.writes;Step(namedConfidence,20,.25f);Call(namedConfidence,"OnDisable");
        Check(Physics.queries==q&&named.Get<ZNetView>().data.writes==writes&&!namedConfidence.TryGetSnapshot(out unused),"Malformed profile gates reads, scans and saved confidence writes");
        var invalidProfile=Human();var invalidIdentity=invalidProfile.Add<HumanIdentity>();invalidIdentity.state=HumanProfileState.Refused;var invalidConfidence=invalidProfile.Add<HumanPartyConfidence>();Step(invalidConfidence,3,.25f);
        Check(invalidProfile.Get<ZNetView>().data.writes==0,"Malformed profile never receives neutral fallback confidence");
        Console.WriteLine("ConfidenceAdapterHarness PASS: "+checks+" actual-adapter checks with managed doubles. Unity sensing NOT RUN.");
    }
}
