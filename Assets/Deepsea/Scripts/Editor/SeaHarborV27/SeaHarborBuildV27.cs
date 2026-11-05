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
 public static class SeaHarborBuildV27 {
  const string Root="Assets/Deepsea/Resources/SeaHarborV27/",Scene="Assets/Deepsea/Scenes/CoastSeaHarborV27.unity";
  public static void ImportAndBuild(){Import();Prepare();Build();}
  public static void Import(){
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);Directory.CreateDirectory(Root+"Baked");
   foreach(var name in new[]{"Lantern_V27_Hanging","Pier_V27_BarrelsAndCrates","Coral_V27_Cluster","Kelp_V27_GiantFrond"}){
    string path=Root+"Models/"+name+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)throw new Exception("Missing model "+path);importer.isReadable=true;importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
    var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);Func<string,Vector3> anchor=n=>go.transform.InverseTransformPoint(go.GetComponentsInChildren<Transform>().First(t=>t.name=="Basis_"+n).position);
    Vector3 o=anchor("Origin");var basis=Matrix4x4.identity;basis.SetColumn(0,anchor("Right")-o);basis.SetColumn(1,anchor("Up")-o);basis.SetColumn(2,anchor("Forward")-o);basis.SetColumn(3,new Vector4(o.x,o.y,o.z,1));
    var combines=go.GetComponentsInChildren<MeshFilter>().Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=basis.inverse*go.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();var mesh=new Mesh{name=name};mesh.CombineMeshes(combines,true,true);
    // Mesh.CombineMeshes does not reverse winding when the basis changes handedness.
    if(combines[0].transform.determinant<0){var tri=mesh.triangles;for(int i=0;i<tri.Length;i+=3){int tmp=tri[i];tri[i]=tri[i+1];tri[i+1]=tmp;}mesh.triangles=tri;}
    mesh.RecalculateNormals();mesh.RecalculateBounds();var exact=mesh.bounds;
    if(name.StartsWith("Lantern")&&Mathf.Abs(exact.size.y-.43f)>.015f)throw new Exception("Lantern basis/scale invalid "+exact);
    if(name.StartsWith("Kelp")&&Mathf.Abs(exact.size.y-1.85f)>.02f)throw new Exception("Kelp basis/scale invalid "+exact);
    var uv=mesh.uv;mesh.colors=Enumerable.Range(0,mesh.vertexCount).Select(i=>new Color(1,1,1,i<uv.Length?Mathf.Clamp01(uv[i].y):0)).ToArray();var bounds=mesh.bounds;bounds.Expand(.70f);mesh.bounds=bounds;
    string dst=Root+"Baked/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(dst);if(old==null)AssetDatabase.CreateAsset(mesh,dst);else{EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);}
    Debug.Log("HARBOR_MESH "+name+" vertices="+(old!=null?old.vertexCount:mesh.vertexCount)+" exactBounds="+exact);
   }
   foreach(var name in new[]{"WeatheredPierWood_V27","CoralPaletteLUT_V27"}){var ti=AssetImporter.GetAtPath(Root+"Textures/"+name+".png") as TextureImporter;ti.sRGBTexture=true;ti.npotScale=TextureImporterNPOTScale.None;ti.wrapMode=name.Contains("Palette")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;ti.filterMode=FilterMode.Bilinear;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=!name.Contains("Palette");ti.maxTextureSize=512;ti.SaveAndReimport();}
   Keep("ReefShaderKeep","Deepsea/UnderwaterReefVibrantV27");Keep("HarborShaderKeep","Deepsea/HarborSurfaceV27");Keep("WaterShaderKeep","Deepsea/SeaSurfaceSparkleHarborV27");Keep("GlowShaderKeep","Universal Render Pipeline/Unlit");AssetDatabase.SaveAssets();
  }
  static void Keep(string name,string shaderName){var s=Shader.Find(shaderName);if(s==null||!s.isSupported)throw new Exception("Missing shader "+shaderName);var errors=ShaderUtil.GetShaderMessages(s).Where(m=>m.severity.ToString()=="Error").ToArray();if(errors.Length>0)throw new Exception(string.Join(";",errors.Select(e=>e.message)));var m=new Material(s){name=name,enableInstancing=true};var old=AssetDatabase.LoadAssetAtPath<Material>(Root+name+".mat");if(old==null)AssetDatabase.CreateAsset(m,Root+name+".mat");else{EditorUtility.CopySerialized(m,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(m);}}
  public static void Prepare(){var s=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaSurfaceV26.unity");var rt=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();if(rt.GetComponent<SeaHarborAndReefUpgradeV27>()==null)rt.gameObject.AddComponent<SeaHarborAndReefUpgradeV27>();EditorSceneManager.SaveScene(s,Scene);AssetDatabase.SaveAssets();}
  public static void Build(){
   EditorSceneManager.OpenScene(Scene);var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var bytes=files.Select(File.ReadAllBytes).ToArray();var product=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try{PlayerSettings.productName="深海灯塔 · 海湾生活 v21";PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.renderPipelineAsset=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");QualitySettings.renderPipeline=GraphicsSettings.renderPipelineAsset;Directory.CreateDirectory("Builds/SeaHarborV27");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaHarborV27/Guichao-SeaHarborV27.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=product;PlayerSettings.enableFrameTimingStats=timing;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],bytes[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v27 build failed");
   foreach(var f in Directory.GetFiles("Builds/SeaSurfaceV26/Licenses","*",SearchOption.AllDirectories)){var dst=Path.Combine("Builds/SeaHarborV27/Licenses",f.Substring("Builds/SeaSurfaceV26/Licenses".Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(f,dst,true);}
   int shaderErrors=new[]{"Deepsea/UnderwaterReefVibrantV27","Deepsea/HarborSurfaceV27","Deepsea/SeaSurfaceSparkleHarborV27"}.Sum(n=>ShaderUtil.GetShaderMessages(Shader.Find(n)).Count(m=>m.severity.ToString()=="Error"));if(shaderErrors!=0)throw new Exception("v27 shader errors: "+shaderErrors);
   File.WriteAllText("Tools/SeaHarborV27/build.json","{\"passed\":true,\"warnings\":"+report.summary.totalWarnings+",\"shaderErrors\":0}");Debug.Log("HARBOR_V27_BUILD_OK");
  }
 }
}
