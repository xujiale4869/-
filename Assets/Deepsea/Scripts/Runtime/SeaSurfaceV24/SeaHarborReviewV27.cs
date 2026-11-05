using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaHarborAndReefUpgradeV27 {
  [Serializable] class HarborResult {
   public bool passed;public string resolution,gpu,savePath,scope="Native 1080p integration and bounded fixed-camera performance; visual captures are not user art approval.";
   public List<string> checks=new List<string>(),failed=new List<string>(),errors=new List<string>(),warnings=new List<string>();
   public int changedRenderers,importedMeshUses,reefClusters,kelpClusters,addedColliders,frames=480;
   public float baselineFps,v27Fps,baselineP95Ms,v27P95Ms,walkDistance,boatDistance;
  }
  HarborResult result;string evidence;
  void Check(bool pass,string label){result.checks.Add(label+": "+pass);if(!pass)result.failed.Add(label);}
  void ReviewLog(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)result.errors.Add(s);else if(type==LogType.Warning)result.warnings.Add(s);}
  IEnumerator Shot(string name){for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();var colors=tex.GetPixels32();int black=0,pink=0;foreach(var c in colors){if(c.r+c.g+c.b<25)black++;if(c.r>245&&c.b>245&&c.g<10)pink++;}Check(black<colors.Length*.85f,name+" visible frame");Check(pink<colors.Length*.001f,name+" no magenta shader");File.WriteAllBytes(Path.Combine(evidence,name+".jpg"),tex.EncodeToJPG(94));Destroy(tex);}
  void CameraView(Vector3 target,float size,float pitch=42,float yaw=-18){var q=Quaternion.Euler(pitch,yaw,0);game.view.fieldOfView=2*Mathf.Atan(size/104)*Mathf.Rad2Deg;game.view.transform.SetPositionAndRotation(target-q*Vector3.forward*104,q);}
  IEnumerator Perf(bool on){ApplyAtmosphereState(on);for(int i=0;i<60;i++)yield return null;var times=new List<float>();long prev=System.Diagnostics.Stopwatch.GetTimestamp();for(int i=0;i<result.frames;i++){yield return null;long now=System.Diagnostics.Stopwatch.GetTimestamp();times.Add((float)((now-prev)*1000.0/System.Diagnostics.Stopwatch.Frequency));prev=now;}times.Sort();if(on){result.v27Fps=1000/times.Average();result.v27P95Ms=times[(int)(times.Count*.95)];}else{result.baselineFps=1000/times.Average();result.baselineP95Ms=times[(int)(times.Count*.95)];}}
  IEnumerator ReviewHarbor(){
   result=new HarborResult();Application.logMessageReceived+=ReviewLog;StartCoroutine(Deadline());var args=Environment.GetCommandLineArgs();int ix=Array.IndexOf(args,"-harbor-v27-dir");evidence=ix>=0?args[ix+1]:"Verification/harbor-v27-review01";Directory.CreateDirectory(evidence);
   game.AutoInput=true;game.DriveInput=Vector2.zero;game.store=new SeaLifeStore(Path.Combine(Application.temporaryCachePath,"harbor-v27-"+DateTime.UtcNow.Ticks+".json"));game.save=new SeaLifeSave();game.save.Normalize();game.RefreshWaterCollection();
   result.savePath=game.store.PathName;result.resolution=Screen.width+"x"+Screen.height;result.gpu=SystemInfo.graphicsDeviceName;
   yield return new WaitForSeconds(1);
   if(args.Contains("-harbor-v27-native")){yield return NativeReview();yield break;}
   Check(Screen.width==1920&&Screen.height==1080,"native 1920x1080 game window");Check(result.savePath.StartsWith(Application.temporaryCachePath),"isolated temporary player save");
   result.changedRenderers=ChangedRenderers;result.importedMeshUses=loadedMeshes;result.addedColliders=worldRoot.GetComponentsInChildren<Collider>(true).Length+boatRoot.GetComponentsInChildren<Collider>(true).Length;
   result.reefClusters=worldRoot.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("V27_ShowcaseCoralCluster"));result.kelpClusters=worldRoot.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("V27_ShowcaseGiantKelp"));
   Check(ChangedRenderers>0,"existing reef renderers use v27 palette");Check(loadedMeshes==5,"five asset uses from four Blender models, no primitive model fallback");Check(result.addedColliders==0,"decorative layer adds no colliders");Check(result.reefClusters>=4&&result.kelpClusters>=4,"near-bay new reef and kelp rooted on visible seabed");
   Check(boatRoot.parent.name=="船身随波 · 视觉层","stern lamp and cargo inherit wave-driven hull transform");Check(boatLight.shadows==UnityEngine.LightShadows.None&&pierLight.shadows==UnityEngine.LightShadows.None,"two bounded point lights add no shadow maps");Check(corals.All(m=>m.enableInstancing),"reef materials retain instancing support");Check(water.shader.isSupported&&kelp.shader.isSupported&&wood.shader.isSupported,"all v27 shaders supported");
   Check(Mathf.Abs(water.GetFloat("_CausticsIntensity")-.92f)<.00001f&&Mathf.Abs(water.GetFloat("_GlintIntensity")-1.85f)<.00001f&&Mathf.Abs(water.GetFloat("_GlintFocus")-72)<.00001f,"v26.2 water optical defaults retained");
   ApplyAtmosphereState(false);Check(OriginalsRestored(),"F8 restores every replaced material slot and legacy lamp mesh");ApplyAtmosphereState(true);
   int assetCount=owned.Count,objectCount=worldRoot.GetComponentsInChildren<Transform>(true).Length;
   for(int i=0;i<4;i++){ApplyAtmosphereState(false);Check(water.shader==originalWaterShader&&!worldRoot.gameObject.activeSelf&&!boatRoot.gameObject.activeSelf,"F8 off restores water shader and hides new geometry "+i);ApplyAtmosphereState(true);}
   Check(assetCount==owned.Count&&objectCount==worldRoot.GetComponentsInChildren<Transform>(true).Length,"repeated comparison reuses assets and objects");
   game.PlaceBoat(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);game.SnapWaterCamera();game.NoticeTime=0;game.ui.Refresh();
   game.Paused=true;Time.timeScale=0;yield return Shot("preview");
   ApplyAtmosphereState(false);yield return Shot("01-baseline");ApplyAtmosphereState(true);
   game.enabled=false;var canvases=FindObjectsOfType<Canvas>();foreach(var c in canvases)c.enabled=false;
   CameraView(new Vector3(-3.3f,.6f,2.2f),8.5f);yield return Shot("02-harbor");
   CameraView(game.boat.position+Vector3.up*.55f,3.1f,32,-35);yield return Shot("03-stern-lantern");
   CameraView(new Vector3(3,-.6f,-2),5.2f,57,-18);yield return Shot("04-reef");
   float frozenClock=clock;Quaternion frozenBoat=boatPivot.localRotation;float light=boatLight.intensity;
   for(int i=0;i<12;i++)yield return new WaitForEndOfFrame();Check(clock==frozenClock&&boatPivot.localRotation==frozenBoat&&boatLight.intensity==light,"pause freezes lamp, reef clock and light flicker");
   CameraView(new Vector3(-1.5f,.6f,2.3f),12.5f);game.Paused=false;Time.timeScale=1;
   yield return Perf(false);yield return Perf(true);foreach(var c in canvases)c.enabled=true;game.enabled=true;
   game.PlaceBoat(new Vector3(17,SeaLifeRuntime.Sea,3),0);Vector3 start=game.boat.position;game.DriveInput=Vector2.up;yield return new WaitForSeconds(2);game.DriveInput=Vector2.zero;result.boatDistance=Vector3.Distance(start,game.boat.position);Check(result.boatDistance>1,"existing propulsion moves boat with v27 details attached");
   game.PlaceBoat(game.dockBerth,90);game.ToggleBoard();Check(game.OnFoot&&game.controller.enabled,"existing disembark route retained");start=game.walker.position;game.DriveInput=new Vector2(-.95106f,.30902f);yield return new WaitForSeconds(2);game.DriveInput=Vector2.zero;result.walkDistance=Vector3.Distance(start,game.walker.position);Check(result.walkDistance>3&&game.walker.position.y>-.2f,"actual controller traverses pier centre past cargo");
   game.PlaceWalker(game.Grounded(game.dockFeet));game.ToggleBoard();Check(!game.OnFoot&&game.captain.gameObject.activeSelf,"existing reboard route retained");
   game.SaveNow();Check(File.Exists(game.store.PathName),"test writes temporary save only");
   result.passed=result.failed.Count==0&&result.errors.Count==0;File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(result,true));Application.logMessageReceived-=ReviewLog;Debug.Log("HARBOR_V27_REVIEW_DONE "+result.passed);Application.Quit(result.passed?0:2);
  }
  bool OriginalsRestored(){return originalMaterials.All(p=>p.Key!=null&&p.Key.sharedMaterials.SequenceEqual(p.Value))&&originalMeshes.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value);}
  void NativePhase(string phase){File.WriteAllText(Path.Combine(evidence,"native-state.json"),"{\"phase\":\""+phase+"\"}");}
  IEnumerator NativeReview(){
   game.PlaceBoat(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);game.SnapWaterCamera();game.NoticeTime=0;game.ui.Refresh();game.Paused=true;Time.timeScale=0;
   NativePhase("await-off");float until=Time.unscaledTime+35;while(v27AtmosphereEnabled&&Time.unscaledTime<until)yield return null;
   Check(!v27AtmosphereEnabled,"Windows F8 disables v27");Check(OriginalsRestored(),"Windows F8 restores every material and legacy lamp mesh");Check(water.shader==originalWaterShader&&!worldRoot.gameObject.activeSelf&&!boatRoot.gameObject.activeSelf,"Windows F8 hides decorations and restores water shader");yield return Shot("native-off");
   NativePhase("await-on");until=Time.unscaledTime+35;while(!v27AtmosphereEnabled&&Time.unscaledTime<until)yield return null;
   Check(v27AtmosphereEnabled&&worldRoot.gameObject.activeSelf&&boatRoot.gameObject.activeSelf,"Windows F8 restores v27 layer");Check(water.shader==harborWaterShader,"Windows F8 restores harbor water reflection");yield return Shot("native-on");
   result.passed=result.failed.Count==0&&result.errors.Count==0;File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(result,true));NativePhase("done");Application.logMessageReceived-=ReviewLog;Debug.Log("HARBOR_V27_NATIVE_DONE "+result.passed);Application.Quit(result.passed?0:2);
  }
  IEnumerator Deadline(){yield return new WaitForSecondsRealtime(180);if(result!=null){result.failed.Add("review timeout");File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(result,true));}Application.Quit(3);}
 }
}
