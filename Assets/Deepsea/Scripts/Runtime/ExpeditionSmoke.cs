#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Deepsea
{
    public sealed class ExpeditionSmoke : MonoBehaviour
    {
        public Action<bool,string> Complete;
        public bool TerrainOnly;
        public bool BoatOnly;
        GameDirector game;
        ExpeditionController e;
        string failure="";
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string condition,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure=condition;}
        void Check(bool condition,string text){if(!condition&&failure=="")failure=text;}
        IEnumerator Start()
        {
            yield return new WaitForSeconds(2);
            game=GetComponent<GameDirector>();e=game.Expedition;
            if(BoatOnly)
            {
                game.Boat.Recover(e.Landings[2].berth);Check(e.Disembark(),"Boat fixture disembark failed");
                game.progress.packParts=3;e.DiagnoseBoat();e.SelectTool(HandTool.Repair);
                Check(e.RepairBoat(),"Boat fixture field repair failed");
                float deadline=Time.time+70,reportAt=Time.time+10;
                WorldInteractable boat=game.World.Targets.First(t=>t.id=="qinglu-boat");
                while(!game.progress.boats.Exists(b=>b.id=="qinglu-boat")&&Time.time<deadline)
                {
                    if(Time.time>reportAt){reportAt=Time.time+10;Debug.Log("DEEPSEA_BOAT_TRACE transform="+boat.transform.position.ToString("F2")+" body="+boat.GetComponent<Rigidbody>().position.ToString("F2"));}
                    yield return null;
                }
                Check(game.progress.boats.Exists(b=>b.id=="qinglu-boat"&&b.repaired),"Repaired vessel did not reach harbour; actual="+boat.transform.position.ToString("F2"));
                Complete(failure=="",failure==""?"Field-repaired vessel followed its physics route and settled once at harbour.":failure);yield break;
            }
            if(TerrainOnly)
            {
                game.Boat.Recover(e.Landings[1].berth);Check(e.Disembark(),"Terrain fixture disembark failed");
                e.Explorer.Place(IslandTerrain.Point(-12,78));
                yield return Walk(22,80);
                Complete(failure=="",failure==""?"Terrain crossing passed.":failure);yield break;
            }
            Check(e.Landings.Count==3&&e.Sites.Count>=8,"Landing and island content missing");
            foreach(MeshCollider ramp in game.World.Root.GetComponentsInChildren<MeshCollider>().Where(c=>c.name=="木板缓坡"))
            {
                RaycastHit hit;
                Check(ramp.Raycast(new Ray(ramp.bounds.center+Vector3.up*5,Vector3.down),out hit,10)&&hit.normal.y>.7f,"Ramp must support a downward ray from above");
            }
            Debug.Log("DEEPSEA_EXPEDITION_STAGE east shore rescue");
            e.ToggleModule(ShipModule.TowAssist);e.ToggleModule(ShipModule.FieldRepair);
            Check(game.progress.modules.Contains(ShipModule.FieldRepair),"Port loadout switch failed");
            e.MoveParts(ItemLocation.Village,ItemLocation.Ship,4);
            e.MoveParts(ItemLocation.Ship,ItemLocation.Backpack,2);
            LandingPoint east=e.Landings[2];
            game.Boat.Recover(east.berth);
            Check(e.Disembark()&&game.Boat.Moored,"East shore disembark or stable mooring failed");
            Vector3 moored=game.Boat.transform.position;
            e.ToggleAssets();
            Check(game.Paused&&e.AssetsOpen&&Time.timeScale==0,"Asset view must pause the expedition");
            yield return new WaitForSecondsRealtime(.25f);
            e.ClosePanels();
            Check(!game.Paused&&!e.AssetsOpen&&Time.timeScale==1,"Closing assets must restore input time");
            Check(Vector3.Distance(moored,game.Boat.transform.position)<.01f,"Mooring drifted while inspecting assets");
            yield return Walk(37,94);yield return Walk(34,100);
            IslandSite survivor=e.Sites.First(s=>s.kind==IslandSiteKind.Survivor);
            e.InteractSite(survivor);
            Check(e.Companion.Following&&!game.progress.island.stationPowered,"Rescue should work without station power");
            yield return Walk(37,94);yield return Walk(east.feet.x,east.feet.z,true);
            float until=Time.time+6;
            while(Vector3.Distance(e.Companion.transform.position,east.feet)>5.5f&&Time.time<until)yield return null;
            Check(e.BoardShip()&&game.trip.passengers==1,"Actual companion arrival or boarding failed");
            game.Boat.Recover(game.World.DockPosition);game.Dock();game.HarbourMenu=false;
            Check(game.progress.people.Exists(p=>p.id=="linzhou"),"Named survivor not settled");
            e.AssignTechnician();Check(game.progress.technicianAssigned,"Resident collaboration has no effect");
            Debug.Log("DEEPSEA_EXPEDITION_STAGE south shore station");
            e.MoveParts(ItemLocation.Village,ItemLocation.Ship,2);e.MoveParts(ItemLocation.Ship,ItemLocation.Backpack,2);
            LandingPoint south=e.Landings[1];game.Boat.Recover(south.berth);
            Check(e.Disembark(),"South shore disembark failed");
            yield return Walk(8,68);yield return Walk(-9,77);yield return Walk(-21,89);yield return Walk(-16,108);yield return Walk(-3,107);
            e.SelectTool(HandTool.Observe);e.Inspect(false);Check(!game.progress.island.connectorDiagnosed,"Battery check should not solve connector fault");
            e.Inspect(true);e.SelectTool(HandTool.Repair);Check(e.RepairStation(),"Diagnosis and carried-material station repair failed");
            e.SetBearing(25);Check(!e.ReadSignal(false),"Wrong antenna bearing must not complete signal");
            e.SetBearing(95);Check(e.ReadSignal(false),"East shore signal tuning failed");
            e.SetBearing(336);Check(e.ReadSignal(true),"Sea echo tuning failed");
            Capture("Verification/unity-island-station.png");
            yield return Walk(-16,108);yield return Walk(-21,89);yield return Walk(-9,77);yield return Walk(-12,78);
            e.InteractSite(e.Sites.First(s=>s.kind==IslandSiteKind.Supplies));
            Check(game.trip.packParts==3,"Island supplies must physically enter backpack");
            yield return Walk(22,80);yield return Walk(35,96);yield return Walk(38,96);
            e.SelectTool(HandTool.Observe);e.DiagnoseBoat();e.SelectTool(HandTool.Repair);
            Check(e.RepairBoat()&&game.trip.packParts==1,"Repair loadout should spend two carried parts");
            Debug.Log("DEEPSEA_EXPEDITION_STAGE field boat repair and return");
            yield return Walk(22,80);yield return Walk(8,68);yield return Walk(south.feet.x,south.feet.z,true);
            Check(e.BoardShip(),"Return to original landing failed");
            game.Boat.Recover(game.World.DockPosition);game.Dock();game.HarbourMenu=false;
            Check(game.progress.island.stationPowered&&game.progress.repairDebt==0,"Field state or costs not settled once");
            until=Time.time+70;
            while(!game.progress.boats.Exists(b=>b.id=="qinglu-boat")&&Time.time<until)yield return null;
            Check(game.progress.boats.Exists(b=>b.id=="qinglu-boat"&&b.repaired),"Repaired vessel did not reach harbour");
            e.AssignRelay();Check(game.progress.relayAssigned,"Recovered vessel lacks usable support assignment");
            Debug.Log("DEEPSEA_EXPEDITION_STAGE village walking");
            Check(e.Disembark(),"Village disembark failed");
            yield return Walk(2,-28,true);yield return Walk(2,-36.5f,true);
            yield return Walk(2,-28,true);yield return Walk(e.Landings[0].feet.x,e.Landings[0].feet.z,true);
            Check(e.BoardShip(),"Village ramp return and boarding failed");
            Capture("Verification/unity-island-bay.png",true);
            Complete(failure=="",failure==""?"Both landing shores, physical island walking, companion following/boarding, tools, station diagnosis/tuning, carried stock, resident collaboration, field boat repair and harbour relay passed.":failure);
        }
        IEnumerator Walk(float x,float z,bool pier=false)
        {
            Debug.Log("DEEPSEA_EXPEDITION_WALK "+x+", "+z);
            Vector3 goal=pier?new Vector3(x,.72f,z):IslandTerrain.Point(x,z);
            float deadline=Time.time+25;
            float reportAt=Time.time+5;
            e.Explorer.AutomationInput=true;
            while(Time.time<deadline)
            {
                Vector3 delta=goal-e.Explorer.transform.position;delta.y=0;
                if(delta.magnitude<.85f)break;
                e.Explorer.AutomationDirection=delta.normalized;
                if(Time.time>reportAt){reportAt=Time.time+5;Debug.Log("DEEPSEA_WALK_TRACE pos="+e.Explorer.transform.position.ToString("F3")+" move="+e.Explorer.LastMove.ToString("F3")+" flags="+e.Explorer.LastFlags+" normal="+e.Explorer.LastContactNormal.ToString("F3"));}
                yield return null;
            }
            e.Explorer.AutomationInput=false;e.Explorer.AutomationDirection=Vector3.zero;
            Check(new Vector2(e.Explorer.transform.position.x-x,e.Explorer.transform.position.z-z).magnitude<1.2f,
                "Walking route blocked near "+x+", "+z+"; actual="+e.Explorer.transform.position.ToString("F2")+"; blocked="+e.Explorer.Blocked+"; contacts="+
                string.Join(",",Physics.OverlapSphere(e.Explorer.transform.position+Vector3.up*.7f,1).Select(c=>c.name).ToArray()));
            if(failure!=""){Capture("Verification/unity-expedition-failure.png");Complete(false,failure);yield break;}
        }
        void Capture(string path,bool overview=false)
        {
            Camera camera=game.Camera;game.CameraFollow.enabled=false;
            if(overview){Quaternion r=Quaternion.Euler(55,0,0);camera.transform.SetPositionAndRotation(new Vector3(0,0,40)-r*Vector3.forward*220,r);camera.orthographicSize=112;}
            RenderTexture previous=camera.targetTexture,active=RenderTexture.active;
            RenderTexture target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            Texture2D output=new Texture2D(1600,900,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1600,900),0,0);output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());
            camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(output);game.CameraFollow.enabled=true;
        }
    }
}
#endif
