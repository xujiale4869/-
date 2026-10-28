using UnityEngine;

namespace Deepsea
{
    public sealed class ShoreWildlife : MonoBehaviour
    {
        public ExpeditionController expedition;
        Transform beast,bait;
        Vector3 foodPoint;
        float fedUntil;
        public bool GuardingNet { get { return beast!=null&&Vector3.Distance(beast.position,IslandTerrain.Point(-13,93))<4.5f; } }
        public string State { get { return Time.time<fedUntil?"礁背兽转向食物":GuardingNet?"礁背兽在潮池附近警戒":"礁背兽沿潮池觅食"; } }
        void Start()
        {
            beast=VisualFactory.Group("礁背兽",expedition.game.World.Root,IslandTerrain.Point(-16,93));
            VisualFactory.Sphere("湿润背甲",beast,new Vector3(0,.45f,0),new Vector3(1.7f,.9f,2.2f),new Color(.24f,.36f,.34f));
            VisualFactory.Sphere("头",beast,new Vector3(0,.32f,1.1f),new Vector3(.7f,.55f,.7f),new Color(.32f,.4f,.35f));
            for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)
                VisualFactory.Sphere("短足",beast,new Vector3(i*.75f,.18f,j*.6f),new Vector3(.28f,.26f,.38f),VisualFactory.Rock);
            CapsuleCollider collider=beast.gameObject.AddComponent<CapsuleCollider>();collider.height=.9f;collider.radius=.65f;collider.center=new Vector3(0,.45f,0);
        }
        void Update()
        {
            if(beast==null||expedition.game.Paused)return;
            Vector3 target=Time.time<fedUntil?foodPoint:IslandTerrain.Point(-16+Mathf.Sin(Time.time*.13f)*2,93+Mathf.Cos(Time.time*.1f)*2);
            Vector3 delta=target-beast.position;delta.y=0;
            if(delta.sqrMagnitude>.15f)
            {beast.rotation=Quaternion.Slerp(beast.rotation,Quaternion.LookRotation(delta),Time.deltaTime*2);Vector3 pos=Vector3.MoveTowards(beast.position,target,.95f*Time.deltaTime);pos.y=IslandTerrain.Height(pos.x,pos.z)+.05f;beast.position=pos;}
            if(bait!=null&&Time.time>fedUntil){Destroy(bait.gameObject);bait=null;}
        }
        public void Lure(Vector3 point)
        {
            if(!IslandTerrain.Contains(point))return;
            foodPoint=point;foodPoint.y=IslandTerrain.Height(point.x,point.z)+.1f;fedUntil=Time.time+24;
            if(bait!=null)Destroy(bait.gameObject);
            bait=VisualFactory.Sphere("投放的食物",expedition.game.World.Root,foodPoint,new Vector3(.4f,.2f,.4f),VisualFactory.Rope);
        }
    }
}
