using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.Rendering;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace Deepsea.Editor {
public static class CoastSliceBuild {
 const string Source="Assets/Deepsea/ThirdParty/CoastSlice/",Out="Assets/Deepsea/Resources/CoastSlice/";
 [MenuItem("深海灯塔/构建立体海岸片段")]
 public static void Build(){ProjectSetup.EnsureProject();Directory.CreateDirectory(Out);AssetDatabase.Refresh();if(AssetDatabase.LoadMainAssetAtPath(Out+"TreeReduced.asset")!=null)AssetDatabase.DeleteAsset(Out+"TreeReduced.asset");
  var report=new List<string>();
  foreach(var id in new[]{"coastal_cliff_01","boulder_01","island_tree_01","fern_02"}){
   string path=Source+id+"/"+id+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)throw new Exception("Missing asset "+path);importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
   foreach(var tex in Directory.GetFiles(Source+id).Where(f=>f.Contains("nor_gl")&&!f.EndsWith(".meta"))){var ti=AssetImporter.GetAtPath(tex) as TextureImporter;if(ti==null)continue;ti.textureType=TextureImporterType.NormalMap;ti.maxTextureSize=2048;ti.SaveAndReimport();}
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var instance=UnityEngine.Object.Instantiate(model);instance.name=id;
   foreach(var r in instance.GetComponentsInChildren<MeshRenderer>()){
    var materials=r.sharedMaterials;report.Add(id+" / "+r.name+" mats="+string.Join(",",materials.Select(m=>m==null?"null":m.name)));
    for(int k=0;k<materials.Length;k++){string name=materials[k]==null?"":materials[k].name.ToLower();Material mat;
     if(id=="fern_02")mat=PlantMaterial(id,"",true);
     else if(id=="island_tree_01")mat=PlantMaterial(id,name.Contains("leav")?"leaves_":name.Contains("branch")?"branches_":"",name.Contains("leav")||name.Contains("branch"));
     else{mat=new Material(Shader.Find("Deepsea/CoastRockLit"));mat.SetTexture("_BaseMap",Texture(id,"diff"));mat.SetTexture("_BumpMap",Texture(id,"nor_gl"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.65f);mat.SetFloat("_Moss",.52f);mat.SetFloat("_Desaturate",.72f);mat.SetColor("_BaseColor",new Color(.85f,.89f,.88f));mat.enableInstancing=false;}
     string mp=Out+id+"-"+r.name.Replace('/','_')+"-"+k+".mat";Put(mat,mp);materials[k]=mat;
    }r.sharedMaterials=materials;
   }
   if(id=="island_tree_01")CoastTreeTools.Install(instance);
   foreach(var c in instance.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
   PrefabUtility.SaveAsPrefabAsset(instance,Out+id+".prefab");
   foreach(var f in instance.GetComponentsInChildren<MeshFilter>()){report.Add("MESH "+id+" "+f.name+" vertices="+f.sharedMesh.vertexCount+" bounds="+f.sharedMesh.bounds);}
   UnityEngine.Object.DestroyImmediate(instance);
  }
  CoastFernTools.Optimize();
  var ground=new Material(Shader.Find("Deepsea/CoastRockLit"));ground.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/forest_ground_04/forest_ground_04_diff_2k.jpg"));ground.SetTexture("_DetailMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/grass_path_2/grass_path_2_diff_2k.jpg"));ground.SetTexture("_SandMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/aerial_sand/aerial_sand_diff_2k.jpg"));ground.SetFloat("_Ground",1);ground.SetFloat("_Triplanar",1);ground.SetFloat("_Scale",.36f);ground.SetFloat("_Moss",.88f);ground.SetColor("_BaseColor",new Color(.93f,.96f,.87f));Put(ground,Out+"Ground.mat");
  var sand=new Material(ground);sand.SetFloat("_Moss",0);sand.SetFloat("_Ground",0);sand.SetColor("_BaseColor",new Color(.94f,.92f,.79f));sand.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/aerial_sand/aerial_sand_diff_2k.jpg"));Put(sand,Out+"Sand.mat");
  var timber=new Material(Shader.Find("Deepsea/CoastRockLit"));timber.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/weathered_brown_planks/weathered_brown_planks_diff_2k.jpg"));timber.SetFloat("_Triplanar",1);timber.SetFloat("_Scale",.32f);timber.SetFloat("_Moss",0);timber.SetColor("_BaseColor",new Color(.72f,.66f,.52f));Put(timber,Out+"Timber.mat");
  var coral=new Material(Shader.Find("Deepsea/NaturalFoliage"));Put(coral,Out+"Ecology.mat");
  var pipeline=UnityEngine.Object.Instantiate(GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset);pipeline.shadowDistance=180;pipeline.msaaSampleCount=4;pipeline.supportsHDR=true;pipeline.supportsCameraDepthTexture=true;pipeline.supportsCameraOpaqueTexture=true;Put(pipeline,Out+"Pipeline.asset");
  var profile=ScriptableObject.CreateInstance<VolumeProfile>();var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.15f);color.contrast.Override(9);color.saturation.Override(-5);Put(profile,Out+"Look.asset");
  var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var included=graphics.FindProperty("m_AlwaysIncludedShaders");for(int i=included.arraySize-1;i>=0;i--){var sh=included.GetArrayElementAtIndex(i).objectReferenceValue as Shader;if(sh!=null&&sh.name=="Deepsea/CoastSliceSurface"){included.GetArrayElementAtIndex(i).objectReferenceValue=null;included.DeleteArrayElementAtIndex(i);}}var coastShader=Shader.Find("Deepsea/CoastRockLit");bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==coastShader)found=true;if(!found){int n=included.arraySize;included.InsertArrayElementAtIndex(n);included.GetArrayElementAtIndex(n).objectReferenceValue=coastShader;}graphics.ApplyModifiedPropertiesWithoutUndo();
  AssetDatabase.SaveAssets();foreach(var shader in new[]{Shader.Find("Deepsea/CoastRockLit"),Shader.Find("Deepsea/Ocean")})foreach(var e in ShaderUtil.GetShaderMessages(shader))if(e.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(shader.name+": "+e.message);
  Directory.CreateDirectory("Verification/coast-slice-v1");File.WriteAllLines("Verification/coast-slice-v1/asset-audit.txt",report);
  var options=new BuildPlayerOptions{scenes=new[]{ProjectSetup.MainScene},locationPathName="Builds/CoastSliceV2/GuichaoBay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};var result=BuildPipeline.BuildPlayer(options);if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed");string notices="Builds/CoastSliceV2/ThirdParty";Directory.CreateDirectory(notices);string prior="Builds/NaturalCoastV1/ThirdParty";if(Directory.Exists(prior))foreach(var f in Directory.GetFiles(prior,"*",SearchOption.AllDirectories)){string target=notices+f.Substring(prior.Length);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(f,target,true);}foreach(var id in new[]{"VertexBlend","StylizedTrees","ACGImporter","VolumetricLight"}){string from="Tools/StyleCandidates/"+id;foreach(var f in Directory.GetFiles(from).Where(f=>Path.GetFileName(f).StartsWith("LICENSE",StringComparison.OrdinalIgnoreCase))){string target=notices+"/StyleTrial/"+id+"/"+Path.GetFileName(f);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(f,target,true);}}Directory.CreateDirectory(notices+"/CoastSlice");File.Copy(Source+"SOURCE-LICENSE.txt",notices+"/CoastSlice/PolyHaven.txt",true);File.Copy("Tools/CoastSlice/UnityMeshSimplifier/LICENSE.md",notices+"/CoastSlice/UnityMeshSimplifier-LICENSE.md",true);Debug.Log("COAST_SLICE_BUILD_OK");
 }
 static Texture2D Texture(string id,string type){string dir=Source+id;string file=Directory.GetFiles(dir).FirstOrDefault(f=>!f.EndsWith(".meta")&&Path.GetFileName(f).StartsWith(id+"_"+type+"_"));return file==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(file);}
 static Material PlantMaterial(string id,string part,bool cutout){
  var mat=new Material(Shader.Find("Deepsea/CoastRockLit"));string dir=Source+id;string prefix=id+"_"+part;
  string df=Directory.GetFiles(dir).FirstOrDefault(f=>!f.EndsWith(".meta")&&Path.GetFileName(f).StartsWith(prefix+"diff_"));
  string nf=Directory.GetFiles(dir).FirstOrDefault(f=>!f.EndsWith(".meta")&&Path.GetFileName(f).StartsWith(prefix+"nor_gl_"));
  var color=df==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(df);
  string af=Directory.GetFiles(dir).FirstOrDefault(f=>!f.EndsWith(".meta")&&Path.GetFileName(f).StartsWith(prefix+"alpha_"));
  if(cutout&&af!=null&&df!=null){var img=new Texture2D(2,2);img.LoadImage(File.ReadAllBytes(df));var alpha=new Texture2D(2,2);alpha.LoadImage(File.ReadAllBytes(af));var pixels=img.GetPixels();for(int i=0;i<pixels.Length;i++){int x=i%img.width,y=i/img.width;pixels[i].a=alpha.GetPixelBilinear((x+.5f)/img.width,(y+.5f)/img.height).r;}img.SetPixels(pixels);img.Apply();string merged=Out+id+"-"+part+"cutout.png";File.WriteAllBytes(merged,img.EncodeToPNG());UnityEngine.Object.DestroyImmediate(img);UnityEngine.Object.DestroyImmediate(alpha);AssetDatabase.ImportAsset(merged);color=AssetDatabase.LoadAssetAtPath<Texture2D>(merged);}
  mat.SetTexture("_BaseMap",color);if(nf!=null){mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(nf));mat.EnableKeyword("_NORMALMAP");}mat.SetColor("_BaseColor",part.Length>0?new Color(.77f,.93f,.82f):new Color(.87f,.89f,.83f));mat.SetFloat("_Smoothness",.18f);mat.SetFloat("_Cull",0);mat.SetFloat("_Sway",.16f);mat.enableInstancing=false;
  if(cutout){mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.40f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;mat.SetOverrideTag("RenderType","TransparentCutout");}return mat;
 }
 static void Put(UnityEngine.Object obj,string path){if(AssetDatabase.LoadMainAssetAtPath(path)!=null)AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(obj,path);}
}
}
