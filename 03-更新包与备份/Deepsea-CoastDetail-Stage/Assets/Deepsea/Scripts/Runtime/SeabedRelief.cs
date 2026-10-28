using UnityEngine;
namespace Deepsea {
/// <summary>Seeded metre-scale sand banks. Generated once into the existing bed mesh and depth map.</summary>
public static class SeabedRelief {
 public static bool Enabled=true;
 static readonly FastNoiseLite field=Field(4387,.052f);
 static readonly FastNoiseLite warp=Warp();
 static FastNoiseLite Field(int seed,float frequency){
  var n=new FastNoiseLite(seed);n.SetFrequency(frequency);n.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
  n.SetFractalType(FastNoiseLite.FractalType.FBm);n.SetFractalOctaves(2);return n;
 }
 static FastNoiseLite Warp(){var n=new FastNoiseLite(9341);n.SetFrequency(.043f);n.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2Reduced);n.SetDomainWarpAmp(3.2f);return n;}
 static float Smooth(float a,float b,float x){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);}
 public static float Pattern(float x,float z){warp.DomainWarp(ref x,ref z);return field.GetNoise(x,z);}
 public static float SandHeight(float x,float z,float original,float pattern){
  // The actual walkable island and swash strip retain their existing surface.
  float pocket=Smooth(48,53,x)*(1-Smooth(76,87,x))*Smooth(65,75,z)*(1-Smooth(111,123,z));
  float bed=Smooth(.9f,2.4f,-original);
  float bank=Mathf.Sin(z*.17f+x*.055f+pattern*2.3f)*.48f+pattern*.75f;
  return original+bank*pocket*bed;
 }
 public static float ReefShelf(float x,float z,Bounds b,float pattern){
  float dx=(x-b.center.x)/Mathf.Max(2,b.extents.x),dz=(z-b.center.z)/Mathf.Max(2,b.extents.z);
  float radius=Mathf.Sqrt(dx*dx+dz*dz),angle=Mathf.Atan2(dz,dx);
  float margin=Smooth(.55f,1.25f,radius);
  float scallop=Mathf.Sin(angle*3+b.center.x*.17f)*.17f+Mathf.Cos(angle*5+b.center.z*.11f)*.09f;
  radius+=(scallop+pattern*.32f)*margin;
  float edge=Smooth(.55f,2.65f,radius);
  // A sloping apron replaces the old almost-flat, very shallow elliptical platform.
  bool focus=(new Vector2(b.center.x-64,b.center.z-89).sqrMagnitude<4)||(new Vector2(b.center.x-47,b.center.z-73).sqrMagnitude<4);
  if(CoastalDetail.Enabled&&focus)return Mathf.Lerp(-1.32f,-12,Smooth(.55f,2.15f,radius));
  return Mathf.Lerp(-.92f,-12,edge);
 }
}
}

