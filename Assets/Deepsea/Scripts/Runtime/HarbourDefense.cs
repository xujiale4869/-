using UnityEngine;

namespace Deepsea
{
    // Optional, explicitly started sample. Exploration never starts a raid timer.
    public sealed class HarbourDefense : MonoBehaviour
    {
        public GameDirector game;
        public bool Active { get; private set; }
        public string Status { get; private set; }
        public float WarningRemaining { get { return Mathf.Max(0, warningUntil-Time.time); } }
        Transform beast;
        LineRenderer net;
        float warningUntil, lastShot, netUntil, progress, glowUntil;

        public void StartDrill()
        {
            if(Active||!game.progress.net)return;
            Active=true;progress=0;netUntil=0;lastShot=Time.time;
            warningUntil=Time.time+12;
            Status="东侧水痕正在接近渔获浮台。12 秒准备。";
            beast=VisualFactory.Group("浅海掠食兽 · 守港样本",game.World.Root,new Vector3(91,0,16));
            VisualFactory.Sphere("背部轮廓",beast,new Vector3(0,.08f,0),new Vector3(3.2f,.45f,8),new Color(.14f,.25f,.26f));
            Vector2[] fin={new Vector2(0,0),new Vector2(0,1.6f),new Vector2(.8f,-.8f)};
            Transform dorsal=VisualFactory.MeshObject("背鳍",VisualFactory.Prism(fin,0,.12f),beast,new Color(.18f,.3f,.3f)).transform;
            dorsal.localPosition=new Vector3(0,.2f,-.5f);dorsal.localRotation=Quaternion.Euler(0,0,90);
            net=VisualFactory.Line("缆网发射",game.World.Root,.08f,VisualFactory.Rope);net.positionCount=2;net.enabled=false;
            game.HarbourMenu=false;game.Notify(Status);
        }
        void Update()
        {
            if(!Active||game.Paused)return;
            if(Time.time<warningUntil)return;
            Vector3 target=game.World.FishPlatform.position;
            Vector3 delta=target-beast.position;delta.y=0;
            beast.rotation=Quaternion.LookRotation(delta);
            float slow=Time.time<netUntil?.24f:1;
            beast.position+=delta.normalized*2.2f*slow*Time.deltaTime;
            float range=Vector3.Distance(beast.position,game.World.NetMount.position);
            if(range<33&&Time.time>lastShot+3)
            {
                lastShot=Time.time;netUntil=Time.time+2.6f;progress+=1;
                net.SetPosition(0,game.World.NetMount.position+Vector3.up);net.SetPosition(1,beast.position+Vector3.up*.3f);
                glowUntil=Time.time+.7f;
                game.Audio.PlayCue(false);
            }
            net.enabled=Time.time<glowUntil;
            Status="掠食兽逼近 · 缆网阻滞 "+progress.ToString("0")+" / 4 · 近船探测可扰动它";
            if(progress>=4)Finish(true);
            else if(delta.magnitude<4)Finish(false);
        }
        public void OnScan(Vector3 center,float radius)
        {
            if(!Active||Time.time<warningUntil||Vector3.Distance(center,beast.position)>Mathf.Min(radius,18))return;
            netUntil=Time.time+2;progress+=.5f;
            game.Notify("近距探测扰动目标，为网台争取了时间。");
        }
        void Finish(bool success)
        {
            Active=false;
            if(success)game.Notify("掠食兽已被驱离，渔获浮台保住了。可以继续出航。");
            else{game.progress.parts=Mathf.Max(0,game.progress.parts-2);game.Save();game.Notify("浮台损失 2 构件。村庄仍能维修和出航，演练可重新尝试。");}
            Destroy(beast.gameObject);Destroy(net.gameObject);Status="";
        }
    }
}
