using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Opt-in standalone comparison; uses the existing isolated verification save.</summary>
public sealed class WaterPolishCheck : MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-water-polish-check")>=0;}}
 static bool Baseline {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-water-baseline")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested){GameDirector.VerificationMode=true;SeabedRelief.Enabled=!Baseline;Time.captureFramerate=20;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<WaterPolishCheck>();}
 string Dir {get{return "Verification/water-polish-v1/"+(Baseline?"before":"after");}}
 string failure="";GameDirector game;float captureTime;
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;Time.timeScale=1;Time.captureFramerate=0;}
 void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure+=message+"; ";}
 void Require(bool value,string message){if(!value)failure+=message+"; ";}
 Color32[] Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",t.EncodeToPNG());var p=t.GetPixels32();Destroy(t);return p;}
 void View(Vector3 focus,float size){var rotation=Quaternion.Euler(55,0,0);game.Camera.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*160,rotation);game.Camera.orthographicSize=size;}
 int FlowDifference(Color32[] a,Color32[] b){int changed=0;for(int y=130;y<550;y++)for(int x=1000;x<1250;x++){int i=y*1280+x;if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>10)changed++;}return changed;}
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1280,720,FullScreenMode.Windowed);
  while(Time.frameCount<50)yield return new WaitForEndOfFrame();
  game=GetComponent<GameDirector>();var ocean=OceanSurface.Active;var coast=FindObjectOfType<ShorelineAtmosphere>();var eco=FindObjectOfType<CoastalEcology>();
  Require(ocean!=null&&ocean.BathymetryReady,"Seabed missing");Require(coast!=null&&coast.Ready&&coast.AudioReady,"Existing coast/audio missing");
  Require(coast.RockCount==2&&coast.ColliderCountBefore==coast.ColliderCountAfter,"Existing reefs or navigation colliders changed");
  Require(eco!=null&&eco.Ready&&eco.FishCount==18&&eco.BirdCount==2,"Existing ecology missing");
  foreach(var n in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/ShoreSwash"}){var shader=Shader.Find(n);Require(shader!=null&&shader.isSupported,"Unsupported shader "+n);}
  game.UI.Close();game.HarbourMenu=false;game.CameraFollow.enabled=false;foreach(var canvas in FindObjectsOfType<Canvas>())canvas.enabled=false;
  eco.BirdVisitsEnabled=false;FindObjectOfType<Wildlife>().AmbientFlights=false;
  game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);View(new Vector3(47,0,92),23);coast.SetPreviewTime(ShoreCycle.Period*.53f);
  Time.timeScale=0;captureTime=Time.time;yield return new WaitForEndOfFrame();var normal=Capture("normal");
  View(new Vector3(64,0,89),11);yield return new WaitForEndOfFrame();Capture("reef");
  View(new Vector3(54,0,85),11);yield return new WaitForEndOfFrame();Capture("grass");
  float min=100,max=-100;for(float z=65;z<=122;z+=1.5f)for(float x=40;x<=86;x+=1.5f){float h=ocean.BottomHeight(x,z);Require(!float.IsNaN(h)&&!float.IsInfinity(h),"Non-finite bed");min=Mathf.Min(min,h);max=Mathf.Max(max,h);}
  Require(max<=-.579f,"Visual sand emerged above the protected submerged level");
  float clearance=100;for(int i=0;i<eco.FishCount;i++){Vector3 p=eco.FishPosition(i);clearance=Mathf.Min(clearance,p.y-ocean.BottomHeight(p.x,p.z));}
  Require(clearance>=.10f,"Fish intersect new seabed");
  View(new Vector3(47,0,92),23);Time.timeScale=1;for(int i=0;i<20;i++)yield return new WaitForEndOfFrame();
  var moving=Capture("flow");int difference=FlowDifference(normal,moving);Require(difference>200,"Water motion unreadable in open-water crop");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-water-polish-demo")>=0){
   coast.SetPreviewTime(-1);View(new Vector3(51,0,91),18);
   for(int frame=0;frame<180;frame++){yield return new WaitForEndOfFrame();Capture("video-"+frame.ToString("0000"));}
  }
  var result=new Result{passed=failure=="",baseline=Baseline,detail=failure==""?"Windows rendering, bounded seabed, existing coast/audio/ecology, fish clearance and moving open water passed":failure,
   bakeMilliseconds=ocean.SeabedBakeMilliseconds,captureTime=captureTime,minBed=min,maxBed=max,minimumFishBedClearance=clearance,flowChangedPixels=difference,colliders=game.World.Root.GetComponentsInChildren<Collider>().Length};
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log(failure==""?"WATER_POLISH_PLAYER_OK":"WATER_POLISH_PLAYER_FAILED "+failure);
  Time.timeScale=1;Time.captureFramerate=0;Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed,baseline;public string detail;public double bakeMilliseconds;public float captureTime,minBed,maxBed,minimumFishBedClearance;public int flowChangedPixels,colliders;}
}
}
