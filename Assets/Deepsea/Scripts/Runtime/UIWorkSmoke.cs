using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Deepsea
{
    // Explicit test mode uses isolated progress and never reads or writes player saves.
    public sealed class UIWorkSmoke : MonoBehaviour
    {
        public Action<bool,string> Complete;
        GameDirector game;
        string failure="";
        int assertions;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Before(){if(Environment.GetCommandLineArgs().Contains("-deepsea-check"))GameDirector.VerificationMode=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void After(){if(Environment.GetCommandLineArgs().Contains("-deepsea-check")){var g=FindObjectOfType<GameDirector>();var s=g.gameObject.AddComponent<UIWorkSmoke>();s.Complete=(ok,detail)=>Application.Quit(ok?0:1);}}
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;Time.timeScale=1;}
        void Log(string c,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)failure=c;}
        void Check(bool ok,string detail){assertions++;if(!ok){failure=detail;Debug.LogError("UI_WORK_CHECK_FAILED "+detail);}}
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(2);game=GetComponent<GameDirector>();var e=game.Expedition;var w=game.Timber;var ui=game.UI;var wood=AssetRules.Stock(game.progress,"wood");
            Check(!w.Assign(),"Unsettled resident must not accept work");
            Check(e.Disembark(),"Village disembark failed");
            e.Explorer.Place(w.TreePoints[0]+Vector3.right);e.SelectTool(HandTool.Axe);e.UseTool();
            yield return new WaitForSeconds(1);e.Explorer.Place(w.StorePoint);yield return null;
            Check(!w.PlayerWorking&&game.progress.timber.trees[0].hits<3,"Moving must interrupt chopping");
            e.Explorer.Place(w.TreePoints[0]+Vector3.right);e.UseTool();yield return new WaitForSeconds(3.5f);
            Check(game.progress.timber.trees[0].hits==3&&game.progress.timber.trees[0].logs==2,"Real axe use must leave a finite bundle");
            Check(w.Pickup(0)&&!w.Pickup(0)&&wood.backpack==2,"Bundle pickup must happen once");
            Check(!w.Deposit(),"Forest logs cannot remotely enter warehouse");e.Explorer.Place(w.StorePoint);Check(w.Deposit()&&wood.village==2&&wood.backpack==0,"Wood must be delivered to village");
            Check(!w.BuildPier(),"Pier requires six real logs");
            // Persist finite tree drops and carried work, not a production counter.
            game.progress.timber.workerCarry=1;var reloaded=JsonUtility.FromJson<HarbourProgress>(JsonUtility.ToJson(game.progress));reloaded.Normalize();
            Check(reloaded.timber.workerCarry==1&&reloaded.timber.trees[0].hits==3&&AssetRules.Stock(reloaded,"wood").village==2,"Save roundtrip lost real resource state");game.progress.timber.workerCarry=0;
            game.progress.packParts=6;Check(!ui.Transfer("parts",ItemLocation.Village,ItemLocation.Backpack),"Full backpack must reject parts");game.progress.packParts=0;
            int total=AssetRules.Stock(game.progress,"bait").village;
            Check(ui.Transfer("bait",ItemLocation.Village,ItemLocation.Backpack)&&AssetRules.Stock(game.progress,"bait").village+AssetRules.Stock(game.progress,"bait").backpack==total,"Transfer must conserve provisions");
            e.Explorer.Place(IslandTerrain.Point(8,80));Check(ui.TransferReason("bait",ItemLocation.Village,ItemLocation.Backpack)!=""&&!ui.Transfer("bait",ItemLocation.Village,ItemLocation.Backpack),"Remote access must be blocked");e.Explorer.Place(w.StorePoint);
            ui.Open(UiPage.Inventory);ui.Open(UiPage.Residents);ui.Close();Check(!game.Paused&&Time.timeScale==1,"Switching panels must restore original pause");
            game.SetPause(true);ui.Open(UiPage.Inventory);ui.Close();Check(game.Paused,"Closing menu must preserve inherited pause");game.SetPause(false);
            e.SelectTool(HandTool.Observe);e.UseTool();Check(game.CameraFollow.Scoping,"Binoculars must enter actual scope");yield return new WaitForSeconds(1);Check(game.Camera.orthographicSize<8,"Scope must visibly magnify world");e.SelectTool(HandTool.Axe);Check(!game.CameraFollow.Scoping,"Switching tool must leave scope");
            // Resident fixture is settled exactly as docking adds the named survivor.
            AssetRules.AddResident(game.progress,new NamedResident{id="linzhou",name="林舟",skill="船匠"});w.RefreshWorker();
            Check(w.Assign(),"Settled resident work order failed");
            e.Explorer.Place(e.Landings[0].feet);Check(e.BoardShip(),"Boarding after order failed");game.Boat.Recover(new Vector3(25,0,20));
            Time.timeScale=5;float deadline=Time.realtimeSinceStartup+120,trace=0;
            while(game.progress.timber.delivered<6&&Time.realtimeSinceStartup<deadline)
            {if(Time.realtimeSinceStartup>trace){trace=Time.realtimeSinceStartup+8;Debug.Log("UI_WORK_TRACE "+w.WorkerState+" pos="+w.Worker.position.ToString("F2")+" delivered="+game.progress.timber.delivered);}yield return null;}
            Time.timeScale=1;
            Check(game.progress.timber.delivered==6&&wood.village==8&&!game.progress.timber.assigned,"Resident must physically produce and deliver six logs while player is at sea; "+w.WorkerState+" "+w.Worker.position);
            game.Boat.Recover(game.World.DockPosition);Check(w.BuildPier(),"Delivered materials must build functional pier");Check(!w.BuildPier()&&wood.village==2&&e.Landings.Count==4,"Pier must consume once and add a real landing");
            Debug.Log("UI_WORK_STAGE responsive UI");
            foreach(int[] size in new[]{new[]{1280,720},new[]{1920,1080},new[]{2560,1440},new[]{2560,1080}})
            {
                Screen.SetResolution(size[0],size[1],FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);
                foreach(float scale in new[]{1f,1.5f})
                {
                    ui.SetScale(scale,false);
                    foreach(UiPage page in new[]{UiPage.Inventory,UiPage.Loadout,UiPage.Residents,UiPage.Harbour,UiPage.Pause})
                    {
                        ui.Open(page);yield return null;UnityEngine.Canvas.ForceUpdateCanvases();
                        var panel=ui.Canvas.transform.Find("唯一模态窗口/管理面板");
                        Check(panel!=null&&game.Paused&&game.Boat.InputBlocked,"Modal must block gameplay");
                        // Every page keeps its close control visible; long content scrolls inside viewport.
                        var close=panel.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("关闭"));Vector3[] corners=new Vector3[4];((RectTransform)close.transform).GetWorldCorners(corners);
                        Check(corners.All(p=>p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1),"Close control outside "+Screen.width+"x"+Screen.height+" scale "+scale);
                        var footer=(RectTransform)panel.GetChild(panel.childCount-1);footer.GetWorldCorners(corners);Check(corners.All(p=>p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1),"Footer outside screen at "+Screen.width+" scale "+scale);
                        foreach(var t in panel.GetComponentsInChildren<TextMeshProUGUI>())Check(t.font!=null,"Missing bundled font");
                        if(!Application.isEditor&&((size[0]==1280&&scale==1.5f)||(size[0]==1920&&scale==1f)))
                        {yield return new WaitForEndOfFrame();Capture("ui-"+size[0]+"x"+size[1]+"-"+scale.ToString("0.0")+"-"+page+".png");}
                    }
                    ui.Open(UiPage.Inventory);if(!Application.isEditor)yield return new WaitForEndOfFrame();else yield return null;
                    if(!Application.isEditor)Capture("ui-"+size[0]+"x"+size[1]+"-"+scale.ToString("0.0")+".png");
                    ui.Close();
                }
            }
            ui.SetScale(1,false);ui.Close();
            Directory.CreateDirectory("Verification");string detail=failure==""?"Axe interruption/pickup, inventory conservation/location/capacity, pause stack, real binocular camera, physical resident wood delivery, once-only pier and responsive Canvas passed; checks="+assertions:failure;
            File.WriteAllText("Verification/unity-ui-work-result.json",JsonUtility.ToJson(new Result{passed=failure=="",detail=detail,unity=Application.unityVersion,checks=assertions},true));
            Debug.Log(failure==""?"DEEPSEA_UI_WORK_OK "+detail:"DEEPSEA_UI_WORK_FAILED "+failure);Complete(failure=="",detail);
        }
        void Capture(string name){var image=ScreenCapture.CaptureScreenshotAsTexture();Directory.CreateDirectory("Verification");File.WriteAllBytes("Verification/"+name,image.EncodeToPNG());Destroy(image);}
        [Serializable] class Result {public bool passed;public string detail,unity;public int checks;}
    }
}
