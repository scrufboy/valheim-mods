// Executes the actual presence/posture adapters against small managed API
// doubles. This is lifecycle/decision coverage, NOT Unity animation/physics.
using System;
using System.Collections.Generic;
using System.Reflection;
using LivingWorld.HumanNpc;
using UnityEngine;

namespace UnityEngine
{
    public class Object
    {
        static int next; readonly int id=++next;
        public string name=""; public bool destroyed;
        public int GetInstanceID(){return id;}
        public static void Destroy(Object x){if(x!=null)x.destroyed=true;}
    }
    public class GameObject : Object
    {
        public Transform transform;
        public GameObject(){transform=new Transform();transform.gameObject=this;components.Add(transform);}
        public readonly List<Component> components = new List<Component>();
        public T Add<T>() where T : Component, new() { var x = new T(); x.gameObject = this; components.Add(x); return x; }
        public T Get<T>() where T : class { foreach(var x in components) if(x is T) return x as T; return null; }
        public T[] GetComponentsInChildren<T>(bool all) where T:class {var found=new List<T>();foreach(var x in components)if(x is T)found.Add(x as T);return found.ToArray();}
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : class { return gameObject.Get<T>(); }
        public T GetComponentInParent<T>() where T : class { return GetComponent<T>(); }
        public T GetComponentInChildren<T>() where T : class { return GetComponent<T>(); }
    }
    public class MonoBehaviour : Component { public bool isActiveAndEnabled = true; }
    public class Transform : Component { public Vector3 position,forward=new Vector3(0,0,1),right=new Vector3(1,0,0); }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float a,float b,float c) { x=a;y=b;z=c; }
        public float sqrMagnitude { get { return x*x+y*y+z*z; } }
        public float magnitude {get{return (float)Math.Sqrt(sqrMagnitude);}}
        public Vector3 normalized {get{return magnitude<.001f?new Vector3():this*(1f/magnitude);}}
        public static Vector3 up {get{return new Vector3(0,1,0);}}
        public static Vector3 down {get{return new Vector3(0,-1,0);}}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
        public static Vector3 operator *(Vector3 a,float n){return new Vector3(a.x*n,a.y*n,a.z*n);}
        public static float Angle(Vector3 a,Vector3 b){float d=a.magnitude*b.magnitude;return d<.001f?0:(float)(Math.Acos(Math.Max(-1,Math.Min(1,(a.x*b.x+a.y*b.y+a.z*b.z)/d)))*180/Math.PI);}
        public static float Distance(Vector3 a,Vector3 b) { return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z)); }
    }
    public static class Time { public static float time; }
}
namespace HarmonyLib
{
    public class Harmony { public void Patch(MethodInfo method,HarmonyMethod prefix=null,HarmonyMethod postfix=null){} }
    public class HarmonyMethod { public HarmonyMethod(MethodInfo method) {} }
}
public class BaseAI : MonoBehaviour
{
    public int moves, stops,looks,resets,m_pathAgentType;
    public float m_lastFindPathTime;
    public Vector3 lastGoal;
    public bool lastRun,moveCompleted;
    public void Follow(GameObject go,float dt) {}
    public void StopMoving() { ++stops; }
    public void ResetRandomMovement() { ++resets; }
    public bool MoveTo(float dt,Vector3 point,float stop,bool run) { ++moves;lastGoal=point;lastRun=run;return moveCompleted; }
    public void LookAt(Vector3 point){++looks;var d=point-transform.position;d.y=0;transform.forward=d.normalized;}
}
public class Character : MonoBehaviour
{
    public bool dead,swimming,flying,attacking,bowing,blocking;
    public bool grounded = true;
    public Vector3 velocity;
    public Vector3 GetVelocity(){return velocity;}
    public virtual bool IsCrouching() { return false; }
    public virtual bool IsSitting() { return false; }
    public bool IsEncumbered() { return false; }
    public bool IsDead(){return dead;}
    public bool IsOnGround(){return grounded;}
    public bool IsSwimming(){return swimming;}
    public bool IsFlying(){return flying;}
    public bool InAttack(){return attacking;}
    public bool IsDrawingBow(){return bowing;}
    public bool IsBlocking(){return blocking;}
}
public class Player : Character
{
    public static Player m_localPlayer;
    public long playerId=42;
    public CraftingStation currentStation;
    public bool placement;
    public Transform m_eye = new Transform();
    public bool crouched,emote;
    public override bool IsCrouching(){return crouched;}
    public bool InEmote(){return emote;}
    public long GetPlayerID(){return playerId;}
    public CraftingStation GetCurrentCraftingStation(){return currentStation;}
    public bool InPlaceMode(){return placement;}
}
public class ZDO { public string emote=""; public string GetString(int key,string fallback){return emote;} }
public static class ZDOVars { public const int s_emote=1; }
public class ZNetView : MonoBehaviour
{
    public bool valid=true,owner=true;
    public ZDO data=new ZDO();
    public bool IsValid(){return valid;} public bool IsOwner(){return owner;} public ZDO GetZDO(){return data;}
}
public class ZSyncAnimation : MonoBehaviour
{
    public int writes;
    public readonly Dictionary<string,bool> values=new Dictionary<string,bool>();
    public void SetBool(string key,bool value){values[key]=value;++writes;}
    public void SetTrigger(string key){++writes;}
    public void SetInt(string key,int value){var a=GetComponent<Animator>();if(a!=null)a.ints[key]=value;++writes;}
}
public class CharacterAnimEvent : MonoBehaviour
{
    public Transform m_lookAt;
    public float m_lookWeight=.5f,m_bodyLookWeight=.1f;
    public void UpdateHeadRotation(float dt) {}
}
namespace LivingWorld.HumanNpc
{
    public class HumanCompanion : MonoBehaviour
    {
        public Player leader;
        public Character awareness;
        public bool eligible=true,combat;
        public Player PresenceLeader { get { return eligible && !combat ? leader : null; } }
        public Player PartyLeader { get { return eligible ? leader : null; } }
        public bool HasAuthorizedCombat { get { return combat; } }
        public bool CombatReadinessActive { get { return combat; } }
        public bool CombatOwnsActor { get { return combat; } }
        public Character AwarenessTarget { get { return awareness; } }
    }
    public class HumanDoorTraversal : MonoBehaviour
    {
        public bool blocked;
        public bool NeedsCloseFollow(GameObject target){return blocked;}
        public bool MayRestNear(GameObject target){return !blocked;}
    }
}
namespace LivingWorld.ManualTest
{
    public class ManualTestPlugin { public static ManualTestPlugin Instance; public void Report(string text){} }
}
static class PresenceAdapterHarness
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Check(bool value,string label){++checks;if(!value)throw new Exception(label);}
    static object Call(object obj,string name,params object[] values){return obj.GetType().GetMethod(name,Hidden).Invoke(obj,values);}
    static void Main()
    {
        var npc=new GameObject(); var ai=npc.Add<BaseAI>(); var body=npc.Add<Humanoid>();
        var view=npc.Add<ZNetView>(); var sync=npc.Add<ZSyncAnimation>();
        var orders=npc.Add<HumanCompanion>(); var doors=npc.Add<HumanDoorTraversal>();
        var pose=npc.Add<HumanCompanionPosture>(); var presence=npc.Add<HumanFollowPresence>(); var gaze=npc.Add<CharacterAnimEvent>();
        var person=new GameObject(); var leader=person.Add<Player>(); var leaderView=person.Add<ZNetView>(); orders.leader=leader;
        HumanFollowPresence.InstallPatches(new HarmonyLib.Harmony()); HumanCompanionPosture.InstallPatches(new HarmonyLib.Harmony());
        Time.time=100; npc.transform.position=new Vector3(0,0,8);
        Check((bool)Call(presence,"Follow",person,.02f),"Own active following handled");
        Check(ai.moves==1&&ai.lastRun&&Vector3.Distance(ai.lastGoal,leader.transform.position)>=CompanionPresencePolicy.LeaderExclusionRadius,
            "Catch up via native MoveTo without routing into leader exclusion");
        npc.transform.position=new Vector3(0,0,14);ai.moveCompleted=true;Time.time=101;Call(presence,"Follow",person,.02f);ai.moveCompleted=false;
        Check(ai.resets==1&&Single.IsNegativeInfinity(ai.m_lastFindPathTime),
            "Failed far route invalidates native path cache without teleporting");
        Call(presence,"Attention",gaze);
        Check(gaze.m_lookAt==leader.m_eye&&gaze.m_bodyLookWeight==0&&gaze.m_lookWeight==.75f,"Head looks at eye, no torso twisting");
        npc.transform.position=new Vector3(0,0,4);Call(presence,"Follow",person,.02f);Check(ai.lastRun,"Run hysteresis retained");
        npc.transform.position=new Vector3(0,0,3.4f);Call(presence,"Follow",person,.02f);Check(!ai.lastRun,"Ease to walking");
        npc.transform.position=new Vector3(0,0,2.2f);int moves=ai.moves;Call(presence,"Follow",person,.02f);Check(ai.moves==moves,"Stop near person");
        npc.transform.position=new Vector3(0,0,2.8f);Call(presence,"Follow",person,.02f);Check(ai.moves==moves,"No micro chase in stop band");
        doors.blocked=true;Call(presence,"Follow",person,.02f);Check(ai.moves==moves+1&&!ai.lastRun,"Door-cross override wins over stop band");doors.blocked=false;
        npc.transform.position=new Vector3(0,0,2);Call(presence,"Follow",person,.02f);
        Time.time=111;Call(presence,"Attention",gaze);Check(gaze.m_lookAt==leader.m_eye,"Attention maintained through quiet delay");
        Time.time=113;Call(presence,"Attention",gaze);Check(gaze.m_lookAt==null&&gaze.m_lookWeight==.5f&&gaze.m_bodyLookWeight==.1f,"Quiet idle restores native gaze weights");
        person.transform.position=new Vector3(.2f,0,0);Call(presence,"Attention",gaze);Check(gaze.m_lookAt==leader.m_eye,"Leader motion regains attention");
        orders.combat=true;Call(presence,"Attention",gaze);Check(gaze.m_lookAt==null,"Combat immediately releases head to native target");
        Check(!(bool)Call(presence,"Follow",person,.02f),"No movement override during combat");orders.combat=false;
        var foreign=new Transform();gaze.m_lookAt=foreign;Call(presence,"Attention",gaze);Check(gaze.m_lookAt==foreign&&gaze.m_lookWeight==.5f,"External explicit focus not stolen");gaze.m_lookAt=null;
        Call(presence,"Attention",gaze);orders.eligible=false;Call(presence,"Attention",gaze);Check(gaze.m_lookAt==null,"Authority/order loss releases attention");orders.eligible=true;
        leader.crouched=true;npc.transform.position=new Vector3(0,0,8);Call(presence,"Follow",person,.02f);
        Check(pose.Posture==CompanionPosture.Crouch&&sync.values["crouching"]&&!ai.lastRun,"Crouching uses native parameter and suppresses running");
        leader.crouched=false;Call(presence,"Follow",person,.02f);Check(pose.Posture==CompanionPosture.Upright&&!sync.values["crouching"],"Standing leader releases crouch");
        npc.transform.position=new Vector3(0,0,2);leader.emote=true;leaderView.data.emote="sit";Time.time=200;pose.Refresh();
        Check(pose.Posture==CompanionPosture.Upright,"Do not sit instantly");Time.time=202;pose.Refresh();
        Check(pose.Posture==CompanionPosture.Sit&&sync.values["emote_sit"],"Settle and use native looping sit");
        moves=ai.moves;Call(presence,"Follow",person,.02f);Check(ai.moves==moves,"Seated companion does not receive travel commands");
        orders.combat=true;pose.Refresh();Check(pose.Posture==CompanionPosture.Upright&&!sync.values["emote_sit"],"Combat clears sit loop");orders.combat=false;
        doors.blocked=true;Time.time=210;pose.Refresh();Time.time=220;pose.Refresh();Check(pose.Posture==CompanionPosture.Upright,"Never sit across blocked passage");doors.blocked=false;
        Time.time=230;pose.Refresh();Time.time=232;pose.Refresh();Check(pose.Posture==CompanionPosture.Sit,"Sit request can resume after clear arrival");
        leader.emote=false;leaderView.data.emote="";pose.Refresh();Check(pose.Posture==CompanionPosture.Upright&&!sync.values["emote_sit"],"Leader stands, companion stands");
        leader.crouched=true;body.swimming=true;pose.Refresh();Check(pose.Posture==CompanionPosture.Upright,"No crouch while swimming");body.swimming=false;
        int writes=sync.writes;view.owner=false;orders.eligible=false;pose.Refresh();Check(sync.writes==writes,"Remote instance does not write animation state");
        view.owner=true;orders.eligible=true;pose.Refresh();Check(pose.Posture==CompanionPosture.Crouch,"New authority recomputes posture, not stale pose");
        Call(pose,"OnDisable");Check(!sync.values["crouching"]&&!sync.values["emote_sit"],"Disable cleans owned pose");
        Call(presence,"Attention",gaze);Call(presence,"OnDisable");Check(gaze.m_lookAt==null,"Disable cleans owned attention");
        Console.WriteLine("PresenceAdapterHarness PASS: "+checks+" actual-adapter checks with managed doubles. Unity NOT RUN.");
    }
}
