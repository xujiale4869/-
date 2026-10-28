Shader "Deepsea/CoastHeroCloth" {
 Properties{_BaseColor("Colour",Color)=(.12,.24,.28,1) _BaseMap("Fabric diffuse",2D)="white"{} _BumpMap("Fabric normal",2D)="bump"{} _Fabric("Fabric texture",Float)=1 _ColourTexture("Original colour texture",Float)=0 _Smoothness("Smoothness",Float)=.1 _Metallic("Metal",Float)=0}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;float _Fabric,_ColourTexture,_Smoothness,_Metallic;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 tangent:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 tangent:TEXCOORD2;float2 uv:TEXCOORD3;};
 V Vert(A i){UNITY_SETUP_INSTANCE_ID(i);V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.tangent=float4(TransformObjectToWorldDir(i.tangent.xyz),i.tangent.w*GetOddNegativeScale());o.uv=i.uv;return o;}
 ENDHLSL
 Pass{Name "HeroForward" Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_instancing
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 half4 Frag(V i):SV_Target{
 half3 n=normalize(i.n);half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv*lerp(2,1,_ColourTexture)).rgb;
 half lum=dot(tex,half3(.2126,.7152,.0722));half3 albedo=_BaseColor.rgb*lerp(lerp(half3(1,1,1),half3(.7+lum*.8,.7+lum*.8,.7+lum*.8),_Fabric),tex,_ColourTexture);
 half3 nt=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv*2),.24*_Fabric);
 half3 tangent=normalize(i.tangent.xyz);half3 bitangent=cross(n,tangent)*i.tangent.w;n=normalize(nt.x*tangent+nt.y*bitangent+nt.z*n);
 Light light=GetMainLight(TransformWorldToShadowCoord(i.w));half diffuse=saturate(dot(n,light.direction));
 half3 c=albedo*(max(half3(.12,.16,.17),SampleSH(n))+light.color*(.12+.88*diffuse)*light.shadowAttenuation);
 float3 view=GetWorldSpaceNormalizeViewDir(i.w);half spec=pow(saturate(dot(n,normalize(light.direction+view))),lerp(20,100,_Smoothness))*lerp(.025,.20,_Metallic);
 return half4(c+light.color*spec*light.shadowAttenuation,1);
 }
 ENDHLSL
 }
 Pass{Name "HeroDepth" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Depth
 #pragma multi_compile_instancing
 half4 Depth(V i):SV_Target{return 0;}
 ENDHLSL
 }
 Pass{Name "HeroShadow" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex ShadowVert
 #pragma fragment Depth
 #pragma multi_compile_instancing
 float3 _LightDirection;
 V ShadowVert(A a){V o=Vert(a);o.p=TransformWorldToHClip(ApplyShadowBias(o.w,normalize(o.n),_LightDirection));
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
