using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
public sealed class NaturalCoast:MonoBehaviour {
 public static bool Enabled=true;public bool Ready{get;private set;}public int Trees{get;private set;}public int Kelp{get;private set;}public int Triangles{get;private set;}
 public static readonly Vector2[] Route={new Vector2(8,68),new Vector2(-9,77),new Vector2(-21,89),new Vector2(-16,108),new Vector2(2,120),new Vector2(25,111),new Vector2(35,96),new Vector2(22,80),new Vector2(8,68)};
 static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));}
 public static float RouteDistance(float x,float z){float d=1000;for(int i=0;i<Route.Length-1;i++)d=Mathf.Min(d,Segment(new Vector2(x,z),Route[i],Route[i+1]));return d;}
 static float Bell(float x,float z,float cx,float cz,float rx,float rz){return Mathf.Exp(-((x-cx)*(x-cx)/(rx*rx)+(z-cz)*(z-cz)/(rz*rz)));}
 public static float LandRelief(float x,float z){if(!Enabled)return 0;float d=RouteDistance(x,z);float clear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.8f,6.5f,d));
  float relief=5.5f*Bell(x,z,31,113,7,10)+3.7f*Bell(x,z,32,76,8,7)+2.5f*Bell(x,z,22,101,9,8);
  return relief*clear*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.8f,BaseLandMask(x,z)));
 }
 static float BaseLandMask(float x,float z){float dx=(x-8)/38,dz=(z-94)/42;return Mathf.Clamp01((1-Mathf.Sqrt(dx*dx+dz*dz))/.17f);}
 public static float SeaRelief(float x,float z,float height){if(!Enabled||height>-3.2f)return height;float strength=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-3.2f,-6,height));
  float ridge=3.6f*Bell(x,z,77+Mathf.Sin(z*.15f)*3,z,4.8f,100)+2.8f*Bell(x,z,89,79,7,12);
  float channel=4.5f*Bell(x,z,67+Mathf.Sin(z*.10f)*2,z,3.2f,100)*Mathf.Exp(-(z-94)*(z-94)/500);
  float pocket=Mathf.SmoothStep(0,1,Mathf.InverseLerp(60,67,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(118,128,z)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(62,69,x))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(96,109,x)));
  return Mathf.Min(-2.6f,height+(ridge-channel)*strength*pocket);
 }
 sealed class Batch{public List<Vector3> v=new List<Vector3>();public List<Vector2> uv=new List<Vector2>();public List<Color> c=new List<Color>();public List<int> t=new List<int>();public void Add(Vector3 p,Color col,Vector2 tex){v.Add(p);c.Add(col);uv.Add(tex);}public void Tri(int a,int b,int d){t.Add(a);t.Add(b);t.Add(d);}}
 readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> mats=new List<Material>();Transform root;OceanSurface sea;GameDirector game;System.Random random=new System.Random(28091);
 float R(float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
 public void Initialize(OceanSurface water){sea=water;game=water.game;Shader.SetGlobalFloat("_BayNaturalCoast",Enabled?1:0);if(!Enabled){Ready=true;return;}
  root=new GameObject("自然海岸 · 林冠、礁脊与海带林").transform;root.SetParent(game.World.Root,false);
  var wood=new Batch();var foliage=new Batch();var kelp=new Batch();
  Vector2[] trees={new Vector2(26,116),new Vector2(34,118),new Vector2(35,110),new Vector2(26,104),new Vector2(24,97),new Vector2(30,88),new Vector2(31,73),new Vector2(24,70),new Vector2(15,94),new Vector2(18,111),new Vector2(10,84)};
  foreach(var p in trees){if(IslandTerrain.Height(p.x,p.y)<.7f||RouteDistance(p.x,p.y)<3.6f)continue;Tree(wood,foliage,IslandTerrain.Point(p.x,p.y),R(4.5f,7.4f));Trees++;}
  for(int i=0;i<360;i++){float x=R(13,37),z=R(65,124);if(IslandTerrain.Height(x,z)<.8f||RouteDistance(x,z)<2.2f||Protected(x,z,5))continue;var p=IslandTerrain.Point(x,z);for(int j=0;j<5;j++){float a=j*1.256f+R(0,1);Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Leaf(foliage,p,d,R(.75f,1.6f),R(.10f,.23f),new Color(.13f,.30f,.22f),new Color(.40f,.56f,.27f),false);}}
  Debug.Log("SEA_SAMPLE "+sea.BottomHeight(77,94)+" "+sea.BottomHeight(80,105)+" protected="+Protected(77,94,7));for(int i=0;i<220;i++){float x=R(67,94),z=R(64,122),bed=sea.BottomHeight(x,z),depth=OceanSurface.SeaLevel-bed;
   float ridge=Mathf.Abs(x-(77+Mathf.Sin(z*.15f)*3));if(depth<1.8f||depth>18||ridge>5&&Vector2.Distance(new Vector2(x,z),new Vector2(88,77))>8||Protected(x,z,7))continue;
   var p=new Vector3(x,bed-.05f,z);float height=Mathf.Min(R(5.5f,9.0f),depth-.85f);
   for(int st=0;st<3;st++){Vector3 q=p+new Vector3(R(-.6f,.6f),0,R(-.6f,.6f));float h=height*R(.65f,1);Vector3 bend=new Vector3(R(.35f,1),0,R(-.4f,.4f));Tube(kelp,q,q+Vector3.up*h+bend,.055f,new Color(.11f,.25f,.16f),true);
    for(int j=0;j<9;j++){float f=.15f+j*.085f;Vector3 b=q+Vector3.up*(h*f)+bend*f*f;Vector3 d=new Vector3(j%2==0?1:-1,.35f,R(-.4f,.4f));Leaf(kelp,b,d,R(.9f,1.7f),R(.18f,.35f),new Color(.08f,.23f,.15f),new Color(.36f,.53f,.26f),true);}}
   Kelp++;
  }
  Make("弯曲树干与枝条",wood,false);Make("风剪林冠与蕨叶",foliage,true);Make("深浅交界的高海带林",kelp,true);
  foreach(var light in FindObjectsOfType<Light>())if(light.type==LightType.Directional){light.color=new Color(1,.92f,.76f);light.intensity=1.12f;light.transform.rotation=Quaternion.Euler(46,-42,0);light.shadows=LightShadows.Soft;break;}
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.30f,.42f,.49f);RenderSettings.ambientEquatorColor=new Color(.19f,.28f,.31f);RenderSettings.ambientGroundColor=new Color(.12f,.18f,.21f);
  Ready=true;Debug.Log("NATURAL_COAST_READY trees="+Trees+" kelp="+Kelp+" triangles="+Triangles);
 }
 bool Protected(float x,float z,float distance){var p=new Vector3(x,0,z);foreach(var l in game.Expedition.Landings){var d=p-l.feet;d.y=0;if(d.magnitude<distance)return true;d=p-l.berth;d.y=0;if(d.magnitude<distance)return true;}foreach(var s in game.Expedition.Sites){var d=p-s.position;d.y=0;if(d.magnitude<distance*.6f)return true;}return false;}
 void Tree(Batch wood,Batch leaf,Vector3 p,float h){Vector3 top=p+new Vector3(1.0f,h,.5f);Tube(wood,p,top,.21f,new Color(.25f,.25f,.18f),false);
  for(int b=0;b<7;b++){float a=b*2.4f;Vector3 start=Vector3.Lerp(p,top,.40f+b*.07f),end=top+new Vector3(Mathf.Cos(a)*R(1.1f,2.7f),R(-1.8f,-.2f),Mathf.Sin(a)*R(1.2f,2.6f));Tube(wood,start,end,.085f,new Color(.24f,.27f,.19f),false);
   for(int j=0;j<22;j++){Vector3 q=Vector3.Lerp(start,end,R(.4f,1))+new Vector3(R(-.6f,.6f),R(-.1f,.6f),R(-.6f,.6f));float t=R(0,6.28f);Leaf(leaf,q,new Vector3(Mathf.Cos(t),R(-.1f,.6f),Mathf.Sin(t)),R(.75f,1.5f),R(.19f,.37f),new Color(.10f,.24f,.20f),new Color(.45f,.59f,.29f),false);}
  }
 }
 void Tube(Batch b,Vector3 a,Vector3 end,float radius,Color color,bool under){int k=b.v.Count;Vector3 axis=(end-a).normalized,right=Vector3.Cross(axis,Vector3.forward).normalized,forward=Vector3.Cross(axis,right);for(int ring=0;ring<=12;ring++){float f=ring/12f;Vector3 p=Vector3.Lerp(a,end,f)+right*(Mathf.Sin(f*Mathf.PI)*radius*2);for(int j=0;j<=8;j++){float angle=j*Mathf.PI*.25f;b.Add(p+(right*Mathf.Cos(angle)+forward*Mathf.Sin(angle))*radius*(1-f*.72f),color*(.85f+f*.25f),new Vector2(under?f:0,a.x+a.z));}}for(int ring=0;ring<12;ring++)for(int j=0;j<8;j++){int v=k+ring*9+j;b.Tri(v,v+9,v+1);b.Tri(v+1,v+9,v+10);}}
 void Leaf(Batch b,Vector3 p,Vector3 direction,float length,float width,Color low,Color tip,bool under){int k=b.v.Count;Vector3 d=direction.normalized,side=Vector3.Cross(d,Vector3.up).normalized;float phase=p.x*.7f+p.z;for(int j=0;j<=10;j++){float f=j/10f;Vector3 mid=p+d*(length*f)+Vector3.up*(Mathf.Sin(f*Mathf.PI)*length*.22f-f*f*length*.20f);float w=width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(f*Mathf.PI)),.7f);for(int s=-1;s<=1;s++){Vector3 q=mid+side*w*s-Vector3.up*(Mathf.Abs(s)*w*.18f);b.Add(q,Color.Lerp(low,tip,f)*(s==0?1.12f:.88f),new Vector2(under?f:0,phase));}}for(int j=0;j<10;j++)for(int s=0;s<2;s++){int v=k+j*3+s;b.Tri(v,v+3,v+1);b.Tri(v+1,v+3,v+4);}}
 void Make(string label,Batch b,bool foliage){if(b.v.Count==0)return;var mesh=new Mesh{name=label,indexFormat=IndexFormat.UInt32};mesh.SetVertices(b.v);mesh.SetColors(b.c);mesh.SetUVs(0,b.uv);mesh.SetTriangles(b.t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var bounds=mesh.bounds;bounds.Expand(2);mesh.bounds=bounds;meshes.Add(mesh);Triangles+=b.t.Count/3;var mat=new Material(Shader.Find("Deepsea/NaturalFoliage"));mats.Add(mat);var go=new GameObject(label);go.layer=4;go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.TwoSided;}
 void OnDestroy(){foreach(var m in meshes)if(m!=null)Destroy(m);foreach(var m in mats)if(m!=null)Destroy(m);if(root!=null)Destroy(root.gameObject);}
}
}
