Shader "Deepsea/SeaLifeAquariumGlassV25" {
 Properties{
  [MainColor] _BaseColor("Tint",Color)=(.42,.84,.90,.045)
  _RimColor("Beveled Edge",Color)=(.78,.96,1,.24)
  _CausticsMap("Caustics",2D)="black"{}
  _CausticsOffset("Scroll",Vector)=(0,0,0,0)
  [Enum(Glass,0,WaterSurface,1,Caustics,2)] _Mode("Layer",Float)=0
 }
 SubShader{
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass{
   Tags{"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Varyings{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;float2 uv:TEXCOORD2;float localHeight:TEXCOORD3;};
   TEXTURE2D(_CausticsMap);SAMPLER(sampler_CausticsMap);
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor,_RimColor;float4 _CausticsOffset;float _Mode;
   CBUFFER_END
   Varyings vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=v.uv;o.localHeight=v.positionOS.y;return o;}
   half4 frag(Varyings v):SV_Target{
    half3 n=normalize(v.normalWS),view=normalize(_WorldSpaceCameraPos-v.positionWS);
    half f=pow(1-saturate(abs(dot(n,view))),3.5);
    half c=min(SAMPLE_TEXTURE2D(_CausticsMap,sampler_CausticsMap,v.uv*2.2+_CausticsOffset.xy).r,SAMPLE_TEXTURE2D(_CausticsMap,sampler_CausticsMap,v.uv*1.7+_CausticsOffset.zw).r);
    if(_Mode>1.5)return half4(_BaseColor.rgb,saturate(c*c*_BaseColor.a));
    if(_Mode>.5)return half4(_BaseColor.rgb+f*.10,c*.022+_BaseColor.a+f*.035);
    // Thin clear glass with restrained edge reflection; height is local to each tank.
    half height=saturate((v.localHeight-.8)/1.4);
    half3 color=lerp(_BaseColor.rgb,_RimColor.rgb,f*.65)+half3(.03,.045,.05)*height;
    return half4(color,saturate(_BaseColor.a+f*_RimColor.a*.22));
   }
   ENDHLSL
  }
 }
}
