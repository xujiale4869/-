using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Deepsea.Editor {[InitializeOnLoad] public static class SeaVillagePlotProbeV31 {
 static double entered;
 static SeaVillagePlotProbeV31(){entered=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}
 public static void RunPlay(){SessionState.SetBool("VillageV31PlotProbe",true);EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaVillageV31.unity");EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool("VillageV31PlotProbe",false))return;if(EditorApplication.timeSinceStartup-entered>90){SessionState.SetBool("VillageV31PlotProbe",false);EditorApplication.Exit(2);return;}if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup-entered<8)return;var world=UnityEngine.Object.FindObjectOfType<SeaSeamlessWorldAndNauticalUIV29>();var g=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();if(world==null||g==null)return;SessionState.SetBool("VillageV31PlotProbe",false);try{Dump(world,(Transform)typeof(SeaLifeRuntime).GetField("world",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(g));EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(2);}}
 [Serializable] class Canopy {public string name;public Vector3 center,size;public bool readable;}
 [Serializable] class Site {public float x,z,lo,hi,frontDrop;public string[] blocked;}
 [Serializable] class Report {public string scope="Unsaved editor geometry diagnostic, v30 base terrain plus native colliders";public Canopy[] canopies,annexNearbyRenderers;public List<Site> sites=new List<Site>();public int clearGround,clearCanopy,annexVertices,annexTriangles,buriedDeckVertices;public Vector3 annexPosition,annexBoundsCenter,annexBoundsSize;public string[] annexSightBlockers;}
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaUnifiedV30.unity");
  var host=new GameObject("V31_PlotDiagnostic_NotSaved");var g=host.AddComponent<SeaLifeRuntime>();var w=GameObject.Find("EN_MistCapeShowcase").transform;
  var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;typeof(SeaLifeRuntime).GetField("world",flags).SetValue(g,w);
  host.AddComponent<SeaBayExpansionAndBuildSpaceV28>();var world=host.AddComponent<SeaSeamlessWorldAndNauticalUIV29>();world.PrepareWorld(g,new GameObject("V31_DiagnosticOnly").transform);Physics.SyncTransforms();
  Dump(world,w);
 }
 static void Dump(SeaSeamlessWorldAndNauticalUIV29 world,Transform w){
  var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;var type=world.GetType();var ground=type.GetMethod("V31Ground",flags);var shore=type.GetMethod("ShoreX",flags);
  Func<float,float,float> h=(x,z)=>(float)ground.Invoke(world,new object[]{x,z});
  var leaves=w.GetComponentsInChildren<MeshRenderer>().Where(r=>{string n=(r.name+" "+(r.transform.parent!=null?r.transform.parent.name:"")+" "+(r.sharedMaterial!=null?r.sharedMaterial.name:"")).ToLowerInvariant();return !n.Contains("substrate")&&(n.Contains("leaves")||n.Contains("leaf")||n.Contains("mapletree")||n.Contains("redmaple"));}).ToArray();
  var report=new Report();report.canopies=leaves.Select(r=>new Canopy{name=r.name+" / "+r.sharedMaterial.name,center=r.bounds.center,size=r.bounds.size,readable=r.GetComponent<MeshFilter>()!=null&&r.GetComponent<MeshFilter>().sharedMesh.isReadable}).ToArray();
  var mesh=Resources.Load<Mesh>("SeaVillageV31/Baked/Village_0_BoatwrightHouse");var rot=Quaternion.Euler(0,78,0);
  foreach(float dz in new[]{0f,-5,5,-10,10,-15,15,-20,20})foreach(float inland in new[]{7f,10,13,16,19,22,25,28,31}){
   float z=-28+dz,x=(float)shore.Invoke(world,new object[]{z})-inland,lo=float.PositiveInfinity,hi=float.NegativeInfinity;
   for(int a=-1;a<=1;a++)for(int b=-1;b<=1;b++){var q=new Vector3(x,0,z)+rot*new Vector3(a*3,0,b*3);float y=h(q.x,q.z);lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}
   var front=new Vector3(x,0,z)+rot*new Vector3(0,0,6);float drop=hi-h(front.x,front.z);var local=mesh.bounds;var wb=new Bounds(new Vector3(x,hi+2.4f,z),Vector3.zero);
   for(int c=0;c<8;c++)wb.Encapsulate(new Vector3(x,hi-.2f,z)+rot*new Vector3((c&1)==0?local.min.x:local.max.x,(c&2)==0?local.min.y:local.max.y,(c&4)==0?local.min.z:local.max.z));wb.Expand(new Vector3(1.2f,0,1.2f));
   var blockers=leaves.Where(r=>r.bounds.Intersects(wb)).Select(r=>r.name).ToArray();bool gentle=lo>=SeaLifeRuntime.Sea+.4f&&hi-lo<=1.05f&&drop<=1.3f;if(gentle){report.clearGround++;if(blockers.Length==0)report.clearCanopy++;}
   report.sites.Add(new Site{x=x,z=z,lo=lo,hi=hi,frontDrop=drop,blocked=blockers});
  }
  var annex=(Transform)type.GetField("boatyardAnnexV31",flags).GetValue(world);
  if(annex!=null){var m=annex.GetComponent<MeshFilter>().sharedMesh;report.annexPosition=annex.position;report.annexVertices=m.vertexCount;report.annexTriangles=m.triangles.Length/3;report.annexBoundsCenter=annex.GetComponent<Renderer>().bounds.center;report.annexBoundsSize=annex.GetComponent<Renderer>().bounds.size;var nearby=new Bounds(report.annexBoundsCenter,new Vector3(24,14,24));report.annexNearbyRenderers=UnityEngine.Object.FindObjectsOfType<MeshRenderer>().Where(r=>r.enabled&&r.bounds.Intersects(nearby)).Select(r=>new Canopy{name=r.name+" / "+r.sharedMaterial.name+" gameWorld="+r.transform.IsChildOf(w)+" parent="+r.transform.parent.name,center=r.bounds.center,size=r.bounds.size,readable=r.GetComponent<MeshFilter>()!=null&&r.GetComponent<MeshFilter>().sharedMesh.isReadable}).ToArray();var vs=m.vertices;var ns=m.normals;for(int i=0;i<vs.Length;i++){var wp=annex.TransformPoint(vs[i]);if(annex.TransformDirection(ns[i]).y>.8f&&wp.y<SeaLifeRuntime.Sea+1&&wp.y>SeaLifeRuntime.Sea+.25f&&wp.y<h(wp.x,wp.z))report.buriedDeckVertices++;}
   var target=annex.position+Quaternion.Euler(0,78,0)*new Vector3(0,0,4.4f);var direction=Quaternion.Euler(48,-32,0)*Vector3.forward;var vroot=(Transform)type.GetField("_v31Root",flags).GetValue(world);report.annexSightBlockers=Physics.RaycastAll(target-direction*30,direction,29.5f).Where(hit=>!hit.transform.IsChildOf(vroot)).Select(hit=>hit.transform.name).ToArray();}
  string dir=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Tools/SeaVillageV31/Diagnostics");Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,Application.isPlaying?(annex!=null?"plot-runtime-renderer-coverage.json":"plot-runtime.json"):"plot-editor.json"),JsonUtility.ToJson(report,true));Debug.Log("V31_PLOT_PROBE ground="+report.clearGround+" canopy="+report.clearCanopy+" renderers="+leaves.Length);
 }
}}
