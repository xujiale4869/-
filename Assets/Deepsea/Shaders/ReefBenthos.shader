Shader "Deepsea/ReefBenthos" {
 Properties{_Phase("Resident phase",Float)=0}
 SubShader{
  Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  float _Phase;
  struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;float2 uv:TEXCOORD0;};
  struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float4 c:COLOR;};
  V vert(A a){V o;float3 p=a.p.xyz;
   if(a.uv.y!=0)p.z+=sin(_Time.y*6+a.uv.y+_Phase)*.025*saturate(abs(p.x)*2);
   o.p=TransformObjectToHClip(p);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;return o;}
  ENDHLSL
  Pass{Tags{"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   half4 frag(V i):SV_Target{Light sun=GetMainLight();return half4(i.c.rgb*(.72+.28*abs(dot(normalize(i.n),sun.direction))),1);}
   ENDHLSL
  }
  Pass{Tags{"LightMode"="DepthOnly"}ColorMask 0 ZWrite On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment depth
   half4 depth(V i):SV_Target{return 0;}
   ENDHLSL
  }
 }
}
