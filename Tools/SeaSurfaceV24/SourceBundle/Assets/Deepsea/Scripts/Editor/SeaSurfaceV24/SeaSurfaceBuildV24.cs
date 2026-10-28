using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea.Editor {
 public static class SeaSurfaceBuildV24 {
  const string Scene="Assets/Deepsea/Scenes/CoastSeaSurfaceV24.unity",Root="Assets/Deepsea/Resources/SeaSurfaceV24/";
  public static void Prepare(){
   var scene=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaLifeNavigationV23.unity",OpenSceneMode.Single);var game=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();game.gameObject.AddComponent<SeaSurfaceV24>();Directory.CreateDirectory(Root);
   var water=new Material(Shader.Find("Deepsea/SeaSurfaceV24")){name="滚动海面与浅水窗口 v24"};water.SetTexture("_WaveMap",Resources.Load<Material>("SeaLifeNavigationV23/Water").GetTexture("_WaveMap"));Save(water,Root+"Water.mat");
   var pipeline=UnityEngine.Object.Instantiate(Resources.Load<UniversalRenderPipelineAsset>("CoastRefinement/SamplePipeline"));pipeline.name="v24全分辨率双采样";pipeline.msaaSampleCount=2;pipeline.renderScale=1;
   var so=new SerializedObject(pipeline);so.FindProperty("m_OpaqueDownsampling").intValue=1;so.ApplyModifiedPropertiesWithoutUndo();Save(pipeline,Root+"Pipeline.asset");
   EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
  }
  static void Save(UnityEngine.Object value,string path){var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(old==null)AssetDatabase.CreateAsset(value,path);else{EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(value);}}
  // Explicit revision step. Ordinary Build preserves the saved scene/materials.
  public static void TuneAndBuild(){
   var water=Resources.Load<Material>("SeaSurfaceV24/Water");water.SetColor("_Shallow",new Color(.012f,.40f,.48f));water.SetColor("_Deep",new Color(.004f,.14f,.22f));EditorUtility.SetDirty(water);
   var pipeline=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");var two=UnityEngine.Object.Instantiate(pipeline);two.name="v24双采样对照";two.msaaSampleCount=2;Save(two,Root+"PipelineMSAA2.asset");pipeline.msaaSampleCount=1;EditorUtility.SetDirty(pipeline);
   var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);GameObject.Find("ShowcaseCamera").GetComponent<UniversalAdditionalCameraData>().antialiasing=AntialiasingMode.FastApproximateAntialiasing;EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Build();
  }
  public static void Build(){
   if(!File.Exists(Scene))Prepare();EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
   var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var bytes=files.Select(File.ReadAllBytes).ToArray();
   var product=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try{PlayerSettings.productName="深海灯塔 · 海湾生活 v21";PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.renderPipelineAsset=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");QualitySettings.renderPipeline=GraphicsSettings.renderPipelineAsset;
    Directory.CreateDirectory("Builds/SeaSurfaceV24");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaSurfaceV24/Guichao-SeaSurface.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   }finally{PlayerSettings.productName=product;PlayerSettings.enableFrameTimingStats=timing;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],bytes[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v24 build failed");
   if(Directory.Exists("Builds/SeaLifeNavigationV23/Licenses"))CopyDirectory("Builds/SeaLifeNavigationV23/Licenses","Builds/SeaSurfaceV24/Licenses");Directory.CreateDirectory("Tools/SeaSurfaceV24");File.WriteAllText("Tools/SeaSurfaceV24/build.json","{\"passed\":true,\"bytes\":"+report.summary.totalSize+",\"warnings\":"+report.summary.totalWarnings+"}");Debug.Log("SEA_SURFACE_V24_BUILD_OK");
  }
  public static void PrepareBudgetAndBuild(){foreach(var n in new[]{"CoastFoundation","CoastMapleSurface","CoastShelfLit","CoastShowcaseSurface"}){var shader=Shader.Find("Deepsea/V24"+n);if(shader==null||!shader.isSupported)throw new Exception("Missing budget shader "+n);Save(new Material(shader){name="v24水下细节预算 "+n},Root+"Budget"+n+".mat");}AssetDatabase.SaveAssets();Build();}
  static void CopyDirectory(string source,string dest){foreach(var path in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){string target=Path.Combine(dest,path.Substring(source.Length+1));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,true);}}
 }
}
