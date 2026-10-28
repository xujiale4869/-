using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
 public static class CoastGroundSmoothing {
  [System.Serializable]class Piece{public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;}
  [System.Serializable]class Archive{public Piece[] meshes;}
  static readonly Dictionary<MeshFilter,Mesh[]> surfaces=new Dictionary<MeshFilter,Mesh[]>();
  public static void SetVisible(bool visible){foreach(var pair in surfaces)if(pair.Key!=null)pair.Key.sharedMesh=pair.Value[visible?1:0];}
  public static int VisualTriangles {get;private set;}
  public static float MaxDryDeviation {get;private set;}
  public static float Height(float x,float z){
   float shore=CoastBlockoutLayout.Shore(z),d=shore-x;
   if(Mathf.Abs(d)>=.85f)return CoastBlockoutLayout.TerrainHeight(x,z);
   float a=CoastBlockoutLayout.TerrainHeight(shore+.85f,z);
   float b=CoastBlockoutLayout.TerrainHeight(shore-.85f,z);
   float t=Mathf.InverseLerp(-.85f,.85f,d);
   return Mathf.Lerp(a,b,Mathf.SmoothStep(0,1,t));
  }
  public static void Apply(CoastBlockoutRegion region){
   VisualTriangles=0;MaxDryDeviation=0;surfaces.Clear();var archive=new List<Piece>();
   foreach(var f in region.GetComponentsInChildren<MeshFilter>()){
    if(!f.name.StartsWith("地形块"))continue;
    var original=f.sharedMesh;var bounds=original.bounds;
    int nx=Mathf.RoundToInt(bounds.size.x/.325f),nz=Mathf.RoundToInt(bounds.size.z/.325f);
    var points=new Vector3[(nx+1)*(nz+1)];var normals=new Vector3[points.Length];var uv=new Vector2[points.Length];
    var triangles=new int[nx*nz*6];
    for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){
     int k=j*(nx+1)+i;float x=Mathf.Lerp(bounds.min.x,bounds.max.x,i/(float)nx),z=Mathf.Lerp(bounds.min.z,bounds.max.z,j/(float)nz);
     float h=Height(x,z);points[k]=new Vector3(x,h,z);uv[k]=new Vector2(x*.2f,z*.2f);
     normals[k]=new Vector3(Height(x-.09f,z)-Height(x+.09f,z),.18f,Height(x,z-.09f)-Height(x,z+.09f)).normalized;
     if(x<=35.5f&&h>.12f&&CoastBlockoutLayout.PathDistance(points[k])<1.4f)MaxDryDeviation=Mathf.Max(MaxDryDeviation,Mathf.Abs(h-CoastBlockoutLayout.TerrainHeight(x,z)));
    }
    for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){
     int k=j*(nx+1)+i,t=(j*nx+i)*6;
     triangles[t]=k;triangles[t+1]=k+nx+1;triangles[t+2]=k+1;
     triangles[t+3]=k+1;triangles[t+4]=k+nx+1;triangles[t+5]=k+nx+2;
    }
    var visual=new Mesh{name="EN_ContinuousShore_"+f.name,indexFormat=IndexFormat.UInt32};
    visual.vertices=points;visual.normals=normals;visual.uv=uv;visual.triangles=triangles;visual.RecalculateBounds();f.sharedMesh=visual;VisualTriangles+=triangles.Length/3;
    surfaces.Add(f,new[]{original,visual});
    archive.Add(new Piece{name=visual.name,vertices=points,normals=normals,uv=uv,triangles=triangles});
    var collider=f.GetComponent<MeshCollider>();if(collider!=null&&collider.sharedMesh!=original)throw new System.InvalidOperationException("Ground collision mesh changed");
   }
   if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-whitebox-check")>=0){System.IO.Directory.CreateDirectory("Verification/coast-art-v15");System.IO.File.WriteAllText("Verification/coast-art-v15/ground-meshes.json",JsonUtility.ToJson(new Archive{meshes=archive.ToArray()}));}
   Debug.Log("COAST_RENDER_GROUND triangles="+VisualTriangles+" maxPathHeightChange="+MaxDryDeviation+" originalColliders=true");
  }
 }
}
