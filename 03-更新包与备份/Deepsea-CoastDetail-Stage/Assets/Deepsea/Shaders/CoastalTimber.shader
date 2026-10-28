Shader "Deepsea/CoastalTimber" {
 Properties{_BaseColor("Timber",Color)=(.48,.40,.29,1) _BoardSeed("Variation",Float)=0}
 SubShader{
 Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
 Pass{
 Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;float _BoardSeed;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float3 local:TEXCOORD2;float fog:TEXCOORD3;};
 V vert(A a){V o;o.local=a.p.xyz;o.world=TransformObjectToWorld(a.p.xyz);o.normal=TransformObjectToWorldNormal(a.n);o.p=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{
  float grain=sin(i.local.z*67+sin(i.local.x*9+_BoardSeed)*1.4);
  float stripes=sin(i.local.z*125+i.local.x*3+_BoardSeed);
  float fine=1-smoothstep(.10,.22,max(length(ddx(i.world)),length(ddy(i.world))));
  float wear=.95+.045*sin(_BoardSeed*3.1)+(grain*.025+stripes*.012)*fine;
  float edge=smoothstep(.42,.50,max(abs(i.local.x),abs(i.local.z)))*saturate(i.normal.y);
  float3 c=_BaseColor.rgb*wear*(1-edge*.14);
  c=lerp(c,c*float3(.62,.73,.70),1-smoothstep(-.12,.22,i.world.y));
  Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
  c*=.70+.40*saturate(dot(normalize(i.normal),sun.direction))*sun.shadowAttenuation;
  return half4(MixFog(c,i.fog),1);
 }
 ENDHLSL
 }
 UsePass "Deepsea/CoastalGround/DepthOnly"
 UsePass "Deepsea/CoastalGround/ShadowCaster"
 }
}
