using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using B=Deepsea.ReefLifeGarden.Batch;
namespace Deepsea {
// An isolated visual habitat layer on the accepted coast. No added colliders.
public sealed class CoastEcology:MonoBehaviour {
 public static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-coast-ecology")>=0;}}
 public bool Visible=true,Ready;
 public bool MarineMaterialsSupported;public int MarineTriangles,MarineUvVertices;
 public int FernTrianglesPerPlant;public int Saplings;public int Trees,Ferns,GrassTufts,KelpClumps,Corals,FishCount,DrawGroups,Triangles;
 public int RouteSamples,RouteObstructions;
 public int AddedColliders {get{return root!=null?root.GetComponentsInChildren<Collider>(true).Length:0;}}
 public float HighestUnderwaterTip=-100;
 CoastBlockoutRegion region;CoastMaterialsAppearance look;Transform root;
 CoastBlockoutSchool oldSchool;Renderer oldFish;
 readonly List<Mesh> ownedMeshes=new List<Mesh>();readonly List<Material> ownedMats=new List<Material>();readonly List<Renderer> solids=new List<Renderer>();
 readonly List<Group> groups=new List<Group>();readonly List<Shoal> shoals=new List<Shoal>();
 readonly System.Random rng=new System.Random(127321);
 sealed class Group{public Mesh mesh;public Material mat;public int sub;public Matrix4x4[] matrices;public bool fish,noShadow;}
 sealed class Part{public Mesh mesh;public Material mat;public Matrix4x4 local;public int sub;}
 sealed class Species{public List<Part> parts=new List<Part>();public Bounds bounds;}
 sealed class Shoal{public Group group;public Vector3[] path;public int count;public float size,period;public bool cave;}
 float R(float a,float b){return Mathf.Lerp(a,b,(float)rng.NextDouble());}
 Material Mat(string name,Color tint,float sway=0){var m=new Material(Shader.Find("Deepsea/CoastEcology")){name=name,enableInstancing=true};m.SetColor("_Tint",tint);m.SetFloat("_VertexColor",1);m.SetFloat("_Sway",sway);ownedMats.Add(m);return m;}
 public void Initialize(CoastBlockoutRegion area,CoastMaterialsAppearance appearance){
  region=area;look=appearance;root=new GameObject("东岸生态 · 独立视觉层").transform;root.SetParent(transform,false);
  oldSchool=area.GetComponentInChildren<CoastBlockoutSchool>();oldFish=oldSchool.GetComponent<Renderer>();
  var tree=LoadSpecies("island_tree_01",1);var fern=LoadSpecies("fern_02");
  Vector2[] trunks={new Vector2(31,101),new Vector2(26,110),new Vector2(22,92),new Vector2(32,85),new Vector2(33,78)};
  var treeMatrices=new List<Matrix4x4>();foreach(var p in trunks){Vector3 q; if(!Ground(p,out q)||CoastBlockoutLayout.PathDistance(q)<4)continue;treeMatrices.Add(PlantMatrix(q,R(4.8f,7.2f),tree.bounds));Trees++;}AddSpecies(tree,treeMatrices);
  // Young trees supply a middle layer using the same scanned species and texture atlas.
  var sapling=LoadSpecies("island_tree_01",2);var saplingMatrices=new List<Matrix4x4>();for(int i=0;i<12;i++){
   float z=R(75,111),x=CoastBlockoutLayout.Shore(z)-R(2.0f,8.5f);Vector3 q;
   if(!Ground(new Vector2(x,z),out q)||CoastBlockoutLayout.PathDistance(q)<2.8f||Vector2.Distance(new Vector2(q.x,q.z),new Vector2(30,93))<3.2f)continue;
   saplingMatrices.Add(PlantMatrix(q,R(1.4f,2.4f),tree.bounds));Saplings++;
  }AddSpecies(sapling,saplingMatrices,false);
  var fernMatrices=new List<Matrix4x4>();for(int i=0;i<280;i++){
   float z=R(73,113),x=CoastBlockoutLayout.Shore(z)-R(1.8f,10);Vector3 q;
   if(!Ground(new Vector2(x,z),out q)||CoastBlockoutLayout.PathDistance(q)<2.2f||Vector2.Distance(new Vector2(q.x,q.z),new Vector2(30,93))<2)continue;
   if(Mathf.Sin(x*.8f+z*.3f)<-.35f)continue;
   fernMatrices.Add(PlantMatrix(q,R(.34f,.64f),fern.bounds,3.8f));Ferns++;
  }AddSpecies(fern,fernMatrices,false);
  var grass=new B();var kelp=new B();var coral=new B();
  // Patches follow shelves, leaving the pier approach and central sand channel open.
  Vector2[] beds={new Vector2(45,82),new Vector2(48,105),new Vector2(59,103),new Vector2(62,87),new Vector2(66,112),new Vector2(52,76)};
  for(int patch=0;patch<beds.Length;patch++)for(int i=0;i<78;i++){
   float a=R(0,Mathf.PI*2),r=Mathf.Sqrt(R(0,1))*R(1,4);Vector2 spot=beds[patch]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;Vector3 p;
   if(!Seabed(spot,out p))continue;
   float maxHeight=-.65f-p.y;if(maxHeight<.40f)continue;
   if(i%5==0){float h=Mathf.Min(R(.6f,1.25f),maxHeight);int kind=(i+patch)%4;
    if(kind==0)ReefDetails.Fan(coral,p,h,new Color(.28f,.20f,.16f),new Color(.47f,.35f,.25f),i+patch*91);
    else if(kind==1)ReefDetails.Dome(coral,p,h*.56f,new Color(.30f,.28f,.14f),new Color(.53f,.49f,.25f));
    else if(kind==2)Sponge(coral,p,h,new Color(.37f,.31f,.26f),i+patch*73);
    else Branch(coral,p,h);
    Corals++;
   }else if(i%3==0){Kelp(kelp,p,Mathf.Min(R(1.5f,3.2f),maxHeight));KelpClumps++;}
   else{Blades(grass,p,Mathf.Min(R(.45f,1.10f),maxHeight),9,.065f,new Color(.13f,.23f,.12f),new Color(.33f,.42f,.19f));GrassTufts++;}
  }
  var grassMaterial=Resources.Load<Material>("CoastMarine/Seagrass");var kelpMaterial=Resources.Load<Material>("CoastMarine/Kelp");var coralMaterial=Resources.Load<Material>("CoastMarine/Coral");
  MarineMaterialsSupported=grassMaterial!=null&&kelpMaterial!=null&&coralMaterial!=null&&grassMaterial.shader.isSupported&&kelpMaterial.shader.isSupported&&coralMaterial.shader.isSupported;
  if(!MarineMaterialsSupported)throw new InvalidOperationException("Marine material resources missing or unsupported");
  Batch("海草草甸",grass,grassMaterial);Batch("礁台海带",kelp,kelpMaterial);Batch("珊瑚群落",coral,coralMaterial);
  MarineTriangles=(grass.tr.Count+kelp.tr.Count+coral.tr.Count)/3;foreach(var uv in kelp.uv)if(uv.sqrMagnitude>.001f)MarineUvVertices++;
  Debug.Log("COAST_MARINE_READY triangles="+MarineTriangles+" uvVertices="+MarineUvVertices+" kelp="+KelpClumps+" coral="+Corals);
  AddShoal(0,22,new Vector2(0,0),.78f,24,true);AddShoal(1,28,new Vector2(57,102),.60f,23,false);AddShoal(2,38,new Vector2(56,86),.48f,28,false);
  DrawGroups=groups.Count;Ready=true;Debug.Log("COAST_ECOLOGY_READY trees="+Trees+" ferns="+Ferns+" grass="+GrassTufts+" kelp="+KelpClumps+" coral="+Corals+" fish="+FishCount+" routeObstructions="+RouteObstructions+" drawGroups="+DrawGroups);
 }
 bool Ground(Vector2 p,out Vector3 q){
  q=Vector3.zero;float best=-100;foreach(var hit in Physics.RaycastAll(new Vector3(p.x,25,p.y),Vector3.down,50))if(hit.transform.IsChildOf(region.transform)&&hit.point.y>best){best=hit.point.y;q=hit.point;}
  return best>.6f&&best<8;
 }
 bool Seabed(Vector2 spot,out Vector3 p){
  p=new Vector3(spot.x,0,spot.y);Vector3 q;if(!GroundUnder(spot,out q))return false;p=q-Vector3.up*.04f;
  if(p.y>-.95f||p.y< -10)return false;
  // Protect the berth, its approach, and the established through-cave fish envelope.
  if(Mathf.Abs(p.z-94)<3.1f&&p.x>35&&p.x<61)return false;
  if(Vector2.Distance(spot,new Vector2(CoastBlockoutLayout.CaveFocus.x,CoastBlockoutLayout.CaveFocus.z))<10)return false;
  return true;
 }
 bool GroundUnder(Vector2 p,out Vector3 q){q=Vector3.zero;float best=-100;foreach(var hit in Physics.RaycastAll(new Vector3(p.x,15,p.y),Vector3.down,45))if(hit.transform.IsChildOf(region.transform)&&hit.point.y>best){best=hit.point.y;q=hit.point;}return best> -30;}
 Species LoadSpecies(string name,int firstLod=0){
  var source=Resources.Load<GameObject>("CoastSlice/"+name);if(source==null)throw new InvalidOperationException("Missing existing flora "+name);
  var keep=new HashSet<Renderer>();var lods=source.GetComponentsInChildren<LODGroup>(true);
  foreach(var g in lods){var levels=g.GetLODs();for(int n=firstLod;n<levels.Length;n++){bool valid=false;foreach(var r in levels[n].renderers)if(r!=null&&r.GetComponent<MeshFilter>()!=null&&r.GetComponent<MeshFilter>().sharedMesh.vertexCount>30)valid=true;if(!valid)continue;foreach(var r in levels[n].renderers)if(r!=null)keep.Add(r);break;}}
  var result=new Species();bool first=true;var cache=new Dictionary<Material,Material>();
  foreach(var r in source.GetComponentsInChildren<MeshRenderer>(true)){
   if(lods.Length>0&&!keep.Contains(r))continue;if(lods.Length==0&&source.GetComponent<MeshFilter>()!=null&&r.transform!=source.transform)continue;var f=r.GetComponent<MeshFilter>();if(f==null||f.sharedMesh==null)continue;
   Matrix4x4 local=Matrix4x4.Translate(-source.transform.position)*f.transform.localToWorldMatrix;Bounds b=f.sharedMesh.bounds;
   for(int k=0;k<8;k++){Vector3 v=local.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1)));if(first){result.bounds=new Bounds(v,Vector3.zero);first=false;}else result.bounds.Encapsulate(v);}
   for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++){
    Material old=r.sharedMaterials[Mathf.Min(sub,r.sharedMaterials.Length-1)],mat;if(!cache.TryGetValue(old,out mat)){
     mat=Mat(name+"统一叶面",new Color(.67f,.83f,.65f),name.Contains("fern")?.045f:.012f);mat.SetFloat("_VertexColor",0);mat.SetFloat("_Cutoff",.27f);
     Texture tex=old.HasProperty("_BaseMap")?old.GetTexture("_BaseMap"):old.mainTexture;if(tex!=null)mat.SetTexture("_BaseMap",tex);cache.Add(old,mat);
    }
    var renderMesh=f.sharedMesh;if(name=="fern_02"){var reduced=Resources.Load<Mesh>("CoastHabitat/Fern");if(reduced!=null){renderMesh=reduced;FernTrianglesPerPlant=reduced.triangles.Length/3;}}
    result.parts.Add(new Part{mesh=renderMesh,mat=mat,local=local,sub=sub});
   }
  }
  if(result.parts.Count==0)throw new InvalidOperationException("No usable flora LOD "+name);
  Debug.Log("COAST_FLORA_LOD "+name+" parts="+result.parts.Count+" bounds="+result.bounds);return result;
 }
 Matrix4x4 PlantMatrix(Vector3 p,float height,Bounds b,float widthRatio=1.8f){float scale=Mathf.Min(height/Mathf.Max(.01f,b.size.y),height*widthRatio/Mathf.Max(.01f,Mathf.Max(b.size.x,b.size.z)));return Matrix4x4.TRS(p,Quaternion.Euler(0,R(0,360),0),Vector3.one*scale)*Matrix4x4.Translate(-new Vector3(b.center.x,b.min.y,b.center.z));}
 void AddSpecies(Species s,List<Matrix4x4> matrices,bool castShadows=true){foreach(var p in s.parts){var g=new Group{mesh=p.mesh,mat=p.mat,sub=p.sub,noShadow=!castShadows,matrices=new Matrix4x4[matrices.Count]};for(int i=0;i<matrices.Count;i++)g.matrices[i]=matrices[i]*p.local;groups.Add(g);}}
 void Batch(string name,B b,Material mat){if(b.v.Count==0)return;var mesh=MeshOf(b,name);var go=new GameObject(name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;solids.Add(r);foreach(var v in b.v)HighestUnderwaterTip=Mathf.Max(HighestUnderwaterTip,v.y);}
 Mesh MeshOf(B b,string name){var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(b.v);m.SetColors(b.c);m.SetUVs(0,b.uv);m.SetTriangles(b.tr,0);m.RecalculateNormals();m.RecalculateBounds();ownedMeshes.Add(m);Triangles+=b.tr.Count/3;return m;}
 // Three vertices across each blade give it a curved cross section instead of a flat card.
 void Frond(B b,Vector3 start,Vector3 end,float width,float seed,Vector3 basePoint,float plantHeight){
  int first=b.v.Count;Vector3 direction=(end-start).normalized;
  Vector3 side=Vector3.Cross(direction,Vector3.up);if(side.sqrMagnitude<.05f)side=new Vector3(Mathf.Cos(seed),0,Mathf.Sin(seed));side.Normalize();
  Vector3 convex=Vector3.Cross(side,direction).normalized;
  const int sections=12;
  for(int row=0;row<=sections;row++){
   float t=row/(float)sections;Vector3 centre=Vector3.Lerp(start,end,t)+side*Mathf.Sin(t*4.2f+seed)*width*.35f+convex*Mathf.Sin(t*Mathf.PI)*plantHeight*.055f;
   float w=width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.63f)*(.88f+.12f*Mathf.Sin(t*18+seed));
   for(int col=0;col<3;col++){
    float across=col-1;Vector3 q=centre+side*w*across+convex*w*.23f*(1-across*across);
    float weight=Mathf.Clamp01((q.y-basePoint.y)/Mathf.Max(.1f,plantHeight));
    b.Vertex(q,new Color(.35f+.58f*t,.8f,.6f,weight),new Vector2(col*.5f,t));
   }
  }
  for(int row=0;row<sections;row++)for(int col=0;col<2;col++){int a=first+row*3+col;b.Tri(a,a+3,a+1);b.Tri(a+1,a+3,a+4);}
 }
 void Blades(B b,Vector3 p,float h,int count,float width,Color low,Color high){
  for(int blade=0;blade<count;blade++){
   float angle=R(0,Mathf.PI*2),height=h*R(.58f,1);Vector3 lean=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
   Vector3 start=p+lean*R(.015f,.10f);Vector3 end=start+Vector3.up*height+lean*height*R(.20f,.50f);
   Frond(b,start,end,width*R(.75f,1.2f),angle,p,h);
  }
 }
 void Kelp(B b,Vector3 p,float h){
  float angle=R(0,Mathf.PI*2);Vector3 side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
  Vector3 tip=p+Vector3.up*h+side*h*.20f;ReefDetails.Tube(b,p,tip,.026f,.008f,new Color(.38f,.52f,.23f),new Color(.56f,.69f,.34f),6,angle);
  // Asymmetric blades grow from staggered points on a shared stipe, never as identical paper fans.
  for(int i=0;i<7;i++){
   float t=.18f+i*.105f;Vector3 start=Vector3.Lerp(p,tip,t);float yaw=angle+i*2.399f;
   Vector3 outward=new Vector3(Mathf.Cos(yaw),0,Mathf.Sin(yaw));float length=h*R(.35f,.60f)*(1-t*.38f);
   Vector3 end=start+outward*length*.68f+Vector3.up*length*.60f;
   Frond(b,start,end,h*R(.045f,.080f),yaw,p,h);
  }
  Frond(b,Vector3.Lerp(p,tip,.72f),tip+Vector3.up*h*.05f,h*.058f,angle+.7f,p,h);
 }
 void Sponge(B b,Vector3 p,float height,Color tint,int seed){
  int count=3+seed%3;
  for(int tube=0;tube<count;tube++){
   float phase=tube*2.399f+seed*.31f;Vector3 centre=p+new Vector3(Mathf.Sin(phase)*.25f,0,Mathf.Cos(phase)*.25f);
   float h=height*(.68f+.26f*(.5f+.5f*Mathf.Sin(tube*1.7f+seed)));int first=b.v.Count;const int sides=18,rings=8;
   for(int ring=0;ring<=rings;ring++)for(int j=0;j<=sides;j++){
    float a=j*Mathf.PI*2/sides,u=ring/(float)rings;bool inner=ring>=6;
    float y=ring<6?h*(ring/5f):h-(ring-5)*h*.10f;
    float radius=inner?.092f:(.077f+.068f*Mathf.Sin(u*Mathf.PI*.65f));radius*=1+.065f*Mathf.Sin(a*3+phase)+.035f*Mathf.Sin(a*7+y*8);
    Vector3 q=centre+new Vector3(Mathf.Sin(a)*radius+Mathf.Cos(phase)*y*.16f,y+Mathf.Sin(a*3+phase)*.015f,Mathf.Cos(a)*radius);
    b.Vertex(q,inner?tint*.30f:Color.Lerp(tint*.68f,tint,u),new Vector2(j/(float)sides,u));
   }
   for(int ring=0;ring<rings;ring++)for(int j=0;j<sides;j++){int a=first+ring*(sides+1)+j,c=a+sides+1;b.Tri(a,a+1,c);b.Tri(a+1,c+1,c);}
  }
 }
 void Branch(B b,Vector3 p,float h){Color low=new Color(.28f,.19f,.14f),high=new Color(.63f,.45f,.25f);for(int i=0;i<5;i++){float a=i*2.399f;Vector3 end=p+new Vector3(Mathf.Cos(a)*h*.35f,h*R(.6f,1),Mathf.Sin(a)*h*.35f);ReefDetails.Tube(b,p,end,.07f,.025f,low,high,6,a);for(int j=0;j<2;j++){Vector3 from=Vector3.Lerp(p,end,.45f+j*.2f),tip=from+new Vector3(Mathf.Cos(a+j)*h*.22f,h*.25f,Mathf.Sin(a+j)*h*.22f);ReefDetails.Tube(b,from,tip,.035f,.008f,low,high,5,a+j);}}}
 Mesh FishMesh(int kind){var b=new B();Color back=kind==1?new Color(.55f,.32f,.10f):kind==2?new Color(.19f,.28f,.32f):new Color(.30f,.42f,.44f),belly=kind==1?new Color(.72f,.58f,.29f):new Color(.64f,.70f,.68f);
  const int rings=9,sides=10;float broad=kind==1?.12f:.10f,tall=kind==1?.27f:.13f;
  for(int ring=0;ring<rings;ring++){float t=ring/(float)(rings-1),z=Mathf.Lerp(-.32f,.45f,t),shape=Mathf.Sin(t*Mathf.PI)*.94f+.04f;for(int side=0;side<sides;side++){float a=side*Mathf.PI*2/sides;var p=new Vector3(Mathf.Cos(a)*broad*shape,Mathf.Sin(a)*tall*shape,z);b.Vertex(p,Color.Lerp(belly,back,Mathf.InverseLerp(-tall,tall,p.y)),new Vector2(side/(float)sides,t));}}
  for(int ring=0;ring<rings-1;ring++)for(int side=0;side<sides;side++){int k=ring*sides+side,next=ring*sides+(side+1)%sides;b.Tri(k,next,k+sides);b.Tri(next,next+sides,k+sides);}
  int tail=b.v.Count;b.Vertex(new Vector3(0,0,-.28f),back,Vector2.zero);b.Vertex(new Vector3(0,.19f,-.55f),back,Vector2.zero);b.Vertex(new Vector3(0,0,-.47f),back,Vector2.zero);b.Vertex(new Vector3(0,-.19f,-.55f),back,Vector2.zero);b.Tri(tail,tail+1,tail+2);b.Tri(tail,tail+2,tail+3);
  int fin=b.v.Count;b.Vertex(new Vector3(0,.08f,-.12f),back,Vector2.zero);b.Vertex(new Vector3(0,tall+.13f,-.08f),back,Vector2.zero);b.Vertex(new Vector3(0,.08f,.23f),back,Vector2.zero);b.Tri(fin,fin+1,fin+2);return MeshOf(b,"鱼体种类"+kind);
 }
 void AddShoal(int kind,int count,Vector2 centre,float size,float period,bool cave){
  var path=new Vector3[160];if(!cave){
   bool valid=false;Vector2[] alternatives={centre,new Vector2(54,104),new Vector2(55,84),new Vector2(56,109),new Vector2(50,80),new Vector2(53,101)};
   foreach(var candidate in alternatives){
    bool clear=true;for(int i=0;i<path.Length;i++){float a=i*Mathf.PI*2/path.Length;Vector2 q=candidate+new Vector2(Mathf.Cos(a)*1.65f,Mathf.Sin(a)*2.25f);float bed=CoastBlockoutLayout.Height(q.x,q.y);float y=Mathf.Max(bed+1.2f,-1.65f);path[i]=new Vector3(q.x,y,q.y);if(y>-.85f)clear=false;foreach(var hit in Physics.OverlapSphere(path[i],.75f))if(hit.transform.IsChildOf(region.transform))clear=false;}
    if(clear){valid=true;break;}
   }
   if(!valid){Debug.LogWarning("COAST_ECOLOGY_SKIP_UNSAFE_SHOAL "+kind);return;}
  }
  var mat=Mat("鱼群"+kind,Color.white);mat.SetFloat("_Fish",1);var group=new Group{mesh=FishMesh(kind),mat=mat,sub=0,matrices=new Matrix4x4[count],fish=true};groups.Add(group);shoals.Add(new Shoal{group=group,path=path,count=count,size=size,period=period,cave=cave});FishCount+=count;
  for(int step=0;step<160;step++)for(int fish=0;fish<count;fish++){Vector3 forward;var p=FishPoint(shoals[shoals.Count-1],step/160f,fish,out forward);RouteSamples++;foreach(var hit in Physics.OverlapSphere(p,cave?.56f:.40f))if(hit.transform.IsChildOf(region.transform)){RouteObstructions++;break;}}
 }
 Vector3 FishPoint(Shoal s,float phase,int i,out Vector3 forward){if(s.cave)return CoastBlockoutSchool.FishPosition(phase,i,out forward);float t=Mathf.Repeat(phase-i*.007f,1)*s.path.Length;int index=Mathf.FloorToInt(t),a=index%s.path.Length,b=(a+1)%s.path.Length;var p=Vector3.Lerp(s.path[a],s.path[b],t-index);forward=(s.path[b]-s.path[a]).normalized;var side=Vector3.Cross(Vector3.up,forward).normalized;return p+side*(Mathf.Sin(i*2.3f)*.38f)+Vector3.up*Mathf.Cos(i*1.7f)*.14f;}
 void LateUpdate(){if(!Ready)return;if(Input.GetKeyDown(KeyCode.F2))Visible=!Visible;bool show=Visible&&!look.Grey;if(oldSchool!=null){oldSchool.enabled=!show;if(oldFish!=null)oldFish.enabled=!show;}foreach(var r in solids)r.enabled=show;if(!show)return;
  foreach(var school in shoals)for(int i=0;i<school.count;i++){Vector3 forward,p=FishPoint(school,Mathf.Repeat(Time.time/school.period,1),i,out forward);school.group.matrices[i]=Matrix4x4.TRS(p,Quaternion.LookRotation(forward),Vector3.one*school.size*(1+(i%4)*.06f));}
  foreach(var g in groups)if(g.matrices.Length>0)Graphics.DrawMeshInstanced(g.mesh,g.sub,g.mat,g.matrices,g.matrices.Length,null,g.fish||g.noShadow?ShadowCastingMode.Off:ShadowCastingMode.On,true,0,null,LightProbeUsage.Off);
 }
 void OnDestroy(){foreach(var m in ownedMeshes)if(m!=null)Destroy(m);foreach(var m in ownedMats)if(m!=null)Destroy(m);if(root!=null)Destroy(root.gameObject);}
}
}
