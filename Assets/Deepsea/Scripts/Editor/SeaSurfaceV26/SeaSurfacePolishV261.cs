using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace Deepsea.Editor {
 public static class SeaSurfacePolishV261 {
  public static void SyncAndBuild(){
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   var shader=Shader.Find("Deepsea/SeaSurfaceSparkleV26");if(shader==null||!shader.isSupported)throw new Exception("v26.1 shader unavailable");
   var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();if(errors.Length>0)throw new Exception(string.Join("; ",errors));
   var material=Resources.Load<Material>("SeaSurfaceV26/Water");if(material==null)throw new Exception("Existing resource material missing");material.shader=shader;
   string source=File.ReadAllText("Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaWaterArtUpgradeV26.cs");
   foreach(Match m in Regex.Matches(source,@"_v26WaterMaterial\.SetFloat\(""([^""]+)"",\s*([0-9.]+)f\)"))material.SetFloat(m.Groups[1].Value,float.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture));
   foreach(Match m in Regex.Matches(source,@"_v26WaterMaterial\.SetColor\(""([^""]+)"", new Color\(([^)]+)\)")){
    var values=m.Groups[2].Value.Split(',').Select(x=>float.Parse(x.Trim().TrimEnd('f'),CultureInfo.InvariantCulture)).ToArray();material.SetColor(m.Groups[1].Value,new Color(values[0],values[1],values[2],values[3]));
   }
   material.SetTexture("_CausticsMap",Resources.Load<Texture2D>("SeaSurfaceV26/Textures/WaterCaustics_V26"));
   material.SetTexture("_FoamMap",Resources.Load<Texture2D>("SeaSurfaceV26/Textures/ShoreFoamNoise_V26"));
   material.SetTexture("_DepthRampMap",Resources.Load<Texture2D>("SeaSurfaceV26/Textures/WaterDepthRamp_V26"));material.SetFloat("_UseDepthRamp",1);
   material.SetTexture("_WaveMap",Resources.Load<Material>("SeaSurfaceV24/Water").GetTexture("_WaveMap"));
   EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
   foreach(var pair in new[]{new Vector2(1.68f,material.GetFloat("_CausticsIntensity")),new Vector2(3.45f,material.GetFloat("_GlintIntensity")),new Vector2(1.28f,material.GetFloat("_FoamIntensity"))})if(Mathf.Abs(pair.x-pair.y)>.00001f)throw new Exception("Serialized optical parameter mismatch");
   Debug.Log("WATER_V261_MATERIAL_SYNC_OK");SeaSurfaceBuildV26.Build();
  }
 }
}
