using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Unity.Profiling;
namespace Deepsea {
public sealed partial class CoastSlice {
 static bool Perf{get{return Arg("-coast-perf");}}
 [Serializable]class PerfReport{public string gpu,resolution,notes;public int rocks,corals,ferns,trees;public bool passed;public string failure;public List<PerfSample> samples=new List<PerfSample>();public List<string> meshes=new List<string>();}
 [Serializable]class PerfSample{public string mode,context;public int frames;public double meanMs,p50Ms,p95Ms,p99Ms,fps,triangles,drawCalls,setPass,gcBytes,mainThreadMs,renderThreadMs;public bool renderCounters,cpuCounters,frameTimings;public double gpuMs,cpuActiveMs,cpuWaitMs;}
 IEnumerator RunPerformance(){
  string output=Arg("-perf-cpu")?"Verification/coast-perf-cpu":Arg("-perf-detail")?"Verification/coast-perf-detail":"Verification/coast-perf";Directory.CreateDirectory(output);
  game.Boat.AutomationInput=true;game.Boat.AutomationThrottle=0;game.Boat.AutomationRudder=0;game.Expedition.Explorer.AutomationInput=true;game.Expedition.Explorer.AutomationDirection=Vector3.zero;
  Screen.SetResolution(1920,1080,FullScreenMode.Windowed);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
  game.CameraFollow.enabled=false;View(new Vector3(58,0,98),18);
  var report=new PerfReport{rocks=rocks,corals=corals,ferns=ferns,trees=trees.Count,gpu=SystemInfo.graphicsDeviceName,resolution="1920x1080",notes="Release player. UI active. No screenshot/file IO during timed frames. Default:120 warm-up +360 measured; acceptance:360 warm-up +1200 measured frames per case. Diagnostic cases restore original state."};
  foreach(var o in trees)foreach(var f in o.GetComponentsInChildren<MeshFilter>())report.meshes.Add("TREE "+f.sharedMesh.name+" vertices="+f.sharedMesh.vertexCount+" triangles="+f.sharedMesh.triangles.Length/3);
  foreach(var f in game.World.Root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()!=null&&f.GetComponent<MeshRenderer>().enabled).OrderByDescending(f=>f.sharedMesh==null?0:f.sharedMesh.triangles.Length).Take(80))if(f.sharedMesh!=null)report.meshes.Add("SCENE "+f.name+" vertices="+f.sharedMesh.vertexCount+" triangles="+f.sharedMesh.triangles.Length/3);
  foreach(var group in root.GetComponentsInChildren<LODGroup>()) {var lods=group.GetLODs();report.meshes.Add("LOD "+group.name+" levels="+lods.Length);}
  var surfaceRenderers=game.World.Root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="海面可裁剪分块"||r.name=="海床可裁剪分块"||r.name=="连续细分礁台与深沟").ToArray();
  var lights=FindObjectsOfType<Light>().Where(l=>l.type==LightType.Directional).ToArray();var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;int originalMsaa=pipeline==null?4:pipeline.msaaSampleCount;var cameraData=game.Camera.GetUniversalAdditionalCameraData();
  Require(trees.All(o=>o.GetComponentsInChildren<MeshFilter>().Length==3),"Tree LOD hierarchy incomplete");
  var canvases=FindObjectsOfType<Canvas>();var canvasStates=Array.ConvertAll(canvases,c=>c.enabled);var monitor=GetComponent<CoastFrameMonitor>();var timing=new FrameTiming[1];
  var life=FindObjectsOfType<MonoBehaviour>().Where(m=>m is LivingReef||m is ReefResidents||m is ShoreWildlife||m is ShallowWaterFlow||m is ShorelineAtmosphere).ToArray();var lifeStates=Array.ConvertAll(life,m=>m.enabled);
  using(var tri=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count"))
  using(var draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
  using(var pass=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count"))
  using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
  using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))
  using(var render=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread")){
   foreach(string mode in (Arg("-perf-cpu")?new[]{"full","no-monitor","no-ui","no-life-update","full-repeat"}:Arg("-perf-acceptance")?new[]{"full","sailing-follow","walking"}:Arg("-perf-detail")?new[]{"full","no-water","no-bed","no-shadows","msaa2","no-post","full-repeat"}:new[]{"full","no-trees","no-reflection","no-trees-no-reflection","full-repeat","sailing-follow"})){
    foreach(var r in surfaceRenderers)r.enabled=!(mode=="no-water"&&r.name=="海面可裁剪分块"||mode=="no-bed"&&r.name!="海面可裁剪分块");foreach(var light in lights)light.shadows=mode=="no-shadows"?LightShadows.None:LightShadows.Soft;if(pipeline!=null)pipeline.msaaSampleCount=mode=="msaa2"?2:originalMsaa;cameraData.renderPostProcessing=mode!="no-post";
    for(int ci=0;ci<canvases.Length;ci++)canvases[ci].enabled=canvasStates[ci]&&mode!="no-ui";if(monitor!=null)monitor.enabled=mode!="no-monitor"&&mode!="no-ui";for(int li=0;li<life.Length;li++)life[li].enabled=lifeStates[li]&&mode!="no-life-update";
    bool hideTrees=mode.Contains("no-trees");foreach(var o in trees)o.SetActive(!hideTrees);sea.ReflectionEnabled=!mode.Contains("no-reflection");
    bool sailing=mode=="sailing-follow",walking=mode=="walking";game.CameraFollow.enabled=sailing||walking;game.Boat.Moor(new Vector3(55,0,94),Quaternion.Euler(0,20,0));game.Boat.Unmoor();if(!sailing)View(new Vector3(58,0,98),18);
    if(walking){game.Boat.Moor(new Vector3(53,0,94),Quaternion.Euler(0,90,0));yield return new WaitForEndOfFrame();Require(game.Expedition.Disembark(),"Walking view could not disembark");}
    var body=game.Boat.GetComponent<Rigidbody>();for(int i=0;i<(Arg("-perf-acceptance")?360:120);i++)yield return new WaitForEndOfFrame();
    Vector3 walkStart=game.Expedition.Explorer.transform.position;
    var values=new double[Arg("-perf-acceptance")?1200:360];double tris=0,draws=0,passes=0,gcs=0,mains=0,renders=0;double gpuTotal=0,activeTotal=0,waitTotal=0;int timingCount=0;double last=Time.realtimeSinceStartupAsDouble;
    for(int i=0;i<values.Length;i++){FrameTimingManager.CaptureFrameTimings();if(sailing)body.velocity=game.Boat.transform.forward*1.7f;yield return new WaitForEndOfFrame();if(FrameTimingManager.GetLatestTimings(1,timing)>0){gpuTotal+=timing[0].gpuFrameTime;activeTotal+=Math.Max(0,timing[0].cpuMainThreadFrameTime-timing[0].cpuMainThreadPresentWaitTime);waitTotal+=timing[0].cpuMainThreadPresentWaitTime;timingCount++;}double now=Time.realtimeSinceStartupAsDouble;values[i]=(now-last)*1000;last=now;tris+=tri.Valid?tri.LastValue:0;draws+=draw.Valid?draw.LastValue:0;passes+=pass.Valid?pass.LastValue:0;gcs+=gc.Valid?gc.LastValue:0;mains+=main.Valid?main.LastValue:0;renders+=render.Valid?render.LastValue:0;}
    var sample=new PerfSample{mode=mode,context=walking?("start="+walkStart+" end="+game.Expedition.Explorer.transform.position+" landing="+game.Expedition.CurrentLanding.feet):("boat="+game.Boat.transform.position),frames=values.Length,meanMs=values.Average(),triangles=tris/values.Length,drawCalls=draws/values.Length,setPass=passes/values.Length,gcBytes=gcs/values.Length,mainThreadMs=mains/values.Length/1e6,renderThreadMs=renders/values.Length/1e6,renderCounters=tri.Valid,cpuCounters=main.Valid,frameTimings=timingCount>0,gpuMs=timingCount==0?0:gpuTotal/timingCount,cpuActiveMs=timingCount==0?0:activeTotal/timingCount,cpuWaitMs=timingCount==0?0:waitTotal/timingCount};Array.Sort(values);sample.p50Ms=values[values.Length/2];sample.p95Ms=values[Math.Min(values.Length-1,(int)(values.Length*.95))];sample.p99Ms=values[Math.Min(values.Length-1,(int)(values.Length*.99))];sample.fps=1000/sample.meanMs;report.samples.Add(sample);Debug.Log("COAST_PERF "+JsonUtility.ToJson(sample));
    if(mode=="full"||sailing||walking){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+mode+".png",tex.EncodeToPNG());Destroy(tex);}
    if(walking){Require(Vector3.Distance(game.Expedition.Explorer.transform.position,game.Expedition.CurrentLanding.feet)<5,"Idle explorer left berth: "+sample.context);Require(game.Expedition.BoardShip(),"Walking view could not board: "+sample.context);}
   }
  }
  for(int ci=0;ci<canvases.Length;ci++)canvases[ci].enabled=canvasStates[ci];if(monitor!=null)monitor.enabled=true;for(int li=0;li<life.Length;li++)life[li].enabled=lifeStates[li];
  foreach(var o in trees)o.SetActive(true);sea.ReflectionEnabled=true;foreach(var r in surfaceRenderers)r.enabled=true;foreach(var light in lights)light.shadows=LightShadows.Soft;if(pipeline!=null)pipeline.msaaSampleCount=originalMsaa;cameraData.renderPostProcessing=true;
  game.Boat.Moor(new Vector3(53,0,94),Quaternion.Euler(0,90,0));yield return new WaitForEndOfFrame();Require(game.Expedition.Disembark(),"Cannot land at east pier");for(int i=0;i<12;i++)yield return new WaitForEndOfFrame();Require(game.Expedition.BoardShip(),"Cannot board at east pier");
  report.passed=failure=="";report.failure=failure;report.resolution=Screen.width+"x"+Screen.height;File.WriteAllText(output+"/result.json",JsonUtility.ToJson(report,true));Debug.Log("COAST_PERF_DONE "+report.passed);Application.Quit(report.passed?0:1);
 }
}
}
