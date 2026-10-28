using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
/// <summary>Two small schools plus infrequent flying-fish events. No gameplay colliders.</summary>
public sealed class Wildlife : MonoBehaviour {
 public GameDirector game;
 const int SchoolSize=21,SwimmerCount=42,FlyCount=5;
 static readonly Vector3[] Centres={new Vector3(18,0,-2),new Vector3(58,0,61)};
 sealed class Swimmer {public Transform root;public Vector3 position,velocity;public float size;}
 sealed class Flyer {public Transform root,wings,shadow;public Vector3 origin,direction;public float start,duration,length;public bool launched,finished;}
 readonly Swimmer[] swimmers=new Swimmer[SwimmerCount];
 readonly Flyer[] flyers=new Flyer[FlyCount];
 readonly Vector3[] nextPosition=new Vector3[SwimmerCount],nextVelocity=new Vector3[SwimmerCount];
 readonly System.Random random=new System.Random(61219);
 Transform visuals;Mesh fishMesh,wingMesh;Material swimMaterial,flyMaterial,wingMaterial,shadowMaterial;
 MarineWaterEffects effects;float nextFlight;bool inFlight;
 public int FishCount {get{return swimmers[0]==null?0:SwimmerCount;}}
 public bool ImportedMeshLoaded {get;private set;}
 public int FlightStarts {get;private set;}
 public bool AmbientFlights {get;set;}=true;
 public int FlightCompletions {get;private set;}
 public int BoatAvoidanceCount {get;private set;}
 public float MinimumFlightClearance {get;private set;}=100;
 public MarineWaterEffects Effects {get{return effects;}}
 public Vector3 FishPosition(int i){return swimmers[i].root.position;}
 public float MaximumUnderwaterError(){
  float max=0;foreach(var f in swimmers)max=Mathf.Max(max,Mathf.Abs(f.root.position.y-OceanSurface.Height(f.root.position,Time.time)));
  return max;
 }
 float Range(float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
 void Start(){
  visuals=VisualFactory.Group("海湾生物 · 鱼群与飞鱼",transform,Vector3.zero);
  fishMesh=Resources.Load<Mesh>("Wildlife/SwimmingFish");ImportedMeshLoaded=fishMesh!=null;
  if(fishMesh==null){Debug.LogError("Missing imported marine fish mesh");enabled=false;return;}
  swimMaterial=NewMaterial(new Color(.22f,.52f,.48f,.78f),1);
  flyMaterial=NewMaterial(new Color(.68f,.89f,.94f,1),0);
  wingMaterial=NewMaterial(new Color(.35f,.72f,.79f,.88f),0);
  shadowMaterial=NewMaterial(new Color(.025f,.20f,.23f,.24f),1);
  effects=gameObject.AddComponent<MarineWaterEffects>();effects.Initialize();
  for(int i=0;i<SwimmerCount;i++){
   Vector3 home=Centres[i/SchoolSize],p=home;
   for(int attempt=0;attempt<35;attempt++){
    float a=Range(0,Mathf.PI*2),r=Range(1,5.5f);
    Vector3 candidate=home+new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);
    if(IsClear(candidate,1)){p=candidate;break;}
   }
   float angle=Range(0,Mathf.PI*2);
   var f=new Swimmer{position=p,velocity=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*1.1f,size=Range(.75f,1.15f)};
   f.root=MakeFish("浅海银鳞鱼",swimMaterial,i);f.root.localScale=Vector3.one*f.size;
   f.root.position=p;swimmers[i]=f;
  }
  Vector2[] outline={new Vector2(-.07f,.28f),new Vector2(-.62f,-.15f),new Vector2(-.7f,-.37f),new Vector2(-.18f,-.21f),new Vector2(0,-.26f),new Vector2(.18f,-.21f),new Vector2(.7f,-.37f),new Vector2(.62f,-.15f),new Vector2(.07f,.28f)};
  wingMesh=VisualFactory.Prism(outline,-.008f,.008f);
  for(int i=0;i<FlyCount;i++){
   var f=new Flyer();f.root=MakeFish("飞鱼 · 银背长鳍",flyMaterial,i+50);
   f.root.localScale=Vector3.one*(.95f+i*.06f);
   f.wings=MakeMesh("展开的胸鳍",wingMesh,wingMaterial,f.root);
   f.shadow=MakeFish("水面下的飞鱼投影",shadowMaterial,i+80);
   f.shadow.localScale=new Vector3(1,.12f,1);
   f.root.gameObject.SetActive(false);f.shadow.gameObject.SetActive(false);flyers[i]=f;
  }
  nextFlight=Time.time+Range(9,14);
 }
 Material NewMaterial(Color tint,float submerged){
  var m=new Material(Shader.Find("Deepsea/MarineFish"));m.SetColor("_Tint",tint);m.SetFloat("_Submerged",submerged);return m;
 }
 Transform MakeMesh(string label,Mesh mesh,Material material,Transform parent){
  var go=new GameObject(label);go.layer=4;go.transform.SetParent(parent,false);
  go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;
  r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return go.transform;
 }
 Transform MakeFish(string label,Material material,int phase){
  Transform root=MakeMesh(label,fishMesh,material,visuals);
  var block=new MaterialPropertyBlock();block.SetFloat("_Phase",phase*1.731f);
  root.GetComponent<Renderer>().SetPropertyBlock(block);return root;
 }
 bool IsClear(Vector3 p,float margin){
  if(IslandTerrain.Contains(p))return false;
  p.y=.45f;
  return !Physics.CheckSphere(p,margin,Physics.AllLayers,QueryTriggerInteraction.Ignore);
 }
 void Update(){
  if(game==null||game.Boat==null||fishMesh==null||game.Paused)return;
  float dt=Mathf.Min(Time.deltaTime,.05f),t=Time.time;Vector3 boat=game.Boat.transform.position;
  for(int i=0;i<SwimmerCount;i++){
   Swimmer f=swimmers[i];int school=i/SchoolSize,count=0;Vector3 heading=Vector3.zero,separation=Vector3.zero,centre=Vector3.zero;
   for(int j=school*SchoolSize;j<(school+1)*SchoolSize;j++){
    if(i==j)continue;Vector3 delta=swimmers[j].position-f.position;float d=delta.sqrMagnitude;
    if(d<25){count++;heading+=swimmers[j].velocity.normalized;centre+=swimmers[j].position;if(d<1.5f)separation-=delta/Mathf.Max(.08f,d);}
   }
   Vector3 target=Centres[school]+new Vector3(Mathf.Sin(t*.14f+school*2)*3,0,Mathf.Cos(t*.11f+school)*3);
   Vector3 away=f.position-boat;away.y=0;float scare=Mathf.Clamp01(1-away.magnitude/8);
   float speed=1.1f+scare*2.3f;
   Vector3 accel=SchoolSteering.Acceleration(f.velocity,heading,separation,centre,f.position,count,target,speed);
   if(scare>0){accel+=away.normalized*scare*8;BoatAvoidanceCount++;}
   Vector3 direction=f.velocity.normalized;
   if(!IsClear(f.position+direction*2.6f,.8f)){
    Vector3 right=Quaternion.Euler(0,65,0)*direction;
    Vector3 left=Quaternion.Euler(0,-65,0)*direction;
    Vector3 safe=IsClear(f.position+right*2.6f,.8f)?right:IsClear(f.position+left*2.6f,.8f)?left:-direction;
    accel+=safe*7;
   }
   Vector3 velocity=f.velocity+accel*dt;
   velocity=velocity.normalized*Mathf.Clamp(velocity.magnitude,.7f,speed);
   Vector3 p=f.position+velocity*dt;
   if(!IsClear(p,.45f)){velocity=Quaternion.Euler(0,110,0)*velocity;p=f.position;}
   nextPosition[i]=p;nextVelocity[i]=velocity;
  }
  for(int i=0;i<SwimmerCount;i++){
   var f=swimmers[i];f.position=nextPosition[i];f.velocity=nextVelocity[i];
   Vector3 p=f.position;p.y=OceanSurface.Height(p,t)-.30f-.10f*Mathf.Sin(t*1.6f+i);
   f.root.SetPositionAndRotation(p,Quaternion.LookRotation(f.velocity));
   f.root.gameObject.SetActive(game.CameraFollow.Overview||(boat-p).sqrMagnitude<70*70);
  }
  UpdateFlights(t);
  if(AmbientFlights&&!inFlight&&t>=nextFlight){
   bool started=false;
   if(!game.CameraFollow.Overview&&!game.Expedition.OnFoot&&(boat-game.World.DockPosition).sqrMagnitude>18*18){
    for(int attempt=0;attempt<6&&!started;attempt++){
     float side=attempt%2==0?1:-1;
     Vector3 forward=Quaternion.Euler(0,Range(-35,35),0)*game.Boat.transform.forward;
     Vector3 right=Vector3.Cross(Vector3.up,forward);
     started=TryStartFlight(boat+forward*7+right*side*Range(8,13),(forward-right*side*.23f).normalized);
    }
   }
   nextFlight=t+(started?Range(27,44):4);
  }
 }
 public bool TryStartFlight(Vector3 origin,Vector3 direction){
  if(inFlight||fishMesh==null)return false;direction.y=0;direction.Normalize();
  for(int i=0;i<=12;i++)if(!IsClear(origin+direction*(i*2),2.5f))return false;
  Vector3 right=Vector3.Cross(Vector3.up,direction);
  var origins=new Vector3[FlyCount];var directions=new Vector3[FlyCount];var lengths=new float[FlyCount];
  for(int i=0;i<FlyCount;i++){
   origins[i]=origin+right*Range(-2.2f,2.2f)+direction*Range(-1.8f,1.8f);
   directions[i]=Quaternion.Euler(0,Range(-5,5),0)*direction;lengths[i]=Range(17,22);
   for(int step=0;step<=12;step++)if(!IsClear(origins[i]+directions[i]*(lengths[i]*step/12),1.4f))return false;
  }
  for(int i=0;i<FlyCount;i++){
   var f=flyers[i];f.origin=origins[i];f.direction=directions[i];f.length=lengths[i];
   f.start=Time.time+i*.16f+Range(0,.08f);f.duration=Range(2.7f,3.5f);
   f.launched=f.finished=false;f.root.gameObject.SetActive(false);f.shadow.gameObject.SetActive(false);
  }
  inFlight=true;FlightStarts++;return true;
 }
 void UpdateFlights(float t){
  if(!inFlight)return;bool any=false;
  for(int i=0;i<FlyCount;i++){
   var f=flyers[i];float age=t-f.start;
   if(f.finished)continue;any=true;if(age<0)continue;
   if(!f.launched){f.launched=true;effects.Impact(f.origin,.38f);}
   float u=Mathf.Clamp01(age/f.duration);Vector3 p=f.origin+f.direction*(u*f.length);
   float height=1.35f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.65f);
   float sea=OceanSurface.Height(p,t);p.y=sea+.065f+height;
   float pitch=Mathf.Clamp(Mathf.Cos(u*Mathf.PI)*.36f,-.36f,.36f);
   f.root.gameObject.SetActive(true);
   f.root.SetPositionAndRotation(p,Quaternion.LookRotation(f.direction+Vector3.up*pitch)*Quaternion.Euler(0,0,Mathf.Sin(u*Mathf.PI)*7*(i%2==0?1:-1)));
   float opening=Mathf.SmoothStep(0,1,u/.12f)*(1-Mathf.SmoothStep(0,1,(u-.84f)/.16f));
   f.wings.localScale=new Vector3(.14f+opening*.86f,1,1);
   Vector3 shadow=p+new Vector3(.2f,0,-.15f);shadow.y=OceanSurface.Height(shadow,t)+.025f;
   f.shadow.gameObject.SetActive(true);f.shadow.SetPositionAndRotation(shadow,Quaternion.LookRotation(f.direction));
   MinimumFlightClearance=Mathf.Min(MinimumFlightClearance,p.y-sea);
   if(age>=f.duration){
    f.finished=true;f.root.gameObject.SetActive(false);f.shadow.gameObject.SetActive(false);
    effects.Impact(p,1);FlightCompletions++;
   }
  }
  if(!any)inFlight=false;
 }
 void OnDestroy(){
  if(visuals!=null)Destroy(visuals.gameObject);if(wingMesh!=null)Destroy(wingMesh);
  foreach(var m in new[]{swimMaterial,flyMaterial,wingMaterial,shadowMaterial})if(m!=null)Destroy(m);
 }
}
}
