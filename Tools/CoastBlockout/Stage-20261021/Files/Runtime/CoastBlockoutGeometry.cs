using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    public static class CoastBlockoutGeometry
    {
        sealed class Shape
        {
            public readonly List<Vector3> v=new List<Vector3>();
            public readonly List<int> t=new List<int>();
            public void Face(Vector3 a,Vector3 b,Vector3 c)
            {int k=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(k);t.Add(k+1);t.Add(k+2);}
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {Face(a,b,c);Face(a,c,d);}
            public Mesh Mesh(string name)
            {var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
        static Transform Group(string name,Transform parent)
        {var o=new GameObject(name);o.transform.SetParent(parent,false);return o.transform;}
        static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material,bool collision)
        {var o=new GameObject(name);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;if(collision)o.AddComponent<MeshCollider>().sharedMesh=mesh;return o;}
        static Transform Box(string name,Transform parent,Vector3 center,Vector3 size,Material material,bool collision=false)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=center;o.transform.localScale=size;o.GetComponent<MeshRenderer>().sharedMaterial=material;if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o.transform;}
        static Transform Cylinder(string name,Transform parent,Vector3 center,float radius,float height,Material mat,bool collision=false)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=center;o.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);o.GetComponent<MeshRenderer>().sharedMaterial=mat;if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o.transform;}

        public static CoastBlockoutRegion Build(Transform parent,Material clay,Material water)
        {
            var root=Group("东岸白模 · 共用地形资源",parent);
            var region=root.gameObject.AddComponent<CoastBlockoutRegion>();region.clay=clay;
            var terrain=Group("连续岩岸与海底",root);
            const float step=.65f;const int nx=218,nz=172;
            // Four chunks, full 3D geometry. The submerged shelves are never a background image.
            for(int cz=0;cz<2;cz++)for(int cx=0;cx<2;cx++)
            {
                var s=new Shape();int x0=cx*(nx/2),x1=(cx+1)*(nx/2),z0=cz*(nz/2),z1=(cz+1)*(nz/2);
                for(int z=z0;z<z1;z++)for(int x=x0;x<x1;x++)
                {
                    Vector3 a=Point(x,z,step),b=Point(x,z+1,step),c=Point(x+1,z+1,step),d=Point(x+1,z,step);
                    s.Quad(a,b,c,d);
                }
                var mesh=s.Mesh("地形块-"+cx+"-"+cz);region.terrainTriangles+=mesh.triangles.Length/3;
                MeshObject(mesh.name,terrain,mesh,clay,true);
            }
            var cliffs=Group("连接成岸壁的岩体",root);var rng=new System.Random(103119);
            Rock("灯塔岬角前断面",cliffs,new Vector3(26.5f,-1.3f,89.5f),3.7f,3.7f,5.55f,621,clay,true);region.rockMasses++;
            Rock("灯塔岬角侧断面",cliffs,new Vector3(31.5f,-1.3f,88.7f),3.4f,3.2f,5.45f,622,clay,true);region.rockMasses++;
            float[] coastZ={58,65,72,79,85,104,111,119,126,134,143};
            for(int i=0;i<coastZ.Length;i++)
            {
                float z=coastZ[i],x=CoastBlockoutLayout.Shore(z)-3.6f;
                float rx=3.7f+(float)rng.NextDouble()*1.3f,rz=4.3f+(float)rng.NextDouble()*1.6f;
                float top=3.1f+(float)rng.NextDouble()*1.7f;
                if(CoastBlockoutLayout.PathDistance(new Vector3(x,0,z))<rx+2)rx*=.66f;
                Rock("岩岸断面-"+i,cliffs,new Vector3(x,-2.3f,z),rx,rz,top+2.3f,700+i,clay,true);region.rockMasses++;
                Rock("岸脚碎岩-"+i,cliffs,new Vector3(x+rx*.72f,-1.3f,z+rz*.55f),1.5f,1.85f,2.1f,900+i,clay,true);region.rockMasses++;
            }
            var reefs=Group("有侧壁的礁台与深沟",root);
            foreach(var rock in CoastBlockoutLayout.WaterRocks)
            {MeshObject(rock.name,reefs,rock.CreateMesh(),clay,true);region.rockMasses++;}
            var details=Group("低岸与岩台连接",root);
            for(int i=0;i<36;i++)
            {
                float z=55+i*2.5f,x=CoastBlockoutLayout.Shore(z)+.3f+Mathf.Sin(i*2.3f)*2;
                var p=new Vector3(x,CoastBlockoutLayout.Height(x,z)-.30f,z);
                if(CoastBlockoutLayout.PathDistance(p)<3.4f||Mathf.Abs(z-94)<4.3f)continue;
                float r=.45f+(float)rng.NextDouble()*.85f;
                Rock("岸脚落石-"+i,details,p,r,r*1.2f,.6f+r*.5f,1800+i,clay,true);region.rockMasses++;
            }
            BuildPier(root,clay);BuildPath(root,clay);region.lighthouse=BuildTower(root,clay);
            Group("穿洞灰模鱼群 · 路线验证",root).gameObject.AddComponent<CoastBlockoutSchool>();
            var waterShape=new Shape();waterShape.Quad(new Vector3(-160,-.22f,-80),new Vector3(-160,-.22f,300),new Vector3(280,-.22f,300),new Vector3(280,-.22f,-80));
            region.water=MeshObject("技术水面 · F9隐藏",root,waterShape.Mesh("技术水面"),water,false).transform;
            region.water.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            region.referenceFocus=Anchor("参考机位焦点",root,CoastBlockoutLayout.Focus);
            region.landingFeet=Anchor("东岸登岸点 · 原坐标",root,CoastBlockoutLayout.Feet);
            region.berth=Anchor("东岸船泊位 · 原坐标",root,CoastBlockoutLayout.Berth);
            return region;
        }
        static Vector3 Point(int x,int z,float step)
        {float wx=x*step,wz=42+z*step;return new Vector3(wx,CoastBlockoutLayout.TerrainHeight(wx,wz),wz);}
        static Transform Anchor(string name,Transform root,Vector3 p){var t=Group(name,root);t.localPosition=p;return t;}

        static void Rock(string name,Transform parent,Vector3 p,float rx,float rz,float height,int seed,Material mat,bool collide)
        {
            var rock=rx>1.3f&&height>1.7f?CoastBlockoutRock.Scanned(name,p,rx,rz,height,seed):new CoastBlockoutRock(name,p,rx,rz,height,seed);
            MeshObject(name,parent,rock.CreateMesh(),mat,collide);
        }
        static void BuildPier(Transform parent,Material mat)
        {
            var p=Group("东岸栈桥 · 原登陆位置",parent);
            for(int i=0;i<25;i++)Box("桥面板-"+i,p,new Vector3(35.5f+i*.60f,.59f,94),new Vector3(.575f,.22f,3.15f),mat,true);
            for(int side=-1;side<=1;side+=2)
            {
                Box("纵梁",p,new Vector3(42.7f,.24f,94+side*1.15f),new Vector3(15.3f,.35f,.24f),mat,true);
                for(int i=0;i<6;i++)
                {
                    float x=36.3f+i*2.7f,z=94+side*1.32f,bed=CoastBlockoutLayout.Height(x,z)-.25f;
                    float top=1.38f,bottom=Mathf.Min(-.6f,bed);
                    Cylinder("落底立柱",p,new Vector3(x,(top+bottom)*.5f,z),.18f,top-bottom,mat,true);
                }
            }
            Box("岸侧过渡板",p,new Vector3(34.7f,.64f,94),new Vector3(1.7f,.12f,3.3f),mat,true);
        }
        static void BuildPath(Transform parent,Material mat)
        {
            var root=Group("码头至灯塔步道",parent);
            for(int i=2;i<CoastBlockoutLayout.Walk.Length;i++)
            {
                Vector3 a=CoastBlockoutLayout.Walk[i-1],b=CoastBlockoutLayout.Walk[i];
                int count=Mathf.Max(2,Mathf.CeilToInt((b.y-a.y)/.17f));Vector3 direction=b-a;direction.y=0;
                float length=direction.magnitude;Quaternion rotation=Quaternion.LookRotation(direction);
                for(int j=0;j<count;j++)
                {
                    float t=(j+.5f)/count;Vector3 q=Vector3.Lerp(a,b,t);q.y=Mathf.Lerp(a.y,b.y,(j+1f)/count)-.10f;
                    var step=Box("石阶",root,q,new Vector3(2.25f,.18f,length/count+.03f),mat,true);step.localRotation=rotation;
                }
                for(int j=0;j<=2;j++)
                {
                    Vector3 q=Vector3.Lerp(a,b,j/2f)+rotation*Vector3.right*1.25f;
                    Cylinder("步道护栏柱",root,q+Vector3.up*.52f,.055f,1.05f,mat);
                }
                Vector3 side=rotation*Vector3.right*1.25f;
                var rail=Box("步道护栏横杆",root,(a+b)*.5f+side+Vector3.up*.95f,new Vector3(.08f,.08f,(b-a).magnitude),mat);
                rail.localRotation=Quaternion.LookRotation(b-a);
            }
        }
        static Transform BuildTower(Transform parent,Material mat)
        {
            var t=Group("灯塔比例体 · 参考图位置",parent);t.localPosition=CoastBlockoutLayout.Tower;t.localScale=Vector3.one*.9f;
            Cylinder("台基",t,new Vector3(0,.18f,0),2.4f,.36f,mat,true);
            Cylinder("塔身",t,new Vector3(0,3.6f,0),1.35f,6.5f,mat,true);
            Cylinder("灯室基座",t,new Vector3(0,6.9f,0),1.7f,.34f,mat);
            Cylinder("灯室",t,new Vector3(0,7.55f,0),1.13f,1.0f,mat);
            Cylinder("塔顶",t,new Vector3(0,8.15f,0),1.45f,.24f,mat);
            var dome=GameObject.CreatePrimitive(PrimitiveType.Sphere);dome.name="灯塔穹顶比例体";dome.transform.SetParent(t,false);dome.transform.localPosition=new Vector3(0,8.35f,0);dome.transform.localScale=new Vector3(2.5f,.70f,2.5f);dome.GetComponent<MeshRenderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(dome.GetComponent<Collider>());
            for(int i=0;i<16;i++)
            {float a=i*Mathf.PI/8;Cylinder("灯室围栏柱",t,new Vector3(Mathf.Cos(a)*1.58f,7.35f,Mathf.Sin(a)*1.58f),.035f,.75f,mat);}
            Box("塔门比例体",t,new Vector3(0,1.07f,-1.35f),new Vector3(.7f,1.55f,.10f),mat);
            return t;
        }
        public static GameObject BuildBoat(Material mat)
        {
            var root=Group("参考小船 · 灰模",null);var s=new Shape();
            float[] z={-2.0f,-1.45f,-.6f,.7f,1.55f,2.15f};float[] widths={.55f,.75f,.89f,.82f,.52f,.03f};
            for(int side=-1;side<=1;side+=2)for(int i=0;i<z.Length-1;i++)
            {
                Vector3 a=new Vector3(side*widths[i],.70f,z[i]),b=new Vector3(side*widths[i+1],.70f,z[i+1]);
                Vector3 c=new Vector3(side*widths[i+1]*.68f,-.05f,z[i+1]),d=new Vector3(side*widths[i]*.68f,-.05f,z[i]);
                if(side==1)s.Quad(a,b,c,d);else s.Quad(d,c,b,a);
                var ia=a-Vector3.right*side*.10f;var ib=b-Vector3.right*side*.10f;var ic=c-Vector3.right*side*.10f;var id=d-Vector3.right*side*.10f;
                if(side==1){s.Quad(ia,id,ic,ib);s.Quad(a,ia,ib,b);}
                else{s.Quad(ib,ic,id,ia);s.Quad(b,ib,ia,a);}
            }
            s.Quad(new Vector3(-widths[0],.70f,z[0]),new Vector3(widths[0],.70f,z[0]),new Vector3(widths[0]*.68f,-.05f,z[0]),new Vector3(-widths[0]*.68f,-.05f,z[0]));
            s.Quad(new Vector3(-widths[0]*.68f,-.05f,z[0]+.10f),new Vector3(widths[0]*.68f,-.05f,z[0]+.10f),new Vector3(widths[0]-.10f,.70f,z[0]+.10f),new Vector3(-widths[0]+.10f,.70f,z[0]+.10f));
            s.Quad(new Vector3(-widths[0],.70f,z[0]),new Vector3(-widths[0]+.10f,.70f,z[0]+.10f),new Vector3(widths[0]-.10f,.70f,z[0]+.10f),new Vector3(widths[0],.70f,z[0]));
            for(int i=0;i<z.Length-1;i++)
                s.Quad(new Vector3(-widths[i]*.68f,.04f,z[i]),new Vector3(-widths[i+1]*.68f,.04f,z[i+1]),new Vector3(widths[i+1]*.68f,.04f,z[i+1]),new Vector3(widths[i]*.68f,.04f,z[i]));
            MeshObject("船壳",root,s.Mesh("开口船壳"),mat,false);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<z.Length-1;i++)
            {
                var a=new Vector3(side*widths[i],.73f,z[i]);var b=new Vector3(side*widths[i+1],.73f,z[i+1]);
                var rail=Box("连续船舷",root,(a+b)*.5f,new Vector3(.12f,.12f,(b-a).magnitude+.035f),mat);rail.localRotation=Quaternion.LookRotation(b-a);
            }
            for(int i=0;i<3;i++)Box("横座",root,new Vector3(0,.37f,-1.1f+i*.95f),new Vector3(1.23f,.075f,.32f),mat);
            Cylinder("船中人物",root,new Vector3(0,.94f,.10f),.18f,.83f,mat);
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="人物头部";head.transform.SetParent(root,false);head.transform.localPosition=new Vector3(0,1.52f,.10f);head.transform.localScale=Vector3.one*.34f;head.GetComponent<MeshRenderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            return root.gameObject;
        }
    }
}
