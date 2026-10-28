using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Opt-in Windows visual check; separate save, no effect in normal play.</summary>
public sealed class MarinePlayerCapture : MonoBehaviour {
 static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-marine-check")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested)GameDirector.VerificationMode=true;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<MarinePlayerCapture>();}
 const string Dir="Verification/marine-life-v1";string failure="";
 void Awake(){Application.logMessageReceived+=Log;}
 void OnDestroy(){Application.logMessageReceived-=Log;Time.captureFramerate=0;}
 void Log(string c,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)failure=c;}
 void Capture(string name){
  var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"/"+name+".png",tex.EncodeToPNG());Destroy(tex);
 }
 IEnumerator Start(){
  Directory.CreateDirectory(Dir);Screen.SetResolution(1600,900,FullScreenMode.Windowed);
  yield return new WaitForSecondsRealtime(2);
  var g=GetComponent<GameDirector>();g.UI.Close();g.HarbourMenu=false;g.CameraFollow.enabled=false;
  foreach(var c in FindObjectsOfType<Canvas>())c.enabled=false;
  var wildlife=FindObjectOfType<Wildlife>();wildlife.AmbientFlights=false;Quaternion rot=Quaternion.Euler(55,0,0);
  g.Boat.Moor(new Vector3(18,0,-9),Quaternion.identity);
  g.Camera.transform.SetPositionAndRotation(new Vector3(18,0,-2)-rot*Vector3.forward*160,rot);g.Camera.orthographicSize=23;
  yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();Capture("school-gameplay");
  g.Camera.orthographicSize=11;
  yield return new WaitForEndOfFrame();Capture("school-close");
  int avoidance=wildlife.BoatAvoidanceCount;
  Vector3 near=wildlife.FishPosition(0);near.y=0;g.Boat.Moor(near,Quaternion.identity);
  yield return new WaitForSecondsRealtime(1.2f);
  bool reacts=wildlife.BoatAvoidanceCount>avoidance;
  yield return new WaitForEndOfFrame();Capture("school-scatter");
  float underwaterError=wildlife.MaximumUnderwaterError();
  g.Boat.Moor(new Vector3(25,0,-2),Quaternion.identity);
  g.Camera.transform.SetPositionAndRotation(new Vector3(34,0,10)-rot*Vector3.forward*160,rot);g.Camera.orthographicSize=18;
  if(wildlife.TryStartFlight(new Vector3(4,0,94),Vector3.right))failure="Flight path crossed the island";
  int before=wildlife.FlightCompletions;bool started=wildlife.TryStartFlight(new Vector3(24,0,9),Vector3.right);
  if(!started)failure="Clear-water flight failed to start";
  Time.captureFramerate=20;
  for(int frame=0;frame<130;frame++){
   yield return new WaitForEndOfFrame();
   Capture("video-"+frame.ToString("0000"));
   if(frame==26)Capture("flight-gameplay");
   if(frame==74)Capture("entry-ripples");
  }
  Time.captureFramerate=0;
  bool landed=wildlife.FlightCompletions-before==5;
  int impacts=wildlife.Effects.ImpactCount;
  yield return new WaitForSecondsRealtime(3.5f);
  if(!wildlife.ImportedMeshLoaded||wildlife.FishCount!=42)failure="Imported fish mesh / school count mismatch";
  if(!reacts)failure="Fish did not react to vessel";
  if(underwaterError>.5f)failure="Fish are not following the wave surface";
  if(!landed||impacts<10)failure="Flight entry / exit impacts did not complete";
  if(wildlife.MinimumFlightClearance<.05f)failure="Airborne fish clipped through the wave";
  if(wildlife.Effects.ActiveRipples!=0)failure="Ripples failed to expire";
  int controlledLandings=wildlife.FlightCompletions-before;
  int events=wildlife.FlightStarts;
  g.Boat.Moor(new Vector3(12,0,18),Quaternion.identity);
  g.Camera.transform.SetPositionAndRotation(new Vector3(14,0,31)-rot*Vector3.forward*160,rot);g.Camera.orthographicSize=23;
  wildlife.AmbientFlights=true;
  float deadline=Time.realtimeSinceStartup+10;
  while(wildlife.FlightStarts==events&&Time.realtimeSinceStartup<deadline)yield return null;
  bool ambient=wildlife.FlightStarts>events;
  yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();Capture("ambient-gameplay");
  if(!ambient)failure="Natural flight scheduler did not trigger";
  var result=new Result{passed=failure=="",detail=failure==""?"Imported model, 42 fish, vessel avoidance, five flights, ten impacts and wave alignment passed":failure,
   fishCount=wildlife.FishCount,flightLandings=controlledLandings,impacts=impacts,boatAvoidance=reacts,ambientFlight=ambient,
   underwaterOffsetMaximum=underwaterError,flightClearanceMinimum=wildlife.MinimumFlightClearance};
  File.WriteAllText(Dir+"/result.json",JsonUtility.ToJson(result,true));Debug.Log(failure==""?"MARINE_PLAYER_OK":"MARINE_PLAYER_FAILED "+failure);
  Application.Quit(failure==""?0:1);
 }
 [Serializable]class Result{public bool passed;public string detail;public int fishCount,flightLandings,impacts;public bool boatAvoidance,ambientFlight;public float underwaterOffsetMaximum,flightClearanceMinimum;}
}
}
