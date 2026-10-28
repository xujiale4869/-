Shader "Deepsea/SeabirdShadow" {
 Properties {_Opacity("Soft water shadow",Float)=.15}
 SubShader {
  Tags{"RenderType"="Transparent" "Queue"="Transparent+7" "RenderPipeline"="UniversalPipeline"}
  Pass {
   Tags{"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   float _Opacity;
   struct A{float4 p:POSITION;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float4 screen:TEXCOORD1;};
   V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.screen=ComputeScreenPos(o.p);return o;}
   half4 frag(V i):SV_Target{
    float2 uv=i.screen.xy/i.screen.w;float raw=SampleSceneDepth(uv);
    #if UNITY_REVERSED_Z
    bool valid=raw>.00001;
    #else
    bool valid=raw<.99999;raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
    #endif
    if(valid){float3 scene=ComputeWorldSpacePosition(uv,raw,UNITY_MATRIX_I_VP);clip(.035-dot(scene-i.world,GetWorldSpaceNormalizeViewDir(i.world)));}
    return half4(.025,.16,.19,_Opacity);
   }
   ENDHLSL
  }
 }
}
