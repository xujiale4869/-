using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
// Render a UV-preserving surface over each accepted scan proxy. Physics keeps its original mesh.
public sealed class CoastRockSurfaces:MonoBehaviour {
 public int Surfaces,SmallSurfaces,Triangles,ReshapedReefs,GeneratedVariants,GeneratedInstances;public float FitError;public bool Ready;
 CoastShelfLandform shelf;CoastMaterialsAppearance look;Mesh mesh,lowMesh;Material material;Matrix4x4[] matrices,smallMatrices;
 Mesh[] reefMeshes;Material reefMaterial;Matrix4x4[][] reefMatrices;
 readonly List<Renderer> originals=new List<Renderer>();
 readonly HashSet<Transform> replacedTransforms=new HashSet<Transform>();
 readonly List<CoastVisualSurface> visualSurfaces=new List<CoastVisualSurface>();
 public bool Replaces(Transform item){return replacedTransforms.Contains(item);}
 public bool ReplacesAt(Transform item,Vector2 p){return Replaces(item)||(shelf!=null&&shelf.Ready&&item.name.StartsWith("地形块")&&CoastShelfShape.CoveredCell(p.x,p.y));}
 public bool SampleVisualTop(Vector2 p,ref Vector3 point,ref Vector3 normal){bool found=CoastVisualSurface.SampleAll(visualSurfaces,p,ref point,ref normal);if(shelf!=null)found=shelf.Sample(p,ref point,ref normal)||found;return found;}
 public void Initialize(CoastBlockoutRegion region,CoastMaterialsAppearance appearance){
  look=appearance;shelf=gameObject.AddComponent<CoastShelfLandform>();shelf.Initialize(appearance);lowMesh=Resources.Load<Mesh>("CoastMaterials/RockSurfaceLow");mesh=Resources.Load<Mesh>("CoastMaterials/RockSurface");material=Resources.Load<Material>("CoastMaterials/RockSurface");
  if(mesh==null||material==null)throw new InvalidOperationException("UV rock surface resource missing");
  reefMaterial=Resources.Load<Material>("CoastReefRocks/Stone");reefMeshes=new Mesh[6];var reefLists=new List<Matrix4x4>[6];
  for(int i=0;i<6;i++){string name=i<3?"EN_ReefBoulder"+(i+1)+"_LOD0":"EN_ReefCliff"+(i-2)+"_LOD0";reefMeshes[i]=Resources.Load<Mesh>("CoastReefRocks/"+name);reefLists[i]=new List<Matrix4x4>();if(reefMeshes[i]==null)throw new InvalidOperationException("Reef variant missing: "+name);}
  if(reefMaterial==null||!reefMaterial.shader.isSupported)throw new InvalidOperationException("Reef material unavailable");GeneratedVariants=6;
  var template=Resources.Load<Mesh>("CoastBlockout/ScannedRock");var indices=template.triangles;var v=template.vertices;var canonical=new Vector3[indices.Length];for(int i=0;i<indices.Length;i++)canonical[i]=v[indices[i]];
  int first=0,second=0,third=0,fourth=0;float best=-1;
  for(int i=1;i<canonical.Length;i++){float d=(canonical[i]-canonical[first]).sqrMagnitude;if(d>best){best=d;second=i;}}
  Vector3 axis=canonical[second]-canonical[first];best=-1;
  for(int i=1;i<canonical.Length;i++){float d=Vector3.Cross(axis,canonical[i]-canonical[first]).sqrMagnitude;if(d>best){best=d;third=i;}}
  Vector3 normal=Vector3.Cross(axis,canonical[third]-canonical[first]).normalized;best=-1;
  for(int i=1;i<canonical.Length;i++){float d=Mathf.Abs(Vector3.Dot(normal,canonical[i]-canonical[first]));if(d>best){best=d;fourth=i;}}
  if(best<.01f)throw new InvalidOperationException("Rock template cannot define a volume");
  Matrix4x4 inverse=Basis(canonical[first],canonical[second],canonical[third],canonical[fourth]).inverse;
  var transforms=new List<Matrix4x4>();var small=new List<Matrix4x4>();
  foreach(var f in region.GetComponentsInChildren<MeshFilter>()){
   if(f.sharedMesh==null)continue;
   if(!f.sharedMesh.name.Contains("扫描形体")){
    string group=f.transform.parent!=null?f.transform.parent.name:"";
    if((group=="连接成岸壁的岩体"||group=="有侧壁的礁台与深沟"||group=="低岸与岩台连接")&&f.name!="鱼群贯通岩洞"){
     var oldBounds=f.sharedMesh.bounds;var lowBounds=lowMesh.bounds;Vector3 size=oldBounds.size,sourceSize=lowBounds.size;
     var matrix=f.transform.localToWorldMatrix*Matrix4x4.Translate(oldBounds.min)*Matrix4x4.Scale(new Vector3(size.x/sourceSize.x,size.y/sourceSize.y,size.z/sourceSize.z))*Matrix4x4.Translate(-lowBounds.min);
     originals.Add(f.GetComponent<Renderer>());small.Add(matrix);
    }continue;
   }var points=f.sharedMesh.vertices;
   if(points.Length!=canonical.Length)throw new InvalidOperationException("Scan proxy topology changed: "+f.name);
   Matrix4x4 local=Basis(points[first],points[second],points[third],points[fourth])*inverse;
   float error=0;for(int i=0;i<points.Length;i+=17)error=Mathf.Max(error,Vector3.Distance(local.MultiplyPoint3x4(canonical[i]),points[i]));
   FitError=Mathf.Max(FitError,error);if(error>.004f)throw new InvalidOperationException("Scan surface alignment failed: "+f.name+" "+error);
   var r=f.GetComponent<Renderer>();if(r==null)continue;originals.Add(r);Surfaces++;
   var world=f.transform.localToWorldMatrix*local;
   Vector3 bottom=world.MultiplyPoint3x4(new Vector3(0,0,0)),top=world.MultiplyPoint3x4(new Vector3(0,1,0));
   if(top.y<-.5f&&top.y-bottom.y>8){
    if(CoastShelfShape.ReplacesRock((top+bottom)*.5f)){ReshapedReefs++;continue;}
    // One varied broad-shouldered module replaces the repeated three-layer scan pile.
    int variant=ReshapedReefs%6;reefLists[variant].Add(world);ReshapedReefs++;GeneratedInstances++;
    // One short fractured crown retains scan detail above the generated primary mass.
    small.Add(world*Matrix4x4.Translate(new Vector3(0,.74f,0))*Matrix4x4.Scale(new Vector3(1,.26f,1)));
   }else transforms.Add(world);
  }
  matrices=transforms.ToArray();smallMatrices=small.ToArray();SmallSurfaces=smallMatrices.Length;Triangles=mesh.triangles.Length/3*matrices.Length+lowMesh.triangles.Length/3*SmallSurfaces;Ready=true;
  foreach(var original in originals)if(original!=null)replacedTransforms.Add(original.transform);
  foreach(var matrix in matrices)visualSurfaces.Add(new CoastVisualSurface(mesh,matrix));
  foreach(var matrix in smallMatrices)visualSurfaces.Add(new CoastVisualSurface(lowMesh,matrix));
  reefMatrices=new Matrix4x4[6][];for(int i=0;i<6;i++){reefMatrices[i]=reefLists[i].ToArray();Triangles+=reefMeshes[i].triangles.Length/3*reefMatrices[i].Length;foreach(var matrix in reefMatrices[i])visualSurfaces.Add(new CoastVisualSurface(reefMeshes[i],matrix));}
  Debug.Log("COAST_ROCK_SURFACES_READY count="+Surfaces+" small="+SmallSurfaces+" triangles="+Triangles+" fitError="+FitError+" uv="+mesh.uv.Length);
 }
 static Matrix4x4 Basis(Vector3 origin,Vector3 a,Vector3 b,Vector3 c){var m=Matrix4x4.identity;Vector3 x=a-origin,y=b-origin,z=c-origin;m.SetColumn(0,new Vector4(x.x,x.y,x.z,0));m.SetColumn(1,new Vector4(y.x,y.y,y.z,0));m.SetColumn(2,new Vector4(z.x,z.y,z.z,0));m.SetColumn(3,new Vector4(origin.x,origin.y,origin.z,1));return m;}
 void LateUpdate(){if(!Ready)return;DrawReef();bool visible=!look.Grey;foreach(var r in originals)if(r!=null)r.enabled=!visible;if(visible&&matrices.Length>0)Graphics.DrawMeshInstanced(mesh,0,material,matrices,matrices.Length,null,ShadowCastingMode.On,true,0,null,LightProbeUsage.Off);if(visible&&smallMatrices.Length>0)Graphics.DrawMeshInstanced(lowMesh,0,material,smallMatrices,smallMatrices.Length,null,ShadowCastingMode.On,true,0,null,LightProbeUsage.Off);}
 void DrawReef(){if(!Ready||look.Grey)return;for(int i=0;i<6;i++)if(reefMatrices[i].Length>0)Graphics.DrawMeshInstanced(reefMeshes[i],0,reefMaterial,reefMatrices[i],reefMatrices[i].Length,null,ShadowCastingMode.On,true,0,null,LightProbeUsage.Off);}
 void OnDestroy(){foreach(var r in originals)if(r!=null)r.enabled=true;}
}
}
