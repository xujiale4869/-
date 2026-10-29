using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea {
 [DefaultExecutionOrder(600)]
 public sealed partial class SeaSurfaceV24:MonoBehaviour {
  public static SeaSurfaceV24 Active {get;private set;}
  public static bool RollingActive {get{return Active!=null&&Active.ready&&Active.Rolling;}}
  public bool Rolling {get;private set;}
  public float Clock {get;private set;}
  SeaLifeRuntime game;SeaLifeNavigationV23 navigation;MeshRenderer water;Material legacy,rolling;
  UniversalRenderPipelineAsset originalPipeline,fastPipeline,msaa2Pipeline;UniversalRendererData rendererData;bool[] originalFeatures;
  bool ready,manual,showFPS=true;float displayTimer;int displayFrames;string fpsText="";GUIStyle fpsStyle;
  RenderTexture fourK;SeaFrameDescriptorV24 descriptorFeature;bool checking;readonly List<string> errors=new List<string>(),warnings=new List<string>();
  void Awake(){Active=this;checking=Environment.GetCommandLineArgs().Contains("-ocean-check");if(checking)Application.logMessageReceived+=Log;}
  IEnumerator Start(){
   game=GetComponent<SeaLifeRuntime>();navigation=GetComponent<SeaLifeNavigationV23>();while(!game.Ready||!navigation.NavigationReady)yield return null;
   water=game.world.Find("ShowcaseWater").GetComponent<MeshRenderer>();legacy=new Material(Resources.Load<Material>("SeaLifeNavigationV23/Water"));rolling=new Material(Resources.Load<Material>("SeaSurfaceV24/Water"));
   originalPipeline=Resources.Load<UniversalRenderPipelineAsset>("CoastRefinement/SamplePipeline");fastPipeline=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");msaa2Pipeline=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/PipelineMSAA2");
   rendererData=Resources.Load<UniversalRendererData>("CoastRefinement/SampleRenderer");originalFeatures=rendererData.rendererFeatures.Select(f=>f.isActive).ToArray();
   if(checking){SeaFrameDescriptorV24.HasNative=false;descriptorFeature=ScriptableObject.CreateInstance<SeaFrameDescriptorV24>();rendererData.rendererFeatures.Add(descriptorFeature);rendererData.SetDirty();}
   SetupAmbientBudget();SetupDepthBudget();navigation.SetBaseline(false);ready=true;SetMode(true,true);Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
   if(checking){game.store=new SeaLifeStore(Path.Combine(Application.temporaryCachePath,"ocean-v24-"+DateTime.UtcNow.Ticks+".json"));game.save=new SeaLifeSave();game.save.Normalize();game.RefreshWaterCollection();StartCoroutine(Review());}
   else game.Notify("滚动海面与浅水层次样板 · F7对照v23水面 · F9显示帧率 · 当前不限帧。");
  }
  void SetMode(bool nextRolling,bool fast){
   Rolling=nextRolling;water.enabled=true;water.sharedMaterial=nextRolling?rolling:legacy;SetDepthBudget(nextRolling&&fast);SetAmbientBudget(fast);
   var pipeline=fast?fastPipeline:originalPipeline;ApplyPipeline(pipeline,fast?AntialiasingMode.FastApproximateAntialiasing:AntialiasingMode.None);
   Shader.SetGlobalFloat("_OceanV24Layer",0);
  }
  void ApplyPipeline(UniversalRenderPipelineAsset pipeline,AntialiasingMode aa){
   GraphicsSettings.renderPipelineAsset=pipeline;QualitySettings.renderPipeline=pipeline;game.view.GetUniversalAdditionalCameraData().antialiasing=aa;
   if(fourK!=null&&fourK.antiAliasing!=pipeline.msaaSampleCount){fourK.Release();fourK.antiAliasing=pipeline.msaaSampleCount;fourK.Create();}
  }
  void Update(){if(!ready)return;if(!game.Paused&&!manual)Clock+=Time.deltaTime;Shader.SetGlobalFloat("_OceanV24Time",Clock);
   if(!checking&&SeaWaterArtUpgradeV26.Active==null&&Input.GetKeyDown(KeyCode.F7)){SetMode(!Rolling,true);game.Notify(Rolling?"v24滚动海面与浅水层次。F7对照v23。":"v23透明水面对照。F7返回v24。");}
   if(Input.GetKeyDown(KeyCode.F9))showFPS=!showFPS;
   displayTimer+=Time.unscaledDeltaTime;displayFrames++;if(displayTimer>.5f){fpsText="FPS "+(displayFrames/displayTimer).ToString("F0");displayTimer=0;displayFrames=0;}
  }
  void OnGUI(){if(fourK!=null&&Event.current.type==EventType.Repaint)GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),fourK,ScaleMode.ScaleToFit,false);if(!ready||!showFPS)return;if(fpsStyle==null)fpsStyle=new GUIStyle(GUI.skin.label){fontSize=16,alignment=TextAnchor.MiddleRight,normal={textColor=Color.white}};GUI.Label(new Rect(Screen.width-125,Screen.height-30,110,25),fpsText,fpsStyle);}
  void Log(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(s);else if(type==LogType.Warning)warnings.Add(s);}
  void RestoreFeatures(){if(rendererData!=null)for(int i=0;i<originalFeatures.Length;i++)rendererData.rendererFeatures[i].SetActive(originalFeatures[i]);if(descriptorFeature!=null)descriptorFeature.SetActive(true);}
  void OnDestroy(){if(checking)Application.logMessageReceived-=Log;if(Active==this)Active=null;DisposeDepthBudget();SetAmbientBudget(false);RestoreFeatures();if(descriptorFeature!=null&&rendererData!=null){rendererData.rendererFeatures.Remove(descriptorFeature);rendererData.SetDirty();Destroy(descriptorFeature);}if(game!=null&&game.view!=null)game.view.targetTexture=null;if(fourK!=null){fourK.Release();Destroy(fourK);}if(legacy!=null)Destroy(legacy);if(rolling!=null)Destroy(rolling);}
 }
}
