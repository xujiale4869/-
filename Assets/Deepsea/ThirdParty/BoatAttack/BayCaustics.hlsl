// Adapted from Unity Technologies Boat Attack Water / Runtime/Shaders/Caustics.shader.
// Boat Attack copyright (c) 2018 Unity Technologies ApS.
// Unity Companion License: ThirdParty/BoatAttack/LICENSE.md.
// Adaptation: apply the packed-map driver directly to submerged ground, without a renderer feature.
#ifndef BAY_CAUSTICS_INCLUDED
#define BAY_CAUSTICS_INCLUDED
TEXTURE2D(_BayCausticMap);SAMPLER(sampler_BayCausticMap);
float3 BayCaustics(float3 world,float3 lightDirection){
 float depth=-.22-world.y;
 if(depth<=.02||depth>7)return 0;
 float time=_Time.y*.018;
 float2 rawUV=world.xz+depth*lightDirection.xz*.25;
 float waveOffset=SAMPLE_TEXTURE2D_LOD(_BayCausticMap,sampler_BayCausticMap,rawUV*.025+time*.25,1).w-.5;
 float2 uv=rawUV*.23+waveOffset*.1;
 float lod=max(0,depth*.38);
 float4 A=SAMPLE_TEXTURE2D_LOD(_BayCausticMap,sampler_BayCausticMap,uv+time,lod);
 float4 B=SAMPLE_TEXTURE2D_LOD(_BayCausticMap,sampler_BayCausticMap,uv*1.73-time,lod);
 float driver=(A.z*B.z)*10+A.z+B.z;
 return saturate(driver)*float3(.08,.10,.07)*smoothstep(.02,.45,depth)*exp(-depth*.22);
}
#endif
