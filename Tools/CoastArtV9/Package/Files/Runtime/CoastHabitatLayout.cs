using UnityEngine;
namespace Deepsea {
// Shared paint/placement controls. Does not change accepted height or collision.
public static class CoastHabitatLayout {
 public static float S(float a,float b,float v){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));}
 public static Color Control(float x,float z){
  float h=CoastBlockoutLayout.TerrainHeight(x,z),path=CoastBlockoutLayout.PathDistance(new Vector3(x,h,z));
  float tower=Vector2.Distance(new Vector2(x,z),new Vector2(30,93));
  float road=Mathf.Max(1-S(1.20f,2.35f,path),1-S(2.65f,3.60f,tower));
  float inland=CoastBlockoutLayout.Shore(z)-x;
  float noise=Mathf.PerlinNoise(x*.19f+13,z*.19f+7)*.68f+Mathf.PerlinNoise(x*.061f+53,z*.073f+29)*.32f;
  float land=S(.62f,1.25f,h)*S(.8f,3.3f,inland);
  float cover=land*(1-road)*Mathf.Clamp01(.34f+noise*.90f);
  float density=cover*S(2.05f,2.65f,path)*S(3.25f,4.4f,tower);
  return new Color(cover,road,noise,density);
 }
}
}
