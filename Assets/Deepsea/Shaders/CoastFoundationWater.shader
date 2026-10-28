Shader "Deepsea/CoastFoundationWater"
{
 Properties{_Shallow("Shallow scatter",Color)=(.025,.36,.50,1) _Deep("Deep scatter",Color)=(.008,.065,.105,1) _WaveMap("Tileable ripple slopes",2D)="gray"{} _Refraction("Refraction",Float)=.0022}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass{
 Name "FoundationWater" Tags{"LightMode"="UniversalForward"} Blend One Zero ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
 TEXTURE2D(_WaveMap);SAMPLER(sampler_WaveMap);
 CBUFFER_START(UnityPerMaterial)
 half4 _Shallow,_Deep;float _Refraction;
 CBUFFER_END
 float _CoastWaterDepthProbe,_CoastWaterDetails;float4 _CoastBoat,_CoastBoatHeading;
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);return o;}
 float DeviceDepth(float2 uv){float depth=SampleSceneDepth(uv);
 #if !UNITY_REVERSED_Z
 depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
 #endif
 return depth;}
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
