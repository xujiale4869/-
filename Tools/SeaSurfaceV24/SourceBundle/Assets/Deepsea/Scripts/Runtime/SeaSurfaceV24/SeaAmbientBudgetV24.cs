using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Rendering.Universal;
namespace Deepsea {
 public sealed partial class SeaSurfaceV24 {
  sealed class AmbientSetting {public object settings;public FieldInfo blur;public object original;}
  readonly List<AmbientSetting> ambientSettings=new List<AmbientSetting>();bool ambientBudget;
  void SetupAmbientBudget(){foreach(var feature in rendererData.rendererFeatures){if(!feature.GetType().Name.Contains("AmbientOcclusion"))continue;var field=feature.GetType().GetField("m_Settings",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);var s=field==null?null:field.GetValue(feature);if(s==null)continue;var blur=s.GetType().GetField("BlurQuality",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(blur!=null)ambientSettings.Add(new AmbientSetting{settings=s,blur=blur,original=blur.GetValue(s)});}}
  // Keep the same occlusion samples, normals and intensity; use the existing single-pass blur.
  void SetAmbientBudget(bool on){ambientBudget=on;foreach(var s in ambientSettings)s.blur.SetValue(s.settings,on?Enum.ToObject(s.blur.FieldType,2):s.original);}
 }
}
