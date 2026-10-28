using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deepsea.Editor
{
    public static class Verification
    {
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Verification: "+message);}
        public static void RunRules()
        {
            var harbour=new HarbourProgress();var trip=new Expedition();
            Check(trip.TryLoad("crate-a",6,1,10),"load materials");
            Check(!trip.TryLoad("crate-a",6,1,10),"duplicate collection blocked");
            Check(!trip.TryLoad("oversize",4,0,10),"cargo capacity enforced");
            Check(trip.TryRescue("rescue-a",4,4),"rescue capacity independent from cargo");
            Check(!trip.TryRescue("rescue-b",1,4),"passenger capacity enforced");
            Check(harbour.parts==0&&harbour.residents==5,"uncommitted expedition does not alter harbour");
            trip.Commit(harbour);trip.Commit(harbour);
            Check(harbour.parts==6&&harbour.equipment==1&&harbour.residents==9&&harbour.voyages==1,"docking commits exactly once");
            Check(Economy.TryBuild(harbour,Building.Winch,6,1,0),"winch can be purchased");
            Check(!Economy.TryBuild(harbour,Building.Winch,0,0,0),"building cannot be purchased twice");
            Check(!Economy.TryBuild(harbour,Building.Charts,4,0,1),"construction blocked without materials");
            Check(Economy.TowSpeedMultiplier(true,true)>Economy.TowSpeedMultiplier(false,true),"winch improves actual towing");
            var repairHarbour=new HarbourProgress{repairDebt=2};
            var repairTrip=new Expedition();
            repairTrip.TryLoad("repair-crate",3,0,10);repairTrip.parts-=2;repairTrip.fieldRepairCredit=2;
            repairTrip.Commit(repairHarbour);
            Check(repairHarbour.parts==1&&repairHarbour.repairDebt==0,"field repair consumes exactly once at settlement");
            var afterLoss=new HarbourProgress{repairDebt=2};var laterTrip=new Expedition();
            laterTrip.TryLoad("later-crate",3,0,10);laterTrip.Commit(afterLoss);
            Check(afterLoss.parts==1&&afterLoss.repairDebt==0,"persisted field repair cannot refund consumed cargo after loss");
            string path=Path.Combine(Application.temporaryCachePath,"deepsea-rules-"+Guid.NewGuid()+".json");
            var save=new SaveStore(path);
            Check(save.Save(harbour),"first save");
            harbour.parts=3;Check(save.Save(harbour),"atomic replacement");
            Check(save.Load().winch&&save.Load().parts==3,"construction survives reload");
            File.WriteAllText(path,"malformed");
            Check(save.Load().winch&&save.Load().parts==0,"backup recovers malformed latest save");
            File.Delete(path);File.Delete(path+".bak");
            Debug.Log("DEEPSEA_RULES_OK");
        }

        [MenuItem("深海灯塔/检查规则")]
        public static void RulesMenu(){RunRules();}

        // Called without -quit. Unity must enter Play Mode and render real frames.
        public static void PlaySmoke()
        {
            ProjectSetup.EnsureProject();RunRules();
            EditorSceneManager.OpenScene(ProjectSetup.MainScene);
            SessionState.SetBool("DeepseaSmoke",true);
            SessionState.SetFloat("DeepseaSmokeDeadline",(float)EditorApplication.timeSinceStartup+120);
            EditorApplication.update+=WatchSmoke;
            GameDirector.VerificationMode=true;
            EditorApplication.playModeStateChanged+=OnPlayState;
            EditorApplication.isPlaying=true;
        }
        static void OnPlayState(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                GameDirector game=UnityEngine.Object.FindObjectOfType<GameDirector>();
                Check(game!=null,"play mode director");
                game.gameObject.AddComponent<SmokeRoutine>().Complete=CompleteSmoke;
            }
        }

        [InitializeOnLoadMethod]
        static void RestoreSmokeHook()
        {
            if(!SessionState.GetBool("DeepseaSmoke",false))return;
            GameDirector.VerificationMode=true;
            EditorApplication.playModeStateChanged-=OnPlayState;
            EditorApplication.playModeStateChanged+=OnPlayState;
            EditorApplication.update-=WatchSmoke;
            EditorApplication.update+=WatchSmoke;
        }
        static void WatchSmoke()
        {
            if(SessionState.GetBool("DeepseaSmoke",false)&&EditorApplication.timeSinceStartup>SessionState.GetFloat("DeepseaSmokeDeadline",float.MaxValue))
                CompleteSmoke(false,"Play smoke timeout");
        }
        public static void CompleteSmoke(bool ok,string detail)
        {
            Directory.CreateDirectory("Verification");
            File.WriteAllText("Verification/unity-play-result.json","{\"unityVersion\":\""+Application.unityVersion+"\",\"passed\":"+(ok?"true":"false")+",\"detail\":\""+detail.Replace("\"","'").Replace("\n"," ")+"\"}");
            SessionState.SetBool("DeepseaSmoke",false);
            Debug.Log(ok?"DEEPSEA_PLAY_OK":"DEEPSEA_PLAY_FAILED "+detail);
            EditorApplication.Exit(ok?0:1);
        }
    }
}
