Shader "Deepsea/CoastLeafRipple" {Properties{_Tint("Tint",Color)=(.72,.88,.85,1) _Opacity("Opacity",Float)=.2} SubShader{Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+35" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};float4 _Tint;float _Opacity;
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target{float r=length((i.uv-.5)*2);float a=1-smoothstep(.025,.10,abs(r-.72));a*=1-smoothstep(.94,1,r);return half4(_Tint.rgb,_Tint.a*_Opacity*a);}
 ENDHLSL}}}