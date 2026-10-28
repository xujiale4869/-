Shader "Deepsea/CoastSunGlintStudy"
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
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);return o;}
 float DeviceDepth(float2 uv){float depth=SampleSceneDepth(uv);
 #if !UNITY_REVERSED_Z
 depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
 #endif
 return depth;}

 // World-anchored statistical microfacets affect reflected sun light only.
 // They deliberately do not change the accepted low-frequency ripple normal,
 // transmission, scattering, refraction or the seabed caustic materials.
 float2 GlintHash22(float2 p)
 {
  float2 q=float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3)));
  return frac(sin(q)*43758.5453);
 }
 float GlintFacetLayer(float2 worldXZ,float2 baseSlope,float3 lightDir,float3 viewDir,float time,float cellScale,float seed)
 {
  float2 drift=float2(.06,-.035)*time*_GlintSpeed;
  float2 grid=(worldXZ+drift)*cellScale;
  float2 id=floor(grid),local=frac(grid);
  float2 u=clamp(GlintHash22(id+seed),.001,.999);
  float2 q=GlintHash22(id+seed+37.17);
  float population=step(q.y, saturate(_GlintDensity));
  // GGX slope-radius inverse CDF. The long tail allows rare facets steep enough
  // to reflect the *actual* sun in this camera; no camera-facing fake light.
  float radius=min(3.0,_GlintRoughness*sqrt(u.x/max(.002,1-u.x)));
  float angle=u.y*6.28318530718;
  float phase=q.x*6.28318530718;
  float t=time*_GlintSpeed;
  float2 tinyWaves=_GlintRoughness*.14*float2(sin(t*.75+phase),cos(t*.62+phase*1.37));
  float2 facetSlope=radius*float2(cos(angle),sin(angle))+baseSlope+tinyWaves;
  float3 facetNormal=normalize(float3(facetSlope.x,1,facetSlope.y));
  float3 halfDir=normalize(lightDir+viewDir);
  float alignment=pow(saturate(dot(facetNormal,halfDir)),160.0);
  float facing=step(.001,dot(facetNormal,lightDir))*step(.001,dot(facetNormal,viewDir));
  // An energy-aware pixel footprint broadens subpixel features while lowering
  // their peaks. Remote facets fade rather than turning into a white haze.
  float2 centre=.22+.56*q;
  float2 delta=local-centre;
  float2 physicalWidth=_GlintCoverage*float2(1,.68);
  float2 footprint=max(fwidth(grid),.0001);
  float2 variance=physicalWidth*physicalWidth+footprint*footprint/12.0;
  float energy=(physicalWidth.x*physicalWidth.y)/max(.00001,sqrt(variance.x*variance.y));
  float spot=exp(-2.0*dot(delta*delta,rcp(variance)))*energy;
  float subpixelFade=1-smoothstep(.75,1.8,max(footprint.x,footprint.y));
  return population*facing*alignment*spot*subpixelFade;
 }
 float SunGlitter(float2 worldXZ,float2 acceptedSlope,float3 lightDir,float3 viewDir,float time,float thickness)
 {
  float scale=max(3,_GlintScale);
  float fine=GlintFacetLayer(worldXZ,acceptedSlope,lightDir,viewDir,time,scale,13.71);
  float coarse=GlintFacetLayer(worldXZ+float2(.371,.817),acceptedSlope,lightDir,viewDir,time,scale*.61,83.27);
  float shoreFade=smoothstep(.035,.22,thickness);
  return saturate(fine+coarse*.65)*_GlintIntensity*shoreFade;
 }

 half4 Frag(V i):SV_Target{
 float2 screen=GetNormalizedScreenSpaceUV(i.p);float3 bottom=ComputeWorldSpacePosition(screen,DeviceDepth(screen),UNITY_MATRIX_I_VP);
 float thickness=max(0,i.world.y-bottom.y);
 if(_CoastWaterDepthProbe>.5)return half4(saturate(thickness/25),0,0,1);
 float2 p=i.world.xz;float t=_Time.y;
 float2 a=SAMPLE_TEXTURE2D(_WaveMap,sampler_WaveMap,p*.063+float2(t*.006,t*.003)).rg*2-1;
 float2 b=SAMPLE_TEXTURE2D(_WaveMap,sampler_WaveMap,p*.109-float2(t*.004,t*.005)).rg*2-1;
 float2 slope=(a*.17+b*.11)*_CoastWaterDetails;
 slope+=float2(sin(p.x*.64+p.y*.32+t*.9),cos(p.y*.72-p.x*.22+t*.7))*.025*_CoastWaterDetails;
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
 Light sun=GetMainLight();float glint=pow(saturate(dot(n,normalize(sun.direction+v))),180);
 c+=sun.color*glint*.7*_CoastWaterDetails;
 // Baseline glint remains byte-for-byte above; the new layer is independently
 // switchable, so _GlintEnable=0 retains the original water appearance.
 if(_GlintEnable>.001&&_CoastWaterDetails>.001&&_WorldSpaceCameraPos.y>i.world.y+.05)
 {
  Light glitterSun=GetMainLight(TransformWorldToShadowCoord(i.world));
  float glitter=SunGlitter(p,slope,glitterSun.direction,v,t,thickness);
  c+=min(glitterSun.color*glitter,half3(3,3,3))*glitterSun.shadowAttenuation*_GlintEnable*_CoastWaterDetails;
 }

 // Thin, broken foam follows the actual depth intersection, with a gentle incoming pulse.
 float edge=.5+.28*sin(p.x*4.6+sin(p.y*2.1)+t*.8)+.22*sin(p.y*6.1-p.x*2.3-t*.5);
 float pulse=.55+.45*sin(t*1.5+p.x*.33+p.y*.27);
 float width=.055+.095*pulse;
 float foam=smoothstep(.008,.025,thickness)*(1-smoothstep(width*.45,width,thickness));
 foam*=smoothstep(.58,.85,edge)*(.6+.4*pulse)*_CoastWaterDetails;
 // A bounded V wake follows real velocity. It vanishes when the boat is moored.
 float2 d=p-_CoastBoat.xy;float2 heading=normalize(_CoastBoatHeading.xy+float2(.00001,0));
 float aft=-dot(d,heading),side=dot(d,float2(heading.y,-heading.x));
 float trail=aft-2.4,spread=.6+max(trail,0)*.29;
 float wave=exp(-pow((abs(side)-spread)/(.10+max(trail,0)*.025),2));
 float wake=wave*smoothstep(0,.8,trail)*(1-smoothstep(3,10,trail));
 wake*=saturate(_CoastBoat.z/2.5)*(.65+.35*sin(trail*5-t*4))*_CoastBoat.w*_CoastWaterDetails;
 c=lerp(c,half3(.62,.78,.71),saturate(foam*.42+wake*.48));
 // Underwater cave views retain the same colour family as submerged rock fog.
 if(_WorldSpaceCameraPos.y<i.world.y-.05)c=lerp(scene,half3(.018,.10,.13),.18);
 return half4(c,1);
 }
 ENDHLSL
 }
 }
}
