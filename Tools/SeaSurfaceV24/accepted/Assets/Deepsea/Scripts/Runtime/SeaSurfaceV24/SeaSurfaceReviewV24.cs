using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea {
 public sealed partial class SeaSurfaceV24 {
  [Serializable]public class PerfCase {
   public string name,window,renderTarget,edgeAA,colorFormat;public int frames,targetFPS,vsync,msaa;public float renderScale;public bool reducedUnderwater,ambientSinglePass;
   public double fps,meanMs,medianMs,p95Ms,gpuMs,cpuFrameMs,mainThreadMs,renderThreadMs,drawCalls,triangles;
   public int gpuSamples,cpuSamples;public bool focused;
  }
  [Serializable]public class OceanReport {
   public bool passed;public string gpu,window,renderMode,savePath;public bool timingEnabled;public int cameras;
   public List<string> checks=new List<string>(),errors=new List<string>(),warnings=new List<string>();
   public List<PerfCase> cases=new List<PerfCase>();public float maxWave,derivativeError,boatTravel,bobberError;public int budgetMaterialCount;
  }
  OceanReport report=new OceanReport();string evidence;readonly FrameTiming[] timingBuffer=new FrameTiming[1];
  string Arg(string key,string fallback){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0&&i+1<a.Length?a[i+1]:fallback;}
  void Check(bool ok,string label){report.checks.Add(label+": "+ok);if(!ok)errors.Add("CHECK "+label);}
  IEnumerator Shot(string name){for(int i=0;i<3;i++)yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();var pixels=tex.GetPixels32();int black=0,pink=0;foreach(var c in pixels){if(c.r+c.g+c.b<25)black++;if(c.r>245&&c.b>245&&c.g<10)pink++;}Check(black<pixels.Length*.85f,name+" visible");Check(pink<pixels.Length*.001f,name+" no missing shader");File.WriteAllBytes(Path.Combine(evidence,name+".jpg"),tex.EncodeToJPG(94));Destroy(tex);}
  IEnumerator Sample(string name){
   for(int i=0;i<80;i++){FrameTimingManager.CaptureFrameTimings();yield return null;}
   int count=480;var times=new List<double>(count);var gpu=new List<double>(count);var cpu=new List<double>(count);var mains=new List<double>(count);var renders=new List<double>(count);double draws=0,tris=0;
   var draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");var tri=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
   var mainField=typeof(FrameTiming).GetField("cpuMainThreadFrameTime");var renderField=typeof(FrameTiming).GetField("cpuRenderThreadFrameTime");
   long before=System.Diagnostics.Stopwatch.GetTimestamp();
   for(int i=0;i<count;i++){FrameTimingManager.CaptureFrameTimings();yield return null;long now=System.Diagnostics.Stopwatch.GetTimestamp();times.Add((now-before)*1000.0/System.Diagnostics.Stopwatch.Frequency);before=now;
    if(FrameTimingManager.GetLatestTimings(1,timingBuffer)>0){var f=timingBuffer[0];if(f.gpuFrameTime>0)gpu.Add(f.gpuFrameTime);if(f.cpuFrameTime>0)cpu.Add(f.cpuFrameTime);if(mainField!=null)mains.Add(Convert.ToDouble(mainField.GetValue(f)));if(renderField!=null)renders.Add(Convert.ToDouble(renderField.GetValue(f)));}
    if(draw.Valid)draws+=draw.LastValue;if(tri.Valid)tris+=tri.LastValue;
   }
   draw.Dispose();tri.Dispose();times.Sort();var pipe=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
   var c=new PerfCase{name=name,frames=count,targetFPS=Application.targetFrameRate,vsync=QualitySettings.vSyncCount,window=Screen.width+"x"+Screen.height,renderTarget=game.view.pixelWidth+"x"+game.view.pixelHeight,edgeAA=game.view.GetUniversalAdditionalCameraData().antialiasing.ToString(),colorFormat=SeaFrameDescriptorV24.Latest.graphicsFormat.ToString(),msaa=pipe.msaaSampleCount,renderScale=pipe.renderScale,focused=Application.isFocused,reducedUnderwater=depthBudget,ambientSinglePass=ambientBudget,
    meanMs=times.Average(),medianMs=times[count/2],p95Ms=times[(int)(count*.95)],fps=1000/times.Average(),gpuMs=gpu.Count>0?gpu.Average():0,cpuFrameMs=cpu.Count>0?cpu.Average():0,mainThreadMs=mains.Count>0?mains.Average():0,renderThreadMs=renders.Count>0?renders.Average():0,gpuSamples=gpu.Count,cpuSamples=cpu.Count,drawCalls=draws/count,triangles=tris/count};
   report.cases.Add(c);File.WriteAllText(Path.Combine(evidence,"progress.json"),JsonUtility.ToJson(report,true));Debug.Log("OCEAN_SAMPLE "+name+" FPS="+c.fps+" GPUms="+c.gpuMs);
  }
  IEnumerator Review(){
   evidence=Arg("-ocean-review-dir","Verification/sea-surface-v24-draft01");Directory.CreateDirectory(evidence);StartCoroutine(Timeout());
   report.gpu=SystemInfo.graphicsDeviceName;report.window=Screen.width+"x"+Screen.height;report.savePath=game.store.PathName;report.timingEnabled=FrameTimingManager.IsFeatureEnabled();report.cameras=FindObjectsOfType<Camera>().Count(c=>c.enabled&&c.gameObject.activeInHierarchy);
   game.AutoInput=true;game.DriveInput=Vector2.zero;game.PlaceBoat(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);game.SnapWaterCamera();
   Check(Application.targetFrameRate== -1&&QualitySettings.vSyncCount==0,"unlimited frame rate and vsync disabled");Check(report.savePath.Contains(Application.temporaryCachePath),"isolated review save");
   Check(FindObjectOfType<GameDirector>()==null,"independent life runtime");Check(water.sharedMaterial.shader.isSupported,"water shader supported");report.budgetMaterialCount=budgetMaterials.Count;Check(budgetMaterials.Count>0,"submerged material budget installed");Check(ambientSettings.Count>0,"contact filter configuration found");
   for(int i=0;i<1200;i++){var p=new Vector2(-15+i%40,-15+i/40);float t=i*.013f;var f=OceanWaveV24.Field(p,t);report.maxWave=Mathf.Max(report.maxWave,Mathf.Abs(f.x));float dx=(OceanWaveV24.Field(p+Vector2.right*.001f,t).x-OceanWaveV24.Field(p-Vector2.right*.001f,t).x)/.002f;report.derivativeError=Mathf.Max(report.derivativeError,Mathf.Abs(dx-f.y));}
   Check(report.maxWave>.2f&&report.maxWave<.2901f,"rolling wave field bounded");Check(report.derivativeError<.003f,"world query derivatives agree");
   bool native4k=Environment.GetCommandLineArgs().Contains("-ocean-native-4k");bool render4k=native4k||Environment.GetCommandLineArgs().Contains("-ocean-render-4k");
   if(native4k){Screen.SetResolution(3840,2160,FullScreenMode.Windowed);yield return new WaitForSeconds(1);native4k=Screen.width==3840&&Screen.height==2160;report.window=Screen.width+"x"+Screen.height;}
   if(!render4k){
    manual=true;Clock=1;SetMode(false,true);yield return Shot("01-v23-water-same-pipeline");SetMode(true,true);yield return Shot("02-v24-layered-water");Clock=1.8f;yield return Shot("03-v24-next-wave");
    Shader.SetGlobalFloat("_OceanV24Layer",1);yield return Shot("04-surface-only");Shader.SetGlobalFloat("_OceanV24Layer",2);yield return Shot("05-transmission-mask");Shader.SetGlobalFloat("_OceanV24Layer",0);SetDepthBudget(false);yield return Shot("07-full-underwater-detail");SetDepthBudget(true);yield return Shot("08-budget-underwater-detail");manual=false;
   }else if(!native4k){
    for(int i=0;i<8;i++)yield return null;
    Check(SeaFrameDescriptorV24.HasNative,"actual native HDR descriptor captured");
    var desc=SeaFrameDescriptorV24.Native;desc.width=3840;desc.height=2160;desc.depthBufferBits=24;desc.msaaSamples=1;desc.useDynamicScale=false;
    fourK=new RenderTexture(desc){name="4K matching native HDR format"};fourK.Create();game.view.targetTexture=fourK;
   }
   report.renderMode=native4k?"3840x2160 actual game window, scene and UI; renderScale=1. Window may exceed physical monitor area.":render4k?"3840x2160 offscreen scene matching native HDR format, plus native-window presentation/UI; renderScale=1. Not native 4K desktop presentation.":"Native window scene and UI; renderScale=1.";
   if(Environment.GetCommandLineArgs().Contains("-ocean-capture-only")){manual=false;SetMode(true,true);yield return Record();report.errors=errors;report.warnings=warnings;report.passed=errors.Count==0;File.WriteAllText(Path.Combine(evidence,"capture-result.json"),JsonUtility.ToJson(report,true));Application.Quit(report.passed?0:2);yield break;}
   yield return new WaitForSeconds(1);
   SetMode(false,false);yield return Sample("v23-original-4x");SetMode(true,false);yield return Sample("v24-water-original-4x");SetMode(true,true);SetAmbientBudget(false);SetDepthBudget(false);ApplyPipeline(msaa2Pipeline,AntialiasingMode.None);yield return Sample("v24-water-2x-half-refraction");SetMode(true,true);SetAmbientBudget(false);SetDepthBudget(false);yield return Sample("v24-water-FXAA-half-refraction");SetDepthBudget(true);yield return Sample("v24-depth-budget");SetAmbientBudget(true);yield return Sample("v24-final-contact-filter");
   if(!render4k){
    water.enabled=false;yield return Sample("diagnostic-no-water");water.enabled=true;
    foreach(var f in rendererData.rendererFeatures)f.SetActive(false);yield return Sample("diagnostic-no-SSAO");RestoreFeatures();
    float shadows=fastPipeline.shadowDistance;fastPipeline.shadowDistance=0;yield return Sample("diagnostic-no-shadows");fastPipeline.shadowDistance=shadows;
    game.view.GetUniversalAdditionalCameraData().renderPostProcessing=false;yield return Sample("diagnostic-no-postprocessing");game.view.GetUniversalAdditionalCameraData().renderPostProcessing=true;
    SetMode(false,false);yield return Sample("v23-original-4x-repeat");SetMode(true,true);yield return Sample("v24-optimized-repeat");
    game.PlaceBoat(new Vector3(17,SeaLifeRuntime.Sea,3),0);game.SnapWaterCamera();var start=game.boat.position;game.DriveInput=Vector2.up;yield return new WaitForSeconds(2.5f);report.boatTravel=Vector3.Distance(start,game.boat.position);Check(report.boatTravel>1,"existing actual rigidbody propulsion");game.DriveInput=new Vector2(.5f,1);yield return new WaitForSeconds(1.2f);Check(navigation.WakeCount>0&&navigation.WakeCount<=112,"historical wake retained");yield return Shot("06-rolling-turn");
    game.DriveInput=Vector2.zero;game.PlaceBoat(new Vector3(2,SeaLifeRuntime.Sea,7),90);Check(game.BeginFishing(),"prepare rod retained");Check(game.Cast(new Vector3(3,SeaLifeRuntime.Sea,7)),"cast retained");yield return new WaitForSeconds(3.2f);Check(game.StartReeling(),"bite and strike retained");yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();report.bobberError=Mathf.Abs(game.WaterBobberPosition.y-navigation.Height(game.WaterBobberPosition)-.01f);Check(report.bobberError<.002f,"bobber matches new rolling surface");
    float frozen=Clock;game.Paused=true;Time.timeScale=0;for(int i=0;i<8;i++)yield return new WaitForEndOfFrame();Check(Clock==frozen,"pause freezes water");game.Paused=false;Time.timeScale=1;game.CancelActivity();
    if(Environment.GetCommandLineArgs().Contains("-ocean-record"))yield return Record();
   }
   game.DriveInput=Vector2.zero;game.SaveNow();Check(new SeaLifeStore(game.store.PathName).Load().fish.Count==game.save.fish.Count,"isolated save readback");
   report.errors=errors;report.warnings=warnings;report.passed=errors.Count==0;File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(report,true));Debug.Log("OCEAN_V24_DONE "+report.passed);Application.Quit(report.passed?0:2);
  }
  IEnumerator Record(){string dir=Path.Combine(evidence,"motion");Directory.CreateDirectory(dir);bool oldFPS=showFPS;showFPS=false;game.NoticeTime=0;game.PlaceBoat(new Vector3(14,SeaLifeRuntime.Sea,4),0);game.SnapWaterCamera();game.DriveInput=Vector2.zero;Time.captureFramerate=30;for(int i=0;i<240;i++){if(i==120)game.DriveInput=Vector2.up;yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(dir,i.ToString("D4")+".jpg"),tex.EncodeToJPG(90));Destroy(tex);}Time.captureFramerate=0;game.DriveInput=Vector2.zero;showFPS=oldFPS;}
  IEnumerator Timeout(){yield return new WaitForSecondsRealtime(300);errors.Add("review watchdog");report.errors=errors;File.WriteAllText(Path.Combine(evidence,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(3);}
 }
}
