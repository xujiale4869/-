using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace Deepsea.Editor {
 public static class SeaSurfacePolishV262 {
  const string Controller="Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaWaterArtUpgradeV26.cs";
  public static void SyncAndBuild(){Sync();SeaSurfaceBuildV26.Build();}
  public static void Sync(){
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   var shader=Shader.Find("Deepsea/SeaSurfaceSparkleV26");
   if(shader==null||!shader.isSupported)throw new Exception("v26.2 shader unavailable");
   var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
   if(errors.Length>0)throw new Exception(string.Join("; ",errors));
   var material=Resources.Load<Material>("SeaSurfaceV26/Water");
   if(material==null)throw new Exception("Existing water resource material missing");
   material.shader=shader;string source=File.ReadAllText(Controller);
   var expected=new Dictionary<string,float>();
   foreach(Match m in Regex.Matches(source,@"_v26WaterMaterial\.SetFloat\(""([^""]+)"",\s*([0-9.]+)f\)"))
    expected[m.Groups[1].Value]=float.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture);
   var defaults=Regex.Matches(source,@"public float (\w+)\s*=\s*([0-9.]+)f;").Cast<Match>().ToDictionary(m=>m.Groups[1].Value,m=>float.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture));
   foreach(Match m in Regex.Matches(source,@"_v26WaterMaterial\.SetFloat\(""([^""]+)"",\s*(\w+)\)")){
    float value;if(defaults.TryGetValue(m.Groups[2].Value,out value))expected[m.Groups[1].Value]=value;
   }
   if(expected.Count!=14||!expected.ContainsKey("_GlintFocus"))throw new Exception("v26.2 optical defaults could not be resolved");
   foreach(var setting in expected){if(!material.HasProperty(setting.Key))throw new Exception("Shader property missing "+setting.Key);material.SetFloat(setting.Key,setting.Value);}
   foreach(Match m in Regex.Matches(source,@"_v26WaterMaterial\.SetColor\(""([^""]+)"", new Color\(([^)]+)\)")){
    var c=m.Groups[2].Value.Split(',').Select(x=>float.Parse(x.Trim().TrimEnd('f'),CultureInfo.InvariantCulture)).ToArray();
    material.SetColor(m.Groups[1].Value,new Color(c[0],c[1],c[2],c[3]));
   }
   // Keep the verified texture import settings and references; require the full existing bundle.
   foreach(var name in new[]{"_WaveMap","_CausticsMap","_FoamMap","_DepthRampMap"})
    if(material.GetTexture(name)==null)throw new Exception("Existing optical texture missing "+name);
   material.SetFloat("_UseDepthRamp",1);
   foreach(var setting in expected)if(Mathf.Abs(material.GetFloat(setting.Key)-setting.Value)>.00001f)throw new Exception("Serialized setting mismatch "+setting.Key);
   EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();Debug.Log("WATER_V262_MATERIAL_SYNC_OK");
  }
 }
}
