Shader "Deepsea/CoastFoundationWater"
{
 Properties{_Shallow("Shallow",Color)=(.12,.66,.66,1) _Deep("Deep",Color)=(.018,.12,.21,1)}
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
 float _CoastWaterDepthProbe;
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
 half3 c=lerp(_Shallow.rgb,_Deep.rgb,1-exp(-thickness*.14));
 float2 p=i.world.xz;float dx=.012*cos(p.x*.42+p.y*.24+_Time.y*.32),dz=.012*sin(p.y*.38-p.x*.12+_Time.y*.27);
 float3 n=normalize(float3(dx,1,dz)),v=GetWorldSpaceNormalizeViewDir(i.world);Light sun=GetMainLight();
 float fresnel=pow(1-saturate(dot(n,v)),4);c=lerp(c,half3(.25,.39,.44),fresnel*.45);
 c+=sun.color*pow(saturate(dot(n,normalize(sun.direction+v))),100)*.09;
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
