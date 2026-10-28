#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Deepsea
{
    public sealed class SmokeRoutine : MonoBehaviour
    {
        public Action<bool,string> Complete;
        bool loggedError;
        string failure="";
        void Awake(){Application.logMessageReceived+=OnLog;}
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
        void OnLog(string condition,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){loggedError=true;failure=condition;}}
        IEnumerator Start()
        {
            yield return new WaitForSeconds(2);
            GameDirector g=GetComponent<GameDirector>();
            Vector3 initial=g.Boat.GetComponent<Rigidbody>().position;
            float fuel=g.Boat.Fuel;
            g.Boat.AutomationInput=true;g.Boat.AutomationThrottle=1;
            yield return new WaitForSeconds(2);
            if(Vector3.Distance(initial,g.Boat.GetComponent<Rigidbody>().position)<2||g.Boat.Fuel>=fuel)
            {Complete(false,"Actual physics propulsion or fuel failed");yield break;}
            g.Boat.AutomationRudder=1;
            Quaternion rotation=g.Boat.transform.rotation;
            yield return new WaitForSeconds(.7f);
            if(Quaternion.Angle(rotation,g.Boat.transform.rotation)<3)
            {Complete(false,"Actual physics steering failed");yield break;}
            g.Boat.AutomationInput=false;g.Boat.Service();
            string result=RunFlow(g);
            if(result!=""){Complete(false,result);yield break;}
            g.HarbourMenu=false;g.CameraFollow.Overview=false;
            yield return new WaitForSeconds(2);
            g.CameraFollow.enabled=false;
            Quaternion harbourRotation=Quaternion.Euler(55,0,0);
            g.Camera.transform.SetPositionAndRotation(new Vector3(5,0,-29)-harbourRotation*Vector3.forward*160,harbourRotation);
            g.Camera.orthographicSize=30;
            Capture(g.Camera,"Verification/unity-harbour-render.png");
            Quaternion bayRotation=Quaternion.Euler(55,0,0);
            g.Camera.transform.SetPositionAndRotation(new Vector3(0,0,34)-bayRotation*Vector3.forward*220,bayRotation);
            g.Camera.orthographicSize=106;
            yield return new WaitForSeconds(2);
            Capture(g.Camera,"Verification/unity-bay-render.png");
            Complete(!loggedError,loggedError?failure:"Physics propulsion, steering, rescue, tow, docking, construction, scanner and real camera render passed.");
        }
        string RunFlow(GameDirector g)
        {
            try
            {
                if(g.World.Targets.Count<10)throw new Exception("World targets missing");
                g.trip.TryLoad("test-supplies",14,3,20);
                g.trip.records=1;
                WorldInteractable rescue=g.World.Targets.First(t=>t.kind==InteractionKind.Rescue);
                g.Boat.GetComponent<Rigidbody>().position=rescue.transform.position+Vector3.right*3;
                rescue.Interact();
                if(g.trip.passengers!=2)throw new Exception("Rescue interaction failed");
                rescue.Interact();
                if(g.Boat.Tow!=rescue.towable)throw new Exception("Tow interaction failed");
                g.Boat.GetComponent<Rigidbody>().position=g.World.DockPosition;
                g.Boat.transform.position=g.World.DockPosition;
                rescue.GetComponent<Rigidbody>().position=g.World.DockPosition+Vector3.left*5;
                rescue.transform.position=g.World.DockPosition+Vector3.left*5;
                g.Boat.GetComponent<Rigidbody>().velocity=Vector3.zero;
                g.Dock();
                if(g.progress.residents!=7||g.progress.recoveredBoats!=1)throw new Exception("Delivery failed");
                if(!g.Build(Building.Winch)||!g.Build(Building.Charts))throw new Exception("Construction failed");
                if(!g.Build(Building.Net))throw new Exception("Defense construction failed");
                if(g.ScanRadius()!=g.settings.improvedScanRadius)throw new Exception("Scanner upgrade not applied");
                WorldInteractable heavy=g.World.Targets.First(t=>t.kind==InteractionKind.HeavySalvage);
                g.Boat.GetComponent<Rigidbody>().position=heavy.transform.position+Vector3.right*3;
                g.Boat.transform.position=heavy.transform.position+Vector3.right*3;
                heavy.Interact();
                if(g.Boat.Tow!=heavy.towable)throw new Exception("Heavy tow unlock failed");
                g.Boat.ReleaseTow();
                g.Scan();
                if(g.ScanRemaining<=0)throw new Exception("Scanner cooldown missing");
                g.Boat.Recover(g.World.DockPosition+new Vector3(4,0,6));
                return "";
            }
            catch(Exception e){return e.Message;}
        }
        static void Capture(Camera camera,string path)
        {
            RenderTexture previous=camera.targetTexture;
            RenderTexture active=RenderTexture.active;
            RenderTexture texture=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
            Texture2D output=new Texture2D(1600,900,TextureFormat.RGB24,false);
            output.ReadPixels(new Rect(0,0,1600,900),0,0);output.Apply();
            File.WriteAllBytes(path,output.EncodeToPNG());
            camera.targetTexture=previous;RenderTexture.active=active;texture.Release();
            Destroy(texture);Destroy(output);
        }
    }
}
#endif
