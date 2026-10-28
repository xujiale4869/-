using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Deepsea.Editor
{
    public static class UIWorkCheck
    {
        public static void Run()
        {
            ProjectSetup.EnsureProject();EditorSceneManager.OpenScene(ProjectSetup.MainScene);SessionState.SetBool("UIWorkCheck",true);GameDirector.VerificationMode=true;
            EditorApplication.playModeStateChanged+=Hook;EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] static void Restore(){if(SessionState.GetBool("UIWorkCheck",false)){GameDirector.VerificationMode=true;EditorApplication.playModeStateChanged+=Hook;}}
        static void Hook(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode){var g=Object.FindObjectOfType<GameDirector>();g.gameObject.AddComponent<UIWorkSmoke>().Complete=(ok,detail)=>{SessionState.SetBool("UIWorkCheck",false);EditorApplication.Exit(ok?0:1);};}}
    }
}
