// Bird mesh: #NVJOB, MIT. Original lightweight URP wing animation and coastal colours.
Shader "Deepsea/Seabird" {
 Properties {_Flap("Wing beats",Float)=.6 _Phase("Individual phase",Float)=0}
 SubShader {
  Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  CBUFFER_START(UnityPerMaterial)
  float _Flap,_Phase;
  CBUFFER_END
  struct A{float4 p:POSITION;float3 n:NORMAL;};
  struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float3 local:TEXCOORD1;float fog:TEXCOORD2;};
  V vert(A a){
   V o;float3 p=a.p.xyz;float mask=smoothstep(.08,.25,abs(p.x));
   float angle=sin(_Time.y*7.2+_Phase)*.65*_Flap;
   p.y+=(abs(p.x)-.07)*sin(angle)*mask;
   p.x*=1-mask*(1-cos(angle));p.z+=mask*.015*cos(_Time.y*7.2+_Phase)*_Flap;
   float3 world=TransformObjectToWorld(p);o.p=TransformWorldToHClip(world);
   o.n=TransformObjectToWorldNormal(a.n);o.local=a.p.xyz;o.fog=ComputeFogFactor(o.p.z);return o;
  }
  ENDHLSL
  Pass {
   Tags{"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   half4 frag(V i):SV_Target{
    Light sun=GetMainLight();float light=.73+.27*abs(dot(normalize(i.n),sun.direction));
    float tips=smoothstep(.34,.43,abs(i.local.x));
    float3 colour=lerp(float3(.86,.9,.85),float3(.12,.21,.25),tips);
    colour*=light;return half4(MixFog(colour,i.fog),1);
   }
   ENDHLSL
  }
  Pass {
   Tags{"LightMode"="DepthOnly"} ColorMask 0 ZWrite On
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment depth
   half4 depth(V i):SV_Target{return 0;}
   ENDHLSL
  }
 }
}
