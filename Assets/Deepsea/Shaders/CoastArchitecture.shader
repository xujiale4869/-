Shader "Deepsea/CoastArchitecture" {
 Properties {
  _BaseMap("Base colour",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _MaskMap("AO Roughness Metal",2D)="white"{}
  _BaseColor("Material tint",Color)=(1,1,1,1) _EmissionColor("Emission",Color)=(0,0,0,1)
  _TextureWeight("Texture weight",Float)=1 _UseMask("ARM enabled",Float)=0 _NormalStrength("Normal strength",Float)=.35
  _Smoothness("Smoothness",Float)=.25 _Metallic("Metallic",Float)=0 _UseVertexColor("Use vertex colour",Float)=0
  _SrcBlend("Source blend",Float)=1 _DstBlend("Destination blend",Float)=0 _ZWrite("Depth write",Float)=1
 }
 SubShader {
 Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Back
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_EmissionColor;float _TextureWeight,_UseMask,_NormalStrength,_Smoothness,_Metallic,_UseVertexColor;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;half4 colour:COLOR;};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;half4 colour:COLOR;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=a.uv;o.colour=a.colour;return o;}
 ENDHLSL
 Pass {Name "FoundationArchitecture" Tags{"LightMode"="UniversalForward"} Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite]
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 half4 Frag(V i):SV_Target {
  float3 geom=normalize(i.n),t=normalize(i.t.xyz),b=cross(geom,t)*i.t.w;
  float3 bump=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv));bump.xy*=_NormalStrength;bump=normalize(bump);
  float3 n=normalize(t*bump.x+b*bump.y+geom*bump.z);
  half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
  half3 arm=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,i.uv).rgb;
  SurfaceData surface=(SurfaceData)0;
  surface.albedo=_BaseColor.rgb*lerp(half3(1,1,1),tex,_TextureWeight)*lerp(half3(1,1,1),i.colour.rgb,_UseVertexColor);
  surface.metallic=_Metallic;surface.specular=half3(.04,.04,.04);surface.smoothness=lerp(_Smoothness,1-arm.g,_UseMask);
  surface.normalTS=half3(0,0,1);surface.occlusion=lerp(1,lerp(.6,1,arm.r),_UseMask);surface.emission=_EmissionColor.rgb;surface.alpha=_BaseColor.a;
  InputData input=(InputData)0;input.positionWS=i.world;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
  input.shadowCoord=TransformWorldToShadowCoord(i.world);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);
  half4 colour=UniversalFragmentPBR(input,surface);return half4(colour.rgb,_BaseColor.a);
 }
 ENDHLSL
 }
 Pass{Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Depth
 half4 Depth(V i):SV_Target{return 0;}
 ENDHLSL
 }
 Pass{Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex ShadowVert
 #pragma fragment Depth
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
