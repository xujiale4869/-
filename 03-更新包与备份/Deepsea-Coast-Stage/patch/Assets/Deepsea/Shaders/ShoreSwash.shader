Shader "Deepsea/ShoreSwash" {
 SubShader {
  Tags{"RenderType"="Transparent" "Queue"="Transparent+1" "RenderPipeline"="UniversalPipeline"}
  Pass {
   Tags{"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Includes/SurfWash.hlsl"
   struct A{float4 p:POSITION;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float fog:TEXCOORD1;};
   V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.p.z);return o;}
   half4 frag(V i):SV_Target{
    // Only the thin film on the sloping beach; open water uses Ocean.shader.
    clip(i.world.y+.18);
    float cover=SurfCoverage(i.world.xz);
    float foam=SurfFoam(i.world.xz,SurfNoise(i.world.xz*3.2-_BaySurfClock*.12));
    float edge=1-smoothstep(.20,.45,i.world.y);
    float alpha=(cover*.19+foam*.67)*edge;
    clip(alpha-.005);
    float3 c=lerp(float3(.33,.66,.58),float3(.79,.9,.83),saturate(foam*2.4));
    return half4(MixFog(c,i.fog),saturate(alpha));
   }
   ENDHLSL
  }
 }
}
