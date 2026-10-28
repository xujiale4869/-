using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Deepsea {
 public sealed partial class SeaLifeWaterV22 {
  [Serializable] public class WaterReport {
   public bool passed;public string scope="Water-only v22: material A/B, actual controller route, bounded historical wake, floating hooks, fishing integration, fixed camera frame sample. No UI/ecology/grounding repair claim.";
   public string resolution,gpu,savePath;public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>();public float maxWave,boatTravel,yawChange,wakeTurnAnchorShift,reelingFloatError;public int peakWakePoints;public double baselineFPS,newFPS,newP95ms;public int sampleFrames;
  }
  WaterReport report=new WaterReport();string evidence;
  void RecordLog(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(s);if(type==LogType.Warning)report.warnings.Add(s);}
  void Check(bool value,string name){report.checks.Add(name+": "+value);if(!value)report.errors.Add("CHECK: "+name);}
  string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
  IEnumerator Shot(string name){for(int i=0;i<3;i++)yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();int dark=0,pink=0;foreach(var p in tex.GetPixels32()){if(p.r+p.g+p.b<25)dark++;if(p.r>245&&p.b>245&&p.g<10)pink++;}Check(dark<tex.width*tex.height*.85f,name+" visible");Check(pink<tex.width*tex.height*.001f,name+" no pink");File.WriteAllBytes(Path.Combine(evidence,name+".png"),tex.EncodeToPNG());Destroy(tex);}
  IEnumerator Sample(bool baseline){SetBaseline(baseline);yield return new WaitForSeconds(1);var times=new List<float>();for(int i=0;i<600;i++){yield return null;times.Add(Time.unscaledDeltaTime);}times.Sort();if(baseline)report.baselineFPS=1/times.Average();else{report.newFPS=1/times.Average();report.newP95ms=times[(int)(times.Count*.95f)]*1000;report.sampleFrames=times.Count;}}
  IEnumerator RunReview(){
   evidence=Arg("-water-review-dir","Verification/sea-water-v22-review01");Directory.CreateDirectory(evidence);StartCoroutine(ReviewTimeout());report.savePath=game.store.PathName;report.resolution=Screen.width+"x"+Screen.height;report.gpu=SystemInfo.graphicsDeviceName;
   game.AutoInput=true;game.DriveInput=Vector2.zero;game.PlaceBoat(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);game.SnapWaterCamera();yield return new WaitForSeconds(1);
   Check(FindObjectOfType<GameDirector>()==null,"independent life entry");Check(report.savePath.Contains(Application.temporaryCachePath),"review save is isolated");Check(newWater.GetFloat("_Refraction")==.0022f,"accepted refraction retained");Check(surface.GetComponent<MeshFilter>().sharedMesh.vertexCount>10000,"water mesh supports actual displacement");
   float maxDerivative=0;for(int i=0;i<2000;i++){var p=new Vector2(-20+(i%50)*1.4f,-20+(i/50)*1.5f);var f=Field(p,i*.017f);report.maxWave=Mathf.Max(report.maxWave,Mathf.Abs(f.x));float d=(Field(p+Vector2.right*.001f,i*.017f).x-Field(p-Vector2.right*.001f,i*.017f).x)/.002f;maxDerivative=Mathf.Max(maxDerivative,Mathf.Abs(d-f.y));}
   Check(report.maxWave<.0801f&&report.maxWave>.04f,"height field bounded and nonzero");Check(maxDerivative<.001f,"height derivatives agree with finite difference");
   SetBaseline(true);yield return Shot("01-v21-water");oldWater.SetFloat("_GlintEnable",0);yield return Shot("02-v21-without-extra-glint");oldWater.SetFloat("_GlintEnable",1);SetBaseline(false);yield return Shot("03-v22-water");yield return new WaitForSeconds(2);yield return Shot("04-v22-wave-later");
   game.PlaceBoat(new Vector3(14,SeaLifeRuntime.Sea,4),0);game.SnapWaterCamera();var start=game.boat.position;var yaw=game.boat.eulerAngles.y;game.DriveInput=new Vector2(0,1);yield return new WaitForSeconds(2.7f);report.boatTravel=Vector3.Distance(start,game.boat.position);Check(report.boatTravel>1,"actual rigidbody propulsion preserved");
   report.peakWakePoints=WakeCount;Check(WakeCount>3,"moving boat emits historical points");int countBefore=WakeCount;var anchor=points.Count>0?points[0].p:Vector3.zero;float born=points.Count>0?points[0].born:-1;
   game.DriveInput=new Vector2(.55f,1);yield return new WaitForSeconds(2);report.yawChange=Mathf.Abs(Mathf.DeltaAngle(yaw,game.boat.eulerAngles.y));Check(report.yawChange>5,"actual rigidbody steering preserved");
   var retained=points.Where(x=>Mathf.Abs(x.born-born)<.0001f).ToArray();report.wakeTurnAnchorShift=retained.Length>0?Vector3.Distance(anchor,retained[0].p):0;Check(retained.Length>0&&report.wakeTurnAnchorShift<.0001f,"old wake anchor stays in world while turning");report.peakWakePoints=Mathf.Max(report.peakWakePoints,WakeCount);yield return Shot("05-turning-wake");
   game.DriveInput=Vector2.zero;yield return new WaitForSeconds(8);Check(EmissionSpeed<.28f,"coasting slows below emission threshold");Check(WakeCount<=112,"history stays within fixed budget");yield return new WaitForSeconds(WakeLifetime+1);Check(WakeCount==0,"idle stern wake ages out");yield return Shot("06-idle-no-stern-wake");
   game.PlaceBoat(new Vector3(2,SeaLifeRuntime.Sea,7),90);game.SnapWaterCamera();Check(game.BeginFishing(),"existing prepare rod");Check(game.Cast(new Vector3(3,SeaLifeRuntime.Sea,7)),"cast remains valid on water");yield return new WaitForSeconds(3.2f);Check(game.fishingStage==3,"bite state preserved");float expected=Height(game.WaterBobberPosition)-.14f+Mathf.Sin(Time.time*18)*.025f;
   yield return new WaitForEndOfFrame();Check(game.WaterBobberPosition.y>Height(game.WaterBobberPosition)-.18f&&game.WaterBobberPosition.y<Height(game.WaterBobberPosition)-.09f,"biting float follows displaced water");Check(game.StartReeling(),"strike still starts reeling");yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();report.reelingFloatError=Mathf.Abs(game.WaterBobberPosition.y-Height(game.WaterBobberPosition)-.01f);Check(report.reelingFloatError<.002f,"reeling float shares water height");
   var pausedBobber=game.WaterBobberPosition;game.Paused=true;Time.timeScale=0;for(int i=0;i<8;i++)yield return new WaitForEndOfFrame();Check(Vector3.Distance(pausedBobber,game.WaterBobberPosition)<.0001f,"pausing does not accumulate surface offsets");game.Paused=false;Time.timeScale=1;yield return Shot("07-fishing-water");game.CancelActivity();
   int leafSamples=0;float maxJump=0;if(oldLeaves!=null&&oldLeaves.targets!=null&&oldLeaves.targets.Length>0)for(int i=0;i<30;i++)for(int j=0;j<80;j++){var s=oldLeaves.Evaluate(i,j*.31f);if(s.floating){var p=s.p;p.y+=Field(new Vector2(p.x,p.z),j*.31f).x;CheckFinite(p);leafSamples++;}float fall=9+(i%5)*.65f;var a=oldLeaves.Evaluate(i,fall-i*1.618034f-.001f);var b=oldLeaves.Evaluate(i,fall-i*1.618034f+.001f);maxJump=Mathf.Max(maxJump,Vector3.Distance(a.p,b.p));}
   Check(leafSamples>100&&maxJump<.02f,"existing leaf contact continuity retained with shared wave offset");
   game.PlaceBoat(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);game.SnapWaterCamera();yield return Sample(true);yield return Sample(false);yield return Shot("08-delivery");
   if(Environment.GetCommandLineArgs().Contains("-water-record"))yield return RecordMotion();
   game.DriveInput=Vector2.zero;game.SaveNow();var loaded=new SeaLifeStore(game.store.PathName).Load();Check(loaded.fish.Count==game.save.fish.Count,"isolated save roundtrip preserved");report.passed=report.errors.Count==0;File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(report,true));Debug.Log("SEA_WATER_V22_REVIEW_DONE passed="+report.passed);Application.Quit(report.passed?0:2);
  }
  void CheckFinite(Vector3 p){if(float.IsNaN(p.y)||float.IsInfinity(p.y))report.errors.Add("invalid floating leaf");}
  IEnumerator ReviewTimeout(){yield return new WaitForSecondsRealtime(240);report.errors.Add("review watchdog timeout");report.passed=false;File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(3);}
  IEnumerator RecordMotion(){
   string dir=Path.Combine(evidence,"motion");Directory.CreateDirectory(dir);game.PlaceBoat(new Vector3(14,SeaLifeRuntime.Sea,4),0);game.SnapWaterCamera();SetBaseline(false);Time.captureFramerate=12;
   for(int i=0;i<144;i++){float t=i/12f;game.DriveInput=new Vector2(t>3&&t<7?.45f:0,t<8?1:0);yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(dir,i.ToString("D4")+".png"),tex.EncodeToPNG());Destroy(tex);}
   Time.captureFramerate=0;game.DriveInput=Vector2.zero;
  }
 }
}
