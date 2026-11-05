using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea.Editor {
 public static class SeaUnifiedBuildV30 {
  const string Scene="Assets/Deepsea/Scenes/CoastSeaUnifiedV30.unity";
  public static void PrepareAndBuild(){Prepare();Build();}
  public static void Prepare(){AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaSeamlessV29.unity");var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(s,Scene);AssetDatabase.SaveAssets();}
  public static void Build(){
   EditorSceneManager.OpenScene(Scene);var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var bytes=files.Select(File.ReadAllBytes).ToArray();var name=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try{PlayerSettings.productName="深海灯塔 · 海湾生活 v21";PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.renderPipelineAsset=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");QualitySettings.renderPipeline=GraphicsSettings.renderPipelineAsset;Directory.CreateDirectory("Builds/SeaUnifiedV30");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaUnifiedV30/Guichao-SeaUnifiedV30.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=name;PlayerSettings.enableFrameTimingStats=timing;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],bytes[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v30 build failed");foreach(var f in Directory.GetFiles("Builds/SeaSeamlessV29/Licenses","*",SearchOption.AllDirectories)){var dst=Path.Combine("Builds/SeaUnifiedV30/Licenses",f.Substring("Builds/SeaSeamlessV29/Licenses".Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(f,dst,true);}File.WriteAllText("Tools/SeaUnifiedV30/build.json","{\"passed\":true,\"warnings\":"+report.summary.totalWarnings+"}");Debug.Log("UNIFIED_V30_BUILD_OK");
  }
 }
}
