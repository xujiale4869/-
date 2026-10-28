Shader "Deepsea/Seagrass" {
 SubShader {
  Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  #include "../ThirdParty/BoatAttack/BayCaustics.hlsl"
  float4 _BayHabitatFlow, _BayGrassBoat;
  struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
  struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 color:COLOR;float fog:TEXCOORD2;};
  V vert(A a){
   V o;float3 p=TransformObjectToWorld(a.p.xyz);
   float amount=a.uv.x*a.uv.x;
   float bend=sin(_Time.y*1.1+a.uv.y+p.x*.12+p.z*.16);
   float2 flow=_BayHabitatFlow.xz;
   float2 across=float2(flow.y,-flow.x);
   p.xz+=amount*(flow*(.08+bend*.15)+across*cos(_Time.y*.9+a.uv.y)*.045);
   float2 away=p.xz-_BayGrassBoat.xy;
   float influence=saturate(1-length(away)/4)*_BayGrassBoat.z;
   p.xz+=amount*normalize(away+float2(.001,.001))*influence*.22;
   o.world=p;o.n=TransformObjectToWorldNormal(a.n);o.color=a.color;
   o.p=TransformWorldToHClip(p);o.fog=ComputeFogFactor(o.p.z);return o;
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
    Light sun=GetMainLight();float l=.75+.25*abs(dot(normalize(i.n),sun.direction));
    float3 c=i.color.rgb*l+BayCaustics(i.world,sun.direction)*.55;
    return half4(MixFog(c,i.fog),1);
   }
   ENDHLSL
  }
  Pass {
   Tags{"LightMode"="DepthOnly"}ColorMask 0 ZWrite On
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment depth
   half4 depth(V i):SV_Target{return 0;}
   ENDHLSL
  }
 }
}
