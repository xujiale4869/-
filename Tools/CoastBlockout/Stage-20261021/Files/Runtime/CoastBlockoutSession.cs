using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deepsea
{
    [DefaultExecutionOrder(-10000)]
    public sealed partial class CoastBlockoutSession : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public CoastBlockoutRegion region;
        public bool referenceView=true, weakPerspective=true;
        GameDirector game;
        CoastFrameMonitor monitor;
        bool ready,showHud=true,caveView;
        float viewSize=15.5f;
        Vector3 focus;
        string failure="";
        bool VisualCheck { get { return Array.IndexOf(Environment.GetCommandLineArgs(),"-whitebox-visual")>=0; } }
        bool Check { get { return Array.IndexOf(Environment.GetCommandLineArgs(),"-whitebox-check")>=0||VisualCheck; } }

        void Awake()
        {
            Active=true;GameDirector.VerificationMode=true;
            Application.logMessageReceived+=OnLog;
        }
        void OnLog(string message,string stack,LogType kind)
        {if(kind==LogType.Error||kind==LogType.Assert||kind==LogType.Exception)failure+=message+"; ";}
        IEnumerator Start()
        {
            // Original gameplay initialization completes first. This scene swaps only the environment.
            yield return null;game=GetComponent<GameDirector>();
            if(region==null)region=FindObjectOfType<CoastBlockoutRegion>();
            if(game==null||region==null){Debug.LogError("Whitebox scene missing director or shared coast prefab");yield break;}
            game.UI.Close();game.HarbourMenu=false;game.SetPause(false);
            game.settings=Instantiate(game.settings);game.settings.seaLimit=230;
            game.CameraFollow.settings=game.settings;game.CameraFollow.enabled=false;
            game.Expedition.Wildlife.enabled=false;game.Timber.enabled=false;game.Defense.enabled=false;
            foreach(var m in game.World.Root.GetComponents<MonoBehaviour>())m.enabled=false;
            foreach(Transform child in game.World.Root)
            {
                if(child==game.Boat.transform||child==game.Expedition.Explorer.transform)continue;
                child.gameObject.SetActive(false);
            }
            if(OceanSurface.Active!=null)OceanSurface.Active.ReflectionEnabled=false;
            game.UI.enabled=false;foreach(var canvas in FindObjectsOfType<Canvas>())canvas.enabled=false;
            foreach(var wake in game.Boat.GetComponents<BoatWake>())wake.enabled=false;
            foreach(var r in game.Boat.GetComponentsInChildren<Renderer>())r.enabled=false;
            foreach(var l in game.Boat.GetComponentsInChildren<Light>())l.enabled=false;
            game.Boat.hull.gameObject.SetActive(false);
            var boat=Instantiate(Resources.Load<GameObject>("CoastBlockout/BoatProxy"),game.Boat.transform);
            boat.transform.localScale=Vector3.one*1.25f;
            game.Boat.GetComponent<BoxCollider>().size=new Vector3(2.05f,1.2f,4.4f);
            boat.name="白模船壳 · 使用原BoatController";game.Boat.hull=boat.transform;
            foreach(var point in game.Expedition.Landings)
                if(point.name=="雾岬岛东岸避风湾"){point.berth=region.berth.position;point.feet=region.landingFeet.position;}
            NeutralizeExplorer();Lighting();
            game.Boat.Moor(new Vector3(54.5f,0,94),Quaternion.Euler(0,110,0));game.Boat.Unmoor();
            focus=region.referenceFocus.position;ApplyCamera(true);ready=true;
            monitor=gameObject.AddComponent<CoastFrameMonitor>();
            QualitySettings.vSyncCount=0;Application.targetFrameRate=Check?-1:60;
            if(Check){yield return RunCheck();yield break;}
            Debug.Log("COAST_BLOCKOUT_READY sharedPrefab="+region.layoutVersion+" rocks="+region.rockMasses+" terrainTriangles="+region.terrainTriangles);
        }
        void Lighting()
        {
            foreach(var l in FindObjectsOfType<Light>())if(l.type==LightType.Directional)
            {l.transform.rotation=Quaternion.Euler(48,-35,0);l.color=Color.white;l.intensity=1.10f;l.shadows=LightShadows.Soft;l.shadowBias=.02f;l.shadowNormalBias=.18f;}
            RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.46f,.47f,.49f);
            RenderSettings.ambientEquatorColor=new Color(.33f,.34f,.36f);
            RenderSettings.ambientGroundColor=new Color(.20f,.21f,.23f);
            game.Camera.backgroundColor=new Color(.31f,.36f,.39f);
            game.Camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            game.Camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(pipeline!=null){pipeline.msaaSampleCount=2;pipeline.shadowDistance=180;}
            Shader.SetGlobalFloat("_BayCausticReady",0);
        }
        void NeutralizeExplorer()
        {foreach(var r in game.Expedition.Explorer.GetComponentsInChildren<Renderer>(true))if(r.sharedMaterial!=region.clay)r.sharedMaterial=region.clay;}
        void Update()
        {
            if(!ready||Check)return;
            if(Input.GetKeyDown(KeyCode.F5))weakPerspective=!weakPerspective;
            if(Input.GetKeyDown(KeyCode.F6)){referenceView=!referenceView;caveView=false;}
            if(Input.GetKeyDown(KeyCode.F7))caveView=!caveView;
            if(Input.GetKeyDown(KeyCode.F9))region.water.gameObject.SetActive(!region.water.gameObject.activeSelf);
            if(Input.GetKeyDown(KeyCode.F10)){showHud=!showHud;if(monitor!=null)monitor.enabled=showHud;}
            if(Input.GetKeyDown(KeyCode.Escape))game.SetPause(!game.Paused);
            if(Input.GetKeyDown(KeyCode.Home))
            {
                if(game.Expedition.OnFoot){game.Expedition.ReturnToLanding();game.Expedition.BoardShip();}
                game.Boat.Moor(new Vector3(54.5f,0,94),Quaternion.Euler(0,110,0));game.Boat.Unmoor();
                game.SetPause(false);referenceView=true;caveView=false;viewSize=15.5f;
            }
            viewSize=Mathf.Clamp(viewSize-Input.mouseScrollDelta.y*1.5f,8,28);
        }
        void LateUpdate()
        {
            if(!ready)return;game.CameraFollow.enabled=false;game.CameraFollow.Overview=false;
            NeutralizeExplorer();ApplyCamera(false);
        }
        void ApplyCamera(bool snap)
        {
            Vector3 wanted=caveView?CoastBlockoutLayout.CaveFloor+Vector3.up*3.8f:(referenceView?region.referenceFocus.position:game.Expedition.ActivePosition);
            focus=snap?wanted:Vector3.Lerp(focus,wanted,1-Mathf.Exp(-5*Time.unscaledDeltaTime));
            var rotation=caveView?Quaternion.LookRotation(-CoastBlockoutLayout.CaveAxis+Vector3.down*.33f):Quaternion.Euler(42,-18,0);
            float distance=caveView?10.5f:104,size=caveView?6.8f:viewSize;
            game.Camera.orthographic=!weakPerspective;game.Camera.orthographicSize=size;
            game.Camera.fieldOfView=2*Mathf.Atan(size/distance)*Mathf.Rad2Deg;
            game.Camera.nearClipPlane=.3f;game.Camera.farClipPlane=350;
            game.Camera.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*distance,rotation);
        }
        void OnGUI()
        {
            if(!ready||!showHud)return;
            Matrix4x4 previousMatrix=GUI.matrix;
            float scale=Screen.width/1920f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            var style=new GUIStyle(GUI.skin.label){fontSize=20,normal={textColor=new Color(.92f,.93f,.94f)}};
            GUI.Box(new Rect(20,20,570,106),GUIContent.none);
            GUI.Label(new Rect(34,28,540,29),"东岸白模验收 · "+(game.Expedition.OnFoot?"步行":"航行")+" · "+(weakPerspective?"弱透视":"正交"),style);
            style.fontSize=16;
            GUI.Label(new Rect(34,61,540,26),"WASD 移动　F 上下船　滚轮缩放　Home 回到起点",style);
            GUI.Label(new Rect(34,87,540,26),"F5 投影　F6 参考/跟随　F7 岩洞　F9 水面　F10 界面",style);
            string hint=game.Expedition.OnFoot?"沿石阶前往灯塔；返回栈桥按 F 登船":"靠近左侧栈桥、减速后按 F 下船";
            GUI.Box(new Rect(650,1010,620,42),hint);
            GUI.matrix=previousMatrix;
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;Active=false;Time.captureFramerate=0;Time.timeScale=1;}
    }
}
