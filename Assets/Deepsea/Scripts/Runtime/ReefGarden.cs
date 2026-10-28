using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ProceduralModeling;
namespace Deepsea {
/// <summary>Authored sparse reef colonies and nursery grass. Geometry is generated once.</summary>
public sealed class ReefGarden : MonoBehaviour {
 public static bool Enabled=true;
 public bool Ready {get;private set;}
 public int BenthosCount {get;private set;}
 public int CoralCount {get;private set;}
 public int BranchCorals {get;private set;}
 public int PlateCorals {get;private set;}
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
 sealed class Batch {
  public readonly List<Vector3> v=new List<Vector3>();public readonly List<Color> c=new List<Color>();
  public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> tr=new List<int>();
  public void Vertex(Vector3 p,Color color,Vector2 tex){v.Add(p);c.Add(color);uv.Add(tex);}
  public void Tri(int a,int b,int c){tr.Add(a);tr.Add(b);tr.Add(c);}
 }
 public void Initialize(OceanSurface water){
  var watch=System.Diagnostics.Stopwatch.StartNew();ocean=water;game=water.game;
  NurseryCentre=GetComponent<ShoreHabitat>().NurseryCentre;
  if(!Enabled){Ready=true;return;}
  root=VisualFactory.Group("礁边珊瑚与草甸 · 仅视觉",game.World.Root,Vector3.zero);
  var coral=new Batch();var grass=new Batch();
  // Larger habitat patches separated by open sand channels.
  PlantCorals(coral,new Vector2(64,89),32,1.8f,7.0f);
  PlantCorals(coral,new Vector2(47,73),26,2.0f,6.5f);
  PlantCorals(coral,new Vector2(50.5f,106),30,0,6.0f);
  PlantCorals(coral,new Vector2(59,101),18,0,4.5f);
  PlantGrass(grass,new Vector2(NurseryCentre.x,NurseryCentre.z),52,6.5f,5.0f);
  PlantGrass(grass,new Vector2(53.5f,103),44,6.0f,4.5f);
  PlantGrass(grass,new Vector2(61.5f,94.5f),32,4.0f,5.0f);
  PlantGrass(grass,new Vector2(44,76),28,5.0f,4.0f);
  PlantBenthos(coral);
  AddBatch("礁边暖色珊瑚",coral,"Deepsea/ReefCoral",.04f);
  AddBatch("浅滩宽叶海草",grass,"Deepsea/Seagrass",.8f);
  gameObject.AddComponent<LivingReef>().Initialize(ocean);BakeMilliseconds=watch.Elapsed.TotalMilliseconds;Ready=true;Debug.Log("REEF_ABUNDANCE corals="+CoralCount+" tufts="+TuftCount+" benthos="+BenthosCount);
 }
 bool Place(Vector2 spot,float footprint,bool coral,out Vector3 p){
  p=new Vector3(spot.x,0,spot.y);float bed=ocean.BottomHeight(p.x,p.z),depth=OceanSurface.SeaLevel-bed;
  if(depth<.9f||depth>(coral?3.0f:3.9f))return false;
  if(coral&&(new Vector2(p.x-NurseryCentre.x,p.z-NurseryCentre.z)).sqrMagnitude<8.5f*8.5f)return false;
  float lo=bed,hi=bed;
  for(int i=0;i<8;i++){float a=i*Mathf.PI*.25f;float y=ocean.BottomHeight(p.x+Mathf.Cos(a)*footprint,p.z+Mathf.Sin(a)*footprint);lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}
  if(hi-lo>(coral?.65f:.8f))return false;
  foreach(var landing in game.Expedition.Landings){
   Vector3 a=p-landing.berth,b=p-landing.feet;a.y=b.y=0;
   if(a.sqrMagnitude<6.0f*6.0f||b.sqrMagnitude<5.5f*5.5f)return false;
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
  for(int attempt=0;attempt<1400&&added<count;attempt++){
   float a=Range(0,Mathf.PI*2),r=Range(minRadius,maxRadius);Vector3 p;
   if(!Place(centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,.48f,true,out p))continue;
   bool crowded=false;foreach(var other in CoralRoots){var d=p-other;d.y=0;if(d.sqrMagnitude<1.05f*1.05f){crowded=true;break;}}
   if(crowded)continue;
   float height=Mathf.Min(Range(1.25f,2.05f),OceanSurface.SeaLevel-.50f-p.y);
   if(height<.6f)continue;
   Color baseColor=Color.Lerp(new Color(.38f,.12f,.07f),new Color(.45f,.16f,.29f),Range(0,.65f));
   Color tip=Color.Lerp(new Color(.98f,.30f,.08f),new Color(.86f,.20f,.44f),Range(0,.6f));
   if(CoralCount%3==2){Plate(batch,p,height*.55f,baseColor,tip);PlateCorals++;}
   else{Branch(batch,p,height,baseColor,tip);BranchCorals++;}
   Foot(batch,p);Record(p,true);CoralCount++;added++;
  }
 }
 void Branch(Batch batch,Vector3 p,float height,Color low,Color tip){
  var data=new TreeData{randomSeed=1203+CoralCount*193,branchesMin=2,branchesMax=3,lengthAttenuation=.72f,radiusAttenuation=.68f,growthAngleMin=-25,growthAngleMax=25,growthAngleScale=2.6f,heightSegments=4,radialSegments=5,bendDegree=.13f};
  Mesh mesh=ProceduralTree.Build(data,3,.40f,.105f);mesh.RecalculateBounds();var bounds=mesh.bounds;
  var v=mesh.vertices;int offset=batch.v.Count;float scale=height/Mathf.Max(.01f,bounds.size.y);var rotation=Quaternion.Euler(0,Range(0,360),0);
  foreach(var q in v){
   float h=Mathf.InverseLerp(bounds.min.y,bounds.max.y,q.y);
   Vector3 local=new Vector3((q.x-bounds.center.x)*scale*1.35f,(q.y-bounds.min.y)*scale,(q.z-bounds.center.z)*scale*.52f);
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
   float radius=Range(.52f,.85f)*(1-tier*.12f);
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
  for(int attempt=0;attempt<1000&&added<count;attempt++){
   float a=Range(0,Mathf.PI*2),r=Mathf.Sqrt(Range(0,1));Vector3 p;
   if(!Place(centre+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*rz)*r,.30f,false,out p))continue;
   bool crowded=false;foreach(var other in GrassRoots){var d=other-p;d.y=0;if(d.sqrMagnitude<.78f*.78f){crowded=true;break;}}if(crowded)continue;
   bool coralNear=false;foreach(var other in CoralRoots){var d=other-p;d.y=0;if(d.sqrMagnitude<1.05f*1.05f){coralNear=true;break;}}if(coralNear)continue;
   for(int blade=0;blade<25;blade++){
    float direction=Range(0,Mathf.PI*2),phase=Range(0,Mathf.PI*2);
    Vector3 side=new Vector3(Mathf.Cos(direction),0,Mathf.Sin(direction));
    Vector3 b=p+new Vector3(Range(-.48f,.48f),0,Range(-.48f,.48f));b.y=ocean.BottomHeight(b.x,b.z)-.07f;
    float h=Mathf.Min(Range(.9f,1.65f),OceanSurface.SeaLevel-.43f-b.y);if(h<.3f)continue;
    float width=Range(.10f,.17f),lean=Range(.18f,.37f);int first=batch.v.Count;
    Color rootTint=Color.Lerp(new Color(.055f,.20f,.13f),new Color(.12f,.25f,.12f),Range(0,1));
    for(int segment=0;segment<=6;segment++){
     float f=segment/6f;Vector3 mid=b+Vector3.up*h*f+side*(f*f*lean);
     float w=width*Mathf.Pow(1-f,.65f)*(.8f+.2f*Mathf.Sin(f*Mathf.PI));
     Color c=Color.Lerp(rootTint,new Color(.27f,.48f,.12f),f);
     batch.Vertex(mid-side*w,c,new Vector2(f,phase));batch.Vertex(mid+side*w,c,new Vector2(f,phase));
    }
    for(int segment=0;segment<6;segment++){int i=first+segment*2;batch.Tri(i,i+2,i+1);batch.Tri(i+1,i+2,i+3);}
    HighestTip=Mathf.Max(HighestTip,b.y+h);BladeCount++;
   }
   Record(p,false);TuftCount++;added++;
  }
 }
 // Original seabed invertebrates, batched with coral: no added colliders.
 void PlantBenthos(Batch batch){
  Vector2[] centres={new Vector2(64,89),new Vector2(47,73),new Vector2(53,104),new Vector2(59,98)};
  foreach(var centre in centres){int added=0;
   for(int attempt=0;attempt<500&&added<18;attempt++){
    float a=Range(0,Mathf.PI*2),r=Range(1,6);Vector3 p;
    if(!Place(centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,.25f,false,out p))continue;
    bool crowded=false;foreach(var q in CoralRoots)if((q-p).sqrMagnitude<.85f*.85f)crowded=true;
    if(crowded)continue;p.y=ocean.BottomHeight(p.x,p.z)+.025f;
    if(added%3==0){ // Five thick arms, recognizable from the gameplay camera.
     Color c=new Color(.85f,.35f,.12f);float angle=Range(0,6.28f);
     for(int arm=0;arm<5;arm++){
      float t=angle+arm*Mathf.PI*.4f;Vector3 d=new Vector3(Mathf.Cos(t),0,Mathf.Sin(t)),side=new Vector3(-d.z,0,d.x);
      int k=batch.v.Count;batch.Vertex(p+Vector3.up*.14f,c,Vector2.zero);
      batch.Vertex(p+side*.16f,c*.8f,Vector2.zero);batch.Vertex(p+d*Range(.52f,.78f)+Vector3.up*.04f,c,Vector2.zero);batch.Vertex(p-side*.16f,c*.8f,Vector2.zero);
      batch.Tri(k,k+1,k+2);batch.Tri(k,k+2,k+3);
     }
    }else if(added%3==1){ // Hollow ochre sponge tubes, dark inside.
     for(int tube=0;tube<3;tube++){
      Vector3 q=p+new Vector3(Range(-.3f,.3f),0,Range(-.3f,.3f));float h=Mathf.Min(Range(.45f,.95f),OceanSurface.SeaLevel-.45f-q.y);if(h<.2f)continue;
      int k=batch.v.Count;float radius=Range(.16f,.25f);Color c=new Color(.68f,.46f,.12f);
      for(int ring=0;ring<4;ring++)for(int j=0;j<=10;j++){
       float t=j*Mathf.PI*.2f,rad=ring==0?radius*.7f:ring==1?radius:ring==2?radius*.65f:radius*.5f;
       float y=ring==0?0:ring<3?h:h*.48f;
       batch.Vertex(q+new Vector3(Mathf.Cos(t)*rad,y,Mathf.Sin(t)*rad),ring==3?c*.30f:c,Vector2.zero);
      }
      for(int ring=0;ring<3;ring++)for(int j=0;j<10;j++){int v=k+ring*11+j;batch.Tri(v,v+1,v+11);batch.Tri(v+1,v+12,v+11);}
      HighestTip=Mathf.Max(HighestTip,q.y+h);
     }
    }else{ // Radial dark urchin spines.
     Color c=new Color(.20f,.11f,.28f);
     for(int j=0;j<24;j++){
      float t=j*Mathf.PI*2/24;Vector3 d=new Vector3(Mathf.Cos(t),0,Mathf.Sin(t));Vector3 side=new Vector3(-d.z,0,d.x)*.035f;
      int k=batch.v.Count;Vector3 q=p+Vector3.up*.13f;
      batch.Vertex(q+side,c,Vector2.zero);batch.Vertex(q+d*Range(.25f,.43f)+Vector3.up*Range(.08f,.25f),c*1.4f,Vector2.zero);batch.Vertex(q-side,c,Vector2.zero);batch.Tri(k,k+1,k+2);
     }
    }
    BenthosCount++;added++;
   }
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
 void OnDestroy(){foreach(var m in meshes)if(m!=null)Destroy(m);foreach(var m in materials)if(m!=null)Destroy(m);if(root!=null)Destroy(root.gameObject);}
}
}
