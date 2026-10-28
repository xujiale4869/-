// Adapted from Unity-Technologies/boat-attack-water, commit 45d6e8c52a9f615fd6b83a91cdd0e9fda9ea86d3.
// Boat Attack Water copyright (c) 2018 Unity Technologies ApS. Unity Companion License.
// See Tools/SeaLifeNavigationV23/ThirdParty/BoatAttack and SOURCES.md.
using UnityEngine;
namespace Deepsea {
 public static class OceanWaveV24 {
  const float Q=.70f;
  static void Wave(Vector2 p,float t,Vector2 direction,float amplitude,float wavelength,float phase,ref Vector3 displacement,ref Vector3 dx,ref Vector3 dz){
   direction.Normalize();float k=2*Mathf.PI/wavelength,frequency=Mathf.Sqrt(9.8f*k);
   float angle=Vector2.Dot(direction,p)*k-t*frequency*.78f+phase,s=Mathf.Sin(angle),c=Mathf.Cos(angle);
   displacement+=new Vector3(Q*amplitude*direction.x*c,amplitude*s,Q*amplitude*direction.y*c);
   float horizontal=-Q*amplitude*k*s,vertical=amplitude*k*c;
   dx+=new Vector3(horizontal*direction.x*direction.x,vertical*direction.x,horizontal*direction.y*direction.x);
   dz+=new Vector3(horizontal*direction.x*direction.y,vertical*direction.y,horizontal*direction.y*direction.y);
  }
  public static Vector3 Displacement(Vector2 p,float t,out Vector3 dx,out Vector3 dz){
   var d=Vector3.zero;dx=Vector3.right;dz=Vector3.forward;
   Wave(p,t,new Vector2(.45f,.89f),.190f,7.6f,0,ref d,ref dx,ref dz);
   Wave(p,t,new Vector2(.13f,1),.070f,4.7f,.8f,ref d,ref dx,ref dz);
   Wave(p,t,new Vector2(-.8f,.6f),.030f,2.8f,2.1f,ref d,ref dx,ref dz);return d;
  }
  // Invert horizontal Gerstner displacement so floats query the rendered world position.
  public static Vector3 Field(Vector2 world,float t){
   var param=world;Vector3 dx,dz,d;
   for(int i=0;i<6;i++){d=Displacement(param,t,out dx,out dz);param=world-new Vector2(d.x,d.z);}
   d=Displacement(param,t,out dx,out dz);float det=dx.x*dz.z-dz.x*dx.z;
   return new Vector3(d.y,(dx.y*dz.z-dz.y*dx.z)/det,(dz.y*dx.x-dx.y*dz.x)/det);
  }
 }
}
