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
 public static class SeaSurfaceBuildV26 {
  const string Root="Assets/Deepsea/Resources/SeaSurfaceV26/",Scene="Assets/Deepsea/Scenes/CoastSeaSurfaceV26.unity";
  public static void ImportAndBuild(){Import();Prepare();Build();}
  public static void Import(){
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   foreach(var name in new[]{"WaterCaustics_V26","ShoreFoamNoise_V26","WaterDepthRamp_V26"}){
    var path=Root+"Textures/"+name+".png";var ti=AssetImporter.GetAtPath(path) as TextureImporter;if(ti==null)throw new Exception("Missing texture "+path);
    ti.textureType=TextureImporterType.Default;ti.sRGBTexture=name=="WaterDepthRamp_V26";ti.wrapMode=name=="WaterDepthRamp_V26"?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
    ti.npotScale=TextureImporterNPOTScale.None;ti.mipmapEnabled=name!="WaterDepthRamp_V26";ti.filterMode=FilterMode.Bilinear;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=512;ti.SaveAndReimport();
   }
   var shader=Shader.Find("Deepsea/SeaSurfaceSparkleV26");if(shader==null||!shader.isSupported)throw new Exception("v26 shader missing or unsupported");
   var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();if(errors.Length>0)throw new Exception(string.Join("; ",errors));
   // A Resources material keeps Shader.Find available in the built player.
   var m=new Material(shader){name="v26 runtime shader inclusion"};var old=AssetDatabase.LoadAssetAtPath<Material>(Root+"Water.mat");if(old==null)AssetDatabase.CreateAsset(m,Root+"Water.mat");else{EditorUtility.CopySerialized(m,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(m);}
   AssetDatabase.SaveAssets();Debug.Log("WATER_V26_IMPORT_OK");
  }
  public static void Prepare(){var s=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaLifeV25.unity",OpenSceneMode.Single);var rt=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();if(rt.GetComponent<SeaWaterArtUpgradeV26>()==null)rt.gameObject.AddComponent<SeaWaterArtUpgradeV26>();EditorSceneManager.SaveScene(s,Scene);AssetDatabase.SaveAssets();}
  public static void Build(){
   EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
   var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var bytes=files.Select(File.ReadAllBytes).ToArray();var product=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try{PlayerSettings.productName="深海灯塔 · 海湾生活 v21";PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.renderPipelineAsset=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");QualitySettings.renderPipeline=GraphicsSettings.renderPipelineAsset;Directory.CreateDirectory("Builds/SeaSurfaceV26");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaSurfaceV26/Guichao-SeaSurfaceV26.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=product;PlayerSettings.enableFrameTimingStats=timing;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],bytes[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v26 build failed");
   if(Directory.Exists("Builds/SeaLifeV25/Licenses"))foreach(var f in Directory.GetFiles("Builds/SeaLifeV25/Licenses","*",SearchOption.AllDirectories)){var dst=Path.Combine("Builds/SeaSurfaceV26/Licenses",f.Substring("Builds/SeaLifeV25/Licenses".Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(f,dst,true);}
   File.WriteAllText("Tools/SeaSurfaceV26/build.json","{\"passed\":true,\"warnings\":"+report.summary.totalWarnings+",\"shaderErrors\":0}");Debug.Log("WATER_V26_BUILD_OK");
  }
 }
}
