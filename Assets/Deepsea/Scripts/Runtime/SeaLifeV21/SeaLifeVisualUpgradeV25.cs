// v25 handoff adapted for the existing v21 save/gameplay and v24 sea scene.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using OceanGame.Data;

namespace Deepsea {
 [DefaultExecutionOrder(650)]
 public sealed partial class SeaLifeVisualUpgradeV25 : MonoBehaviour {
  public FishAndAquariumDatabaseSO database;
  public bool Ready {get;private set;}
  public int FishUpgrades {get;private set;}
  public int TankUpgrades {get;private set;}
  public float Clock {get;private set;}
  public Mesh[] FishMeshes {get;private set;}
  public Material[] FishMaterials {get;private set;}
  public Mesh FrameMesh,InteriorMesh,GlassMesh;
  public Material GlassMaterial;
  SeaLifeRuntime game;
  Material[] frameMaterials,interiorMaterials;
  Material surfaceMaterial,causticsMaterial;
  readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();

  // Only the new entry opts in. Loading an old scene does not upgrade it implicitly.
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void AutoBootstrap(){
   var rt=FindObjectOfType<SeaLifeRuntime>();
   if(rt!=null&&rt.gameObject.scene.name=="CoastSeaLifeV25"&&rt.GetComponent<SeaLifeVisualUpgradeV25>()==null)
    rt.gameObject.AddComponent<SeaLifeVisualUpgradeV25>();
  }
  IEnumerator Start(){
   game=GetComponent<SeaLifeRuntime>();
   while(game!=null&&!game.Ready)yield return null;
   if(game==null)yield break;
   database=database!=null?database:Resources.Load<FishAndAquariumDatabaseSO>("SeaLifeV25/FishAndAquariumDatabase");
   if(database==null)throw new InvalidOperationException("v25 database missing: run SeaLifeV25Build.PrepareAssets");
   FishMeshes=new Mesh[3];FishMaterials=new Material[3];
   for(int i=0;i<3;i++){
    var f=database.GetFishById(i);
    FishMeshes[i]=f.highDetailMesh;
    FishMaterials[i]=Resources.Load<Material>("SeaLifeV25/Materials/Fish"+i);
    if(FishMeshes[i]==null||FishMaterials[i]==null||!FishMaterials[i].shader.isSupported)
     throw new InvalidOperationException("v25 fish assets incomplete: "+i);
   }
   var style=database.defaultTankStyle;
   FrameMesh=style.frameMesh;InteriorMesh=style.interiorMesh;GlassMesh=style.glassMesh;
   if(FrameMesh==null||InteriorMesh==null||GlassMesh==null)throw new InvalidOperationException("v25 tank meshes incomplete");
   frameMaterials=new[]{Material("Wood"),Material("Brass"),Material("Lamp")};
   interiorMaterials=new[]{Material("Sand"),Material("Reef")};
   GlassMaterial=Material("Glass");surfaceMaterial=Material("WaterSurface");causticsMaterial=Material("Caustics");
   // Make only animated materials private; imported materials remain reusable assets.
   surfaceMaterial=Own(new Material(surfaceMaterial));causticsMaterial=Own(new Material(causticsMaterial));
   Ready=true;game.InstallVisualsV25(this);SetupReviewV25();
   if(!checking)game.Notify("v25鱼类与展缸升级 · 原钓鱼、建缸和收藏存档继续使用。");
  }
  T Own<T>(T o)where T:UnityEngine.Object{owned.Add(o);return o;}
  Material Material(string n){var m=Resources.Load<Material>("SeaLifeV25/Materials/"+n);if(m==null)throw new InvalidOperationException("v25 material missing "+n);return m;}
  void Update(){if(!Ready)return;if(!game.Paused)Clock+=Time.deltaTime;
   var t=Clock*.14f;causticsMaterial.SetVector("_CausticsOffset",new Vector4(t,t*.7f,-t*.6f,t*.9f));
   surfaceMaterial.SetVector("_CausticsOffset",new Vector4(t*.35f,t*.23f,-t*.17f,t*.2f));
  }
  public void DecorateFish(Transform fish,int species){
   if(!Ready)return;var s=database.GetFishById(species);
   var filter=fish.GetComponent<MeshFilter>();var renderer=fish.GetComponent<MeshRenderer>();
   if(filter==null||renderer==null)return;
   filter.sharedMesh=FishMeshes[species];renderer.sharedMaterial=FishMaterials[species];
   var swimmer=fish.GetComponent<SeaLifeFishSwimmerV25>();
   if(swimmer==null){swimmer=fish.gameObject.AddComponent<SeaLifeFishSwimmerV25>();FishUpgrades++;}
   swimmer.Configure(game,species,s.tailWaveAmplitude,s.tailWaveFrequency);
   // Preserve the original gameplay scale, orbit and head-forward transform.
  }
  public void DecorateTank(Transform tank,int index){
   if(!Ready||tank.Find("V25_AquariumDetails")!=null)return;
   foreach(Transform child in tank){
    string n=child.name;
    bool replace=n=="木柜底座"||n=="缸底砂"||n=="缸内主石"||n=="局部水面"||n.Contains("玻璃")||n.StartsWith("缸角")||n.StartsWith("缸边");
    if(!replace)continue;
    foreach(var r in child.GetComponentsInChildren<Renderer>(true))r.enabled=false;
    // Keep the old cabinet collider: walking/placement limits remain the same.
   }
   var group=new GameObject("V25_AquariumDetails").transform;group.SetParent(tank,false);
   Child("倒角木柜、黄铜包角与悬灯",group,FrameMesh,frameMaterials);
   Child("波纹砂床与五块层理礁石",group,InteriorMesh,interiorMaterials);
   var glass=Child("超白倒角玻璃",group,GlassMesh,new[]{GlassMaterial});
   glass.shadowCastingMode=ShadowCastingMode.Off;glass.receiveShadows=false;
   // One top surface rather than six overlapping transparent cube faces.
   var top=GameObject.CreatePrimitive(PrimitiveType.Quad);top.name="缸内水面";Destroy(top.GetComponent<Collider>());top.transform.SetParent(group,false);
   top.transform.localPosition=new Vector3(0,2.10f,0);top.transform.localRotation=Quaternion.Euler(90,0,0);top.transform.localScale=new Vector3(3.40f,1.88f,1);
   var tr=top.GetComponent<MeshRenderer>();tr.sharedMaterial=surfaceMaterial;tr.shadowCastingMode=ShadowCastingMode.Off;
   var sandGlow=Child("贴合波纹砂床的动态焦散",group,Resources.Load<Mesh>("SeaLifeV25/Meshes/SandCaustics"),new[]{causticsMaterial});
   sandGlow.shadowCastingMode=ShadowCastingMode.Off;sandGlow.receiveShadows=false;
   var lamp=new GameObject("暖白生态顶灯").AddComponent<Light>();lamp.transform.SetParent(group,false);lamp.transform.localPosition=new Vector3(0,2.31f,0);
   lamp.type=LightType.Point;lamp.range=3.4f;lamp.color=database.defaultTankStyle.topLightColor;lamp.intensity=database.defaultTankStyle.topLightIntensity;lamp.shadows=LightShadows.None;
   TankUpgrades++;
  }
  static MeshRenderer Child(string name,Transform parent,Mesh mesh,Material[] materials){
   var o=new GameObject(name);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;
   var r=o.AddComponent<MeshRenderer>();r.sharedMaterials=materials;return r;
  }
  void OnDestroy(){if(checking)Application.logMessageReceived-=ReviewLog;foreach(var o in owned)if(o!=null)Destroy(o);}
 }

