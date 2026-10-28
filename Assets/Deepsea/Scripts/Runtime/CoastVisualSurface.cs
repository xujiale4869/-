using System.Collections.Generic;
using UnityEngine;

namespace Deepsea {
// Placement queries against rendered decorative stone without introducing physics colliders.
public sealed class CoastVisualSurface {
 readonly Vector3[] world;readonly int[] triangles;readonly Bounds bounds;
 public CoastVisualSurface(Mesh mesh,Matrix4x4 matrix){
  var points=mesh.vertices;triangles=mesh.triangles;world=new Vector3[points.Length];
  bounds=new Bounds(matrix.MultiplyPoint3x4(points[0]),Vector3.zero);
  for(int i=0;i<points.Length;i++){world[i]=matrix.MultiplyPoint3x4(points[i]);bounds.Encapsulate(world[i]);}
 }
 public bool Sample(Vector2 p,ref Vector3 point,ref Vector3 normal){
  if(p.x<bounds.min.x||p.x>bounds.max.x||p.y<bounds.min.z||p.y>bounds.max.z)return false;
  bool found=false;
  for(int i=0;i<triangles.Length;i+=3){
   var a=world[triangles[i]];var b=world[triangles[i+1]];var c=world[triangles[i+2]];
   float denominator=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
   if(Mathf.Abs(denominator)<.000001f)continue;
   float u=((b.z-c.z)*(p.x-c.x)+(c.x-b.x)*(p.y-c.z))/denominator;
   float v=((c.z-a.z)*(p.x-c.x)+(a.x-c.x)*(p.y-c.z))/denominator;
   if(u<0||v<0||u+v>1)continue;
   float y=u*a.y+v*b.y+(1-u-v)*c.y;
   if(y<=point.y)continue;
   Vector3 n=Vector3.Cross(b-a,c-a).normalized;if(n.y<0)n=-n;
   point=new Vector3(p.x,y,p.y);normal=n;found=true;
  }
  return found;
 }
 public static bool SampleAll(List<CoastVisualSurface> surfaces,Vector2 p,ref Vector3 point,ref Vector3 normal){
  bool found=false;foreach(var surface in surfaces)if(surface.Sample(p,ref point,ref normal))found=true;return found;
 }
}
}
