Shader "Deepsea/MarineSplash" {
 Properties {_Color("Tint",Color)=(.8,1,.95,1)}
 SubShader {
  Tags{"Queue"="Transparent+12" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Pass {
   Tags{"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float4 _Color;
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float fog:TEXCOORD1;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color*_Color;o.fog=ComputeFogFactor(o.p.z);return o;}
   half4 frag(V i):SV_Target{
    float r=length((i.uv-.5)*2);float a=smoothstep(1,.35,r);
    return half4(MixFog(i.color.rgb,i.fog),i.color.a*a);
   }
   ENDHLSL
  }
 }
}
