using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;
namespace Deepsea {public sealed partial class CoastSlice {
 void ChunkSurfaces(){foreach(var r in game.World.Root.GetComponentsInChildren<MeshRenderer>()){
  if(r.name=="海面 · 无碰撞")ChunkGrid(r,360,30,true);
  if(r.name=="连续海床 · 仅视觉无碰撞")ChunkGrid(r,512,32,false);
 }}
 void ChunkGrid(MeshRenderer source,int cells,int tile,bool waves){
  var original=source.GetComponent<MeshFilter>().sharedMesh;int side=cells+1;
  if(original.vertexCount!=side*side){Require(false,"Unexpected surface grid: "+source.name);return;}
  var vertices=original.vertices;var normals=original.normals;var uv=original.uv;
  for(int z=0;z<cells;z+=tile)for(int x=0;x<cells;x+=tile){
   int nx=Mathf.Min(tile,cells-x),nz=Mathf.Min(tile,cells-z),factor=waves?3:1,cx=nx*factor,cz=nz*factor,stride=cx+1;var v=new Vector3[stride*(cz+1)];var n=new Vector3[v.Length];var u=new Vector2[v.Length];var tr=new List<int>(cx*cz*6);
   var lo=vertices[z*side+x];var hi=vertices[(z+nz)*side+x+nx];
   for(int j=0;j<=cz;j++)for(int i=0;i<=cx;i++){int a=j*stride+i;if(waves){v[a]=new Vector3(Mathf.Lerp(lo.x,hi.x,i/(float)cx),lo.y,Mathf.Lerp(lo.z,hi.z,j/(float)cz));n[a]=Vector3.up;u[a]=new Vector2(v[a].x/700+.5f,v[a].z/700+.5f);}else{int b=(z+j)*side+x+i;v[a]=vertices[b];n[a]=normals.Length==vertices.Length?normals[b]:Vector3.up;u[a]=uv.Length==vertices.Length?uv[b]:new Vector2(v[a].x,v[a].z)*.16f;}}
   for(int j=0;j<cz;j++)for(int i=0;i<cx;i++){int a=j*stride+i;var p=v[a];var q=v[a+stride+1];
    // The detailed local bed replaces these cells; retain the narrow edge band to avoid cracks.
    if(!waves&&p.x>=43&&q.x<=109&&p.z>=61&&q.z<=133)continue;
    tr.Add(a);tr.Add(a+stride);tr.Add(a+1);tr.Add(a+1);tr.Add(a+stride);tr.Add(a+stride+1);
   }
   if(tr.Count==0)continue;
   var mesh=new Mesh{name=waves?"海面可裁剪分块":"海床可裁剪分块"};mesh.vertices=v;mesh.normals=n;mesh.uv=u;mesh.SetTriangles(tr,0);mesh.RecalculateBounds();if(waves){var bounds=mesh.bounds;bounds.Expand(new Vector3(4,2,4));mesh.bounds=bounds;}meshes.Add(mesh);
   var obj=new GameObject(mesh.name);obj.layer=source.gameObject.layer;obj.transform.SetParent(source.transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var r=obj.AddComponent<MeshRenderer>();r.sharedMaterial=source.sharedMaterial;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=source.receiveShadows;
  }source.enabled=false;
 }
}}
