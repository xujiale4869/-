using System.Collections.Generic;
using UnityEngine;
namespace Deepsea {
/// <summary>Local original material polish. Collision and playable terrain are preserved.</summary>
public sealed class CoastalDetail : MonoBehaviour {
 public static bool Enabled=true;
 readonly List<Material> materials=new List<Material>();
 readonly List<Renderer> renderers=new List<Renderer>();readonly List<Material> previous=new List<Material>();
 public bool Ready {get;private set;}
 public int TimberRenderers {get;private set;}
 public int CollidersBefore {get;private set;}
 public int CollidersAfter {get;private set;}
 static int CollisionCount(Transform root){int n=0;foreach(var c in root.GetComponentsInChildren<Collider>())if(c.enabled)n++;return n;}
 public void Initialize(GameDirector game){
  Shader.SetGlobalFloat("_BayCoastDetail",Enabled?1:0);
  CollidersBefore=CollisionCount(game.World.Root);
  if(Enabled)foreach(var r in game.World.Root.GetComponentsInChildren<MeshRenderer>()){
   Vector3 p=r.bounds.center;
   if(p.x<36||p.x>56||p.z<90||p.z>98)continue;
   string n=r.gameObject.name;if(n!="铺板"&&n!="桥桩"&&n!="平台结构"&&n!="木板缓坡")continue;
   var old=r.sharedMaterial;
   var m=new Material(Shader.Find("Deepsea/CoastalTimber")){name="东岸栈桥 · 细化木面"};
   m.SetColor("_BaseColor",old!=null&&old.HasProperty("_BaseColor")?old.GetColor("_BaseColor"):VisualFactory.PaleWood);
   m.SetFloat("_BoardSeed",p.x*1.731f+p.z*.631f);renderers.Add(r);previous.Add(old);materials.Add(m);r.sharedMaterial=m;TimberRenderers++;
  }
  CollidersAfter=CollisionCount(game.World.Root);Ready=true;
 }
 void OnDestroy(){for(int i=0;i<renderers.Count;i++)if(renderers[i]!=null)renderers[i].sharedMaterial=previous[i];foreach(var m in materials)if(m!=null)Destroy(m);Shader.SetGlobalFloat("_BayCoastDetail",0);}
}
}
