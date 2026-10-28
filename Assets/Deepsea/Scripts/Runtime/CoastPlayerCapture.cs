using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Opt-in Windows test. Normal launch does not activate this component.</summary>
public sealed class CoastPlayerCapture : MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-coast-check")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested)GameDirector.VerificationMode=true;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<CoastPlayerCapture>();}
 const string Dir="Verification/coast-v1";string failure="";GameDirector game;ShorelineAtmosphere coast;
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;Time.timeScale=1;Time.captureFramerate=0;}
 void Log(string c,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)failure+=c+"; ";}
 void Require(bool ok,string message){if(!ok)failure+=message+"; ";}
 Color32[] Capture(string name){
  var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",texture.EncodeToPNG());
  var pixels=texture.GetPixels32();Destroy(texture);return pixels;
 }
 void View(Vector3 p,float size){Quaternion rotation=Quaternion.Euler(55,0,0);game.Camera.transform.SetPositionAndRotation(p-rotation*Vector3.forward*160,rotation);game.Camera.orthographicSize=size;}
 int Difference(Color32[] a,Color32[] b){int count=0;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>14)count++;return count;}
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(2);
  game=GetComponent<GameDirector>();coast=FindObjectOfType<ShorelineAtmosphere>();
  if(coast==null||!coast.Ready){failure="Coast did not initialize";Finish(0,0,0,0);yield break;}
  float effectsBefore=game.Audio.EffectsVolume;game.Audio.EffectsVolume=.55f;
  game.UI.Close();game.HarbourMenu=false;game.CameraFollow.enabled=false;
  foreach(var canvas in FindObjectsOfType<Canvas>())canvas.enabled=false;
  foreach(string name in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/ShoreSwash"})Require(Shader.Find(name).isSupported,"Unsupported Windows shader "+name);
  Require(coast.RockCount==2,"Expected two existing reefs to be refined");
  Require(coast.ColliderCountBefore==coast.ColliderCountAfter,"Navigation collider count changed");
  Require(coast.AudioReady,"Missing ocean audio");
  var ecology=FindObjectOfType<CoastalEcology>();Require(ecology!=null&&ecology.Ready,"Existing coastal ecology did not initialize");
  if(ecology!=null)ecology.BirdVisitsEnabled=false;
  var wildlife=FindObjectOfType<Wildlife>();if(wildlife!=null)wildlife.AmbientFlights=false;
  game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);View(new Vector3(47,0,92),23);
  yield return new WaitForSecondsRealtime(.3f);Time.timeScale=0;coast.SetPreviewTime(ShoreCycle.Period*.15f);
  yield return new WaitForEndOfFrame();var incoming=Capture("normal-incoming");
  coast.SetPreviewTime(ShoreCycle.Period*.53f);yield return new WaitForEndOfFrame();var peak=Capture("normal-runup");
  coast.SetPreviewTime(ShoreCycle.Period*.82f);yield return new WaitForEndOfFrame();Capture("normal-retreat");
  int motion=Difference(incoming,peak);Require(motion>120,"Shore cycle unreadable at normal camera size");
  View(new Vector3(43,0,93),9);coast.SetPreviewTime(ShoreCycle.Period*.53f);
  yield return new WaitForEndOfFrame();Capture("swash-close");
  coast.SetPreviewTime(ShoreCycle.Period*.82f);yield return new WaitForEndOfFrame();Capture("wet-trail-close");
  View(new Vector3(64,0,89),11);yield return new WaitForEndOfFrame();Capture("reef-close");
  game.Boat.Moor(new Vector3(ShoreCycle.EastShoreX(93)+5,0,93),Quaternion.identity);
  coast.SetPreviewTime(ShoreCycle.Period*.47f);float near=coast.MaximumAudioVolume;
  game.Boat.Moor(new Vector3(110,0,93),Quaternion.identity);coast.SetPreviewTime(ShoreCycle.Period*.47f);float far=coast.MaximumAudioVolume;
  Require(near>.015f&&far<near*.2f,"Shore sound did not follow actual boat distance");
  game.Audio.EffectsVolume=0;game.Boat.Moor(new Vector3(47,0,93),Quaternion.identity);
  coast.SetPreviewTime(4);float muted=coast.MaximumAudioVolume;game.Audio.EffectsVolume=effectsBefore;
  Require(muted<.00001f,"Effects volume did not mute the added ocean sound");
  Time.timeScale=1;coast.SetPreviewTime(-1);
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coast-demo")>=0){
   game.Audio.EffectsVolume=.55f;game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);
   View(new Vector3(47,0,92),18);Time.captureFramerate=20;
   for(int frame=0;frame<180;frame++){yield return new WaitForEndOfFrame();Capture("video-"+frame.ToString("0000"));}
   Time.captureFramerate=0;
   // Record the actual Windows audio listener mix, not an offline gain simulation.
   var listener=FindObjectOfType<AudioListener>();var probe=listener.gameObject.AddComponent<CoastMixCapture>();
   float musicBefore=game.Audio.MusicVolume;game.Audio.MusicVolume=0;
   yield return new WaitForSecondsRealtime(.25f);probe.Begin(18);
   coast.SetPreviewTime(ShoreCycle.Period*.47f);game.Boat.Moor(new Vector3(ShoreCycle.EastShoreX(93)+5,0,93),Quaternion.identity);
   yield return new WaitForSecondsRealtime(6);
   game.Boat.Moor(new Vector3(110,0,93),Quaternion.identity);yield return new WaitForSecondsRealtime(6);
   game.Boat.Moor(new Vector3(47,0,93),Quaternion.identity);game.Audio.EffectsVolume=0;
   yield return new WaitForSecondsRealtime(6);
   probe.Save(Dir+"/windows-listener-mix.wav");Destroy(probe);
   game.Audio.MusicVolume=musicBefore;coast.SetPreviewTime(-1);
  }
  game.Audio.EffectsVolume=effectsBefore;Finish(motion,near,far,muted);
 }
 void Finish(int pixels,float near,float far,float muted){
  var result=new Result{passed=failure=="",detail=failure==""?"Windows shaders, readable shore cycle, two refined reefs, unchanged collider count, actual listener distance and audio mute passed":failure,
   shoreChangedPixels=pixels,nearVolume=near,farVolume=far,mutedVolume=muted,refinedRocks=coast==null?0:coast.RockCount};
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log(failure==""?"COAST_PLAYER_OK":"COAST_PLAYER_FAILED "+failure);Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed;public string detail;public int shoreChangedPixels,refinedRocks;public float nearVolume,farVolume,mutedVolume;}
}
}
