// Managed test doubles only. No claims of Unity physics/visual verification.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public class Collider : Component { public bool isTrigger,blocksPoint; }
    public class MeshFilter : Component {}
    public class MeshRenderer : Component {}
    public class AudioSource : Component {}
    public class ParticleSystem : Component {}
    public class ParticleSystemRenderer : Component {}
    public class AnimationClip : Object {public AnimationEvent[] events=new AnimationEvent[0];}
    public class RuntimeAnimatorController : Object {public AnimationClip[] animationClips=new AnimationClip[0];}
    public class Animator : Component
    {
        public RuntimeAnimatorController runtimeAnimatorController=new RuntimeAnimatorController();
        public readonly Dictionary<string,int> ints=new Dictionary<string,int>();
        public int GetInteger(string key){int value;return ints.TryGetValue(key,out value)?value:0;}
    }
    public struct AnimatorClipInfo {public AnimationClip clip;}
    public class AnimationEvent
    {
        public string functionName,stringParameter;
        public Object objectReferenceParameter;
        public int intParameter;
        public AnimatorClipInfo animatorClipInfo;
    }
    public struct RaycastHit {public Collider collider;public float distance;public Vector3 normal,point;}
    public enum QueryTriggerInteraction {Ignore,Collide}
    public static class Physics
    {
        public const int DefaultRaycastLayers=1;
        public static readonly List<Collider> world=new List<Collider>();
        public static bool fullScan,blocked,groundMissing;
        public static int scans,rays,capsules;
        static readonly Collider ground=new GameObject().Add<Collider>();
        public static int OverlapSphereNonAlloc(Vector3 at,float radius,Collider[] result,int mask,QueryTriggerInteraction q)
        {++scans;if(fullScan)return result.Length;int n=0;foreach(var c in world)if(Vector3.Distance(at,c.transform.position)<=radius&&n<result.Length)result[n++]=c;return n;}
        public static int OverlapCapsuleNonAlloc(Vector3 a,Vector3 b,float radius,Collider[] result,int mask,QueryTriggerInteraction q)
        {++capsules;int n=0;foreach(var c in world)if(c.blocksPoint&&Vector3.Distance(a-Vector3.up*.45f,c.transform.position)<.75f&&n<result.Length)result[n++]=c;return n;}
        public static int RaycastNonAlloc(Vector3 origin,Vector3 dir,RaycastHit[] result,float distance,int mask,QueryTriggerInteraction q)
        {++rays;if(dir.y<-.99f){if(groundMissing)return 0;result[0]=new RaycastHit{collider=ground,point=new Vector3(origin.x,0,origin.z),normal=Vector3.up,distance=origin.y};return 1;}if(blocked){result[0]=new RaycastHit{collider=ground};return 1;}return 0;}
        public static void Reset(){world.Clear();fullScan=blocked=groundMissing=false;scans=rays=capsules=0;}
    }
}
public class Attack {public bool done;public bool IsDone(){return done;}}
public class Humanoid : Character
{
    public object m_leftItem,m_rightItem,m_hiddenLeftItem,m_hiddenRightItem;
    public Attack m_currentAttack;
    public int hides,shows;
    public bool HideHandItems(bool rightOnly,bool effects)
    {++hides;if(m_leftItem==null&&m_rightItem==null)return false;m_hiddenLeftItem=m_leftItem;m_hiddenRightItem=m_rightItem;m_leftItem=m_rightItem=null;return true;}
    public void ShowHandItems(bool rightOnly,bool effects)
    {++shows;m_leftItem=m_hiddenLeftItem;m_rightItem=m_hiddenRightItem;m_hiddenLeftItem=m_hiddenRightItem=null;}
}
public class AnimationEffect : MonoBehaviour
{
    public List<GameObject> m_attachments=new List<GameObject>();
    public void Attach(AnimationEvent e){}
    public void Effect(AnimationEvent e){}
}
public class TimedDestruction : MonoBehaviour {}
public class ZSFX : MonoBehaviour {}
public class CraftingStation : MonoBehaviour
{
    public int m_useAnimation=1;
    public bool m_craftRequireFire,m_craftRequireRoof;
    public Transform m_roofCheckPoint;
}
public class Piece : MonoBehaviour {public long creator=42;public long GetCreator(){return creator;}}
public static class PrivateArea {public static bool allowed=true;public static bool CheckAccess(Vector3 at,float r,bool flash,bool ward){return allowed;}}
public static class Cover {public static bool roof=true;public static void GetCoverForPoint(Vector3 at,out float cover,out bool covered,float radius){cover=1;covered=roof;}}
public class Aoe : MonoBehaviour {}
public class Smoke : MonoBehaviour {}
public static class EffectArea {public static bool burning;public static bool IsPointPlus025InsideBurningArea(Vector3 at){return burning;}}
public class Pathfinding
{
    public static Pathfinding instance=new Pathfinding();
    public bool reachable=true;public int queries;
    public bool GetPath(Vector3 a,Vector3 b,List<Vector3> path,int agent,bool a1,bool a2,bool a3){++queries;if(reachable)path.Add(b);return reachable;}
}
public static class InventoryGui {public static bool visible;public static bool IsVisible(){return visible;}}
public static class StoreGui {public static bool visible;public static bool IsVisible(){return visible;}}
