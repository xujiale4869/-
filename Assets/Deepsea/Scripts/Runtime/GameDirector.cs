using System.Collections.Generic;
using UnityEngine;

namespace Deepsea
{
    public sealed class GameDirector : MonoBehaviour
    {
        public BaySettings settings;
        public HarbourProgress progress;
        public readonly Expedition trip = new Expedition();
        public BoatController Boat { get; private set; }
        public Camera Camera { get; private set; }
        public FollowCamera CameraFollow { get; private set; }
        public WorldBuilder World { get; private set; }
        public AmbientAudio Audio { get; private set; }
        public HarbourDefense Defense { get; private set; }
        public ExpeditionController Expedition { get; private set; }
        public bool Paused { get; private set; }
        public bool HarbourMenu;
        public WorldInteractable Nearby { get; private set; }
        public float ScanRemaining { get { return Mathf.Max(0,scanNext-Time.time); } }
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        public bool CanDock { get { return Boat != null &&
            Vector3.Distance(Boat.transform.position,World.DockPosition)<settings.dockingRadius; } }
        public static bool VerificationMode;
        SaveStore store;
        float scanNext, nextFarWarning;
        LineRenderer scanRing;
        float scanStarted = -100;

        void Awake()
        {
            Time.timeScale = 1;
            if(settings==null)settings=ScriptableObject.CreateInstance<BaySettings>();
            store=VerificationMode ? new SaveStore(System.IO.Path.Combine(Application.temporaryCachePath,"deepsea-verification.json")) : SaveStore.Default();
            progress=VerificationMode ? new HarbourProgress() : store.Load();
            progress.Normalize();AssetRules.Initialize(progress);
            ConfigureLight();
            World=new WorldBuilder();World.Build(this);
            Boat=World.CreateBoat();
            GameObject cameraObj=new GameObject("海面随船镜头");
            cameraObj.transform.SetParent(transform,false);
            Camera=cameraObj.AddComponent<Camera>();
            cameraObj.tag="MainCamera";
            Camera.orthographic=true;Camera.orthographicSize=settings.cameraSize;
            Camera.nearClipPlane=.1f;Camera.farClipPlane=500;
            Camera.clearFlags=CameraClearFlags.SolidColor;Camera.backgroundColor=settings.fogColor;
            Camera.allowHDR=true;Camera.allowMSAA=true;
            cameraObj.AddComponent<AudioListener>();
            CameraFollow=cameraObj.AddComponent<FollowCamera>();CameraFollow.target=Boat.transform;CameraFollow.settings=settings;
            Audio=gameObject.AddComponent<AmbientAudio>();Audio.game=this;
            Defense=gameObject.AddComponent<HarbourDefense>();Defense.game=this;
            Expedition=gameObject.AddComponent<ExpeditionController>();Expedition.Initialize(this);
            gameObject.AddComponent<GameHud>().game=this;
            gameObject.AddComponent<ExpeditionHud>().game=this;
            scanRing=VisualFactory.Line("短距探测脉冲",transform,.09f,new Color(.42f,.75f,.77f,.5f));
            scanRing.positionCount=81;scanRing.enabled=false;
            Notify("海陆远征：Tab 查看资产与配装，低速靠近码头按 F 下船。雾岬岛有南、东两个登陆岸。");
            Save();
        }

        void ConfigureLight()
        {
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=settings.fogColor;RenderSettings.fogStartDistance=190;RenderSettings.fogEndDistance=360;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.44f,.56f,.60f);
            GameObject sun=new GameObject("薄云后的晨光");sun.transform.SetParent(transform,false);
            sun.transform.rotation=Quaternion.Euler(48,-34,0);
            Light light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.95f;
            light.color=new Color(.84f,.91f,.9f);light.shadows=LightShadows.Soft;
            QualitySettings.shadowDistance=160;
            Shader.SetGlobalVector("_DeepseaLightDirection",-sun.transform.forward);
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {if(Expedition.AssetsOpen||Expedition.OpenSite!=null){Expedition.ClosePanels();return;}else if(HarbourMenu)HarbourMenu=false;else SetPause(!Paused);}
            if(Input.GetKeyDown(KeyCode.M)&&!Paused&&!HarbourMenu)CameraFollow.Overview=!CameraFollow.Overview;
            Boat.InputBlocked=Paused||HarbourMenu||CameraFollow.Overview||Expedition.OnFoot;
            if(Paused)return;
            Nearby=null;
            float distance=settings.interactionRadius;
            foreach(WorldInteractable target in World.Targets)
            {
                if(target==null||target.Taken||!target.gameObject.activeInHierarchy)continue;
                float d=Vector3.Distance(Boat.transform.position,target.transform.position);
                if(d<distance){distance=d;Nearby=target;}
            }
            if(!HarbourMenu&&!CameraFollow.Overview&&!Expedition.OnFoot)
            {
                if(Input.GetKeyDown(KeyCode.Q))Scan();
                if(Input.GetKeyDown(KeyCode.E))
                {
                    if(Boat.Speed>settings.interactionSpeed)Notify("先减速，再进行接应、回收或靠泊。");
                    else if(CanDock)Dock();
                    else if(Nearby!=null)Nearby.Interact();
                }
            }
            float age=Time.time-scanStarted;
            scanRing.enabled=age>=0&&age<1.5f;
            if(scanRing.enabled)
            {
                float radius=ScanRadius()*age/1.5f;
                for(int i=0;i<scanRing.positionCount;i++)
                {float a=i*Mathf.PI*2/(scanRing.positionCount-1);scanRing.SetPosition(i,scanOrigin+new Vector3(Mathf.Sin(a)*radius,.16f,Mathf.Cos(a)*radius));}
                Color c=new Color(.45f,.77f,.76f,(1-age/1.5f)*.5f);
                scanRing.startColor=scanRing.endColor=c;
            }
            if(Boat.transform.position.magnitude>settings.seaLimit-20&&Time.time>nextFarWarning)
            {Notify("前方涌浪与回流加重：一级船的浅湾试航范围到此，仍可手动掉头。");nextFarWarning=Time.time+15;}
        }

