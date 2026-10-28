using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
// Decorative scanned rock only. All accepted terrain meshes and colliders remain untouched.
public sealed class CoastShoreFinish:MonoBehaviour {
 public bool Ready{get;private set;}public bool Visible=true;
 public int RockCount,Triangles;public float MinimumVisualPathClearance=1000;public int AddedColliders{get{return 0;}}
 CoastMaterialsAppearance look;Mesh mesh;Material material;Matrix4x4[] matrices;
 readonly System.Random random=new System.Random(938417);
 float R(float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
 public void Initialize(CoastBlockoutRegion region,CoastMaterialsAppearance appearance){
  look=appearance;mesh=Resources.Load<Mesh>("CoastMaterials/RockSurface");material=Resources.Load<Material>("CoastMaterials/RockSurface");
  if(mesh==null||material==null)throw new InvalidOperationException("Shore finish requires existing scanned rock");
  var items=new List<Matrix4x4>();
  // Follow BOTH terrain shelf edges. Each scanned volume overlaps its neighbours and the bank.
  foreach(float inset in new[]{10.6f,3.6f})for(float z=71;z<120;z+=2.7f){
   float x=CoastBlockoutLayout.Shore(z)-inset;
   float high=CoastBlockoutLayout.TerrainHeight(x-.85f,z),low=CoastBlockoutLayout.TerrainHeight(x+.95f,z);
   float drop=high-low;if(drop<.8f||low<.1f)continue;
   float width=R(3.2f,4.3f),length=R(3.65f,4.9f),yaw=R(-24,24);
   // Broad overlapping lower/upper layers expose a rock bank rather than a row of upright spikes.
   Add(items,new Vector3(x-.28f,low-.28f,z+R(-.25f,.25f)),new Vector3(width,drop*.67f,length),yaw);
   Add(items,new Vector3(x-.66f,low+drop*.29f,z+R(-.4f,.4f)),new Vector3(width*R(.95f,1.18f),drop*.62f,length*R(.90f,1.12f)),yaw+R(-15,15));
  }
  matrices=items.ToArray();RockCount=matrices.Length;Triangles=mesh.triangles.Length/3*RockCount;Ready=true;
  Debug.Log("COAST_SHORE_FINISH_READY rocks="+RockCount+" triangles="+Triangles+" clearance="+MinimumVisualPathClearance+" colliders=0");
 }
 void Add(List<Matrix4x4> items,Vector3 bottom,Vector3 size,float yaw){
  Bounds b=mesh.bounds;var m=Matrix4x4.TRS(bottom,Quaternion.Euler(0,yaw,0),Vector3.one)*Matrix4x4.Scale(new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z))*Matrix4x4.Translate(-new Vector3(b.center.x,b.min.y,b.center.z));
  float clearance=1000;
  // Check every rendered vertex rather than accepting a safe centre with protruding edges.
  foreach(var v in mesh.vertices){Vector3 p=m.MultiplyPoint3x4(v);clearance=Mathf.Min(clearance,CoastBlockoutLayout.PathDistance(p));if(p.x<10||p.x>42||p.z<68||p.z>123)return;if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(30,93))<4.1f)return;}
  if(clearance<2.6f)return;items.Add(m);MinimumVisualPathClearance=Mathf.Min(MinimumVisualPathClearance,clearance);
 }
 public void SetVisible(bool value){Visible=value;}
 void LateUpdate(){if(!Ready)return;if(Input.GetKeyDown(KeyCode.F8))Visible=!Visible;if(Visible&&!look.Grey&&matrices.Length>0)Graphics.DrawMeshInstanced(mesh,0,material,matrices,matrices.Length,null,ShadowCastingMode.On,true,0,null,LightProbeUsage.Off);}
}
}
