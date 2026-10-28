using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea.Editor {
 public static class CoastFoundationBuild {
  const string Root="Assets/Deepsea/Resources/CoastFoundation/";
  const string Source="Assets/Deepsea/ThirdParty/CoastArtV14/LighthouseMeshes.json";
  [Serializable]class Archive{public Record[] meshes;}
  [Serializable]class Record{public string name,material;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;}
  public static void Prepare(){
   var data=JsonUtility.FromJson<Archive>(File.ReadAllText(Source));
   if(data==null||data.meshes==null||data.meshes.Length!=8)throw new Exception("Lighthouse material groups invalid");
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();var root=new GameObject("AR_CoastLighthouse");int total=0;
   try {
    foreach(var r in data.meshes){
     if(r.vertices.Length!=r.normals.Length||r.vertices.Length!=r.uv.Length)throw new Exception("Lighthouse vertex data invalid: "+r.name);
     var mesh=new Mesh{name=r.name,indexFormat=IndexFormat.UInt32};mesh.vertices=r.vertices;mesh.normals=r.normals;mesh.uv=r.uv;mesh.triangles=r.triangles;mesh.RecalculateTangents();mesh.RecalculateBounds();
     string path=Root+r.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}
     var go=new GameObject(r.name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=Material(r.material);
     total+=r.triangles.Length/3;
    }
    if(total>30000)throw new Exception("Lighthouse triangle budget exceeded");
    PrefabUtility.SaveAsPrefabAsset(root,Root+"Lighthouse.prefab");AssetDatabase.SaveAssets();Debug.Log("COAST_FOUNDATION_BUILT lighthouseTriangles="+total+" materials=8 colliders=0");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  static Material Material(string role){
   string path=Root+role+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find("Deepsea/CoastArchitecture");if(shader==null)throw new Exception("Architecture shader missing");
   if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
   m.shaderKeywords=Array.Empty<string>();m.renderQueue=-1;m.SetOverrideTag("RenderType","Opaque");m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.Zero);m.SetFloat("_ZWrite",1);m.SetShaderPassEnabled("ShadowCaster",true);
   m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",Color.black);m.SetFloat("_TextureWeight",0);m.SetFloat("_UseMask",0);m.SetFloat("_NormalStrength",0);m.SetFloat("_Smoothness",.25f);m.SetFloat("_Metallic",0);m.SetFloat("_UseVertexColor",0);
   string texture=role=="Plaster"?"worn_plaster_wall":role=="Stone"?"seaside_rock":null;
   if(texture!=null){
    string input="Assets/Deepsea/ThirdParty/CoastArtV4/"+texture;
    m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(input+"_diff_2k.jpg"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(input+"_nor_gl_2k.jpg"));m.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(input+"_arm_2k.jpg"));
    m.SetFloat("_TextureWeight",role=="Plaster"?.85f:1);m.SetFloat("_UseMask",1);m.SetFloat("_NormalStrength",role=="Plaster"?.30f:.50f);m.SetColor("_BaseColor",role=="Plaster"?new Color(.94f,.91f,.81f):new Color(.88f,.90f,.86f));
   }else if(role=="Trim")m.SetColor("_BaseColor",new Color(.52f,.49f,.40f));
   else if(role=="Iron"){m.SetColor("_BaseColor",new Color(.055f,.072f,.064f));m.SetFloat("_Metallic",.45f);m.SetFloat("_Smoothness",.32f);}
   else if(role=="Roof"){m.SetColor("_BaseColor",new Color(.16f,.11f,.061f));m.SetFloat("_Metallic",.35f);m.SetFloat("_Smoothness",.26f);}
   else if(role=="Wood"){m.SetColor("_BaseColor",new Color(.16f,.10f,.048f));m.SetTexture("_BaseMap",Resources.Load<Material>("CoastSlice/Timber").GetTexture("_BaseMap"));m.SetFloat("_TextureWeight",.65f);}
   else if(role=="Glass"){
    m.SetColor("_BaseColor",new Color(.055f,.13f,.14f,.24f));m.SetFloat("_Smoothness",.82f);m.SetFloat("_Metallic",.05f);
    m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
    m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent;m.SetShaderPassEnabled("ShadowCaster",false);
   }
   else if(role=="Lens"){m.SetColor("_BaseColor",new Color(.83f,.42f,.09f));m.SetColor("_EmissionColor",new Color(1.15f,.52f,.12f));m.SetFloat("_Smoothness",.55f);}
   EditorUtility.SetDirty(m);return m;
  }
 }
}
