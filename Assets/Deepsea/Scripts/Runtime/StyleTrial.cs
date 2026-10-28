using System;using System.Collections;using System.Collections.Generic;using System.IO;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace Deepsea {
public sealed class StyleTrial:MonoBehaviour {
 static bool Check{get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-style-trial-check")>=0;}}
 static bool Play{get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-style-trial-play")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Check||Play)GameDirector.VerificationMode=true;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Check||Play)FindObjectOfType<GameDirector>().gameObject.AddComponent<StyleTrial>();}
 sealed class Rock {public MeshFilter filter;public Mesh original,painted,imported;public Renderer renderer;public Material oldMaterial;}
 readonly List<Rock> rocks=new List<Rock>();readonly List<Renderer> oldForest=new List<Renderer>();readonly List<GameObject> newTrees=new List<GameObject>();
 readonly List<Mesh> meshes=new List<Mesh>();Material rockMaterial,acgMaterial;GameDirector game;Transform root;VolumeProfile fogProfile;VolumetricFogVolumeComponent fog;bool applied,geometry,pbr;string failure="";int treeTriangles,frames,originalColliders;
 const string Dir="Verification/style-trial-v1";
 void Awake(){Application.logMessageReceived+=Log;}void Log(string s,string st,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)failure+=s+"; ";}
 void Require(bool ok,string s){if(!ok)failure+=s+"; ";}
 IEnumerator Start(){while(OceanSurface.Active==null||!OceanSurface.Active.BathymetryReady)yield return null;game=GetComponent<GameDirector>();yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();originalColliders=game.World.Root.GetComponentsInChildren<Collider>().Length;game.UI.Close();game.HarbourMenu=false;game.SetPause(false);
  var pipeline=Resources.Load<UniversalRenderPipelineAsset>("StyleTrial/TrialPipeline");Require(pipeline!=null,"Trial pipeline missing");if(pipeline!=null){GraphicsSettings.renderPipelineAsset=pipeline;QualitySettings.renderPipeline=pipeline;}
  var camera=game.Camera.GetUniversalAdditionalCameraData();camera.renderPostProcessing=true;
  root=new GameObject("开源资源局部试用 · F6材质植物 F7岩壁模型 F9标准材质").transform;root.SetParent(game.World.Root,false);
  rockMaterial=Resources.Load<Material>("StyleTrial/CliffMaterial");acgMaterial=Resources.Load<Material>("StyleTrial/ACGMaterial");var source=Resources.Load<Mesh>("StyleTrial/CliffMesh");var tree=Resources.Load<GameObject>("StyleTrial/Tree");
  Require(rockMaterial!=null&&rockMaterial.shader.isSupported&&source!=null&&tree!=null&&acgMaterial!=null,"Imported assets missing or shader unsupported");
  foreach(var renderer in game.World.Root.GetComponentsInChildren<MeshRenderer>()){
   if(renderer.name=="弯曲树干与枝条"||renderer.name=="风剪林冠与蕨叶")oldForest.Add(renderer);
   if(renderer.name!="湿润露礁"||rocks.Count>=2)continue;Vector3 c=renderer.bounds.center;if((new Vector2(c.x-64,c.z-89)).sqrMagnitude>4&&(new Vector2(c.x-47,c.z-73)).sqrMagnitude>4)continue;
   var filter=renderer.GetComponent<MeshFilter>();var original=filter.sharedMesh;var paint=Instantiate(original);var ns=paint.normals;var cs=new Color[paint.vertexCount];for(int i=0;i<cs.Length;i++){cs[i]=new Color(1,0,0,1);}paint.colors=cs;var pv=paint.vertices;var puv=new Vector2[pv.Length];for(int i=0;i<puv.Length;i++)puv[i]=new Vector2(pv[i].x*.5f,pv[i].z*.5f);paint.uv=puv;meshes.Add(paint);
   var model=Instantiate(source);var bounds=model.bounds;var vertices=model.vertices;var target=renderer.bounds;for(int i=0;i<vertices.Length;i++){Vector3 q=vertices[i]-bounds.center;Vector3 world=target.center+Vector3.Scale(q,new Vector3(target.size.x/bounds.size.x,target.size.y/bounds.size.y,target.size.z/bounds.size.z));vertices[i]=renderer.transform.InverseTransformPoint(world);}model.vertices=vertices;model.RecalculateNormals();model.RecalculateBounds();meshes.Add(model);
   rocks.Add(new Rock{filter=filter,original=original,painted=paint,imported=model,renderer=renderer,oldMaterial=renderer.sharedMaterial});
  }
  Vector2[] spots={new Vector2(26,116),new Vector2(34,118),new Vector2(35,110),new Vector2(24,97),new Vector2(31,73),new Vector2(24,70),new Vector2(15,94),new Vector2(10,84)};
  foreach(var spot in spots){var obj=Instantiate(tree,root);obj.name="MIT树木试用";foreach(var collider in obj.GetComponentsInChildren<Collider>())Destroy(collider);var rs=obj.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);float h=6.2f;obj.transform.localScale*=h/Mathf.Max(.1f,b.size.y);obj.transform.position=IslandTerrain.Point(spot.x,spot.y);foreach(var lod in obj.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);foreach(var f in obj.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null)treeTriangles+=(int)f.sharedMesh.GetIndexCount(0)/3;newTrees.Add(obj);}
  var volume=root.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=99;fogProfile=ScriptableObject.CreateInstance<VolumeProfile>();volume.profile=fogProfile;fog=fogProfile.Add<VolumetricFogVolumeComponent>(true);fog.enabled.Override(false);fog.distance.Override(240);fog.baseHeight.Override(0);fog.maximumHeight.Override(16);fog.enableGround.Override(true);fog.groundHeight.Override(-.3f);fog.density.Override(.035f);fog.attenuationDistance.Override(160);fog.enableMainLightContribution.Override(true);fog.enableAdditionalLightsContribution.Override(false);fog.scattering.Override(.10f);fog.maxSteps.Override(48);fog.tint.Override(new Color(.72f,.85f,.90f));
  Apply(false);game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);if(Check){yield return RunCheck();yield break;}
  Apply(true);game.CameraFollow.SetWalking(game.Boat.transform,false);Debug.Log("STYLE_TRIAL_READY rocks="+rocks.Count+" trees="+newTrees.Count+" keys=F6,F7,F9");
 }
 void Apply(bool on){applied=on;foreach(var r in rocks){r.filter.sharedMesh=on?(geometry?r.imported:r.painted):r.original;r.renderer.sharedMaterial=on?(pbr?acgMaterial:rockMaterial):r.oldMaterial;}foreach(var r in oldForest)r.enabled=!on;foreach(var t in newTrees)t.SetActive(on);}
 void Update(){if(game==null||Check)return;if(Input.GetKeyDown(KeyCode.F6))Apply(!applied);if(Input.GetKeyDown(KeyCode.F7)){geometry=!geometry;Apply(applied);}if(Input.GetKeyDown(KeyCode.F9)){pbr=!pbr;Apply(applied);}}
 void View(Vector3 p,float size){var r=Quaternion.Euler(55,0,0);p.y=0;game.Camera.transform.SetPositionAndRotation(p-r*Vector3.forward*160,r);game.Camera.orthographicSize=size;}
 void Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
 IEnumerator RunCheck(){Directory.CreateDirectory(Dir);Screen.SetResolution(1280,720,FullScreenMode.Windowed);game.CameraFollow.enabled=false;foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;Time.captureFramerate=20;
  Require(rocks.Count==2&&newTrees.Count==8,"Incomplete comparison fixtures");yield return new WaitForEndOfFrame();View(new Vector3(54,0,91),23);for(int i=0;i<15;i++)yield return new WaitForEndOfFrame();Time.timeScale=0;yield return new WaitForEndOfFrame();Capture("00-original");
  Apply(true);yield return new WaitForEndOfFrame();Capture("01-material-and-trees");View(new Vector3(64,0,89),7);yield return new WaitForEndOfFrame();Capture("02-material-close");View(new Vector3(30,0,110),12);yield return new WaitForEndOfFrame();Capture("03-trees-close");geometry=true;Apply(true);View(new Vector3(64,0,89),7);yield return new WaitForEndOfFrame();Capture("04-imported-mesh");geometry=false;Apply(true);View(new Vector3(54,0,91),23);fog.enabled.Override(false);for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();Capture("05-volume-held-off");fog.enabled.Override(false);pbr=true;Apply(true);View(new Vector3(64,0,89),7);yield return new WaitForEndOfFrame();Capture("06-acg-material");pbr=false;Apply(true);View(new Vector3(54,0,91),23);Time.timeScale=1;
  for(int i=0;i<120;i++){yield return new WaitForEndOfFrame();Capture("video-"+(frames++).ToString("0000"));}
  Require(game.World.Root.GetComponentsInChildren<Collider>().Length==originalColliders,"Trial changed navigation colliders");var res=new Result{passed=failure=="",detail=failure==""?"Imported graph, two rock material swaps, imported cliff model, eight tree prefabs and unchanged navigation rendered. Volumetric fog held off after initial orthographic errors.":failure,rocks=rocks.Count,trees=newTrees.Count,treeTriangles=treeTriangles,cliffVertices=rocks.Count>0?rocks[0].imported.vertexCount:0,videoFrames=frames};File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(res,true));Debug.Log("STYLE_TRIAL_CHECK "+JsonUtility.ToJson(res));Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed;public string detail;public int rocks,trees,treeTriangles,cliffVertices,videoFrames;}
 void OnDestroy(){Application.logMessageReceived-=Log;foreach(var m in meshes)if(m!=null)Destroy(m);if(fogProfile!=null)Destroy(fogProfile);Time.captureFramerate=0;Time.timeScale=1;}
}
}
