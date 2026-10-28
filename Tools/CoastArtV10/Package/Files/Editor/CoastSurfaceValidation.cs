using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Deepsea.Editor {
public static class CoastSurfaceValidation {
 [Serializable]sealed class Report{public bool passed;public int visualSurfaces,caveBodySamples,visualObstructions;public float affineFitError;public List<string> failures=new List<string>();}
 public static void Verify(){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var area=((GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("CoastBlockout/EastCoast"))).GetComponent<CoastBlockoutRegion>();
  var owner=new GameObject("Temporary surface verification");var look=owner.AddComponent<CoastMaterialsAppearance>();var surfaces=owner.AddComponent<CoastRockSurfaces>();surfaces.Initialize(area,look);
  var type=typeof(CoastRockSurfaces);var matrices=(Matrix4x4[])type.GetField("matrices",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(surfaces);var mesh=(Mesh)type.GetField("mesh",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(surfaces);
  var small=(Matrix4x4[])type.GetField("smallMatrices",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(surfaces);var low=(Mesh)type.GetField("lowMesh",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(surfaces);
  var colliders=new HashSet<Collider>();var report=new Report{visualSurfaces=surfaces.Surfaces+surfaces.SmallSurfaces,affineFitError=surfaces.FitError};
  try{
   for(int i=0;i<matrices.Length+small.Length;i++){
    var m=i<matrices.Length?matrices[i]:small[i-matrices.Length];var go=new GameObject("Temporary visual rock "+i);go.layer=2;go.transform.SetParent(owner.transform,false);Vector3 x=m.GetColumn(0),y=m.GetColumn(1),z=m.GetColumn(2);go.transform.position=m.GetColumn(3);go.transform.rotation=Quaternion.LookRotation(z.normalized,y.normalized);go.transform.localScale=new Vector3(x.magnitude,y.magnitude,z.magnitude);colliders.Add(go.AddComponent<MeshCollider>());go.GetComponent<MeshCollider>().sharedMesh=i<matrices.Length?mesh:low;
   }
   Physics.SyncTransforms();var buffer=new Collider[32];
   for(int step=0;step<160;step++)for(int fish=0;fish<CoastBlockoutSchool.Fish;fish++){
    Vector3 forward,p=CoastBlockoutSchool.FishPosition(step/160f,fish,out forward);report.caveBodySamples++;
    int count=Physics.OverlapSphereNonAlloc(p,.56f,buffer,1<<2,QueryTriggerInteraction.Ignore);
    for(int j=0;j<count;j++)if(colliders.Contains(buffer[j])){report.visualObstructions++;if(report.failures.Count<12)report.failures.Add("Fish body at "+p+" meets "+buffer[j].name);break;}
   }
   report.passed=surfaces.Surfaces==77&&surfaces.SmallSurfaces>=80&&report.affineFitError<.004f&&report.visualObstructions==0;
   Directory.CreateDirectory("Verification/coast-art-v10");File.WriteAllText("Verification/coast-art-v10/visual-surface-route.json",JsonUtility.ToJson(report,true));Debug.Log("COAST_VISUAL_SURFACE_ROUTE "+JsonUtility.ToJson(report));
   if(!report.passed)throw new Exception("Visual surface route verification failed");
  }finally{UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(area.gameObject);}
 }
}
}
