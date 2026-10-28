using UnityEngine;
using B=Deepsea.ReefLifeGarden.Batch;
namespace Deepsea {
/// <summary>Original low-poly reef forms, shared by habitat and small benthic residents.</summary>
public static class ReefDetails {
 public static void Tube(B b,Vector3 from,Vector3 to,float r0,float r1,Color low,Color tip,int sides=6,float phase=0){
  Vector3 up=(to-from).normalized;if(up.sqrMagnitude<.1f)return;
  Vector3 right=Vector3.Cross(up,Mathf.Abs(up.y)>.9f?Vector3.forward:Vector3.up).normalized,forward=Vector3.Cross(up,right);int first=b.v.Count;
  for(int ring=0;ring<2;ring++)for(int i=0;i<=sides;i++){
   float a=i*Mathf.PI*2/sides;Vector3 q=(ring==0?from:to)+(right*Mathf.Cos(a)+forward*Mathf.Sin(a))*(ring==0?r0:r1);
   b.Vertex(q,ring==0?low:tip,new Vector2(0,phase));
  }
  for(int i=0;i<sides;i++){int a=first+i,c=a+sides+1;b.Tri(a,a+1,c);b.Tri(a+1,c+1,c);}
 }
 public static void Ball(B b,Vector3 p,Vector3 size,Color low,Color tip,int sides=12,int rings=6){
  int first=b.v.Count;
  for(int r=0;r<=rings;r++)for(int i=0;i<=sides;i++){
   float phi=r*Mathf.PI/rings,a=i*Mathf.PI*2/sides;Vector3 n=new Vector3(Mathf.Sin(phi)*Mathf.Cos(a),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(a));
   b.Vertex(p+Vector3.Scale(n,size),Color.Lerp(low,tip,(n.y+1)*.5f),Vector2.zero);
  }
  for(int r=0;r<rings;r++)for(int i=0;i<sides;i++){int a=first+r*(sides+1)+i,c=a+sides+1;b.Tri(a,c,a+1);b.Tri(a+1,c,c+1);}
 }
 public static void Dome(B b,Vector3 p,float height,Color low,Color tip){
  const int sides=18,rings=6;int first=b.v.Count;float radius=height*1.30f;
  for(int r=0;r<=rings;r++)for(int i=0;i<=sides;i++){
   float u=r/(float)rings,a=i*Mathf.PI*2/sides,phi=u*Mathf.PI*.5f;
   float ridges=1+.045f*Mathf.Sin(a*9+phi*8);
   Vector3 q=p+new Vector3(Mathf.Sin(phi)*Mathf.Cos(a)*radius*ridges,Mathf.Cos(phi)*height,Mathf.Sin(phi)*Mathf.Sin(a)*radius*.83f*ridges);
   b.Vertex(q,Color.Lerp(tip,low,u*.72f),new Vector2(0,u));
  }
  for(int r=0;r<rings;r++)for(int i=0;i<sides;i++){int a=first+r*(sides+1)+i,c=a+sides+1;b.Tri(a,c,a+1);b.Tri(a+1,c,c+1);}
 }
 public static int Sponges(B b,Vector3 p,float height,Color tint,int seed){
  int count=3+seed%3;
  for(int tube=0;tube<count;tube++){
   float direction=tube*Mathf.PI*2/count,spread=tube==0?0:.34f;Vector3 centre=p+new Vector3(Mathf.Sin(direction)*spread,0,Mathf.Cos(direction)*spread);
   float h=height*(.70f+.30f*Mathf.Sin(tube*1.7f+seed)*.5f+.15f);int first=b.v.Count;const int sides=10;
   for(int ring=0;ring<5;ring++)for(int j=0;j<=sides;j++){
    float a=j*Mathf.PI*2/sides,y=ring==0?0:ring==1?h*.72f:ring==2?h:ring==3?h-.025f:h-.20f;
    float radius=ring==0?.12f:ring==1?.23f:ring==2?.24f:.145f;
    Vector3 q=centre+new Vector3(Mathf.Sin(a)*radius+Mathf.Cos(direction)*y*.09f,y,Mathf.Cos(a)*radius);
    Color c=ring>=3?tint*.38f:Color.Lerp(tint,new Color(.80f,.56f,.63f),ring/5f);
    b.Vertex(q,c,Vector2.zero);
   }
   for(int ring=0;ring<4;ring++)for(int j=0;j<sides;j++){int a=first+ring*(sides+1)+j,c=a+sides+1;b.Tri(a,a+1,c);b.Tri(a+1,c+1,c);}
  }
  return count;
 }
 public static void Fan(B b,Vector3 p,float height,Color low,Color tip,int seed){
  Quaternion rot=Quaternion.Euler(0,(seed*73)%180,0);Vector3 stalk=p+Vector3.up*height*.22f;
  Tube(b,p,stalk,.10f,.06f,low,low);
  const int arms=15;
  for(int arm=0;arm<arms;arm++){
   float angle=Mathf.Lerp(-1.12f,1.12f,arm/(float)(arms-1));Vector3 prev=stalk;
   for(int segment=1;segment<=5;segment++){
    float f=segment/5f;Vector3 q=stalk+rot*new Vector3(Mathf.Sin(angle)*height*f,Mathf.Cos(angle)*height*f*.78f,Mathf.Sin(f*2.4f+arm)*height*.025f);
    Color c=Color.Lerp(low,tip,f);Tube(b,prev,q,.035f*(1-f)+.014f,.028f*(1-f)+.009f,c,c,5);
    if(segment>=2&&segment<=4){Vector3 side=rot*new Vector3((arm%2==0?1:-1)*height*.14f,height*.10f,0);Tube(b,q,q+side,.019f,.006f,c,tip,5);}
    prev=q;
   }
  }
 }
 public static void Anemone(B b,Vector3 p,float height,int seed){
  for(int arm=0;arm<18;arm++){
   float a=arm*Mathf.PI*2/18,phase=arm*.9f+seed;Vector3 side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));int first=b.v.Count;
   for(int s=0;s<=5;s++){
    float f=s/5f;Vector3 q=p+Vector3.up*height*(f-.20f*f*f)+side*(f*f*.42f);float width=.035f*(1-f)+.012f;
    Vector3 across=Vector3.Cross(Vector3.up,side)*width;Color c=Color.Lerp(new Color(.11f,.35f,.32f),new Color(.58f,.67f,.34f),f);
    b.Vertex(q-across,c,new Vector2(f,phase));b.Vertex(q+across,c,new Vector2(f,phase));
   }
   for(int s=0;s<5;s++){int a0=first+s*2;b.Tri(a0,a0+2,a0+1);b.Tri(a0+1,a0+2,a0+3);}
  }
 }
 public static void Star(B b,Vector3 p,float radius,int seed){
  Color low=new Color(.53f,.20f,.09f),tip=new Color(.90f,.46f,.21f);int first=b.v.Count;b.Vertex(p+Vector3.up*.10f,tip,Vector2.zero);
  for(int i=0;i<=10;i++){float a=i*Mathf.PI/5+seed*.8f,r=i%2==0?radius:radius*.40f;b.Vertex(p+new Vector3(Mathf.Cos(a)*r,.015f,Mathf.Sin(a)*r),low,Vector2.zero);}
  for(int i=0;i<10;i++)b.Tri(first,first+i+2,first+i+1);
 }
 public static void Urchin(B b,Vector3 p,int seed){
  Color low=new Color(.075f,.07f,.13f),tip=new Color(.23f,.16f,.29f);Ball(b,p+Vector3.up*.12f,Vector3.one*.19f,low,tip,10,5);
  for(int i=0;i<28;i++){
   float a=i*2.399963f+seed,y=.15f+(i%7)/7f*.85f;Vector3 dir=new Vector3(Mathf.Cos(a)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(a)*Mathf.Sqrt(1-y*y));
   Tube(b,p+Vector3.up*.12f+dir*.12f,p+Vector3.up*.12f+dir*(.32f+.08f*Mathf.Sin(i)),.020f,0,low,tip,4);
  }
 }
}
}
