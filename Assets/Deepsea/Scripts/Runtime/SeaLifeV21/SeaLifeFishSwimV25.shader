Shader "Deepsea/SeaLifeFishSwimV25" {
 Properties {
  [MainColor] _BaseColor("Base Color",Color)=(1,1,1,1)
  [MainTexture] _BaseMap("Albedo / Smoothness",2D)="white"{}
  _RimColor("Scale Iridescence",Color)=(.52,.88,.94,1)
  _Smoothness("Smoothness",Range(0,1))=.78
  _Metallic("Metallic",Range(0,1))=.25
  _TailAmplitude("Tail Amplitude",Range(0,.25))=.085
  _TailFrequency("Spatial Frequency",Range(1,12))=5.5
  _SwimPhase("Per Fish Phase",Float)=0
 }
 SubShader {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
  CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor,_RimColor;float4 _BaseMap_ST;
   half _Smoothness,_Metallic;float _TailAmplitude,_TailFrequency,_SwimPhase;
  CBUFFER_END
  struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
  struct Varyings{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;float2 uv:TEXCOORD2;};
  void Swim(inout float3 p,inout float3 n,float2 uv){
   float a=saturate(uv.x),mask=pow(a,1.65),phase=_SwimPhase-a*_TailFrequency;
   float wave=sin(phase)*_TailAmplitude*mask;
   float dwdz=-.92*_TailAmplitude*(1.65*pow(max(.0001,a),.65)*sin(phase)-mask*_TailFrequency*cos(phase));
   p.x+=wave+p.y*cos(phase)*_TailAmplitude*.28*mask;
   n.z-=n.x*dwdz;n=normalize(n);
  }
  Varyings vert(Attributes v){Varyings o;float3 p=v.positionOS.xyz,n=v.normalOS;Swim(p,n,v.uv);o.positionWS=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(n);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
  ENDHLSL
  Pass{
   Name "Forward" Tags{"LightMode"="UniversalForward"} Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   half4 frag(Varyings v,FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC):SV_Target{
    half3 n=normalize(v.normalWS)*IS_FRONT_VFACE(facing,1,-1),view=normalize(_WorldSpaceCameraPos-v.positionWS);
    Light sun=GetMainLight(TransformWorldToShadowCoord(v.positionWS));half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv);
    half3 albedo=tex.rgb*_BaseColor.rgb;
    half3 diffuse=max(SampleSH(n),half3(.14,.18,.20))+sun.color*saturate(dot(n,sun.direction))*lerp(.38h,1.0h,sun.shadowAttenuation);
    #if defined(_ADDITIONAL_LIGHTS)
    for(uint i=0;i<GetAdditionalLightsCount();i++){Light l=GetAdditionalLight(i,v.positionWS);diffuse+=l.color*saturate(dot(n,l.direction))*l.distanceAttenuation*.65;}
    #endif
    half3 h=normalize(sun.direction+view);half spec=pow(saturate(dot(n,h)),48)*_Smoothness*tex.a;
    half fres=pow(1-saturate(abs(dot(n,view))),2.8);
    half fin=step(.72h,v.uv.y)*step(v.uv.y,.98h),back=saturate(dot(-n,sun.direction))*fin*.2;
    return half4(albedo*(diffuse+back)+sun.color*spec*lerp(.12h,.42h,_Metallic)+_RimColor.rgb*fres*.22h,1);
   }
   ENDHLSL
  }
  Pass{
   Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 Cull Off
   HLSLPROGRAM
   #pragma vertex shadowVert
   #pragma fragment depthFrag
   float3 _LightDirection;
   float4 shadowVert(Attributes v):SV_POSITION{float3 p=v.positionOS.xyz,n=v.normalOS;Swim(p,n,v.uv);float3 w=TransformObjectToWorld(p);float4 c=TransformWorldToHClip(ApplyShadowBias(w,TransformObjectToWorldNormal(n),_LightDirection));
    #if UNITY_REVERSED_Z
    c.z=min(c.z,UNITY_NEAR_CLIP_VALUE);
    #else
    c.z=max(c.z,UNITY_NEAR_CLIP_VALUE);
    #endif
    return c;}
   half4 depthFrag():SV_Target{return 0;}
   ENDHLSL
  }
  Pass{
   Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0 Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment depthFrag
   half4 depthFrag():SV_Target{return 0;}
   ENDHLSL
  }
  Pass{
   Name "DepthNormals" Tags{"LightMode"="DepthNormals"} ZWrite On Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment normalFrag
   half4 normalFrag(Varyings v,FRONT_FACE_TYPE f:FRONT_FACE_SEMANTIC):SV_Target{return half4(normalize(v.normalWS)*IS_FRONT_VFACE(f,1,-1),0);}
   ENDHLSL
  }
 }
}
