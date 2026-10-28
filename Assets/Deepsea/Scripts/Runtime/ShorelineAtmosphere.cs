using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
/// <summary>Local east-shore swash and manual world-distance sound for the orthographic camera.</summary>
public sealed class ShorelineAtmosphere : MonoBehaviour {
 sealed class Rock {public MeshFilter filter;public Renderer renderer;public Mesh oldMesh;public Material oldMaterial;}
 readonly List<Rock> rocks=new List<Rock>();readonly List<Mesh> meshes=new List<Mesh>();
 readonly List<Material> materials=new List<Material>();readonly Vector4[] rockData=new Vector4[2];
 readonly AudioSource[] surf=new AudioSource[3];readonly Vector3[] soundPoints=new Vector3[3];
 GameDirector game;Transform root;AudioSource bed;MeshCollider ground;float overrideTime=-1;
 public bool Ready {get;private set;}
 public bool AudioReady {get;private set;}
 public int RockCount {get{return rocks.Count;}}
 public int ColliderCountBefore {get;private set;}
 public int ColliderCountAfter {get;private set;}
 public float MaximumAudioVolume {get;private set;}
 public float Clock {get{return overrideTime>=0?overrideTime:Time.time;}}
 public void Initialize(GameDirector director){
  game=director;ColliderCountBefore=game.World.Root.GetComponentsInChildren<Collider>().Length;
  root=VisualFactory.Group("东岸浪涌 · 泡沫与退水",game.World.Root,Vector3.zero);root.gameObject.layer=4;
  foreach(var collider in game.World.Root.GetComponentsInChildren<MeshCollider>())if(collider.gameObject.name=="雾岬岛 · 可步行地形"){ground=collider;break;}
  MakeSwash();RefineRocks();MakeAudio();
  ColliderCountAfter=game.World.Root.GetComponentsInChildren<Collider>().Length;
  Ready=true;Tick();
 }
 public void SetPreviewTime(float value){overrideTime=value;Tick();}
 void MakeSwash(){
  const int along=112,across=18;var v=new Vector3[(along+1)*(across+1)];var tr=new int[along*across*6];
  for(int z=0;z<=along;z++){
   float wz=Mathf.Lerp(76,109,z/(float)along),edge=ShoreCycle.EastShoreX(wz);
   for(int x=0;x<=across;x++){
    float wx=edge+Mathf.Lerp(-1.8f,2.8f,x/(float)across);
    float height=IslandTerrain.Height(wx,wz);RaycastHit hit;
    if(ground!=null&&ground.Raycast(new Ray(new Vector3(wx,12,wz),Vector3.down),out hit,30))height=hit.point.y;
    v[z*(across+1)+x]=new Vector3(wx,height+.018f,wz);
   }
  }
  int k=0;for(int z=0;z<along;z++)for(int x=0;x<across;x++){
   int a=z*(across+1)+x,b=a+across+1;
   tr[k++]=a;tr[k++]=b;tr[k++]=a+1;tr[k++]=a+1;tr[k++]=b;tr[k++]=b+1;
  }
  var mesh=new Mesh{name="沿真实坡岸贴合的薄水膜"};mesh.vertices=v;mesh.triangles=tr;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
  var material=new Material(Shader.Find("Deepsea/ShoreSwash"));materials.Add(material);
  var go=new GameObject("东岸薄水膜 · 无碰撞");go.layer=4;go.transform.SetParent(root,false);
  go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
  renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
 }
 void RefineRocks(){
  Vector2[] targets={new Vector2(64,89),new Vector2(47,73)};
  var renderers=game.World.Root.GetComponentsInChildren<MeshRenderer>();
  for(int n=0;n<targets.Length;n++)foreach(var renderer in renderers){
   if(renderer.gameObject.name!="湿润露礁")continue;
   Bounds b=renderer.bounds;Vector2 p=new Vector2(b.center.x,b.center.z);
   if((p-targets[n]).sqrMagnitude>4)continue;
   var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
   rocks.Add(new Rock{filter=filter,renderer=renderer,oldMesh=filter.sharedMesh,oldMaterial=renderer.sharedMaterial});
   var mesh=RockMesh(b,renderer.transform,17+n*31);meshes.Add(mesh);filter.sharedMesh=mesh;
   var material=new Material(Shader.Find("Deepsea/CoastalGround"));material.SetFloat("_SurfaceKind",2);material.SetColor("_BaseColor",new Color(.34f,.40f,.35f));
   materials.Add(material);renderer.sharedMaterial=material;
   rockData[rocks.Count-1]=new Vector4(b.center.x,b.center.z,b.extents.x,b.extents.z);break;
  }
 }
 static Mesh RockMesh(Bounds bounds,Transform frame,int seed){
  const int sides=64,rings=24;
  var v=new Vector3[rings*sides+2];var tr=new List<int>();
  for(int ring=0;ring<rings;ring++)for(int side=0;side<sides;side++){
   float f=ring/(float)(rings-1),a=side*Mathf.PI*2/sides;
   float irregular=(.94f+.06f*Mathf.Sin(a*3+seed+f*1.8f))*(.96f+.04f*Mathf.Cos(a*7-seed));
   float radius=(.82f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*(.16f+.84f*f))),.52f)+.06f)*irregular;
   Vector3 p=new Vector3(bounds.center.x+Mathf.Cos(a)*bounds.extents.x*radius,
    Mathf.Lerp(bounds.min.y,bounds.max.y,f)+Mathf.Sin(a*3+seed)*bounds.size.y*.035f*Mathf.Sin(f*Mathf.PI),
    bounds.center.z+Mathf.Sin(a)*bounds.extents.z*radius);
   p.y=Mathf.Clamp(p.y,bounds.min.y,bounds.max.y);v[ring*sides+side]=frame.InverseTransformPoint(p);
  }
  for(int ring=0;ring<rings-1;ring++)for(int side=0;side<sides;side++){
   int a=ring*sides+side,b=a+sides,c=ring*sides+(side+1)%sides,d=c+sides;
   tr.Add(a);tr.Add(b);tr.Add(c);tr.Add(c);tr.Add(b);tr.Add(d);
  }
  int bottom=v.Length-2,top=v.Length-1;v[bottom]=frame.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
  v[top]=frame.InverseTransformPoint(new Vector3(bounds.center.x,bounds.max.y,bounds.center.z));
  for(int side=0;side<sides;side++){
   tr.Add(bottom);tr.Add(side);tr.Add((side+1)%sides);
   int a=(rings-1)*sides+side,b=(rings-1)*sides+(side+1)%sides;tr.Add(top);tr.Add(b);tr.Add(a);
  }
  var mesh=new Mesh{name="原创不规则潮湿礁石"};mesh.vertices=v;mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 AudioSource Source(string label,AudioClip clip){
  var go=new GameObject(label);go.transform.SetParent(root,false);
  var source=go.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.playOnAwake=false;
  // The camera/listener is 160m above the scene; distance/pan use the actual boat or walker.
  source.spatialBlend=0;source.volume=0;source.priority=170;return source;
 }
 void MakeAudio(){
  AudioClip clip=Resources.Load<AudioClip>("Coast/OceanSurf");AudioReady=clip!=null;
  if(clip==null){Debug.LogWarning("Missing coast ocean recording");return;}
  bed=Source("东岸远处海声",clip);bed.time=3;bed.Play();
  for(int i=0;i<surf.Length;i++){
   float z=82+i*11;soundPoints[i]=new Vector3(ShoreCycle.EastShoreX(z)+1,0,z);
   surf[i]=Source("岸浪声 · "+i,clip);surf[i].time=(i*7.7f)%clip.length;surf[i].Play();
  }
 }
 Vector3 Listener(){
  if(game.Expedition!=null&&game.Expedition.OnFoot&&game.Expedition.Explorer!=null)return game.Expedition.Explorer.transform.position;
  return game.Boat.transform.position;
 }
 void Update(){if(Ready)Tick();}
 void Tick(){
  Shader.SetGlobalFloat("_BaySurfEnabled",1);Shader.SetGlobalFloat("_BaySurfClock",Clock);
  Shader.SetGlobalInt("_BaySurfRockCount",rocks.Count);Shader.SetGlobalVectorArray("_BaySurfRocks",rockData);
  MaximumAudioVolume=0;if(!AudioReady||game.Boat==null)return;
  Vector3 listener=Listener();listener.y=0;float volume=game.Paused?0:game.Audio.EffectsVolume;
  float total=0;for(int i=0;i<surf.Length;i++)total+=ShoreCycle.AudioDistance(Vector3.Distance(listener,soundPoints[i]));
  for(int i=0;i<surf.Length;i++){
   Vector3 delta=soundPoints[i]-listener;float weight=ShoreCycle.AudioDistance(delta.magnitude);
   float crash=ShoreCycle.Crash(ShoreCycle.Phase(soundPoints[i].z,Clock));
   surf[i].volume=volume*weight/Mathf.Max(1,total)*(.035f+.45f*crash);
   surf[i].panStereo=Mathf.Clamp(Vector3.Dot(delta,game.Camera.transform.right)/28,-.65f,.65f);
   MaximumAudioVolume=Mathf.Max(MaximumAudioVolume,surf[i].volume);
  }
  float bedWeight=ShoreCycle.AudioDistance(Vector3.Distance(listener,soundPoints[1]));bed.volume=volume*bedWeight*.055f;
  MaximumAudioVolume=Mathf.Max(MaximumAudioVolume,bed.volume);
 }
 void OnDestroy(){
  foreach(var rock in rocks){if(rock.filter!=null)rock.filter.sharedMesh=rock.oldMesh;if(rock.renderer!=null)rock.renderer.sharedMaterial=rock.oldMaterial;}
  if(root!=null)Destroy(root.gameObject);foreach(var m in meshes)if(m!=null)Destroy(m);foreach(var m in materials)if(m!=null)Destroy(m);
  Shader.SetGlobalFloat("_BaySurfEnabled",0);Shader.SetGlobalInt("_BaySurfRockCount",0);
 }
}
}
