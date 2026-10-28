using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deepsea.Editor
{
    public static class ExpeditionPlayCheck
    {
        public static void Run()
        {SessionState.SetBool("ExpeditionTerrainOnly",false);SessionState.SetBool("ExpeditionBoatOnly",false);Begin();}
        public static void RunTerrain()
        {SessionState.SetBool("ExpeditionTerrainOnly",true);SessionState.SetBool("ExpeditionBoatOnly",false);Begin();}
        public static void RunBoat()
        {SessionState.SetBool("ExpeditionTerrainOnly",false);SessionState.SetBool("ExpeditionBoatOnly",true);Begin();}
        static void Begin()
        {
            ProjectSetup.EnsureProject();Verification.RunRules();ExpeditionVerification.RunRules();
            EditorSceneManager.OpenScene(ProjectSetup.MainScene);
            SessionState.SetBool("ExpeditionPlayCheck",true);
            SessionState.SetFloat("ExpeditionDeadline",(float)EditorApplication.timeSinceStartup+300);
            GameDirector.VerificationMode=true;
            EditorApplication.playModeStateChanged-=Hook;EditorApplication.playModeStateChanged+=Hook;
            EditorApplication.update-=Watch;EditorApplication.update+=Watch;
            EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod]
        static void Restore()
        {
            if(!SessionState.GetBool("ExpeditionPlayCheck",false))return;
            GameDirector.VerificationMode=true;
            EditorApplication.playModeStateChanged-=Hook;EditorApplication.playModeStateChanged+=Hook;
            EditorApplication.update-=Watch;EditorApplication.update+=Watch;
        }
        static void Hook(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            GameDirector game=Object.FindObjectOfType<GameDirector>();
            var smoke=game.gameObject.AddComponent<ExpeditionSmoke>();
            smoke.TerrainOnly=SessionState.GetBool("ExpeditionTerrainOnly",false);
            smoke.BoatOnly=SessionState.GetBool("ExpeditionBoatOnly",false);smoke.Complete=Finish;
        }
        static void Watch(){if(SessionState.GetBool("ExpeditionPlayCheck",false)&&EditorApplication.timeSinceStartup>SessionState.GetFloat("ExpeditionDeadline",float.MaxValue))Finish(false,"Timed out");}
        static void Finish(bool ok,string detail)
        {
            SessionState.SetBool("ExpeditionPlayCheck",false);
            Directory.CreateDirectory("Verification");
            File.WriteAllText("Verification/unity-expedition-result.json","{\"unityVersion\":\""+Application.unityVersion+"\",\"passed\":"+(ok?"true":"false")+",\"detail\":\""+detail.Replace("\"","'").Replace("\n"," ")+"\"}");
            Debug.Log(ok?"DEEPSEA_EXPEDITION_PLAY_OK":"DEEPSEA_EXPEDITION_PLAY_FAILED "+detail);
            EditorApplication.Exit(ok?0:1);
        }
    }
}
