using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
 public sealed partial class SeaBayExpansionAndBuildSpaceV28 {
  static Transform Group(string name,Transform parent,Vector3 local){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
  Material Mat(Shader shader,string name,Color color){if(shader==null)throw new InvalidOperationException("Missing v28 shader: "+name);var m=new Material(shader){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
  void InitMaterials(){
   var lit=Resources.Load<Material>("SeaExpansionV28/SurfaceKeep").shader;sand=new Material(Resources.Load<Material>("CoastRefinement/Sand")){name="V28_Sand"};owned.Add(sand);sand.SetColor("_BaseColor",new Color(.63f,.65f,.53f));
   stone=Mat(lit,"V28_Limestone",new Color(.37f,.42f,.39f));stone.SetFloat("_Smoothness",.2f);
   wood=Mat(lit,"V28_WeatheredWood",new Color(.76f,.66f,.52f));wood.SetTexture("_BaseMap",Resources.Load<Texture2D>("SeaHarborV27/Textures/WeatheredPierWood_V27"));wood.SetFloat("_Smoothness",.2f);
   rope=Mat(lit,"V28_Hemp",new Color(.45f,.34f,.19f));bronze=Mat(lit,"V28_Bronze",new Color(.15f,.18f,.16f));bronze.SetFloat("_Metallic",.65f);
   glow=Mat(Resources.Load<Material>("SeaHarborV27/GlowShaderKeep").shader,"V28_Glow",new Color(1,.69f,.28f));
   buoyWhite=Mat(lit,"V28_BuoyWhite",new Color(.88f,.83f,.69f));buoyRed=Mat(lit,"V28_BuoyRed",new Color(.52f,.15f,.11f));
   var reef=Resources.Load<Material>("SeaHarborV27/ReefShaderKeep").shader;coral=new Material[4];for(int i=0;i<4;i++){coral[i]=Mat(reef,"V28_Coral_"+i,Color.white);coral[i].SetTexture("_PaletteMap",Resources.Load<Texture2D>("SeaHarborV27/Textures/CoralPaletteLUT_V27"));coral[i].SetFloat("_PaletteBand",i);coral[i].SetFloat("_SwayAmplitude",.045f);}
   kelp=Mat(reef,"V28_GreenKelp",Color.white);kelp.SetFloat("_IsKelp",1);kelp.SetFloat("_SwayAmplitude",.14f);
   var overlayShader=Resources.Load<Material>("SeaExpansionV28/OverlayKeep").shader;gridMat=Mat(overlayShader,"V28_Grid",new Color(.65f,.91f,.84f,.20f));validMat=Mat(overlayShader,"V28_Valid",new Color(.20f,.91f,.64f,.55f));invalidMat=Mat(overlayShader,"V28_Invalid",new Color(.98f,.30f,.21f,.55f));
   models[0]=Resources.Load<Mesh>("SeaHarborV27/Baked/Lantern_V27_Hanging");models[1]=Resources.Load<Mesh>("SeaHarborV27/Baked/Coral_V27_Cluster");models[2]=Resources.Load<Mesh>("SeaHarborV27/Baked/Kelp_V27_GiantFrond");
   if(models[0]==null||models[1]==null||models[2]==null)throw new InvalidOperationException("Missing accepted v27 models");
  }
  GameObject MeshObject(string name,Transform parent,Vector3 p,Vector3 scale,Mesh mesh,Material mat){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;return go;}
  Mesh SurfaceMesh(string name,float minX,float maxX,float minZ,float maxZ,int nx,int nz,Func<float,float,float> height){
   var v=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[v.Length];var tr=new int[nx*nz*6];int index=0;
   for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){float xx=Mathf.Lerp(minX,maxX,x/(float)nx),zz=Mathf.Lerp(minZ,maxZ,z/(float)nz);int i=z*(nx+1)+x;v[i]=new Vector3(xx,height(xx,zz),zz);uv[i]=new Vector2(xx*.45f,zz*.45f);}
   for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;tr[index++]=a;tr[index++]=c;tr[index++]=b;tr[index++]=b;tr[index++]=c;tr[index++]=d;}
   var m=new Mesh{name=name,indexFormat=v.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.vertices=v;m.uv=uv;m.triangles=tr;m.RecalculateNormals();m.RecalculateBounds();owned.Add(m);return m;
  }
  void ExpandWaterAndTerraces(){
   waterFilter=game.world.Find("ShowcaseWater").GetComponent<MeshFilter>();oldWaterMesh=waterFilter.sharedMesh;oldWaterScale=waterFilter.transform.localScale;oldWaterPosition=waterFilter.transform.localPosition;
   // Replace local geometry, rather than scaling shader displacement and changing the wave height.
   waterFilter.transform.localScale=Vector3.one;waterFilter.transform.position=new Vector3(25,SeaLifeRuntime.Sea,8);
   var waterMesh=SurfaceMesh("V28_300m_WaveSurface",-150,150,-150,150,200,200,(x,z)=>0);var bounds=waterMesh.bounds;bounds.Expand(Vector3.up*2);waterMesh.bounds=bounds;waterFilter.sharedMesh=waterMesh;
   var floor=SurfaceMesh("V28_OceanFloor",-130,180,-147,163,1,1,(x,z)=>SeaLifeRuntime.Sea-12.5f);MeshObject("V28_DeepOceanFloor",terrainRoot,Vector3.zero,Vector3.one,floor,sand);
   var terrace=SurfaceMesh("V28_ContinuousSandAndCrescent",-10,61,-47,46,142,186,TerraceHeight);var go=MeshObject("V28_ContinuousSeabed",terrainRoot,Vector3.zero,Vector3.one,terrace,sand);go.AddComponent<MeshCollider>().sharedMesh=terrace;
   Physics.SyncTransforms();for(int i=0;i<3;i++){
    var mesh=Resources.Load<Mesh>("SeaExpansionV28/LandmarkRock");
    var p=new[]{new Vector3(26.8f,0,8.5f),new Vector3(27.8f,0,9.2f),new Vector3(28.5f,0,-17.2f)}[i];
    p.y=GroundAt(p)-.1f;float rockHeight=SeaLifeRuntime.Sea+.55f-p.y;var obj=MeshObject("V28_AtollLandmark_"+i,terrainRoot,p,new Vector3(2.2f,rockHeight/Mathf.Max(.1f,mesh.bounds.size.y),1.9f),mesh,stone);obj.transform.localRotation=Quaternion.Euler(0,43*i,0);var collider=obj.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
   }
  }
  float Mask(float x,float z,float cx,float cz,float rx,float rz){float dx=(x-cx)/rx,dz=(z-cz)/rz;float a=Mathf.Atan2(dz,dx);float r=Mathf.Pow(Mathf.Pow(Mathf.Abs(dx),4)+Mathf.Pow(Mathf.Abs(dz),4),.25f);r+=.035f*Mathf.Sin(a*5+cx*.3f);return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,r));}
  float TerraceHeight(float x,float z){
   float sea=SeaLifeRuntime.Sea,floor=sea-12.5f;
   float wave=.06f*Mathf.Sin(x*.72f+Mathf.Sin(z*.3f))+.045f*Mathf.Cos(z*.61f-x*.17f);
   float a=Mask(x,z,11.5f,4.5f,13,14),south=Mask(x,z,14,-16,16,14),north=Mask(x,z,16,24,15,13);
   float dx=(x-36)/13,dz=(z+1)/30,r=Mathf.Sqrt(dx*dx+dz*dz);
   float outside=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1.10f,r));
   float innerR=Mathf.Sqrt(Mathf.Pow((x-41)/11,2)+Mathf.Pow((z+1)/22,2));
   float inner=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1,innerR));float crescent=outside*(1-inner);
   return Mathf.Max(Mathf.Max(Mathf.Lerp(floor,sea-1.12f+wave,a),Mathf.Lerp(floor,sea-1.85f+wave,south)),Mathf.Max(Mathf.Lerp(floor,sea-1.75f+wave,north),Mathf.Lerp(floor,sea-1.35f+wave,crescent)));
  }
  bool V30FlatCell(Vector3 p){float d=DepthAt(p);if(d<.45f||d>3.2f)return false;foreach(var o in new[]{Vector3.right*2,Vector3.left*2,Vector3.forward*2,Vector3.back*2})if(Mathf.Abs(d-DepthAt(p+o))>.65f)return false;return true;}
  void SetupGrid(){
   gridRoot=Group("V28_2m_BuildGrid",root,Vector3.zero);var v=new List<Vector3>();var tr=new List<int>();
   for(int gz=SeaSeamlessWorldAndNauticalUIV29.SceneEnabled?-42:-23;gz<=(SeaSeamlessWorldAndNauticalUIV29.SceneEnabled?48:30);gz++)for(int gx=SeaSeamlessWorldAndNauticalUIV29.SceneEnabled?-6:-3;gx<=(SeaSeamlessWorldAndNauticalUIV29.SceneEnabled?62:42);gx++){var p=CellPosition(new Vector2Int(gx,gz));float d=DepthAt(p);if(SeaSeamlessWorldAndNauticalUIV29.IsV30&&!V30FlatCell(p))continue;if(d<(SeaSeamlessWorldAndNauticalUIV29.SceneEnabled?.35f:.5f)||d>(SeaSeamlessWorldAndNauticalUIV29.SceneEnabled?6.2f:5.5f))continue;float s=.99f,w=.013f;
    Quad(v,tr,p+new Vector3(-s,.035f,-s),new Vector3(w,0,2*s));Quad(v,tr,p+new Vector3(s,.035f,-s),new Vector3(w,0,2*s));Quad(v,tr,p+new Vector3(-s,.035f,-s),new Vector3(2*s,0,w));Quad(v,tr,p+new Vector3(-s,.035f,s),new Vector3(2*s,0,w));}
   var m=new Mesh{name="V28_GridQuads",indexFormat=IndexFormat.UInt32};m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateBounds();owned.Add(m);MeshObject("BuildableCellLines",gridRoot,Vector3.zero,Vector3.one,m,gridMat);gridRoot.gameObject.SetActive(false);
   cursorRoot=Group("V28_Cursor",root,Vector3.zero);for(int i=0;i<4;i++){cursorTemplates[i]=Structure((BayBuildableType)i,true,1.3f);cursorTemplates[i].transform.SetParent(cursorRoot,false);cursorTemplates[i].SetActive(i==selected);}cursorRoot.gameObject.SetActive(false);SetupUI();
  }
  static void Quad(List<Vector3> v,List<int> tr,Vector3 p,Vector3 size){int k=v.Count;v.Add(p);v.Add(p+new Vector3(size.x,0,0));v.Add(p+size);v.Add(p+new Vector3(0,0,size.z));tr.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});}
 }
}
