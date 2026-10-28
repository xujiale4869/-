Shader "Deepsea/SeaLifeWakeV23" {
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+8" "RenderType"="Transparent"}
  Pass {Name "HistoricalWake" Tags {"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
   V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.c=a.c;return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
   half4 Frag(V i):SV_Target {float noise=Noise(float2(i.uv.y*8.3,i.uv.x*4.7));float centre=.5+.08*sin(i.uv.y*5.1);float edge=pow(saturate(1-abs(i.uv.x-centre)*2),1.45);float foam=.18+.82*smoothstep(.2,.78,noise);return half4(i.c.rgb,i.c.a*edge*foam);}
   ENDHLSL
  }
 }
}
