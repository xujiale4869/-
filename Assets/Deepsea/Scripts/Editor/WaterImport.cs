using UnityEditor;
using UnityEngine;
namespace Deepsea.Editor {
public static class WaterImport {
 public static void Prepare(){
  var n=AssetImporter.GetAtPath("Assets/Deepsea/Resources/Water/WaterNormals.tif") as TextureImporter;
  n.textureType=TextureImporterType.Default;n.sRGBTexture=false;n.alphaSource=TextureImporterAlphaSource.FromInput;n.maxTextureSize=1024;n.mipmapEnabled=true;
  n.wrapMode=TextureWrapMode.Repeat;n.filterMode=FilterMode.Trilinear;n.anisoLevel=4;n.SaveAndReimport();
  var f=AssetImporter.GetAtPath("Assets/Deepsea/Resources/Water/basicFoam.png") as TextureImporter;
  f.sRGBTexture=false;f.mipmapEnabled=true;f.wrapMode=TextureWrapMode.Repeat;f.filterMode=FilterMode.Trilinear;f.SaveAndReimport();
  ProjectSetup.Verify();
 }
}
}
