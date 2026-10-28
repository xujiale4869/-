using UnityEngine;
using UnityEngine.Rendering;
using B=Deepsea.ReefLifeGarden.Batch;
namespace Deepsea {
/// <summary>Three reef-bound shoals and small bottom residents. No gameplay colliders.</summary>
public sealed class ReefResidents:MonoBehaviour {
 const int PerSchool=12,Schools=3,Crabs=6;
 sealed class Fish{public Transform root;public Vector3 p,v;public int school;public float phase;}
 sealed class Crab{public Transform root;public Vector3 home,p;public float phase;public bool scared;}
 readonly Fish[] fish=new Fish[Schools*PerSchool];readonly Crab[] crabs=new Crab[Crabs];
 readonly Vector3[] homes=new Vector3[Schools];readonly bool[] scared=new bool[Schools];
 readonly Collider[] overlaps=new Collider[16];
 GameDirector game;OceanSurface water;ReefLifeGarden garden;Transform root;
 Mesh crabMesh,bottomMesh;Material benthos;readonly Material[] fishMats=new Material[Schools];
 MaterialPropertyBlock block;
 public bool Ready {get;private set;}
 public int FishCount {get{return Ready?fish.Length:0;}}
 public int CrabCount {get{return Ready?crabs.Length:0;}}
 public int Stars {get;private set;}public int Urchins {get;private set;}
 public int SchoolScares {get;private set;}public int CrabScares {get;private set;}
 public float MinimumBedClearance {get;private set;}=100;
 public Vector3 SchoolHome(int i){return homes[i];}
 public Vector3 FishPosition(int i){return fish[i].root.position;}
 public Vector3 CrabPosition(int i){return crabs[i].root.position;}
 public void Initialize(OceanSurface ocean,ReefLifeGarden habitat){
  water=ocean;game=ocean.game;garden=habitat;block=new MaterialPropertyBlock();
  root=VisualFactory.Group("礁栖鱼群与底栖生物 · 无碰撞",game.World.Root,Vector3.zero);
  Mesh swimming=Resources.Load<Mesh>("Wildlife/SwimmingFish");
  if(swimming==null||garden.GrassRoots.Count<6){Debug.LogError("Reef residents missing habitat or fish mesh");return;}
  Vector3[] requested={new Vector3(51,0,106),new Vector3(45,0,71),new Vector3(62,0,90)};
  for(int i=0;i<Schools;i++){
   homes[i]=NearestHabitat(requested[i]);
   fishMats[i]=new Material(Shader.Find("Deepsea/ReefFish"));
   Color tint=i==0?new Color(.78f,.65f,.23f,.94f):i==1?new Color(.20f,.54f,.63f,.94f):new Color(.67f,.43f,.24f,.94f);
   fishMats[i].SetColor("_Tint",tint);fishMats[i].SetFloat("_Submerged",1);
   for(int j=0;j<PerSchool;j++){
    int n=i*PerSchool+j;float phase=j*2.39996f+i*1.2f;
    float radius=Mathf.Sqrt(Mathf.Repeat(j*.618034f,.97f))*1.9f;
    Vector3 p=homes[i]+new Vector3(Mathf.Sin(phase),0,Mathf.Cos(phase))*radius;if(!Clear(p,false))p=homes[i];
    var f=new Fish{p=p,v=Vector3.forward,school=i,phase=phase};f.root=Object("礁边鱼群 "+(i+1),swimming,fishMats[i]);f.root.localScale=Vector3.one*(1.02f+(j%4)*.08f);
    block.Clear();block.SetFloat("_Phase",phase);f.root.GetComponent<Renderer>().SetPropertyBlock(block);fish[n]=f;
   }
  }
  benthos=new Material(Shader.Find("Deepsea/ReefBenthos"));BuildBottom();crabMesh=CrabGeometry();
  for(int i=0;i<Crabs;i++){
   Vector3 p=garden.GrassRoots[(i*garden.GrassRoots.Count/Crabs+3)%garden.GrassRoots.Count];p.y=0;
   var c=new Crab{home=p,p=p,phase=i*1.73f};c.root=Object("草甸边的小螃蟹",crabMesh,benthos);c.root.localScale=Vector3.one*(.86f+(i%3)*.10f);
   block.Clear();block.SetFloat("_Phase",c.phase);c.root.GetComponent<Renderer>().SetPropertyBlock(block);crabs[i]=c;
  }
  Ready=true;UpdatePositions(0,Time.time);
 }
 Vector3 NearestHabitat(Vector3 desired){
  Vector3 best=desired;float distance=float.MaxValue;
  foreach(var q in garden.GrassRoots){Vector3 p=q;p.y=0;float d=(p-desired).sqrMagnitude;if(d<distance&&Clear(p,false)){distance=d;best=p;}}
  return best;
 }
 bool Clear(Vector3 p,bool ignoreBoat){
  if(IslandTerrain.Contains(p)||water.BottomHeight(p.x,p.z)>OceanSurface.SeaLevel-.75f)return false;
  p.y=.35f;int n=Physics.OverlapSphereNonAlloc(p,.38f,overlaps,Physics.AllLayers,QueryTriggerInteraction.Ignore);if(n==overlaps.Length)return false;
  for(int i=0;i<n;i++)if(!(ignoreBoat&&overlaps[i].transform.IsChildOf(game.Boat.transform)))return false;
  return true;
 }
 Transform Object(string name,Mesh mesh,Material material){
  var go=new GameObject(name);go.layer=4;go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
  var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go.transform;
 }
 static Mesh MeshOf(B b,string name){
  var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(b.v);mesh.SetColors(b.c);mesh.SetUVs(0,b.uv);mesh.SetTriangles(b.tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 void BuildBottom(){
  var batch=new B();int index=0;
  foreach(var q in garden.GrassRoots){
   if(index++%4!=0)continue;Vector3 p=q+new Vector3(.7f,0,.55f);float bed=water.BottomHeight(p.x,p.z);
   if(OceanSurface.SeaLevel-bed>2.65f||OceanSurface.SeaLevel-bed<.65f)continue;
   p.y=bed-.075f;
   if(Stars<=Urchins){ReefDetails.Star(batch,p,.43f+(index%3)*.07f,index);Stars++;}else{ReefDetails.Urchin(batch,p,index);Urchins++;}
  }
  bottomMesh=MeshOf(batch,"浅海海星与海胆");Object("海星与海胆 · 着床",bottomMesh,benthos);
 }
 Mesh CrabGeometry(){
  var b=new B();Color baseTint=new Color(.34f,.10f,.045f),tip=new Color(.69f,.31f,.10f);
  ReefDetails.Ball(b,new Vector3(0,.18f,0),new Vector3(.32f,.14f,.25f),baseTint,tip);
  for(int side=-1;side<=1;side+=2){
   for(int i=0;i<4;i++){
    Vector3 a=new Vector3(side*.25f,.16f,.15f-i*.12f),k=new Vector3(side*.48f,.13f,.28f-i*.19f),foot=new Vector3(side*.63f,.005f,.34f-i*.22f);
    ReefDetails.Tube(b,a,k,.032f,.026f,baseTint,tip,5,i*.8f);ReefDetails.Tube(b,k,foot,.026f,.012f,tip,baseTint,5,i*.8f);
   }
   Vector3 claw=new Vector3(side*.39f,.23f,.42f);ReefDetails.Tube(b,new Vector3(side*.22f,.18f,.20f),claw,.055f,.072f,baseTint,tip);
   ReefDetails.Tube(b,claw,claw+new Vector3(side*.13f,.04f,.15f),.060f,.012f,tip,tip);ReefDetails.Tube(b,claw,claw+new Vector3(-side*.015f,.025f,.17f),.040f,.010f,tip,tip);
  }
  return MeshOf(b,"原创小型礁蟹");
 }
 void Update(){if(Ready&&!game.Paused)UpdatePositions(Mathf.Min(.05f,Time.deltaTime),Time.time);}
 void UpdatePositions(float dt,float t){
  Vector3 boat=game.Boat.transform.position;boat.y=0;
  for(int school=0;school<Schools;school++){
   bool near=(boat-homes[school]).sqrMagnitude<8.5f*8.5f;if(near&&!scared[school])SchoolScares++;scared[school]=near;
  }
  for(int i=0;i<fish.Length;i++){
   Fish f=fish[i];Vector3 home=homes[f.school],centre=home+new Vector3(Mathf.Sin(t*.18f+f.school)*2.4f,0,Mathf.Cos(t*.13f+f.school)*1.8f);
   Vector3 drift=new Vector3(Mathf.Cos(t*.18f+f.school)*.432f,0,-Mathf.Sin(t*.13f+f.school)*.234f);
   Vector3 forward=drift.normalized,side=Vector3.Cross(Vector3.up,forward);
   // Irregular moving formations share a travel direction rather than outlining an orbit.
   Vector3 target=centre+forward*(Mathf.Sin(f.phase*1.7f)*1.55f)+side*(Mathf.Cos(f.phase*2.3f)*.85f+Mathf.Sin(t*.7f+f.phase)*.12f);
   bool fear=scared[f.school];if(fear){Vector3 away=home-boat;away.y=0;target=home+away.normalized*5.5f+side*Mathf.Sin(f.phase)*.7f;}
   Vector3 velocity=fear?Vector3.ClampMagnitude((target-f.p)*1.4f,2.6f):Vector3.ClampMagnitude(drift+(target-f.p)*1.15f,1.12f);
   if(!fear&&velocity.sqrMagnitude<.09f)velocity=forward*.30f;
   f.v=Vector3.Lerp(f.v,velocity,Mathf.Min(1,dt*4));
   Vector3 p=f.p+f.v*dt;
   if(!Clear(p,fear)){f.v=Quaternion.Euler(0,90,0)*f.v;p=f.p+f.v*dt;if(!Clear(p,fear))p=f.p;}
   f.p=p;float sea=OceanSurface.Height(p,t),bed=water.BottomHeight(p.x,p.z);
   p.y=Mathf.Min(sea-.18f,Mathf.Max(bed+.34f,sea-.58f-Mathf.Sin(t*1.2f+f.phase)*.08f));
   MinimumBedClearance=Mathf.Min(MinimumBedClearance,p.y-bed);f.root.SetPositionAndRotation(p,Quaternion.LookRotation(f.v.sqrMagnitude>.001f?f.v:Vector3.forward));
  }
  for(int i=0;i<crabs.Length;i++){
   Crab c=crabs[i];bool fear=(boat-c.home).sqrMagnitude<7*7;if(fear&&!c.scared)CrabScares++;c.scared=fear;
   Vector3 target=c.home+new Vector3(Mathf.Sin(t*.37f+c.phase)*.8f,0,Mathf.Cos(t*.29f+c.phase)*.48f);
   if(fear){Vector3 away=c.home-boat;away.y=0;target=c.home+away.normalized*1.7f;}
   Vector3 next=Vector3.MoveTowards(c.p,target,dt*(fear?.70f:.13f));
   float bed=water.BottomHeight(next.x,next.z);if(bed<OceanSurface.SeaLevel-.55f&&bed>OceanSurface.SeaLevel-3.8f)c.p=next;
   Vector3 visible=c.p;visible.y=water.BottomHeight(visible.x,visible.z)-.09f;c.root.position=visible;c.root.rotation=Quaternion.Euler(0,Mathf.Sin(t*.25f+c.phase)*28+i*47,0);
  }
 }
 void OnDestroy(){if(root!=null)Destroy(root.gameObject);if(bottomMesh!=null)Destroy(bottomMesh);if(crabMesh!=null)Destroy(crabMesh);if(benthos!=null)Destroy(benthos);foreach(var m in fishMats)if(m!=null)Destroy(m);}
}
}