 public sealed partial class SeaLifeRuntime {
  SeaLifeVisualUpgradeV25 visualsV25;Mesh[] previousFishMeshesV25;Material[] previousFishMaterialsV25;
  internal void InstallVisualsV25(SeaLifeVisualUpgradeV25 visuals){
   var oldMeshes=fishMeshes;previousFishMeshesV25=fishMeshes;previousFishMaterialsV25=fishMaterials;visualsV25=visuals;
   // One bounded traversal of gameplay fish, including the inactive caught fish.
   foreach(var f in actorRoot.GetComponentsInChildren<MeshFilter>(true)){
    int species=Array.IndexOf(oldMeshes,f.sharedMesh);if(species>=0)visuals.DecorateFish(f.transform,species);
   }
   fishMeshes=visuals.FishMeshes;fishMaterials=visuals.FishMaterials;RefreshCollection();
  }
  internal void SetReviewFishBaselineV25(bool baseline){
   foreach(var mf in actorRoot.GetComponentsInChildren<MeshFilter>(true)){
    int species=Array.IndexOf(fishMeshes,mf.sharedMesh);if(species<0)species=Array.IndexOf(previousFishMeshesV25,mf.sharedMesh);if(species<0)continue;
    mf.sharedMesh=baseline?previousFishMeshesV25[species]:fishMeshes[species];mf.GetComponent<Renderer>().sharedMaterial=baseline?previousFishMaterialsV25[species]:fishMaterials[species];
    var swimmer=mf.GetComponent<SeaLifeFishSwimmerV25>();if(swimmer!=null)swimmer.enabled=!baseline;
   }
  }
  void DecorateFishV25(Transform fish,int species){if(visualsV25!=null)visualsV25.DecorateFish(fish,species);}
  void DecorateTankV25(Transform tank,int index){if(visualsV25!=null)visualsV25.DecorateTank(tank,index);}
  internal void RefreshVisualCollectionV25(){RefreshCollection();}
  internal Transform[] VisualTanksV25{get{return tankObjects;}}
  internal Transform VisualFishV25{get{return indoorFish;}}
  internal void EnterVisualReviewRoomV25(){
   AutoInput=true;DriveInput=Vector2.zero;Panel=0;
   if(InRoom)LeaveRoom();if(OnFoot)ToggleBoard();PlaceBoat(dockBerth,90);ToggleBoard();PlaceWalker(Grounded(galleryDoor+Vector3.forward*.7f));Interact();
  }
 }
}
