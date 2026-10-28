Shader "Deepsea/SeaLifeWaterV22"
{
 Properties{_Shallow("Shallow scatter",Color)=(.025,.36,.50,1) _Deep("Deep scatter",Color)=(.008,.065,.105,1) _WaveMap("Tileable ripple slopes",2D)="gray"{} _Refraction("Refraction",Float)=.0022
  _GlintEnable("Extra sun glitter",Range(0,1))=1
  _GlintIntensity("Glitter peak intensity",Range(0,3))=2.4
  _GlintScale("Glitter cells per metre",Range(3,24))=8
  _GlintRoughness("Glitter facet distribution",Range(.08,.7))=.52
  _GlintDensity("Glitter cell population",Range(0,1))=.9
  _GlintCoverage("Glitter footprint in cell",Range(.05,.25))=.25
  _GlintSpeed("Glitter motion",Range(0,2))=.6
 }
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass{
 Name "FoundationWater" Tags{"LightMode"="UniversalForward"} Blend One Zero ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #define _SURFACE_TYPE_TRANSPARENT 1
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
 TEXTURE2D(_WaveMap);SAMPLER(sampler_WaveMap);
 CBUFFER_START(UnityPerMaterial)
 half4 _Shallow,_Deep;float _Refraction;
 float _GlintEnable,_GlintIntensity,_GlintScale,_GlintRoughness,_GlintDensity,_GlintCoverage,_GlintSpeed;
 CBUFFER_END
 float _CoastWaterDepthProbe,_CoastWaterDetails;float4 _CoastBoat,_CoastBoatHeading;
 #include "WaterV22Waves.hlsl"
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.world.y+=WaterV22Field(o.world.xz,_WaterV22Time).x;o.p=TransformWorldToHClip(o.world);return o;}
 float DeviceDepth(float2 uv){float depth=SampleSceneDepth(uv);
 #if !UNITY_REVERSED_Z
 depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
 #endif
 return depth;}

 half4 Frag(V i):SV_Target{
 float2 screen=GetNormalizedScreenSpaceUV(i.p);float3 bottom=ComputeWorldSpacePosition(screen,DeviceDepth(screen),UNITY_MATRIX_I_VP);
 float thickness=max(0,i.world.y-bottom.y);
 if(_CoastWaterDepthProbe>.5)return half4(saturate(thickness/25),0,0,1);
 float2 p=i.world.xz;float t=_WaterV22Time;
 float2 a=SAMPLE_TEXTURE2D(_WaveMap,sampler_WaveMap,p*.063+float2(t*.006,t*.003)).rg*2-1;
 float2 b=SAMPLE_TEXTURE2D(_WaveMap,sampler_WaveMap,p*.109-float2(t*.004,t*.005)).rg*2-1;
 float3 field=WaterV22Field(p,t);float2 slope=(a*.105+b*.065)*_CoastWaterDetails-field.yz;
 slope+=float2(sin(p.x*.64+p.y*.32+t*.9),cos(p.y*.72-p.x*.22+t*.7))*.014*_CoastWaterDetails;
 float3 n=normalize(float3(slope.x,1,slope.y)),v=GetWorldSpaceNormalizeViewDir(i.world);
 float2 offset=slope*_Refraction*smoothstep(.12,1,thickness);float2 refractedUV=saturate(screen+offset);
 float3 shiftedBottom=ComputeWorldSpacePosition(refractedUV,DeviceDepth(refractedUV),UNITY_MATRIX_I_VP);
 // Reject shifted pixels above the waterline, so the bank and hull do not bleed into the water.
 if(shiftedBottom.y>i.world.y-.045)refractedUV=screen;
 half3 scene=SampleSceneColor(refractedUV);
 // Beer-Lambert transmission keeps the real seabed visible and removes red first with depth.
 // Oblique light travels farther through the water.
 float opticalDepth=min(thickness/max(.42,abs(v.y)),60);
 float3 transmission=exp(-opticalDepth*float3(.95,.15,.08));
 half3 scatter=lerp(_Shallow.rgb,_Deep.rgb,1-exp(-thickness*.13));
 half3 c=scene*transmission+scatter*(1-transmission);
 float fresnel=.02+.98*pow(1-saturate(dot(n,v)),5);
 float3 reflected=reflect(-v,n);half3 reflection=GlossyEnvironmentReflection(reflected,i.world,.12,1);
 half3 sky=lerp(half3(.13,.22,.25),half3(.34,.48,.55),saturate(reflected.y));
 reflection=max(reflection*.55,sky*.45);c=lerp(c,reflection,saturate(fresnel*1.8));
 Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
 // Broad, restrained sun reflection; no random bright microfacet cells.
 float highlight=pow(saturate(dot(n,normalize(sun.direction+v))),48);
 c+=sun.color*highlight*.16*sun.shadowAttenuation*_CoastWaterDetails;
 // Slow coherent reflection bands make movement readable without whitening the bay.
 float band=.5+.5*sin(p.x*.72+p.y*.36-t*1.12);
 c+=half3(.003,.006,.007)*(band-.5)*smoothstep(.25,1.2,thickness);
 // Thin, broken foam follows the actual depth intersection, with a gentle incoming pulse.
 float edge=.5+.28*sin(p.x*4.6+sin(p.y*2.1)+t*.8)+.22*sin(p.y*6.1-p.x*2.3-t*.5);
 float pulse=.55+.45*sin(t*1.5+p.x*.33+p.y*.27);
 float width=.055+.095*pulse;
 float foam=smoothstep(.008,.025,thickness)*(1-smoothstep(width*.45,width,thickness));
 foam*=smoothstep(.58,.85,edge)*(.6+.4*pulse)*_CoastWaterDetails;
 // Bow pressure is speed driven; the historical stern wake is a separate bounded mesh.
 float2 d=p-_CoastBoat.xy,heading=normalize(_CoastBoatHeading.xy+float2(.00001,0));
 float forward=dot(d,heading),side=dot(d,float2(heading.y,-heading.x));
 float speed=saturate(_CoastBoat.z/3.2)*_CoastBoat.w;
 float bow=exp(-pow((forward-(2.05-abs(side)*.40))/.13,2))*(1-smoothstep(.65,1.05,abs(side)))*speed;
 float ring=sqrt(pow(forward/2.3,2)+pow(side/1.03,2));
 float lap=exp(-pow((ring-1.03)/.10,2))*(.55+.45*sin(t*1.7+ring*12));
 c=lerp(c,half3(.53,.75,.76),saturate(foam*.35+bow*.15+lap*.018));
 // Underwater cave views retain the same colour family as submerged rock fog.
 if(_WorldSpaceCameraPos.y<i.world.y-.05)c=lerp(scene,half3(.018,.10,.13),.18);
 return half4(c,1);
 }
 ENDHLSL
 }
 }
}
