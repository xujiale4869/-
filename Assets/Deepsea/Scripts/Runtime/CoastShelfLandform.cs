using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
public sealed class CoastShelfLandform:MonoBehaviour {
 public bool Ready;public int Chunks,Triangles;public int AddedColliders{get{return 0;}}
 CoastMaterialsAppearance look;Mesh[] meshes;Material material;
 readonly List<CoastVisualSurface> surfaces=new List<CoastVisualSurface>();
 float[][] heightGrid;int gridX,gridZ;float gridDX,gridDZ;
 public void Initialize(CoastMaterialsAppearance appearance){
  look=appearance;material=Resources.Load<Material>("CoastShelf/Shelf");meshes=new Mesh[8];Shader.SetGlobalTexture("_CoastShelfMask",Resources.Load<Texture2D>("CoastShelf/Coverage"));
  if(material==null||!material.shader.isSupported)throw new InvalidOperationException("Shelf material unavailable");
  gridX=Mathf.CeilToInt((CoastShelfShape.X1-CoastShelfShape.X0)/CoastShelfShape.Step);gridZ=Mathf.CeilToInt(11.5f/CoastShelfShape.Step);gridDX=(CoastShelfShape.X1-CoastShelfShape.X0)/gridX;gridDZ=11.5f/gridZ;heightGrid=new float[8][];
  for(int i=0;i<8;i++){meshes[i]=Resources.Load<Mesh>("CoastShelf/Shelf"+i);if(meshes[i]==null)throw new InvalidOperationException("Shelf mesh unavailable");Triangles+=meshes[i].triangles.Length/3;Chunks++;surfaces.Add(new CoastVisualSurface(meshes[i],Matrix4x4.identity));
   heightGrid[i]=new float[(gridX+1)*(gridZ+1)];for(int k=0;k<heightGrid[i].Length;k++)heightGrid[i][k]=float.NaN;
   foreach(var vertex in meshes[i].vertices){int x=Mathf.RoundToInt((vertex.x-CoastShelfShape.X0)/gridDX),z=Mathf.RoundToInt((vertex.z-CoastShelfShape.Z0-i*11.5f)/gridDZ);heightGrid[i][z*(gridX+1)+x]=vertex.y;}
  }
  Ready=true;
 }
 public bool Sample(Vector2 p,ref Vector3 point,ref Vector3 normal){
  if(!Ready||!CoastShelfShape.Contains(p.x,p.y))return false;return CoastVisualSurface.SampleAll(surfaces,p,ref point,ref normal);
 }
 public bool BlocksFish(Vector3 p,float radius){
  foreach(var offset in new[]{Vector2.zero,Vector2.right*radius,Vector2.left*radius,Vector2.up*radius,Vector2.down*radius,new Vector2(.7071f,.7071f)*radius,new Vector2(-.7071f,.7071f)*radius,new Vector2(.7071f,-.7071f)*radius,new Vector2(-.7071f,-.7071f)*radius}){
   float height;if(!SurfaceHeight(p.x+offset.x,p.z+offset.y,out height))continue;
   if(p.y-radius<height+.04f)return true;
  }return false;
 }
 // Exact interpolation of the two rendered triangles. Sharp ledges must not be checked
 // against the continuous authoring function between the baked vertices.
 public bool SurfaceHeight(float x,float z,out float height){
  height=0;if(!Ready||x<CoastShelfShape.X0||x>=CoastShelfShape.X1||z<CoastShelfShape.Z0||z>=CoastShelfShape.Z1)return false;
  int chunk=Mathf.FloorToInt((z-CoastShelfShape.Z0)/11.5f);float gx=(x-CoastShelfShape.X0)/gridDX,gz=(z-CoastShelfShape.Z0-chunk*11.5f)/gridDZ;
  int ix=Mathf.FloorToInt(gx),iz=Mathf.FloorToInt(gz);if(ix<0||ix>=gridX||iz<0||iz>=gridZ)return false;
  int k=iz*(gridX+1)+ix;float a=heightGrid[chunk][k],b=heightGrid[chunk][k+1],c=heightGrid[chunk][k+gridX+1],d=heightGrid[chunk][k+gridX+2];
  if(float.IsNaN(a)||float.IsNaN(b)||float.IsNaN(c)||float.IsNaN(d)||!CoastShelfShape.CoveredCell(x,z))return false;
  float u=gx-ix,v=gz-iz;height=u+v<=1?a*(1-u-v)+b*u+c*v:d*(u+v-1)+b*(1-v)+c*(1-u);return true;
 }
 void LateUpdate(){Shader.SetGlobalFloat("_CoastShelfEnabled",Ready&&!look.Grey?1:0);if(!Ready||look.Grey)return;foreach(var mesh in meshes)Graphics.DrawMesh(mesh,Matrix4x4.identity,material,0,null,0,null,ShadowCastingMode.On,true,null,LightProbeUsage.Off);}
 void OnDestroy(){Shader.SetGlobalFloat("_CoastShelfEnabled",0);}
}
}
