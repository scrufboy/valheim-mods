using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
namespace LivingVillageCore
{
    public static class AreaWork
    {
        sealed class Job
        {public string Key;public Vector3 Center;public VillageWorker Claimant;public float Deferred;public readonly List<Piece> Pieces=new List<Piece>();}
        sealed class Board
        {public float NextSurvey;public readonly Dictionary<string,Job> Jobs=new Dictionary<string,Job>();}
        sealed class Worker
        {
            public Job Job;public WorkBudget Budget;public WorkState State;
            public Vector3 Position;public float Stroke,RestUntil,Deadline,LastPersist;public int EmptyStrokes;
            public ItemDrop.ItemData Hammer,Previous;
            public MonsterAI AI;public bool AnchorActive,HadPatrol;public Vector3 OriginalPatrol;public float OriginalRange;
        }
        static readonly Dictionary<SettlementCenter,Board> Boards=new Dictionary<SettlementCenter,Board>();
        static readonly Dictionary<VillageWorker,Worker> Workers=new Dictionary<VillageWorker,Worker>();
        static readonly MethodInfo SetPatrol=AccessTools.Method(typeof(BaseAI),"SetPatrolPoint",new Type[]{typeof(Vector3)});
        static readonly MethodInfo ResetPatrol=AccessTools.Method(typeof(BaseAI),"ResetPatrolPoint",Type.EmptyTypes);
        static readonly MethodInfo GetPatrol=AccessTools.Method(typeof(BaseAI),"GetPatrolPoint",new Type[]{typeof(Vector3).MakeByRefType()});
        static readonly FieldInfo RandomRange=AccessTools.Field(typeof(BaseAI),"m_randomMoveRange");
        static readonly MethodInfo Equipped=AccessTools.Method(typeof(Humanoid),"IsItemEquiped",new Type[]{typeof(ItemDrop.ItemData)});
        static readonly FieldInfo Agent=AccessTools.Field(typeof(BaseAI),"m_pathAgentType");
        static readonly FieldInfo Consume=AccessTools.Field(typeof(MonsterAI),"m_consumeTarget");
        static readonly FieldInfo RightItem=AccessTools.Field(typeof(Humanoid),"m_rightItem");
        const float Radius=8f,Height=12f,Cost=5f;
        public static void Validate(){if(SetPatrol==null || ResetPatrol==null || GetPatrol==null || GetPatrol.ReturnType!=typeof(bool) || RandomRange==null || RandomRange.FieldType!=typeof(float) || Equipped==null || Equipped.ReturnType!=typeof(bool) || Agent==null || Consume==null || RightItem==null || RightItem.FieldType!=typeof(ItemDrop.ItemData))throw new MissingMemberException("AreaWork native API");}
        static string Cell(Vector3 p){return Mathf.FloorToInt(p.x/8f)+":"+Mathf.FloorToInt(p.z/8f);}
        static bool Damaged(WearNTear wear){var view=wear?wear.GetComponent<ZNetView>():null;return view && view.IsValid() && wear.GetHealthPercentage()>0 && wear.GetHealthPercentage()<0.98f;}
        static bool InArea(Vector3 p,Vector3 area){var d=p-area;return d.x*d.x+d.z*d.z<=Radius*Radius && Mathf.Abs(d.y)<=Height;}
        static List<Piece> Pieces(SettlementCenter home)
        {var pieces=new List<Piece>();Piece.GetAllPiecesInRadius(home.Center,SettlementOutpost.SurveyRadius(home)+Height,pieces);return pieces.Where(p=>p && home.Contains(p.transform.position) && PrivateArea.CheckAccess(p.transform.position,0f,false,true)).ToList();}
        static Board Survey(SettlementCenter home)
        {
            Board board;if(!Boards.TryGetValue(home,out board)){board=new Board();Boards[home]=board;}
            if(Time.time<board.NextSurvey)return board;board.NextSurvey=Time.time+15f;
            foreach(var stale in Workers.Keys.Where(worker=>!worker).ToArray())Workers.Remove(stale);
            bool steward=Character.GetAllCharacters().Any(c=>SettlementRoster.IsResident(c) && !c.IsDead() && SettlementRoster.Role(c)=="steward" && home.Contains(c.transform.position));
            // A destroyed, disabled, or reassigned worker must never reserve work indefinitely.
            foreach(var job in board.Jobs.Values)
                if(job.Claimant && (!job.Claimant.enabled || job.Claimant.Home!=home || job.Claimant.GetComponent<Character>().IsDead()))job.Claimant=null;
            if(!steward){LivingVillagePlugin.Log.LogInfo("Steward work board unavailable: no loaded living steward");return board;}
            var damage=Pieces(home).Where(p=>Damaged(p.GetComponent<WearNTear>())).ToArray();
            foreach(var job in board.Jobs.Values)job.Pieces.Clear();
            foreach(var group in damage.GroupBy(p=>Cell(p.transform.position)))
            {
                Job job;
                if(!board.Jobs.TryGetValue(group.Key,out job))
                {
                    var p=group.First().transform.position;
                    p.x=(Mathf.FloorToInt(p.x/8f)+0.5f)*8f;p.z=(Mathf.FloorToInt(p.z/8f)+0.5f)*8f;
                    p.y=ZoneSystem.instance.GetGroundHeight(p);
                    job=new Job{Key=group.Key,Center=p};board.Jobs[group.Key]=job;
                }
                job.Pieces.AddRange(group);
            }
            foreach(var key in board.Jobs.Keys.ToArray())if(!board.Jobs[key].Claimant && board.Jobs[key].Pieces.Count==0)board.Jobs.Remove(key);
            LivingVillagePlugin.Log.LogInfo("Steward work report: damaged="+damage.Length+" repairAreas="+board.Jobs.Count);
            return board;
        }
        static bool WorkPosition(MonsterAI ai,Character c,Job job,out Vector3 point)
        {
            var candidates=new List<Vector3>();
            // Work positions must cover actual board tasks, not merely share their cell.
            if(InArea(c.transform.position,job.Center))candidates.Add(c.transform.position);
            candidates.Add(job.Center);
            for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;var p=job.Center+new Vector3(Mathf.Cos(angle)*3f,0,Mathf.Sin(angle)*3f);p.y=ZoneSystem.instance.GetGroundHeight(p);candidates.Add(p);}
            foreach(var p in candidates.OrderByDescending(p=>job.Pieces.Count(piece=>piece && Damaged(piece.GetComponent<WearNTear>()) && InArea(piece.transform.position,p))))
            {
                if(!job.Pieces.Any(piece=>piece && Damaged(piece.GetComponent<WearNTear>()) && InArea(piece.transform.position,p)))continue;
                var path=new List<Vector3>();if(Pathfinding.instance && Pathfinding.instance.GetPath(c.transform.position,p,path,(Pathfinding.AgentType)Agent.GetValue(ai),true,true,false)){point=p;return true;}
            }
            point=Vector3.zero;return false;
        }
        static void Release(Worker state,Humanoid human)
        {
            if(state.AnchorActive && state.AI)
            {
                var aiView=state.AI.GetComponent<ZNetView>();
                if(aiView && aiView.IsValid() && aiView.IsOwner())
                {if(state.HadPatrol)SetPatrol.Invoke(state.AI,new object[]{state.OriginalPatrol});else ResetPatrol.Invoke(state.AI,null);}
                RandomRange.SetValue(state.AI,state.OriginalRange);state.AnchorActive=false;
            }
            var humanView=human?human.GetComponent<ZNetView>():null;
            if(state.Hammer!=null && human && humanView && humanView.IsValid() && humanView.IsOwner() && (bool)Equipped.Invoke(human,new object[]{state.Hammer})){human.UnequipItem(state.Hammer,false);if(state.Previous!=null && human.GetInventory().ContainsItem(state.Previous))human.EquipItem(state.Previous,false);}
            state.Previous=null;
        }
        static void Anchor(Worker state,MonsterAI ai)
        {
            if(state.AnchorActive)return;
            var args=new object[]{Vector3.zero};state.HadPatrol=(bool)GetPatrol.Invoke(ai,args);state.OriginalPatrol=(Vector3)args[0];
            state.OriginalRange=(float)RandomRange.GetValue(ai);state.AI=ai;
            SetPatrol.Invoke(ai,new object[]{state.Position});RandomRange.SetValue(ai,1f);state.AnchorActive=true;
        }
        static void Recover(Worker state,ZNetView view,float dt)
        {
            state.Budget.Rest(Mathf.Min(Mathf.Max(dt,0f),1f));
            if(Time.time>=state.LastPersist+5f){view.GetZDO().Set("living_work_stamina_v1",state.Budget.Current);state.LastPersist=Time.time;}
        }
        static bool Hammer(Worker state,Humanoid human)
        {
            if(state.Hammer==null)
            {
                var prefab=ObjectDB.instance.GetItemPrefab("Hammer");var drop=prefab?prefab.GetComponent<ItemDrop>():null;if(!drop)return false;
                state.Hammer=human.GetInventory().GetItem(drop.m_itemData.m_shared.m_name,-1,false);
                if(state.Hammer==null){state.Hammer=drop.m_itemData.Clone();state.Hammer.m_dropPrefab=prefab;if(!human.GetInventory().AddItem(state.Hammer)){state.Hammer=null;return false;}}
            }
            // Keep the native Hammer a Tool. Never relabel it as a combat weapon.
            if(state.Hammer.m_shared.m_itemType!=ItemDrop.ItemData.ItemType.Tool)return false;
            if(System.Object.ReferenceEquals(RightItem.GetValue(human),state.Hammer))return true;
            if(state.Previous==null)state.Previous=human.GetCurrentWeapon();
            human.EquipItem(state.Hammer,false);
            // EquipItem's return value alone does not establish right-hand ownership.
            return System.Object.ReferenceEquals(RightItem.GetValue(human),state.Hammer);
        }
        public static bool Run(VillageWorker component,float dt)
        {
            var home=component.Home;var c=component.GetComponent<Character>();var human=c as Humanoid;var ai=component.GetComponent<MonsterAI>();var view=component.GetComponent<ZNetView>();
            if(!home || !home.IsOwner || !c || c is Player || !human || !ai || !view || !view.IsValid() || !view.IsOwner())return false;
            Worker state;if(!Workers.TryGetValue(component,out state)){state=new Worker{Budget=new WorkBudget(view.GetZDO().GetFloat("living_work_stamina_v1",100f))};Workers[component]=state;}
            try
            {
                var enemy=ai.GetTargetCreature();
                if(c.IsDead() || !c.IsTamed() || c.GetHealthPercentage()<0.2f || ai.GetFollowTarget() || ai.IsAlerted() || Time.time-home.LastThreatTime<8f || (enemy && !enemy.IsDead() && BaseAI.IsEnemy(c,enemy)) || (Consume.GetValue(ai) as ItemDrop))
                {Release(state,human);state.State=WorkState.Interrupted;Recover(state,view,dt);return false;}
                if(state.State==WorkState.Interrupted)state.Deadline=Time.time+90f;
                if(state.Budget.Current<20f || Time.time<state.RestUntil)
                {Release(state,human);if(state.State!=WorkState.Resting){state.RestUntil=Time.time+8f;LivingVillagePlugin.Log.LogInfo("Worker resting: id="+c.GetZDOID()+" stamina="+state.Budget.Current);}state.State=WorkState.Resting;state.Deadline+=Mathf.Max(dt,0f);Recover(state,view,dt);return false;}
                Board board=Survey(home);
                if(state.Job!=null && state.Job.Claimant!=component){Release(state,human);state.Job=null;}
                if(state.Job==null)
                {
                    foreach(var job in board.Jobs.Values.Where(j=>!j.Claimant && Time.time>=j.Deferred).OrderBy(j=>Vector3.Distance(j.Center,c.transform.position)))
                    {
                        Vector3 position;if(!WorkPosition(ai,c,job,out position)){job.Deferred=Time.time+30f;continue;}
                        state.Job=job;job.Claimant=component;state.Position=position;state.Deadline=Time.time+90;state.EmptyStrokes=0;state.State=WorkState.Travelling;
                        view.GetZDO().Set("living_work_area_v1",job.Center);
                        LivingVillagePlugin.Log.LogInfo("Steward assigned area: id="+c.GetZDOID()+" area="+job.Key+" workPosition="+position);break;
                    }
                    if(state.Job==null){state.State=WorkState.Idle;Recover(state,view,dt);return false;}
                }
                if(Time.time>state.Deadline)
                {state.Job.Claimant=null;state.Job.Deferred=Time.time+30f;state.Job=null;Release(state,human);LivingVillagePlugin.Log.LogInfo("Area job deferred: id="+c.GetZDOID()+" no completion within active deadline");return false;}
                Anchor(state,ai);
                // Native AI owns all travelling and combat movement. Set only the job anchor.
                if(Vector3.Distance(c.transform.position,state.Position)>2f){state.State=WorkState.Travelling;return true;}
                // Consume the steward's cached job. No worker performs a village-wide scan.
                var remaining=state.Job.Pieces.Where(p=>p && home.Contains(p.transform.position) && Damaged(p.GetComponent<WearNTear>())).ToArray();
                if(remaining.Length==0){string key=state.Job.Key;board.Jobs.Remove(key);state.Job.Claimant=null;state.Job=null;Release(state,human);state.State=WorkState.Idle;LivingVillagePlugin.Log.LogInfo("Area job complete: id="+c.GetZDOID()+" area="+key);return false;}
                var pieces=remaining.Where(p=>InArea(p.transform.position,c.transform.position) && PrivateArea.CheckAccess(p.transform.position,0f,false,true)).ToArray();
                if(pieces.Length==0)
                {
                    Vector3 position;if(WorkPosition(ai,c,state.Job,out position) && Vector3.Distance(position,c.transform.position)>2f){Release(state,human);state.Position=position;state.State=WorkState.Travelling;Anchor(state,ai);return true;}
                    state.Job.Claimant=null;state.Job.Deferred=Time.time+30f;state.Job=null;Release(state,human);state.State=WorkState.Idle;
                    LivingVillagePlugin.Log.LogInfo("Area job deferred: id="+c.GetZDOID()+" no reachable work position covering remaining pieces");return false;
                }
                c.SetMoveDir(Vector3.zero);
                if(!Hammer(state,human)){state.RestUntil=Time.time+3;return false;}
                if(Time.time<state.Stroke)return true;state.Stroke=Time.time+3f;
                state.State=WorkState.Working;int repaired=0;
                foreach(var piece in pieces)
                {
                    if(repaired>=4 || state.Budget.Current<Cost)break;
                    var wear=piece.GetComponent<WearNTear>();if(!wear.Repair())continue;
                    state.Budget.Spend(Cost);repaired++;piece.m_placeEffect.Create(piece.transform.position,piece.transform.rotation,null,1f,-1,default(ZDOID));
                }
                if(repaired>0)
                {
                    var animation=c.GetComponent<ZSyncAnimation>();var attack=state.Hammer.m_shared.m_attack;if(animation && attack!=null)animation.SetTrigger(attack.m_attackAnimation);
                    view.GetZDO().Set("living_work_stamina_v1",state.Budget.Current);
                    LivingVillagePlugin.Log.LogInfo("Area repair requested: id="+c.GetZDOID()+" pieces="+repaired+" stamina="+state.Budget.Current);
                }
                state.EmptyStrokes=repaired>0?0:state.EmptyStrokes+1;
                if(state.EmptyStrokes>=3){state.Job.Claimant=null;state.Job.Deferred=Time.time+30f;state.Job=null;Release(state,human);state.State=WorkState.Idle;LivingVillagePlugin.Log.LogInfo("Area job deferred: id="+c.GetZDOID()+" three unaccepted repair strokes");}
                return true;
            }
            catch(Exception error){LivingVillagePlugin.Log.LogError("Area worker disabled: "+error);Disable(component);component.enabled=false;return false;}
        }
        public static void Disable(VillageWorker component)
        {Worker state;if(!Workers.TryGetValue(component,out state))return;Release(state,component.GetComponent<Humanoid>());if(state.Job!=null)state.Job.Claimant=null;Workers.Remove(component);}
        public static void Reset()
        {foreach(var pair in Workers.ToArray())Release(pair.Value,pair.Key?pair.Key.GetComponent<Humanoid>():null);Boards.Clear();Workers.Clear();}
    }
}
