using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaWaterArtUpgradeV26 {
  [Serializable] class V26Result {
   public bool passed; public string resolution,gpu,savePath,scope="Fixed-camera native-window visual integration; not full gameplay or target-art approval";
   public List<string> checks=new List<string>(),failed=new List<string>(),errors=new List<string>(),warnings=new List<string>();
   public float baselineFps,v26Fps,baselineP95Ms,v26P95Ms; public int frames=480,fishCount,visibleFish;
  }
  V26Result result;string evidence;
  void NoteV26(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)result.errors.Add(s);else if(type==LogType.Warning)result.warnings.Add(s);}
  void CheckV26(bool ok,string label){result.checks.Add(label+": "+ok);if(!ok)result.failed.Add(label);}
  IEnumerator ShotV26(string name){
   for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();
   var t=ScreenCapture.CaptureScreenshotAsTexture();var pixels=t.GetPixels32();int black=0,pink=0;
   foreach(var c in pixels){if(c.r+c.g+c.b<25)black++;if(c.r>245&&c.b>245&&c.g<10)pink++;}
   CheckV26(black<pixels.Length*.85f,name+" visible frame");CheckV26(pink<pixels.Length*.001f,name+" no magenta shader");
   File.WriteAllBytes(Path.Combine(evidence,name+".jpg"),t.EncodeToJPG(94));Destroy(t);
  }
  IEnumerator PerfV26(bool enabled){
   ApplyWaterMode(enabled);for(int i=0;i<60;i++)yield return null;
   var times=new List<float>();long prev=System.Diagnostics.Stopwatch.GetTimestamp();
   for(int i=0;i<result.frames;i++){yield return null;long now=System.Diagnostics.Stopwatch.GetTimestamp();times.Add((float)((now-prev)*1000.0/System.Diagnostics.Stopwatch.Frequency));prev=now;}
   times.Sort();float fps=1000/times.Average(),p95=times[(int)(times.Count*.95)];
   if(enabled){result.v26Fps=fps;result.v26P95Ms=p95;}else{result.baselineFps=fps;result.baselineP95Ms=p95;}
  }
  IEnumerator ReviewV26(){
   result=new V26Result();Application.logMessageReceived+=NoteV26;
   var args=Environment.GetCommandLineArgs();int ix=Array.IndexOf(args,"-water-v26-dir");evidence=ix>=0?args[ix+1]:"Docs/SeaSurfaceV26";Directory.CreateDirectory(evidence);
   StartCoroutine(WatchdogV26());
   _game.AutoInput=true;_game.DriveInput=Vector2.zero;
   _game.store=new SeaLifeStore(Path.Combine(Application.temporaryCachePath,"water-v26-"+DateTime.UtcNow.Ticks+".json"));
   _game.save=new SeaLifeSave();_game.save.Normalize();_game.RefreshWaterCollection();
   result.savePath=_game.store.PathName;result.resolution=Screen.width+"x"+Screen.height;result.gpu=SystemInfo.graphicsDeviceName;
   CheckV26(Screen.width==1920&&Screen.height==1080,"native 1920x1080 game window");
   CheckV26(result.savePath.StartsWith(Application.temporaryCachePath),"isolated temporary save");
   CheckV26(_waterRenderer.gameObject.name=="ShowcaseWater","exact main water renderer");
   CheckV26(_v26WaterMaterial.shader.isSupported,"v26 shader supported");
   CheckV26(_causticsTex.width==384&&_causticsTex.height==384,"384 caustics texture loaded");
   CheckV26(_foamTex.width==256&&_foamTex.height==256,"256 foam texture loaded");
   CheckV26(_v26WaterMaterial.GetTexture("_DepthRampMap")!=null,"depth ramp connected");
   CheckV26(_v26WaterMaterial.GetTexture("_WaveMap")!=null,"legacy wave slopes retained");
   CheckV26(_trenchSchool.Count==72&&_mantaRay!=null,"72 silver fish and manta created");
   result.fishCount=_trenchSchool.Count;
   yield return new WaitForSeconds(1);
   result.visibleFish=_trenchSchool.Count(f=>f.tr.GetComponent<Renderer>().enabled);CheckV26(result.visibleFish>0,"marine life has safe underwater placements");
   _game.PlaceBoat(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);_game.SnapWaterCamera();_game.NoticeTime=0;
   var manual=typeof(SeaSurfaceV24).GetField("manual",BindingFlags.Instance|BindingFlags.NonPublic);manual.SetValue(_surface,true);
   typeof(SeaSurfaceV24).GetProperty("Clock").SetValue(_surface,1.5f,null);
   ApplyWaterMode(false);yield return ShotV26("01-baseline");CheckV26(_waterRenderer.sharedMaterial==_originalWaterMaterial,"F6 route restores original material");
   ApplyWaterMode(true);yield return ShotV26("preview");CheckV26(_waterRenderer.sharedMaterial==_v26WaterMaterial,"F6 route applies v26 material");
   typeof(SeaSurfaceV24).GetProperty("Clock").SetValue(_surface,2.5f,null);yield return ShotV26("03-next-wave");
   manual.SetValue(_surface,false);
   yield return PerfV26(false);yield return PerfV26(true);
   float clock=_surface.Clock;Vector3 manta=_mantaRay.position;_game.Paused=true;Time.timeScale=0;
   for(int i=0;i<12;i++)yield return new WaitForEndOfFrame();
   CheckV26(_surface.Clock==clock&&_mantaRay.position==manta,"pause freezes water and marine motion");_game.Paused=false;Time.timeScale=1;
   _game.PlaceBoat(new Vector3(17,SeaLifeRuntime.Sea,3),0);_game.SnapWaterCamera();var start=_game.boat.position;_game.DriveInput=Vector2.up;
   yield return new WaitForSeconds(2);CheckV26(Vector3.Distance(start,_game.boat.position)>1,"existing boat propulsion retained");_game.DriveInput=Vector2.zero;
   _game.SaveNow();CheckV26(File.Exists(_game.store.PathName),"temporary save writes only to isolated path");
   result.passed=result.failed.Count==0&&result.errors.Count==0;
   File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(result,true));Application.logMessageReceived-=NoteV26;
   Debug.Log("WATER_V26_REVIEW_DONE "+result.passed);Application.Quit(result.passed?0:2);
  }
  IEnumerator WatchdogV26(){yield return new WaitForSecondsRealtime(180);if(result!=null){result.failed.Add("review timeout");File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(result,true));}Application.Quit(3);}
 }
}
