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
  if(importer==null)throw new Exception("Install v9 kelp albedo before building");
  importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
  var leaf=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);var stone=Resources.Load<Material>("CoastMaterials/Rock");
  Make("Kelp",leaf,new Color(.82f,.92f,.78f),0,.15f);
  Make("Seagrass",leaf,new Color(.56f,.78f,.64f),1,.095f);
  var coral=Make("Coral",stone.GetTexture("_BaseMap"),new Color(.72f,.76f,.67f),2,.008f);coral.SetTexture("_BumpMap",stone.GetTexture("_BumpMap"));EditorUtility.SetDirty(coral);
  AssetDatabase.SaveAssets();Debug.Log("COAST_MARINE_ASSETS_READY 1K kelp albedo + scanned rock coral surface");
 }
 static Material Make(string name,Texture texture,Color tint,float kind,float sway){
  string path=Root+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Deepsea/CoastMarine"));AssetDatabase.CreateAsset(mat,path);}else mat.shader=Shader.Find("Deepsea/CoastMarine");
  mat.SetTexture("_BaseMap",texture);mat.SetColor("_Tint",tint);mat.SetFloat("_Kind",kind);mat.SetFloat("_Sway",sway);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
 }
}
}
