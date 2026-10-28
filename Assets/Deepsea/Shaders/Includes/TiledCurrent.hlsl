#ifndef DEEPSEA_TILED_CURRENT_INCLUDED
#define DEEPSEA_TILED_CURRENT_INCLUDED
// Adapted from Scrawk/Tiled-Directional-Flow at 386892cc3f6e21b4694b2777a9e7e66d47a3dd67.
// Original tiled directional flow (c) 2010 Frans van Hoesel, University of Groningen.
// Unity port copyright (c) 2017 Justin. MIT + original Creditware notice retained
// in ThirdParty/TiledDirectionalFlow/LICENSE.txt and Upstream.shader.txt.
// Adaptation: URP texture API, world coordinates, independent normal rotation,
// bounded local field, analytic smooth normal source. Depth/refraction stay in Ocean.shader.
TEXTURE2D(_CurrentMap);SAMPLER(sampler_CurrentMap);
TEXTURE2D(_CurrentNormals);SAMPLER(sampler_CurrentNormals);
float4 _CurrentRegion;
float3 CurrentField(float2 p){
 float2 uv=(p-_CurrentRegion.xy)*_CurrentRegion.zw;
 float blend=smoothstep(0,.06,uv.x)*smoothstep(0,.06,uv.y)*(1-smoothstep(.94,1,uv.x))*(1-smoothstep(.94,1,uv.y));
 float3 f=SAMPLE_TEXTURE2D_LOD(_CurrentMap,sampler_CurrentMap,saturate(uv),0).rgb;
 float2 direction=normalize(lerp(float2(.42,.9075),f.rg*2-1,blend));
 return float3(direction,lerp(1,f.b*1.5,blend));
}
float2 CurrentTile(float2 p,float2 tileOffset,float time,float speedScale,float phase){
 const float tileSize=7;
 float2 centre=(floor(p/tileSize+tileOffset)-tileOffset+.5)*tileSize;
 float3 flow=CurrentField(centre);
 float2 along=flow.xy,across=float2(-along.y,along.x);
 // Wave crests extend across the flow instead of forming round cloud-like patches.
 float2 uv=float2(dot(p,along),dot(p,across)*.38)*.067;
 uv.x-=time*.045*flow.z*speedScale;
 uv+=phase;
 float2 s=SAMPLE_TEXTURE2D_LOD(_CurrentNormals,sampler_CurrentNormals,uv,1).rg*2-1;
 return along*s.x+across*s.y*.38;
}
float2 TiledCurrentSlope(float2 p,float time){
 // Original four offset tiles with a cubic seam blend and variance compensation.
 float2 f=abs(2*frac(p/7)-1)-.5;
 f=.5-4*f*f*f;
 float2 compensation=sqrt(f*f+(1-f)*(1-f));
 float2 a=CurrentTile(p,float2(0,0),time,1,0);
 float2 b=CurrentTile(p,float2(.5,0),time,1.06,.62);
 float2 c=CurrentTile(p,float2(0,.5),time,1.33,.27);
 float2 d=CurrentTile(p,float2(.5,.5),time,1.24,0);
 return lerp(lerp(d,c,f.x),lerp(b,a,f.x),f.y)/max(.25,compensation.x*compensation.y)*.27;
}
#endif
