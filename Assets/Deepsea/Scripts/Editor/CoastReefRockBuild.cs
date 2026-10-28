using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Deepsea.Editor {
public static class CoastReefRockBuild {
 [Serializable] class Library { public Record[] meshes; }
 [Serializable] class Record { public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles; }
 public static void Prepare(){
  const string root="Assets/Deepsea/Resources/CoastReefRocks/";
  var source=JsonUtility.FromJson<Library>(File.ReadAllText("Assets/Deepsea/ThirdParty/CoastArtV12/ReefLibrary.json"));
  if(source==null||source.meshes==null||source.meshes.Length!=6)throw new Exception("Six generated reef variants required");
  Directory.CreateDirectory(root);AssetDatabase.Refresh();
  foreach(var r in source.meshes){
   if(r.vertices.Length!=r.normals.Length||r.triangles.Length/3>1600)throw new Exception("Reef geometry budget invalid: "+r.name);
   var mesh=new Mesh{name=r.name};mesh.vertices=r.vertices;mesh.normals=r.normals;mesh.uv=r.uv;mesh.triangles=r.triangles;mesh.RecalculateBounds();mesh.RecalculateTangents();
   string path=root+r.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);}
  }
  var shader=Shader.Find("Deepsea/CoastReefStone");if(shader==null)throw new Exception("Triplanar reef shader missing");
  var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"Stone.mat");
  if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,root+"Stone.mat");}else mat.shader=shader;
  const string textures="Assets/Deepsea/ThirdParty/CoastArtV4/";
  mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"seaside_rock_diff_2k.jpg"));
  mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"seaside_rock_nor_gl_2k.jpg"));
  mat.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"seaside_rock_arm_2k.jpg"));
  mat.SetTexture("_CausticMap",Resources.Load<Texture2D>("CoastMaterials/CausticLight"));mat.enableInstancing=true;
  EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();Debug.Log("COAST_REEF_ROCK_LIBRARY_READY variants=6 trianglesEach=1600");
 }
}
}
