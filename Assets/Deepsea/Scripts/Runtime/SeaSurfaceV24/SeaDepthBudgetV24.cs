using System.Collections.Generic;
using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaSurfaceV24 {
  sealed class MaterialBinding {public Renderer renderer;public Material[] original,budget;}
  readonly List<MaterialBinding> materialBindings=new List<MaterialBinding>();readonly List<Material> budgetMaterials=new List<Material>();
  bool depthBudget;
  void SetupDepthBudget(){
   var shaders=new Dictionary<string,Shader>();foreach(var n in new[]{"CoastFoundation","CoastMapleSurface","CoastShelfLit","CoastShowcaseSurface"}){var m=Resources.Load<Material>("SeaSurfaceV24/Budget"+n);if(m!=null)shaders["Deepsea/"+n]=m.shader;}
   var cache=new Dictionary<Material,Material>();
   foreach(var r in game.world.GetComponentsInChildren<Renderer>(true)){var original=r.sharedMaterials;var next=(Material[])original.Clone();bool changed=false;
    for(int i=0;i<original.Length;i++){var m=original[i];if(m==null||!shaders.ContainsKey(m.shader.name))continue;Material replacement;if(!cache.TryGetValue(m,out replacement)){replacement=new Material(m){name=m.name+" · 水下细节预算",shader=shaders[m.shader.name]};cache[m]=replacement;budgetMaterials.Add(replacement);}next[i]=replacement;changed=true;}
    if(changed)materialBindings.Add(new MaterialBinding{renderer=r,original=original,budget=next});
   }
  }
  void SetDepthBudget(bool on){depthBudget=on;Shader.SetGlobalFloat("_OceanV24UnderLOD",on?1:0);foreach(var b in materialBindings)if(b.renderer!=null)b.renderer.sharedMaterials=on?b.budget:b.original;}
  void DisposeDepthBudget(){SetDepthBudget(false);foreach(var m in budgetMaterials)if(m!=null)Destroy(m);}
 }
}
