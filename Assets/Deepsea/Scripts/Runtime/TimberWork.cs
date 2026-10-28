using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deepsea
{
    [Serializable] public sealed class TimberTree
    { public int hits, logs; }
    [Serializable] public sealed class TimberProgress
    {
        public List<TimberTree> trees=new List<TimberTree>();
        public bool assigned, pierBuilt;
        public int workerCarry, delivered, goal;
        public void Normalize()
        {
            if(trees==null)trees=new List<TimberTree>();
            while(trees.Count<5)trees.Add(new TimberTree());
            foreach(var t in trees){t.hits=Math.Max(0,Math.Min(3,t.hits));t.logs=Math.Max(0,t.logs);}
            workerCarry=Math.Max(0,workerCarry);
        }
    }
    // The player and worker use the same finite trees and physical bundles.
    public sealed class TimberWork : MonoBehaviour
    {
        public GameDirector game;
        public readonly Vector3 StorePoint=new Vector3(5,.73f,-30);
        public readonly Vector3 PierPoint=new Vector3(-1,.73f,-25);
        public readonly Vector3[] TreePoints={new Vector3(-8,2.45f,-52),new Vector3(-10,2.45f,-55),new Vector3(-5,2.45f,-57),new Vector3(-11,2.45f,-49),new Vector3(-8,2.45f,-59)};
        readonly List<Transform> crowns=new List<Transform>(), bundles=new List<Transform>();
        Transform stockVisual,pierVisual,worker,workerModel,workerTool,carried;
        CharacterController walker;
        readonly Vector3[] route={new Vector3(2,.73f,-30),new Vector3(2,2.45f,-38),new Vector3(5,2.45f,-40),new Vector3(5,2.45f,-49),new Vector3(-5,2.45f,-51)};
        public string WorkerState {get;private set;}="在村仓旁休息";
        public bool PlayerWorking {get;private set;}
        int playerTree=-1,workerTree=-1,pathIndex,phase; // 0 home, 1 outward, 2 chop, 3 inward
        float swing,workerSwing,vertical,stuckTime;
        public Transform Worker {get{return worker;}}
        public bool AtStore {get{return game.Expedition.OnFoot?Vector3.Distance(game.Expedition.ActivePosition,StorePoint)<5.5f:game.CanDock&&game.Boat.Speed<game.settings.interactionSpeed;}}
        TimberProgress P {get{return game.progress.timber;}}
        ItemStock Wood {get{return AssetRules.Stock(game.progress,"wood");}}
        public void Initialize(GameDirector g)
        {
            game=g;P.Normalize();
            var root=VisualFactory.Group("本岛备料林地",g.World.Village,Vector3.zero);
            VisualFactory.Box("林地可步行土台",root,new Vector3(-8,2.22f,-54),new Vector3(13,.38f,15),new Color(.28f,.34f,.26f),true);
            for(int i=0;i<TreePoints.Length;i++)
            {
                var t=VisualFactory.Group("可采伐海松 "+(i+1),root,TreePoints[i]);
                VisualFactory.Cylinder("留下的树桩",t,new Vector3(0,.2f,0),.24f,.4f,VisualFactory.PaleWood);
                var crown=VisualFactory.Group("树干与树冠",t,Vector3.zero);
                VisualFactory.Cylinder("树干",crown,new Vector3(0,1.35f,0),.19f,2.7f,VisualFactory.Wood);
                for(int j=0;j<3;j++)VisualFactory.Sphere("海松枝叶",crown,new Vector3(j==1?.35f:-.18f,2.2f+j*.7f,0),new Vector3(2.5f-j*.5f,1.8f,2.2f-j*.3f),new Color(.18f,.31f+j*.02f,.25f));
                crowns.Add(crown);
                bundles.Add(LogBundle(root,TreePoints[i]+new Vector3(.7f,.2f,.7f)));
            }
            var rack=VisualFactory.Group("村仓工具架 · 基础工具随身领用",root,new Vector3(4,.73f,-30));
            VisualFactory.Box("工具架",rack,new Vector3(0,.8f,0),new Vector3(1.4f,.15f,.8f),VisualFactory.PaleWood);
            ToolModels.Build(rack,HandTool.Axe).localPosition=new Vector3(0,1.2f,0);
            stockVisual=VisualFactory.Group("村仓实际原木",root,new Vector3(6,.75f,-28));
            pierVisual=VisualFactory.Group("修复码头",root,Vector3.zero);
            Refresh();RefreshWorker();
        }
        static Transform LogBundle(Transform root,Vector3 pos)
        {
            var b=VisualFactory.Group("可拾取原木捆",root,pos);
            for(int j=0;j<3;j++){var log=VisualFactory.Cylinder("原木",b,new Vector3((j%2)*.32f, j==2?.28f:0,0),.16f,1.1f,VisualFactory.PaleWood);log.localRotation=Quaternion.Euler(90,0,0);}
            VisualFactory.Box("捆扎绳",b,new Vector3(.16f,.05f,0),new Vector3(.72f,.12f,.12f),VisualFactory.Rope);return b;
        }
        public void Refresh()
        {
            for(int i=0;i<TreePoints.Length;i++){crowns[i].gameObject.SetActive(P.trees[i].hits<3);bundles[i].gameObject.SetActive(P.trees[i].logs>0);}
            foreach(Transform t in stockVisual)Destroy(t.gameObject);
            for(int i=0;i<Mathf.Min(Wood.village,24);i++)
            {var log=VisualFactory.Cylinder("已入村仓原木",stockVisual,new Vector3((i%4)*.3f,(i/4)*.28f,0),.14f,1.6f,VisualFactory.PaleWood);log.localRotation=Quaternion.Euler(90,0,0);}
            foreach(Transform t in pierVisual)Destroy(t.gameObject);
            if(P.pierBuilt)
            {
                VisualFactory.Pier(pierVisual,new Vector3(-4,0,-25),8,3,90);
                if(!game.Expedition.Landings.Exists(l=>l.name=="修复的西侧装卸码头"))
                    game.Expedition.Landings.Add(new LandingPoint{name="修复的西侧装卸码头",berth=new Vector3(-7,0,-22),feet=new Vector3(-6,.73f,-25),village=true});
            }
            else
            {
                VisualFactory.Box("破损平台缺板",pierVisual,new Vector3(-3,.45f,-25),new Vector3(3,.2f,2.6f),VisualFactory.Wood);
                for(int i=0;i<3;i++)VisualFactory.Box("待修铺板",pierVisual,new Vector3(-2.1f-i*.8f,.64f,-25),new Vector3(.4f,.12f,2.5f),VisualFactory.PaleWood);
            }
        }
        public int NearbyTree()
        {
            if(!game.Expedition.OnFoot)return -1;
            for(int i=0;i<TreePoints.Length;i++)if(Vector3.Distance(game.Expedition.ActivePosition,TreePoints[i])<2.5f&&(P.trees[i].hits<3||P.trees[i].logs>0))return i;
            return -1;
        }
        public string NearbyHint()
        {
            if(!game.Expedition.OnFoot)return "";
            int i=NearbyTree();
            if(i>=0)return P.trees[i].logs>0?"E 拾取原木捆 · 随身 "+Wood.backpack+"/6":"E / R 持斧砍伐 · 移动取消 · "+P.trees[i].hits+"/3";
            if(Vector3.Distance(game.Expedition.ActivePosition,StorePoint)<3)return "E 打开村仓 · 随身原木 "+Wood.backpack+" / 村仓 "+Wood.village;
            if(Vector3.Distance(game.Expedition.ActivePosition,PierPoint)<2.2f)return P.pierBuilt?"E 查看已修码头 · 新增西侧装卸岸":"E 修复西侧码头 · 村仓原木 "+Wood.village+"/6";
            return "";
        }
        public bool Interact()
        {
            if(!game.Expedition.OnFoot||game.Paused)return false;
            int i=NearbyTree();
            if(i>=0)
            {
                if(P.trees[i].logs>0)Pickup(i);
                else{game.Expedition.SelectTool(HandTool.Axe);playerTree=i;PlayerWorking=!PlayerWorking;swing=0;game.Notify(PlayerWorking?"开始采木。移动或再次使用取消；原木需要拾取并带回村仓。":"已停止采木，击打进度保留。");}
                return true;
            }
            if(Vector3.Distance(game.Expedition.ActivePosition,StorePoint)<3){game.UI.Open(UiPage.Inventory);return true;}
            if(Vector3.Distance(game.Expedition.ActivePosition,PierPoint)<2.2f){game.UI.Open(UiPage.Harbour);return true;}
            return false;
        }
        public bool Strike(int i)
        {
            if(i<0||i>=P.trees.Count||P.trees[i].hits>=3)return false;
            var t=P.trees[i];t.hits++;
            if(t.hits==3)t.logs+=2;
            game.Audio.PlayCue(false);Refresh();game.Save();return true;
        }
        public bool Pickup(int i)
        {
            int take=Mathf.Min(P.trees[i].logs,6-Wood.backpack);
            if(take<=0){game.Notify("随身原木已满 6/6，请先送到村仓。");return false;}
            P.trees[i].logs-=take;Wood.backpack+=take;Refresh();game.Save();game.Notify("拾取 "+take+" 原木。到村仓交付后可用于修码头。");return true;
        }
        public bool Deposit()
        {
            if(!AtStore)return false;
            int n=Mathf.Min(Wood.backpack,24-Wood.village);if(n<=0)return false;
            Wood.backpack-=n;Wood.village+=n;Refresh();game.Save();game.Notify("已向村仓交付 "+n+" 原木。");return true;
        }
        public bool BuildPier()
        {
            if(!game.Expedition.AtVillage||P.pierBuilt||Wood.village<6)return false;
            Wood.village-=6;P.pierBuilt=true;Refresh();game.Save();game.Audio.PlayCue(true);game.Notify("西侧码头修复完成。新增可步行平台与装卸岸；林舟已交付 "+P.delivered+" 根原木。");return true;
        }
        public bool Assign()
        {
            if(!game.Expedition.AtVillage||!game.progress.people.Exists(x=>x.id=="linzhou"))return false;
            P.assigned=!P.assigned;
            if(P.assigned){P.goal=P.delivered+6;game.progress.technicianAssigned=false;}
            game.Save();RefreshWorker();game.Notify(P.assigned?"林舟：我用自己的斧头备齐 6 根原木，送到村仓。你可以继续出航。":"林舟停止新采伐；手上的木料会先送回村仓。");return true;
        }
        public void RefreshWorker()
        {
            if(!game.progress.people.Exists(x=>x.id=="linzhou"))return;
            if(worker==null)
            {
                worker=VisualFactory.Group("林舟 · 可见备料工作",game.World.Root,StorePoint);
                walker=worker.gameObject.AddComponent<CharacterController>();walker.height=1.5f;walker.radius=.22f;walker.center=new Vector3(0,.75f,0);walker.stepOffset=.3f;walker.minMoveDistance=0;
                workerModel=VisualFactory.Person(worker,Vector3.zero,new Color(.43f,.53f,.48f));
                workerTool=ToolModels.Build(workerModel,HandTool.Axe);workerTool.localPosition=new Vector3(.28f,.72f,.2f);
                carried=LogBundle(workerModel,new Vector3(0,.85f,.38f));
                phase=P.workerCarry>0?3:0;pathIndex=0;
            }
            workerTool.gameObject.SetActive(P.assigned);carried.gameObject.SetActive(P.workerCarry>0);
        }
        void Update()
        {
            if(game==null||game.Paused)return;
            if(PlayerWorking)
            {
                if(!game.Expedition.OnFoot||game.Expedition.Tool!=HandTool.Axe||Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.D)||NearbyTree()!=playerTree)PlayerWorking=false;
                else
                {swing+=Time.deltaTime;game.Expedition.Explorer.tool.localRotation=Quaternion.Euler(15+Mathf.Sin(swing*8)*55,0,-25);if(swing>=.85f){swing=0;Strike(playerTree);if(P.trees[playerTree].hits>=3){PlayerWorking=false;game.Notify("树已砍下，留下树桩与 2 原木。按 E 拾取。");}}}
            }
            RefreshWorker();if(worker==null)return;
            if(P.workerCarry>0&&phase!=3){phase=3;pathIndex=4;}
            if(phase==0)
            {
                WorkerState=game.progress.technicianAssigned?"在船坞保养 · 耗油 −15%":"在村仓旁休息";
                if(!P.assigned)return;
                if(Wood.village>=24){WorkerState="受阻：村仓原木已满 24/24";return;}
                workerTree=P.trees.FindIndex(t=>t.logs>0||t.hits<3);
                if(workerTree<0){WorkerState="林地已采完，任务结束";P.assigned=false;game.Save();return;}
                phase=1;pathIndex=0;
            }
            if(phase==1)
            {
                if(!P.assigned){phase=3;pathIndex=Mathf.Clamp(pathIndex-1,0,4);return;}
                WorkerState="前往本岛林地 · 自带工作斧";
                if(pathIndex<route.Length){if(Walk(route[pathIndex]))pathIndex++;}
                else if(Walk(TreePoints[workerTree]+new Vector3(1,0,0)))phase=2;
            }
            else if(phase==2)
            {
                WorkerState="采伐海松 · "+P.trees[workerTree].hits+"/3";
                workerTool.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*7)*55,0,-25);
                if(!P.assigned){phase=3;pathIndex=4;return;}
                workerSwing+=Time.deltaTime;
                if(workerSwing>=.85f){workerSwing=0;var t=P.trees[workerTree];if(t.hits<3)Strike(workerTree);else {int n=Mathf.Min(2,t.logs);t.logs-=n;P.workerCarry+=n;phase=3;pathIndex=4;Refresh();game.Save();}}
            }
            else if(phase==3)
            {
                WorkerState="搬回村仓 · 携带 "+P.workerCarry+" 原木";
                if(pathIndex>=0){if(Walk(route[pathIndex]))pathIndex--;}
                else if(Walk(StorePoint))
                {
                    int n=Mathf.Min(P.workerCarry,24-Wood.village);
                    if(n==0&&P.workerCarry>0){WorkerState="受阻：村仓已满，保留手中木料";return;}
                    Wood.village+=n;P.workerCarry-=n;P.delivered+=n;Refresh();
                    if(P.delivered>=P.goal&&P.goal>0){P.assigned=false;game.Notify("林舟已备齐木料，累计交付 "+P.delivered+" 原木。可在村仓修复码头。");}
                    phase=0;game.Save();
                }
            }
        }
        bool Walk(Vector3 goal)
        {
            Vector3 delta=goal-worker.position;delta.y=0;
            if(delta.magnitude<.4f)return true;
            Vector3 before=worker.position;
            vertical=walker.isGrounded?-1:Mathf.Max(-18,vertical-18*Time.deltaTime);
            walker.Move((delta.normalized*2.4f+Vector3.up*vertical)*Time.deltaTime);
            workerModel.rotation=Quaternion.LookRotation(delta);workerModel.localPosition=Vector3.up*Mathf.Abs(Mathf.Sin(Time.time*8))*.035f;
            stuckTime=(worker.position-before).sqrMagnitude<.0000001f?stuckTime+Time.deltaTime:0;
            if(stuckTime>4)WorkerState="受阻：前往林地的路线被挡住";
            return false;
        }
    }
}
