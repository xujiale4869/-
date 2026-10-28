using System.Collections.Generic;
using UnityEngine;
namespace Deepsea {
/// <summary>Original static visual current field; does not change wave height or boat physics.</summary>
public sealed class ShallowWaterFlow : MonoBehaviour {
 public static bool Enabled=true;
 public static readonly Vector2 OceanDirection=new Vector2(.42f,.9075f).normalized;
 public const int MapSize=257;
 public static readonly Vector4 Region=new Vector4(8,46,1f/112,1f/112);
 readonly List<Vector3> obstacles=new List<Vector3>();
 Texture2D flowMap,normalMap;Material material;
 public bool Ready {get;private set;}
 public int ObstacleCount {get{return obstacles.Count;}}
 public double BakeMilliseconds {get;private set;}
 public void Initialize(Material water,Transform root){
  var timer=System.Diagnostics.Stopwatch.StartNew();material=water;
  foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
   if(renderer.gameObject.name!="湿润露礁")continue;
   var b=renderer.bounds;
   if(b.center.x<Region.x||b.center.x>120||b.center.z<46||b.center.z>158)continue;
   obstacles.Add(new Vector3(b.center.x,b.center.z,Mathf.Max(b.extents.x,b.extents.z)+.7f));
  }
  var data=new Color[MapSize*MapSize];
  for(int y=0;y<MapSize;y++)for(int x=0;x<MapSize;x++){
   Vector2 p=new Vector2(Region.x+x/(float)(MapSize-1)/Region.z,Region.y+y/(float)(MapSize-1)/Region.w);
   Vector2 v=FlowAt(p);float speed=v.magnitude;Vector2 dir=v/Mathf.Max(speed,.001f);
   data[y*MapSize+x]=new Color(dir.x*.5f+.5f,dir.y*.5f+.5f,speed/1.5f,1);
  }
  flowMap=new Texture2D(MapSize,MapSize,TextureFormat.RGBAHalf,false,true){name="原创东岸绕礁流向",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
  flowMap.SetPixels(data);flowMap.Apply(false,true);
  const int side=256;var normalData=new Color[side*side];
  // Periodic analytic wave slopes: smooth at every mip level, without polygonal source facets.
  Vector2[] modes={new Vector2(2,1),new Vector2(3,-1),new Vector2(1,4),new Vector2(5,2),new Vector2(7,-3),new Vector2(4,7)};
  float[] weights={.33f,.24f,.19f,.12f,.07f,.05f};
  for(int y=0;y<side;y++)for(int x=0;x<side;x++){
   Vector2 slope=Vector2.zero,p=new Vector2(x/(float)side,y/(float)side);
   for(int k=0;k<modes.Length;k++)slope+=modes[k].normalized*(Mathf.Cos(2*Mathf.PI*Vector2.Dot(modes[k],p)+k*1.731f)*weights[k]);
   normalData[y*side+x]=new Color(slope.x*.5f+.5f,slope.y*.5f+.5f,1,1);
  }
  normalMap=new Texture2D(side,side,TextureFormat.RGBA32,true,true){name="原创柔和流动水纹",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
  normalMap.SetPixels(normalData);normalMap.Apply(true,true);
  material.SetTexture("_CurrentMap",flowMap);material.SetTexture("_CurrentNormals",normalMap);
  material.SetVector("_CurrentRegion",Region);material.SetFloat("_FlowPolish",Enabled?1:0);
  Ready=true;BakeMilliseconds=timer.Elapsed.TotalMilliseconds;
 }
 public Vector2 FlowAt(Vector2 p){
  Vector2 v=OceanDirection;
  Vector2 local=new Vector2((p.x-8)/38,(p.y-94)/42);
  float shoreDistance=(local.magnitude-.91465f)*38;
  Vector2 n=new Vector2(local.x/38,local.y/42).normalized;
  float influence=(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(shoreDistance/18)))*Mathf.SmoothStep(0,1,Mathf.Clamp01((p.x-28)/10));
  // Shore current is predominantly tangential; a small cross-shore component remains.
  Vector2 tangent=v-n*Vector2.Dot(v,n)*.92f;
  if(tangent.sqrMagnitude<.03f)tangent=new Vector2(-n.y,n.x)*.3f;
  v=Vector2.Lerp(v,tangent,influence*.94f);
  foreach(var o in obstacles){
   Vector2 d=p-new Vector2(o.x,o.y);float distance=d.magnitude;
   Vector2 outward=distance>.001f?d/distance:Vector2.right;
   float q=Mathf.Min(.94f,o.z*o.z/Mathf.Max(distance*distance,.01f));
   q*=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((distance-o.z*2)/(o.z*3)));
   v=v*(1+q)-outward*(2*q*Vector2.Dot(v,outward));
  }
  float speed=Mathf.Clamp(v.magnitude,.25f,1.35f);
  return (v.sqrMagnitude<.0001f?OceanDirection:v.normalized)*speed;
 }
 void OnDestroy(){if(flowMap!=null)Destroy(flowMap);if(normalMap!=null)Destroy(normalMap);}
}
}
