Shader "Deepsea/ReefCoral" {
 SubShader {
  Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Off
  Pass {
   Tags{"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "../ThirdParty/BoatAttack/BayCaustics.hlsl"
   float _BayReefLife;
   struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;float2 uv:TEXCOORD0;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 c:COLOR;float fog:TEXCOORD2;};
   V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;o.fog=ComputeFogFactor(o.p.z);return o;}
   half4 frag(V i):SV_Target{
    Light sun=GetMainLight();float l=.67+.33*abs(dot(normalize(i.n),sun.direction));
    float pores=.97+.03*sin(i.world.y*39+sin(i.world.x*21)*1.2)*sin(i.world.z*34);
    float3 c=i.c.rgb*l*pores+BayCaustics(i.world,sun.direction)*.30;
    // Submerged opaque objects are fogged by the water composite exactly once.
    return half4(lerp(MixFog(c,i.fog),c,_BayReefLife),1);
   }
   ENDHLSL
  }
  UsePass "Deepsea/CoastalGround/DepthOnly"
 }
}
