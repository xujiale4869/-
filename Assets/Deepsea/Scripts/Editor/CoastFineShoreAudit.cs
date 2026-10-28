using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Deepsea.Editor {
 public static class CoastFineShoreAudit {
  const string OldScene="Assets/Deepsea/Scenes/CoastArtMaple.unity",NewScene="Assets/Deepsea/Scenes/CoastArtFineShore.unity";
  [Serializable]class Report {
   public bool passed;
   public string scope="Read-only authored-scene comparison. No Play Mode, no scene saving. Checks references and local object state; old asset content hashes require the separate file preservation audit.";
   public int oldObjects,newObjects,checkedObjects,checkedMeshes,checkedMaterialSlots,checkedRenderers,checkedLeafTargets,checkedPreservedAnchors;
   public int allowedRemovedRocks,allowedChangedTreeMeshes,allowedChangedWaterMaterialSlots,newSectionObjects;
   public string[] failures;
  }
  static GameObject Root(Scene scene){foreach(var go in scene.GetRootGameObjects())if(go.name=="EN_MistCapeShowcase")return go;throw new Exception("Missing authored environment root in "+scene.path);}
  static void Collect(Transform t,string path,Dictionary<string,Transform> result,List<string> failures){if(result.ContainsKey(path)){failures.Add("Duplicate hierarchy path: "+path);return;}result.Add(path,t);foreach(Transform child in t)Collect(child,path.Length==0?child.name:path+"/"+child.name,result,failures);}
  static bool RemovedRock(string path){return path=="EN_MapleHeadland03_LOD0"||path=="EN_MapleFoot03_LOD0";}
  static bool ChangedTree(string path){return path=="EN_RedMaple00_Bark_LOD0"||path=="EN_RedMaple00_Leaves_LOD0";}
  static bool NewSection(string path){return path=="EN_FineShoreSection"||path.StartsWith("EN_FineShoreSection/",StringComparison.Ordinal);}
  static bool SameVector(Vector3 a,Vector3 b){return a.x.Equals(b.x)&&a.y.Equals(b.y)&&a.z.Equals(b.z);}
  static bool SameRotation(Quaternion a,Quaternion b){return a.x.Equals(b.x)&&a.y.Equals(b.y)&&a.z.Equals(b.z)&&a.w.Equals(b.w);}
  static void Require(bool ok,string message,List<string> failures){if(!ok)failures.Add(message);}
  [MenuItem("深海灯塔/美术样板/只读检查v20保留范围")]
  public static void Run(){
   var report=new Report();var failures=new List<string>();
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Preservation audit must run outside Play Mode");
   try {
    var oldScene=EditorSceneManager.OpenScene(OldScene,OpenSceneMode.Single);
    var newScene=EditorSceneManager.OpenScene(NewScene,OpenSceneMode.Additive);
    var oldRoot=Root(oldScene);var newRoot=Root(newScene);
    var before=new Dictionary<string,Transform>();var after=new Dictionary<string,Transform>();Collect(oldRoot.transform,"",before,failures);Collect(newRoot.transform,"",after,failures);report.oldObjects=before.Count;report.newObjects=after.Count;
    foreach(var entry in before){
     string path=entry.Key;var a=entry.Value;
     if(RemovedRock(path)){report.allowedRemovedRocks++;Require(!after.ContainsKey(path),"Old replacement rock still present: "+path,failures);continue;}
     if(!after.TryGetValue(path,out var b)){failures.Add("Missing old object: "+path);continue;}
     report.checkedObjects++;
     Require(SameVector(a.localPosition,b.localPosition),"Local position changed: "+path,failures);Require(SameRotation(a.localRotation,b.localRotation),"Local rotation changed: "+path,failures);Require(SameVector(a.localScale,b.localScale),"Local scale changed: "+path,failures);Require(a.gameObject.activeSelf==b.gameObject.activeSelf,"activeSelf changed: "+path,failures);
     var af=a.GetComponent<MeshFilter>();var bf=b.GetComponent<MeshFilter>();
     Require((af==null)==(bf==null),"MeshFilter presence changed: "+path,failures);
     if(af!=null&&bf!=null){report.checkedMeshes++;if(ChangedTree(path)){report.allowedChangedTreeMeshes++;Require(bf.sharedMesh!=null,"Tree replacement mesh missing: "+path,failures);}else Require(af.sharedMesh==bf.sharedMesh,"Mesh reference changed: "+path,failures);}
     var ar=a.GetComponent<MeshRenderer>();var br=b.GetComponent<MeshRenderer>();
     Require((ar==null)==(br==null),"MeshRenderer presence changed: "+path,failures);
     if(ar!=null&&br!=null){report.checkedRenderers++;Require(ar.enabled==br.enabled,"Renderer enabled flag changed: "+path,failures);Require(ar.shadowCastingMode==br.shadowCastingMode,"Shadow casting changed: "+path,failures);Require(ar.receiveShadows==br.receiveShadows,"Receive shadows changed: "+path,failures);Require(ar.lightProbeUsage==br.lightProbeUsage,"Light probe usage changed: "+path,failures);Require(ar.reflectionProbeUsage==br.reflectionProbeUsage,"Reflection probe usage changed: "+path,failures);Require(ar.renderingLayerMask==br.renderingLayerMask,"Rendering layer mask changed: "+path,failures);
      var am=ar.sharedMaterials;var bm=br.sharedMaterials;Require(am.Length==bm.Length,"Material slot count changed: "+path,failures);
      for(int i=0;i<Math.Min(am.Length,bm.Length);i++){report.checkedMaterialSlots++;if(path=="ShowcaseWater"){report.allowedChangedWaterMaterialSlots++;Require(bm[i]!=null,"Water replacement material missing",failures);}else Require(am[i]==bm[i],"Material reference changed: "+path+" slot "+i,failures);}
     }
    }
    foreach(var entry in after)if(!before.ContainsKey(entry.Key)){if(NewSection(entry.Key))report.newSectionObjects++;else failures.Add("Unexpected new environment object: "+entry.Key);}
    Require(report.allowedRemovedRocks==2,"Expected two original replacement rocks",failures);Require(report.allowedChangedTreeMeshes==2,"Expected exactly two allowed Tree00 meshes",failures);Require(report.newSectionObjects>1,"New shoreline section or mesh child missing",failures);
    var oldLeaves=oldRoot.GetComponent<CoastMapleLeaves>();var newLeaves=newRoot.GetComponent<CoastMapleLeaves>();Require(oldLeaves!=null&&newLeaves!=null,"Leaf motion component missing",failures);
    if(oldLeaves!=null&&newLeaves!=null){
     Require(oldLeaves.targets!=null&&newLeaves.targets!=null,"Null leaf targets",failures);
     if(oldLeaves.targets!=null&&newLeaves.targets!=null){Require(oldLeaves.targets.Length==newLeaves.targets.Length,"Leaf target count changed",failures);for(int i=0;i<Math.Min(oldLeaves.targets.Length,newLeaves.targets.Length);i++){report.checkedLeafTargets++;Require(SameVector(oldLeaves.targets[i],newLeaves.targets[i]),"Leaf water target changed: "+i,failures);}}
     Require(oldLeaves.anchors!=null&&newLeaves.anchors!=null,"Null canopy anchors",failures);
     if(oldLeaves.anchors!=null&&newLeaves.anchors!=null){Require(oldLeaves.anchors.Length==newLeaves.anchors.Length,"Canopy anchor count changed",failures);for(int i=1;i<Math.Min(oldLeaves.anchors.Length,newLeaves.anchors.Length);i++){report.checkedPreservedAnchors++;Require(SameVector(oldLeaves.anchors[i],newLeaves.anchors[i]),"Unselected canopy anchor changed: "+i,failures);}}
    }
   }
   catch(Exception ex){failures.Add(ex.GetType().Name+": "+ex.Message);}
   report.failures=failures.ToArray();report.passed=failures.Count==0;const string folder="Verification/coast-art-v20-build";Directory.CreateDirectory(folder);File.WriteAllText(folder+"/preservation-audit.json",JsonUtility.ToJson(report,true));
   if(!report.passed)throw new Exception("v20 preservation audit failed: "+string.Join("; ",report.failures));Debug.Log("COAST_FINE_SHORE_PRESERVATION_OK "+JsonUtility.ToJson(report));
  }
 }
}
