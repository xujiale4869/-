using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
/// <summary>A sparse authored shallow-water pocket. Decoration has no colliders.</summary>
public sealed class ShoreHabitat : MonoBehaviour {
 public Vector3 NurseryCentre {get;private set;}=new Vector3(49.5f,0,83.5f);
 public int NurseryTufts {get;private set;}
 public int TuftCount {get;private set;}
 public int BladeCount {get;private set;}
 public int RockCount {get;private set;}
 public float HighestBladeTip {get;private set;}=-100;
 readonly List<Mesh> meshes=new List<Mesh>();
 readonly List<Material> materials=new List<Material>();
 OceanSurface ocean;GameDirector game;Transform root;
 readonly System.Random random=new System.Random(7319);
 float Range(float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
 public void Initialize(OceanSurface water){
  ocean=water;game=water.game;
  root=VisualFactory.Group("东岸浅滩 · 仅视觉无碰撞",game.World.Root,Vector3.zero);
  Material shore=GroundMaterial(0,new Color(.7f,.7f,.5f));
  foreach(var r in game.World.Root.GetComponentsInChildren<MeshRenderer>())
   if(r.gameObject.name=="雾岬岛 · 可步行地形")r.sharedMaterial=shore;
  Stones();Grass();
  gameObject.AddComponent<CoastalEcology>().Initialize(game,this);
 }
 Material GroundMaterial(float kind,Color tint){
  var m=new Material(Shader.Find("Deepsea/CoastalGround"));m.SetFloat("_SurfaceKind",kind);m.SetColor("_BaseColor",tint);materials.Add(m);return m;
 }
 void RenderMesh(string name,Mesh mesh,Material material){
  meshes.Add(mesh);var go=new GameObject(name);go.layer=4;go.transform.SetParent(root,false);
  go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
 }
 bool Clear(Vector3 p){
  if(IslandTerrain.Height(p.x,p.z)>-.64f)return false;
  foreach(var landing in game.Expedition.Landings){
   Vector3 a=p-landing.berth,b=p-landing.feet;a.y=b.y=0;
   if(a.sqrMagnitude<30||b.sqrMagnitude<25)return false;
  }
  Vector3 test=p;test.y=1.3f;
  return !Physics.CheckSphere(test,1,Physics.AllLayers,QueryTriggerInteraction.Ignore);
 }
 void Stones(){
  Vector2[] spots={new Vector2(47.5f,84),new Vector2(50.5f,82.5f),new Vector2(53,100),new Vector2(56,103),new Vector2(59.5f,96),new Vector2(61,86),new Vector2(46.5f,106),new Vector2(56.5f,81)};
  var v=new List<Vector3>();var tr=new List<int>();
  foreach(var spot in spots){
   Vector3 p=new Vector3(spot.x,0,spot.y);if(!Clear(p))continue;p.y=ocean.BottomHeight(p.x,p.z)-.03f;
   float rx=Range(.65f,1.25f),rz=Range(.6f,1.1f),ry=Mathf.Min(Range(.28f,.48f),OceanSurface.SeaLevel-.15f-p.y);
   if(ry<.18f)continue;
   int first=v.Count;const int sides=10,rings=4;
   for(int ring=0;ring<=rings;ring++){
    float phi=ring*Mathf.PI/rings;float y=-Mathf.Cos(phi),width=Mathf.Sin(phi);
    for(int i=0;i<=sides;i++){
     float a=i*Mathf.PI*2/sides;float irregular=1+.08f*Mathf.Sin(i*2.1f+RockCount);
     v.Add(p+new Vector3(Mathf.Cos(a)*width*rx*irregular,y*ry,Mathf.Sin(a)*width*rz*irregular));
    }
   }
   for(int ring=0;ring<rings;ring++)for(int i=0;i<sides;i++){
    int a=first+ring*(sides+1)+i,b=a+sides+1;
    tr.Add(a);tr.Add(b);tr.Add(a+1);tr.Add(a+1);tr.Add(b);tr.Add(b+1);
   }
   RockCount++;
  }
  var mesh=new Mesh{name="水下浅色岩石"};mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
  RenderMesh("水下岩石 · 无碰撞",mesh,GroundMaterial(2,new Color(.43f,.48f,.37f)));
 }
 void Grass(){
  Vector2[] patches={new Vector2(49.5f,83.5f),new Vector2(53.5f,101),new Vector2(59,96)};
  var v=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var tr=new List<int>();
  Vector3 nurserySum=Vector3.zero;
  foreach(var patch in patches)for(int tuft=0;tuft<5;tuft++){
   float a=Range(0,Mathf.PI*2),r=Range(.3f,2.5f);
   Vector3 p=new Vector3(patch.x+Mathf.Cos(a)*r,0,patch.y+Mathf.Sin(a)*r);if(!Clear(p))continue;
   p.y=ocean.BottomHeight(p.x,p.z)+.025f;
   if(OceanSurface.SeaLevel-.32f-p.y<.25f)continue;TuftCount++;
   if(patch.y<90){nurserySum+=p;NurseryTufts++;}
   for(int blade=0;blade<13;blade++){
    float direction=Range(0,Mathf.PI*2),phase=Range(0,Mathf.PI*2);
    Vector3 side=new Vector3(Mathf.Cos(direction),0,Mathf.Sin(direction));
    Vector3 basePos=p+new Vector3(Range(-.45f,.45f),0,Range(-.45f,.45f));
    basePos.y=ocean.BottomHeight(basePos.x,basePos.z)+.025f;
    float height=Mathf.Min(Range(.45f,.95f),OceanSurface.SeaLevel-.32f-basePos.y);
    if(height<.2f)continue;
    int start=v.Count;float width=Range(.045f,.075f);
    for(int segment=0;segment<=4;segment++){
     float f=segment/4f;Vector3 centre=basePos+Vector3.up*(height*f)+side*(f*f*Range(.07f,.19f));
     float w=width*(1-f)*(.7f+.3f*Mathf.Sin(f*Mathf.PI));
     v.Add(centre-side*w);v.Add(centre+side*w);
     uv.Add(new Vector2(f,phase));uv.Add(new Vector2(f,phase));
     Color c=Color.Lerp(new Color(.07f,.22f,.13f),new Color(.27f,.43f,.21f),f);
     colors.Add(c);colors.Add(c);
    }
    for(int segment=0;segment<4;segment++){
     int i=start+segment*2;tr.Add(i);tr.Add(i+2);tr.Add(i+1);tr.Add(i+1);tr.Add(i+2);tr.Add(i+3);
    }
    HighestBladeTip=Mathf.Max(HighestBladeTip,basePos.y+height);BladeCount++;
   }
  }
  if(NurseryTufts>0){NurseryCentre=nurserySum/NurseryTufts;NurseryCentre=new Vector3(NurseryCentre.x,0,NurseryCentre.z);}
  var mesh=new Mesh{name="稀疏海草叶片"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
  var bounds=mesh.bounds;bounds.Expand(.6f);mesh.bounds=bounds;
  var m=new Material(Shader.Find("Deepsea/Seagrass"));materials.Add(m);RenderMesh("随水流摆动的海草",mesh,m);
 }
 void OnDestroy(){
  foreach(var m in meshes)if(m!=null)Destroy(m);
  foreach(var m in materials)if(m!=null)Destroy(m);
  if(root!=null)Destroy(root.gameObject);
 }
}
}
