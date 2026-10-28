using UnityEngine;

namespace Deepsea
{
    // Neutral low-cost fish proxies to inspect the through-opening, not final ecology.
    public sealed class CoastBlockoutSchool : MonoBehaviour
    {
        Mesh mesh;Vector3[] vertices;int[] indices;
        public const int Fish=22;
        const int PerFish=30;
        public int crossingCount {get;private set;}
        float previousPhase;
        void Start()
        {
            mesh=new Mesh{name="穿洞鱼群灰模"};mesh.MarkDynamic();
            vertices=new Vector3[Fish*PerFish];indices=new int[vertices.Length];
            for(int i=0;i<indices.Length;i++)indices[i]=i;
            mesh.vertices=vertices;mesh.triangles=indices;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial=GetComponentInParent<CoastBlockoutRegion>().clay;
        }
        public static Vector3 Path(float phase)
        {
            float a=phase*Mathf.PI*2;
            Vector3 axis=CoastBlockoutLayout.CaveAxis,side=Vector3.Cross(Vector3.up,axis);
            // A narrow ellipse passes through the physical tunnel twice per cycle.
            float lateral=.65f*Mathf.Sin(a);
            return CoastBlockoutLayout.CaveFocus+axis*(8.5f*Mathf.Cos(a))+side*lateral;
        }
        public static Vector3 FishPosition(float phase,int index,out Vector3 forward)
        {
            float t=phase-index*.004f;Vector3 p=Path(t);
            forward=(Path(t+.001f)-p).normalized;
            Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            return p+side*(Mathf.Sin(index*2.3f)*.55f)+Vector3.up*(Mathf.Cos(index*1.7f)*.45f);
        }
        void Update()
        {
            if(mesh==null)return;
            float phase=Mathf.Repeat(Time.time/24,1);
            if(previousPhase<.25f&&phase>=.25f||previousPhase<.75f&&phase>=.75f)crossingCount++;
            previousPhase=phase;
            for(int i=0;i<Fish;i++)
            {
                Vector3 forward;Vector3 p=FishPosition(phase,i,out forward);
                Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
                Vector3 nose=p+forward*.45f,tail=p-forward*.30f,left=p+side*.12f,right=p-side*.12f,top=p+Vector3.up*.13f,bottom=p-Vector3.up*.09f;
                int k=i*PerFish;
                vertices[k++]=nose;vertices[k++]=left;vertices[k++]=top;
                vertices[k++]=nose;vertices[k++]=top;vertices[k++]=right;
                vertices[k++]=nose;vertices[k++]=right;vertices[k++]=bottom;
                vertices[k++]=nose;vertices[k++]=bottom;vertices[k++]=left;
                vertices[k++]=tail;vertices[k++]=top;vertices[k++]=left;
                vertices[k++]=tail;vertices[k++]=right;vertices[k++]=top;
                vertices[k++]=tail;vertices[k++]=left;vertices[k++]=bottom;
                vertices[k++]=tail;vertices[k++]=bottom;vertices[k++]=right;
                Vector3 finA=tail-forward*.20f+Vector3.up*.17f,finB=tail-forward*.20f-Vector3.up*.17f;
                vertices[k++]=tail;vertices[k++]=finA;vertices[k++]=finB;
                vertices[k++]=tail;vertices[k++]=finB;vertices[k++]=finA;
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
