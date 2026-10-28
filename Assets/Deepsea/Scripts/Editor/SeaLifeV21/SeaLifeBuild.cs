using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
namespace Deepsea.Editor {
 public static class SeaLifeBuild {
  public const string Scene="Assets/Deepsea/Scenes/CoastSeaLife.unity";
  const string Evidence="Verification/sea-life-v21-build";
  [Serializable] class MeshInfo {public string name,materials;public Vector3 center,size;public int triangles;}
  [Serializable] class Inspection {public MeshInfo[] meshes;public Vector3 boat;public string scene;}
  public static void Inspect(){
   EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastArtFineShore.unity",OpenSceneMode.Single);
   var world=GameObject.Find("EN_MistCapeShowcase");if(world==null)throw new Exception("Missing accepted v20 environment");
   var meshes=world.GetComponentsInChildren<MeshRenderer>(true).Select(r=>new MeshInfo{name=r.name,center=r.bounds.center,size=r.bounds.size,materials=string.Join(";",r.sharedMaterials.Select(m=>m==null?"null":AssetDatabase.GetAssetPath(m)).ToArray()),triangles=r.GetComponent<MeshFilter>()==null?0:r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3}).ToArray();
   Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/scene-inspect.json",JsonUtility.ToJson(new Inspection{meshes=meshes,boat=world.transform.Find("HeroBoat").position,scene=EditorSceneManager.GetActiveScene().path},true));
   Debug.Log("SEA_LIFE_INSPECT_OK meshes="+meshes.Length);
  }
  [MenuItem("深海灯塔/海湾生活/构建独立体验v21")]
  public static void Build(){
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Deepsea.SeaLifeRuntime")).FirstOrDefault(t=>t!=null);if(type==null)throw new Exception("SeaLifeRuntime not compiled");
   var scene=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastArtFineShore.unity",OpenSceneMode.Single);
   foreach(var go in scene.GetRootGameObjects())foreach(var old in go.GetComponents<CoastFineShoreRuntime>())UnityEngine.Object.DestroyImmediate(old);
   var host=new GameObject("海湾生活 · 独立入口");host.AddComponent(type);
   const string root="Assets/Deepsea/Resources/SeaLifeV21/";Directory.CreateDirectory(root);
   var sourceFont=AssetDatabase.LoadAssetAtPath<Font>(root+"SeaLifeNotoSansSC.otf");if(sourceFont==null)throw new Exception("New Chinese source font missing");
   var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(root+"SeaLifeChinese.asset");
   if(font==null){font=TMPro.TMP_FontAsset.CreateFontAsset(sourceFont,36,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,TMPro.AtlasPopulationMode.Dynamic,true);font.name="SeaLifeChinese";font.isMultiAtlasTexturesEnabled=true;AssetDatabase.CreateAsset(font,root+"SeaLifeChinese.asset");AssetDatabase.AddObjectToAsset(font.material,font);foreach(var tex in font.atlasTextures)AssetDatabase.AddObjectToAsset(tex,font);}
   var text=string.Concat(Directory.GetFiles("Assets/Deepsea/Scripts/Runtime/SeaLifeV21","*.cs").Select(File.ReadAllText));string missing;var characters=new string(text.Where(c=>!char.IsControl(c)).Distinct().ToArray());font.TryAddCharacters(characters,out missing);System.Collections.Generic.List<char> absent;if(!font.HasCharacters(characters,out absent))throw new Exception("SeaLife font missing characters: "+new string(absent.ToArray()));EditorUtility.SetDirty(font);
   foreach(var name in new[]{"Glass","Surface"}){var shader=Shader.Find(name=="Glass"?"Deepsea/SeaLifeGlass":"Deepsea/SeaLifeSurface");if(shader==null)throw new Exception("Required shader missing: "+name);var material=AssetDatabase.LoadAssetAtPath<Material>(root+name+".mat");if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,root+name+".mat");}material.shader=shader;if(name=="Glass")material.SetColor("_BaseColor",new Color(.38f,.76f,.79f,.08f));EditorUtility.SetDirty(material);}
   EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
   var settingsFiles=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};
   var snapshots=settingsFiles.Select(File.ReadAllBytes).ToArray();
   var oldName=PlayerSettings.productName;var oldGraphics=GraphicsSettings.renderPipelineAsset;var oldQuality=QualitySettings.renderPipeline;
   BuildReport report=null;
   try {
    PlayerSettings.productName="深海灯塔 · 海湾生活 v21";
    var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Deepsea/Resources/CoastRefinement/SamplePipeline.asset");GraphicsSettings.renderPipelineAsset=pipeline;QualitySettings.renderPipeline=pipeline;
    Directory.CreateDirectory("Builds/SeaLifeV21");
    report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaLifeV21/Guichao-SeaLife.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   } finally {PlayerSettings.productName=oldName;GraphicsSettings.renderPipelineAsset=oldGraphics;QualitySettings.renderPipeline=oldQuality;AssetDatabase.SaveAssets();for(int i=0;i<settingsFiles.Length;i++)File.WriteAllBytes(settingsFiles[i],snapshots[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("Sea life build failed");
   Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/build.json","{\"passed\":true,\"scene\":\""+Scene+"\",\"bytes\":"+report.summary.totalSize+"}");Debug.Log("SEA_LIFE_BUILD_OK");
  }
 }
}
