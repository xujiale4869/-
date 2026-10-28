using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaLifeNavigationV23 {
  [Serializable]public class NavigationReport {
   public bool passed;public string scope="v23 water and navigation: actual Rigidbody controls, acceleration/coasting/braking/reverse, world anchored wakes, obstacle retreat, float/pause, isolated save and fixed-camera frame sampling. Automated inputs, not manual whole-map playtest.";
   public string resolution,gpu,savePath;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>();
   public float maxWave,maxDerivativeError,initialSpeed,cruiseSpeed,coastSpeed,coastTravel,brakedSpeed,reverseSpeed,turnDegrees,reverseTurnDegrees,wakeAnchorShift,obstacleRetreat,floatError;
   public double fps,p95ms;public int sampleFrames,peakWakePoints;
  }
  NavigationReport report=new NavigationReport();string evidence;
  void RecordLog(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(s);if(type==LogType.Warning)report.warnings.Add(s);}
  void Check(bool value,string name){report.checks.Add(name+": "+value);if(!value)report.errors.Add("CHECK: "+name);}
  string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
  IEnumerator Shot(string name){for(int i=0;i<3;i++)yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();int dark=0,pink=0;foreach(var p in tex.GetPixels32()){if(p.r+p.g+p.b<25)dark++;if(p.r>245&&p.b>245&&p.g<10)pink++;}Check(dark<tex.width*tex.height*.85f,name+" visible");Check(pink<tex.width*tex.height*.001f,name+" no pink");File.WriteAllBytes(Path.Combine(evidence,name+".png"),tex.EncodeToPNG());Destroy(tex);}
  void ResetRoute(Vector3 p,float yaw){game.DriveInput=Vector2.zero;game.PlaceBoat(p,yaw);throttle=0;rudder=0;yawRate=0;points.Clear();previousBoat=p;previousSpeed=0;game.SnapWaterCamera();}
  float Along {get{return Vector3.Dot(game.body.velocity,game.boat.forward);}}
  IEnumerator RunReview(){
   evidence=Arg("-navigation-review-dir","Verification/sea-navigation-v23-review01");Directory.CreateDirectory(evidence);StartCoroutine(ReviewTimeout());
   report.savePath=game.store.PathName;report.resolution=Screen.width+"x"+Screen.height;report.gpu=SystemInfo.graphicsDeviceName;
   game.AutoInput=true;ResetRoute(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);yield return new WaitForSeconds(1);
   Check(FindObjectOfType<GameDirector>()==null,"independent life entry");Check(game.GetComponent<SeaLifeWaterV22>()==null,"one active water controller");Check(game.navigationV23==this,"new drive hook installed");Check(report.savePath.Contains(Application.temporaryCachePath),"review save isolated");
   Check(Mathf.Abs(newWater.GetFloat("_Refraction")-.0022f)<.0000001f,"accepted refraction retained");Check(surface.GetComponent<MeshFilter>().sharedMesh.vertexCount>10000,"existing subdivided water mesh retained");
   Check(!game.boat.GetComponent<BoxCollider>().enabled&&game.boat.GetComponent<MeshCollider>().convex,"tapered collision hull active");
   for(int i=0;i<2000;i++){var p=new Vector2(-20+(i%50)*1.4f,-20+(i/50)*1.5f);var f=Field(p,i*.017f);report.maxWave=Mathf.Max(report.maxWave,Mathf.Abs(f.x));float dx=(Field(p+Vector2.right*.001f,i*.017f).x-Field(p-Vector2.right*.001f,i*.017f).x)/.002f;report.maxDerivativeError=Mathf.Max(report.maxDerivativeError,Mathf.Abs(dx-f.y));}
   Check(report.maxWave>.07f&&report.maxWave<.1221f,"gentle Gerstner height bounded and nonzero");Check(report.maxDerivativeError<.002f,"world-coordinate inverse derivatives agree");
   Check(NavigationResistanceV23.ViscousAcceleration(0,8,.58f,4.6f)==0,"zero-speed drag finite");Check(NavigationResistanceV23.ViscousAcceleration(3,8,.58f,4.6f)<0&&NavigationResistanceV23.ViscousAcceleration(-3,8,.58f,4.6f)>0,"water drag opposes both travel directions");
   SetBaseline(true);yield return Shot("01-v22-comparison");SetBaseline(false);yield return Shot("02-v23-flowing-water");yield return new WaitForSeconds(1.2f);yield return Shot("03-v23-later-wave");
   ResetRoute(new Vector3(17,SeaLifeRuntime.Sea,1),0);game.DriveInput=Vector2.up;yield return new WaitForSeconds(.16f);report.initialSpeed=Along;yield return new WaitForSeconds(3.3f);report.cruiseSpeed=Along;
   Check(report.initialSpeed>=0&&report.initialSpeed<.8f&&report.cruiseSpeed>2.0f,"progressive acceleration without speed snap");Check(report.cruiseSpeed<4.8f,"cruise speed within small-bay range");
   var coastStart=game.boat.position;game.DriveInput=Vector2.zero;yield return new WaitForSeconds(.8f);report.coastSpeed=Along;report.coastTravel=Vector3.Distance(coastStart,game.boat.position);
   Check(report.coastSpeed>.2f&&report.coastSpeed<report.cruiseSpeed,"release coasts with water resistance");Check(report.coastTravel>.2f,"release retains finite momentum");
   game.DriveInput=Vector2.down;yield return new WaitForSeconds(1.5f);report.brakedSpeed=Along;Check(report.brakedSpeed<.2f,"reverse input brakes forward movement");yield return new WaitForSeconds(2);report.reverseSpeed=Along;Check(report.reverseSpeed<-.3f&&report.reverseSpeed> -2.4f,"reverse builds at lower speed");
   ResetRoute(new Vector3(17,SeaLifeRuntime.Sea,1),0);game.DriveInput=Vector2.up;yield return new WaitForSeconds(2.5f);report.peakWakePoints=WakeCount;Check(WakeCount>4,"actual travel emits bounded wake history");
   var anchor=points.Count>0?points[0].p:Vector3.zero;float born=points.Count>0?points[0].born:-1;float yaw=game.boat.eulerAngles.y;
   game.DriveInput=new Vector2(.55f,1);yield return new WaitForSeconds(1.7f);report.turnDegrees=Mathf.DeltaAngle(yaw,game.boat.eulerAngles.y);var retained=points.Where(x=>Mathf.Abs(x.born-born)<.0001f).ToArray();report.wakeAnchorShift=retained.Length>0?Vector3.Distance(anchor,retained[0].p):999;
   Check(report.turnDegrees>8,"forward rudder turns actual body");Check(report.wakeAnchorShift<.0001f,"turn leaves old wake anchors in world");Check(Mathf.Abs(Mathf.DeltaAngle(0,visual.localEulerAngles.z))>.05f,"hull responds visually to waves and turning");yield return Shot("04-turning-wake");
   ResetRoute(new Vector3(17,SeaLifeRuntime.Sea,3),0);game.DriveInput=Vector2.down;yield return new WaitForSeconds(2.4f);yaw=game.boat.eulerAngles.y;game.DriveInput=new Vector2(.6f,-1);yield return new WaitForSeconds(1);report.reverseTurnDegrees=Mathf.DeltaAngle(yaw,game.boat.eulerAngles.y);Check(report.reverseTurnDegrees< -3,"reverse rudder changes direction");
   ResetRoute(new Vector3(20,SeaLifeRuntime.Sea,3),0);var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="temporary navigation check obstruction";obstacle.transform.position=new Vector3(20,SeaLifeRuntime.Sea+.4f,6.6f);obstacle.transform.localScale=new Vector3(4,1.5f,.5f);
   game.DriveInput=Vector2.up;yield return new WaitForSeconds(2);Check(game.boat.position.z<5,"solid obstruction blocks hull");float blockedZ=game.boat.position.z;game.DriveInput=Vector2.down;yield return new WaitForSeconds(2.8f);report.obstacleRetreat=blockedZ-game.boat.position.z;Check(report.obstacleRetreat>.5f,"reverse can retreat from contact");Destroy(obstacle);
   ResetRoute(new Vector3(17,SeaLifeRuntime.Sea,4),0);game.DriveInput=Vector2.up;yield return new WaitForSeconds(1.6f);float menuSpeed=game.Speed;game.Panel=2;yield return new WaitForSeconds(1);Check(game.Speed<menuSpeed,"menu blocks propulsion while resistance continues");game.Panel=0;game.DriveInput=Vector2.zero;yield return new WaitForSeconds(12);Check(WakeCount<=112,"wake bounded to fixed point budget");yield return new WaitForSeconds(WakeLifetime+1);Check(WakeCount==0,"stopped stern wake expires");
   ResetRoute(new Vector3(2,SeaLifeRuntime.Sea,7),90);Check(game.BeginFishing(),"prepare rod remains available");Check(game.Cast(new Vector3(3,SeaLifeRuntime.Sea,7)),"cast to water remains valid");yield return new WaitForSeconds(3.2f);Check(game.fishingStage==3,"bite state preserved");Check(game.StartReeling(),"strike enters reeling");yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();report.floatError=Mathf.Abs(game.WaterBobberPosition.y-Height(game.WaterBobberPosition)-.01f);Check(report.floatError<.002f,"float follows shared Gerstner surface");
   var pausedBobber=game.WaterBobberPosition;float pausedTime=WaterTime;game.Paused=true;Time.timeScale=0;for(int i=0;i<8;i++)yield return new WaitForEndOfFrame();Check(Vector3.Distance(pausedBobber,game.WaterBobberPosition)<.0001f&&WaterTime==pausedTime,"pause freezes waves and float without accumulating offsets");game.Paused=false;Time.timeScale=1;yield return Shot("05-fishing-water");game.CancelActivity();
   ResetRoute(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);yield return new WaitForSeconds(1);var times=new List<float>();for(int i=0;i<600;i++){yield return null;times.Add(Time.unscaledDeltaTime);}times.Sort();report.fps=1/times.Average();report.p95ms=times[(int)(times.Count*.95f)]*1000;report.sampleFrames=times.Count;yield return Shot("06-delivery");
   if(Environment.GetCommandLineArgs().Contains("-navigation-record"))yield return RecordMotion();
   game.DriveInput=Vector2.zero;game.SaveNow();var loaded=new SeaLifeStore(game.store.PathName).Load();Check(loaded.fish.Count==game.save.fish.Count,"isolated save roundtrip");report.passed=report.errors.Count==0;File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(report,true));Debug.Log("SEA_NAVIGATION_V23_REVIEW_DONE passed="+report.passed);Application.Quit(report.passed?0:2);
  }
  IEnumerator ReviewTimeout(){yield return new WaitForSecondsRealtime(240);report.errors.Add("review watchdog timeout");report.passed=false;File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(3);}
  IEnumerator RecordMotion(){string dir=Path.Combine(evidence,"motion");Directory.CreateDirectory(dir);ResetRoute(new Vector3(17,SeaLifeRuntime.Sea,1),0);SetBaseline(false);Time.captureFramerate=12;
   for(int i=0;i<144;i++){float t=i/12f;game.DriveInput=new Vector2(t>3&&t<7?.45f:0,t<8?1:t<10?-1:0);yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(dir,i.ToString("D4")+".png"),tex.EncodeToPNG());Destroy(tex);}Time.captureFramerate=0;game.DriveInput=Vector2.zero;
  }
 }
}
