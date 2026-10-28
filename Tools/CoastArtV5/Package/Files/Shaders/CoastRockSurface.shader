Shader "Deepsea/CoastRockSurface" {
 Properties{_BaseMap("Scanned rock colour",2D)="white"{} _BumpMap("Scanned rock normal",2D)="bump"{} _RoughMap("Scanned roughness",2D)="white"{} _CoverMap("Coastal ground cover",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _NormalStrength("Normal strength",Float)=.4}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Back
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);TEXTURE2D(_RoughMap);SAMPLER(sampler_RoughMap);TEXTURE2D(_CoverMap);SAMPLER(sampler_CoverMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _Tint;float _NormalStrength;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;};
 V Vert(A a){UNITY_SETUP_INSTANCE_ID(a);V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=a.uv;return o;}
 ENDHLSL
 Pass{Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_instancing
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 half4 Frag(V i):SV_Target{
 float3 geom=normalize(i.n),t=normalize(i.t.xyz),b=cross(geom,t)*i.t.w;
 float3 bump=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv));bump.xy*=_NormalStrength;bump=normalize(bump);
 float3 n=normalize(t*bump.x+b*bump.y+geom*bump.z);
 half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;half luminance=dot(base,half3(.299,.587,.114));base=lerp(luminance.xxx,base,.25)*_Tint.rgb;
 float roughness=SAMPLE_TEXTURE2D(_RoughMap,sampler_RoughMap,i.uv).r;
 // Cover has its own ground-space texture, while rock keeps its original baked UVs.
 float patch=.5+.23*sin(i.world.x*.37+sin(i.world.z*.29)*1.8)+.18*sin(i.world.z*.61-i.world.x*.19);
 float moss=smoothstep(.32,.77,geom.y)*smoothstep(.3,1.5,i.world.y)*smoothstep(.31,.70,patch)*.72;
 if(moss>.01){half3 cover=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,i.world.xz*.7).rgb;float lum=dot(cover,half3(.299,.587,.114));base=lerp(base,half3(.095,.15,.047)*(.60+lum*2.2),moss);}
 float wet=1-smoothstep(-.3,.55,i.world.y);base*=lerp(half3(1,1,1),half3(.58,.70,.66),wet);
 Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));half3 ambient=max(half3(.055,.075,.08),SampleSH(n));
 half3 c=base*(ambient+sun.color*(.045+.955*saturate(dot(n,sun.direction)))*sun.shadowAttenuation);
 float3 h=normalize(sun.direction+GetWorldSpaceNormalizeViewDir(i.world));c+=sun.color*pow(saturate(dot(n,h)),lerp(12,90,1-roughness*.8))*(.01+wet*.09)*sun.shadowAttenuation;
 float depth=saturate((-i.world.y-.7)/16.3);c*=lerp(half3(1,1,1),half3(.35,.68,.71),depth*.65);
 float under=saturate((-.22-_WorldSpaceCameraPos.y)*.4);c=lerp(c,half3(.018,.10,.13),(1-exp(-distance(_WorldSpaceCameraPos,i.world)*.038))*under);
 return half4(c,1);
 }
 ENDHLSL
 }
 Pass{Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Depth
 #pragma multi_compile_instancing
 half4 Depth(V i):SV_Target{return 0;}
 ENDHLSL
 }
 Pass{Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex ShadowVert
 #pragma fragment Depth
 #pragma multi_compile_instancing
 float3 _LightDirection;
 V ShadowVert(A a){V o=Vert(a);o.p=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.n),_LightDirection));
 #if UNITY_REVERSED_Z
 o.p.z=min(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
 #else
 o.p.z=max(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
 #endif
 return o;}
 half4 Depth(V i):SV_Target{return 0;}
 ENDHLSL
 }
 }
}
