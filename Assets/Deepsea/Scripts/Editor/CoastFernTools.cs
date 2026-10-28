using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.Rendering;
namespace Deepsea.Editor {public static class CoastFernTools {
 public static void Build(){Optimize();CoastPerfBuild.Build();} public static void Optimize(){string dir="Assets/Deepsea/Resources/CoastSlice/",path=dir+"fern_02.prefab";var instance=PrefabUtility.LoadPrefabContents(path);try{
  var parts=instance.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()!=null&&f.GetComponent<MeshRenderer>().enabled).ToArray();
  if(parts.Length>1){var material=parts[0].GetComponent<MeshRenderer>().sharedMaterial;foreach(var part in parts){var m=part.GetComponent<MeshRenderer>().sharedMaterial;if(m.GetTexture("_BaseMap")!=material.GetTexture("_BaseMap")||m.GetTexture("_BumpMap")!=material.GetTexture("_BumpMap"))throw new Exception("Fern parts have different textures");}
   var mesh=new Mesh{name="四种蕨叶合并株丛",indexFormat=IndexFormat.UInt32};var combined=parts.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=instance.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();mesh.CombineMeshes(combined,true,true);mesh.RecalculateBounds();string mp=dir+"FernPatchCombined.asset";if(AssetDatabase.LoadMainAssetAtPath(mp)!=null)AssetDatabase.DeleteAsset(mp);AssetDatabase.CreateAsset(mesh,mp);
   foreach(var part in parts)part.GetComponent<MeshRenderer>().enabled=false;
   var filter=instance.GetComponent<MeshFilter>();if(filter==null)filter=instance.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var renderer=instance.GetComponent<MeshRenderer>();if(renderer==null)renderer=instance.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
   PrefabUtility.SaveAsPrefabAsset(instance,path);Debug.Log("FERN_COMBINED parts="+parts.Length+" vertices="+mesh.vertexCount+" triangles="+mesh.triangles.Length/3);
  }
 }finally{PrefabUtility.UnloadPrefabContents(instance);}AssetDatabase.SaveAssets();}
}}
