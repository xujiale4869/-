Shader "Deepsea/CoastRockLit" {
Properties {
_BaseMap("Albedo",2D)="white"{} _DetailMap("Ground cover",2D)="white"{} _SandMap("Sand",2D)="white"{} _Ground("Terrain",Float)=0 _Desaturate("Desaturate",Float)=0 _BumpMap("Normal",2D)="bump"{} _BaseColor("Color",Color)=(1,1,1,1)
_BumpScale("Normal strength",Float)=.6 _Triplanar("World mapping",Float)=0 _Scale("Scale",Float)=.15 _Moss("Moss",Float)=0 _AlphaClip("Cutout",Float)=0 _Cutoff("Alpha cutoff",Float)=.4 _Sway("Wind",Float)=0 [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
}
SubShader {Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull [_Cull]
HLSLINCLUDE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "../ThirdParty/BoatAttack/BayCaustics.hlsl"
TEXTURE2D(_DetailMap);SAMPLER(sampler_DetailMap);TEXTURE2D(_SandMap);SAMPLER(sampler_SandMap);TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor,_BaseMap_ST;float _BumpScale,_Triplanar,_Scale,_Moss,_AlphaClip,_Cutoff,_Sway,_Ground,_Desaturate;
CBUFFER_END
struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;};
struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;float fog:TEXCOORD4;};
V vert(A a){V o;float3 p=TransformObjectToWorld(a.p.xyz);float w=saturate((p.y-UNITY_MATRIX_M._m13)/5);p.x+=sin(_Time.y*.85+p.y*.6+UNITY_MATRIX_M._m03)*_Sway*w;p.z+=cos(_Time.y*.65+UNITY_MATRIX_M._m23)*_Sway*w*.5;o.world=p;o.p=TransformWorldToHClip(p);o.n=TransformObjectToWorldNormal(a.n);o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.fog=ComputeFogFactor(o.p.z);return o;}
half4 Albedo(V i){half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);if(_Triplanar>1.5)c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.world.xz*_Scale);else if(_Triplanar>.5){float3 w=pow(abs(normalize(i.n)),4);w/=max(.001,w.x+w.y+w.z);c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.world.zy*_Scale)*w.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.world.xz*_Scale)*w.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.world.xy*_Scale)*w.z;}if(_Ground>.5){float pattern=.5+.25*sin(i.world.x*.37+sin(i.world.z*.28))*sin(i.world.z*.42);float3 grass=SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,i.world.xz*_Scale*.74).rgb;c.rgb=lerp(c.rgb,grass,smoothstep(.22,.70,pattern));float radius=length((i.world.xz-float2(8,94))/float2(38,42));float3 sand=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.world.xz*.18).rgb;c.rgb=lerp(c.rgb,sand,smoothstep(.82,.97,radius));}clip(lerp(1,c.a-_Cutoff,_AlphaClip));c.rgb=lerp(c.rgb,dot(c.rgb,float3(.299,.587,.114)).xxx,_Desaturate);return c*_BaseColor;}
void ClipAlpha(V i){if(_AlphaClip>.5)clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);}
ENDHLSL
Pass {Name "ForwardLit" Tags{"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma target 3.5
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#pragma multi_compile_fog
half4 frag(V i):SV_Target{half4 albedo=Albedo(i);float3 n=normalize(i.n);if(_Triplanar<.5){float3 nt=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);float3 t=normalize(i.t.xyz);if(dot(t,t)>.1)n=normalize(t*nt.x+cross(n,t)*i.t.w*nt.y+n*nt.z);}Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));float nl=saturate(dot(n,sun.direction));if(_AlphaClip>.5)nl=abs(dot(n,sun.direction))*.8+.2;float3 ambient=max(float3(.08,.1,.11),SampleSH(n));float3 c=albedo.rgb*(ambient+sun.color*(.12+nl*.88)*sun.shadowAttenuation);float moss=_Moss*saturate(n.y)*smoothstep(.2,.8,.5+.25*sin(i.world.x*.6)*sin(i.world.z*.7));c=lerp(c,c*float3(.52,.67,.33),moss);float wet=1-smoothstep(-.25,.35,i.world.y);c*=1-.22*wet;float3 h=normalize(sun.direction+GetWorldSpaceNormalizeViewDir(i.world));c+=sun.color*pow(saturate(dot(n,h)),72)*(.025+wet*.12)*sun.shadowAttenuation;if(i.world.y<-.3)c+=BayCaustics(i.world,sun.direction)*.55;return half4(MixFog(c,i.fog),1);}
ENDHLSL
}
Pass {Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
HLSLPROGRAM
#pragma target 3.5
#pragma vertex vert
#pragma fragment depth
half4 depth(V i):SV_Target{ClipAlpha(i);return 0;}
ENDHLSL
}
Pass {Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
HLSLPROGRAM
#pragma target 3.5
#pragma vertex vertShadow
#pragma fragment depth
float3 _LightDirection;
V vertShadow(A a){V o=vert(a);o.p=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.n),_LightDirection));
#if UNITY_REVERSED_Z
o.p.z=min(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
#else
o.p.z=max(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
#endif
return o;}
half4 depth(V i):SV_Target{ClipAlpha(i);return 0;}
ENDHLSL
}
}
}
