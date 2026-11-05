Shader "Deepsea/BayOverlayV28" {
 Properties {[MainColor] _BaseColor("Tint",Color)=(.2,.9,.7,.4)}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+60" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;
 CBUFFER_END
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;};
 V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);return o;}
 half4 Frag(V i):SV_Target{return _BaseColor;}
 ENDHLSL}
 }
}
