using UnityEngine;

namespace Deepsea
{
    public static class IslandTerrain
    {
        public static readonly Vector3 Center=new Vector3(8,0,94);
        public static float Height(float x,float z)
        {
            return CoastBlockoutSession.Active&&CoastBlockoutLayout.Contains(x,z)?CoastBlockoutLayout.Height(x,z):BaseHeight(x,z);
        }
        public static float BaseHeight(float x,float z)
        {
            float dx=(x-Center.x)/38f,dz=(z-Center.z)/42f;
            float radius=Mathf.Sqrt(dx*dx+dz*dz);
            float coast=Mathf.Clamp01((1-radius)/.17f);
            float hill=Mathf.Exp(-((x+2)*(x+2)/210f+(z-105)*(z-105)/260f))*3.1f;
            // Preserve walkable land; continue the submerged slope beyond the old rectangle.
            float underwater=-1.3f-10.7f*Mathf.SmoothStep(0,1,Mathf.Clamp01((radius-1)/.85f));
            return Mathf.Lerp(underwater,.85f+hill,coast)+NaturalCoast.LandRelief(x,z);
        }
        public static Vector3 Point(float x,float z){return new Vector3(x,Height(x,z)+.03f,z);}
        public static bool Contains(Vector3 p)
        {float dx=(p.x-Center.x)/38f,dz=(p.z-Center.z)/42f;return dx*dx+dz*dz<1;}
        public static Transform Build(Transform parent)
        {
            const int cells=128;int side=cells+1;
            Vector3[] v=new Vector3[side*side];int[] indices=new int[cells*cells*6];
            for(int z=0;z<side;z++)for(int x=0;x<side;x++)
            {float wx=Center.x-40+x*80f/cells,wz=Center.z-44+z*88f/cells;v[z*side+x]=new Vector3(wx,Height(wx,wz),wz);}
            int k=0;
            for(int z=0;z<cells;z++)for(int x=0;x<cells;x++)
            {int i=z*side+x;indices[k++]=i;indices[k++]=i+side;indices[k++]=i+1;indices[k++]=i+1;indices[k++]=i+side;indices[k++]=i+side+1;}
            Mesh mesh=new Mesh{name="雾岬岛海岸与缓坡"};mesh.vertices=v;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();
            Transform root=VisualFactory.MeshObject("雾岬岛 · 可步行地形",mesh,parent,new Color(.30f,.37f,.32f)).transform;
            root.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
            return root;
        }

        public static Transform Ramp(Transform parent,Vector3 a,Vector3 b,float width)
        {
            Vector3 side=Vector3.Cross((b-a).normalized,Vector3.up)*width*.5f;
            Mesh m=new Mesh{name="可见缓坡"};m.vertices=new[]{a-side,a+side,b-side,b+side};
            // The side vector points left of the ramp. Wind the top upward so
            // the one-sided mesh collider supports feet approaching from above.
            m.triangles=new[]{0,1,2,1,3,2};m.RecalculateNormals();m.RecalculateBounds();
            Transform t=VisualFactory.MeshObject("木板缓坡",m,parent,VisualFactory.PaleWood).transform;
            t.gameObject.AddComponent<MeshCollider>().sharedMesh=m;return t;
        }
    }

    public sealed class LandingPoint
    {
        public string name;
        public Vector3 berth,feet;
        public bool village;
    }
    public enum IslandSiteKind { Station, Receiver, Net, Survivor, Boat, Workshop, Supplies, Warehouse, HighLookout }
    public sealed class IslandSite
    {
        public string name;
        public IslandSiteKind kind;
        public Vector3 position;
        public Transform visual;
    }
}
