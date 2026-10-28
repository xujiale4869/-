using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deepsea.Editor {
// Imports original Blender output. Json arrays retain the exact per-vertex colour and normals.
public static class CoastCoralBuild {
 [Serializable] class Library { public Record[] meshes; }
 [Serializable] class Record { public string name; public Vector3[] vertices,normals; public Vector2[] uv; public Color[] colors; public int[] triangles; }
 public static void Prepare() {
  const string root="Assets/Deepsea/Resources/CoastCoral/";
  var source=JsonUtility.FromJson<Library>(File.ReadAllText("Assets/Deepsea/ThirdParty/CoastArtV13/CoralLibrary.json"));
  if(source==null||source.meshes==null||source.meshes.Length!=5)throw new Exception("Five habitat coral models are required");
  Directory.CreateDirectory(root);AssetDatabase.Refresh();
  foreach(var r in source.meshes){
   if(r.vertices.Length!=r.colors.Length||r.vertices.Length!=r.normals.Length||r.triangles.Length/3>2400)throw new Exception("Coral attributes or budget invalid: "+r.name);
   var mesh=new Mesh{name=r.name};mesh.vertices=r.vertices;mesh.normals=r.normals;mesh.colors=r.colors;mesh.uv=r.uv;mesh.triangles=r.triangles;mesh.RecalculateBounds();mesh.RecalculateTangents();
   string path=root+r.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);}
   Debug.Log("COAST_CORAL_IMPORTED "+r.name+" triangles="+r.triangles.Length/3);
  }
  var shader=Shader.Find("Deepsea/CoastCoralLit");if(shader==null)throw new Exception("Coral shader missing");
  var material=AssetDatabase.LoadAssetAtPath<Material>(root+"Coral.mat");
  if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,root+"Coral.mat");}else material.shader=shader;
  material.enableInstancing=true;material.SetTexture("_CausticMap",Resources.Load<Texture2D>("CoastMaterials/CausticLight"));EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
 }
}
}
