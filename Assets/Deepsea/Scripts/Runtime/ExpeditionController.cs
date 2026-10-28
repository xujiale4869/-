using System.Collections.Generic;
using UnityEngine;

namespace Deepsea
{
    public sealed class ExpeditionController : MonoBehaviour
    {
        public GameDirector game;
        public ExplorerController Explorer { get; private set; }
        public ShoreCompanion Companion { get; private set; }
        public ShoreWildlife Wildlife { get; private set; }
        public readonly List<LandingPoint> Landings=new List<LandingPoint>();
        public readonly List<IslandSite> Sites=new List<IslandSite>();
        public bool OnFoot { get; private set; }
        public bool AssetsOpen { get; private set; }
        public IslandSite OpenSite { get; private set; }
        public IslandSite NearbySite { get; private set; }
        public LandingPoint CurrentLanding { get; private set; }
        public HandTool Tool { get; private set; }=HandTool.Observe;
        public float AntennaBearing=25;
        public string Inspection="指示灯没有亮，但便携接收器仍在发出断续声。观察电池与连接线。";
        public bool RelayUsed;
        Transform stationLamp,antenna,netBarrier,modulesVisual;
        Transform islandBoat;
        readonly List<Vector3> breadcrumbs=new List<Vector3>();
        readonly List<Transform> nodeVisuals=new List<Transform>();
        bool boatReturning;
        int returnWaypoint;
        readonly Vector3[] boatRoute={new Vector3(104,0,90),new Vector3(105,0,-8),new Vector3(22,0,-36)};
        public Vector3 ActivePosition { get { return OnFoot?Explorer.transform.position:game.Boat.transform.position; } }
        public bool AtShip { get { return !OnFoot||(CurrentLanding!=null&&Vector3.Distance(Explorer.transform.position,CurrentLanding.feet)<5); } }
        public bool AtVillage { get { return OnFoot?Vector3.Distance(ActivePosition,new Vector3(0,2,-35))<24:game.CanDock; } }
        public bool CanChangeModules { get { return AtVillage&&(OnFoot||game.Boat.Speed<game.settings.interactionSpeed); } }
        public int CargoUsed { get { return game.trip.CargoUsed+game.progress.shipParts; } }
        public string ToolName { get { return Tool==HandTool.Observe?"观察镜":Tool==HandTool.Repair?"修理工具":Tool==HandTool.CutNet?"割网工具":Tool==HandTool.Buoy?"观测浮标":"诱饵盒"; } }

