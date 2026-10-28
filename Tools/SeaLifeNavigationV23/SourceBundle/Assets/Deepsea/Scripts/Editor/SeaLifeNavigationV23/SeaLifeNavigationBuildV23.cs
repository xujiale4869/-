using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea.Editor {
 public static class SeaLifeNavigationBuildV23 {
  const string Scene="Assets/Deepsea/Scenes/CoastSeaLifeNavigationV23.unity";
  const string Root="Assets/Deepsea/Resources/SeaLifeNavigationV23/";
  public static void Prepare(){
   var scene=EditorSceneManager.OpenScene(SeaLifeWaterBuildV22.Scene,OpenSceneMode.Single);
   var game=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();if(game==null)throw new Exception("Accepted v22 scene missing runtime");
   foreach(var old in game.GetComponents<SeaLifeWaterV22>())UnityEngine.Object.DestroyImmediate(old);
   game.gameObject.AddComponent<SeaLifeNavigationV23>();Directory.CreateDirectory(Root);
   var water=new Material(Resources.Load<Material>("SeaLifeWaterV22/Water")){name="流动清透海面 v23",shader=Shader.Find("Deepsea/SeaLifeNavigationV23")};
   if(water.shader==null||!water.shader.isSupported)throw new Exception("v23 water shader unavailable");
   SaveAsset(water,Root+"Water.mat");SaveAsset(new Material(Shader.Find("Deepsea/SeaLifeWakeV23")){name="转弯扩散尾迹 v23"},Root+"Wake.mat");
   EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
  }
  static void SaveAsset(UnityEngine.Object value,string path){var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(old==null)AssetDatabase.CreateAsset(value,path);else{EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(value);}}
  [MenuItem("深海灯塔/海湾生活/构建流动水面与船控v23")]
  public static void Build(){
   if(!File.Exists(Scene))Prepare();EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
   if(UnityEngine.Object.FindObjectOfType<SeaLifeNavigationV23>()==null)throw new Exception("Saved v23 scene missing runtime");
   var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var snapshots=files.Select(File.ReadAllBytes).ToArray();
   var product=PlayerSettings.productName;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try {PlayerSettings.productName="深海灯塔 · 海湾生活 v21";var pipeline=Resources.Load<RenderPipelineAsset>("CoastRefinement/SamplePipeline");GraphicsSettings.renderPipelineAsset=pipeline;QualitySettings.renderPipeline=pipeline;
    Directory.CreateDirectory("Builds/SeaLifeNavigationV23");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaLifeNavigationV23/Guichao-SeaNavigation.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   }finally{PlayerSettings.productName=product;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],snapshots[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v23 navigation build failed");
   string licenseOut="Builds/SeaLifeNavigationV23/Licenses/SeaLifeNavigationV23";foreach(string path in Directory.GetFiles("Tools/SeaLifeNavigationV23/ThirdParty","*",SearchOption.AllDirectories)){string target=Path.Combine(licenseOut,path.Substring("Tools/SeaLifeNavigationV23/ThirdParty".Length+1));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,true);}
   Directory.CreateDirectory("Verification/sea-navigation-v23-build");File.WriteAllText("Verification/sea-navigation-v23-build/build.json","{\"passed\":true,\"bytes\":"+report.summary.totalSize+",\"warnings\":"+report.summary.totalWarnings+"}");Debug.Log("SEA_NAVIGATION_V23_BUILD_OK");
  }
 }
}
