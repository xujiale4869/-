#ifndef DEEPSEA_COAST_DETAIL_INCLUDED
#define DEEPSEA_COAST_DETAIL_INCLUDED
// Original local material treatment. SurfWash.hlsl supplies shared noise/reef data.
float CoastDetailPocket(float2 p){
 return smoothstep(29,37,p.x)*(1-smoothstep(82,96,p.x))*smoothstep(62,72,p.y)*(1-smoothstep(114,125,p.y))*_BayCoastDetail;
}
float CoastStoneFoot(float2 p){
 float amount=0;
 [unroll]for(int k=0;k<2;k++)if(k<_BaySurfRockCount){
  float4 rock=_BaySurfRocks[k];float2 local=(p-rock.xy)/max(rock.zw,float2(.1,.1));
  float rough=(SurfNoise(p*.60)-.5)*.20;
  amount=max(amount,1-smoothstep(.78,1.65,length(local)+rough));
 }
 return amount*_BayCoastDetail;
}
float CoastSandRidge(float2 p,float footprint){
 float phase=dot(p,float2(3.6,6.4))+(SurfNoise(p*.20)-.5)*2;
 float ridge=pow(saturate(sin(phase)*.5+.5),3)-.32;
 return ridge*(1-smoothstep(.08,.19,footprint));
}
#endif
