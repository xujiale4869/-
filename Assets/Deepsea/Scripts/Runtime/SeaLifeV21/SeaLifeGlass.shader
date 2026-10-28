Shader "Deepsea/SeaLifeGlass" {
 Properties { [MainColor] _BaseColor("Glass tint",Color)=(0.38,0.76,0.79,0.08) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
   Varyings vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o;}
   half4 frag(Varyings v):SV_Target {half f=pow(1-saturate(abs(dot(normalize(v.normalWS),normalize(_WorldSpaceCameraPos-v.positionWS)))),3);return half4(_BaseColor.rgb,_BaseColor.a+f*.08);}
   ENDHLSL
  }
 }
}
