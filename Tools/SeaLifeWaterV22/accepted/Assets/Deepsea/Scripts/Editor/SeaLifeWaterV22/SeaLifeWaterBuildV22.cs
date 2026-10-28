using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
namespace Deepsea.Editor {
 public static class SeaLifeWaterBuildV22 {
  const string Root="Assets/Deepsea/Resources/SeaLifeWaterV22/";
  public const string Scene="Assets/Deepsea/Scenes/CoastSeaLifeWaterV22.unity";
  // Explicit regeneration entry. Normal builds open the saved v22 scene.
  public static void Prepare(){
   var scene=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaLife.unity",OpenSceneMode.Single);
   var game=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();if(game==null)throw new Exception("Accepted v21 life scene missing runtime");game.gameObject.AddComponent<SeaLifeWaterV22>();
   Directory.CreateDirectory(Root);
   var original=Resources.Load<Material>("CoastFineShore/WaterStudy");var water=new Material(original){name="清透缓浪 v22",shader=Shader.Find("Deepsea/SeaLifeWaterV22")};if(water.shader==null)throw new Exception("Water shader missing");
   SaveAsset(water,Root+"Water.mat");var wake=new Material(Shader.Find("Deepsea/SeaLifeWakeV22")){name="轻柔历史尾迹 v22"};SaveAsset(wake,Root+"Wake.mat");
   var sea=GameObject.Find("EN_MistCapeShowcase").transform.Find("ShowcaseWater");var bounds=sea.GetComponent<MeshRenderer>().bounds;
   var xs=Axis(bounds.min.x,bounds.max.x,-40,65);var zs=Axis(bounds.min.z,bounds.max.z,-40,80);var v=new List<Vector3>();var tri=new List<int>();
   foreach(float z in zs)foreach(float x in xs)v.Add(sea.InverseTransformPoint(new Vector3(x,SeaLifeRuntime.Sea,z)));
   for(int j=0;j<zs.Count-1;j++)for(int i=0;i<xs.Count-1;i++){int a=j*xs.Count+i;tri.AddRange(new[]{a,a+xs.Count,a+1,a+1,a+xs.Count,a+xs.Count+1});}
   var mesh=new Mesh{name="近岸细分水面 v22",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var mb=mesh.bounds;mb.Expand(new Vector3(0,.4f,0));mesh.bounds=mb;SaveAsset(mesh,Root+"WaterMesh.asset");
   sea.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"WaterMesh.asset");EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
  }
  [MenuItem("深海灯塔/海湾生活/构建水面修订v22")]
  public static void Build(){
   if(!File.Exists(Scene))Prepare();
   var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
   if(UnityEngine.Object.FindObjectOfType<SeaLifeWaterV22>()==null)throw new Exception("Saved v22 scene has no water runtime");
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"WaterMesh.asset");if(mesh==null)throw new Exception("Saved v22 water mesh missing");
   var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var snapshots=files.Select(File.ReadAllBytes).ToArray();
   var product=PlayerSettings.productName;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try {PlayerSettings.productName="深海灯塔 · 海湾生活 v21";var pipeline=Resources.Load<RenderPipelineAsset>("CoastRefinement/SamplePipeline");GraphicsSettings.renderPipelineAsset=pipeline;QualitySettings.renderPipeline=pipeline;
    Directory.CreateDirectory("Builds/SeaLifeWaterV22");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaLifeWaterV22/Guichao-SeaWater.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   }finally{PlayerSettings.productName=product;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],snapshots[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("Water v22 build failed");
   Directory.CreateDirectory("Verification/sea-water-v22-build");File.WriteAllText("Verification/sea-water-v22-build/build.json","{\"passed\":true,\"vertices\":"+mesh.vertexCount+",\"triangles\":"+mesh.triangles.Length/3+",\"bytes\":"+report.summary.totalSize+"}");Debug.Log("SEA_WATER_V22_BUILD_OK");
  }
  static void SaveAsset(UnityEngine.Object value,string path){var existing=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(existing==null)AssetDatabase.CreateAsset(value,path);else{EditorUtility.CopySerialized(value,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(value);}}
  static List<float> Axis(float min,float max,float denseMin,float denseMax){var a=new List<float>{min};for(float p=min+15;p<denseMin;p+=15)a.Add(p);for(float p=denseMin;p<denseMax;p+=.65f)a.Add(p);a.Add(denseMax);for(float p=denseMax+15;p<max;p+=15)a.Add(p);a.Add(max);return a;}
 }
}
