using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Deepsea.Editor {
public static class CoastMarineBuild {
 const string Root="Assets/Deepsea/Resources/CoastMarine/";
 public static void Prepare(){
  Directory.CreateDirectory(Root);AssetDatabase.Refresh();
  string texture=Root+"KelpBlade-Albedo.jpg";var importer=AssetImporter.GetAtPath(texture)as TextureImporter;
  if(importer==null)throw new Exception("Install green kelp albedo before building");
  importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
  PrepareKelpMesh();
  var leaf=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);var stone=Resources.Load<Material>("CoastMaterials/Rock");
  Make("Kelp",leaf,new Color(.82f,.92f,.78f),0,.15f);
  Make("Seagrass",leaf,new Color(.56f,.78f,.64f),1,.095f);
  var coral=Make("Coral",stone.GetTexture("_BaseMap"),new Color(.72f,.76f,.67f),2,.008f);coral.SetTexture("_BumpMap",stone.GetTexture("_BumpMap"));EditorUtility.SetDirty(coral);
  AssetDatabase.SaveAssets();Debug.Log("COAST_MARINE_ASSETS_READY 1K kelp albedo + scanned rock coral surface");
 }
 static void PrepareKelpMesh(){
  string path=Root+"KelpRibbon.obj";var importer=AssetImporter.GetAtPath(path)as ModelImporter;
  if(importer==null)throw new Exception("Install v10 ribbon model before building");
  importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var filter=source.GetComponentInChildren<MeshFilter>(true);
  if(filter==null||filter.sharedMesh==null)throw new Exception("Ribbon OBJ has no mesh");
  var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var matrix=source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;var points=mesh.vertices;var normals=mesh.normals;var normalMatrix=matrix.inverse.transpose;
  var bounds=new Bounds(matrix.MultiplyPoint3x4(points[0]),Vector3.zero);
  for(int i=0;i<points.Length;i++){points[i]=matrix.MultiplyPoint3x4(points[i]);bounds.Encapsulate(points[i]);}
  if(bounds.size.y<.01f||mesh.uv.Length!=points.Length)throw new Exception("Ribbon orientation or UV invalid");
  var colours=new Color[points.Length];float inv=1/bounds.size.y;
  for(int i=0;i<points.Length;i++){points[i]=(points[i]-new Vector3(0,bounds.min.y,0))*inv;normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;float w=Mathf.Clamp01(points[i].y);colours[i]=new Color(.42f+.50f*w,.8f,.7f,w);}
  mesh.vertices=points;mesh.normals=normals;mesh.colors=colours;mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name="Kelp ribbons with preserved UV and root sway weights";
  if(mesh.triangles.Length/3>1000)throw new Exception("Ribbon model exceeds mesh budget");
  string saved=Root+"KelpRibbon.asset";if(AssetDatabase.LoadMainAssetAtPath(saved)!=null)AssetDatabase.DeleteAsset(saved);AssetDatabase.CreateAsset(mesh,saved);
  var data=ScriptableObject.CreateInstance<CoastKelpData>();data.vertices=mesh.vertices;data.uv=mesh.uv;data.colors=mesh.colors;data.triangles=mesh.triangles;data.bounds=mesh.bounds;data.Validate();
  string dataPath=Root+"KelpRibbonData.asset";if(AssetDatabase.LoadMainAssetAtPath(dataPath)!=null)AssetDatabase.DeleteAsset(dataPath);AssetDatabase.CreateAsset(data,dataPath);
  Debug.Log("COAST_KELP_RIBBON_ASSET vertices="+mesh.vertexCount+" triangles="+mesh.triangles.Length/3+" height="+mesh.bounds.size.y);
 }
 static Material Make(string name,Texture texture,Color tint,float kind,float sway){
  string path=Root+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Deepsea/CoastMarine"));AssetDatabase.CreateAsset(mat,path);}else mat.shader=Shader.Find("Deepsea/CoastMarine");
  mat.SetTexture("_BaseMap",texture);mat.SetColor("_Tint",tint);mat.SetFloat("_Kind",kind);mat.SetFloat("_Sway",sway);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
 }
}
}
