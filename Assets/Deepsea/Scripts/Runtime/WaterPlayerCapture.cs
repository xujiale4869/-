using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Explicit opt-in Windows rendering capture; never runs during normal play.</summary>
public sealed class WaterPlayerCapture : MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-water-visual-check")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested)GameDirector.VerificationMode=true;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<WaterPlayerCapture>();}
 const string Dir="Verification/water-player-shallow-v3";string failure="";
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;}
 void Log(string c,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure=c;}
 Color32[] Capture(string name){
  Texture2D tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",tex.EncodeToPNG());
  Color32[] pixels=tex.GetPixels32();Destroy(tex);return pixels;
 }
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1600,900,FullScreenMode.Windowed);
  yield return new WaitForSecondsRealtime(2);
  var g=GetComponent<GameDirector>();g.UI.Close();g.HarbourMenu=false;Time.timeScale=1;
  foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;
  g.CameraFollow.enabled=false;g.Boat.Moor(new Vector3(11,0,-22),Quaternion.identity);
  Vector3[] focus={new Vector3(5,0,-29),new Vector3(8,0,94),new Vector3(44,0,94),new Vector3(11,0,-22)};
  float[] size={30,55,18,8};string[] name={"harbour","island-wide","island-edge","boat-close"};
  Quaternion rot=Quaternion.Euler(55,0,0);
  for(int i=0;i<focus.Length;i++){
   g.Camera.transform.SetPositionAndRotation(focus[i]-rot*Vector3.forward*160,rot);g.Camera.orthographicSize=size[i];
   yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();Capture(name[i]);
  }
  g.Camera.transform.SetPositionAndRotation(new Vector3(18,0,8)-rot*Vector3.forward*160,rot);g.Camera.orthographicSize=18;
  yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();Color32[] first=Capture("motion-00");
  double diff=0,brightness=0;int count=0;
  for(int i=1;i<=8;i++){
   yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();var frame=Capture("motion-"+i.ToString("00"));
   if(i==8)for(int y=Screen.height/5;y<Screen.height*2/5;y+=3)for(int x=Screen.width/12;x<Screen.width/3;x+=3){
    int idx=y*Screen.width+x;Color32 a=first[idx],b=frame[idx];
    diff+=(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/(255.0*3);
    brightness+=(b.r*.2126+b.g*.7152+b.b*.0722)/255.0;count++;
   }
  }
  if(diff/count<.002)failure="Stationary water did not animate";
  if(brightness/count<.35)failure="Standalone water remains too dark";
  if(OceanSurface.Active==null||!OceanSurface.Active.BathymetryReady)failure="Missing continuous seabed";
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(new Result{passed=failure=="",detail=failure==""?"Windows swapchain renders, shoreline/boat captures and stationary water motion passed":failure,width=Screen.width,height=Screen.height,motionDifference=(float)(diff/count),waterBrightness=(float)(brightness/count)},true));
  if(failure==""&&Array.IndexOf(Environment.GetCommandLineArgs(),"-water-video")>=0){
   g.Boat.Moor(new Vector3(11,0,-22),Quaternion.identity);
   g.Camera.transform.SetPositionAndRotation(new Vector3(11,0,-22)-rot*Vector3.forward*160,rot);g.Camera.orthographicSize=15;
   Time.captureFramerate=24;
   for(int frame=0;frame<72;frame++){yield return new WaitForEndOfFrame();Capture("video-"+frame.ToString("0000"));}
   Time.captureFramerate=0;
  }
  Debug.Log(failure==""?"WATER_PLAYER_OK":"WATER_PLAYER_FAILED "+failure);Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed;public string detail;public int width,height;public float motionDifference,waterBrightness;}
}
}