        public void Initialize(GameDirector director)
        {
            game=director;
            IslandTerrain.Build(game.World.Root);
            CreateLanding("归潮村公共码头",new Vector3(7,0,-16),new Vector3(2,.72f,-16),true);
            CreateLanding("雾岬岛南岸旧码头",new Vector3(8,0,53),new Vector3(8,.72f,59),false);
            CreateLanding("雾岬岛东岸避风湾",new Vector3(53,0,94),new Vector3(46,.72f,94),false);
            VisualFactory.Pier(game.World.Root,new Vector3(8,0,60),10,3);
            VisualFactory.Pier(game.World.Root,new Vector3(45,0,94),8,3,90);
            IslandTerrain.Ramp(game.World.Root,new Vector3(8,.73f,61),IslandTerrain.Point(8,68),2.6f);
            IslandTerrain.Ramp(game.World.Root,new Vector3(43,.73f,94),IslandTerrain.Point(37,94),2.6f);
            IslandTerrain.Ramp(game.World.Village,new Vector3(2,.72f,-29),new Vector3(2,2.45f,-37),2.4f);
            BuildIslandSites();
            Transform actor=VisualFactory.Group("远征者",game.World.Root,Vector3.zero);
            actor.gameObject.AddComponent<CharacterController>();
            Explorer=actor.gameObject.AddComponent<ExplorerController>();Explorer.game=game;
            Explorer.model=VisualFactory.Person(actor,Vector3.zero,new Color(.58f,.46f,.27f));
            Explorer.model.localScale=Vector3.one*1.13f;
            VisualFactory.Box("背包",Explorer.model,new Vector3(0,.7f,-.23f),new Vector3(.4f,.45f,.22f),VisualFactory.Wood);
            Explorer.ShowTool(Tool);actor.gameObject.SetActive(false);
            Transform person=VisualFactory.Group("东岸遇险者林舟",game.World.Root,IslandTerrain.Point(34,100));
            Companion=person.gameObject.AddComponent<ShoreCompanion>();Companion.expedition=this;
            Companion.model=VisualFactory.Person(person,Vector3.zero,new Color(.43f,.53f,.48f));
            if(game.IsDone("linzhou-people"))person.gameObject.SetActive(false);
            Wildlife=gameObject.AddComponent<ShoreWildlife>();Wildlife.expedition=this;
            RefreshModules();RefreshIsland();RefreshNodes();
        }
        void CreateLanding(string name,Vector3 berth,Vector3 feet,bool village)
        {Landings.Add(new LandingPoint{name=name,berth=berth,feet=feet,village=village});}
        IslandSite Site(string name,IslandSiteKind kind,float x,float z)
        {var site=new IslandSite{name=name,kind=kind,position=IslandTerrain.Point(x,z)};Sites.Add(site);return site;}
        void BuildIslandSites()
        {
            Transform root=game.World.Root;
            var station=Site("旧观测站",IslandSiteKind.Station,-3,107);
            Transform house=VisualFactory.House(root,station.position+new Vector3(0,0,3),0,1.2f,true);
            Transform desk=VisualFactory.Group("观测站操作台",root,station.position);
            VisualFactory.Box("海图台",desk,new Vector3(0,.8f,0),new Vector3(2,.16f,1),VisualFactory.PaleWood);
            VisualFactory.Box("蓄电池",desk,new Vector3(-.7f,1,0),new Vector3(.6f,.45f,.48f),VisualFactory.Roof);
            VisualFactory.Box("松脱的接头",desk,new Vector3(.7f,1,0),new Vector3(.22f,.18f,.35f),VisualFactory.Rope);
            LineRenderer cable=VisualFactory.Line("可见电缆",desk,.045f,VisualFactory.Rope);
            cable.positionCount=3;cable.SetPosition(0,desk.position+new Vector3(-.5f,1,0));cable.SetPosition(1,desk.position+new Vector3(0,1,.3f));cable.SetPosition(2,desk.position+new Vector3(.47f,1,0));
            stationLamp=VisualFactory.Sphere("站内供电灯",desk,new Vector3(.25f,1.2f,0),Vector3.one*.2f,VisualFactory.Lamp,true);
            antenna=VisualFactory.Group("可转向天线",house,new Vector3(0,4.9f,0));
            VisualFactory.Cylinder("天线杆",antenna,Vector3.zero,.05f,2,VisualFactory.PaleWood);
            VisualFactory.Box("天线指向",antenna,new Vector3(0,.7f,.7f),new Vector3(.08f,.08f,2),VisualFactory.Rope);
            var receiver=Site("便携接收器",IslandSiteKind.Receiver,1,107);
            VisualFactory.Box("接收器",root,receiver.position+Vector3.up*.45f,new Vector3(.8f,.9f,.6f),VisualFactory.Roof);
            Site("林舟的求救痕迹",IslandSiteKind.Survivor,34,100);
            var workshop=Site("低处维修棚",IslandSiteKind.Workshop,-15,80);
            VisualFactory.House(root,workshop.position+new Vector3(-2,0,2),-20,.85f);
            var supplies=Site("维修棚备用构件",IslandSiteKind.Supplies,-12,78);
            supplies.visual=VisualFactory.Box("备用板材",root,supplies.position+Vector3.up*.35f,new Vector3(1.8f,.65f,1.1f),VisualFactory.PaleWood);
            var shore=Site("青鹭号船况",IslandSiteKind.Boat,38,96);
            VisualFactory.Box("维修工具台",root,shore.position+Vector3.up*.7f,new Vector3(1.4f,.15f,1),VisualFactory.Wood);
            var net=Site("潮池缠网",IslandSiteKind.Net,-13,93);
            netBarrier=VisualFactory.Box("可清除缠网",root,net.position+new Vector3(0,.75f,0),new Vector3(.2f,1.5f,5),VisualFactory.Rope,true);
            for(int i=0;i<5;i++)VisualFactory.Box("网绳",netBarrier,new Vector3(0,.2f*i-.4f,0),new Vector3(1.04f,.035f,1.03f),VisualFactory.Wood);
            Site("北坡海面观察",IslandSiteKind.HighLookout,2,124);
            Site("村仓与工具架",IslandSiteKind.Warehouse,1,-31).position=new Vector3(1,.72f,-31);
            // The ring remains open even when the optional net shortcut is blocked.
            Vector2[] route={new Vector2(8,68),new Vector2(-9,77),new Vector2(-21,89),new Vector2(-16,108),new Vector2(2,120),new Vector2(25,111),new Vector2(35,96),new Vector2(22,80),new Vector2(8,68)};
            for(int i=0;i<route.Length-1;i++)
            {
                Vector2 a=route[i],b=route[i+1];int count=Mathf.CeilToInt(Vector2.Distance(a,b)/3);
                for(int j=0;j<count;j++)
                {Vector2 p=Vector2.Lerp(a,b,j/(float)count);Transform stone=VisualFactory.Box("环路踏石",root,IslandTerrain.Point(p.x,p.y)+Vector3.up*.02f,new Vector3(2,.07f,1.7f),new Color(.41f,.44f,.37f));stone.rotation=Quaternion.LookRotation(new Vector3(b.x-a.x,0,b.y-a.y));}
            }
            for(int i=0;i<13;i++)
            {
                float x=-20+i%4*7,z=83+i/4*10;
                if(Vector3.Distance(IslandTerrain.Point(x,z),station.position)<5)continue;
                Transform tree=VisualFactory.Group("风剪小树",root,IslandTerrain.Point(x,z));
                VisualFactory.Cylinder("树干",tree,new Vector3(0,1,0),.13f,2,VisualFactory.Wood);
                VisualFactory.Sphere("低矮树冠",tree,new Vector3(.2f,2.4f,0),new Vector3(2.8f,1.8f,2.5f),new Color(.22f,.34f,.28f));
            }
            if(!game.IsDone("qinglu-boat"))
            {
                islandBoat=VisualFactory.Group("青鹭号",root,new Vector3(61,0,102));
                VisualFactory.BoatVisual(islandBoat,false);islandBoat.gameObject.AddComponent<Rigidbody>();
                BoxCollider c=islandBoat.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.4f,0);c.size=new Vector3(1.8f,1,3.7f);
                Towable tow=islandBoat.gameObject.AddComponent<Towable>();tow.isBoat=true;tow.Bow=VisualFactory.Group("拖缆环",islandBoat,new Vector3(0,.5f,2));
                WorldInteractable target=islandBoat.gameObject.AddComponent<WorldInteractable>();target.id="qinglu-boat";target.displayName="青鹭号";target.kind=InteractionKind.IslandBoat;target.towable=tow;target.game=game;
                game.World.Targets.Add(target);
                if(game.progress.island.boatRepaired)StartBoatReturn();
            }
        }

