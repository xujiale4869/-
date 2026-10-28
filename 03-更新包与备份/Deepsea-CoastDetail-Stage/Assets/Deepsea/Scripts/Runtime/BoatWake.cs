using UnityEngine;
namespace Deepsea {
public sealed class BoatWake : MonoBehaviour {
 public BoatController boat;
 const int Count=42;
 struct Sample{public Vector3 position,right;public float born,speed;}
 readonly Sample[] history=new Sample[Count];
 readonly Vector3[] vertices=new Vector3[Count*4];
 readonly Vector2[] uvs=new Vector2[Count*4];
 readonly Color[] colors=new Color[Count*4];
 Mesh mesh;Material material;GameObject wake;int next;float emitAt;
 Vector3 lastBoatPosition;bool hasPosition;
 void Start(){
  wake=new GameObject("船尾渐散泡沫");wake.transform.SetParent(boat.game.World.Root,false);wake.layer=4;
  mesh=new Mesh{name="船尾两侧连续尾流"};mesh.MarkDynamic();
  int[] triangles=new int[(Count-1)*12];int k=0;
  for(int side=0;side<2;side++)for(int i=0;i<Count-1;i++){
   int a=side*Count*2+i*2;
   triangles[k++]=a;triangles[k++]=a+2;triangles[k++]=a+1;
   triangles[k++]=a+1;triangles[k++]=a+2;triangles[k++]=a+3;
  }
  for(int i=0;i<Count;i++)history[i].born=-100;
  mesh.vertices=vertices;mesh.triangles=triangles;
  wake.AddComponent<MeshFilter>().sharedMesh=mesh;
  material=new Material(Shader.Find("Deepsea/WakeFoam"));
  material.SetTexture("_FoamMap",Resources.Load<Texture2D>("Water/basicFoam"));
  wake.AddComponent<MeshRenderer>().sharedMaterial=material;
 }
 void LateUpdate(){
  if(boat==null||mesh==null)return;
  Vector3 boatPosition=boat.transform.position;
  // Do not bridge a recovery/landing teleport with a transparent triangle across the bay.
  if(hasPosition&&Vector3.Distance(boatPosition,lastBoatPosition)>Mathf.Max(8,boat.Speed*Time.deltaTime*4)){
   for(int j=0;j<Count;j++)history[j].born=-100;
   next=0;emitAt=Time.time;
  }
  lastBoatPosition=boatPosition;hasPosition=true;
  if(Time.time>=emitAt&&!boat.Moored&&boat.Speed>.15f){emitAt=Time.time+.12f;history[next]=new Sample{position=boatPosition-boat.transform.forward*1.8f,right=boat.transform.right,born=Time.time,speed=boat.Speed};next=(next+1)%Count;}
  Sample lastValid=new Sample{position=boatPosition-boat.transform.forward*1.8f,right=boat.transform.right,born=Time.time,speed=0};
  for(int side=0;side<2;side++)for(int i=0;i<Count;i++){
   Sample s=history[(next+Count-1-i)%Count];bool valid=s.born>=0;
   if(valid)lastValid=s;else s=lastValid;
   float age=Mathf.Max(0,Time.time-s.born);
   float sign=side==0?-1:1;float width=.08f+age*.21f+Mathf.Clamp01(s.speed/5)*.10f;float spread=.65f+age*.48f;
   float alpha=Mathf.Clamp01(s.speed/5)*Mathf.Pow(Mathf.Clamp01(1-age/4.8f),1.6f)*.48f;
   if(!valid)alpha=0;
   for(int edge=0;edge<2;edge++){
    int index=side*Count*2+i*2+edge;
    Vector3 p=s.position+s.right*sign*(spread+(edge==0?-width:width));
    p.y=OceanSurface.Height(p,Time.time)+.025f;
    vertices[index]=p;uvs[index]=new Vector2(edge,s.born*.8f);
    colors[index]=new Color(.62f,.76f,.72f,alpha);
   }
  }
  mesh.vertices=vertices;mesh.uv=uvs;mesh.colors=colors;mesh.RecalculateBounds();
 }
 void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(wake!=null)Destroy(wake);}
}
}

