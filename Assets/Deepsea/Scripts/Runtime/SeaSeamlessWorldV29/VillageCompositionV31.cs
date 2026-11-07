using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Deepsea {public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
 readonly List<Transform> courtsV31=new List<Transform>();
 readonly Dictionary<Transform,int> morphologyV31=new Dictionary<Transform,int>();
 readonly List<Vector3> slipwayRouteV31=new List<Vector3>();
 Material hullAccentV31,marketFishV31;
 Material HullAccentV31(){if(hullAccentV31==null){hullAccentV31=new Material(_v31VillageWoodMat){name="V31_LightRepairHullTimber"};hullAccentV31.SetColor("_BaseColor",new Color(.94f,.77f,.50f));_owned.Add(hullAccentV31);}return hullAccentV31;}
 void BuildCourtyardV31(Transform house,int kind){
  if(kind==2)return;
  var court=new GameObject("V31_SupportedForecourt").transform;court.SetParent(house,false);courtsV31.Add(court);
  float front=kind==1?3.1f:2.7f,width=kind==1?6.0f:4.8f;const int count=12;float length=4.2f;
  var tops=new float[count];
  for(int i=0;i<count;i++){float z=front+.35f+i*length/(count-1);tops[i]=.56f;foreach(float x in new[]{-width*.5f,0,width*.5f}){var p=house.TransformPoint(new Vector3(x,0,z));tops[i]=Mathf.Max(tops[i],V31Ground(p.x,p.z)-house.position.y+.12f);}}
  for(int i=count-2;i>=0;i--)tops[i]=Mathf.Max(tops[i],tops[i+1]-.16f);
  for(int i=1;i<count;i++)tops[i]=Mathf.Max(tops[i],tops[i-1]-.16f);
  for(int i=0;i<count;i++){
   float z=front+.35f+i*length/(count-1);AddBoxPart("ForecourtTread_"+i,court,new Vector3(0,tops[i]-.10f,z),new Vector3(width,.20f,length/(count-1)+.02f),Quaternion.identity,_v31VillageWoodMat,true);
   if(i%3==0||i==count-1)foreach(float x in new[]{-width*.46f,width*.46f}){
    var p=house.TransformPoint(new Vector3(x,0,z));float floor=V31Ground(p.x,p.z)-house.position.y-.08f,h=Mathf.Max(.18f,tops[i]-floor);
    AddBoxPart("ForecourtSupport",court,new Vector3(x,floor+h*.5f,z),new Vector3(.17f,h,.17f),Quaternion.identity,_v31VillageWoodMat,true);
    AddBoxPart("ForecourtRailPost",court,new Vector3(x,tops[i]+.43f,z),new Vector3(.11f,.86f,.11f),Quaternion.identity,_v31VillageWoodMat,false);
   }
  }
  for(int i=0;i<count-1;i++)foreach(float sign in new[]{-1f,1f}){var a=new Vector3(sign*width*.46f,tops[i]+.78f,front+.35f+i*length/(count-1));var b=new Vector3(sign*width*.46f,tops[i+1]+.78f,front+.35f+(i+1)*length/(count-1));AddBoxPart("ForecourtHandrail",court,(a+b)*.5f,new Vector3(.075f,.075f,Vector3.Distance(a,b)+.02f),Quaternion.LookRotation(b-a),_v31VillageWoodMat,false);}
  if(kind==1){
   float deck=tops[Mathf.Clamp(Mathf.RoundToInt((3.45f-.35f)/length*(count-1)),0,count-1)],z=front+3.45f;
   foreach(float x in new[]{-.85f,.85f})AddBoxPart("MarketRackPost",court,new Vector3(x,deck+.92f,z),new Vector3(.10f,1.84f,.10f),Quaternion.identity,_v31VillageWoodMat,false);
   AddBoxPart("MarketRackBeam",court,new Vector3(0,deck+1.85f,z),new Vector3(1.95f,.12f,.12f),Quaternion.identity,_v31VillageWoodMat,false);
   var fish=Resources.Load<Mesh>("SeaLifeV25/Meshes/Fish_0_SilverBream");var original=Resources.Load<Material>("SeaLifeV25/Materials/Fish0");
   if(fish==null||original==null)throw new InvalidOperationException("Canonical market fish resource missing");
   marketFishV31=new Material(original){name="V31_StaticFreshFishRack"};marketFishV31.SetFloat("_TailAmplitude",0);marketFishV31.SetFloat("_Smoothness",.35f);marketFishV31.SetColor("_BaseColor",new Color(.82f,.80f,.70f));_owned.Add(marketFishV31);
   for(int i=0;i<3;i++){float x=-.62f+i*.62f;AddBoxPart("MarketHangingCord",court,new Vector3(x,deck+1.58f,z),new Vector3(.025f,.46f,.025f),Quaternion.identity,_v31VillageWoodMat,false);var f=AddMesh("CanonicalFishForTrade_"+i,fish,marketFishV31,new Vector3(x,deck+.98f,z),Vector3.one*.68f,false,court);f.transform.localRotation=Quaternion.Euler(90,0,i*8-8);}
  }
  MergeStaticRenderersV31(court);
 }
 void MergeStaticRenderersV31(Transform parent){
  var all=parent.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
  foreach(var group in all.GroupBy(r=>r.sharedMaterial)){
   var parts=new List<CombineInstance>();foreach(var r in group){var mesh=r.GetComponent<MeshFilter>().sharedMesh;for(int s=0;s<mesh.subMeshCount;s++)parts.Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix});}
   var merged=new Mesh{name="V31_Static_"+parent.name+"_"+group.Key.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};merged.CombineMeshes(parts.ToArray(),true,true);_owned.Add(merged);AddMesh(merged.name,merged,group.Key,Vector3.zero,Vector3.one,false,parent);foreach(var r in group)r.enabled=false;
  }
 }
 Bounds[] ShoreObstaclesV31(bool visibility){
  return game.world.GetComponentsInChildren<MeshRenderer>().Where(r=>{
   // Include static LOD renderers before the first camera selects a level.
   if(r.transform.IsChildOf(_v31Root)||r==waterFilter.GetComponent<Renderer>()||r.transform.IsChildOf(game.boat)||r.transform.IsChildOf(game.walker))return false;
   string n=(r.name+" "+(r.sharedMaterial!=null?r.sharedMaterial.name:"")).ToLowerInvariant();var b=r.bounds;
   if(n.Contains("water")||n.Contains("groundleaves")||n.Contains("shadow"))return false;
   return b.max.y>SeaLifeRuntime.Sea+.25f&&b.size.x<30&&b.size.z<30&&b.size.y>.25f;
  }).Select(r=>r.bounds).ToArray();
 }
 float RouteFloorV31(Vector3 p,Bounds[] solids){float y=Mathf.Max(SeaLifeRuntime.Sea+.60f,V31Ground(p.x,p.z)+.14f);foreach(var b in solids)if(p.x>b.min.x-1&&p.x<b.max.x+1&&p.z>b.min.z-1&&p.z<b.max.z+1)y=Mathf.Max(y,b.max.y+.18f);return y;}
 bool TrySlipwayRouteV31(float x,float z,Bounds[] solids,out List<Vector3> points){
  var end=new Vector3(x,SeaLifeRuntime.Sea+.60f,z)-Quaternion.Euler(0,78,0)*new Vector3(0,0,1.5f);var start=new Vector3(ShoreX(end.z)-8,0,end.z);int count=Mathf.Max(5,Mathf.CeilToInt(Vector3.Distance(new Vector3(start.x,0,start.z),new Vector3(end.x,0,end.z))/.8f));points=new List<Vector3>();
  for(int i=0;i<=count;i++){var p=Vector3.Lerp(start,end,i/(float)count);p.y=RouteFloorV31(p,solids);points.Add(p);}float first=points[0].y;var last=points[count];last.y=end.y;points[count]=last;
  for(int i=count-1;i>=0;i--){var p=points[i];p.y=Mathf.Max(p.y,points[i+1].y-.18f);points[i]=p;}
  for(int i=1;i<=count;i++){var p=points[i];p.y=Mathf.Max(p.y,points[i-1].y-.18f);points[i]=p;}
  return points[count].y<=end.y+.025f&&points[0].y<=first+.25f;
 }
 bool FindSlipwaySiteV31(float preferred,out float x,out float z,out float depth,out List<Vector3> route){
  var solids=ShoreObstaclesV31(true);x=z=depth=0;route=null;float best=float.PositiveInfinity;
  foreach(float dz in new[]{0f,-2,2,-4,4,-6,6,-10,10,-14,14,-18,18}){float zz=preferred+dz;for(float d=12;d<=55;d+=.75f){float xx=ShoreX(zz)+d,dep=SeaLifeRuntime.Sea-V31Ground(xx,zz);if(dep<.55f||dep>2.5f)continue;
   var envelope=new Bounds(new Vector3(xx,SeaLifeRuntime.Sea+.6f,zz),new Vector3(17,5,17));if(solids.Any(b=>b.Intersects(envelope)))continue;
   List<Vector3> candidate;if(!TrySlipwayRouteV31(xx,zz,solids,out candidate))continue;float cost=d+8+Mathf.Abs(dz)*1.25f+candidate.Max(p=>p.y)*2;if(cost>=best)continue;best=cost;x=xx;z=zz;depth=dep;route=candidate;
  }}return route!=null;
 }
 void BuildSlipwayBridgeV31(List<Vector3> route){
  var root=new GameObject("V31_SupportedShoreAccess").transform;root.SetParent(_v31Root,false);slipwayRouteV31.AddRange(route);
  for(int i=0;i<route.Count-1;i++){
   var a=route[i];var b=route[i+1];var p=(a+b)*.5f;var d=b-a;float length=new Vector2(d.x,d.z).magnitude;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
   AddBoxPart("ShoreAccessTread_"+i,root,p-Vector3.up*.10f,new Vector3(1.85f,.2f,length+.025f),Quaternion.Euler(0,yaw,0),_v31VillageWoodMat,true);
   var railSide=Quaternion.Euler(0,yaw,0)*Vector3.right;foreach(float sign in new[]{-1f,1f})AddBoxPart("ShoreAccessHandrail",root,p+railSide*.78f*sign+Vector3.up*.78f,new Vector3(.075f,.075f,Vector3.Distance(a,b)+.02f),Quaternion.LookRotation(d),_v31VillageWoodMat,false);
   if(i%4==0){var side=Quaternion.Euler(0,yaw,0)*Vector3.right;foreach(float sign in new[]{-1f,1f}){var at=p+side*.78f*sign;float floor=V31Ground(at.x,at.z)-.12f,h=Mathf.Max(.25f,p.y-floor);AddBoxPart("ShoreAccessPile",root,new Vector3(at.x,floor+h*.5f,at.z),new Vector3(.16f,h,.16f),Quaternion.identity,_v31VillageWoodMat,true);AddBoxPart("ShoreAccessRail",root,at+Vector3.up*.42f,new Vector3(.1f,.84f,.1f),Quaternion.identity,_v31VillageWoodMat,false);}}
  }
  MergeStaticRenderersV31(root);
 }
 void SelectMorphologyViewsV31(){
  for(int kind=0;kind<6;kind++){
   Transform best=null;float score=float.NegativeInfinity;
   foreach(var pair in morphologyV31){if(pair.Value!=kind)continue;var t=pair.Key;float nearest=20;foreach(var other in floraV31)if(other!=t)nearest=Mathf.Min(nearest,Vector3.Distance(t.position,other.position));var b=t.GetComponent<Renderer>().bounds;float candidate=nearest*3+Mathf.Min(2.5f,b.size.y)*.4f;if(candidate>score){score=candidate;best=t;}}
   samplesV31[kind]=best;
  }
 }
}}
