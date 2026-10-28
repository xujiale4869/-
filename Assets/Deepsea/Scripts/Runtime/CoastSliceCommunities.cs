using UnityEngine;
namespace Deepsea {public sealed partial class CoastSlice {
 static float Range(System.Random r,float a,float b){return Mathf.Lerp(a,b,(float)r.NextDouble());}
 void BuildReefCommunities(){
  var colony=new ReefLifeGarden.Batch();var kelp=new ReefLifeGarden.Batch();var random=new System.Random(20261020);
  Color[] low={new Color(.27f,.25f,.20f),new Color(.26f,.20f,.25f),new Color(.37f,.21f,.16f),new Color(.19f,.29f,.22f)};
  Color[] tip={new Color(.56f,.44f,.31f),new Color(.51f,.33f,.43f),new Color(.70f,.45f,.28f),new Color(.40f,.51f,.34f)};
  for(int i=0;i<20;i++){
   int group=i<6?0:i<13?1:2;int local=i-(group==0?0:group==1?6:13);
   float z=(group==0?66:group==1?91:115)+local*(group==0?2.8f:group==1?2.25f:1.9f)+Range(random,-.8f,.8f);
   float ridge=78+Mathf.Sin(z*.13f)*3.5f,x=ridge+Range(random,-2.5f,2.5f),y=sea.BottomHeight(x,z);
   float sx=Range(random,3.6f,6.1f),sy=Range(random,1.25f,2.1f),sz=Range(random,2.8f,5.6f);
   Rock(new Vector3(x,y-.5f,z),new Vector3(sx,sy,sz),190+i*37,false,false);
   int density=8+random.Next(7);float rotation=Range(random,0,Mathf.PI*2);
   for(int j=0;j<density;j++){
    float angle=rotation+j*2.399963f,rad=Mathf.Sqrt((j+.4f)/density)*Mathf.Min(sx,sz)*.46f;
    var p=new Vector3(x+Mathf.Cos(angle)*rad,Mathf.Min(y+.45f,-1.65f),z+Mathf.Sin(angle)*rad);
    int color=random.Next(low.Length),kind=random.Next(5),seed=i*29+j;float height=Range(random,.42f,.95f);
    if(kind==0||kind==4)ReefDetails.Sponges(colony,p,height*.88f,tip[color],seed);
    else if(kind==1)ReefDetails.Dome(colony,p,height*.72f,low[color],tip[color]);
    else if(kind==2)ReefDetails.Fan(colony,p,height*1.12f,low[color],tip[color],seed);
    else ReefDetails.Anemone(colony,p,height*.78f,seed);
    corals++;
   }
   // Small benthic animals sit on sand beside the reef, outside the clear navigation channel.
   float bx=x+(i%2==0?2.5f:-2.5f),bz=z+Range(random,-1,1),bed=sea.BottomHeight(bx,bz);
   if(i%2==0)ReefDetails.Star(colony,new Vector3(bx,bed+.04f,bz),Range(random,.20f,.36f),i*11);
   else ReefDetails.Urchin(colony,new Vector3(bx,bed+.03f,bz),i*11);
   for(int j=0;j<3;j++){
    float xx=ridge+(j%2==0?1:-1)*Range(random,2.8f,5.2f),zz=z+Range(random,-1.4f,1.4f),bottom=sea.BottomHeight(xx,zz);
    Kelp(kelp,new Vector3(xx,bottom,zz),Mathf.Min(Range(random,3.4f,5.5f),-.95f-bottom),i*17+j);
   }
  }
  BatchObject("礁脊珊瑚群落",colony,"Deepsea/ReefCoral");BatchObject("深沟边缘海带林",kelp,"Deepsea/NaturalFoliage");
  Debug.Log("COAST_COMMUNITIES colonies=3 coralGroups="+corals+" benthicAnimals=20");
 }
}}
