using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Comparable gameplay views and direct checks of new reef residents.</summary>
public sealed class ReefLifeCheck:MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-reef-life-check")>=0;}}
 static bool Baseline {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-reef-life-baseline")>=0;}}
 bool Video {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-reef-life-demo")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested){GameDirector.VerificationMode=true;ReefGarden.Enabled=true;ReefLifeGarden.Enabled=!Baseline;Time.captureFramerate=20;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<ReefLifeCheck>();}
 string Dir {get{return "Verification/reef-life-v1/"+(Baseline?"before":"after");}}
 GameDirector game;string failure="";int frames;
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;Time.timeScale=1;Time.captureFramerate=0;}
 void Log(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure+=s+"; ";}
 void Require(bool ok,string why){if(!ok)failure+=why+"; ";}
 void View(Vector3 focus,float size){var rot=Quaternion.Euler(55,0,0);focus.y=0;game.Camera.transform.SetPositionAndRotation(focus-rot*Vector3.forward*160,rot);game.Camera.orthographicSize=size;}
 void Capture(string name){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",image.EncodeToPNG());Destroy(image);}
 IEnumerator Wait(float seconds){for(int i=0;i<Mathf.RoundToInt(seconds*20);i++)yield return new WaitForEndOfFrame();}
 IEnumerator MoveBoat(Vector3 p){game.Boat.Moor(p,Quaternion.identity);yield return new WaitForFixedUpdate();Physics.SyncTransforms();yield return new WaitForEndOfFrame();}
 float MeanDistance(ReefResidents life,Vector3 boat){boat.y=0;float sum=0;for(int i=0;i<12;i++){var p=life.FishPosition(i);p.y=0;sum+=Vector3.Distance(p,boat);}return sum/12;}
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1280,720,FullScreenMode.Windowed);
  while(Time.frameCount<5||OceanSurface.Active==null||!OceanSurface.Active.BathymetryReady)yield return new WaitForEndOfFrame();
  game=GetComponent<GameDirector>();var garden=FindObjectOfType<ReefLifeGarden>();var residents=FindObjectOfType<ReefResidents>();var ecology=FindObjectOfType<CoastalEcology>();
  game.UI.Close();game.HarbourMenu=false;game.CameraFollow.enabled=false;game.SetPause(false);foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;
  ecology.BirdVisitsEnabled=false;FindObjectOfType<Wildlife>().AmbientFlights=false;
  game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);View(new Vector3(54,0,91),23);var shore=FindObjectOfType<ShorelineAtmosphere>();shore.SetPreviewTime(ShoreCycle.Period*.53f);
  while(Time.frameCount<50)yield return new WaitForEndOfFrame();
  Require(garden!=null&&garden.Ready,"Garden missing");Require(game.World.Root.GetComponentsInChildren<Collider>().Length==44,"Navigation colliders changed");Require(ecology.Ready&&ecology.FishCount==18&&ecology.BirdCount==2,"Original ecology missing");
  Require(Vector3.Distance(game.Boat.transform.position,new Vector3(54,0,91))<.1f,"Comparison pose unsettled");
  if(!Baseline){
   Require(garden.CoralCount>=35&&garden.TuftCount>=100&&garden.BladeCount>=2200,"Habitat expansion insufficient");
   Require(garden.FanCorals>=4&&garden.DomeCorals>=4&&garden.SpongeTubes>=18&&garden.Anemones>=8,"Habitat forms insufficient");
   Require(garden.TriangleCount<320000&&garden.RendererCount<=4,"Static habitat budget exceeded");Require(garden.HighestTip<OceanSurface.SeaLevel-.35f,"Plants cross mean sea surface");Require(garden.MinimumLandingDistance>=3.8f,"Plants occupy berth");
   Require(residents!=null&&residents.Ready&&residents.FishCount==36&&residents.CrabCount==6,"Residents missing");Require(residents.Stars>=6&&residents.Urchins>=6,"Bottom residents insufficient");
  }
  foreach(var name in new[]{"Deepsea/ReefCoral","Deepsea/ReefFoliage","Deepsea/ReefBenthos","Deepsea/ReefFish"}){var s=Shader.Find(name);Require(s!=null&&s.isSupported,"Unsupported shader "+name);}
  float captured=Time.time;Time.timeScale=0;yield return new WaitForEndOfFrame();Capture("normal");
  View(new Vector3(51,0,104),12);yield return new WaitForEndOfFrame();Capture("north");
  View(new Vector3(49,0,81),11);yield return new WaitForEndOfFrame();Capture("nursery");
  View(new Vector3(45,0,71),11);yield return new WaitForEndOfFrame();Capture("south");Time.timeScale=1;
  float fishMotion=0,crabMotion=0,escapeBefore=0,escapeAfter=0,shelterDistance=0;int fishScares=0,crabScares=0;
  if(!Baseline){
   yield return MoveBoat(new Vector3(100,0,60));shore.SetPreviewTime(-1);
   Vector3[] start=new Vector3[36];for(int i=0;i<36;i++)start[i]=residents.FishPosition(i);Vector3 crabStart=residents.CrabPosition(0);
   yield return Wait(2);
   for(int i=0;i<36;i++)fishMotion+=Vector3.Distance(start[i],residents.FishPosition(i))/36;crabMotion=Vector3.Distance(crabStart,residents.CrabPosition(0));Require(fishMotion>.3f&&crabMotion>.03f,"Residents appear static");
   if(Video)foreach(var shot in new[]{new Vector4(54,91,23,6),new Vector4(51,104,12,5),new Vector4(49,81,11,5),new Vector4(45,71,11,4)}){
    View(new Vector3(shot.x,0,shot.y),shot.z);for(int i=0;i<Mathf.RoundToInt(shot.w*20);i++){yield return new WaitForEndOfFrame();Capture("video-"+(frames++).ToString("0000"));}
   }
   int before=residents.SchoolScares;Vector3 near=residents.SchoolHome(0)+Vector3.right*3;escapeBefore=MeanDistance(residents,near);
   yield return MoveBoat(near);yield return Wait(1.5f);escapeAfter=MeanDistance(residents,near);fishScares=residents.SchoolScares-before;
   Require(fishScares>0&&escapeAfter>escapeBefore+.3f,"Reef fish do not respond to boat");
   yield return Wait(3);
   for(int i=0;i<12;i++){Vector3 delta=residents.FishPosition(i)-residents.SchoolHome(0);delta.y=0;shelterDistance=Mathf.Max(shelterDistance,delta.magnitude);}
   Require(shelterDistance<8.5f,"Moored boat drives reef fish out of habitat");
   before=residents.CrabScares;near=residents.CrabPosition(0);near.y=0;near+=Vector3.right*2;yield return MoveBoat(near);yield return Wait(1);crabScares=residents.CrabScares-before;Require(crabScares>0,"Crabs do not react to boat");
   Require(residents.MinimumBedClearance>.15f,"New fish intersect bed");
  }
  var result=new Result{passed=failure=="",baseline=Baseline,detail=failure==""?"Gameplay-scale habitat comparison, resident motion and vessel response, water/berth and collider checks passed":failure,captureTime=captured,corals=garden.CoralCount,tufts=garden.TuftCount,blades=garden.BladeCount,domes=garden.DomeCorals,fans=garden.FanCorals,spongeGroups=garden.SpongeGroups,spongeTubes=garden.SpongeTubes,anemones=garden.Anemones,triangles=garden.TriangleCount,renderers=garden.RendererCount,highestTip=garden.HighestTip,minimumLandingDistance=garden.MinimumLandingDistance,bakeMilliseconds=garden.BakeMilliseconds,fish=Baseline?0:residents.FishCount,crabs=Baseline?0:residents.CrabCount,stars=Baseline?0:residents.Stars,urchins=Baseline?0:residents.Urchins,fishMotion=fishMotion,crabMotion=crabMotion,fishScares=fishScares,crabScares=crabScares,escapeBefore=escapeBefore,escapeAfter=escapeAfter,maximumShelterDistance=shelterDistance,minimumFishBedClearance=Baseline?0:residents.MinimumBedClearance,videoFrames=frames,colliders=game.World.Root.GetComponentsInChildren<Collider>().Length};
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log(failure==""?"REEF_LIFE_PLAYER_OK":"REEF_LIFE_PLAYER_FAILED "+failure);Time.captureFramerate=0;Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed,baseline;public string detail;public float captureTime,highestTip,minimumLandingDistance,fishMotion,crabMotion,escapeBefore,escapeAfter,maximumShelterDistance,minimumFishBedClearance;public double bakeMilliseconds;public int corals,tufts,blades,domes,fans,spongeGroups,spongeTubes,anemones,triangles,renderers,fish,crabs,stars,urchins,fishScares,crabScares,videoFrames,colliders;}
}
}
