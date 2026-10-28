using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea {
/// <summary>Boat Attack Gerstner sampling with a URP14 orthographic reflection adapter.</summary>
public sealed class OceanSurface : MonoBehaviour {
 public const float SeaLevel=-.22f;
 public static OceanSurface Active {get;private set;}
 public Material Material {get;private set;}
 public GameDirector game;
 public int ReflectionRenderCount {get;private set;}
 readonly Vector4[] waves=new Vector4[20];
 Camera mirror;RenderTexture reflection;bool rendering;
 Texture2D depthMap;Mesh seabedMesh;Material seabedMaterial;GameObject seabed;
 readonly List<Bounds> shoals=new List<Bounds>();
 public bool BathymetryReady {get;private set;}
 public double SeabedBakeMilliseconds {get;private set;}
 static readonly float[] Lengths={32,18,10,6};
 static readonly float[] Directions={18,42,-24,76};
 static readonly float[] Amplitudes={.8f,.45f,.25f,.12f};
 public void Initialize(GameDirector g,Material m){
  game=g;Material=m;Active=this;gameObject.layer=4;
  Shader.SetGlobalFloat("_BayCoastDetail",CoastalDetail.Enabled?1:0);
  float scale=Mathf.Clamp(g.settings.waveHeight*3.6f,.25f,.65f);
  for(int i=0;i<4;i++)waves[i]=new Vector4(Amplitudes[i]*scale,Directions[i],Lengths[i],0);
  Material.SetInt("_WaveCount",4);Material.SetVectorArray("waveData",waves);
  Material.SetTexture("_NormalMap",Resources.Load<Texture2D>("Water/WaterNormals"));
  Material.SetTexture("_FoamMap",Resources.Load<Texture2D>("Water/basicFoam"));
  Material.SetColor("_DeepColor",new Color(.018f,.29f,.41f));
  Material.SetColor("_ShallowColor",new Color(.07f,.64f,.54f));
  Material.SetFloat("_SurfacePolish",SeabedRelief.Enabled?1:0);
  Shader.SetGlobalFloat("_BayWaterPolish",SeabedRelief.Enabled?1:0);
  Shader.SetGlobalTexture("_BayCausticMap",Resources.Load<Texture2D>("Water/WaterCaustics"));
  RenderPipelineManager.beginCameraRendering+=RenderReflection;
 }

 void Start(){
  var timer=System.Diagnostics.Stopwatch.StartNew();
  // One continuous visual seabed removes empty-background / rectangular mesh transitions.
  foreach(var r in game.World.Root.GetComponentsInChildren<MeshRenderer>())
   if(r.gameObject.name=="湿润露礁")shoals.Add(r.bounds);
  const int cells=512;const float size=700;int side=cells+1;
  var vertices=new Vector3[side*side];var triangles=new int[cells*cells*6];
  var data=new float[side*side];
  for(int z=0;z<side;z++)for(int x=0;x<side;x++){
   float wx=(x/(float)cells-.5f)*size,wz=(z/(float)cells-.5f)*size;
   float height=BottomHeight(wx,wz);
   vertices[z*side+x]=new Vector3(wx,height-.12f,wz);
   data[z*side+x]=height;
  }
  int k=0;for(int z=0;z<cells;z++)for(int x=0;x<cells;x++){
   int i=z*side+x;triangles[k++]=i;triangles[k++]=i+side;triangles[k++]=i+1;
   triangles[k++]=i+1;triangles[k++]=i+side;triangles[k++]=i+side+1;
  }
  seabedMesh=new Mesh{name="连续浅滩海床",indexFormat=IndexFormat.UInt32};
  seabedMesh.vertices=vertices;seabedMesh.triangles=triangles;seabedMesh.RecalculateNormals();seabedMesh.RecalculateBounds();
  seabed=new GameObject("连续海床 · 仅视觉无碰撞");seabed.transform.SetParent(game.World.Root,false);
  seabed.layer=4;seabed.AddComponent<MeshFilter>().sharedMesh=seabedMesh;
  seabedMaterial=new Material(Shader.Find("Deepsea/CoastalGround"));
  seabedMaterial.SetFloat("_SurfaceKind",1);
  var renderer=seabed.AddComponent<MeshRenderer>();renderer.sharedMaterial=seabedMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
  depthMap=new Texture2D(side,side,TextureFormat.RFloat,false,true){name="连续海床高度",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
  depthMap.SetPixelData(data,0);depthMap.Apply(false,true);
  Material.SetTexture("_Bathymetry",depthMap);Material.SetFloat("_BathymetryReady",1);
  BathymetryReady=true;
  SeabedBakeMilliseconds=timer.Elapsed.TotalMilliseconds;
  gameObject.AddComponent<ShoreHabitat>().Initialize(this);
  gameObject.AddComponent<ShorelineAtmosphere>().Initialize(game);
  gameObject.AddComponent<ShallowWaterFlow>().Initialize(Material,game.World.Root);
  gameObject.AddComponent<CoastalDetail>().Initialize(game);
 }
 public float BottomHeight(float x,float z){
  float height=IslandTerrain.Height(x,z);
  float pattern=SeabedRelief.Enabled?SeabedRelief.Pattern(x,z):0;
  if(SeabedRelief.Enabled)height=SeabedRelief.SandHeight(x,z,height,pattern);
  foreach(var b in shoals){
   float rx=Mathf.Max(2,b.extents.x),rz=Mathf.Max(2,b.extents.z);
   float dx=(x-b.center.x)/rx,dz=(z-b.center.z)/rz;
   float r=Mathf.Sqrt(dx*dx+dz*dz);
   float edge=Mathf.Clamp01((r-.82f)/1.7f);
   float shelf=Mathf.Lerp(-.65f,-12,Mathf.SmoothStep(0,1,edge));
   if(SeabedRelief.Enabled)shelf=SeabedRelief.ReefShelf(x,z,b,pattern);
   height=Mathf.Max(height,shelf);
  }
  return Mathf.Min(-.58f,height);
 }

 public static float Height(Vector3 p,float time){
  return Active==null?SeaLevel:Active.SurfaceHeight(p,time);
 }
 float SurfaceHeight(Vector3 p,float time){
  // Invert the same small horizontal displacement used by the shader.
  Vector2 q=new Vector2(p.x,p.z);
  for(int pass=0;pass<2;pass++){
   Vector2 offset=Vector2.zero;
   for(int i=0;i<4;i++){float k=Mathf.PI*2/Lengths[i],d=Directions[i]*Mathf.Deg2Rad;
    Vector2 dir=new Vector2(Mathf.Sin(d),Mathf.Cos(d));
    float phase=k*Vector2.Dot(dir,q)-time*Mathf.Sqrt(9.8f*k);
    offset+=dir*(2/(k*4)*Mathf.Cos(phase)*.14f);
   }
   q=new Vector2(p.x,p.z)-offset;
  }
  float y=SeaLevel;
  for(int i=0;i<4;i++){float k=Mathf.PI*2/Lengths[i],d=Directions[i]*Mathf.Deg2Rad;
   y+=waves[i].x*.25f*Mathf.Sin(k*(q.x*Mathf.Sin(d)+q.y*Mathf.Cos(d))-time*Mathf.Sqrt(9.8f*k));}
  return y;
 }
 void LateUpdate(){
  if(game==null||game.Boat==null)return;
  Vector3 p=game.Boat.transform.position,dir=game.Boat.transform.forward;
  Shader.SetGlobalVector("_BayBoat",new Vector4(p.x,p.y,p.z,game.Boat.Speed));
  Shader.SetGlobalVector("_BayBoatDirection",new Vector4(dir.x,dir.z,0,0));
 }
 void RenderReflection(ScriptableRenderContext context,Camera source){
  if(rendering||game==null||source!=game.Camera||Material==null)return;
  if(mirror==null){var go=new GameObject("海面反射镜头");go.hideFlags=HideFlags.HideAndDontSave;
   mirror=go.AddComponent<Camera>();mirror.enabled=false;}
  int w=Mathf.Clamp(source.pixelWidth/2,256,1280),h=Mathf.Clamp(source.pixelHeight/2,144,720);
  if(reflection==null||reflection.width!=w||reflection.height!=h){
   if(reflection!=null){reflection.Release();Destroy(reflection);}
   reflection=new RenderTexture(w,h,24,RenderTextureFormat.ARGBHalf){name="归潮湾柔化水面反射",useMipMap=true,autoGenerateMips=false,filterMode=FilterMode.Trilinear};
   reflection.Create();
  }
  mirror.CopyFrom(source);mirror.enabled=false;mirror.targetTexture=reflection;
  mirror.backgroundColor=new Color(.32f,.60f,.68f);
  mirror.cameraType=CameraType.Reflection;mirror.useOcclusionCulling=false;
  mirror.cullingMask=source.cullingMask&~(1<<4)&~(1<<5);
  var data=mirror.GetUniversalAdditionalCameraData();
  data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
  data.renderShadows=false;data.renderPostProcessing=false;
  // Reflection matrix is the horizontal-plane specialization of Boat Attack PlanarReflections.
  Matrix4x4 r=Matrix4x4.identity;r.m11=-1;r.m13=2*SeaLevel;
  Vector3 pos=source.transform.position;pos.y=2*SeaLevel-pos.y;
  mirror.transform.SetPositionAndRotation(pos,Quaternion.Euler(-source.transform.eulerAngles.x,source.transform.eulerAngles.y,0));
  mirror.worldToCameraMatrix=source.worldToCameraMatrix*r;
  Vector3 cp=mirror.worldToCameraMatrix.MultiplyPoint(new Vector3(0,SeaLevel+.04f,0));
  Vector3 cn=mirror.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
  mirror.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(cn.x,cn.y,cn.z,-Vector3.Dot(cp,cn)));
  bool oldCulling=GL.invertCulling;bool oldFog=RenderSettings.fog;
  try{
   rendering=true;GL.invertCulling=!oldCulling;RenderSettings.fog=false;
   UniversalRenderPipeline.RenderSingleCamera(context,mirror);reflection.GenerateMips();ReflectionRenderCount++;
   Shader.SetGlobalTexture("_BayReflection",reflection);Shader.SetGlobalFloat("_BayReflectionReady",1);
  }finally{GL.invertCulling=oldCulling;RenderSettings.fog=oldFog;rendering=false;}
 }
 void OnDestroy(){
  RenderPipelineManager.beginCameraRendering-=RenderReflection;
  if(Active==this){Active=null;Shader.SetGlobalFloat("_BayReflectionReady",0);}
  if(depthMap!=null)Destroy(depthMap);if(seabedMesh!=null)Destroy(seabedMesh);if(seabedMaterial!=null)Destroy(seabedMaterial);if(seabed!=null)Destroy(seabed);
  if(mirror!=null)Destroy(mirror.gameObject);
  if(reflection!=null){reflection.Release();Destroy(reflection);}
 }
}
}

