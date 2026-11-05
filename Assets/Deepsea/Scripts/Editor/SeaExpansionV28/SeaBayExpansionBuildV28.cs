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
 public static class SeaBayExpansionBuildV28 {
  const string Root="Assets/Deepsea/Resources/SeaExpansionV28/",Scene="Assets/Deepsea/Scenes/CoastSeaExpansionV28.unity";
  public static void PrepareAndBuild(){Prepare();Build();}
  public static void Prepare(){
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);Directory.CreateDirectory(Root);
   var shader=Shader.Find("Deepsea/BayOverlayV28");if(shader==null)throw new Exception("Missing overlay shader");
   if(AssetDatabase.LoadAssetAtPath<Material>(Root+"OverlayKeep.mat")==null)AssetDatabase.CreateAsset(new Material(shader){name="OverlayKeep"},Root+"OverlayKeep.mat");
   if(AssetDatabase.LoadAssetAtPath<Material>(Root+"SurfaceKeep.mat")==null)AssetDatabase.CreateAsset(new Material(Shader.Find("Deepsea/BaySurfaceV28")){name="SurfaceKeep"},Root+"SurfaceKeep.mat");
   string path=AssetDatabase.FindAssets("ScannedRock t:Mesh",new[]{"Assets/Deepsea/Resources"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
   if(path==null)path=AssetDatabase.FindAssets("EN_Reef_Rock t:Mesh",new[]{"Assets/Deepsea/Resources/CoastRefinement"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
   if(path==null)throw new Exception("No existing irregular rock mesh");var source=AssetDatabase.LoadAssetAtPath<Mesh>(path);var mesh=UnityEngine.Object.Instantiate(source);mesh.name="V28_NormalizedExistingRock";var b=source.bounds;var v=mesh.vertices;float inv=1/Mathf.Max(.001f,Mathf.Max(b.size.x,b.size.z));for(int i=0;i<v.Length;i++)v[i]=new Vector3((v[i].x-b.center.x)*inv,(v[i].y-b.min.y)*inv,(v[i].z-b.center.z)*inv);mesh.vertices=v;mesh.RecalculateBounds();
   if(AssetDatabase.LoadAssetAtPath<Mesh>(Root+"LandmarkRock.asset")==null)AssetDatabase.CreateAsset(mesh,Root+"LandmarkRock.asset");else UnityEngine.Object.DestroyImmediate(mesh);
   var scene=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaHarborV27.unity");var rt=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();if(rt.GetComponent<SeaBayExpansionAndBuildSpaceV28>()==null)rt.gameObject.AddComponent<SeaBayExpansionAndBuildSpaceV28>();EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();Debug.Log("BAY_V28_PREPARED rockSource="+path);
  }
  public static void Build(){
   EditorSceneManager.OpenScene(Scene);var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var bytes=files.Select(File.ReadAllBytes).ToArray();var name=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try{PlayerSettings.productName="深海灯塔 · 海湾生活 v21";PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.renderPipelineAsset=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");QualitySettings.renderPipeline=GraphicsSettings.renderPipelineAsset;Directory.CreateDirectory("Builds/SeaExpansionV28");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaExpansionV28/Guichao-SeaExpansionV28.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=name;PlayerSettings.enableFrameTimingStats=timing;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],bytes[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v28 build failed");
   foreach(var f in Directory.GetFiles("Builds/SeaHarborV27/Licenses","*",SearchOption.AllDirectories)){var dst=Path.Combine("Builds/SeaExpansionV28/Licenses",f.Substring("Builds/SeaHarborV27/Licenses".Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(f,dst,true);}
   int errors=ShaderUtil.GetShaderMessages(Shader.Find("Deepsea/BayOverlayV28")).Count(m=>m.severity.ToString()=="Error");if(errors>0)throw new Exception("v28 shader errors");File.WriteAllText("Tools/SeaExpansionV28/build.json","{\"passed\":true,\"warnings\":"+report.summary.totalWarnings+",\"shaderErrors\":0}");Debug.Log("BAY_V28_BUILD_OK");
  }
 }
}
