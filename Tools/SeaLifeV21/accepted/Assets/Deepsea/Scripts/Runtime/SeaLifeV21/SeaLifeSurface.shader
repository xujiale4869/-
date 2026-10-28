Shader "Deepsea/SeaLifeSurface" {
 Properties {
  [MainColor] _BaseColor("Color",Color)=(0.7,0.7,0.7,1)
  [MainTexture] _BaseMap("Albedo",2D)="white"{}
  _Smoothness("Smoothness",Range(0,1))=.25
  _Metallic("Metallic",Range(0,1))=0
  _Cutoff("Cutoff",Range(0,1))=.5
  _Cull("Cull",Float)=2
 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass { Name "Forward" Tags {"LightMode"="UniversalForward"} Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial) half4 _BaseColor;float4 _BaseMap_ST;half _Smoothness;half _Metallic;half _Cutoff; CBUFFER_END
   Varyings vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
   half4 frag(Varyings v):SV_Target {
    half3 n=normalize(v.normalWS);Light light=GetMainLight(TransformWorldToShadowCoord(v.positionWS));
    half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).rgb*_BaseColor.rgb;
    half diffuse=saturate(dot(n,light.direction));half shadow=lerp(.30h,1.0h,light.shadowAttenuation);
    half3 lighting=max(SampleSH(n),half3(.09,.11,.11))+light.color*diffuse*shadow;
    half3 h=normalize(light.direction+normalize(_WorldSpaceCameraPos-v.positionWS));half spec=pow(saturate(dot(n,h)),lerp(12.0h,110.0h,_Smoothness))*_Smoothness*shadow;
    return half4(base*lighting+light.color*spec*lerp(.08h,.38h,_Metallic),1);
   }
   ENDHLSL
  }
  Pass { Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vertDepth
   #pragma fragment fragDepth
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float4 vertDepth(float4 p:POSITION):SV_POSITION {return TransformObjectToHClip(p.xyz);}
   half4 fragDepth():SV_Target {return 0;}
   ENDHLSL
  }
  Pass { Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask 0 Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vertDepth
   #pragma fragment fragDepth
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float4 vertDepth(float4 p:POSITION):SV_POSITION {return TransformObjectToHClip(p.xyz);}
   half4 fragDepth():SV_Target {return 0;}
   ENDHLSL
  }
  Pass { Name "DepthNormals" Tags {"LightMode"="DepthNormals"} ZWrite On Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vertNormal
   #pragma fragment fragNormal
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Input {float4 p:POSITION;float3 n:NORMAL;};
   struct Output {float4 p:SV_POSITION;float3 n:TEXCOORD0;};
   Output vertNormal(Input i){Output o;o.p=TransformObjectToHClip(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);return o;}
   half4 fragNormal(Output i):SV_Target {return half4(normalize(i.n),0);}
   ENDHLSL
  }
 }
}
