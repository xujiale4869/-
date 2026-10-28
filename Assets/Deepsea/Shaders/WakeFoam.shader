Shader "Deepsea/WakeFoam" {
Properties{_FoamMap("Foam",2D)="white"{}}
SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass{
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Includes/SurfWash.hlsl"
 float _BayWaterFinish;
 TEXTURE2D(_FoamMap);SAMPLER(sampler_FoamMap);
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;float2 age:TEXCOORD1;float4 color:COLOR;};
 struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float fog:TEXCOORD1;float3 world:TEXCOORD2;float2 age:TEXCOORD3;};
 V vert(A v){V o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.pos=TransformWorldToHClip(o.world);o.uv=v.uv;o.color=v.color;o.age=v.age;o.fog=ComputeFogFactor(o.pos.z);return o;}
 half4 frag(V i):SV_Target{
  float tex=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,i.uv*float2(1,2)).r;
  float edge=pow(saturate(1-abs(i.uv.x*2-1)),.8);
  float legacy=edge*smoothstep(.18,.72,tex);
  // World-space overlapping foam fields avoid repeating one leaf along each sample strip.
  float2 p=i.world.xz-float2(.042,.091)*_Time.y*i.age.x;
  float warp=(SurfNoise(p*.8)-.5)*.20;
  float x=abs((i.uv.x+warp)*2-1);
  float softEdge=1-smoothstep(.24,.98,x);
  float cells=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,p*.46+float2(i.age.y*.31,0)).r;
  float fine=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,float2(p.y,-p.x)*.79+.37).r;
  float lace=smoothstep(.25,.66,cells*.68+fine*.32);
  float clumps=lerp(.32,1,smoothstep(.16,.74,SurfNoise(p*1.55)));
  float refined=softEdge*(.10+lace*.90)*clumps;
  return half4(MixFog(i.color.rgb,i.fog),i.color.a*lerp(legacy,refined,_BayWaterFinish));
 }
 ENDHLSL
 }
}
}

