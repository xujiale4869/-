using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deepsea.Editor
{
 public static class CoastMaterialsBuild
 {
  const string Root="Assets/Deepsea/Resources/CoastMaterials/";
  [MenuItem("深海灯塔/第三阶段/构建材质与光照版")]
  public static void Build()
  {
   if(!File.Exists(CoastBlockoutBuild.Scene))throw new Exception("Accepted whitebox scene missing");
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();
   var source=Resources.Load<GameObject>("CoastSlice/coastal_cliff_01");
   if(source==null)throw new Exception("Existing coastal_cliff_01 resource missing");
   Material stone=source.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
   var sand=Resources.Load<Material>("CoastSlice/Sand");var timber=Resources.Load<Material>("CoastSlice/Timber");
   if(stone.GetTexture("_BaseMap")==null||sand==null||timber==null)throw new Exception("Existing texture inputs missing");
   Texture2D caustic=CausticTexture();
   Material rock=Surface("Rock",new Color(.81f,.87f,.86f),0);
   rock.SetTexture("_CausticMap",caustic);rock.SetTexture("_BaseMap",stone.GetTexture("_BaseMap"));rock.SetTexture("_BumpMap",stone.GetTexture("_BumpMap"));rock.SetFloat("_Moss",.78f);rock.SetFloat("_TextureContrast",.48f);
   var ground=Surface("Ground",new Color(.81f,.87f,.86f),1);ground.CopyPropertiesFromMaterial(rock);ground.SetFloat("_Kind",1);ground.SetTexture("_SandMap",sand.GetTexture("_BaseMap"));ground.SetColor("_SandColor",new Color(.88f,.87f,.77f));
   var wood=Surface("Wood",new Color(.71f,.72f,.66f),2);wood.SetTexture("_BaseMap",timber.GetTexture("_BaseMap"));wood.SetFloat("_ObjectMapping",1);wood.SetFloat("_Scale",.55f);wood.SetFloat("_TextureContrast",.55f);
   var plaster=Surface("Plaster",new Color(.64f,.65f,.57f),4);plaster.SetTexture("_BaseMap",stone.GetTexture("_BaseMap"));EditorUtility.SetDirty(plaster);Surface("Metal",new Color(.09f,.16f,.17f),3);
   Surface("Coat",new Color(.10f,.25f,.33f),3);Surface("Skin",new Color(.60f,.42f,.24f),3);Surface("Fish",new Color(.30f,.48f,.53f),3);
   var water=Get("Water","Deepsea/CoastFoundationWater");water.SetColor("_Shallow",new Color(.08f,.53f,.51f));water.SetColor("_Deep",new Color(.012f,.09f,.15f));
   var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"Look.asset");
   if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"Look.asset");}
   Tonemapping tone;if(!profile.TryGet(out tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
   ColorAdjustments color;if(!profile.TryGet(out color)){color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(.16f);color.contrast.Override(9);color.saturation.Override(-5);
   EditorUtility.SetDirty(profile);
   foreach(string name in new[]{"Deepsea/CoastFoundation","Deepsea/CoastFoundationWater","Deepsea/CoastEcology"})Include(name);
   foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Root+"Look.asset"))EditorUtility.SetDirty(asset);
   foreach(var m in new[]{rock,ground,wood,water})EditorUtility.SetDirty(m);
   // Flora and fish are instantiated at runtime, so scene-based stripping cannot infer their variants.
   var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
   var instancing=graphics.FindProperty("m_InstancingStripping");
   if(instancing==null)throw new Exception("Instancing stripping setting missing");
   Debug.Log("COAST_INSTANCING_MODES "+string.Join(",",instancing.enumNames));
   int keep=Array.FindIndex(instancing.enumNames,n=>n.Replace(" ","")=="KeepAll");
   if(keep<0)throw new Exception("KeepAll instancing mode missing");
   instancing.enumValueIndex=keep;graphics.ApplyModifiedPropertiesWithoutUndo();
   Debug.Log("COAST_ECOLOGY_KEEP_INSTANCING_VARIANTS");
   AssetDatabase.SaveAssets();PlayerSettings.enableFrameTimingStats=true;
   Directory.CreateDirectory("Builds/CoastArtV4");
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{CoastBlockoutBuild.Scene},locationPathName="Builds/CoastArtV4/GuichaoBay-Art.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Coast material build failed");
   string prior="Builds/CoastBlockoutV1/ThirdParty",dest="Builds/CoastArtV4/ThirdParty";
   if(Directory.Exists(prior))foreach(var path in Directory.GetFiles(prior,"*",SearchOption.AllDirectories)){var target=dest+path.Substring(prior.Length);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,true);}
   Debug.Log("COAST_MATERIALS_BUILD_OK");
  }
  static Texture2D CausticTexture()
  {
   const int size=512,cells=8;string path=Root+"CausticLight.png";
   var tex=new Texture2D(size,size,TextureFormat.RGB24,false,true);var pixels=new Color32[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float u=x/(float)size*cells,v=y/(float)size*cells;int ix=Mathf.FloorToInt(u),iy=Mathf.FloorToInt(v);float first=99,second=99;
    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
     int cx=ix+dx,cy=iy+dy,wx=(cx+cells)%cells,wy=(cy+cells)%cells;
     uint h=(uint)(wx*374761393+wy*668265263);h=(h^(h>>13))*1274126177u;
     float px=cx+.18f+(h&65535)/65535f*.64f,py=cy+.18f+((h>>16)&65535)/65535f*.64f;
     float d=(u-px)*(u-px)+(v-py)*(v-py);
     if(d<first){second=first;first=d;}else if(d<second)second=d;
    }
    float edge=Mathf.Sqrt(second)-Mathf.Sqrt(first);byte light=(byte)(Mathf.Pow(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/.13f)),2)*255);
    pixels[y*size+x]=new Color32(light,light,light,255);
   }
   tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static Material Surface(string name,Color color,float kind)
  {var m=Get(name,"Deepsea/CoastFoundation");m.SetColor("_BaseColor",color);m.SetFloat("_Kind",kind);m.SetFloat("_BumpStrength",.19f);m.SetFloat("_Scale",.28f);EditorUtility.SetDirty(m);return m;}
  static Material Get(string name,string shader)
  {string path=Root+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}else m.shader=Shader.Find(shader);return m;}
  static void Include(string name)
  {
   var shader=Shader.Find(name);if(shader==null)throw new Exception("Shader missing "+name);
   foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+message.message);
   var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=graphics.FindProperty("m_AlwaysIncludedShaders");
   for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)return;
   int n=list.arraySize;list.InsertArrayElementAtIndex(n);list.GetArrayElementAtIndex(n).objectReferenceValue=shader;graphics.ApplyModifiedPropertiesWithoutUndo();
  }
 }
}