        void Update()
        {
            if(game==null||Explorer==null)return;
            if(Input.GetKeyDown(KeyCode.Tab))ToggleAssets();
            Explorer.Blocked=game.Paused||AssetsOpen||OpenSite!=null||game.CameraFollow.Overview;
            if(game.Paused)return;
            for(int i=0;i<5;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))SelectTool((HandTool)i);
            if(Input.GetKeyDown(KeyCode.F)){if(OnFoot)BoardShip();else Disembark();}
            NearbySite=null;
            if(OnFoot)
            {
                float closest=3;
                foreach(IslandSite site in Sites)
                {if(site.kind==IslandSiteKind.Survivor){if(Companion.Boarded||game.IsDone("linzhou-people"))continue;site.position=Companion.transform.position;}float d=Vector3.Distance(Explorer.transform.position,site.position);if(d<closest){closest=d;NearbySite=site;}}
                if(Input.GetKeyDown(KeyCode.E)&&NearbySite!=null)InteractSite(NearbySite);
                if(Companion.Following&&(breadcrumbs.Count==0||Vector3.Distance(breadcrumbs[breadcrumbs.Count-1],Explorer.transform.position)>1.2f))
                    breadcrumbs.Add(Explorer.transform.position);
                if(breadcrumbs.Count>350)breadcrumbs.RemoveAt(0);
            }
            if(Input.GetKeyDown(KeyCode.R))UseTool();
            foreach(SurveyNode node in game.progress.nodes)
                foreach(WorldInteractable target in game.World.Targets)
                    if(target!=null&&Vector3.Distance(target.transform.position,new Vector3(node.x,0,node.z))<18)target.MarkScanned();
        }

        void FixedUpdate()
        {if(game!=null&&!game.Paused)UpdateBoatReturn();}

        public LandingPoint NearestLanding()
        {
            LandingPoint found=null;float best=8;
            foreach(LandingPoint point in Landings){float d=Vector3.Distance(game.Boat.transform.position,point.berth);if(d<best){best=d;found=point;}}
            return found;
        }
        public bool Disembark()
        {
            if(OnFoot||game.Paused)return false;
            LandingPoint landing=NearestLanding();
            if(landing==null){game.Notify("选择有栈桥或避风支撑的岸段。雾岬岛南岸与东岸均可登陆。");return false;}
            if(game.Boat.Speed>game.settings.interactionSpeed){game.Notify("先减速到低速，再主动下船。");return false;}
            if(game.Boat.Tow!=null){game.Notify("先按 X 解缆，目标会留在原处；再稳定靠岸下船。");return false;}
            game.HarbourMenu=false;CurrentLanding=landing;Vector3 outward=landing.berth-landing.feet;outward.y=0;game.Boat.Moor(landing.berth,Quaternion.LookRotation(outward));
            OnFoot=true;Explorer.gameObject.SetActive(true);Explorer.Place(landing.feet);
            game.CameraFollow.SetWalking(Explorer.transform,true);
            game.Notify("已下船："+landing.name+"。WASD 步行，F 回船，Tab 查看资产，1/2/3 选择工具。");return true;
        }
        public bool BoardShip()
        {
            if(!OnFoot||!AtShip){game.Notify("回到当前工作艇的登陆点，再按 F 登船。");return false;}
            if(Companion.Following&&!Companion.Boarded)
            {
                if(Vector3.Distance(Companion.transform.position,CurrentLanding.feet)>6){game.Notify("林舟还在同行，请等他来到当前泊位。");return false;}
                if(!game.trip.TryRescue("linzhou-people",1,game.settings.passengerSlots)){game.Notify("接应位已满。林舟会在岸上安全等待。");return false;}
                game.trip.people.Add(new NamedResident{id="linzhou",name="林舟",skill="船匠 · 保养船体可降低航行耗油"});Companion.Board();
            }
            OnFoot=false;Explorer.gameObject.SetActive(false);game.Boat.Unmoor();
            game.CameraFollow.SetWalking(game.Boat.transform,false);
            game.Notify("已登船。人员与船体分别接应；航路仍由你决定。");return true;
        }
        public void ReturnToLanding()
        {if(OnFoot&&CurrentLanding!=null){Explorer.Place(CurrentLanding.feet);game.Notify("已恢复到当前登岸点，船与本航次收获保留。");}}
        public void ResetAfterReturn()
        {
            ClosePanels();OnFoot=false;Explorer.gameObject.SetActive(false);game.Boat.Unmoor();
            game.CameraFollow.SetWalking(game.Boat.transform,false);breadcrumbs.Clear();
            if(!game.progress.completed.Contains("linzhou-people"))Companion.ResetAt(IslandTerrain.Point(34,100));
            RefreshIsland();
        }
        public void SelectTool(HandTool tool){Tool=tool;Explorer.ShowTool(tool);}
        public void ToggleAssets()
        {if(AssetsOpen){ClosePanels();return;}if(game.Paused)return;AssetsOpen=true;OpenSite=null;game.SetPause(true);}
        public void ClosePanels(){AssetsOpen=false;OpenSite=null;game.SetPause(false);}
        public void InteractSite(IslandSite site)
        {
            if(site.kind==IslandSiteKind.Survivor)
            {
                if(game.IsDone("linzhou-people")){game.Notify("林舟已经接应，人在船上或归潮村接应屋。");return;}
                Companion.Following=!Companion.Following;breadcrumbs.Clear();breadcrumbs.Add(Companion.transform.position);breadcrumbs.Add(Explorer.transform.position);
                game.Notify(Companion.Following?"林舟：我跟你走。先把人带到你的工作艇，青鹭号可以之后处理。":"林舟暂时在这里等待。靠近他后可以继续同行。");return;
            }
            if(site.kind==IslandSiteKind.Net){CutNet();return;}
            if(site.kind==IslandSiteKind.Receiver){game.Notify("便携接收器：断续求救来自岛的东侧避风岸。无需修好整座站点也能接应。");return;}
            if(site.kind==IslandSiteKind.Warehouse||site.kind==IslandSiteKind.Workshop){ToggleAssets();return;}
            if(site.kind==IslandSiteKind.Supplies)
            {
                if(game.IsDone("island-parts")){game.Notify("这处备用板材已经取走。");return;}
                if(game.progress.packParts+game.trip.packParts+3>6){game.Notify("背包构件上限 6，先送到船舱或用于现场修复。");return;}
                game.trip.packParts+=3;game.trip.completed.Add("island-parts");site.visual.gameObject.SetActive(false);game.Notify("背包带上 3 构件。它们可用于站点或船体，返村前属于本航次收获。");return;
            }
            if(site.kind==IslandSiteKind.HighLookout){game.Notify("北坡观察：西北方有孤立回波；东岸有断续灯闪。可选择步行，也可回船绕岛。");return;}
            OpenSite=site;AssetsOpen=false;game.SetPause(true);
        }
        public void Inspect(bool connector)
        {
            if(Tool!=HandTool.Observe){Inspection="先选择观察镜，检查具体部位。";return;}
            if(connector){game.progress.island.connectorDiagnosed=true;Inspection="电池指示仍有电，接头脱落且有盐蚀。可用修理工具接回，需背包 2 构件。";game.Save();}
            else Inspection="电池剩余电量正常，故障不在电池。沿台上那根线继续观察；检查不消耗材料。";
        }
        public bool RepairStation()
        {
            if(game.progress.island.stationPowered)return false;
            if(Tool!=HandTool.Repair||!game.progress.island.connectorDiagnosed){Inspection="需要先定位故障，再用修理工具处理接头。";return false;}
            if(!AssetRules.ConsumeFieldParts(game.progress,game.trip,2,true)){Inspection="背包需要 2 构件。村仓不能远程供料，可从船上取或使用维修棚的备用板材。";return false;}
            game.progress.island.stationPowered=true;RefreshIsland();game.Save();Inspection="站灯已亮，天线现在可以转向。两条频道分别对应求救和海上作业回波。";return true;
        }
        public float SignalStrength(bool echo)
        {float target=echo?336:95;return Mathf.Clamp01(1-Mathf.Abs(Mathf.DeltaAngle(AntennaBearing,target))/42);}
        public bool ReadSignal(bool echo)
        {
            if(!game.progress.island.stationPowered||SignalStrength(echo)<.78f){Inspection="信号还没有稳定。比较强弱并转动天线，不需要消耗构件。";return false;}
            string id=echo?"station-echo":"station-rescue";
            if(!game.IsDone(id)){game.trip.records++;game.trip.completed.Add(id);}
            if(echo){game.progress.island.echoRead=true;Inspection="作业回波稳定在西北扇区约 336°。北方航标以西还有一件海上设备，可回船验证。";}
            else{game.progress.island.signalRead=true;Inspection="求救频道稳定在东岸约 95°。林舟与青鹭号就在避风岸一带，可徒步或从海面接近。";}
            game.Save();return true;
        }
        public void SetBearing(float bearing){AntennaBearing=bearing;if(antenna!=null)antenna.localRotation=Quaternion.Euler(0,bearing,0);}
        public void DiagnoseBoat(){game.progress.island.boatDiagnosed=true;game.Save();Inspection="青鹭号供电接头盐蚀，船体浮力尚可。现场接回需修理工具和 3 构件；维修模块减少到 2。也可以先接人、拖回船坞。";}
        public bool RepairBoat()
        {
            if(game.progress.island.boatRepaired||game.IsDone("qinglu-boat"))return false;
            if(Tool!=HandTool.Repair||!game.progress.island.boatDiagnosed){Inspection="先检查船况，再用修理工具处理接头。";return false;}
            int cost=game.progress.modules.Contains(ShipModule.FieldRepair)?2:3;
            if(!AssetRules.ConsumeFieldParts(game.progress,game.trip,cost,true)){Inspection="背包构件不足。仍可以接人，再从海面拖回青鹭号。";return false;}
            game.progress.island.boatRepaired=true;StartBoatReturn();game.Save();Inspection="青鹭号恢复供电，正在自行返村。林舟仍需跟随你登上工作艇；两者分别接应。";return true;
        }
        void StartBoatReturn(){if(islandBoat==null)return;islandBoat.GetComponent<Towable>().Detach();islandBoat.GetComponent<Rigidbody>().isKinematic=true;boatReturning=true;}
        void UpdateBoatReturn()
        {
            if(!boatReturning||islandBoat==null)return;
            Rigidbody body=islandBoat.GetComponent<Rigidbody>();
            Vector3 goal=boatRoute[returnWaypoint],delta=goal-body.position;
            if(delta.sqrMagnitude>1)
            {
                body.MoveRotation(Quaternion.LookRotation(delta));
                body.MovePosition(Vector3.MoveTowards(body.position,goal,5*Time.fixedDeltaTime));
                return;
            }
            returnWaypoint++;if(returnWaypoint<boatRoute.Length)return;
            boatReturning=false;
            if(!game.progress.completed.Contains("qinglu-boat")){game.progress.completed.Add("qinglu-boat");game.progress.recoveredBoats++;}
            AssetRules.AddBoat(game.progress,new NamedBoat{id="qinglu-boat",name="青鹭号",repaired=true});
            islandBoat.gameObject.SetActive(false);game.World.RefreshHarbour();game.Save();game.Notify("青鹭号已自行抵达归潮村东泊位。资产页可将它安排为岛岸接应船。");
        }
        public void CutNet()
        {
            if(game.progress.island.netCleared){game.Notify("缠网已经清除，潮池捷径可通行。");return;}
            if(Tool!=HandTool.CutNet){game.Notify("选择割网工具（3），再处理可见缠网。");return;}
            if(Wildlife.GuardingNet){game.Notify("礁背兽在附近警戒。可走环路绕开，或带诱饵把它引向一旁。");return;}
            game.progress.island.netCleared=true;game.progress.island.wildlifeLearned=true;
            if(!game.IsDone("tidepool-record")){game.trip.completed.Add("tidepool-record");game.trip.records++;}
            RefreshIsland();game.Save();game.Notify("缠网已清除，打开一条潮池捷径。礁背兽会寻找食物，同一诱饵也能用于守港引导。");
        }
        public void UseTool()
        {
            if(Tool==HandTool.Bait||Tool==HandTool.Buoy)
            {
                if(Tool==HandTool.Buoy&&RecoverNode()){game.Notify("观测浮标已回收，回到当前背包或船舱。");return;}
                ItemLocation place=OnFoot?ItemLocation.Backpack:ItemLocation.Ship;string item=Tool==HandTool.Bait?"bait":"buoy";
                Vector3 point=PointerPoint(OnFoot?7:12);
                if(Tool==HandTool.Buoy&&IslandTerrain.Height(point.x,point.z)>.5f){game.Notify("浮标需要可漂浮的水面。到岸边或从船上选择部署位置。");return;}
                if(!AssetRules.Consume(game.progress,item,place)){game.Notify(""+ToolName+"不在当前"+(OnFoot?"背包":"船舱")+"。到船旁或村仓现场装卸。");return;}
                if(Tool==HandTool.Bait){Wildlife.Lure(point);game.Defense.Lure(point);game.Notify("已投放诱饵。观察它是否转向食物，也可以直接选择绕行。");}
                else{game.progress.nodes.Add(new SurveyNode{id=System.Guid.NewGuid().ToString("N"),x=point.x,z=point.z});RefreshNodes();game.Notify("观测浮标已部署，保留周围 18 米的观测覆盖。近处可按 R 回收。");}
                game.Save();return;
            }
            if(OnFoot&&NearbySite!=null)InteractSite(NearbySite);
            else game.Notify("接近具体装置后使用工具；1 观察、2 修理、3 割网、4 浮标、5 诱饵。");
        }
        Vector3 PointerPoint(float range)
        {
            Ray ray=game.Camera.ScreenPointToRay(Input.mousePosition);float d;Plane plane=new Plane(Vector3.up,ActivePosition);
            Vector3 point=plane.Raycast(ray,out d)?ray.GetPoint(d):ActivePosition+Vector3.forward*3;
            Vector3 delta=point-ActivePosition;delta.y=0;if(delta.magnitude>range)point=ActivePosition+delta.normalized*range;
            point.y=OnFoot?IslandTerrain.Height(point.x,point.z):.12f;return point;
        }
        public bool RecoverNode()
        {
            SurveyNode node=game.progress.nodes.Find(n=>Vector3.Distance(ActivePosition,new Vector3(n.x,ActivePosition.y,n.z))<5);
            if(node==null)return false;game.progress.nodes.Remove(node);AssetRules.Stock(game.progress,"buoy").Add(OnFoot?ItemLocation.Backpack:ItemLocation.Ship,1);RefreshNodes();game.Save();return true;
        }
        public void RefreshNodes()
        {
            foreach(Transform t in nodeVisuals)Destroy(t.gameObject);nodeVisuals.Clear();
            foreach(SurveyNode node in game.progress.nodes)
            {Transform t=VisualFactory.Cylinder("观测浮标",game.World.Root,new Vector3(node.x,.35f,node.z),.24f,.9f,VisualFactory.Rope);VisualFactory.Sphere("浮标信号灯",t,new Vector3(0,.6f,0),Vector3.one*.2f,new Color(.5f,.77f,.74f),true);nodeVisuals.Add(t);}
        }
        public void RefreshIsland(){stationLamp.gameObject.SetActive(game.progress.island.stationPowered);netBarrier.gameObject.SetActive(!game.progress.island.netCleared);foreach(IslandSite site in Sites)if(site.kind==IslandSiteKind.Supplies&&site.visual!=null)site.visual.gameObject.SetActive(!game.IsDone("island-parts"));}
        public Vector3 CompanionWaypoint(Vector3 position)
        {while(breadcrumbs.Count>1&&Vector3.Distance(position,breadcrumbs[0])<1.3f)breadcrumbs.RemoveAt(0);return breadcrumbs.Count>0?breadcrumbs[0]:Explorer.transform.position;}
        public bool MoveParts(ItemLocation from,ItemLocation to,int amount)
        {
            bool allowed=(from==ItemLocation.Village||to==ItemLocation.Village)?AtVillage:AtShip;
            if(!allowed){game.Notify("查看不等于取用。请到村仓或当前工作艇旁实际装卸。");return false;}
            bool ok=AssetRules.TransferParts(game.progress,game.trip,from,to,amount,game.settings.cargoSlots);
            if(!ok)game.Notify("数量不足、容量已满，或收获尚未靠泊结算。");else game.Save();return ok;
        }
        public bool MoveItem(string id,ItemLocation from,ItemLocation to)
        {
            if((from==ItemLocation.Village||to==ItemLocation.Village)?!AtVillage:!AtShip){game.Notify("需要到物品所在的村仓或船旁取放。");return false;}
            bool ok=AssetRules.Transfer(game.progress,id,from,to,1);if(ok)game.Save();else game.Notify("当前位置没有这件物品。");return ok;
        }
        public void ToggleModule(ShipModule module)
        {
            if(!CanChangeModules){game.Notify("船载模块在归潮村码头或工作坊装配。");return;}
            if(!AssetRules.ToggleModule(game.progress,module)){game.Notify("两个功能位已满，先取回一件模块。");return;}
            RefreshModules();game.Save();
        }
        public void RefreshModules()
        {
            if(modulesVisual!=null)Destroy(modulesVisual.gameObject);
            modulesVisual=VisualFactory.Group("已装配船载模块",game.Boat.hull,Vector3.zero);
            if(game.progress.modules.Contains(ShipModule.Observation))VisualFactory.Cylinder("观测模块天线",modulesVisual,new Vector3(-.3f,2,.1f),.05f,1.4f,VisualFactory.Rope);
            if(game.progress.modules.Contains(ShipModule.TowAssist))VisualFactory.Cylinder("助力缆筒",modulesVisual,new Vector3(-.3f,.86f,-1.5f),.28f,.36f,VisualFactory.Rope);
            if(game.progress.modules.Contains(ShipModule.FieldRepair))VisualFactory.Box("船载维修箱",modulesVisual,new Vector3(.3f,.9f,1),new Vector3(.5f,.4f,.6f),VisualFactory.Roof);
        }
        public bool RepairHarbourBoat(string id)
        {
            if(!AtVillage||!AssetRules.RepairBerthBoat(game.progress,id)){game.Notify("需要在归潮村现场，使用村仓 2 构件维修该船。");return false;}
            game.World.RefreshHarbour();game.Save();game.Notify("泊位船已修好，可安排岛岸接应。");return true;
        }
        public void AssignTechnician()
        {
            if(!AtVillage||!game.progress.people.Exists(p=>p.id=="linzhou")){game.Notify("林舟需先安置到归潮村，现场才能安排工作。");return;}
            game.progress.technicianAssigned=!game.progress.technicianAssigned;game.Save();game.World.RefreshHarbour();
            NamedResident person=game.progress.people.Find(p=>p.id=="linzhou");person.home=game.progress.technicianAssigned?"归潮村船坞":"归潮村接应屋";game.Save();
            game.Notify(game.progress.technicianAssigned?"林舟开始在船坞保养一级船，同样航程耗油减少 15%。":"林舟回到接应屋休息。");
        }
        public void AssignRelay()
        {
            if(!AtVillage||!game.progress.boats.Exists(b=>b.repaired)){game.Notify("先把一艘船修好，再在归潮村安排接应。");return;}
            game.progress.relayAssigned=!game.progress.relayAssigned;game.Save();
            game.Notify(game.progress.relayAssigned?"接应船已安排到雾岬岛东岸。每次返村后的下一趟，可在东岸获得一次燃油接应。":"接应船回到归潮村泊位。");
        }
        public bool UseRelay()
        {
            if(!game.progress.relayAssigned||RelayUsed||Vector3.Distance(game.Boat.transform.position,Landings[2].berth)>13)return false;
            game.Boat.Refuel(.35f);RelayUsed=true;game.Notify("东岸接应船提供一次补给，燃油恢复 35%。返村后可再次准备接应。");return true;
        }
    }
}
