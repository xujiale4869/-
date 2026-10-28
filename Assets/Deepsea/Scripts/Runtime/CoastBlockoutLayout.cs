using UnityEngine;
using System.Collections.Generic;

namespace Deepsea
{
    // Shared source for the prefab geometry, island collision and sea depth adapter.
    // Coordinates remain in the original east-coast world space.
    public static class CoastBlockoutLayout
    {
        public static readonly Vector3 Feet = new Vector3(46, .74f, 94);
        public static readonly Vector3 Berth = new Vector3(53, 0, 94);
        public static readonly Vector3 Focus = new Vector3(52, -.5f, 94);
        public static readonly Vector3 Tower = new Vector3(30, 4.3f, 93);
        public static readonly Vector3[] Walk = {
            new Vector3(46,.74f,94), new Vector3(35.5f,.74f,94),
            new Vector3(33,1.30f,95), new Vector3(31,2.40f,96),
            new Vector3(28,3.30f,97), new Vector3(27,4.30f,95)
        };

        static float S(float a, float b, float v) { return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, v)); }
        static float Bell(float v, float c, float r) { return Mathf.Exp(-Mathf.Pow((v-c)/r, 4)); }
        public static bool Contains(float x, float z) { return x >= 0 && x <= 142 && z >= 42 && z <= 154; }
        public static float Shore(float z)
        {
            return 35.5f + 2.5f*Mathf.Sin((z-82)*.083f) + 1.25f*Mathf.Sin(z*.23f)
                - 6*S(116,138,z) - 5*(1-S(48,67,z))
                -3.4f*Bell(z,79,5.8f)+2.0f*Bell(z,86,3.4f)
                -2.9f*Bell(z,108,5.5f)+1.6f*Bell(z,118,4.5f);
        }
        // Deliberate large bends, asymmetric banks and pinches; small noise cannot replace these.
        static readonly Vector2[] nearBank={new Vector2(42,68),new Vector2(58,66),new Vector2(73,58),new Vector2(82,56.5f),new Vector2(96,65),new Vector2(105,66),new Vector2(118,59),new Vector2(133,63),new Vector2(154,67)};
        static readonly Vector2[] farBank={new Vector2(42,94),new Vector2(60,89),new Vector2(77,86),new Vector2(88,80),new Vector2(100,84),new Vector2(114,85),new Vector2(128,80),new Vector2(141,88),new Vector2(154,92)};
        static float Bank(Vector2[] points,float z)
        {
            for(int i=1;i<points.Length;i++)if(z<=points[i].x)
                return Mathf.Lerp(points[i-1].y,points[i].y,S(points[i-1].x,points[i].x,z));
            return points[points.Length-1].y;
        }
        public static float Spine(float z) { return Bank(nearBank,z); }
        public static float FarSpine(float z) { return Bank(farBank,z); }
        public static readonly Vector3 CaveFloor=new Vector3(74.5f,-16.8f,104);
        public static readonly Vector3 CaveAxis=new Vector3(.309f,0,-.951f).normalized;
        public static readonly Vector3 CaveFocus=CaveFloor+Vector3.up*5;
        static List<CoastBlockoutRock> waterRocks;
        public static List<CoastBlockoutRock> WaterRocks
        {
            get { if(waterRocks==null)waterRocks=BuildWaterRocks();return waterRocks; }
        }
        static List<CoastBlockoutRock> BuildWaterRocks()
        {
            var result=new List<CoastBlockoutRock>();var rng=new System.Random(174321);
            result.Add(CoastBlockoutRock.Scanned("前景岩台-左",new Vector3(43,-3.5f,65),5.5f,5.0f,8.0f,6201));
            result.Add(CoastBlockoutRock.Scanned("前景岩台-右",new Vector3(49,-4.5f,62),4.5f,5.0f,7.0f,6202));
            for(int i=0;i<17;i++)
            {
                float z=47+i*6.2f+Mathf.Sin(i*2.43f)*1.5f;
                float x=Spine(z)+Mathf.Sin(i*2.37f)*1.2f;
                float top=-1.25f-.68f*(i%4)-Bell(z,87,5)*3.6f-Bell(z,115,4)*2.2f;
                float rx=3.4f+(float)rng.NextDouble()*2.2f,rz=3.5f+(float)rng.NextDouble()*2.6f;
                if(i==9){x-=2.2f;rx*=.85f;} // Leave the cave approach physically clear.
                result.Add(CoastBlockoutRock.Scanned("中央礁脊-"+i,new Vector3(x,-19,z),rx,rz,19+top,1300+i));
                if(i==3||i==4||i==7||i==8||i==10||i==13)
                {
                    float capTop=top-.35f;
                    result.Add(CoastBlockoutRock.Scanned("错落侧礁台-"+i,new Vector3(x-rx*.58f,capTop-6.5f,z+rz*.48f),rx*.88f,rz*.76f,5.2f,3600+i));
                }
                // Lower shoulders break up a single sheer vertical extrusion.
                if(i%2==0)result.Add(CoastBlockoutRock.Scanned("礁脊侧阶-"+i,new Vector3(x+rx*.85f,-21,z-1.7f),rx*.82f,rz*.8f,11.5f+(i%3),3300+i));
                float farZ=z+1.3f,far=FarSpine(farZ)+Mathf.Sin(i*1.43f)*1.3f;
                // A real gap is reserved for the arch. No hidden solid rock may fill its opening.
                if(farZ<88||farZ>120)
                    result.Add(CoastBlockoutRock.Scanned("深沟对岸-"+i,new Vector3(far,-24,farZ),5.2f+(float)rng.NextDouble()*2.7f,3.5f+(float)rng.NextDouble()*2.0f,20.0f+(float)rng.NextDouble()*2.1f,1500+i));
            }
            result.Add(CoastBlockoutRock.Passage("鱼群贯通岩洞",CaveFloor,CaveAxis,4201));
            result.Add(CoastBlockoutRock.Scanned("岩洞外侧接岸",new Vector3(85,-24,106),4.0f,7,20.3f,4202));
            Vector3 caveSide=Vector3.Cross(Vector3.up,CaveAxis);
            result.Add(CoastBlockoutRock.Scanned("洞顶错断岩板",CaveFloor+Vector3.up*10.4f+caveSide*1.4f+CaveAxis*.7f,3.6f,3.5f,2.0f,4210));
            result.Add(CoastBlockoutRock.Scanned("洞顶右肩断岩",CaveFloor+Vector3.up*9.8f-caveSide*4.5f-CaveAxis*1.2f,2.9f,3.1f,2.7f,4211));
            foreach(float z in new[]{77f,84f,106f,113f})
            {
                float x=Shore(z)+2.7f,y=TerrainHeight(x,z)-.45f;
                result.Add(CoastBlockoutRock.Scanned("岸脚宽台-"+(int)z,new Vector3(x,y-1.0f,z),2.7f,3.3f,2.2f,4400+(int)z));
            }
            for(int i=0;i<105;i++)
            {
                float z=72+(float)rng.NextDouble()*44,x=Shore(z)+2.5f+(float)rng.NextDouble()*20;
                float floor=TerrainHeight(x,z);if(floor>-.95f||floor< -6.8f)continue;
                float r=.28f+(float)rng.NextDouble()*.72f;
                float h=Mathf.Min(.25f+(float)rng.NextDouble()*.60f,-.8f-floor+.12f);
                result.Add(new CoastBlockoutRock("浅滩碎岩-"+i,new Vector3(x,floor-.12f,z),r,r*(.8f+(float)rng.NextDouble()*.7f),h,2100+i));
            }
            return result;
        }
        public static float Height(float x,float z)
        {
            float h=TerrainHeight(x,z);
            if(x>Shore(z)-1)foreach(var rock in WaterRocks)h=Mathf.Max(h,rock.HeightAt(x,z));
            return h;
        }
        public static float TerrainHeight(float x, float z)
        {
            float d=Shore(z)-x;
            float grain=(Mathf.PerlinNoise(x*.17f,z*.17f)-.5f)*.18f;
            float h;
            if(d>=0)
            {
                float beach=.17f+Mathf.Min(d,3.1f)*.16f;
                float shelf=1.75f+Mathf.Sin(z*.13f)*.20f;
                float upper=4.3f+Mathf.Sin(z*.075f)*.5f;
                h=Mathf.Lerp(beach,shelf,S(3.2f,4.0f,d));
                h=Mathf.Lerp(h,upper,S(10.2f,11.0f,d));
                h+=grain;
                // Low bay cuts through the cliff at the original east landing.
                h=Mathf.Lerp(h,.70f+Mathf.Max(0,d-8)*.12f,Bell(z,94,7.5f)*(1-S(9,15,d)));
                float tower=Vector2.Distance(new Vector2(x,z),new Vector2(Tower.x,Tower.z));
                h=Mathf.Lerp(h,Tower.y,1-S(3.3f,5.4f,tower));
                float best=1000, path=0;
                for(int i=1;i<Walk.Length;i++)
                {
                    Vector2 a=new Vector2(Walk[i-1].x,Walk[i-1].z),b=new Vector2(Walk[i].x,Walk[i].z);
                    Vector2 delta=b-a;float t=Mathf.Clamp01(Vector2.Dot(new Vector2(x,z)-a,delta)/delta.sqrMagnitude);
                    float dist=Vector2.Distance(new Vector2(x,z),a+delta*t);
                    if(dist<best){best=dist;path=Mathf.Lerp(Walk[i-1].y,Walk[i].y,t)-.06f;}
                }
                h=Mathf.Lerp(h,path,1-S(1.45f,2.65f,best));
            }
            else
            {
                float water=-d;
                float edge=Spine(z)-4.8f;
                h=-.28f-water*.145f;
                h+=.95f*Bell(x,Shore(z)+10,5.5f)*Bell(z,78,8);
                h+=.75f*Bell(x,Shore(z)+14,6.0f)*Bell(z,108,8);
                h-=.85f*Bell(x,Shore(z)+17,3.5f)*Bell(z,88,7);
                h-=.65f*Bell(x,Shore(z)+11,6)*Bell(z,80,11);
                h+=.42f*Bell(x,Shore(z)+8,4)*Bell(z,105,8);
                h=Mathf.Lerp(h,-21.5f,S(edge-4,edge+10,x));
                h+=grain*.9f;
            }
            // A narrow seam band joins this fragment to the original terrain.
            float blend=S(0,8,x)*(1-S(134,142,x))*S(42,50,z)*(1-S(146,154,z));
            return Mathf.Lerp(IslandTerrain.BaseHeight(x,z),h,blend);
        }
        public static float PathDistance(Vector3 p)
        {
            float result=1000;
            for(int i=1;i<Walk.Length;i++)
            {
                Vector2 a=new Vector2(Walk[i-1].x,Walk[i-1].z),b=new Vector2(Walk[i].x,Walk[i].z),d=b-a;
                float t=Mathf.Clamp01(Vector2.Dot(new Vector2(p.x,p.z)-a,d)/d.sqrMagnitude);
                result=Mathf.Min(result,Vector2.Distance(new Vector2(p.x,p.z),a+d*t));
            }
            return result;
        }
    }
}
