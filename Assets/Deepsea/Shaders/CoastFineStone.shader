Shader "Deepsea/CoastFineStone" {
 Properties {
  _BaseMap("Baked base colour (sRGB)",2D)="white"{}
  _BumpMap("Matched high-model tangent normal",2D)="bump"{}
  _AOMap("Baked occlusion (linear)",2D)="white"{}
  _RoughnessMap("Baked roughness (linear)",2D)="white"{}
  _CausticMap("Caustic light",2D)="black"{}
  _BaseColor("Tint",Color)=(1,1,1,1)
  _NormalStrength("Matched normal strength",Range(0,1))=1
 }
 SubShader {
  Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Back
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
  TEXTURE2D(_AOMap);SAMPLER(sampler_AOMap);TEXTURE2D(_RoughnessMap);SAMPLER(sampler_RoughnessMap);
  TEXTURE2D(_CausticMap);SAMPLER(sampler_CausticMap);
  CBUFFER_START(UnityPerMaterial)
  half4 _BaseColor;float _NormalStrength;
  CBUFFER_END
  float _ShowcaseClay;
  struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID};
  V Vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=a.uv;return o;}
  ENDHLSL
  Pass{Name "FineStoneLit" Tags{"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   half4 Frag(V i):SV_Target{
    UNITY_SETUP_INSTANCE_ID(i);float3 geom=normalize(i.n),tangent=normalize(i.t.xyz);
    float3 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv));
    float3 bump=tangent*normalTS.x+cross(geom,tangent)*i.t.w*normalTS.y+geom*normalTS.z;
    float3 n=normalize(lerp(geom,bump,_NormalStrength));
    half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
    half rough=clamp(SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,i.uv).r,.24,.96);
    half ao=saturate(SAMPLE_TEXTURE2D(_AOMap,sampler_AOMap,i.uv).r);
    float wet=1-smoothstep(-.30,.46,i.w.y);albedo*=lerp(half3(1,1,1),half3(.89,.94,.93),wet);
    if(_ShowcaseClay>.5){albedo=half3(.52,.52,.52);n=geom;rough=.95;ao=1;}
    SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.metallic=0;s.specular=half3(.04,.04,.04);s.smoothness=1-rough;s.normalTS=half3(0,0,1);s.occlusion=ao;s.alpha=1;
    InputData input=(InputData)0;input.positionWS=i.w;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);input.shadowCoord=TransformWorldToShadowCoord(i.w);input.bakedGI=max(half3(.075,.11,.13),SampleSH(n));input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);
    half3 c=UniversalFragmentPBR(input,s).rgb;Light sun=GetMainLight(input.shadowCoord);float depth=-.22-i.w.y;
    if(_ShowcaseClay<.5){
     if(depth>.12&&depth<12&&geom.y>.15){float2 cuv=i.w.xz*.11+.04*float2(sin(i.w.z*.6+_Time.y*.3),cos(i.w.x*.5-_Time.y*.23));float ca=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,cuv+float2(_Time.y*.002,0)).r;c+=half3(.20,.29,.27)*ca*exp(-depth*.19)*saturate(geom.y)*sun.shadowAttenuation;}
     if(depth>0){c+=albedo*half3(.12,.17,.18)*exp(-depth*.10);c+=albedo*half3(.07,.10,.10)*(1-sun.shadowAttenuation)*exp(-depth*.09);}
     if(_WorldSpaceCameraPos.y<-.25){float f=1-exp(-distance(_WorldSpaceCameraPos,i.w)*.11);c=lerp(c,half3(.018,.10,.13),f);}
    }
    return half4(c,1);
   }
   ENDHLSL
  }
  Pass{Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Depth
   #pragma multi_compile_instancing
   half4 Depth(V i):SV_Target{UNITY_SETUP_INSTANCE_ID(i);return 0;}
   ENDHLSL
  }
  Pass{Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
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
   half4 Depth(V i):SV_Target{UNITY_SETUP_INSTANCE_ID(i);return 0;}
   ENDHLSL
  }
 }
}
