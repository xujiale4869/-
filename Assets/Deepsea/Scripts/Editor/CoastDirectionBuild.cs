using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
namespace Deepsea.Editor{
 public static class CoastDirectionBuild{
  const string Root="Assets/Deepsea/Resources/CoastDirection/";
  static readonly Dictionary<string,Material> mapped=new Dictionary<string,Material>();
  static string SceneSignature(string path){
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var entries=new List<string>();
   foreach(var root in scene.GetRootGameObjects()){
    foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))entries.Add("mesh|"+f.name+"|"+AssetDatabase.GetAssetPath(f.sharedMesh)+"|"+f.transform.localToWorldMatrix.ToString("R"));
    foreach(var c in root.GetComponentsInChildren<Camera>(true))entries.Add("camera|"+c.name+"|"+c.transform.localToWorldMatrix.ToString("R")+"|"+c.fieldOfView.ToString("R")+"|"+c.nearClipPlane.ToString("R")+"|"+c.farClipPlane.ToString("R"));
    foreach(var light in root.GetComponentsInChildren<Light>(true))entries.Add("light|"+light.name+"|"+light.transform.localToWorldMatrix.ToString("R")+"|"+light.color.ToString("R")+"|"+light.intensity.ToString("R"));
    foreach(var volume in root.GetComponentsInChildren<Volume>(true))entries.Add("volume|"+volume.name+"|"+AssetDatabase.GetAssetPath(volume.sharedProfile));
    foreach(var render in root.GetComponentsInChildren<MeshRenderer>(true))if(render.name=="ShowcaseWater")entries.Add("waterMaterial|"+AssetDatabase.GetAssetPath(render.sharedMaterial));
   }
   entries.Sort(StringComparer.Ordinal);return string.Join("\n",entries);
  }
  public static void Audit(){
   var before=SceneSignature("Assets/Deepsea/Scenes/CoastArtRefined.unity");var after=SceneSignature("Assets/Deepsea/Scenes/CoastArtPalette.unity");
   Directory.CreateDirectory("Verification/coast-art-v18-build");File.WriteAllText("Verification/coast-art-v18-build/scene-state-before.txt",before);File.WriteAllText("Verification/coast-art-v18-build/scene-state-after.txt",after);
   bool same=before==after;File.WriteAllText("Verification/coast-art-v18-build/scene-state-check.json","{\"sameMeshReferencesAndTransforms\":"+(same?"true":"false")+",\"sameCameraSunPostAndWaterMaterial\":"+(same?"true":"false")+",\"scope\":\"Authored v17/v18 scene mesh references, world matrices, camera optics, light colour/intensity, volume asset and water material references; land materials deliberately excluded.\"}");if(!same)throw new Exception("Palette scene changed protected authored scene state");Debug.Log("COAST_PALETTE_STATE_OK");
  }
  static float Role(string key){
   if(key.StartsWith("Tower_")){if(key=="Tower_Plaster")return 8;if(key=="Tower_Roof")return 7;if(key=="Tower_Iron")return 6;if(key=="Tower_Stone"||key=="Tower_Trim")return 1;return 0;}
   if(key.Contains("Foliage")||key=="Fern")return 3;if(key.EndsWith("Wood"))return 4;if(key.EndsWith("Bark"))return 5;
   if(key=="Sand"||key=="ContactSoil")return 2;if(key.Contains("Rock")||key=="FootingStone")return 1;return 0;
  }
  static Material Palette(Material source){
   string path=AssetDatabase.GetAssetPath(source),key=Path.GetFileNameWithoutExtension(path);float role=Role(key);if(role==0)return source;
   string family=source.shader.name;
   string shader=family=="Deepsea/CoastShowcaseSurface"?"Deepsea/CoastPaletteSurface":family=="Deepsea/CoastRefinedStone"?"Deepsea/CoastPaletteStone":family=="Deepsea/CoastRefinedArchitecture"?"Deepsea/CoastPaletteArchitecture":null;
   if(shader==null)return source;
   Material material;if(mapped.TryGetValue(path,out material))return material;
   string target=Root+key+".mat";material=AssetDatabase.LoadAssetAtPath<Material>(target);if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,target);}else EditorUtility.CopySerialized(source,material);
   material.shader=Shader.Find(shader);if(material.shader==null)throw new Exception(shader+" missing");material.SetFloat("_PaletteRole",role);EditorUtility.SetDirty(material);mapped[path]=material;return material;
  }
  [MenuItem("深海灯塔/美术样板/构建岸上色彩对照")]
  public static void Build(){
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();mapped.Clear();
   var scene=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastArtRefined.unity",OpenSceneMode.Single);
   foreach(var root in scene.GetRootGameObjects()){
    var old=root.GetComponent<CoastRefinementRuntime>();if(old!=null){UnityEngine.Object.DestroyImmediate(old);root.AddComponent<CoastDirectionRuntime>();}
    foreach(var render in root.GetComponentsInChildren<MeshRenderer>(true)){var mats=render.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]!=null)mats[i]=Palette(mats[i]);render.sharedMaterials=mats;}
   }
   foreach(var name in new[]{"Deepsea/CoastPaletteSurface","Deepsea/CoastPaletteStone","Deepsea/CoastPaletteArchitecture"}){
    var shader=Shader.Find(name);if(shader==null)throw new Exception(name+" missing");var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=settings.FindProperty("m_AlwaysIncludedShaders");bool found=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){list.InsertArrayElementAtIndex(list.arraySize);list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=shader;settings.ApplyModifiedPropertiesWithoutUndo();}
   }
   const string scenePath="Assets/Deepsea/Scenes/CoastArtPalette.unity";EditorSceneManager.SaveScene(scene,scenePath);AssetDatabase.SaveAssets();Directory.CreateDirectory("Builds/CoastArtV18");
   var oldName=PlayerSettings.productName;var oldPipeline=GraphicsSettings.renderPipelineAsset;var oldQuality=QualitySettings.renderPipeline;BuildReport report;
   try{PlayerSettings.productName="深海灯塔 · 岸上配色 A/B";var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Deepsea/Resources/CoastRefinement/SamplePipeline.asset");GraphicsSettings.renderPipelineAsset=pipeline;QualitySettings.renderPipeline=pipeline;report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName="Builds/CoastArtV18/MistCape-Palette.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=oldName;GraphicsSettings.renderPipelineAsset=oldPipeline;QualitySettings.renderPipeline=oldQuality;AssetDatabase.SaveAssets();}
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Palette build failed");
   Directory.CreateDirectory("Verification/coast-art-v18-build");File.WriteAllText("Verification/coast-art-v18-build/materials.txt",string.Join("\n",mapped.Keys));Debug.Log("COAST_PALETTE_BUILD_OK materials="+mapped.Count);
  }
 }
}
