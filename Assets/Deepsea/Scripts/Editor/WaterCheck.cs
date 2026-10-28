using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Deepsea.Editor {
public static class WaterCheck {
 public static void Run(){
  ProjectSetup.EnsureProject();EditorSceneManager.OpenScene(ProjectSetup.MainScene);
  SessionState.SetBool("WaterCheck",true);SessionState.SetString("WaterLabel",Environment.GetCommandLineArgs().Contains("-water-baseline")?"before":"shallow-v2");
  GameDirector.VerificationMode=true;EditorApplication.playModeStateChanged+=Hook;EditorApplication.isPlaying=true;
 }
 [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("WaterCheck",false)){GameDirector.VerificationMode=true;EditorApplication.playModeStateChanged+=Hook;}}
 static void Hook(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode)return;
  var g=UnityEngine.Object.FindObjectOfType<GameDirector>();var s=g.gameObject.AddComponent<WaterSmoke>();s.Label=SessionState.GetString("WaterLabel","after");
  s.Complete=(ok,detail)=>{SessionState.SetBool("WaterCheck",false);EditorApplication.Exit(ok?0:1);};
 }
 public static void Build(){
  ProjectSetup.EnsureProject();
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ProjectSetup.MainScene},locationPathName="Builds/ShallowWaterV3/GuichaoBay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Water build failed");
  Debug.Log("WATER_BUILD_OK");
 }
}
}
