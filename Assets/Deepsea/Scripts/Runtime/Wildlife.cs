using System.Collections.Generic;
using UnityEngine;

namespace Deepsea
{
    public sealed class Wildlife : MonoBehaviour
    {
        public GameDirector game;
        readonly List<Transform> fish = new List<Transform>();
        readonly Vector3 center = new Vector3(58, .18f, 61);
        void Start()
        {
            Vector2[] shape={new Vector2(0,.6f),new Vector2(.16f,.15f),new Vector2(.1f,-.25f),new Vector2(.22f,-.5f),new Vector2(-.22f,-.5f),new Vector2(-.1f,-.25f),new Vector2(-.16f,.15f)};
            Mesh mesh=VisualFactory.Prism(shape,0,.035f);
            for(int i=0;i<21;i++)
            {
                Transform f=VisualFactory.MeshObject("荧鳞鱼",mesh,transform,new Color(.3f,.48f,.48f)).transform;
                f.localScale=Vector3.one*(.7f+i%4*.13f);
                fish.Add(f);
            }
        }
        void Update()
        {
            for(int i=0;i<fish.Count;i++)
            {
                float t=Time.time*.13f+i*.31f, r=4+i%7*.7f;
                Vector3 position=center+new Vector3(Mathf.Sin(t)*r,0,Mathf.Cos(t*.8f)*r*.8f);
                position+=new Vector3(Mathf.Sin(t*3+i)*.25f,0,Mathf.Cos(t*2+i)*.25f);
                Vector3 forward=new Vector3(Mathf.Cos(t)*r,0,-Mathf.Sin(t*.8f)*r*.64f);
                fish[i].position=position;
                if(forward.sqrMagnitude>.001f)fish[i].rotation=Quaternion.LookRotation(forward);
                fish[i].gameObject.SetActive(Vector3.Distance(game.Boat.transform.position,center)<55||game.CameraFollow.Overview);
            }
        }
    }

    public sealed class BoatWake : MonoBehaviour
    {
        public BoatController boat;
        struct Ripple{public Vector3 center;public float born;public float yaw;}
        readonly Ripple[] history=new Ripple[26];
        readonly LineRenderer[] lines=new LineRenderer[26];
        int next;
        float emitAt;
        void Start()
        {
            for(int i=0;i<lines.Length;i++)
            {lines[i]=VisualFactory.Line("船尾波",boat.game.World.Root,.045f,new Color(.38f,.51f,.51f,.2f));lines[i].positionCount=9;lines[i].enabled=false;history[i].born=-100;}
        }
        void Update()
        {
            if(boat.Speed>1&&Time.time>emitAt)
            {
                emitAt=Time.time+.16f;
                history[next]=new Ripple{center=boat.stern.position,born=Time.time,yaw=boat.transform.eulerAngles.y};
                next=(next+1)%history.Length;
            }
            for(int i=0;i<lines.Length;i++)
            {
                float age=Time.time-history[i].born;
                lines[i].enabled=age<3.5f;
                if(!lines[i].enabled)continue;
                float scale=.7f+age*.65f;
                for(int j=0;j<9;j++)
                {
                    float a=Mathf.Lerp(-1.15f,1.15f,j/8f);
                    Vector3 v=new Vector3(Mathf.Sin(a)*scale,.15f,-Mathf.Cos(a)*scale);
                    lines[i].SetPosition(j,history[i].center+Quaternion.Euler(0,history[i].yaw,0)*v);
                }
                Color c=new Color(.46f,.6f,.58f,(1-age/3.5f)*.22f);
                lines[i].startColor=lines[i].endColor=c;
            }
        }
    }
}
