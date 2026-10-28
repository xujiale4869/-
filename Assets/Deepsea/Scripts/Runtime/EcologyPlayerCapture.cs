using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Opt-in standalone checks use the isolated verification save.</summary>
public sealed class EcologyPlayerCapture : MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-ecology-check")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested)GameDirector.VerificationMode=true;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<EcologyPlayerCapture>();}
 const string Dir="Verification/ecology-v1";string failure="";GameDirector game;CoastalEcology eco;
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;Time.captureFramerate=0;Time.timeScale=1;}
 void Log(string c,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)failure=c;}
 Color32[] Capture(string name){
  var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",tex.EncodeToPNG());
  var pixels=tex.GetPixels32();Destroy(tex);return pixels;
 }
 void View(float size){
  Quaternion r=Quaternion.Euler(55,0,0);Vector3 focus=eco.Home+new Vector3(4,0,2);
  game.Camera.transform.SetPositionAndRotation(focus-r*Vector3.forward*160,r);game.Camera.orthographicSize=size;
 }
 void Require(bool condition,string message){if(!condition){failure+=message+"; ";Debug.LogWarning("ECOLOGY_CHECK "+message);}}
 IEnumerator Until(Func<bool> predicate,float seconds){
  float until=Time.realtimeSinceStartup+seconds;while(!predicate()&&Time.realtimeSinceStartup<until)yield return null;
 }
 float FishBoatDistance(){
  float sum=0;Vector3 boat=game.Boat.transform.position;boat.y=0;
  for(int i=0;i<eco.FishCount;i++){Vector3 p=eco.FishPosition(i);p.y=0;sum+=(p-boat).magnitude;}return sum/eco.FishCount;
 }
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1280,720,FullScreenMode.Windowed);
  yield return new WaitForSecondsRealtime(2);
  game=GetComponent<GameDirector>();eco=FindObjectOfType<CoastalEcology>();
  if(eco==null||!eco.Ready){File.WriteAllText(Dir+"/result.json","{\"passed\":false,\"detail\":\"Ecology did not initialize\"}");Application.Quit(1);yield break;}
  game.UI.Close();game.HarbourMenu=false;game.CameraFollow.enabled=false;
  foreach(var canvas in FindObjectsOfType<Canvas>())canvas.enabled=false;
  FindObjectOfType<Wildlife>().AmbientFlights=false;
  Vector3 far=eco.Home+new Vector3(24,0,-18);game.Boat.Moor(far,Quaternion.identity);View(23);
  foreach(string name in new[]{"Deepsea/Seabird","Deepsea/SeabirdShadow","Deepsea/Seagrass"})Require(Shader.Find(name).isSupported,"Unsupported standalone shader "+name);
  Require(eco.FishCount==18&&eco.BirdCount==2,"Missing ecology population");
  var root=GameObject.Find("海草栖息区 · 鱼群与海鸟联动");Require(root.GetComponentsInChildren<Collider>().Length==0,"Ecology introduced collision geometry");
  yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Capture("normal-view");
  Time.timeScale=0;
  var birds=new Renderer[2];for(int i=0;i<2;i++)birds[i]=GameObject.Find("低掠海鸟 · "+(i+1)).GetComponent<Renderer>();
  foreach(var bird in birds)bird.enabled=false;yield return new WaitForEndOfFrame();var absent=Capture("normal-without-birds");
  foreach(var bird in birds)bird.enabled=true;yield return new WaitForEndOfFrame();var present=Capture("normal-birds");
  int birdPixels=0;for(int i=0;i<present.Length;i++)if(Mathf.Abs(present[i].r-absent[i].r)+Mathf.Abs(present[i].g-absent[i].g)+Mathf.Abs(present[i].b-absent[i].b)>20)birdPixels++;
  Require(birdPixels>=25,"Birds are unreadable at normal camera size");Time.timeScale=1;
  View(11);yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();Capture("nursery-detail");View(18);
  yield return Until(()=>eco.VisitsStarted>0,35);Require(eco.VisitsStarted>0,"No natural fish-dependent visit");
  float minimumBirdGeometry=100;Time.captureFramerate=20;
  for(int frame=0;frame<180;frame++){
   yield return new WaitForEndOfFrame();Capture("video-"+frame.ToString("0000"));
   if(frame==70){
    for(int b=0;b<2;b++){
     Transform bird=GameObject.Find("低掠海鸟 · "+(b+1)).transform;
     foreach(Vector3 vertex in bird.GetComponent<MeshFilter>().sharedMesh.vertices){
      Vector3 world=bird.TransformPoint(vertex);
      minimumBirdGeometry=Mathf.Min(minimumBirdGeometry,world.y-OceanSurface.Height(world,Time.time));
     }
    }
   }
   if(frame==58)Capture("approach");if(frame==75)Capture("low-pass");if(frame==110)Capture("fish-scatter");
  }
  Time.captureFramerate=0;
  Require(eco.LowPasses>=1&&eco.BirdScares>=1,"Low pass failed to disturb school or touch water");
  Require(eco.TargetErrorAtLaunch<.1f,"Bird target was not the actual school centre");
  Require(eco.MinimumAirClearance>=.24f,"Bird centre clipped through sea");
  Require(minimumBirdGeometry>=-.02f,"Gliding bird geometry crossed the wave surface");
  yield return Until(()=>eco.ReturnsCompleted>=1&&eco.MeanHomeDistance<4.6f,30);
  Require(eco.ReturnsCompleted>=1&&eco.MeanHomeDistance<4.6f,"Fish did not return to grass after bird passed");
  yield return new WaitForEndOfFrame();Capture("returned-to-grass");
  int returned=eco.ReturnsCompleted;eco.BirdVisitsEnabled=false;
  Vector3 nearTarget=eco.Home+Vector3.right*.5f;
  game.Boat.Moor(nearTarget,Quaternion.identity);yield return null;
  Debug.Log("ECOLOGY_MOOR_FIRST_FRAME boat="+game.Boat.transform.position+" target="+nearTarget+" distance="+FishBoatDistance());
  // Interpolated rigidbodies may still expose the old pose for the first rendered frame.
  // Measure the escape only after the teleport has reached the physics/render pose.
  yield return new WaitForFixedUpdate();Physics.SyncTransforms();yield return new WaitForEndOfFrame();
  float before=FishBoatDistance();Vector3 beforeBoat=game.Boat.transform.position;
  Require(Vector3.Distance(beforeBoat,nearTarget)<.1f,"Moor teleport did not settle before measurement");
  Debug.Log("ECOLOGY_MOOR_SETTLED boat="+beforeBoat+" target="+nearTarget+" distance="+before);
  yield return new WaitForSecondsRealtime(1.7f);float after=FishBoatDistance();
  Require(eco.BoatScares>=1&&eco.State==CoastalEcology.SchoolState.Scattered,"Vessel failed to disturb school");
  Debug.Log("ECOLOGY_ESCAPE before="+before+" after="+after+" boat="+game.Boat.transform.position);
  Require(after>before+.4f,"Fish did not swim away from vessel");
  Require(!eco.TryBeginPass(),"Bird began feeding over a nearby vessel");
  yield return new WaitForEndOfFrame();Capture("boat-disturbance");
  game.Boat.Moor(far,Quaternion.identity);
  yield return Until(()=>eco.ReturnsCompleted>returned&&eco.MeanHomeDistance<4.6f,35);
  Require(eco.ReturnsCompleted>returned&&eco.MeanHomeDistance<4.6f,"Fish did not recover after vessel left, mean grass distance="+eco.MeanHomeDistance);
  int aborted=eco.VisitsAborted;eco.BirdVisitsEnabled=true;
  yield return Until(()=>eco.Passing,55);
  Require(eco.Passing,"Second natural visit failed to start");
  int lowBefore=eco.LowPasses;
  game.Boat.Moor(eco.SchoolCentre,Quaternion.identity);yield return new WaitForSecondsRealtime(.6f);
  Require(eco.VisitsAborted>aborted&&eco.LowPasses==lowBefore,"Approaching vessel did not cancel a pending low pass");
  yield return new WaitForEndOfFrame();Capture("aborted-approach");
  game.Boat.Moor(far,Quaternion.identity);
  Require(eco.MinimumBedClearance>=.12f,"Fish intersected the seabed");
  var result=new Result{passed=failure=="",detail=failure==""?"Windows URP, natural school-targeted visit, bird scatter and return, vessel scatter and return, interrupted low pass, water and bed clearance passed":failure,
   fish=eco.FishCount,birds=eco.BirdCount,birdVisiblePixels=birdPixels,visits=eco.VisitsStarted,lowPasses=eco.LowPasses,aborted=eco.VisitsAborted,birdScares=eco.BirdScares,boatScares=eco.BoatScares,returns=eco.ReturnsCompleted,
   minimumBirdGeometry=minimumBirdGeometry,minimumAirClearance=eco.MinimumAirClearance,minimumBedClearance=eco.MinimumBedClearance,targetError=eco.TargetErrorAtLaunch,vesselEscapeDistance=after-before,escapeDistanceBefore=before,escapeDistanceAfter=after,measuredBoatX=beforeBoat.x,measuredBoatZ=beforeBoat.z};
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log(failure==""?"ECOLOGY_PLAYER_OK":"ECOLOGY_PLAYER_FAILED "+failure);
  Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed;public string detail;public int fish,birds,birdVisiblePixels,visits,lowPasses,aborted,birdScares,boatScares,returns;public float minimumBirdGeometry,minimumAirClearance,minimumBedClearance,targetError,vesselEscapeDistance,escapeDistanceBefore,escapeDistanceAfter,measuredBoatX,measuredBoatZ;}
}
}
