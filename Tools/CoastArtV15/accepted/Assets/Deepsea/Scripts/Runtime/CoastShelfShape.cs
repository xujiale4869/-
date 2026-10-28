using UnityEngine;
namespace Deepsea {
// Art-only bathymetry. The accepted physical terrain and its depth adapter are untouched.
public static class CoastShelfShape {
 public const float X0=38,X1=106,Z0=50,Z1=142,Step=.52f;
 static readonly FastNoiseLite broad=Noise(231301,.075f,3),detail=Noise(231302,.62f,2);
 static FastNoiseLite Noise(int seed,float frequency,int octaves){var n=new FastNoiseLite(seed);n.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);n.SetFrequency(frequency);n.SetFractalType(FastNoiseLite.FractalType.FBm);n.SetFractalOctaves(octaves);return n;}
 static float S(float a,float b,float x){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));}
 // Replace the actual reef only. Keep the approved v12 shallow seabed and its shading visible.
 public static bool Contains(float x,float z){return x>=X0&&x<=X1&&z>=Z0&&z<=Z1&&x>Mathf.Max(CoastBlockoutLayout.Shore(z)+.55f,CoastBlockoutLayout.Spine(z)-10.0f)&&CaveDistance(x,z)>8.0f;}
 public static bool CoveredCell(float x,float z){
  if(x<X0||x>=X1||z<Z0||z>=Z1)return false;float dx=(X1-X0)/Mathf.CeilToInt((X1-X0)/Step),dz=11.5f/Mathf.CeilToInt(11.5f/Step);
  float x0=X0+Mathf.Floor((x-X0)/dx)*dx,chunk=Mathf.Floor((z-Z0)/11.5f)*11.5f+Z0,z0=chunk+Mathf.Floor((z-chunk)/dz)*dz;
  return Contains(x0,z0)&&Contains(x0+dx,z0)&&Contains(x0,z0+dz)&&Contains(x0+dx,z0+dz);
 }
 public static float CaveDistance(float x,float z){return Vector2.Distance(new Vector2(x,z),new Vector2(CoastBlockoutLayout.CaveFocus.x,CoastBlockoutLayout.CaveFocus.z));}
 // Large bends come from the designed bank curves; noise only erodes their outline.
 static float Ridge(float x,float z,bool far,out float weight){
  float centre=far?CoastBlockoutLayout.FarSpine(z):CoastBlockoutLayout.Spine(z);
  // Inferred metre-scale promontories, with lobes rather than a repeated vertical extrusion.
  float bend=broad.GetNoise(z+37,far?314:71)*2.0f;
  float raw=far?centre-x:x-centre;float u=raw+(far?bend:-bend);
  float headland=.5f+.5f*Mathf.Sin(z*.31f+Mathf.Sin(z*.11f)*1.7f);
  float saddle=3.4f*Mathf.Exp(-Mathf.Pow((z-91.5f)/4.1f,4))+2.5f*Mathf.Exp(-Mathf.Pow((z-114.5f)/3.8f,4));
  float top=(far?-4.2f:-2.45f)+broad.GetNoise(z,far?73:113)*1.2f-saddle;
  float shoulder=1-S(-9.0f,-3.0f,u);
  float ramp=2.0f*S(.2f,3.7f,u)+5.0f*S(2.7f,7.1f,u)
   +6.6f*S(6.2f,11.0f,u)+6.3f*S(10.0f,14.4f,u);
  float h=top-shoulder*5.4f-ramp;
  // Low bedding lips interrupt the ramp without creating tall, narrow vertical sheets.
  float bedding=.55f*S(1.5f+headland,2.4f+headland,u)
   +.70f*S(5.3f-headland,6.5f-headland,u);
  h-=bedding;
  // Inclined rock plates: metre-scale masses survive the fixed gameplay camera.
  h+=detail.GetNoise(x*.58f,z*.58f)*(.35f+.45f*S(0,8,u));
  float plates=Plate(x,z);
  h+=plates*(.7f+1.15f*S(-2,7,u))*S(-9,-4,raw)*(1-S(12,16,u));
  h-=Fracture(x,z)*(.18f+.45f*S(-1,8,u))*S(-10,-5,u)*(1-S(12,15,u));
  weight=S(-9.2f,-3.3f,raw)*(1-S(12,16,u));
  if(far)weight*=1-S(86,92,z)*(1-S(116,122,z));
  return h;
 }
 // Blended planar blocks. Each cell has its own height and dip, with rounded fractured edges.
 static float Plate(float x,float z){
  float px=(x+.32f*z)/3.7f,pz=z/4.8f;int ix=Mathf.FloorToInt(px),iz=Mathf.FloorToInt(pz);
  float sum=0,weights=0;
  for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++){
   int a=ix+i,b=iz+j;float cx=a+.5f+broad.GetNoise(a*31+71,b*23+17)*.40f;
   float cz=b+.5f+broad.GetNoise(a*19+131,b*29+53)*.40f;
   float dx=px-cx,dz=pz-cz,d=dx*dx+dz*dz;
   float w=Mathf.Exp(-d*14f);float height=broad.GetNoise(a*73+419,b*67+197)*1.9f+dx*.22f-dz*.17f;
   sum+=height*w;weights+=w;
  }
  return sum/Mathf.Max(.00001f,weights);
 }
 // Distance to two neighbouring geological cells creates irregular connected joints.
 public static float Fracture(float x,float z){
  float px=x/4.6f,pz=z/5.3f;int ix=Mathf.FloorToInt(px),iz=Mathf.FloorToInt(pz);float a=100,b=100;
  for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++){
   float cx=ix+i+.5f+broad.GetNoise((ix+i)*17+71,(iz+j)*13+29)*.38f;
   float cz=iz+j+.5f+broad.GetNoise((ix+i)*11+41,(iz+j)*19+113)*.38f;
   float d=(px-cx)*(px-cx)+(pz-cz)*(pz-cz);
   if(d<a){b=a;a=d;}else if(d<b)b=d;
  }
  return 1-S(.018f,.16f,Mathf.Sqrt(b)-Mathf.Sqrt(a));
 }
 public static float Height(float x,float z,out float stone){
  float bed=CoastBlockoutLayout.TerrainHeight(x,z),w0,w1;
  float a=Ridge(x,z,false,out w0),b=Ridge(x,z,true,out w1);
  float edge=S(X0,X0+4,x)*(1-S(X1-4,X1,x))*S(Z0,Z0+6,z)*(1-S(Z1-6,Z1,z));
  float cave=S(8,11,CaveDistance(x,z));w0*=edge*cave;w1*=edge*cave;
  float h=Mathf.Max(bed,Mathf.Lerp(bed,a,w0));h=Mathf.Max(h,Mathf.Lerp(bed,b,w1));
  stone=Mathf.Max(w0,w1)*S(.05f,1.5f,h-bed);
  // The visible floor sits a few centimetres over the original to eliminate coplanar flicker.
  return h+.035f*edge;
 }
 public static Vector3 Normal(float x,float z){float m;float dx=Height(x+.12f,z,out m)-Height(x-.12f,z,out m),dz=Height(x,z+.12f,out m)-Height(x,z-.12f,out m);return new Vector3(-dx,.24f,-dz).normalized;}
 public static bool ReplacesRock(Vector3 centre){return centre.y<-.6f&&centre.x>CoastBlockoutLayout.Spine(centre.z)-10&&centre.x<CoastBlockoutLayout.FarSpine(centre.z)+10&&Contains(centre.x,centre.z)&&CaveDistance(centre.x,centre.z)>10;}
}
}
