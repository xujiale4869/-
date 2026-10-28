using System;
using System.Collections.Generic;
using UnityEngine;
namespace Deepsea {
public sealed class CoastHeroVisuals:MonoBehaviour {
 public bool Ready{get;private set;}public int BoatTriangles,CharacterTriangles;
 public int AddedColliders{get{return boat==null?0:boat.GetComponentsInChildren<Collider>(true).Length+captain.GetComponentsInChildren<Collider>(true).Length+walker.GetComponentsInChildren<Collider>(true).Length;}}
 public bool EnvelopeSafe{get;private set;}
 public bool MaterialsSupported{get;private set;}
 public bool MaterialsIntact{get{return walker!=null&&walker.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length==4&&boat.GetComponent<MeshRenderer>().sharedMaterials.Length==7;}}
 public bool CaptainVisible{get{return captain!=null&&captain.activeSelf;}}
 public bool Visible{get;private set;}
 GameDirector game;GameObject boat,captain,walker;CoastHeroRig walkRig,captainRig;
 readonly List<Renderer> originals=new List<Renderer>();readonly List<bool> originalEnabled=new List<bool>();Vector3 lastWalk;
 public void Initialize(CoastBlockoutRegion region,GameDirector director,CoastMaterialsAppearance look){
  game=director;
  foreach(var r in game.Boat.hull.GetComponentsInChildren<Renderer>(true))Remember(r);
  foreach(var r in game.Expedition.Explorer.model.GetComponentsInChildren<Renderer>(true)){
   var tool=game.Expedition.Explorer.tool;if(tool!=null&&r.transform.IsChildOf(tool))continue;Remember(r);
  }
  boat=Spawn("CoastHero/Rowboat",game.Boat.hull,"有贴图木船 · 视觉替换");
  captain=Spawn("CoastHero/Fisherman",game.Boat.hull,"船上渔人 · 坐姿");
  captain.transform.localPosition=new Vector3(0,-.32f,.02f);captainRig=captain.GetComponent<CoastHeroRig>();captainRig.seated=true;
  walker=Spawn("CoastHero/Fisherman",game.Expedition.Explorer.model,"同一渔人 · 步行");
  float scale=game.Expedition.Explorer.model.localScale.x;walker.transform.localScale=Vector3.one/Mathf.Max(.01f,scale);walkRig=walker.GetComponent<CoastHeroRig>();
  foreach(var f in boat.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null)BoatTriangles+=f.sharedMesh.triangles.Length/3;
  var bounds=boat.GetComponent<MeshFilter>().sharedMesh.bounds;EnvelopeSafe=bounds.size.x<=1.67f&&bounds.size.y<=.80f&&bounds.size.z<=4.17f&&bounds.min.y>=-.12f;
  CharacterTriangles=walker.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.triangles.Length/3;
  MaterialsSupported=true;foreach(var r in boat.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)MaterialsSupported&=m!=null&&m.shader.isSupported;foreach(var m in walker.GetComponent<SkinnedMeshRenderer>().sharedMaterials)MaterialsSupported&=m!=null&&m.shader.isSupported;
  lastWalk=game.Expedition.Explorer.transform.position;Ready=true;SetVisible(!look.Grey);
  foreach(var r in boat.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)Debug.Log("COAST_HERO_MATERIAL boat="+m.name+" shader="+m.shader.name+" supported="+m.shader.isSupported);
  foreach(var m in walker.GetComponent<SkinnedMeshRenderer>().sharedMaterials)Debug.Log("COAST_HERO_MATERIAL walker="+m.name+" shader="+m.shader.name+" supported="+m.shader.isSupported);
  Debug.Log("COAST_HERO_READY boatTriangles="+BoatTriangles+" characterTriangles="+CharacterTriangles+" colliders="+AddedColliders);
 }
 void Remember(Renderer r){originals.Add(r);originalEnabled.Add(r.enabled);}
 static GameObject Spawn(string path,Transform parent,string name){var prefab=Resources.Load<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Missing hero model: "+path);var g=Instantiate(prefab,parent,false);g.name=name;return g;}
 public void SetVisible(bool value){Visible=value;if(boat==null)return;boat.SetActive(value);walker.SetActive(value);captain.SetActive(value&&!game.Expedition.OnFoot);for(int i=0;i<originals.Count;i++)if(originals[i]!=null)originals[i].enabled=value?false:originalEnabled[i];}
 void LateUpdate(){if(!Ready)return;if(Visible)foreach(var r in originals)if(r!=null)r.enabled=false;captain.SetActive(Visible&&!game.Expedition.OnFoot);Vector3 p=game.Expedition.Explorer.transform.position;walkRig.speed=Time.deltaTime>0?Vector3.Distance(p,lastWalk)/Time.deltaTime:0;lastWalk=p;}
 void OnDestroy(){for(int i=0;i<originals.Count;i++)if(originals[i]!=null)originals[i].enabled=originalEnabled[i];if(boat!=null)Destroy(boat);if(captain!=null)Destroy(captain);if(walker!=null)Destroy(walker);}
}
}
