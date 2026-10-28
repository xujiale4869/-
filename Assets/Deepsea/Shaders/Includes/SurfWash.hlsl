#ifndef DEEPSEA_SURF_WASH_INCLUDED
#define DEEPSEA_SURF_WASH_INCLUDED
// Original local shore treatment. CPU phase constants are in ShoreCycle.cs.
float _BaySurfEnabled,_BaySurfClock,_BayCoastDetail;
float4 _BaySurfRocks[2];int _BaySurfRockCount;
float SurfHash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float SurfNoise(float2 p){
 float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
 return lerp(lerp(SurfHash(a),SurfHash(a+float2(1,0)),f.x),lerp(SurfHash(a+float2(0,1)),SurfHash(a+1),f.x),f.y);
}
float SurfPhase(float z){return frac((_BaySurfClock+(z-94)*.12)/8.8);}
float SurfFront(float phase){
 float q=phase<=.53?phase/.53:(phase-.53)/.47;
 q=q*q*(3-2*q);return phase<=.53?lerp(7.5,-1.05,q):lerp(-1.05,7.5,q);
}
float SurfDistance(float2 p){return (length((p-float2(8,94))/float2(38,42))-.91465)*38;}
float SurfPocket(float2 p){
 return smoothstep(76,81,p.y)*(1-smoothstep(104,109,p.y))*smoothstep(30,37,p.x)*(1-smoothstep(57,61,p.x));
}
float SurfCrash(float phase){return smoothstep(.22,.46,phase)*(1-smoothstep(.55,.88,phase));}
float SurfCoverage(float2 p){
 return smoothstep(-.16,.18,SurfDistance(p)-SurfFront(SurfPhase(p.y)))*SurfPocket(p)*_BaySurfEnabled;
}
float SurfWetness(float2 p){
 float q=SurfPhase(p.y),d=SurfDistance(p);
 float memory=smoothstep(.50,.57,q)*(1-smoothstep(.72,1,q))*smoothstep(-1.35,-.95,d);
 return max(memory,SurfCoverage(p))*SurfPocket(p)*_BaySurfEnabled;
}
float SurfFoam(float2 p,float foamTexture){
 float q=SurfPhase(p.y),d=SurfDistance(p),front=SurfFront(q);
 float warp=(SurfNoise(p*.72+float2(0,_BaySurfClock*.08))-.5)*.38;
 float broken=SurfNoise(float2(p.y*.39,p.x*.17));
 float width=lerp(.24,lerp(.13,.31,broken),_BayCoastDetail);
 warp+=(broken-.5)*.62*_BayCoastDetail;
 float leading=exp(-pow((d-front+warp)/width,2));
 float advancing=smoothstep(.015,.12,q)*(1-smoothstep(.56,.86,q));
 float lace=smoothstep(.15,.64,foamTexture+SurfNoise(p*2.7-_BaySurfClock*.11)*.23);
 float trail=exp(-pow((d-.15)/1.15,2))*smoothstep(.51,.59,q)*(1-smoothstep(.64,.96,q));
 trail*=smoothstep(.50,.77,SurfNoise(p*3.8+_BaySurfClock*.07));
 float gaps=lerp(1,lerp(.14,1,smoothstep(.18,.64,broken)),_BayCoastDetail);
 return (leading*advancing*lace*gaps+trail*.35)*SurfPocket(p)*_BaySurfEnabled;
}
float SurfRockFoam(float2 p,float foamTexture){
 float amount=0;
 [unroll]for(int k=0;k<2;k++)if(k<_BaySurfRockCount){
  float4 rock=_BaySurfRocks[k];float2 local=(p-rock.xy)/max(rock.zw,float2(.1,.1));
  float edge=(length(local)-.92)*min(rock.z,rock.w);
  float windward=smoothstep(-.3,.65,local.x);
  float pulse=SurfCrash(SurfPhase(rock.y));
  float notch=(SurfNoise(p*.85)-.5)*.20*_BayCoastDetail;
  float band=exp(-pow((edge-(1-pulse)*.30+notch)/lerp(.30,.20,_BayCoastDetail),2));
  amount+=band*lerp(lerp(.12,.035,_BayCoastDetail),.60,windward)*pulse*smoothstep(.16,.68,foamTexture);
 }
 return amount*_BaySurfEnabled;
}
#endif

