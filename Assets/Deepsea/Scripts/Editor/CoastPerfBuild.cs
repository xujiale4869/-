using System;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;
namespace Deepsea.Editor {public static class CoastPerfBuild {
 public static void Build(){PlayerSettings.enableFrameTimingStats=true;AssetDatabase.Refresh();var opts=new BuildPlayerOptions{scenes=new[]{ProjectSetup.MainScene},locationPathName="Builds/CoastSliceV2/GuichaoBay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};var result=BuildPipeline.BuildPlayer(opts);if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Performance build failed");Debug.Log("COAST_PERF_BUILD_OK");}
}}
