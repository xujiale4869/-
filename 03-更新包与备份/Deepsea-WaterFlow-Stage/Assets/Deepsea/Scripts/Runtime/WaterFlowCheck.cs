using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Isolated opt-in comparison of the previous release and the current surface treatment.</summary>
public sealed class WaterFlowCheck : MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-water-flow-check")>=0;}}
 static bool Baseline {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-flow-baseline")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested){GameDirector.VerificationMode=true;ShallowWaterFlow.Enabled=!Baseline;Time.captureFramerate=20;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<WaterFlowCheck>();}
 string Dir {get{return "Verification/water-flow-v1/"+(Baseline?"before":"after");}}
 string failure="";GameDirector game;float captureTime;
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;Time.timeScale=1;Time.captureFramerate=0;}
 void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure+=message+"; ";}
 void Require(bool v,string why){if(!v)failure+=why+"; ";}
 Color32[] Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",t.EncodeToPNG());var p=t.GetPixels32();Destroy(t);return p;}
 void View(Vector3 focus,float size){var rotation=Quaternion.Euler(55,0,0);game.Camera.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*160,rotation);game.Camera.orthographicSize=size;}
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1280,720,FullScreenMode.Windowed);
  while(Time.frameCount<50)yield return new WaitForEndOfFrame();
  game=GetComponent<GameDirector>();var ocean=OceanSurface.Active;var flow=FindObjectOfType<ShallowWaterFlow>();var coast=FindObjectOfType<ShorelineAtmosphere>();var eco=FindObjectOfType<CoastalEcology>();
  Require(ocean!=null&&ocean.BathymetryReady,"Seabed missing");Require(flow!=null&&flow.Ready&&flow.ObstacleCount>=2,"Current field missing reefs");
  Require(coast!=null&&coast.Ready&&coast.AudioReady&&coast.RockCount==2,"Existing coast/audio missing");
  Require(coast.ColliderCountBefore==coast.ColliderCountAfter,"Navigation colliders changed");
  Require(eco!=null&&eco.Ready&&eco.FishCount==18&&eco.BirdCount==2,"Existing ecology missing");
  Require(Mathf.Abs(ocean.Material.GetFloat("_FlowPolish")-(Baseline?0:1))<.01f,"Comparison flag mismatch");
  Require(ocean.Material.GetTexture("_CurrentMap")!=null&&ocean.Material.GetTexture("_CurrentNormals")!=null,"Flow textures missing");
  foreach(var n in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/ShoreSwash","Deepsea/WakeFoam"}){var shader=Shader.Find(n);Require(shader!=null&&shader.isSupported,"Unsupported shader "+n);}
  float minimum=100,maximum=-100,maxTurn=0;
  for(float z=48;z<156;z+=2)for(float x=10;x<118;x+=2){var v=flow.FlowAt(new Vector2(x,z));float speed=v.magnitude;Require(!float.IsNaN(speed)&&!float.IsInfinity(speed),"Invalid flow");minimum=Mathf.Min(minimum,speed);maximum=Mathf.Max(maximum,speed);maxTurn=Mathf.Max(maxTurn,Vector2.Angle(v,ShallowWaterFlow.OceanDirection));}
  Require(minimum>=.249f&&maximum<=1.351f&&maxTurn>20,"Unbounded or uniform current");
  game.UI.Close();game.HarbourMenu=false;game.CameraFollow.enabled=false;foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;
  eco.BirdVisitsEnabled=false;FindObjectOfType<Wildlife>().AmbientFlights=false;
  game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);View(new Vector3(47,0,92),23);coast.SetPreviewTime(ShoreCycle.Period*.53f);
  Time.timeScale=0;captureTime=Time.time;yield return new WaitForEndOfFrame();var normal=Capture("normal");
  View(new Vector3(64,0,89),11);yield return new WaitForEndOfFrame();Capture("reef");
  View(new Vector3(54,0,85),11);yield return new WaitForEndOfFrame();Capture("grass");
  View(new Vector3(47,0,92),23);Time.timeScale=1;for(int i=0;i<20;i++)yield return new WaitForEndOfFrame();var moving=Capture("flow");
  int difference=0;for(int y=130;y<550;y++)for(int x=1000;x<1250;x++){int i=y*1280+x;if(Mathf.Abs(normal[i].r-moving[i].r)+Mathf.Abs(normal[i].g-moving[i].g)+Mathf.Abs(normal[i].b-moving[i].b)>10)difference++;}
  Require(difference>200,"Open water remains static");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-water-flow-demo")>=0){
   coast.SetPreviewTime(-1);View(new Vector3(51,0,91),18);
   for(int frame=0;frame<160;frame++){yield return new WaitForEndOfFrame();Capture("video-"+frame.ToString("0000"));}
  }
  var result=new Result{passed=failure=="",baseline=Baseline,detail=failure==""?"Current bounds, reef deflection, original coast/ecology/audio, shader support and moving water passed":failure,
   captureTime=captureTime,flowBakeMilliseconds=flow.BakeMilliseconds,minimumSpeed=minimum,maximumSpeed=maximum,maximumTurnDegrees=maxTurn,obstacles=flow.ObstacleCount,flowChangedPixels=difference,colliders=game.World.Root.GetComponentsInChildren<Collider>().Length};
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log(failure==""?"WATER_FLOW_PLAYER_OK":"WATER_FLOW_PLAYER_FAILED "+failure);
  Time.timeScale=1;Time.captureFramerate=0;Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed,baseline;public string detail;public double flowBakeMilliseconds;public float captureTime,minimumSpeed,maximumSpeed,maximumTurnDegrees;public int obstacles,flowChangedPixels,colliders;}
}
}
