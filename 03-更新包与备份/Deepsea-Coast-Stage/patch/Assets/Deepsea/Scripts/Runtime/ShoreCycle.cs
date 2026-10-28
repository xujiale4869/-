using UnityEngine;
namespace Deepsea {
/// <summary>Shared east-shore cycle. Distances are metres, positive toward the sea.</summary>
public static class ShoreCycle {
 public const float Period=8.8f,Peak=.53f,ShoreRadius=.91465f;
 public static float Phase(float z,float time){return Mathf.Repeat((time+(z-94)*.12f)/Period,1);}
 public static float Front(float phase){
  float q=phase<=Peak?phase/Peak:(phase-Peak)/(1-Peak);
  q=q*q*(3-2*q);return phase<=Peak?Mathf.Lerp(7.5f,-1.05f,q):Mathf.Lerp(-1.05f,7.5f,q);
 }
 public static float Distance(Vector3 p){
  float x=(p.x-8)/38,z=(p.z-94)/42;return (Mathf.Sqrt(x*x+z*z)-ShoreRadius)*38;
 }
 public static float EastShoreX(float z){
  float dz=(z-94)/42;return 8+38*Mathf.Sqrt(Mathf.Max(0,ShoreRadius*ShoreRadius-dz*dz));
 }
 public static float Crash(float phase){
  return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.46f,phase))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,.88f,phase)));
 }
 public static float AudioDistance(float distance){float w=Mathf.Clamp01((42-distance)/34);return w*w;}
}
}
