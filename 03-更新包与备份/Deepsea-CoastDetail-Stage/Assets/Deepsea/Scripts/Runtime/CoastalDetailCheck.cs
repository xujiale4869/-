using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Opt-in isolated material comparison and real Rigidbody/control-path wake check.</summary>
public sealed class CoastalDetailCheck : MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-coast-detail-check")>=0;}}
 static bool Baseline {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-coast-detail-baseline")>=0;}}
 bool Video {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-coast-detail-demo")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested){GameDirector.VerificationMode=true;CoastalDetail.Enabled=!Baseline;Time.captureFramerate=20;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<CoastalDetailCheck>();}
 string Dir {get{return "Verification/coast-detail-v1/"+(Baseline?"before":"after");}}
 string failure="";GameDirector game;int frame;float largestWakeEdge;Mesh wakeMesh;
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;Time.timeScale=1;Time.captureFramerate=0;}
 void Log(string m,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)failure+=m+"; ";}
 void Require(bool v,string why){if(!v)failure+=why+"; ";}
 void Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
 void View(Vector3 focus,float size){var r=Quaternion.Euler(55,0,0);focus.y=0;game.Camera.transform.SetPositionAndRotation(focus-r*Vector3.forward*160,r);game.Camera.orthographicSize=size;}
 float WakeWeight(){float sum=0;foreach(var c in wakeMesh.colors)sum+=c.a;return sum;}
 void CheckEdges(){var v=wakeMesh.vertices;var c=wakeMesh.colors;var tr=wakeMesh.triangles;for(int k=0;k<tr.Length;k+=3)for(int j=0;j<3;j++){int a=tr[k+j],b=tr[k+(j+1)%3];if(c[a].a>.01f||c[b].a>.01f)largestWakeEdge=Mathf.Max(largestWakeEdge,Vector3.Distance(v[a],v[b]));}}
 void MovieFrame(){if(Video)Capture("video-"+(frame++).ToString("0000"));}
 IEnumerator Drive(float seconds,float throttle,float rudder){
  game.Boat.AutomationThrottle=throttle;game.Boat.AutomationRudder=rudder;
  for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){
   yield return new WaitForEndOfFrame();CheckEdges();View(game.Boat.transform.position-game.Boat.transform.forward*5,14);MovieFrame();
  }
 }
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1280,720,FullScreenMode.Windowed);
  while(Time.frameCount<5||OceanSurface.Active==null||!OceanSurface.Active.BathymetryReady)yield return new WaitForEndOfFrame();
  game=GetComponent<GameDirector>();var coast=FindObjectOfType<ShorelineAtmosphere>();var eco=FindObjectOfType<CoastalEcology>();var detail=FindObjectOfType<CoastalDetail>();
  game.UI.Close();game.HarbourMenu=false;game.CameraFollow.enabled=false;game.SetPause(false);foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;
  eco.BirdVisitsEnabled=false;FindObjectOfType<Wildlife>().AmbientFlights=false;
  game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);View(new Vector3(47,0,92),23);coast.SetPreviewTime(ShoreCycle.Period*.53f);
  // Let Rigidbody interpolation settle before freezing the paired comparison at frame 50.
  while(Time.frameCount<50)yield return new WaitForEndOfFrame();
  Require(Vector3.Distance(game.Boat.transform.position,new Vector3(54,0,91))<.1f,"Comparison boat pose not settled");
  Require(detail!=null&&detail.Ready&&detail.CollidersBefore==detail.CollidersAfter,"Detail/collider state failed");
  Require(Baseline?detail.TimberRenderers==0:detail.TimberRenderers>=10,"East pier timber adaptation failed");
  Require(coast.Ready&&coast.AudioReady&&coast.RockCount==2,"Existing coast missing");Require(eco.Ready&&eco.FishCount==18&&eco.BirdCount==2,"Existing ecology missing");
  foreach(string name in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/ShoreSwash","Deepsea/CoastalTimber","Deepsea/WakeFoam"}){var shader=Shader.Find(name);Require(shader!=null&&shader.isSupported,"Unsupported shader "+name);}
  float captureTime=Time.time;Time.timeScale=0;yield return new WaitForEndOfFrame();Capture("normal");
  View(new Vector3(64,0,89),11);yield return new WaitForEndOfFrame();Capture("reef");
  View(new Vector3(44,0,94),8);yield return new WaitForEndOfFrame();Capture("shore");
  View(new Vector3(54,0,85),11);yield return new WaitForEndOfFrame();Capture("grass");
  float minClearance=100;for(int i=0;i<eco.FishCount;i++){var p=eco.FishPosition(i);minClearance=Mathf.Min(minClearance,p.y-OceanSurface.Active.BottomHeight(p.x,p.z));}Require(minClearance>=.1f,"Fish intersect visual bed");
  Time.timeScale=1;float speed=0,turn=0,stopped=0,weight=0,faded=0,distance=0;
  if(!Baseline){
   coast.SetPreviewTime(-1);View(new Vector3(49,0,92),18);
   if(Video)for(int i=0;i<120;i++){yield return new WaitForEndOfFrame();MovieFrame();}
   foreach(var f in game.World.Root.GetComponentsInChildren<MeshFilter>())if(f.gameObject.name=="船尾渐散泡沫")wakeMesh=f.sharedMesh;
   Require(wakeMesh!=null,"Wake mesh missing");
   Vector3 start=new Vector3(80,0,60);game.Boat.Moor(start,Quaternion.identity);yield return new WaitForFixedUpdate();yield return new WaitForEndOfFrame();
   game.Boat.Unmoor();game.Boat.AutomationInput=true;
   yield return Drive(4,1,0);speed=game.Boat.Speed;Capture("cruise");
   Quaternion initial=game.Boat.transform.rotation;yield return Drive(3,1,.85f);turn=Quaternion.Angle(initial,game.Boat.transform.rotation);weight=WakeWeight();Capture("turn");
   yield return Drive(5.5f,0,0);stopped=game.Boat.Speed;faded=WakeWeight();distance=Vector3.Distance(start,game.Boat.transform.position);Capture("stop");game.Boat.AutomationInput=false;
   Require(speed>2&&turn>15&&distance>12,"Cruise/turn control path failed");Require(weight>1&&stopped<.15f&&faded<weight*.10f,"Wake does not dissipate after stopping");Require(largestWakeEdge<8,"Wake bridged invalid/teleport samples");Require(game.Boat.Health>.99f,"Boat path collided with scenery");
  }
  var result=new Result{passed=failure=="",baseline=Baseline,detail=failure==""?"Local materials, settled comparison pose, original ecology/coast, collision count and control-driven cruise/turn/stop passed":failure,captureTime=captureTime,minimumFishBedClearance=minClearance,timberRenderers=detail.TimberRenderers,colliders=detail.CollidersAfter,cruiseSpeed=speed,turnDegrees=turn,stoppedSpeed=stopped,wakeWeightAtTurn=weight,wakeWeightAfterStop=faded,largestVisibleWakeEdge=largestWakeEdge,travelDistance=distance,videoFrames=frame};
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log(failure==""?"COAST_DETAIL_PLAYER_OK":"COAST_DETAIL_PLAYER_FAILED "+failure);
  Time.timeScale=1;Time.captureFramerate=0;Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed,baseline;public string detail;public float captureTime,minimumFishBedClearance,cruiseSpeed,turnDegrees,stoppedSpeed,wakeWeightAtTurn,wakeWeightAfterStop,largestVisibleWakeEdge,travelDistance;public int timberRenderers,colliders,videoFrames;}
}
}
