Shader "Deepsea/CoastFoundationWater"
{
 Properties{_Shallow("Shallow",Color)=(.08,.53,.51,1) _Deep("Deep",Color)=(.012,.09,.15,1)}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass{
 Name "FoundationWater" Tags{"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _Shallow,_Deep;
 CBUFFER_END
 float _CoastWaterDepthProbe,_CoastWaterDetails;
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);return o;}
 half4 Frag(V i):SV_Target{
 float depth=SampleSceneDepth(GetNormalizedScreenSpaceUV(i.p));
 #if !UNITY_REVERSED_Z
 depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
 #endif
 float3 bottom=ComputeWorldSpacePosition(GetNormalizedScreenSpaceUV(i.p),depth,UNITY_MATRIX_I_VP);
 float thickness=max(0,i.world.y-bottom.y),absorption=1-exp(-thickness*.21);
 if(_CoastWaterDepthProbe>.5)return half4(saturate(thickness/25),0,0,1);
 half3 c=lerp(_Shallow.rgb,_Deep.rgb,1-exp(-thickness*.18));
 float2 p=i.world.xz;float t=_Time.y;
 float bend=sin(p.x*.21-p.y*.16+t*.23);
 float dx=.045*cos(p.x*.72+p.y*.31+t*.65+bend)+.018*cos(p.x*2.3-p.y*1.7-t*1.1);
 float dz=.038*sin(p.y*.68-p.x*.22+t*.53)+.015*sin(p.y*2.1+p.x*1.3-t*.93);
 dx*=_CoastWaterDetails;dz*=_CoastWaterDetails;
 float3 n=normalize(float3(dx,1,dz)),v=GetWorldSpaceNormalizeViewDir(i.world);Light sun=GetMainLight();
 float fresnel=pow(1-saturate(dot(n,v)),4);c=lerp(c,half3(.23,.37,.39),fresnel*.45);
 c+=sun.color*pow(saturate(dot(n,normalize(sun.direction+v))),100)*.09;
 // Broad sky reflections vary with the wave normal, without a planar reflection camera.
 float skyRipple=saturate(.5+3.5*(dx+dz));
 c+=half3(.025,.045,.052)*skyRipple*(.25+fresnel)*_CoastWaterDetails;
 // Broken, moving contact foam follows the opaque depth intersection at the bank.
 float edgeNoise=.6+.22*sin(p.x*5.2+sin(p.y*3.3)+t*.7)+.18*sin(p.y*7.1-p.x*2.4-t*.55);
 float foam=smoothstep(.025,.10,thickness)*(1-smoothstep(.16,.62,thickness));
 foam*=saturate(edgeNoise)*(.65+.35*sin(p.x*.8+p.y*.6-t*.6))*_CoastWaterDetails;
 c=lerp(c,half3(.48,.65,.59),foam*.42);
 // Keep the shallow seabed readable while giving even the first metres a water colour.
 // Previously the warm sand overwhelmed the low-opacity surface.
 float shoreFade=smoothstep(0,.22,thickness);
 float alpha=clamp(.42+absorption*.55+fresnel*.04,.42,.97)*shoreFade;
 return half4(c,alpha);
 }
 ENDHLSL
 }
 }
}
