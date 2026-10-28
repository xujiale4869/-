using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OceanGame.Data;
namespace Deepsea.Editor {
 public static class SeaLifeV25Build {
  const string Root="Assets/Deepsea/Resources/SeaLifeV25/",Scene="Assets/Deepsea/Scenes/CoastSeaLifeV25.unity";
  static readonly string[] Names={"Fish_0_SilverBream","Fish_1_BluefinTuna","Fish_2_GoldButterfly","AquariumTank_V25_Frame","AquariumTank_V25_Interior","AquariumTank_V25_Glass"};
  [Serializable]class MeshInfo{public string name;public int vertices,triangles,submeshes;public Vector3 size;}
  [Serializable]class ImportReport{public bool passed;public MeshInfo[] meshes;public string[] shaderErrors;}
  public static void ImportAndBuild(){PrepareAssets();PrepareScene();Build();}
  public static void PrepareAssets(){
   Directory.CreateDirectory(Root+"Meshes");Directory.CreateDirectory(Root+"Materials");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   var meshes=new List<Mesh>();
   foreach(var n in Names){
    string path=Root+"Models/"+n+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)throw new Exception("Missing generated FBX "+path);
    importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;importer.importAnimation=false;importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importNormals=ModelImporterNormals.Import;importer.SaveAndReimport();
    meshes.Add(Bake(path,n));
   }
   foreach(var n in Names.Take(3)){
    var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"Textures/"+n+"_Albedo.png");ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=false;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Clamp;ti.maxTextureSize=512;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
   }
   var db=FishAndAquariumDatabaseSO.CreateRuntimeDefault();
   for(int i=0;i<3;i++){
    var f=db.GetFishById(i);f.highDetailMesh=meshes[i];f.albedoTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+Names[i]+"_Albedo.png");
    var m=new Material(Shader.Find("Deepsea/SeaLifeFishSwimV25")){name="v25 "+f.displayName};m.SetTexture("_BaseMap",f.albedoTexture);m.SetColor("_RimColor",f.iridescentRimColor);m.SetFloat("_Smoothness",f.smoothness);m.SetFloat("_Metallic",f.metallic);m.SetFloat("_TailAmplitude",f.tailWaveAmplitude);Save(m,Root+"Materials/Fish"+i+".mat");
   }
   db.defaultTankStyle.frameMesh=meshes[3];db.defaultTankStyle.interiorMesh=meshes[4];db.defaultTankStyle.glassMesh=meshes[5];db.defaultTankStyle.topLightColor=new Color(1,.95f,.84f);db.defaultTankStyle.topLightIntensity=1.1f;
   Save(db,Root+"FishAndAquariumDatabase.asset");
   var wood=new Material(Resources.Load<Material>("CoastRefinement/Wood")){name="v25原木柜"};wood.SetColor("_BaseColor",new Color(.54f,.36f,.21f));Save(wood,Root+"Materials/Wood.mat");
   Lit("Brass",new Color(.64f,.48f,.23f),.7f,.65f);Lit("Sand",new Color(.65f,.58f,.40f),0,.22f);Lit("Reef",new Color(.32f,.40f,.39f),0,.30f);Lit("Lamp",new Color(.95f,.86f,.62f),.1f,.75f);
   var glassShader=Shader.Find("Deepsea/SeaLifeAquariumGlassV25");if(glassShader==null)throw new Exception("Glass shader missing");
   Optical("Glass",glassShader,0,new Color(.42f,.80f,.86f,.027f),null);
   var caustics=Caustics();Optical("WaterSurface",glassShader,1,new Color(.12f,.48f,.52f,.04f),caustics);Optical("Caustics",glassShader,2,new Color(.69f,.90f,.82f,.20f),caustics);
   var sand=new Mesh{name="v25贴合砂床焦散"};sand.vertices=meshes[4].vertices.Select(v=>v+Vector3.up*.006f).ToArray();sand.normals=meshes[4].normals;sand.uv=sand.vertices.Select(v=>new Vector2(v.x*.30f+.5f,v.z*.45f+.5f)).ToArray();sand.triangles=meshes[4].GetTriangles(0);sand.RecalculateBounds();Save(sand,Root+"Meshes/SandCaustics.asset");
   var shaders=new[]{Shader.Find("Deepsea/SeaLifeFishSwimV25"),glassShader};var errors=shaders.SelectMany(s=>ShaderUtil.GetShaderMessages(s)).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
   var report=new ImportReport{passed=errors.Length==0,shaderErrors=errors,meshes=meshes.Select(m=>new MeshInfo{name=m.name,vertices=m.vertexCount,triangles=m.triangles.Length/3,submeshes=m.subMeshCount,size=m.bounds.size}).ToArray()};
   Directory.CreateDirectory("Tools/SeaLifeV25");File.WriteAllText("Tools/SeaLifeV25/import.json",JsonUtility.ToJson(report,true));
   if(meshes[3].subMeshCount!=3||meshes[4].subMeshCount!=2)throw new Exception("Tank material slots lost in import");
   foreach(var m in meshes.Take(3))if(m.bounds.size.z<.8f||m.bounds.size.z>1.4f||m.bounds.size.y<.25f||m.bounds.size.y>.85f)throw new Exception("Fish import axes/units wrong "+m.name+" "+m.bounds.size);
   if(meshes[3].bounds.size.x<3.5f||meshes[3].bounds.size.y<2.35f||meshes[3].bounds.size.y>2.55f)throw new Exception("Tank axes/units wrong");
   AssetDatabase.SaveAssets();if(errors.Length>0)throw new Exception(string.Join("; ",errors));Debug.Log("SEA_LIFE_V25_IMPORT_OK");
  }
  static Mesh Bake(string path,string name){
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
   try{
    var ts=model.GetComponentsInChildren<Transform>(true);Func<string,Vector3> anchor=n=>ts.First(t=>t.name=="V25_"+n).position;
    Vector3 o=anchor("Origin");var basis=Matrix4x4.identity;basis.SetColumn(0,(Vector4)(anchor("Right")-o));basis.SetColumn(1,(Vector4)(anchor("Up")-o));basis.SetColumn(2,(Vector4)(anchor("Forward")-o));basis.SetColumn(3,new Vector4(o.x,o.y,o.z,1));
    if(Mathf.Abs(basis.determinant)<.0000001f)throw new Exception("Degenerate FBX coordinate anchors "+path);
    var parts=new List<CombineInstance>();foreach(var mf in model.GetComponentsInChildren<MeshFilter>(true))for(int s=0;s<mf.sharedMesh.subMeshCount;s++)parts.Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=s,transform=basis.inverse*mf.transform.localToWorldMatrix});
    var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray(),false,true);mesh.RecalculateBounds();
    if(name.StartsWith("Fish_")){var b=mesh.bounds;b.Expand(new Vector3(.24f,.04f,.01f));mesh.bounds=b;}
    Save(mesh,Root+"Meshes/"+name+".asset");return AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Meshes/"+name+".asset");
   }finally{UnityEngine.Object.DestroyImmediate(model);}
  }
  static Texture2D Caustics(){var t=new Texture2D(128,128,TextureFormat.RGBA32,false){name="v25焦散",wrapMode=TextureWrapMode.Repeat};var px=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float u=x*Mathf.PI*2/128,v=y*Mathf.PI*2/128;float s=Mathf.Sin(u*3+Mathf.Cos(v*2))+Mathf.Cos(v*3-Mathf.Sin(u*2));float value=Mathf.Pow(Mathf.Max(0,1-Mathf.Abs(s)*2.9f),2);px[y*128+x]=new Color(value,value,value,1);}t.SetPixels(px);t.Apply();Save(t,Root+"Textures/Caustics.asset");return AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/Caustics.asset");}
  static void Optical(string n,Shader shader,float mode,Color color,Texture tex){var m=new Material(shader){name="v25 "+n};m.SetColor("_BaseColor",color);m.SetFloat("_Mode",mode);if(tex!=null)m.SetTexture("_CausticsMap",tex);m.renderQueue=mode>1.5?3001:mode>.5?3005:3010;Save(m,Root+"Materials/"+n+".mat");}
  static void Lit(string n,Color c,float metallic,float smooth){var m=new Material(Shader.Find("Deepsea/SeaLifeSurface")){name="v25 "+n};m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);Save(m,Root+"Materials/"+n+".mat");}
  static void Save(UnityEngine.Object obj,string path){var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(old==null)AssetDatabase.CreateAsset(obj,path);else{EditorUtility.CopySerialized(obj,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(obj);}}
  public static void PrepareScene(){var s=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaSurfaceV24.unity",OpenSceneMode.Single);var rt=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();if(rt.GetComponent<SeaLifeVisualUpgradeV25>()==null)rt.gameObject.AddComponent<SeaLifeVisualUpgradeV25>();EditorSceneManager.SaveScene(s,Scene);AssetDatabase.SaveAssets();}
  public static void Build(){
   if(!File.Exists(Scene))throw new Exception("Run ImportAndBuild for initial v25 setup");EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
   var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var bytes=files.Select(File.ReadAllBytes).ToArray();var product=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try{PlayerSettings.productName="深海灯塔 · 海湾生活 v21";PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.renderPipelineAsset=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");QualitySettings.renderPipeline=GraphicsSettings.renderPipelineAsset;Directory.CreateDirectory("Builds/SeaLifeV25");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaLifeV25/Guichao-SeaLifeV25.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=product;PlayerSettings.enableFrameTimingStats=timing;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],bytes[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v25 build failed");
   if(Directory.Exists("Builds/SeaSurfaceV24/Licenses"))CopyDirectory("Builds/SeaSurfaceV24/Licenses","Builds/SeaLifeV25/Licenses");
   File.WriteAllText("Tools/SeaLifeV25/build.json","{\"passed\":true,\"bytes\":"+report.summary.totalSize+",\"warnings\":"+report.summary.totalWarnings+"}");Debug.Log("SEA_LIFE_V25_BUILD_OK");
  }
  static void CopyDirectory(string source,string dest){foreach(var p in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){string t=Path.Combine(dest,p.Substring(source.Length+1));Directory.CreateDirectory(Path.GetDirectoryName(t));File.Copy(p,t,true);}}
 }
}
