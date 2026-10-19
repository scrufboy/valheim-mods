using System;
using System.Reflection;
using LivingWorld.HumanNpc;
using UnityEngine;

static class AmbientAdapterHarness
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok,string message){++checks;if(!ok)throw new Exception(message);}
    static object Call(object target,string method,params object[] args){return target.GetType().GetMethod(method,Hidden).Invoke(target,args);}
    static object Static(string method,params object[] args){return typeof(HumanCompanionPosture).GetMethod(method,Hidden).Invoke(null,args);}
    sealed class Fixture
    {
        public GameObject npc=new GameObject(),person=new GameObject();
        public BaseAI ai;public Humanoid body;public ZNetView net;public ZSyncAnimation sync;
        public Player leader;public HumanCompanion orders;public HumanDoorTraversal doors;
        public HumanCompanionPosture posture;public HumanCompanionAmbient ambient;public HumanFollowPresence presence;public Animator animator;
        public object sword=new object(),shield=new object();
        public float selectedAt, arrivedAt;
        public Fixture(bool reset=true)
        {
            if(reset){Physics.Reset();Pathfinding.instance=new Pathfinding();PrivateArea.allowed=true;EffectArea.burning=false;InventoryGui.visible=true;StoreGui.visible=false;Time.time=100;}
            ai=npc.Add<BaseAI>();body=npc.Add<Humanoid>();body.m_rightItem=sword;body.m_leftItem=shield;
            net=npc.Add<ZNetView>();sync=npc.Add<ZSyncAnimation>();animator=npc.Add<Animator>();
            animator.runtimeAnimatorController.animationClips=new[]{new AnimationClip{name="Workbench"},ForgeClip()};
            orders=npc.Add<HumanCompanion>();doors=npc.Add<HumanDoorTraversal>();posture=npc.Add<HumanCompanionPosture>();
            ambient=npc.Add<HumanCompanionAmbient>();presence=npc.Add<HumanFollowPresence>();
            leader=person.Add<Player>();person.Add<ZNetView>();orders.leader=leader;Player.m_localPlayer=leader;
            npc.transform.position=new Vector3(-2,0,0);
        }
        public void Step(float time){Time.time=time;Call(presence,"Follow",person,.1f);}
        public void Select(){if(Time.time<112)Step(112);for(float at=147;at<=252&&!ambient.Active;at+=35)Step(at);selectedAt=Time.time;}
        public void Arrive()
        {
            for(int i=0;i<16&&!ambient.Performing;i++)
            { arrivedAt=selectedAt+1+i*.1f;npc.transform.position=ai.lastGoal;Step(arrivedAt); }
        }
        public void Clean(){ambient.Cancel("harness teardown");}
    }
    static GameObject Prop(string name,bool effect)
    {var go=new GameObject{name=name};if(effect){go.Add<TimedDestruction>();go.Add<ZSFX>();go.Add<AudioSource>();}else{go.Add<MeshFilter>();go.Add<MeshRenderer>();}return go;}
    static AnimationClip ForgeClip()
    {
        var clip=new AnimationClip{name="Forge"};
        clip.events=new[]{new AnimationEvent{functionName="Attach",stringParameter="RightHand_Attach",objectReferenceParameter=Prop("aniattach_forge_hammer",false)},
            new AnimationEvent{functionName="Effect",stringParameter="RightHand_Attach",objectReferenceParameter=Prop("fx_forge_hammer",true)}};
        foreach(var e in clip.events)e.animatorClipInfo=new AnimatorClipInfo{clip=clip};return clip;
    }
    static CraftingStation Station(float x=3,int kind=1)
    {var go=new GameObject{name=kind==1?"piece_workbench(Clone)":"forge(Clone)"};go.transform.position=new Vector3(x,0,0);var s=go.Add<CraftingStation>();s.m_useAnimation=kind;go.Add<Piece>();go.Add<ZNetView>();Physics.world.Add(go.Add<Collider>());return s;}
    static void Main()
    {
        HumanCompanionPosture.InstallPatches(new HarmonyLib.Harmony());
        var f=new Fixture();Station();f.Step(100);f.Step(111);
        Check(!f.ambient.Active&&f.body.hides==0,"quiet delay never animates or hides equipment");
        f.Select();Check(f.ambient.Active&&!f.ambient.Performing,"ordinary manually named station selected without lab/maypole tags");
        Check(Vector3.Distance(f.ai.lastGoal,f.leader.transform.position)>=CompanionPresencePolicy.LeaderExclusionRadius&&!f.ai.lastRun,
            "sole follow executor receives safe walking waypoint outside leader exclusion");
        Check(f.body.hides==0&&f.animator.GetInteger("crafting")==0,"no crafting before arrival");
        var committed=f.ai.lastGoal;int scans=Physics.scans,paths=Pathfinding.instance.queries;
        f.person.transform.forward=new Vector3(1,0,0);Station(1);f.Step(f.selectedAt+.2f);
        Check(Vector3.Distance(committed,f.ai.lastGoal)==0&&Physics.scans==scans&&Pathfinding.instance.queries==paths,"camera/new better station cannot retarget or rescan commitment");
        f.Arrive();Check(f.ambient.Performing&&f.animator.GetInteger("crafting")==1,"native workbench parameter starts only after arrival and native facing");
        Check(f.body.hides==1&&f.body.m_rightItem==null&&ReferenceEquals(f.body.m_hiddenRightItem,f.sword),"native hide lease holds original sword, no fabricated tool");
        int moves=f.ai.moves;f.Step(f.arrivedAt+1);Check(f.ai.moves==moves&&f.body.hides==1,"performing stays stopped and does not repeatedly hide hands");
        f.person.transform.position=new Vector3(.2f,0,0);f.Step(f.arrivedAt+1.1f);
        Check(!f.ambient.Active&&f.animator.GetInteger("crafting")==0&&ReferenceEquals(f.body.m_rightItem,f.sword)&&ReferenceEquals(f.body.m_leftItem,f.shield),"leader movement cancels pose and restores exact original hand items");
        Check(f.body.shows==1,"one native restoration per lease");f.Clean();

        f=new Fixture();Station(3,2);f.Select();f.Arrive();
        Check(f.animator.GetInteger("crafting")==2,"vetted native forge pose supported");
        var effects=f.npc.Add<AnimationEffect>();var forge=f.animator.runtimeAnimatorController.animationClips[1];
        Check((bool)Static("BeforeCraftEvent",effects,forge.events[0]),"owned active forge permits only inspected Attach event");
        var attached=Prop("aniattach_forge_hammer(Clone)",false);effects.m_attachments.Add(attached);Static("AfterCraftAttach",effects,forge.events[0]);
        f.ambient.Cancel("order change");
        Check(attached.destroyed&&effects.m_attachments.Count==0,"cancellation removes only captured local cosmetic hammer");
        Check(!(bool)Static("BeforeCraftEvent",effects,forge.events[0])&&!(bool)Static("BeforeCraftEvent",effects,forge.events[1]),"late Attach/Effect denied after crafting parameter cleared");

        f=new Fixture();Station();f.Select();f.Arrive();f.body.m_currentAttack=new Attack();
        f.ambient.Cancel("threat");Check(f.body.shows==0&&f.posture.OwnsAmbientHands,"unfinished attack defers equipment restoration even if animation tag is idle");
        f.body.m_currentAttack.done=true;f.posture.Refresh();Check(f.body.shows==1&&!f.posture.OwnsAmbientHands,"finished native attack permits pending hand restoration");

        f=new Fixture();Station();f.Select();f.Arrive();object newer=new object();f.body.m_rightItem=newer;f.ambient.Cancel("external equipment");
        Check(ReferenceEquals(f.body.m_rightItem,newer)&&f.body.shows==0,"never overwrite a newer equipment decision");

        f=new Fixture();Station();f.Select();f.Arrive();int writes=f.sync.writes;f.net.owner=false;Call(f.ambient,"Update");
        Check(!f.ambient.Active&&f.sync.writes==writes&&f.body.shows==0,"owner loss cancels local intent with zero network/equipment writes");
        f.net.owner=true;f.posture.Refresh();Check(f.body.shows==1&&f.animator.GetInteger("crafting")==0,"reacquisition restores pending original hands and clears stale crafting");

        f=new Fixture();Station();f.Select();f.Arrive();f.orders.combat=true;Call(f.ambient,"Update");
        Check(!f.ambient.Active&&f.animator.GetInteger("crafting")==0,"combat/order eligibility loss promptly clears optional pose");
        f=new Fixture();Station();f.Select();f.leader.crouched=true;f.Step(f.selectedAt+.1f);
        Check(!f.ambient.Active&&f.posture.Posture==CompanionPosture.Crouch,"crouch cancels activity and keeps native crouch posture");
        f=new Fixture();Station();f.Select();f.doors.blocked=true;f.Step(f.selectedAt+.1f);
        Check(!f.ambient.Active&&Vector3.Distance(f.ai.lastGoal,f.leader.transform.position)<.01f,
            "door precedence cancels ambience and preserves the real leader route anchor");
        f=new Fixture();Station();f.Select();f.leader.emote=true;f.person.Get<ZNetView>().data.emote="sit";f.Step(f.selectedAt+.1f);
        Check(!f.ambient.Active&&f.animator.GetInteger("crafting")==0,"player sit cancels optional crafting; baseline sit timing remains separate");

        f=new Fixture();Station();f.Select();f.Step(f.selectedAt+6);
        Check(!f.ambient.Active&&f.body.hides==0,"stalled approach fails after bounded progress deadline");
        scans=Physics.scans;f.Step(f.selectedAt+7);Check(Physics.scans==scans,"failed approach observes scan cooldown");
        f=new Fixture();Station();f.Select();f.Arrive();f.Step(f.arrivedAt+60);
        Check(f.ambient.Active&&f.animator.GetInteger("crafting")==1,"unchanged safe workstation session extends without destination bounce");
        f.Step(f.arrivedAt+121);
        Check(!f.ambient.Active&&f.animator.GetInteger("crafting")==0,"sustained ambient work ends and restores equipment");

        f=new Fixture();var station=Station();station.GetComponent<Piece>().creator=77;f.Select();f.Arrive();
        Check(f.animator.GetInteger("crafting")==0&&f.body.hides==0,"foreign owner station refused; calm guard remains possible");f.Clean();
        f=new Fixture();station=Station();station.GetComponent<Piece>().creator=0;f.Select();
        Check(f.ai.lastGoal.x!=3,"unowned/unknown creator stations are not silently granted");f.Clean();
        f=new Fixture();Station();PrivateArea.allowed=false;f.Select();Check(f.body.hides==0&&f.ai.lastGoal.x!=3,"ward access denial refuses station");f.Clean();
        f=new Fixture();station=Station();f.Select();station.GetComponent<ZNetView>().owner=false;f.Step(f.selectedAt+.4f);
        Check(!f.ambient.Active,"station authority transfer invalidates local reservation");
        f=new Fixture();Station();var occupant=new GameObject();occupant.transform.position=new Vector3(3,0,1.35f);var blocker=occupant.Add<Collider>();blocker.blocksPoint=true;Physics.world.Add(blocker);f.Select();
        Check(f.ai.lastGoal.x!=3,"occupied use point is rejected");f.Clean();
        f=new Fixture();Station();EffectArea.burning=true;f.Select();Check(!f.ambient.Active,"burning point rejects craft and guard");
        f=new Fixture();Station();Physics.blocked=true;f.Select();Check(f.ai.lastGoal.x!=3,"wall/closed door along optional route rejects station instead of competing door route");f.Clean();
        f=new Fixture();Station();Pathfinding.instance.reachable=false;f.Select();Check(f.ai.lastGoal.x!=3,"incomplete native path rejected");f.Clean();
        f=new Fixture();Station();Physics.fullScan=true;f.Select();Check(f.ai.lastGoal.x!=3,"saturated collider snapshot fails closed");f.Clean();
        f=new Fixture();for(int i=0;i<12;i++)Station(2+i*.25f);f.Select();Check(Physics.scans>0&&Pathfinding.instance.queries<=Physics.scans*6,"native path queries bounded across walk and station candidates");f.Clean();

        f=new Fixture();Station();InventoryGui.visible=false;StoreGui.visible=true;f.Select();Check(f.ambient.Active,"bound local merchant UI supplies real busy context");f.Clean();
        f=new Fixture();Station();InventoryGui.visible=false;f.Select();Check(f.ai.lastGoal.x!=3,"stationary standing prefers local observation over compulsory workstation use");f.Clean();
        f=new Fixture();Station();Player.m_localPlayer=new GameObject().Add<Player>();f.Select();Check(!f.ambient.Active&&Physics.scans==0,"other local player's menus never drive remote bound companion");
        f=new Fixture();Station();f.animator.runtimeAnimatorController.animationClips[0].events=new[]{new AnimationEvent{functionName="Hit"}};f.Select();Check(f.ai.lastGoal.x!=3,"changed workbench Hit event fails closed before pose/item mutation");f.Clean();
        var dangerous=ForgeClip();((GameObject)dangerous.events[0].objectReferenceParameter).Add<Aoe>();
        var a=new GameObject().Add<Animator>();a.runtimeAnimatorController.animationClips=new[]{dangerous};
        Check(!AmbientAnimationContract.Supports(a,2),"damage-bearing forge prop rejected by strict component allowlist");
        Console.WriteLine("AmbientAdapterHarness PASS: "+checks+" actual adapter checks with managed doubles. Unity physics, visuals and multiplayer NOT RUN.");
    }
}