        Vector3 scanOrigin;
        public float ScanRadius(){return (progress.charts?settings.improvedScanRadius:settings.scanRadius)+(progress.modules.Contains(ShipModule.Observation)?12:0);}
        public void Scan()
        {
            if(Time.time<scanNext){Notify("探测器正在重新充能。");return;}
            scanNext=Time.time+settings.scanCooldown;scanStarted=Time.time;scanOrigin=Boat.transform.position;
            int found=0;
            foreach(WorldInteractable target in World.Targets)
            {
                if(target==null||target.Taken||!target.gameObject.activeInHierarchy)continue;
                if(Vector3.Distance(scanOrigin,target.transform.position)>ScanRadius())continue;
                target.MarkScanned();found++;
            }
            Defense.OnScan(scanOrigin,ScanRadius());
            Audio.PlayScan();
            Notify(found>0?"附近发现 "+found+" 处线索。标记仅显示已探测或近处的目标。":"这一带没有新的目标回波。可以改变航向再探测。");
        }

        public bool IsDone(string id){return progress.completed.Contains(id)||trip.completed.Contains(id);}

        public void Dock()
        {
            if(!CanDock||Boat.Speed>settings.interactionSpeed)return;
            if(Boat.Tow!=null&&Vector3.Distance(Boat.Tow.transform.position,World.DockPosition)>20)
            {Notify("拖带目标还未进入港口接应范围，先把它带近一些。");return;}
            Towable delivered=Boat.DeliverTow();
            if(delivered!=null)
            {
                WorldInteractable target=delivered.GetComponent<WorldInteractable>();
                if(target!=null&&!IsDone(target.id))
                {
                    trip.completed.Add(target.id);
                    if(delivered.isBoat){trip.recoveredBoats++;trip.boats.Add(new NamedBoat{id=target.id,name=target.id=="qinglu-boat"?"青鹭号":target.id=="fishing-boat-a"?"归潮渔舟一号":"归潮渔舟二号",repaired=false});}
                    else{trip.parts+=target.parts;trip.equipment+=target.equipment;}
                    target.MarkTaken();target.gameObject.SetActive(false);
                }
            }
            bool findings=trip.HasFindings;
            int people=trip.passengers,parts=trip.parts,equipment=trip.equipment;
            AssetRules.UnloadOwned(progress);trip.Commit(progress);
            Boat.Service();
            Expedition.RelayUsed=false;
            World.RefreshHarbour();
            Save();
            HarbourMenu=true;
            Notify(findings?"靠泊结算："+people+" 人 / "+parts+" 构件 / "+equipment+" 设备。港内成果已保存。":"已经靠泊。基础维修与补给完成，可直接再次出航。");
            Audio.PlayCue(true);
        }

        public bool Build(Building building)
        {
            if(!CanDock||!HarbourMenu)return false;
            int parts=building==Building.Winch?settings.winchParts:building==Building.Charts?settings.chartParts:settings.netParts;
            int equipment=building==Building.Winch?settings.winchEquipment:building==Building.Net?settings.netEquipment:0;
            int records=building==Building.Charts?settings.chartRecords:0;
            if(!Economy.TryBuild(progress,building,parts,equipment,records))
            {Notify("材料不足，或该设施已经建成。建设使用村仓的已结算材料。");return false;}
            World.RefreshHarbour();Save();Audio.PlayCue(true);
            Notify(building==Building.Winch?"船坞轻绞盘开始工作：拖带更稳定，现在可回收重型设备。":building==Building.Charts?
                "海图屋观测平台已建成：下一趟探测范围更远。":"缆网台已经就位。可以在港内启动一次守港演练。");
            return true;
        }

        public void Save()
        {
            if(!VerificationMode&&!store.Save(progress))Notify("本次写入存档失败，进度仍保留在当前游戏中。");
        }

        public void EmergencyReturn()
        {
            trip.Clear();
            Expedition.ResetAfterReturn();
            Boat.Recover(World.DockPosition+new Vector3(3,0,4));
            // Restore uncommitted targets, including abandoned tow positions.
            foreach(WorldInteractable target in World.Targets)
            {
                if(target==null||progress.completed.Contains(target.id))continue;
                if(target.kind==InteractionKind.Supplies){target.gameObject.SetActive(true);target.ResetTaken();}
                if(target.kind==InteractionKind.FishRecord)target.ResetTaken();
                if(target.kind==InteractionKind.Rescue&&!progress.completed.Contains(target.id+"-people"))
                {Transform person=target.transform.Find("居民");if(person!=null)person.gameObject.SetActive(true);}
            }
            HarbourMenu=false;SetPause(false);
            Notify("村船已接你返港。本航次未结算收获放弃，村庄建设保留。");
        }

        public void SetPause(bool value){Paused=value;Time.timeScale=value?0:1;}
        public void Notify(string message){Notice=message;NoticeUntil=Time.unscaledTime+8;}
        void OnDestroy(){Time.timeScale=1;}
        void OnApplicationQuit(){Save();}
    }
}
