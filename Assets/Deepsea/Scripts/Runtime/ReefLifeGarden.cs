using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ProceduralModeling;
namespace Deepsea {
/// <summary>Authored sparse reef colonies and nursery grass. Geometry is generated once.</summary>
public sealed class ReefLifeGarden : MonoBehaviour {
 public static bool Enabled=true;
 public static bool RichEnabled=true;
 public int OriginalTufts {get;private set;}
 public int OriginalCorals {get;private set;}
 Renderer previousGrass;Material oldGrass,newGrass;
 public bool Ready {get;private set;}
 public int CoralCount {get;private set;}
 public int BranchCorals {get;private set;}
 public int PlateCorals {get;private set;}
 public int DomeCorals {get;private set;}
 public int FanCorals {get;private set;}
 public int SpongeGroups {get;private set;}
 public int SpongeTubes {get;private set;}
 public int Anemones {get;private set;}
 public int TuftCount {get;private set;}
 public int BladeCount {get;private set;}
 public int TriangleCount {get;private set;}
 public int RendererCount {get;private set;}
 public float HighestTip {get;private set;}=-100;
 public float MinimumLandingDistance {get;private set;}=100;
 public double BakeMilliseconds {get;private set;}
 public readonly List<Vector3> CoralRoots=new List<Vector3>();
 public readonly List<Vector3> GrassRoots=new List<Vector3>();
 public Vector3 NurseryCentre {get;private set;}
 OceanSurface ocean;GameDirector game;Transform root;
 readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
 readonly System.Random random=new System.Random(916431);
 float Range(float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
 public sealed class Batch {
  public readonly List<Vector3> v=new List<Vector3>();public readonly List<Color> c=new List<Color>();
  public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> tr=new List<int>();
  public void Vertex(Vector3 p,Color color,Vector2 tex){v.Add(p);c.Add(color);uv.Add(tex);}
  public void Tri(int a,int b,int c){tr.Add(a);tr.Add(b);tr.Add(c);}
 }
 public void Initialize(OceanSurface water){
  var watch=System.Diagnostics.Stopwatch.StartNew();ocean=water;game=water.game;
  var previous=GetComponent<ReefGarden>();NurseryCentre=previous.NurseryCentre;
  OriginalTufts=previous.TuftCount;OriginalCorals=previous.CoralCount;
  CoralCount=previous.CoralCount;BranchCorals=previous.BranchCorals;PlateCorals=previous.PlateCorals;TuftCount=previous.TuftCount;BladeCount=previous.BladeCount;TriangleCount=previous.TriangleCount;RendererCount=previous.RendererCount;
  HighestTip=previous.HighestTip;MinimumLandingDistance=previous.MinimumLandingDistance;
  CoralRoots.AddRange(previous.CoralRoots);GrassRoots.AddRange(previous.GrassRoots);
  Shader.SetGlobalFloat("_BayReefLife",Enabled?1:0);
  if(!Enabled){Ready=true;return;}
  root=VisualFactory.Group("生态带补充 · 无碰撞",game.World.Root,Vector3.zero);
  var coral=new Batch();var grass=new Batch();
  PlantCorals(coral,new Vector2(-32,99),24,0,7.0f);
  PlantCorals(coral,new Vector2(45,65),14,0,6.0f);
  PlantCorals(coral,new Vector2(48,112),14,1.0f,5.0f);
  PlantCorals(coral,new Vector2(59,105),12,0,5.5f);
  PlantCorals(coral,new Vector2(64,89),8,2.0f,5.0f);
  PlantGrass(grass,new Vector2(-32,96),42,6.0f,8.5f);
  PlantGrass(grass,new Vector2(44,68),32,7.0f,5.0f);
  PlantGrass(grass,new Vector2(51,108),28,7.0f,6.0f);
  PlantGrass(grass,new Vector2(58,92),20,5.5f,5.0f);
  for(int i=OriginalCorals;i<CoralRoots.Count;i+=2){Vector3 p;if(!Place(new Vector2(CoralRoots[i].x+.9f,CoralRoots[i].z+.6f),.3f,false,out p))continue;ReefDetails.Anemone(grass,p,Mathf.Min(.85f,OceanSurface.SeaLevel-.50f-p.y),i);Anemones++;}
  foreach(var renderer in game.World.Root.GetComponentsInChildren<MeshRenderer>())if(renderer.gameObject.name=="浅滩宽叶海草"){
   previousGrass=renderer;oldGrass=renderer.sharedMaterial;newGrass=new Material(Shader.Find("Deepsea/ReefFoliage"));renderer.sharedMaterial=newGrass;break;
  }
  AddBatch("礁边暖色珊瑚",coral,"Deepsea/ReefCoral",.04f);
  AddBatch("浅滩宽叶海草",grass,RichEnabled?"Deepsea/ReefFoliage":"Deepsea/Seagrass",.8f);
  foreach(var p in coral.v)HighestTip=Mathf.Max(HighestTip,p.y);
  foreach(var p in grass.v)HighestTip=Mathf.Max(HighestTip,p.y);
  BakeMilliseconds=watch.Elapsed.TotalMilliseconds;Ready=true;
  if(RichEnabled)gameObject.AddComponent<ReefResidents>().Initialize(ocean,this);
 }
 bool Place(Vector2 spot,float footprint,bool coral,out Vector3 p){
  p=new Vector3(spot.x,0,spot.y);float bed=ocean.BottomHeight(p.x,p.z),depth=OceanSurface.SeaLevel-bed;
  if(depth<(RichEnabled?.65f:.9f)||depth>(coral?(RichEnabled?3.3f:3.0f):3.9f))return false;
  if(coral&&(new Vector2(p.x-NurseryCentre.x,p.z-NurseryCentre.z)).sqrMagnitude<8.5f*8.5f)return false;
  float lo=bed,hi=bed;
  for(int i=0;i<8;i++){float a=i*Mathf.PI*.25f;float y=ocean.BottomHeight(p.x+Mathf.Cos(a)*footprint,p.z+Mathf.Sin(a)*footprint);lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}
  if(hi-lo>(coral?(RichEnabled?.85f:.65f):.8f))return false;
  foreach(var landing in game.Expedition.Landings){
   Vector3 a=p-landing.berth,b=p-landing.feet;a.y=b.y=0;
   float berth=RichEnabled?3.8f:6.0f,feet=RichEnabled?4.0f:5.5f;
   if(a.sqrMagnitude<berth*berth||b.sqrMagnitude<feet*feet)return false;
  }
  Vector3 test=p;test.y=.6f;
  if(Physics.CheckSphere(test,.72f,Physics.AllLayers,QueryTriggerInteraction.Ignore))return false;
  p.y=lo-.10f;
  if(coral&&Physics.CheckSphere(p+Vector3.up*.8f,.42f,Physics.AllLayers,QueryTriggerInteraction.Ignore))return false;
  return true;
 }
 void Record(Vector3 p,bool coral){
  (coral?CoralRoots:GrassRoots).Add(p);
  foreach(var landing in game.Expedition.Landings){Vector3 delta=p-landing.berth;delta.y=0;MinimumLandingDistance=Mathf.Min(MinimumLandingDistance,delta.magnitude);}
 }
 void PlantCorals(Batch batch,Vector2 centre,int count,float minRadius,float maxRadius){
  int added=0;
  for(int attempt=0;attempt<(RichEnabled?1100:400)&&added<count;attempt++){
   float a=Range(0,Mathf.PI*2),r=Range(minRadius,maxRadius);Vector3 p;
   if(!Place(centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,.48f,true,out p))continue;
   float spacing=RichEnabled?1.65f:1.25f;
   bool crowded=false;foreach(var other in CoralRoots){var d=p-other;d.y=0;if(d.sqrMagnitude<spacing*spacing){crowded=true;break;}}
   if(crowded)continue;
   float height=Mathf.Min(RichEnabled?Range(1.25f,2.0f):Range(.95f,1.65f),OceanSurface.SeaLevel-.50f-p.y);
   if(height<.6f)continue;
   Color baseColor=Color.Lerp(new Color(.38f,.12f,.07f),new Color(.45f,.16f,.29f),Range(0,.65f));
   Color tip=Color.Lerp(new Color(.86f,.40f,.16f),new Color(.74f,.30f,.43f),Range(0,.6f));
   int kind=RichEnabled?CoralRoots.Count%6:CoralCount%3;
   if(RichEnabled&&kind==3){ReefDetails.Dome(batch,p,height*.55f,new Color(.52f,.48f,.16f),new Color(.70f,.66f,.27f));DomeCorals++;}
   else if(RichEnabled&&kind==4){SpongeTubes+=ReefDetails.Sponges(batch,p,height,new Color(.39f,.20f,.48f),CoralRoots.Count);SpongeGroups++;}
   else if(RichEnabled&&kind==5){ReefDetails.Fan(batch,p,height,new Color(.49f,.18f,.25f),new Color(.85f,.35f,.30f),CoralRoots.Count);FanCorals++;}
   else if(kind==2){Plate(batch,p,height*.55f,baseColor,tip);PlateCorals++;}
   else{Branch(batch,p,height,baseColor,tip);BranchCorals++;}
   Foot(batch,p);Record(p,true);if(!(RichEnabled&&kind==4))CoralCount++;added++;
  }
 }
 void Branch(Batch batch,Vector3 p,float height,Color low,Color tip){
  var data=new TreeData{randomSeed=1203+CoralCount*193,branchesMin=2,branchesMax=3,lengthAttenuation=.72f,radiusAttenuation=.68f,growthAngleMin=-25,growthAngleMax=25,growthAngleScale=2.6f,heightSegments=4,radialSegments=5,bendDegree=.13f};
  Mesh mesh=ProceduralTree.Build(data,3,.40f,.105f);mesh.RecalculateBounds();var bounds=mesh.bounds;
  var v=mesh.vertices;int offset=batch.v.Count;float scale=height/Mathf.Max(.01f,bounds.size.y);var rotation=Quaternion.Euler(0,Range(0,360),0);
  foreach(var q in v){
   float h=Mathf.InverseLerp(bounds.min.y,bounds.max.y,q.y);
   Vector3 local=new Vector3((q.x-bounds.center.x)*scale*(RichEnabled?1.7f:1.35f),(q.y-bounds.min.y)*scale,(q.z-bounds.center.z)*scale*(RichEnabled?.72f:.52f));
   Vector3 world=p+rotation*local;
   batch.Vertex(world,Color.Lerp(low,tip,Mathf.SmoothStep(0,1,h)),new Vector2(h,CoralCount));HighestTip=Mathf.Max(HighestTip,world.y);
  }
  foreach(int t in mesh.triangles)batch.tr.Add(offset+t);
  Destroy(mesh);
 }
 void Plate(Batch batch,Vector3 p,float height,Color low,Color tip){
  // Small layered cups, with a solid rim and irregular profile instead of flat discs.
  int stem=batch.v.Count;
  for(int ring=0;ring<2;ring++)for(int j=0;j<=7;j++){
   float a=j*Mathf.PI*2/7,rad=ring==0?.14f:.08f;
   batch.Vertex(p+new Vector3(Mathf.Cos(a)*rad,ring*height*.92f,Mathf.Sin(a)*rad),low,Vector2.zero);
  }
  for(int j=0;j<7;j++){batch.Tri(stem+j,stem+j+8,stem+j+1);batch.Tri(stem+j+1,stem+j+8,stem+j+9);}
  int tiers=3;float angle=Range(0,Mathf.PI*2);
  for(int tier=0;tier<tiers;tier++){
   const int sides=22,rings=4;int first=batch.v.Count;
   float radius=(RichEnabled?Range(.72f,1.10f):Range(.52f,.85f))*(1-tier*.12f);
   for(int ring=0;ring<=rings;ring++)for(int j=0;j<=sides;j++){
    float f=ring/(float)rings,a=j*Mathf.PI*2/sides;
    float scallop=1+.10f*Mathf.Sin(a*5+angle)+.05f*Mathf.Cos(a*8+angle);
    Vector3 v=p+new Vector3(Mathf.Cos(a)*f*radius*scallop,height*(.18f+tier*.27f)+f*f*.12f+Mathf.Sin(a*4+angle)*f*.035f,Mathf.Sin(a)*f*radius*.83f*scallop);
    batch.Vertex(v,Color.Lerp(low,tip,f*.75f+tier*.08f),new Vector2(f,tier));HighestTip=Mathf.Max(HighestTip,v.y);
   }
   for(int ring=0;ring<rings;ring++)for(int j=0;j<sides;j++){int a=first+ring*(sides+1)+j,b=a+sides+1;batch.Tri(a,a+1,b);batch.Tri(a+1,b+1,b);}
  }
 }
 void Foot(Batch batch,Vector3 p){
  int first=batch.v.Count;Color c=new Color(.25f,.31f,.25f);
  batch.Vertex(p+Vector3.up*.18f,c,Vector2.zero);
  for(int j=0;j<=9;j++){float a=j*Mathf.PI*2/9,r=.44f+.05f*Mathf.Sin(a*3+CoralCount);batch.Vertex(p+new Vector3(Mathf.Cos(a)*r,-.03f,Mathf.Sin(a)*r),c*.87f,Vector2.zero);}
  for(int j=0;j<9;j++)batch.Tri(first,first+j+2,first+j+1);
 }
 void PlantGrass(Batch batch,Vector2 centre,int count,float rx,float rz){
  int added=0;
  for(int attempt=0;attempt<(RichEnabled?1200:300)&&added<count;attempt++){
   float a=Range(0,Mathf.PI*2),r=Mathf.Sqrt(Range(0,1));Vector3 p;
   if(!Place(centre+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*rz)*r,.30f,false,out p))continue;
   bool crowded=false;foreach(var other in GrassRoots){var d=other-p;d.y=0;if(d.sqrMagnitude<.9f*.9f){crowded=true;break;}}if(crowded)continue;
   bool coralNear=false;foreach(var other in CoralRoots){var d=other-p;d.y=0;if(d.sqrMagnitude<1.25f*1.25f){coralNear=true;break;}}if(coralNear)continue;
   for(int blade=0;blade<(RichEnabled?25:21);blade++){
    float direction=Range(0,Mathf.PI*2),phase=Range(0,Mathf.PI*2);
    Vector3 side=new Vector3(Mathf.Cos(direction),0,Mathf.Sin(direction));
    Vector3 b=p+new Vector3(Range(-.48f,.48f),0,Range(-.48f,.48f));b.y=ocean.BottomHeight(b.x,b.z)-.07f;
    float h=Mathf.Min(RichEnabled?Range(1.05f,1.75f):Range(.68f,1.28f),OceanSurface.SeaLevel-.43f-b.y);if(h<.3f)continue;
    float width=RichEnabled?Range(.11f,.18f):Range(.07f,.12f),lean=RichEnabled?Range(.27f,.50f):Range(.18f,.37f);int first=batch.v.Count;
    Color rootTint=Color.Lerp(new Color(.055f,.20f,.13f),new Color(.12f,.25f,.12f),Range(0,1));
    for(int segment=0;segment<=6;segment++){
     float f=segment/6f;Vector3 mid=b+Vector3.up*h*f+side*(f*f*lean);
     float w=width*Mathf.Pow(1-f,.65f)*(.8f+.2f*Mathf.Sin(f*Mathf.PI));
     Color c=Color.Lerp(rootTint,new Color(.37f,.52f,.22f),f);
     batch.Vertex(mid-side*w,c,new Vector2(f,phase));batch.Vertex(mid+side*w,c,new Vector2(f,phase));
    }
    for(int segment=0;segment<6;segment++){int i=first+segment*2;batch.Tri(i,i+2,i+1);batch.Tri(i+1,i+2,i+3);}
    HighestTip=Mathf.Max(HighestTip,b.y+h);BladeCount++;
   }
   Record(p,false);TuftCount++;added++;
  }
 }
 void AddBatch(string label,Batch batch,string shader,float margin){
  if(batch.v.Count==0)return;
  var mesh=new Mesh{name=label,indexFormat=IndexFormat.UInt32};mesh.SetVertices(batch.v);mesh.SetColors(batch.c);mesh.SetUVs(0,batch.uv);mesh.SetTriangles(batch.tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
  var bounds=mesh.bounds;bounds.Expand(margin);mesh.bounds=bounds;meshes.Add(mesh);TriangleCount+=batch.tr.Count/3;
  var material=new Material(Shader.Find(shader));materials.Add(material);
  var go=new GameObject(label);go.layer=4;go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
  var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;RendererCount++;
 }
 void OnDestroy(){if(previousGrass!=null)previousGrass.sharedMaterial=oldGrass;if(newGrass!=null)Destroy(newGrass);foreach(var m in meshes)if(m!=null)Destroy(m);foreach(var m in materials)if(m!=null)Destroy(m);if(root!=null)Destroy(root.gameObject);}
}
}
