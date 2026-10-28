Shader "Deepsea/ReefFish" {
 Properties {
  _Tint("Colour",Color)=(.55,.8,.78,1)
  _Submerged("Underwater silhouette",Float)=0
  _Phase("Tail phase",Float)=0
 }
 SubShader {
  Tags{"RenderType"="Transparent" "Queue"="Transparent+5" "RenderPipeline"="UniversalPipeline"}
  Pass {
   Tags{"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest Always Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint;float _Submerged,_Phase;
   CBUFFER_END
   struct A{float4 p:POSITION;float3 n:NORMAL;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 screen:TEXCOORD2;float fog:TEXCOORD3;float3 local:TEXCOORD4;};
   V vert(A i){
    V o;o.local=i.p.xyz;float tail=1-smoothstep(-.45,.2,i.p.z);
    i.p.x+=sin(_Time.y*10-i.p.z*7+_Phase)*.045*tail;
    o.world=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);
    o.p=TransformWorldToHClip(o.world);o.screen=ComputeScreenPos(o.p);
    o.fog=ComputeFogFactor(o.p.z);return o;
   }
   half4 frag(V i):SV_Target{
    float2 uv=i.screen.xy/i.screen.w;float raw=SampleSceneDepth(uv);
    #if UNITY_REVERSED_Z
    bool valid=raw>.00001;
    #else
    bool valid=raw<.99999;raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
    #endif
    float3 view=GetWorldSpaceNormalizeViewDir(i.world);
    if(valid){
     float3 scene=ComputeWorldSpacePosition(uv,raw,UNITY_MATRIX_I_VP);
     // The sea writes depth. Resolve opaque occluders separately so fish can be
     // seen through shallow water, but never through land, a hull or a pier.
     clip(.035-dot(scene-i.world,view));
    }
    float3 n=normalize(i.n);Light sun=GetMainLight();
    half shade=.68+.32*abs(dot(n,sun.direction));
    float stripes=smoothstep(.74,.91,sin(i.local.z*25));
    half3 c=_Tint.rgb*shade*lerp(1,.50,stripes);
    c+=pow(1-saturate(abs(dot(n,view))),3)*half3(.08,.13,.14);
    c=lerp(c,half3(.07,.39,.36),_Submerged*.20);
    c+=pow(saturate(abs(dot(n,normalize(view+sun.direction)))),64)*.14*_Submerged;
    half alpha=_Tint.a*lerp(1,.86,_Submerged);
    return half4(MixFog(c,i.fog),alpha);
   }
   ENDHLSL
  }
 }
}
