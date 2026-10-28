#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
public sealed class WaterSmoke : MonoBehaviour {
 public Action<bool,string> Complete;public string Label="shallow-v2";
 string failure="";float motionDifference,brightness;GameDirector g;RenderTexture rt;
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;}
 void Log(string c,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure=c;}
 void View(Vector3 focus,float size){
  Quaternion rot=Quaternion.Euler(55,0,0);g.Camera.transform.SetPositionAndRotation(focus-rot*Vector3.forward*160,rot);g.Camera.orthographicSize=size;
 }
 Color32[] Capture(string name){
  var old=RenderTexture.active;RenderTexture.active=rt;
  var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
  File.WriteAllBytes("Verification/water-"+Label+"/"+name+".png",tex.EncodeToPNG());
  var pixels=tex.GetPixels32();Destroy(tex);RenderTexture.active=old;return pixels;
 }
 IEnumerator Start(){
  g=GetComponent<GameDirector>();yield return new WaitForSeconds(2);
  g.UI.Close();g.HarbourMenu=false;Time.timeScale=1;foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;
  g.CameraFollow.enabled=false;rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);rt.Create();g.Camera.targetTexture=rt;
  Directory.CreateDirectory("Verification/water-"+Label);
  Vector3[] points={new Vector3(5,0,-29),new Vector3(8,0,94),new Vector3(44,0,94),new Vector3(11,0,-22)};
  float[] sizes={30,55,18,8};string[] names={"harbour","island-wide","island-edge","boat-close"};
  for(int i=0;i<points.Length;i++){View(points[i],sizes[i]);yield return new WaitForSeconds(1);Capture(names[i]);}
  View(new Vector3(18,0,8),18);g.Boat.Recover(new Vector3(11,0,-2));g.Boat.AutomationInput=true;g.Boat.AutomationThrottle=1;
  yield return new WaitForSeconds(3);Capture("sailing");
  if(g.Boat.Speed<1.5f)failure="Boat propulsion failed";g.Boat.AutomationInput=false;g.Boat.Service();
  // Freeze camera and boat; only water and lighting move.
  g.Boat.Moor(new Vector3(11,0,-2),Quaternion.identity);View(new Vector3(18,0,8),18);
  yield return new WaitForSeconds(1);var first=Capture("motion-00");
  for(int frame=1;frame<=8;frame++){yield return new WaitForSeconds(.25f);var current=Capture("motion-"+frame.ToString("00"));
   if(frame==8){double delta=0,light=0;int count=0;
    for(int y=130;y<420;y+=3)for(int x=110;x<530;x+=3){int idx=y*1600+x;Color32 a=first[idx],b=current[idx];
     delta+=(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/(255.0*3);
     light+=(b.r*.2126+b.g*.7152+b.b*.0722)/255.0;count++;}
    motionDifference=(float)(delta/count);brightness=(float)(light/count);
   }
  }
  var water=OceanSurface.Active;
  if(water==null||!water.BathymetryReady||water.ReflectionRenderCount<3)failure="Water/seabed/reflector not ready";
  if(motionDifference<.002f)failure="Water movement not visible in fixed-camera captures";
  if(brightness<.35f)failure="Bay water remains too dark";
  float expected=OceanSurface.Height(g.Boat.transform.position,Time.time)-OceanSurface.SeaLevel;
  if(Mathf.Abs(g.Boat.hull.localPosition.y-expected)>.08f)failure="Hull wave tracking failed";
  foreach(var msg in UnityEditor.ShaderUtil.GetShaderMessages(Shader.Find("Deepsea/Ocean")))
   if(msg.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)failure=msg.message;
  g.Camera.targetTexture=null;rt.Release();Destroy(rt);
  File.WriteAllText("Verification/water-"+Label+"/result.json",JsonUtility.ToJson(new Result{passed=failure=="",detail=failure==""?"Rendered harbor/island/boat, visible time-varying water, brightness, seabed and propulsion passed":failure,motionDifference=motionDifference,waterBrightness=brightness},true));
  Debug.Log(failure==""?"WATER_RENDER_OK":"WATER_RENDER_FAILED "+failure);Complete(failure=="",failure);
 }
 [Serializable]class Result{public bool passed;public string detail;public float motionDifference,waterBrightness;}
}
}
#endif
