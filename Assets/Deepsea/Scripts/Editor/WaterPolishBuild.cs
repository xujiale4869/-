using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
namespace Deepsea.Editor {
public static class WaterPolishBuild {
 public static void Build(){
  const string audio="Assets/Deepsea/Resources/Coast/OceanSurf.ogg";
  var importer=AssetImporter.GetAtPath(audio) as AudioImporter;
  if(importer==null)throw new Exception("Missing CC0 ocean recording");
  importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;
  settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;
  importer.defaultSampleSettings=settings;importer.SaveAndReimport();
  ProjectSetup.EnsureProject();
  foreach(string name in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/ShoreSwash","Deepsea/Seabird","Deepsea/SeabirdShadow"}){
   var shader=Shader.Find(name);if(shader==null)throw new Exception("Missing shader "+name);
   foreach(var error in ShaderUtil.GetShaderMessages(shader))if(error.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+error.message);
  }
  const string output="Builds/WaterPolishV1";Directory.CreateDirectory(output);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ProjectSetup.MainScene},locationPathName=output+"/GuichaoBay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Coast build failed");
  foreach(string folder in new[]{"BoatAttack","MarineLife","NVJOB","CoastSources","FastNoiseLite"}){
   string target=output+"/ThirdParty/"+folder;Directory.CreateDirectory(target);
   foreach(string file in Directory.GetFiles("Assets/Deepsea/ThirdParty/"+folder))
    if(!file.EndsWith(".meta")&&(file.EndsWith(".md")||file.IndexOf("LICENSE",StringComparison.OrdinalIgnoreCase)>=0))File.Copy(file,target+"/"+Path.GetFileName(file),true);
  }
  Debug.Log("WATER_POLISH_BUILD_OK "+report.summary.totalSize);
 }
}
}
