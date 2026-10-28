Shader "Deepsea/CoastPaletteArchitecture" {
 Properties {
  _PaletteRole("Land palette role",Float)=0
  _StoneMap("Exposed limestone",2D)="white"{} _WeatherRole("Plaster Stone Metal",Float)=0
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
 TEXTURE2D(_StoneMap);SAMPLER(sampler_StoneMap);float _ShowcaseClay;
 float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float Noise(float2 p){float2 k=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(k),Hash(k+float2(1,0)),f.x),lerp(Hash(k+float2(0,1)),Hash(k+1),f.x),f.y);}
 float Grain(float3 p){return Noise(p.xz+p.y*.37)*.64+Noise(p.xy*2.1+p.z*.4)*.24+Noise(p.zy*5.1)*.12;}
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_EmissionColor;float _WeatherRole,_TextureWeight,_UseMask,_NormalStrength,_Smoothness,_Metallic,_UseVertexColor;
 float _PaletteRole;
 CBUFFER_END
 #include "CoastPalette.hlsl"
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;half4 colour:COLOR;};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;half4 colour:COLOR;float3 local:TEXCOORD5;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=a.uv;o.colour=a.colour;o.local=a.p.xyz;return o;}
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
  float3 p=i.local;float weather=Grain(p*2.5);
  if(_WeatherRole<.5){
   float lower=1-smoothstep(.65,1.9,p.y);float chip=smoothstep(.57,.76,weather)*(.10+.90*lower);
   half3 stone=SAMPLE_TEXTURE2D(_StoneMap,sampler_StoneMap,i.uv*1.7).rgb;stone=lerp(dot(stone,half3(.299,.587,.114)).xxx,stone,.2)*half3(.64,.66,.61);
   surface.albedo=lerp(surface.albedo,stone,chip*.88);
   float dripShape=(.45+.55*Noise(float2(p.x*95,p.y*.33)))*(1-smoothstep(.12,.35,abs(p.x+.03*sin(p.y*2))));
   float dripY=max((1-smoothstep(1.83,1.98,p.y))*smoothstep(.72,1.85,p.y),(1-smoothstep(3.98,4.13,p.y))*smoothstep(2.9,4.0,p.y));
   float drip=dripShape*dripY*smoothstep(.9,1.2,p.z);surface.albedo*=1-drip*.28;
   float damp=(1-smoothstep(.45,1.1,p.y))*(.48+.52*weather);surface.albedo*=lerp(half3(1,1,1),half3(.69,.73,.66),damp*.50);surface.smoothness=lerp(.11,.24,damp);surface.occlusion*=1-chip*.07;
  }else if(_WeatherRole<1.5){surface.albedo*=.94+.06*weather;surface.smoothness=.14;}
  else{float oxidation=smoothstep(.48,.78,Grain(p*13));surface.albedo=lerp(surface.albedo,half3(.17,.12,.065),oxidation*.10);surface.smoothness=lerp(surface.smoothness,.21,oxidation);}
  if(_ShowcaseClay<.5){surface.albedo=CoastPalette(surface.albedo,_PaletteRole,i.world,geom,0);n=normalize(lerp(n,geom,CoastLandMask(i.world,_PaletteRole)*.38));}
  if(_ShowcaseClay>.5){surface.albedo=half3(.52,.52,.52);n=geom;surface.smoothness=.05;surface.metallic=0;}
  InputData input=(InputData)0;input.positionWS=i.world;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
  input.shadowCoord=TransformWorldToShadowCoord(i.world);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);
  half4 colour=UniversalFragmentPBR(input,surface);Light light=GetMainLight(input.shadowCoord);if(_ShowcaseClay<.5)colour.rgb=CoastPaletteLight(colour.rgb,_PaletteRole,i.world,n,light.direction,light.shadowAttenuation);return half4(colour.rgb,_BaseColor.a);
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
