using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
public sealed class CoastHabitat:MonoBehaviour {
 public bool FoliageVisible=true;
 public bool Ready{get;private set;}public bool Enabled{get;private set;}=true;
 public int Grass,Shrubs,Triangles,DrawGroups;public float MinimumPathClearance=1000;public int AddedColliders{get{return 0;}}
 CoastBlockoutRegion region;CoastMaterialsAppearance look;CoastEcology ecology;Mesh grass,shrub;Material grassMat,shrubMat;Matrix4x4[] grassMatrices,shrubMatrices;
 readonly System.Random rng=new System.Random(8832701);
 float R(float a,float b){return Mathf.Lerp(a,b,(float)rng.NextDouble());}
 public void Initialize(CoastBlockoutRegion area,CoastMaterialsAppearance appearance){
  region=area;look=appearance;ecology=GetComponent<CoastEcology>();grass=Resources.Load<Mesh>("CoastHabitat/Grass");shrub=Resources.Load<Mesh>("CoastHabitat/Shrub");grassMat=Resources.Load<Material>("CoastHabitat/Grass");shrubMat=Resources.Load<Material>("CoastHabitat/Shrub");
  if(grass==null||shrub==null||grassMat==null||shrubMat==null)throw new InvalidOperationException("Habitat resources missing");
  var g=new List<Matrix4x4>();var s=new List<Matrix4x4>();
  // Clustered tufts stop outside the complete walking strip, not just the centre points.
  for(float z=72;z<116;z+=.72f)for(float x=12;x<CoastBlockoutLayout.Shore(z)-1;x+=.72f){
   if(g.Count>=780)continue;float px=x+R(-.27f,.27f),pz=z+R(-.27f,.27f);Color c=CoastHabitatLayout.Control(px,pz);
   if(c.a<.32f||R(0,1)>c.a*.68f)continue;Vector3 p;Vector3 normal;if(!Ground(px,pz,out p,out normal))continue;
   float clear=CoastBlockoutLayout.PathDistance(p);if(clear<2.1f)continue;
   float h=R(.32f,.62f),width=h*R(1.7f,2.4f);g.Add(Matrix(p,normal,h,width,grass.bounds));MinimumPathClearance=Mathf.Min(MinimumPathClearance,clear);
  }
  float[] bands={76,81,86,101,106,112};foreach(float z in bands)for(int i=0;i<9;i++){
   float pz=z+R(-2.4f,2.4f),px=CoastBlockoutLayout.Shore(pz)-R(4.4f,14.5f);Color c=CoastHabitatLayout.Control(px,pz);
   if(c.a<.33f)continue;Vector3 p,normal;if(!Ground(px,pz,out p,out normal))continue;float clear=CoastBlockoutLayout.PathDistance(p);
   if(clear<3||Vector2.Distance(new Vector2(px,pz),new Vector2(30,93))<4.7f)continue;
   s.Add(Matrix(p,normal,R(1.05f,1.65f),R(1.4f,2.1f),shrub.bounds));MinimumPathClearance=Mathf.Min(MinimumPathClearance,clear);
  }
  grassMatrices=g.ToArray();shrubMatrices=s.ToArray();Grass=g.Count;Shrubs=s.Count;Triangles=Grass*grass.triangles.Length/3+Shrubs*shrub.triangles.Length/3;DrawGroups=(Grass>0?1:0)+(Shrubs>0?1:0);Ready=true;SetEnabled(true);
  Debug.Log("COAST_HABITAT_READY grass="+Grass+" shrubs="+Shrubs+" triangles="+Triangles+" clearance="+MinimumPathClearance+" colliders=0");
 }
 bool Ground(float x,float z,out Vector3 p,out Vector3 normal){p=Vector3.zero;normal=Vector3.up;float best=-100;foreach(var hit in Physics.RaycastAll(new Vector3(x,22,z),Vector3.down,28))if(hit.transform.IsChildOf(region.transform)&&hit.point.y>best){best=hit.point.y;p=hit.point;normal=hit.normal;}return best>.72f&&best<7.2f&&normal.y>.70f;}
 Matrix4x4 Matrix(Vector3 p,Vector3 normal,float height,float width,Bounds b){float scale=Mathf.Min(height/Mathf.Max(.01f,b.size.y),width/Mathf.Max(.01f,Mathf.Max(b.size.x,b.size.z)));Quaternion tilt=Quaternion.FromToRotation(Vector3.up,Vector3.Lerp(Vector3.up,normal,.45f));return Matrix4x4.TRS(p-Vector3.up*.018f,tilt*Quaternion.Euler(0,R(0,360),0),Vector3.one*scale)*Matrix4x4.Translate(-new Vector3(b.center.x,b.min.y,b.center.z));}
 public void SetEnabled(bool value){Enabled=value;Shader.SetGlobalFloat("_CoastHabitatDetails",value?1:0);}
 void LateUpdate(){if(!Ready)return;if(Input.GetKeyDown(KeyCode.F3))SetEnabled(!Enabled);if(!Enabled||!FoliageVisible||look.Grey||(ecology!=null&&!ecology.Visible))return;if(Grass>0)Graphics.DrawMeshInstanced(grass,0,grassMat,grassMatrices,Grass,null,ShadowCastingMode.Off,true,0,null,LightProbeUsage.Off);if(Shrubs>0)Graphics.DrawMeshInstanced(shrub,0,shrubMat,shrubMatrices,Shrubs,null,ShadowCastingMode.Off,true,0,null,LightProbeUsage.Off);}
 void OnDestroy(){Shader.SetGlobalFloat("_CoastHabitatDetails",0);}
}
}
