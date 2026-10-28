using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    // Visible mesh, collider and vertical depth query share these exact triangles.
    public sealed class CoastBlockoutRock
    {
        public readonly string name;
        public readonly Vector3 position;
        public readonly float radiusX,radiusZ,height;
        public readonly int seed;
        readonly Vector3[] vertices,explicitNormals;
        readonly int[] triangles;
        readonly float minX,maxX,minZ,maxZ;
        public CoastBlockoutRock(string label,Vector3 p,float rx,float rz,float h,int randomSeed)
            :this(label,p,rx,rz,h,randomSeed,RockVertices(p,rx,rz,h,randomSeed)) {}
        CoastBlockoutRock(string label,Vector3 p,float rx,float rz,float h,int randomSeed,Vector3[] faces,Vector3[] normals=null)
        {
            name=label;position=p;radiusX=rx;radiusZ=rz;height=h;seed=randomSeed;
            vertices=faces;explicitNormals=normals;triangles=new int[vertices.Length];
            minX=minZ=float.PositiveInfinity;maxX=maxZ=float.NegativeInfinity;
            for(int i=0;i<vertices.Length;i++)
            {triangles[i]=i;minX=Mathf.Min(minX,vertices[i].x);maxX=Mathf.Max(maxX,vertices[i].x);minZ=Mathf.Min(minZ,vertices[i].z);maxZ=Mathf.Max(maxZ,vertices[i].z);}
        }
        static Vector3[] scannedVertices,scannedNormals;static int[] scannedIndices;
        public static CoastBlockoutRock Scanned(string label,Vector3 p,float rx,float rz,float h,int seed)
        {
            if(scannedVertices==null)
            {
                var source=Resources.Load<Mesh>("CoastBlockout/ScannedRock");
                if(source==null)throw new InvalidOperationException("Run the whitebox builder to prepare the existing scanned rock");
                scannedVertices=source.vertices;scannedNormals=source.normals;scannedIndices=source.triangles;
            }
            var faces=new Vector3[scannedIndices.Length];var normals=new Vector3[faces.Length];float yaw=seed*Mathf.Deg2Rad,cs=Mathf.Cos(yaw),sn=Mathf.Sin(yaw);
            for(int i=0;i<faces.Length;i++)
            {
                Vector3 v=scannedVertices[scannedIndices[i]];
                // Rotate in normalized space so requested world bounds remain useful.
                float x=cs*v.x+sn*v.z,z=-sn*v.x+cs*v.z;
                faces[i]=p+new Vector3(x*rx,v.y*h,z*rz);
                var n=scannedNormals[scannedIndices[i]];normals[i]=new Vector3((cs*n.x+sn*n.z)/rx,n.y/h,(-sn*n.x+cs*n.z)/rz).normalized;
            }
            return new CoastBlockoutRock(label+" · 扫描形体",p,rx,rz,h,seed,faces,normals);
        }
        static Vector3[] RockVertices(Vector3 p,float rx,float rz,float h,int seed)
        {
            var rng=new System.Random(seed);int sides=11+seed%5;
            float[] levels={0,.18f,.40f,.69f,.91f,1},widths={1.15f,1.10f,.97f,1.03f,.96f,.91f};
            var ring=new Vector3[levels.Length,sides];
            float[] radii=new float[sides];for(int j=0;j<sides;j++)radii[j]=.87f+(float)rng.NextDouble()*.27f;
            float jitter=Mathf.Min(.20f,h*.025f),phase=(float)rng.NextDouble()*6;
            float yaw=.16f*Mathf.Sin(seed*.7f),cy=Mathf.Cos(yaw),sy=Mathf.Sin(yaw);
            for(int k=0;k<levels.Length;k++)for(int j=0;j<sides;j++)
            {
                float a=j*Mathf.PI*2/sides+phase,f=radii[j]*widths[k];
                float dx=Mathf.Sin(phase)*rx*levels[k]*.26f+Mathf.Sin(k*1.7f+phase)*rx*.035f,dz=Mathf.Cos(phase)*rz*levels[k]*.2f+Mathf.Cos(k*1.2f+phase)*rz*.03f;
                // Angular, broad fracture faces instead of a repeated circular cylinder.
                float u=Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),.52f)*rx*f;
                float w=Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),.52f)*rz*f;
                float tilt=levels[k]*(u/rx*.22f+w/rz*.13f)*Mathf.Sin(phase);
                ring[k,j]=p+new Vector3(cy*u+sy*w+dx,levels[k]*h+tilt+(k>0?Mathf.Sin(j*1.9f+seed)*jitter*(k==levels.Length-1?.12f:1):0),-sy*u+cy*w+dz);
            }
            var v=new List<Vector3>();
            Action<Vector3,Vector3,Vector3> face=(a,b,c)=>{v.Add(a);v.Add(b);v.Add(c);};
            for(int k=0;k<levels.Length-1;k++)for(int j=0;j<sides;j++)
            {int n=(j+1)%sides;face(ring[k,j],ring[k+1,j],ring[k+1,n]);face(ring[k,j],ring[k+1,n],ring[k,n]);}
            Vector3 capCenter=Vector3.zero;for(int j=0;j<sides;j++)capCenter+=ring[levels.Length-1,j]/sides;
            for(int j=0;j<sides;j++)face(capCenter,ring[levels.Length-1,(j+1)%sides],ring[levels.Length-1,j]);
            return v.ToArray();
        }
        public static CoastBlockoutRock Passage(string label,Vector3 floor,Vector3 axis,int seed)
        {
            // A fractured cleft with an asymmetric ceiling, not a semicircular doorway.
            // Every section stays open; the shell includes front rim, inner walls and roof.
            Vector3 side=Vector3.Cross(Vector3.up,axis).normalized;
            Vector2[] outside={new Vector2(7,-.9f),new Vector2(7.1f,3),new Vector2(6.3f,6.5f),new Vector2(5.2f,9.4f),new Vector2(3,11.8f),new Vector2(-.4f,12.2f),new Vector2(-3.7f,11.2f),new Vector2(-6.5f,8.9f),new Vector2(-6.4f,4.9f),new Vector2(-6.8f,-.9f)};
            Vector2[] opening={new Vector2(4,-.7f),new Vector2(3.6f,2),new Vector2(3.2f,5),new Vector2(2.6f,7.3f),new Vector2(1.3f,8.2f),new Vector2(-.7f,8.7f),new Vector2(-2,7.6f),new Vector2(-3,6.8f),new Vector2(-3.2f,3.8f),new Vector2(-3.9f,-.7f)};
            float[] depths={-4.4f,-2.5f,-.4f,2,4.4f};
            int count=outside.Length,sections=depths.Length;
            var outer=new Vector3[sections,count];var inner=new Vector3[sections,count];
            for(int k=0;k<sections;k++)for(int j=0;j<count;j++)
            {
                float widen=1+.065f*Mathf.Sin(k*2.4f+j*1.7f),shift=.22f*Mathf.Sin(k*1.5f);
                outer[k,j]=floor+axis*depths[k]+side*(outside[j].x*widen+shift)+Vector3.up*(outside[j].y+.24f*Mathf.Sin(j*2.1f+k));
                inner[k,j]=floor+axis*depths[k]+side*(opening[j].x*1.1f*(1+.045f*Mathf.Sin(j+k))+shift)+Vector3.up*(opening[j].y*.82f+.12f*Mathf.Sin(k+j));
            }
            var v=new List<Vector3>();
            Action<Vector3,Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d,normal)=>
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var q=b;b=d;d=q;}
                v.Add(a);v.Add(b);v.Add(c);v.Add(a);v.Add(c);v.Add(d);
            };
            for(int j=0;j<count-1;j++)
            {
                quad(outer[0,j],outer[0,j+1],inner[0,j+1],inner[0,j],-axis);
                quad(outer[sections-1,j],inner[sections-1,j],inner[sections-1,j+1],outer[sections-1,j+1],axis);
                for(int k=0;k<sections-1;k++)
                {
                    Vector3 radial=(outer[k,j]+outer[k,j+1])*.5f-floor-axis*depths[k]-Vector3.up*3;
                    quad(outer[k,j],outer[k+1,j],outer[k+1,j+1],outer[k,j+1],radial);
                    quad(inner[k,j],inner[k,j+1],inner[k+1,j+1],inner[k+1,j],-radial);
                }
            }
            for(int k=0;k<sections-1;k++)foreach(int j in new[]{0,count-1})
                quad(outer[k,j],inner[k,j],inner[k+1,j],outer[k+1,j],Vector3.down);
            return new CoastBlockoutRock(label,floor,7,7,12,seed,v.ToArray());
        }
        public float HeightAt(float x,float z)
        {
            if(x<minX||x>maxX||z<minZ||z>maxZ)return float.NegativeInfinity;
            float top=float.NegativeInfinity;
            for(int i=0;i<vertices.Length;i+=3)
            {
                var a=vertices[i];var b=vertices[i+1];var c=vertices[i+2];
                float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                if(Mathf.Abs(den)<.000001f)continue;
                float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/den;
                float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/den;
                if(u>=-.00001f&&v>=-.00001f&&u+v<=1.00001f)top=Mathf.Max(top,u*a.y+v*b.y+(1-u-v)*c.y);
            }
            return top;
        }
        public Mesh CreateMesh()
        {
            var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.vertices=vertices;m.triangles=triangles;if(explicitNormals!=null)m.normals=explicitNormals;else m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
}
