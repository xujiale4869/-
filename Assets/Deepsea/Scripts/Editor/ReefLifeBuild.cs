using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
namespace Deepsea.Editor {
public static class ReefLifeBuild {
 public static void Build(){
  ProjectSetup.EnsureProject();
  var config=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var included=config.FindProperty("m_AlwaysIncludedShaders");
  foreach(string name in new[]{"Deepsea/CoastalTimber","Deepsea/ReefCoral","Deepsea/ReefFoliage","Deepsea/ReefFish","Deepsea/ReefBenthos"}){
   var shader=Shader.Find(name);if(shader==null)throw new Exception("Missing shader "+name);bool found=false;
   for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
   if(!found){included.InsertArrayElementAtIndex(included.arraySize);included.GetArrayElementAtIndex(included.arraySize-1).objectReferenceValue=shader;}
  }
  config.ApplyModifiedProperties();AssetDatabase.SaveAssets();
  foreach(string name in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/CoastalTimber","Deepsea/ReefCoral","Deepsea/ReefFoliage","Deepsea/ReefFish","Deepsea/ReefBenthos"}){
   var shader=Shader.Find(name);if(shader==null)throw new Exception("Missing shader "+name);
   foreach(var error in ShaderUtil.GetShaderMessages(shader))if(error.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+error.message);
  }
  const string output="Builds/ReefLifeV1";Directory.CreateDirectory(output);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ProjectSetup.MainScene},locationPathName=output+"/GuichaoBay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Reef life build failed");
  foreach(string folder in new[]{"BoatAttack","MarineLife","NVJOB","CoastSources","FastNoiseLite","TiledDirectionalFlow","ProceduralTree","ReefLife"}){
   string source="Assets/Deepsea/ThirdParty/"+folder,target=output+"/ThirdParty/"+folder;Directory.CreateDirectory(target);
   foreach(string file in Directory.GetFiles(source))if(!file.EndsWith(".meta")&&(file.EndsWith(".md")||file.IndexOf("LICENSE",StringComparison.OrdinalIgnoreCase)>=0))File.Copy(file,target+"/"+Path.GetFileName(file),true);
  }
  Debug.Log("REEF_LIFE_BUILD_OK "+report.summary.totalSize);
 }
}
}
