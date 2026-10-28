using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
namespace Deepsea.Editor {
 public static class CoastMapleBuild {
  const string Root="Assets/Deepsea/Resources/CoastMaple/",Source="Assets/Deepsea/ThirdParty/CoastArtV19/";
  [Serializable]class Record{public string name,material;public Vector3[] vertices,normals;public Vector2[] uv;public Color[] colors;public int[] triangles;}
  [Serializable]class Archive{public Record[] meshes;public Vector3[] landings,canopyAnchors;public int triangles,leaves,trees;}
  static Dictionary<string,Material> materials=new Dictionary<string,Material>();
  static void SaveMesh(string name,Mesh mesh){var path=Root+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);}}
  static Texture2D Texture(string name){return AssetDatabase.LoadAssetAtPath<Texture2D>(Root+name+".png");}
  static Material Material(string role){if(materials.TryGetValue(role,out var found))return found;string path=Root+role+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Deepsea/CoastMapleSurface"));AssetDatabase.CreateAsset(mat,path);}mat.shader=Shader.Find("Deepsea/CoastMapleSurface");mat.enableInstancing=true;mat.SetFloat("_Roughness",.82f);mat.SetFloat("_Sway",0);mat.SetFloat("_Scale",.47f);mat.SetFloat("_UseMask",0);mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_CausticMap",Resources.Load<Texture2D>("CoastMaterials/CausticLight"));
   if(role=="Limestone"){mat.SetFloat("_Mode",0);mat.SetTexture("_BaseMap",Texture("Limestone-Albedo"));mat.SetTexture("_BumpMap",Texture("Limestone-Normal"));mat.SetFloat("_NormalStrength",.62f);mat.SetColor("_BaseColor",new Color(.80f,.81f,.78f));}
   if(role=="MapleBark"){mat.SetFloat("_Mode",3);mat.SetTexture("_BaseMap",Texture("MapleBark-Albedo"));mat.SetTexture("_BumpMap",Texture("MapleBark-Normal"));mat.SetFloat("_NormalStrength",.30f);mat.SetColor("_BaseColor",new Color(.88f,.88f,.84f));}
   if(role=="MapleLeaf"){mat.SetFloat("_Mode",4);mat.SetTexture("_BaseMap",Texture("MapleLeaf-Albedo"));mat.SetFloat("_NormalStrength",0);mat.SetFloat("_Sway",.018f);mat.SetFloat("_Cutoff",.35f);mat.SetFloat("_Roughness",.72f);mat.SetColor("_BaseColor",new Color(.90f,.95f,.95f));}
   if(role=="Fern"){mat.SetFloat("_Mode",7);mat.SetTexture("_BaseMap",Texture2D.whiteTexture);mat.SetColor("_BaseColor",new Color(.14f,.25f,.07f));mat.SetFloat("_NormalStrength",0);mat.SetFloat("_Sway",.01f);}
   if(role=="CanopyShadow"){mat.SetFloat("_Mode",7);mat.SetTexture("_BaseMap",Texture2D.whiteTexture);mat.SetFloat("_NormalStrength",0);}

   if(role=="Ground"){mat.CopyPropertiesFromMaterial(Resources.Load<Material>("CoastRefinement/Sand"));mat.shader=Shader.Find("Deepsea/CoastMapleSurface");mat.SetTexture("_LandMap",Texture("Limestone-Albedo"));}
   EditorUtility.SetDirty(mat);materials[role]=mat;return mat;
  }
  [MenuItem("深海灯塔/美术样板/构建红枫海岬")]
  public static void Build(){
   Directory.CreateDirectory(Root);materials.Clear();foreach(var file in Directory.GetFiles(Source,"*.png"))File.Copy(file,Root+Path.GetFileName(file),true);AssetDatabase.Refresh();
   foreach(var file in Directory.GetFiles(Root,"*.png")){var importer=AssetImporter.GetAtPath(file) as TextureImporter;bool leafTexture=file.Contains("Leaf"),normal=file.Contains("Normal"),rough=file.Contains("Roughness");importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal&&!rough&&!file.Contains("Coverage");importer.wrapMode=leafTexture?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.alphaSource=leafTexture?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;importer.alphaIsTransparency=leafTexture;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=leafTexture;importer.alphaTestReferenceValue=.35f;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;importer.textureCompression=file.Contains("Coverage")?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();}
   var data=JsonUtility.FromJson<Archive>(File.ReadAllText(Source+"MapleCape.json"));if(data.landings.Length==0)throw new Exception("No validated water leaf targets");
   var scene=EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastArtRefined.unity",OpenSceneMode.Single);var env=GameObject.Find("EN_MistCapeShowcase");if(env==null)throw new Exception("Authored v17 scene missing");
   if(PrefabUtility.IsPartOfPrefabInstance(env))PrefabUtility.UnpackPrefabInstance(env,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   PrefabUtility.SaveAsPrefabAsset(env,Root+"Baseline.prefab");
   foreach(var root in scene.GetRootGameObjects()){var old=root.GetComponent<CoastRefinementRuntime>();if(old!=null){UnityEngine.Object.DestroyImmediate(old);root.AddComponent<CoastMapleRuntime>();}}
   var remove=new List<GameObject>();foreach(Transform t in env.transform){string n=t.name;if(n.Contains("_Pine_")||n.Contains("_ForegroundPine_")||n.Contains("_V17_Land_")||n.Contains("_V17_ShoreFocus_")||n.Contains("_V17_ShoreContact_")||n.Contains("_LandPlants_")||n.Contains("_Ground_Sand_"))remove.Add(t.gameObject);else if(n.Contains("_Ground_Sand_"))t.GetComponent<MeshRenderer>().sharedMaterial=Material("Ground");}
   foreach(var obj in remove)UnityEngine.Object.DestroyImmediate(obj);
   int vertices=0;foreach(var rec in data.meshes){var mesh=new Mesh{name=rec.name,indexFormat=IndexFormat.UInt32};mesh.vertices=rec.vertices;mesh.normals=rec.normals;mesh.uv=rec.uv;mesh.colors=rec.colors;mesh.triangles=rec.triangles;mesh.RecalculateTangents();mesh.RecalculateBounds();vertices+=mesh.vertexCount;SaveMesh(rec.name,mesh);var go=new GameObject(rec.name);go.transform.SetParent(env.transform,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+rec.name+".asset");var render=go.AddComponent<MeshRenderer>();render.sharedMaterial=Material(rec.material);if(rec.material=="MapleLeaf")render.shadowCastingMode=ShadowCastingMode.Off;if(rec.material=="CanopyShadow")render.enabled=false;}
   var leaf=new Mesh{name="DriftLeaf"};leaf.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f),new Vector3(0,.009f,0)};leaf.triangles=new[]{0,4,1,1,4,2,2,4,3,3,4,0};leaf.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up,new Vector2(.5f,.5f)};leaf.colors=new[]{Color.white,Color.white,Color.white,Color.white,Color.white};leaf.RecalculateNormals();leaf.RecalculateTangents();leaf.RecalculateBounds();SaveMesh("DriftLeaf",leaf);
   var ripple=new Mesh{name="Ripple"};ripple.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};ripple.triangles=new[]{0,1,2,0,2,3};ripple.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};ripple.RecalculateNormals();ripple.RecalculateBounds();SaveMesh("Ripple",ripple);
   var rm=AssetDatabase.LoadAssetAtPath<Material>(Root+"LeafRipple.mat");if(rm==null){rm=new Material(Shader.Find("Deepsea/CoastLeafRipple"));AssetDatabase.CreateAsset(rm,Root+"LeafRipple.mat");}
   var fx=env.AddComponent<CoastMapleLeaves>();fx.targets=data.landings;fx.anchors=data.canopyAnchors;
   foreach(var name in new[]{"Deepsea/CoastMapleSurface","Deepsea/CoastLeafRipple"}){var shader=Shader.Find(name);if(shader==null)throw new Exception("Shader missing "+name);var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=settings.FindProperty("m_AlwaysIncludedShaders");bool found=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){list.InsertArrayElementAtIndex(list.arraySize);list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=shader;settings.ApplyModifiedPropertiesWithoutUndo();}}
   const string path="Assets/Deepsea/Scenes/CoastArtMaple.unity";EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();Directory.CreateDirectory("Builds/CoastArtV19");
   var oldName=PlayerSettings.productName;var oldPipeline=GraphicsSettings.renderPipelineAsset;var oldQuality=QualitySettings.renderPipeline;BuildReport report;
   try{PlayerSettings.productName="深海灯塔 · 红枫海岬 v19";var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Deepsea/Resources/CoastRefinement/SamplePipeline.asset");GraphicsSettings.renderPipelineAsset=pipeline;QualitySettings.renderPipeline=pipeline;report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName="Builds/CoastArtV19/MistCape-RedMaple.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=oldName;GraphicsSettings.renderPipelineAsset=oldPipeline;QualitySettings.renderPipeline=oldQuality;AssetDatabase.SaveAssets();}
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Red maple build failed");Directory.CreateDirectory("Verification/coast-art-v19-build");File.WriteAllText("Verification/coast-art-v19-build/mesh-summary.json","{\"newTriangles\":"+data.triangles+",\"newVertices\":"+vertices+",\"treeLeaves\":"+data.leaves+",\"trees\":"+data.trees+",\"waterTargets\":"+data.landings.Length+"}");Debug.Log("COAST_MAPLE_BUILD_OK");
  }
 }
}
