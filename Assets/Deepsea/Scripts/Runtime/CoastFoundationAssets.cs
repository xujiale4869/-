using System;
using System.Collections.Generic;
using UnityEngine;
namespace Deepsea {
 public sealed class CoastFoundationAssets:MonoBehaviour {
  public bool Ready;public int Triangles,MaterialGroups;public int AddedColliders{get{return root==null?0:root.GetComponentsInChildren<Collider>(true).Length;}}
  CoastMaterialsAppearance look;GameObject root;readonly List<Renderer> originals=new List<Renderer>();
  public void Initialize(CoastBlockoutRegion region,CoastMaterialsAppearance appearance){
   look=appearance;foreach(var r in region.lighthouse.GetComponentsInChildren<Renderer>(true))originals.Add(r);
   var source=Resources.Load<GameObject>("CoastFoundation/Lighthouse");if(source==null)throw new InvalidOperationException("Foundation lighthouse missing");
   root=Instantiate(source,region.lighthouse,false);root.transform.localPosition=Vector3.zero;root.transform.localRotation=Quaternion.Euler(0,180,0);root.transform.localScale=Vector3.one;
   foreach(var f in root.GetComponentsInChildren<MeshFilter>()){Triangles+=f.sharedMesh.triangles.Length/3;MaterialGroups++;var renderer=f.GetComponent<Renderer>();if(!renderer.sharedMaterial.shader.isSupported)throw new InvalidOperationException("Foundation shader unsupported");appearance.RegisterArtRenderer(renderer);}
   if(AddedColliders!=0)throw new InvalidOperationException("Foundation art added a collider");Ready=true;Apply();Debug.Log("COAST_FOUNDATION_READY triangles="+Triangles+" materials="+MaterialGroups);
  }
  void Apply(){if(!Ready)return;root.SetActive(!look.Grey);foreach(var r in originals)if(r!=null)r.enabled=look.Grey;}
  void LateUpdate(){Apply();}
  void OnDestroy(){foreach(var r in originals)if(r!=null)r.enabled=true;}
 }
}
