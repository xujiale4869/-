using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using B=Deepsea.ReefLifeGarden.Batch;
namespace Deepsea {
// Additive facade and dock dressing. Accepted terrain, meshes and collision stay intact.
public sealed class CoastArtDetails:MonoBehaviour {
 public bool Ready {get;private set;}
 public int Triangles {get;private set;}
 public int Renderers {get;private set;}
 public int AddedColliders {get{return root==null?0:root.GetComponentsInChildren<Collider>(true).Length;}}
 Transform root;CoastBlockoutRegion region;readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
 readonly List<Renderer> replaced=new List<Renderer>();
 readonly Color iron=new Color(.065f,.12f,.125f),bronze=new Color(.30f,.23f,.13f),rope=new Color(.34f,.32f,.22f),trim=new Color(.48f,.51f,.45f),glass=new Color(.09f,.22f,.23f);
 public void Initialize(CoastBlockoutRegion area,CoastMaterialsAppearance look){
  region=area;root=new GameObject("海岸建筑美术 · 可撤销外观层").transform;root.SetParent(transform,false);
  var solid=new B();var masonry=new B();var lamp=new B();
  Tower(solid,masonry,lamp);Pier(solid);
  Batch("灯室窗框与栈桥五金",solid,Material("风化金属木绳",0));
  Batch("石灰窗套与塔身勒脚",masonry,Material("石灰收边",0));
  Batch("灯塔透镜暖光",lamp,Material("灯塔透镜",1.6f),false);
  Ready=true;SetVisible(true);Debug.Log("COAST_ART_READY triangles="+Triangles+" renderers="+Renderers+" colliders="+AddedColliders);
 }
 Material Material(string name,float emission){var m=new Material(Shader.Find("Deepsea/CoastEcology")){name=name};m.SetColor("_Tint",Color.white);m.SetFloat("_Emission",emission);materials.Add(m);return m;}
 Vector3 T(Vector3 p){return region.lighthouse.TransformPoint(p);}
 void Tower(B b,B m,B glow){
  foreach(var r in region.lighthouse.GetComponentsInChildren<Renderer>())if(r.name=="灯室")replaced.Add(r);
  // Reveal the lantern chamber inside the existing silhouette.
  Ring(b,T(new Vector3(0,7.12f,0)),1.02f,.055f,iron,32);
  Ring(b,T(new Vector3(0,8.02f,0)),1.02f,.065f,iron,32);
  Ring(b,T(new Vector3(0,7.72f,0)),1.42f,.035f,bronze,48);
  Ring(b,T(new Vector3(0,7.38f,0)),1.42f,.022f,iron,48);
  for(int i=0;i<8;i++){
   float a=i*Mathf.PI/4;Vector3 r=new Vector3(Mathf.Cos(a)*1.10f,0,Mathf.Sin(a)*1.10f);
   Tube(b,T(r+Vector3.up*7.12f),T(r+Vector3.up*8.03f),.036f,.036f,iron,8);
  }
  // Fresnel lens represented by nested rings, not an expensive additional shadow light.
  Tube(glow,T(new Vector3(0,7.22f,0)),T(new Vector3(0,7.90f,0)),.23f,.23f,new Color(.88f,.50f,.17f),24);
  for(int i=0;i<9;i++)Ring(glow,T(new Vector3(0,7.25f+i*.075f,0)),.27f,.018f,new Color(.82f,.57f,.27f),32);
  Tube(b,T(new Vector3(0,6.96f,0)),T(new Vector3(0,7.25f,0)),.18f,.10f,bronze,16);
  for(int i=0;i<3;i++){
   float y=i==0?.46f:i==1?.70f:6.73f;
   Ring(m,T(new Vector3(0,y,0)),1.235f,.055f,trim,56);
  }
  // Two deeply coloured window faces with light stone reveals on the sea-facing side.
  foreach(float y in new[]{3.45f,5.35f}){
   Vector3 p=T(new Vector3(0,y,-1.365f));
   Box(b,p,new Vector3(.39f,.72f,.055f),glass);
   foreach(int side in new[]{-1,1})Box(m,p+new Vector3(side*.235f,0,.025f),new Vector3(.095f,.91f,.12f),trim);
   Box(m,p+Vector3.up*.43f,new Vector3(.56f,.10f,.13f),trim);
   Box(m,p-Vector3.up*.43f,new Vector3(.62f,.12f,.19f),trim);
   Box(b,p+new Vector3(0,0,-.04f),new Vector3(.034f,.71f,.035f),bronze);
   Box(b,p+new Vector3(0,0,-.04f),new Vector3(.39f,.035f,.035f),bronze);
  }
  Vector3 d=T(new Vector3(0,1.07f,-1.415f));
  Box(b,d,new Vector3(.59f,1.34f,.05f),new Color(.10f,.17f,.18f));
  for(int i=0;i<5;i++)Box(b,d+new Vector3((i-2)*.112f,0,-.035f),new Vector3(.009f,1.26f,.01f),iron);
  foreach(int side in new[]{-1,1})Box(m,d+new Vector3(side*.36f,0,.015f),new Vector3(.12f,1.57f,.17f),trim);
  Box(m,d+Vector3.up*.75f,new Vector3(.83f,.13f,.19f),trim);
  Ring(b,d+new Vector3(.19f,.02f,-.055f),.045f,.011f,bronze,16,Quaternion.Euler(90,0,0));
  // Roof ribs and a modest finial break the smooth cap into constructed pieces.
  for(int i=0;i<12;i++){
   float a=i*Mathf.PI/6;Vector3 rim=T(new Vector3(Mathf.Cos(a)*1.20f,8.40f,Mathf.Sin(a)*1.20f));
   Tube(b,rim,T(new Vector3(Mathf.Cos(a)*.18f,8.68f,Mathf.Sin(a)*.18f)),.018f,.018f,bronze,6);
  }
  Tube(b,T(new Vector3(0,8.52f,0)),T(new Vector3(0,9.02f,0)),.055f,.012f,iron,12);
 }
 void Pier(B b){
  // All fittings stay on board edges; the central walking strip is clear.
  for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++){
   float x=36.3f+i*2.7f,z=94+side*1.32f;
   Ring(b,new Vector3(x,1.14f,z),.188f,.023f,iron,16);
   Ring(b,new Vector3(x,1.29f,z),.19f,.022f,iron,16);
   if(i==4||i==5)for(int j=0;j<3;j++)Ring(b,new Vector3(x,1.0f+j*.045f,z),.21f,.023f,rope,20);
   if(i<5){Vector3 a=new Vector3(x,.10f,z),end=new Vector3(x+2.7f,-.9f,z);Tube(b,a,end,.055f,.055f,new Color(.19f,.23f,.21f),6);}
  }
  // Recessed-looking nail heads, two rows along each deck plank.
  for(int i=0;i<25;i++)foreach(int side in new[]{-1,1}){
   float x=35.5f+i*.60f;Tube(b,new Vector3(x,.704f,94+side*1.12f),new Vector3(x,.716f,94+side*1.12f),.033f,.033f,iron,8);
  }
  foreach(float x in new[]{44.2f,48.5f}){
   Vector3 p=new Vector3(x,.73f,95.35f);
   Box(b,p,new Vector3(.43f,.07f,.24f),iron);
   Tube(b,p+Vector3.up*.04f,p+Vector3.up*.20f,.07f,.07f,iron,10);
   Tube(b,p+new Vector3(-.31f,.19f,0),p+new Vector3(.31f,.19f,0),.055f,.04f,iron,10);
  }
  for(int j=0;j<4;j++)Ring(b,new Vector3(48.5f,.76f,92.85f),.23f+j*.056f,.021f,rope,36);
  // Small coil tail lies at the edge, rather than stretching across the berth.
  for(int i=0;i<10;i++){float a=i/10f,c=(i+1)/10f;Tube(b,new Vector3(48.83f+a*.55f,.74f,92.85f+Mathf.Sin(a*4)*.10f),new Vector3(48.83f+c*.55f,.74f,92.85f+Mathf.Sin(c*4)*.10f),.02f,.02f,rope,6);}
 }
 void Batch(string name,B b,Material mat,bool shadows=true){if(b.v.Count==0)return;var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(b.v);mesh.SetColors(b.c);mesh.SetUVs(0,b.uv);mesh.SetTriangles(b.tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);Triangles+=b.tr.Count/3;
  var go=new GameObject(name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;Renderers++;
 }
 static void Quad(B b,Vector3 a,Vector3 c,Vector3 d,Vector3 e,Color colour){int n=b.v.Count;b.Vertex(a,colour,Vector2.zero);b.Vertex(c,colour,Vector2.right);b.Vertex(d,colour,Vector2.one);b.Vertex(e,colour,Vector2.up);b.Tri(n,n+1,n+2);b.Tri(n,n+2,n+3);}
 static void Box(B b,Vector3 p,Vector3 size,Color c){Vector3 x=Vector3.right*size.x*.5f,y=Vector3.up*size.y*.5f,z=Vector3.forward*size.z*.5f;
  Quad(b,p-x-y-z,p-x+y-z,p+x+y-z,p+x-y-z,c);Quad(b,p+x-y+z,p+x+y+z,p-x+y+z,p-x-y+z,c);
  Quad(b,p-x+y-z,p-x+y+z,p+x+y+z,p+x+y-z,c);Quad(b,p-x-y+z,p-x-y-z,p+x-y-z,p+x-y+z,c);
  Quad(b,p+x-y-z,p+x+y-z,p+x+y+z,p+x-y+z,c);Quad(b,p-x-y+z,p-x+y+z,p-x+y-z,p-x-y-z,c);
 }
 static void Tube(B b,Vector3 from,Vector3 to,float r0,float r1,Color colour,int sides){Vector3 axis=(to-from).normalized,u=Vector3.Cross(axis,Mathf.Abs(axis.y)>.95f?Vector3.right:Vector3.up).normalized,v=Vector3.Cross(axis,u);int start=b.v.Count;
  for(int j=0;j<2;j++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;b.Vertex((j==0?from:to)+(u*Mathf.Cos(a)+v*Mathf.Sin(a))*(j==0?r0:r1),colour,new Vector2(i/(float)sides,j));}
  for(int i=0;i<sides;i++){int a=start+i,c=start+(i+1)%sides;b.Tri(a,c,a+sides);b.Tri(c,c+sides,a+sides);}
  int cap=b.v.Count;b.Vertex(from,colour,Vector2.zero);b.Vertex(to,colour,Vector2.one);for(int i=0;i<sides;i++){int a=start+i,c=start+(i+1)%sides;b.Tri(cap,c,a);b.Tri(cap+1,a+sides,c+sides);}
 }
 static void Ring(B b,Vector3 p,float radius,float tube,Color colour,int steps,Quaternion rotation=default(Quaternion)){
  if(rotation.x==0&&rotation.y==0&&rotation.z==0&&rotation.w==0)rotation=Quaternion.identity;const int sides=6;int start=b.v.Count;
  for(int i=0;i<steps;i++){float a=i*Mathf.PI*2/steps;Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));for(int j=0;j<sides;j++){float q=j*Mathf.PI*2/sides;b.Vertex(p+rotation*(radial*(radius+tube*Mathf.Cos(q))+Vector3.up*tube*Mathf.Sin(q)),colour,new Vector2(i/(float)steps,j/(float)sides));}}
  for(int i=0;i<steps;i++)for(int j=0;j<sides;j++){int a=start+i*sides+j,c=start+i*sides+(j+1)%sides,d=start+(i+1)%steps*sides+j,e=start+(i+1)%steps*sides+(j+1)%sides;b.Tri(a,c,d);b.Tri(c,e,d);}
 }
 public void SetVisible(bool visible){if(root!=null)root.gameObject.SetActive(visible);foreach(var r in replaced)if(r!=null)r.enabled=!visible;}
 void OnDestroy(){foreach(var r in replaced)if(r!=null)r.enabled=true;foreach(var m in meshes)if(m!=null)Destroy(m);foreach(var m in materials)if(m!=null)Destroy(m);if(root!=null)Destroy(root.gameObject);}
}
}
