using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
namespace Deepsea.Editor {
public static class ShoreBuild {
 public static void Build(){
  const string caustics="Assets/Deepsea/Resources/Water/WaterCaustics.tif";
  var importer=(TextureImporter)AssetImporter.GetAtPath(caustics);
  if(importer==null)throw new Exception("Missing Boat Attack caustic map");
  importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.mipmapEnabled=true;
  importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.maxTextureSize=512;
  importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  if(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Deepsea/Resources/Wildlife/SwimmingFish.asset")==null)throw new Exception("Missing previously imported fish mesh");
  ProjectSetup.EnsureProject();
  foreach(string name in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/Seagrass","Deepsea/MarineFish","Deepsea/MarineSplash"}){
   Shader shader=Shader.Find(name);if(shader==null)throw new Exception("Missing shader "+name);
   foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+message.message);
  }
  const string output="Builds/ShoreV1";Directory.CreateDirectory(output);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ProjectSetup.MainScene},locationPathName=output+"/GuichaoBay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Shore build failed");
  foreach(string folder in new[]{"BoatAttack","MarineLife"}){
   string target=output+"/ThirdParty/"+folder;Directory.CreateDirectory(target);
   foreach(string f in Directory.GetFiles("Assets/Deepsea/ThirdParty/"+folder))
    if(f.EndsWith(".md")||f.IndexOf("LICENSE",StringComparison.OrdinalIgnoreCase)>=0)File.Copy(f,target+"/"+Path.GetFileName(f),true);
  }
  Debug.Log("SHORE_BUILD_OK "+report.summary.totalSize);
 }
}
}
