using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
namespace Deepsea.Editor {
public static class CoastHabitatBuild {
 const string Root="Assets/Deepsea/Resources/CoastHabitat/",Source="Assets/Deepsea/ThirdParty/CoastArtV8/";
 [Serializable]class MeshRecord{public string asset,node;public int sourceTriangles,reducedTriangles;public Vector3 bounds;}
 [Serializable]class Audit{public List<MeshRecord> meshes=new List<MeshRecord>();public string mask="512x512 linear RGBA; cover/path/variation/placement";}
 static Audit audit;
 public static void Prepare(Material ground){
  Directory.CreateDirectory(Root);AssetDatabase.Refresh();audit=new Audit();
  Plant("grass_medium_01","Grass",300,new Color(.78f,.91f,.68f),.035f);
  Plant("shrub_02","Shrub",1100,new Color(.75f,.88f,.72f),.025f);
  var fern=Resources.Load<GameObject>("CoastSlice/fern_02");var filter=fern.GetComponent<MeshFilter>();if(filter==null)filter=fern.GetComponentInChildren<MeshFilter>(true);if(filter==null)throw new Exception("Existing fern mesh missing");
  var low=Reduce(UnityEngine.Object.Instantiate(filter.sharedMesh),1200,"Fern");Save(low,"Fern.asset");audit.meshes.Add(new MeshRecord{asset="fern_02",node=filter.name,sourceTriangles=filter.sharedMesh.triangles.Length/3,reducedTriangles=low.triangles.Length/3,bounds=low.bounds.size});
  ground.shader=Shader.Find("Deepsea/CoastHabitatGround");
  var mask=ControlMask();ground.SetTexture("_HabitatControl",mask);ground.SetTexture("_CoverMap",Texture("grass_path_2/grass_path_2_diff_1k.jpg",true));ground.SetTexture("_CoverBump",Texture("grass_path_2/grass_path_2_nor_gl_1k.jpg",false,true));ground.SetTexture("_CoverMask",Texture("grass_path_2/grass_path_2_arm_1k.jpg",false));ground.SetTexture("_SoilMap",Resources.Load<Material>("CoastSlice/Ground").GetTexture("_BaseMap"));EditorUtility.SetDirty(ground);
  var rock=Resources.Load<Material>("CoastMaterials/RockSurface");rock.SetTexture("_HabitatControl",mask);rock.SetTexture("_CoverMap",ground.GetTexture("_CoverMap"));EditorUtility.SetDirty(rock);
  Directory.CreateDirectory("Verification/coast-art-v14");File.WriteAllText("Verification/coast-art-v14/habitat-assets.json",JsonUtility.ToJson(audit,true));AssetDatabase.SaveAssets();Debug.Log("COAST_HABITAT_ASSETS "+JsonUtility.ToJson(audit));
 }
 static void Save(UnityEngine.Object value,string name){string path=Root+name;if(AssetDatabase.LoadMainAssetAtPath(path)!=null)AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(value,path);}
 static Texture2D Texture(string relative,bool srgb,bool normal=false){string path=Source+relative;var importer=AssetImporter.GetAtPath(path)as TextureImporter;if(importer==null)throw new Exception("Missing habitat texture "+path);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=srgb;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
 static Mesh Reduce(Mesh source,int target,string name){var reducer=new UnityMeshSimplifier.MeshSimplifier();reducer.Initialize(source);reducer.SimplifyMesh(Mathf.Min(1,target/(source.triangles.Length/3f)));var mesh=reducer.ToMesh();UnityEngine.Object.DestroyImmediate(source);mesh.name="Habitat "+name+" with original UVs";mesh.RecalculateBounds();mesh.RecalculateTangents();if(mesh.uv.Length!=mesh.vertexCount)throw new Exception("Habitat UVs lost "+name);return mesh;}
 static void Plant(string id,string name,int target,Color tint,float wind){
  string path=Source+id+"/"+id+".fbx";var importer=AssetImporter.GetAtPath(path)as ModelImporter;if(importer==null)throw new Exception("Missing habitat FBX "+path);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);MeshFilter chosen=null;int least=int.MaxValue;
  foreach(var f in source.GetComponentsInChildren<MeshFilter>(true)){var m=f.sharedMesh;if(m==null||m.uv.Length!=m.vertexCount||m.vertexCount<50||m.triangles.Length<120||m.bounds.size.y<.0001f)continue;Debug.Log("COAST_HABITAT_CANDIDATE "+id+" "+f.name+" triangles="+m.triangles.Length/3+" bounds="+m.bounds.size);if((id!="grass_medium_01"||f.name.Contains("small_b_LOD0"))&&m.triangles.Length<least){least=m.triangles.Length;chosen=f;}}
  if(chosen==null)throw new Exception("No usable habitat mesh "+id);
  var mesh=UnityEngine.Object.Instantiate(chosen.sharedMesh);var vertices=mesh.vertices;var normals=mesh.normals;var matrix=source.transform.worldToLocalMatrix*chosen.transform.localToWorldMatrix;var nmatrix=matrix.inverse.transpose;var bounds=new Bounds(matrix.MultiplyPoint3x4(vertices[0]),Vector3.zero);
  for(int i=0;i<vertices.Length;i++){vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);bounds.Encapsulate(vertices[i]);}
  float scale=1/Mathf.Max(.0001f,bounds.size.y);for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z))*scale;for(int i=0;i<normals.Length;i++)normals[i]=nmatrix.MultiplyVector(normals[i]).normalized;
  mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=chosen.sharedMesh.triangles;
  if(matrix.determinant<0){var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3){int v=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=v;}mesh.triangles=indices;}
  mesh=Reduce(mesh,target,name);Save(mesh,name+".asset");
  var mat=new Material(Shader.Find("Deepsea/CoastEcology")){name=name+" photographic foliage",enableInstancing=true};mat.SetTexture("_BaseMap",Cutout(id,name));mat.SetColor("_Tint",tint);mat.SetFloat("_VertexColor",0);mat.SetFloat("_Cutoff",.30f);mat.SetFloat("_Sway",wind);Save(mat,name+".mat");
  audit.meshes.Add(new MeshRecord{asset=id,node=chosen.name,sourceTriangles=least/3,reducedTriangles=mesh.triangles.Length/3,bounds=mesh.bounds.size});
 }
 static Texture2D Cutout(string id,string name){
  var colour=new Texture2D(2,2);var alpha=new Texture2D(2,2);colour.LoadImage(File.ReadAllBytes(Source+id+"/"+id+"_diff_1k.jpg"));alpha.LoadImage(File.ReadAllBytes(Source+id+"/"+id+"_alpha_1k.png"));var pixels=colour.GetPixels32();
  for(int i=0;i<pixels.Length;i++){int x=i%colour.width,y=i/colour.width;pixels[i].a=(byte)(alpha.GetPixelBilinear((x+.5f)/colour.width,(y+.5f)/colour.height).r*255);}
  colour.SetPixels32(pixels);colour.Apply();string path=Root+name+"-cutout.png";File.WriteAllBytes(path,colour.EncodeToPNG());UnityEngine.Object.DestroyImmediate(colour);UnityEngine.Object.DestroyImmediate(alpha);AssetDatabase.ImportAsset(path);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
 static Texture2D ControlMask(){
  const int size=512;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true);var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=CoastHabitatLayout.Control((x+.5f)/size*142,42+(y+.5f)/size*112);texture.SetPixels(pixels);texture.Apply();string path=Root+"HabitatControl.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
}
}
