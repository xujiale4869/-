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
   Material rock=Surface("Rock",new Color(.88f,.92f,.92f),0);
   rock.SetTexture("_BaseMap",stone.GetTexture("_BaseMap"));rock.SetTexture("_BumpMap",stone.GetTexture("_BumpMap"));rock.SetFloat("_Moss",.18f);rock.SetFloat("_TextureContrast",.72f);
   var ground=Surface("Ground",new Color(.88f,.92f,.92f),1);ground.CopyPropertiesFromMaterial(rock);ground.SetFloat("_Kind",1);ground.SetTexture("_SandMap",sand.GetTexture("_BaseMap"));ground.SetColor("_SandColor",new Color(.88f,.87f,.77f));
   var wood=Surface("Wood",new Color(.83f,.68f,.48f),2);wood.SetTexture("_BaseMap",timber.GetTexture("_BaseMap"));wood.SetFloat("_ObjectMapping",1);wood.SetFloat("_Scale",.55f);wood.SetFloat("_TextureContrast",.70f);
   Surface("Plaster",new Color(.72f,.68f,.53f),3);Surface("Metal",new Color(.12f,.16f,.17f),3);
   Surface("Coat",new Color(.10f,.25f,.33f),3);Surface("Skin",new Color(.60f,.42f,.24f),3);Surface("Fish",new Color(.30f,.48f,.53f),3);
   var water=Get("Water","Deepsea/CoastFoundationWater");water.SetColor("_Shallow",new Color(.12f,.66f,.66f));water.SetColor("_Deep",new Color(.018f,.12f,.21f));
   var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"Look.asset");
   if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"Look.asset");}
   Tonemapping tone;if(!profile.TryGet(out tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
   ColorAdjustments color;if(!profile.TryGet(out color)){color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(.22f);color.contrast.Override(5);color.saturation.Override(-4);
   EditorUtility.SetDirty(profile);
   foreach(string name in new[]{"Deepsea/CoastFoundation","Deepsea/CoastFoundationWater"})Include(name);
   foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Root+"Look.asset"))EditorUtility.SetDirty(asset);
   foreach(var m in new[]{rock,ground,wood,water})EditorUtility.SetDirty(m);
   AssetDatabase.SaveAssets();PlayerSettings.enableFrameTimingStats=true;
   Directory.CreateDirectory("Builds/CoastMaterialsV1");
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{CoastBlockoutBuild.Scene},locationPathName="Builds/CoastMaterialsV1/GuichaoBay-Materials.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Coast material build failed");
   string prior="Builds/CoastBlockoutV1/ThirdParty",dest="Builds/CoastMaterialsV1/ThirdParty";
   if(Directory.Exists(prior))foreach(var path in Directory.GetFiles(prior,"*",SearchOption.AllDirectories)){var target=dest+path.Substring(prior.Length);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,true);}
   Debug.Log("COAST_MATERIALS_BUILD_OK");
  }
  static Material Surface(string name,Color color,float kind)
  {var m=Get(name,"Deepsea/CoastFoundation");m.SetColor("_BaseColor",color);m.SetFloat("_Kind",kind);m.SetFloat("_BumpStrength",.28f);m.SetFloat("_Scale",.36f);EditorUtility.SetDirty(m);return m;}
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
