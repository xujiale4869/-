using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Deepsea {
 public sealed partial class SeaLifeVisualUpgradeV25 {
  bool checking,finished;
  string evidence="Verification/sea-life-v25-review01";
  [Serializable]sealed class ReviewData{
   public bool passed;public string resolution,gpu,temporarySave;public int fishVertices,fishTriangles;
   public List<string> checks=new List<string>(),failed=new List<string>(),errors=new List<string>(),warnings=new List<string>();
   public float seaFps,seaP95Ms,roomFps,roomP95Ms,baselineSeaFps,baselineSeaP95Ms;public int seaFrames,roomFrames;
  }
  ReviewData result;
  void SetupReviewV25(){
   var args=Environment.GetCommandLineArgs();checking=args.Contains("-v25-check");bool gallery=args.Contains("-v25-gallery");
   if(!checking&&!gallery)return;
   int index=Array.IndexOf(args,"-v25-evidence");if(index>=0&&index+1<args.Length)evidence=args[index+1];
   game.store=new SeaLifeStore(Path.Combine(Application.temporaryCachePath,"sea-life-v25-"+DateTime.UtcNow.Ticks+".json"));game.save=new SeaLifeSave();game.save.Normalize();game.AutoInput=true;game.DriveInput=Vector2.zero;game.RefreshVisualCollectionV25();
   Application.runInBackground=true;
   if(checking){result=new ReviewData{resolution=Screen.width+"x"+Screen.height,gpu=SystemInfo.graphicsDeviceName,temporarySave=game.store.PathName};Directory.CreateDirectory(evidence);Application.logMessageReceived+=ReviewLog;StartCoroutine(ReviewDeadlineV25());StartCoroutine(RunReviewV25());}
   else{CreateGalleryFixturesV25();game.Notify("v25展缸演示 · 本入口使用独立临时样本，不写入正常收藏。V切近景，F离馆。");}
  }
  void ReviewLog(string text,string stack,LogType type){if(result==null)return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)result.errors.Add(text);else if(type==LogType.Warning)result.warnings.Add(text);}
  void Check(bool pass,string name){if(pass)result.checks.Add(name);else{result.failed.Add(name);Debug.LogError("V25_CHECK_FAILED "+name);}}
  IEnumerator ReviewDeadlineV25(){yield return new WaitForSecondsRealtime(150);if(!finished){Check(false,"review completed before deadline");FinishReviewV25();}}
  void FinishReviewV25(){finished=true;result.passed=result.failed.Count==0&&result.errors.Count==0;File.WriteAllText(evidence+"/result.json",JsonUtility.ToJson(result,true));Debug.Log("SEA_LIFE_V25_REVIEW_DONE passed="+result.passed+" checks="+result.checks.Count);Application.logMessageReceived-=ReviewLog;Application.Quit(result.passed?0:2);}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();var pixels=tex.GetPixels32();int bright=0,pink=0;foreach(var p in pixels){if(p.r+p.g+p.b>40)bright++;if(p.r>245&&p.b>245&&p.g<12)pink++;}Check(bright>pixels.Length*.15f,name+" visible frame");Check(pink<pixels.Length*.001f,name+" no pink shader errors");File.WriteAllBytes(evidence+"/"+name+".jpg",tex.EncodeToJPG(92));Destroy(tex);}
  IEnumerator Sample(bool room){for(int i=0;i<80;i++)yield return null;var values=new List<float>();for(int i=0;i<600;i++){yield return null;values.Add(Time.unscaledDeltaTime);}values.Sort();float fps=1/values.Average(),p95=values[(int)(values.Count*.95f)]*1000;if(room){result.roomFps=fps;result.roomP95Ms=p95;result.roomFrames=values.Count;}else{result.seaFps=fps;result.seaP95Ms=p95;result.seaFrames=values.Count;}}
  void CreateGalleryFixturesV25(){
   game.EnterVisualReviewRoomV25();game.save.coins=100;
   for(int i=0;i<3;i++){game.SelectedTank=i;game.BuildTank();game.save.tanks[i].kelp=i!=1;for(int f=0;f<3;f++)game.save.fish.Add(new SeaLifeFish{id="v25-gallery-"+i+"-"+f,species=f,tank=i,area="v25临时展示样本"});}
   game.SelectedTank=0;game.RefreshVisualCollectionV25();game.Panel=0;game.ui.RebuildPanel();game.CloseView=true;
  }
  IEnumerator RunReviewV25(){
   yield return new WaitForSeconds(1);
   Check(GetComponent<SeaSurfaceV24>()!=null&&SeaSurfaceV24.RollingActive,"v24 rolling sea retained");
   Check(Application.targetFrameRate==-1&&QualitySettings.vSyncCount==0,"existing uncapped frame policy retained");
   Check(game.save.schema==1&&game.save.fish.Count==0,"fresh temporary schema1 save");
   for(int i=0;i<3;i++){var m=FishMeshes[i];result.fishVertices+=m.vertexCount;result.fishTriangles+=m.triangles.Length/3;Check(m.vertexCount>500&&m.vertexCount<10000,"fish "+i+" imported detailed mesh");Check(FishMaterials[i].shader.name=="Deepsea/SeaLifeFishSwimV25"&&FishMaterials[i].shader.isSupported,"fish "+i+" animation shader active");Check(FishMaterials[i].GetTexture("_BaseMap").width==512,"fish "+i+" 512px albedo");}
   Check(FrameMesh.subMeshCount==3&&InteriorMesh.subMeshCount==2,"wood brass lamp and sand reef material slots preserved");
   Check(FishUpgrades==44,"all 43 swimming fish and inactive catch fish upgraded once");
   yield return Capture("01-sea");game.SetReviewFishBaselineV25(true);yield return new WaitForSeconds(.25f);yield return Sample(false);result.baselineSeaFps=result.seaFps;result.baselineSeaP95Ms=result.seaP95Ms;game.SetReviewFishBaselineV25(false);yield return new WaitForSeconds(.25f);yield return Sample(false);
   var start=game.boat.position;game.DriveInput=new Vector2(.15f,1);yield return new WaitForSeconds(1.5f);game.DriveInput=Vector2.zero;Check(Vector3.Distance(start,game.boat.position)>1,"real boat propulsion still works");
   yield return game.CatchReviewV25(Check);
   game.EnterVisualReviewRoomV25();Check(game.InRoom&&game.OnFoot,"physical gallery entry retains original interaction");
   game.SelectedTank=0;int coins=game.save.coins;Check(game.BuildTank()&&game.save.coins==coins,"first tank still free");Check(!game.BuildTank(),"duplicate construction rejected");game.SelectedTank=1;Check(!game.BuildTank()&&game.save.coins==coins,"unaffordable second tank rejected");
   for(int i=1;i<3;i++)game.save.fish.Add(new SeaLifeFish{id="v25-check-species-"+i,species=i,area="temporary fixture"});
   game.SelectedTank=0;foreach(var f in game.save.fish.ToArray())Check(game.MoveFish(f.id,0),"move species "+f.species+" into tank");
   yield return null;yield return null;
   Check(game.TankCount(0)==3&&game.CarryCount==0,"fish ownership conserved");
   var tank=game.VisualTanksV25[0];Check(tank.Find("V25_AquariumDetails")!=null,"new tank visual installed synchronously");
   Check(!tank.Find("缸内主石").GetComponent<Renderer>().enabled,"old stretched sphere replaced");
   Check(tank.Find("木柜底座").GetComponent<Collider>().enabled,"original walking collider retained");
   Check(tank.Find("V25_AquariumDetails").GetComponentsInChildren<MeshRenderer>().Length==5,"one frame interior glass surface and caustics layer");
   var swimmers=game.VisualFishV25.GetComponentsInChildren<SeaLifeFishSwimmerV25>();Check(swimmers.Length==3,"new collection fish receive independent swimmers");
   Check(swimmers.Select(s=>s.Phase).Distinct().Count()==3,"per fish animation phase differs");
   game.Panel=0;game.ui.RebuildPanel();game.CloseView=false;yield return new WaitForSeconds(1);yield return Capture("02-aquarium-room");
   game.CloseView=true;yield return new WaitForSeconds(1.5f);yield return Capture("03-aquarium-close");yield return Sample(true);
   float phase=swimmers[0].Phase,clock=Clock;game.Paused=true;Time.timeScale=0;yield return new WaitForSecondsRealtime(.25f);Check(Mathf.Abs(swimmers[0].Phase-phase)<.00001f&&Clock==clock,"pause freezes fish and caustics animation");game.Paused=false;Time.timeScale=1;
   string first=game.save.fish[0].id;Check(game.MoveFish(first,-1)&&game.CarryCount==1&&game.TankCount(0)==2,"fish withdrawal preserves ownership");Check(game.MoveFish(first,0),"fish returns to tank");
   game.ChangeTankTheme();yield return null;yield return null;Check(game.VisualTanksV25[0].GetComponentsInChildren<MeshFilter>(true).Any(m=>m.name=="缸景绿色长带海带"),"existing clean green kelp theme retained");
   for(int i=0;i<4;i++)game.save.fish.Add(new SeaLifeFish{id="v25-capacity-"+i,species=i%3,area="temporary capacity fixture"});
   for(int i=0;i<3;i++)Check(game.MoveFish("v25-capacity-"+i,0),"available tank slot accepts fish "+i);
   Check(!game.MoveFish("v25-capacity-3",0)&&game.TankCount(0)==6,"seventh fish blocked at existing capacity six");
   game.SaveNow();var loaded=new SeaLifeStore(game.store.PathName).Load();Check(loaded.schema==1&&loaded.fish.Count==game.save.fish.Count&&loaded.fish.Select(f=>f.id).Distinct().Count()==loaded.fish.Count,"original save schema and unique IDs roundtrip");
   game.save=loaded;game.RefreshVisualCollectionV25();yield return null;yield return null;
   Check(game.VisualTanksV25[0].GetComponentsInChildren<Transform>(true).Count(t=>t.name=="V25_AquariumDetails")==1,"collection rebuild does not duplicate tank layers");
   game.save.fish.RemoveAll(f=>f.id.StartsWith("v25-capacity-"));game.save.coins=100;
   for(int i=1;i<3;i++){game.SelectedTank=i;Check(game.BuildTank(),"tank "+i+" builds with existing 35 coin cost");}
   game.SelectedTank=0;game.RefreshVisualCollectionV25();game.Panel=0;game.ui.RebuildPanel();game.CloseView=false;yield return new WaitForSeconds(1);yield return Capture("04-three-tanks");
   Check(game.VisualTanksV25.All(t=>t!=null&&t.Find("V25_AquariumDetails")!=null),"all three fixed slots upgraded");
   game.SelectedTank=0;game.CloseView=true;yield return new WaitForSeconds(1.5f);
   var canvases=FindObjectsOfType<Canvas>();foreach(var c in canvases)c.enabled=false;game.NoticeTime=0;
   var sea=GetComponent<SeaSurfaceV24>();sea.SetReviewFPSV25(false);Directory.CreateDirectory(evidence+"/motion");Time.captureFramerate=24;
   for(int i=0;i<144;i++){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(evidence+"/motion/"+i.ToString("D4")+".jpg",t.EncodeToJPG(85));Destroy(t);}
   Time.captureFramerate=0;foreach(var c in canvases)c.enabled=true;
   yield return Capture("05-final-close");sea.SetReviewFPSV25(true);Check(swimmers.All(s=>s==null||!float.IsNaN(s.transform.rotation.x)),"animation transforms remain finite");FinishReviewV25();
  }
 }
 public sealed partial class SeaLifeRuntime {
  internal IEnumerator CatchReviewV25(Action<bool,string> check){
   PlaceBoat(fishSpots[0]+new Vector3(-1,0,0),90);DriveInput=Vector2.zero;
   check(BeginFishing(),"prepare existing fishing rig");check(Cast(fishSpots[0]),"cast with new fish assets");yield return new WaitForSeconds(3.2f);check(fishingStage==3,"existing bite state reached");check(StartReeling(),"existing strike starts reeling");
   float limit=Time.time+25;while(fishingStage==4&&Time.time<limit){AutoReel=activityTime%2.8f<1.75f;yield return null;}AutoReel=false;check(fishingStage==5,"existing tension loop catches fish");check(KeepCatch()&&save.fish.Count==1,"caught fish kept once in original collection");check(!KeepCatch()&&save.fish.Count==1,"catch cannot be duplicated");
  }
 }
}

namespace Deepsea { public sealed partial class SeaSurfaceV24 { internal void SetReviewFPSV25(bool visible){showFPS=visible;} } }
