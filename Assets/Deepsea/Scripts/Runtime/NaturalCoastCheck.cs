using System;using System.IO;using System.Collections;using UnityEngine;
namespace Deepsea {
public sealed class NaturalCoastCheck:MonoBehaviour {
 static bool Requested{get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-natural-check")>=0;}}static bool Baseline{get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-natural-baseline")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested){NaturalCoast.Enabled=!Baseline;GameDirector.VerificationMode=true;Time.captureFramerate=20;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<NaturalCoastCheck>();}
 GameDirector game;string failure="";string Dir{get{return "Verification/natural-coast-v1/"+(Baseline?"before":"after");}}int frames;
 void Awake(){Application.logMessageReceived+=Log;}void OnDestroy(){Application.logMessageReceived-=Log;Time.captureFramerate=0;Time.timeScale=1;}void Log(string s,string st,LogType t){if(t==LogType.Exception||t==LogType.Error||t==LogType.Assert)failure+=s+"; ";}
 void Require(bool ok,string s){if(!ok)failure+=s+"; ";}void View(Vector3 p,float size){var r=Quaternion.Euler(55,0,0);p.y=0;game.Camera.transform.SetPositionAndRotation(p-r*Vector3.forward*160,r);game.Camera.orthographicSize=size;}
 void Capture(string s){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+s+".png",t.EncodeToPNG());Destroy(t);}
 IEnumerator Start(){Directory.CreateDirectory(Dir);Screen.SetResolution(1280,720,FullScreenMode.Windowed);while(Time.frameCount<8||OceanSurface.Active==null||!OceanSurface.Active.BathymetryReady)yield return new WaitForEndOfFrame();game=GetComponent<GameDirector>();game.SetPause(false);game.UI.Close();game.HarbourMenu=false;game.CameraFollow.enabled=false;foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;
  game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);View(new Vector3(54,0,91),23);FindObjectOfType<ShorelineAtmosphere>().SetPreviewTime(ShoreCycle.Period*.53f);while(Time.frameCount<50)yield return new WaitForEndOfFrame();
  var art=FindObjectOfType<NaturalCoast>();Require(art!=null&&art.Ready,"Scenery missing");if(!Baseline){Require(art.Trees>=3&&art.Kelp>=15,"Scenery too sparse");Require(art.Triangles<300000,"Scenery budget exceeded");}Require(game.World.Root.GetComponentsInChildren<Collider>().Length==44,"Unexpected navigation colliders");
  foreach(var q in NaturalCoast.Route){float h=IslandTerrain.Height(q.x,q.y);Require(!float.IsNaN(h)&&NaturalCoast.LandRelief(q.x,q.y)<.01f,"Route height modified");RaycastHit hit;Require(Physics.Raycast(new Vector3(q.x,h+6,q.y),Vector3.down,out hit,12),"Route lacks physical ground");}
  var fish=new System.Collections.Generic.List<Renderer>();foreach(var r in game.World.Root.GetComponentsInChildren<Renderer>()){if(r.sharedMaterial==null||r.sharedMaterial.shader==null)continue;string n=r.sharedMaterial.shader.name;if(n.Contains("Fish")||n.Contains("Wildlife")||n.Contains("Seabird")){if(r.enabled){fish.Add(r);r.enabled=false;}}}
  Time.timeScale=0;yield return new WaitForEndOfFrame();Capture("no-fish");View(new Vector3(29,0,96),30);yield return new WaitForEndOfFrame();Capture("island-no-fish");View(new Vector3(76,0,94),20);yield return new WaitForEndOfFrame();Capture("reef-no-fish");foreach(var r in fish)r.enabled=true;View(new Vector3(54,0,91),23);yield return new WaitForEndOfFrame();Capture("normal");Time.timeScale=1;
  if(!Baseline){FindObjectOfType<ShorelineAtmosphere>().SetPreviewTime(-1);for(int i=0;i<200;i++){yield return new WaitForEndOfFrame();Capture("video-"+(frames++).ToString("0000"));}}
  var result=new Result{passed=failure=="",detail=failure==""?"Fish-hidden scenery view, route surface, collider budget and motion capture passed":failure,trees=art.Trees,kelp=art.Kelp,triangles=art.Triangles,videoFrames=frames};File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log("NATURAL_COAST_CHECK "+JsonUtility.ToJson(result));Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed;public string detail;public int trees,kelp,triangles,videoFrames;}
}
}
