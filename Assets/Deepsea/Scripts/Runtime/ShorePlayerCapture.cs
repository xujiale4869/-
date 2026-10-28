using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
public sealed class ShorePlayerCapture : MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-shore-check")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested)GameDirector.VerificationMode=true;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<ShorePlayerCapture>();}
 const string Dir="Verification/shore-v1";string failure="";GameDirector game;
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;Time.captureFramerate=0;Time.timeScale=1;}
 void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure=message;}
 Color32[] Capture(string name){
  var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",texture.EncodeToPNG());
  var pixels=texture.GetPixels32();Destroy(texture);return pixels;
 }
 void View(Vector3 focus,float size){
  Quaternion r=Quaternion.Euler(55,0,0);game.Camera.transform.SetPositionAndRotation(focus-r*Vector3.forward*160,r);game.Camera.orthographicSize=size;
 }
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1600,900,FullScreenMode.Windowed);
  yield return new WaitForSecondsRealtime(2);
  game=GetComponent<GameDirector>();game.UI.Close();game.HarbourMenu=false;game.CameraFollow.enabled=false;
  foreach(string name in new[]{"Deepsea/CoastalGround","Deepsea/Seagrass","Deepsea/Ocean"})if(!Shader.Find(name).isSupported)failure="Unsupported standalone shader: "+name;
  foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;
  FindObjectOfType<Wildlife>().AmbientFlights=false;
  game.Boat.Moor(new Vector3(11,0,-22),Quaternion.identity);
  Vector3[] focus={new Vector3(5,0,-29),new Vector3(8,0,94),new Vector3(44,0,94),new Vector3(11,0,-22)};
  float[] sizes={30,55,18,8};string[] names={"harbour","island-wide","island-edge","boat-close"};
  for(int i=0;i<focus.Length;i++){View(focus[i],sizes[i]);yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();Capture(names[i]);}
  var habitat=FindObjectOfType<ShoreHabitat>();var ocean=OceanSurface.Active;
  if(habitat==null||habitat.TuftCount<8||habitat.RockCount<5)failure="Shallow habitat did not populate";
  if(habitat.HighestBladeTip>OceanSurface.SeaLevel-.30f)failure="Seagrass rises above the surface";
  var decor=GameObject.Find("东岸浅滩 · 仅视觉无碰撞");
  if(decor==null||decor.GetComponentsInChildren<Collider>().Length!=0)failure="Decoration changed collision/navigation";
  View(new Vector3(50,0,94),15);
  yield return new WaitForSecondsRealtime(.5f);Time.timeScale=0;
  var grass=GameObject.Find("随水流摆动的海草").GetComponent<Renderer>();
  var stones=GameObject.Find("水下岩石 · 无碰撞").GetComponent<Renderer>();
  grass.enabled=stones.enabled=false;yield return new WaitForEndOfFrame();var absent=Capture("detail-without-decor");
  grass.enabled=stones.enabled=true;yield return new WaitForEndOfFrame();var present=Capture("detail");
  int visiblePixels=0;double delta=0;
  for(int i=0;i<present.Length;i++){
   int d=Mathf.Abs(present[i].r-absent[i].r)+Mathf.Abs(present[i].g-absent[i].g)+Mathf.Abs(present[i].b-absent[i].b);
   if(d>8)visiblePixels++;delta+=d/(255.0*3);
  }
  if(visiblePixels<100)failure="Underwater rocks and vegetation are not visible through the water";
  Time.timeScale=1;game.Boat.Moor(new Vector3(49,0,89),Quaternion.identity);
  View(new Vector3(48,0,94),18);
  yield return new WaitForSecondsRealtime(.5f);Time.captureFramerate=20;
  Color32[] first=null,last=null;
  for(int frame=0;frame<160;frame++){
   yield return new WaitForEndOfFrame();var pixels=Capture("video-"+frame.ToString("0000"));
   if(frame==0)first=pixels;if(frame==120)last=pixels;
  }
  Time.captureFramerate=0;
  double movement=0,light=0;int count=0;
  for(int y=Screen.height/5;y<Screen.height*4/5;y+=3)for(int x=Screen.width/2;x<Screen.width*4/5;x+=3){
   int i=y*Screen.width+x;var a=first[i];var b=last[i];
   movement+=(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/(255.0*3);
   light+=(b.r*.2126+b.g*.7152+b.b*.0722)/255.0;count++;
  }
  if(movement/count<.002)failure="Fixed-camera water/shore remains static";
  if(light/count<.34)failure="Shallow water remains too dark";
  if(ocean==null||!ocean.BathymetryReady||ocean.ReflectionRenderCount<3)failure="Water/depth/reflection failed";
  var result=new Result{passed=failure=="",detail=failure==""?"Windows shore rendering, visible submerged habitat, non-colliding decor, submerged blades, water brightness and motion passed":failure,
   tufts=habitat.TuftCount,blades=habitat.BladeCount,rocks=habitat.RockCount,underwaterVisiblePixels=visiblePixels,
   underwaterDifference=(float)(delta/present.Length),waterBrightness=(float)(light/count),motionDifference=(float)(movement/count)};
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log(failure==""?"SHORE_PLAYER_OK":"SHORE_PLAYER_FAILED "+failure);
  Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed;public string detail;public int tufts,blades,rocks,underwaterVisiblePixels;public float underwaterDifference,waterBrightness,motionDifference;}
}
}
