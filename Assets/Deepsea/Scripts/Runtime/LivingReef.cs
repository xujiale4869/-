using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
// Original meshes and local-school steering; independent from rescue/gameplay fish.
public sealed class LivingReef:MonoBehaviour {
 sealed class Swimmer {public Transform root;public Vector3 p,v,home;public int kind,index;}
 readonly List<Swimmer> fish=new List<Swimmer>();readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> mats=new List<Material>();
 OceanSurface ocean;GameDirector game;Transform container;float clock;
 public int FishCount{get{return fish.Count;}} public int SpeciesCount{get{return 4;}}
 public int Avoidances{get;private set;} public float MinBed{get;private set;}=100;public float MinSurface{get;private set;}=100;
 public int InvalidPositions{get;private set;}
 public void Initialize(OceanSurface water){
  ocean=water;game=water.game;container=new GameObject("活礁 · 三类鱼群与鳐鱼").transform;container.SetParent(game.World.Root,false);
  Mesh[] shapes={Shape(0),Shape(1),Shape(2),Shape(3)};
  Color[] colors={new Color(.57f,.76f,.80f,1),new Color(.88f,.52f,.17f,1),new Color(.13f,.32f,.33f,1),new Color(.20f,.34f,.35f,1)};
  int[] counts={42,15,12,1};Vector3[] homes={new Vector3(69,0,99),new Vector3(57,0,103),new Vector3(57,0,86),new Vector3(77,0,84)};
  for(int k=0;k<4;k++){
   var mat=new Material(Shader.Find("Deepsea/ReefWildlife"));mat.SetColor("_Tint",colors[k]);mat.SetFloat("_Submerged",1);mat.SetFloat("_Kind",k);mats.Add(mat);
   for(int i=0;i<counts[k];i++){
    var go=new GameObject(new[]{"银色巡游鱼","暖色礁鱼","长身草甸鱼","贴底鳐鱼"}[k]);go.layer=4;go.transform.SetParent(container,false);
    go.AddComponent<MeshFilter>().sharedMesh=shapes[k];var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;
    var block=new MaterialPropertyBlock();block.SetFloat("_Phase",i*2.39f);r.SetPropertyBlock(block);
    float angle=i*2.39996f;Vector3 p=homes[k]+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*Mathf.Sqrt(i+1)*.62f;
    if(!Clear(p))p=homes[k];float size=k==0?1.1f:k==1?1.10f:k==2?1.45f:1.8f;go.transform.localScale=Vector3.one*size*(.90f+(i%5)*.05f);
    fish.Add(new Swimmer{root=go.transform,p=p,v=Vector3.forward,home=homes[k],kind=k,index=i});
   }
  }
  Debug.Log("LIVING_REEF_READY fish="+FishCount+" species="+SpeciesCount);
 }
 bool Clear(Vector3 p){if(IslandTerrain.Contains(p)||ocean.BottomHeight(p.x,p.z)>OceanSurface.SeaLevel-1.0f)return false;p.y=.4f;return !Physics.CheckSphere(p,.70f,Physics.AllLayers,QueryTriggerInteraction.Ignore);}
 void Update(){if(ocean==null||game.Paused)return;float dt=Mathf.Min(Time.deltaTime,.05f);clock+=dt;
  foreach(var f in fish){
   float t=clock,speed=f.kind==0?1.65f:f.kind==1?.68f:f.kind==2?1.0f:.65f;
   float phase=t*(f.kind==0?.13f:.085f)+f.kind*1.7f;
   Vector3 target=f.home+new Vector3(Mathf.Cos(phase)*(f.kind==0?6:3),0,Mathf.Sin(phase)*(f.kind==0?7:4));
   target+=new Vector3(Mathf.Cos(f.index*2.399f),0,Mathf.Sin(f.index*2.399f))*(f.kind==0?2.1f:1.5f); Vector3 steer=(target-f.p)*.55f,separation=Vector3.zero,alignment=Vector3.zero;int near=0;
   foreach(var other in fish){if(other==f||other.kind!=f.kind)continue;Vector3 delta=f.p-other.p;float d=delta.sqrMagnitude;if(d<5){alignment+=other.v;near++;if(d<2.2f)separation+=delta/Mathf.Max(.05f,d);}}
   steer+=separation*2.6f;if(near>0)steer+=(alignment/near-f.v)*.6f;
   Vector3 away=f.p-game.Boat.transform.position;away.y=0;if(away.sqrMagnitude<25){steer+=away.normalized*5;speed*=1.7f;Avoidances++;}
   if(!Clear(f.p+f.v.normalized*1.8f)){steer+=Quaternion.Euler(0,100,0)*f.v*5;Avoidances++;}
   Vector3 v=Vector3.ClampMagnitude(f.v+steer*dt,speed);if(v.magnitude<speed*.55f)v=v.normalized*speed*.55f;
   Vector3 next=f.p+v*dt;if(Clear(next)){f.p=next;f.v=v;}else{f.v=Quaternion.Euler(0,120,0)*f.v;Avoidances++;}
   float bed=ocean.BottomHeight(f.p.x,f.p.z),sea=OceanSurface.Height(f.p,t);
   float y=f.kind==3?bed+.48f:sea-(f.kind==0?.65f:f.kind==1?1.05f:1.35f)+Mathf.Sin(t*1.3f+f.index)*.09f;
   y=Mathf.Clamp(y,bed+.35f,sea-.40f);Vector3 render=f.p;render.y=y;
   f.root.SetPositionAndRotation(render,Quaternion.Slerp(f.root.rotation,Quaternion.LookRotation(f.v),dt*4));
   MinBed=Mathf.Min(MinBed,y-bed);MinSurface=Mathf.Min(MinSurface,sea-y);if(float.IsNaN(y)||y<bed||y>sea)InvalidPositions++;
  }
 }
 Mesh Shape(int kind){
  var v=new List<Vector3>();var colors=new List<Color>();var tr=new List<int>();
  int rings=20,sides=16;
  for(int z=0;z<=rings;z++)for(int j=0;j<=sides;j++){
   float f=z/(float)rings,a=j*Mathf.PI*2/sides,profile=Mathf.Pow(Mathf.Max(0,Mathf.Sin(f*Mathf.PI)),.72f);
   float width=(kind==0?.12f:kind==1?.23f:kind==2?.13f:.65f),height=(kind==0?.13f:kind==1?.29f:kind==2?.13f:.10f),length=kind==2?1.4f:1;
   v.Add(new Vector3(Mathf.Cos(a)*width*profile,Mathf.Sin(a)*height*profile,(f-.5f)*length));
   Color c=Color.Lerp(new Color(.34f,.43f,.47f),new Color(1,1,.85f),Mathf.Sin(a)*-.5f+.5f);
   if(kind==1&&Mathf.Sin(f*22)>0.70f)c*=.50f;if(kind==2)c*=.76f;colors.Add(c);
  }
  for(int z=0;z<rings;z++)for(int j=0;j<sides;j++){int a=z*(sides+1)+j,b=a+sides+1;tr.Add(a);tr.Add(b);tr.Add(a+1);tr.Add(a+1);tr.Add(b);tr.Add(b+1);}
  if(kind==3){
   // Subdivided wings allow a smooth travelling flap in the vertex shader.
   for(int side=-1;side<=1;side+=2){int first=v.Count;
    for(int x=0;x<=12;x++)for(int z=0;z<=12;z++){float f=x/12f,q=z/12f;v.Add(new Vector3(side*(.12f+f*1.15f),.02f,(q-.5f)*(1-f*.7f)*1.45f-f*.12f));colors.Add(Color.Lerp(Color.white,new Color(.6f,.73f,.7f),f));}
    for(int x=0;x<12;x++)for(int z=0;z<12;z++){int a=first+x*13+z,b=a+13;tr.Add(a);tr.Add(b);tr.Add(a+1);tr.Add(a+1);tr.Add(b);tr.Add(b+1);}
   }
   Fin(v,colors,tr,new Vector3(-.04f,0,-.4f),new Vector3(0,0,-1.6f),new Vector3(.04f,0,-.4f),Color.white);
  }else{
   float tail=kind==2?-.70f:-.50f;
   Fin(v,colors,tr,new Vector3(0,0,tail+.08f),new Vector3(-.25f,.02f,tail-.30f),new Vector3(0,.03f,tail-.19f),Color.white);
   Fin(v,colors,tr,new Vector3(0,0,tail+.08f),new Vector3(0,.03f,tail-.19f),new Vector3(.25f,.02f,tail-.30f),Color.white);
   Fin(v,colors,tr,new Vector3(0,.1f,-.18f),new Vector3(0,kind==1?.48f:.28f,-.23f),new Vector3(0,.12f,.22f),new Color(.65f,.78f,.74f));
   for(int side=-1;side<=1;side+=2)Fin(v,colors,tr,new Vector3(side*.10f,0,.1f),new Vector3(side*.35f,-.02f,-.15f),new Vector3(side*.10f,0,-.1f),Color.white);
  }
  var mesh=new Mesh{name="Original smooth reef species "+kind};mesh.SetVertices(v);mesh.SetColors(colors);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var bnd=mesh.bounds;bnd.Expand(.8f);mesh.bounds=bnd;meshes.Add(mesh);return mesh;
 }
 static void Fin(List<Vector3> v,List<Color> c,List<int> t,Vector3 a,Vector3 b,Vector3 d,Color color){int i=v.Count;v.Add(a);v.Add(b);v.Add(d);c.Add(color);c.Add(color);c.Add(color);t.Add(i);t.Add(i+1);t.Add(i+2);}
 void OnDestroy(){foreach(var m in meshes)Destroy(m);foreach(var m in mats)Destroy(m);if(container!=null)Destroy(container.gameObject);}
}
}
