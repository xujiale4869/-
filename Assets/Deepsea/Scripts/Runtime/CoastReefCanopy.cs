using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea {
// Decorative habitat only: real 3D corals on the existing seabed, never a new collision surface.
public sealed class CoastReefCanopy:MonoBehaviour {
 public bool Ready,Visible=true;public int Corals,RockCaps,Triangles,Rejected,RouteSamples;
 public float HighestTip=-100,MinimumRouteClearance=1000;
 public int AddedColliders{get{return 0;}}
 CoastBlockoutRegion region;CoastMaterialsAppearance look;CoastEcology ecology;CoastRockSurfaces rockSurfaces;
 readonly List<CoastVisualSurface> capSurfaces=new List<CoastVisualSurface>();
 readonly System.Random rng=new System.Random(230611);
 readonly List<Set> groups=new List<Set>();
 sealed class Set {public Mesh mesh;public Material material;public Matrix4x4[] matrices;}
 float R(float a,float b){return Mathf.Lerp(a,b,(float)rng.NextDouble());}
 public void Initialize(CoastBlockoutRegion area,CoastMaterialsAppearance appearance,CoastEcology eco){
  region=area;look=appearance;ecology=eco;rockSurfaces=GetComponent<CoastRockSurfaces>();
  var material=Resources.Load<Material>("CoastCoral/Coral");
  if(material==null||!material.shader.isSupported)throw new InvalidOperationException("Coral material unavailable");
  string[] names={"EN_CoralBranch_LOD0","EN_CoralPlate_LOD0","EN_CoralBrain_LOD0","EN_CoralFan_LOD0","EN_CoralFlower_LOD0"};
  var meshes=new Mesh[names.Length];var clusters=new List<Matrix4x4>[names.Length];
  for(int i=0;i<names.Length;i++){meshes[i]=Resources.Load<Mesh>("CoastCoral/"+names[i]);clusters[i]=new List<Matrix4x4>();if(meshes[i]==null)throw new InvalidOperationException("Coral mesh missing: "+names[i]);}
  AddCaps();
  // Follow both shoulders of the ridge; change the patch centre as the reef bends.
  for(int patch=0;patch<19;patch++){
   float z=62+patch*3.8f;float x=CoastBlockoutLayout.Spine(z)-R(.6f,3.5f);
   Populate(meshes,clusters,new Vector2(x,z),patch,39,3.6f,3.2f);
   if(patch%3==1)Populate(meshes,clusters,new Vector2(CoastBlockoutLayout.FarSpine(z)+.4f,z),patch+11,22,3.2f,3.0f);
  }
  // Small, connected near-shore beds; keep the pier and open sand channels readable.
  foreach(var p in new[]{new Vector2(45,80),new Vector2(48,106),new Vector2(53,78),new Vector2(53,111)})Populate(meshes,clusters,p,20+(int)p.y,16,2.3f,3.1f);
  for(int i=0;i<names.Length;i++)groups.Add(new Set{mesh=meshes[i],material=material,matrices=clusters[i].ToArray()});
  Ready=true;
  Debug.Log("COAST_REEF_CANOPY_READY corals="+Corals+" caps="+RockCaps+" triangles="+Triangles+" clearance="+MinimumRouteClearance+" tip="+HighestTip+" colliders=0");
 }
 void Populate(Mesh[] meshes,List<Matrix4x4>[] clusters,Vector2 centre,int seed,int attempts,float rx,float rz){
  for(int i=0;i<attempts;i++){
   // Foundation rebuild: re-anchor the existing population, without increasing its cap.
   if(Corals>=512)return;
   float angle=R(0,Mathf.PI*2),radius=Mathf.Sqrt(R(0,1));
   Vector2 p=centre+new Vector2(Mathf.Cos(angle)*rx,Mathf.Sin(angle)*rz)*radius;
   if(radius>.65f&&R(0,1)>.60f)continue;
   Vector3 bed,normal;if(!Bed(p,out bed,out normal)||bed.y>-.95f||bed.y< -8.5f||normal.y<.48f)continue;
   if(Mathf.Abs(p.y-94)<3.4f&&p.x<72)continue;
   if(Vector2.Distance(p,new Vector2(CoastBlockoutLayout.CaveFocus.x,CoastBlockoutLayout.CaveFocus.z))<7.5f)continue;
   // A dominant species per patch avoids evenly scattered confetti colours.
   int species=(i%5<3)?seed%meshes.Length:(seed+i)%meshes.Length;
   var mesh=meshes[species];float scale=i%7==0?R(1.6f,2.25f):R(.62f,1.35f);
   float max=mesh.bounds.max.y*scale;float heightBudget=-.57f-bed.y;
   if(max>heightBudget)scale*=heightBudget/Mathf.Max(.01f,max);
   if(scale<.45f)continue;
   // The attachment plane follows the visible rock normal, so wide bases do not hover on slopes.
   Quaternion rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,R(0,360),0);
   float top=-100;foreach(var vertex in mesh.vertices)top=Mathf.Max(top,(rotation*(vertex*scale)).y);
   if(top>heightBudget)scale*=heightBudget/Mathf.Max(.01f,top);
   if(scale<.45f)continue;
   var matrix=Matrix4x4.TRS(bed-normal*.07f,rotation,Vector3.one*scale);
   if(!Safe(mesh,matrix)||Triangles+mesh.triangles.Length/3>590000)continue;
   clusters[species].Add(matrix);Corals++;Triangles+=mesh.triangles.Length/3;
   foreach(var vertex in mesh.vertices)HighestTip=Mathf.Max(HighestTip,matrix.MultiplyPoint3x4(vertex).y);
  }
 }
 bool Bed(Vector2 p,out Vector3 bed,out Vector3 normal){
  bed=new Vector3(p.x,-100,p.y);normal=Vector3.up;
  foreach(var hit in Physics.RaycastAll(new Vector3(p.x,20,p.y),Vector3.down,50))if(hit.transform.IsChildOf(region.transform)&&!(rockSurfaces!=null&&rockSurfaces.ReplacesAt(hit.transform,p))&&hit.point.y>bed.y){bed=hit.point;normal=hit.normal;}
  if(rockSurfaces!=null)rockSurfaces.SampleVisualTop(p,ref bed,ref normal);
  CoastVisualSurface.SampleAll(capSurfaces,p,ref bed,ref normal);
  return bed.y> -30;
 }
 bool Safe(Mesh mesh,Matrix4x4 matrix){
  // Validate the full visual volume against every animated fish route, not only placement centres.
  Bounds b=mesh.bounds;Vector3 centre=matrix.MultiplyPoint3x4(b.center);
  float radius=matrix.MultiplyVector(b.extents).magnitude;
  float clearance=ecology.VisualRouteClearance(centre)-radius;
  RouteSamples++;
  if(clearance<.72f){Rejected++;return false;}
  MinimumRouteClearance=Mathf.Min(MinimumRouteClearance,clearance);return true;
 }
 void AddCaps(){
  var mesh=Resources.Load<Mesh>("CoastMaterials/RockSurfaceLow");var material=Resources.Load<Material>("CoastMaterials/RockSurface");var matrices=new List<Matrix4x4>();
  for(int i=0;i<64;i++){
   float z=61+i*1.15f;float x=CoastBlockoutLayout.Spine(z)-R(1.0f,3.8f);Vector3 bed,normal;
   if(!Bed(new Vector2(x,z),out bed,out normal)||bed.y> -1.1f||bed.y< -7.5f||normal.y<.50f)continue;
   if(Mathf.Abs(z-94)<4.4f)continue;
   if(Vector2.Distance(new Vector2(x,z),new Vector2(CoastBlockoutLayout.CaveFocus.x,CoastBlockoutLayout.CaveFocus.z))<8)continue;
   var b=mesh.bounds;float h=Mathf.Min(R(.25f,.60f),-.62f-bed.y);
   var size=new Vector3(R(1.9f,3.2f),h,R(2.0f,3.5f));
   var matrix=Matrix4x4.TRS(bed-Vector3.up*.15f,Quaternion.Euler(0,R(-40,40),0),Vector3.one)*Matrix4x4.Scale(new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z))*Matrix4x4.Translate(-new Vector3(b.center.x,b.min.y,b.center.z));
   if(!Safe(mesh,matrix))continue;matrices.Add(matrix);capSurfaces.Add(new CoastVisualSurface(mesh,matrix));RockCaps++;Triangles+=mesh.triangles.Length/3;
  }
  groups.Add(new Set{mesh=mesh,material=material,matrices=matrices.ToArray()});
 }
 void LateUpdate(){
  if(!Ready||!Visible||look.Grey||!ecology.Visible)return;
  foreach(var g in groups)if(g.matrices.Length>0)Graphics.DrawMeshInstanced(g.mesh,0,g.material,g.matrices,g.matrices.Length,null,ShadowCastingMode.Off,true,0,null,LightProbeUsage.Off);
 }
}
}
