using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
namespace Deepsea.Editor {
public static class NaturalCoastBuild {
 public static void Build(){
  const string audio="Assets/Deepsea/Resources/Coast/OceanSurf.ogg";
  var importer=AssetImporter.GetAtPath(audio) as AudioImporter;
  if(importer==null)throw new Exception("Missing CC0 ocean recording");
  importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;
  settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;
  importer.defaultSampleSettings=settings;importer.SaveAndReimport();
  ProjectSetup.EnsureProject();
  var graphicsConfig=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
  var included=graphicsConfig.FindProperty("m_AlwaysIncludedShaders");var timber=Shader.Find("Deepsea/CoastalTimber");bool found=false;
  for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==timber)found=true;
  if(!found){included.InsertArrayElementAtIndex(included.arraySize);included.GetArrayElementAtIndex(included.arraySize-1).objectReferenceValue=timber;graphicsConfig.ApplyModifiedProperties();AssetDatabase.SaveAssets();}
  var reef=Shader.Find("Deepsea/ReefWildlife");found=false;
  for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==reef)found=true;
  if(!found){included.InsertArrayElementAtIndex(included.arraySize);included.GetArrayElementAtIndex(included.arraySize-1).objectReferenceValue=reef;graphicsConfig.ApplyModifiedProperties();AssetDatabase.SaveAssets();}
  foreach(string name in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/ShoreSwash","Deepsea/Seabird","Deepsea/SeabirdShadow","Deepsea/CoastalTimber","Deepsea/WakeFoam","Deepsea/ReefCoral","Deepsea/ReefWildlife","Deepsea/Seagrass"}){
   var shader=Shader.Find(name);if(shader==null)throw new Exception("Missing shader "+name);
   foreach(var error in ShaderUtil.GetShaderMessages(shader))if(error.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+error.message);
  }
  foreach(string shaderName in new[]{"Deepsea/NaturalFoliage","Deepsea/ReefFoliage","Deepsea/ReefBenthos","Deepsea/ReefFish","Deepsea/ReefCoral"}){var sh=Shader.Find(shaderName);if(sh==null)throw new Exception(shaderName);bool present=false;for(int j=0;j<included.arraySize;j++)if(included.GetArrayElementAtIndex(j).objectReferenceValue==sh)present=true;if(!present){included.InsertArrayElementAtIndex(included.arraySize);included.GetArrayElementAtIndex(included.arraySize-1).objectReferenceValue=sh;}foreach(var e in ShaderUtil.GetShaderMessages(sh))if(e.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(e.message);}graphicsConfig.ApplyModifiedProperties();AssetDatabase.SaveAssets();const string output="Builds/NaturalCoastV1";Directory.CreateDirectory(output);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ProjectSetup.MainScene},locationPathName=output+"/GuichaoBay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Coast build failed");
  foreach(string folder in new[]{"BoatAttack","MarineLife","NVJOB","CoastSources","FastNoiseLite","TiledDirectionalFlow","ProceduralTree"}){
   string target=output+"/ThirdParty/"+folder;Directory.CreateDirectory(target);
   foreach(string file in Directory.GetFiles("Assets/Deepsea/ThirdParty/"+folder))
    if(!file.EndsWith(".meta")&&(file.EndsWith(".md")||file.IndexOf("LICENSE",StringComparison.OrdinalIgnoreCase)>=0))File.Copy(file,target+"/"+Path.GetFileName(file),true);
  }
  Debug.Log("REEF_GARDEN_BUILD_OK "+report.summary.totalSize);
 }
}
}
