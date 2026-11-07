Shader "Deepsea/UnderwaterReefVibrantV27" {
 Properties {
  [MainColor] _BaseColor("Base Tint",Color)=(1,1,1,1)
  _PaletteMap("Four coral palettes",2D)="white"{}
  _PaletteBand("Cluster palette",Float)=0
  _SwayAmplitude("Current sway",Range(0,.45))=.14
  _SwayFrequency("Current frequency",Range(.2,4))=1.35
  _CoralVibrancy("Coral vibrancy",Range(0,1.5))=1.05
  _SubsurfaceGlow("Tip transmitted light",Range(0,1.5))=.55
  _CausticsStrength("Caustics",Range(0,1.5))=.30
  _IsKelp("Kelp",Float)=0
  _RimColor("Coral rim tint",Color)=(.55,.98,.92,1)
  _RimPower("Coral rim falloff",Range(1,6))=2.4
  _RimIntensity("Coral rim contrast",Range(0,3))=0
  _EdgeGlowBoost("Coral growth highlight",Range(0,2))=0
  [HideInInspector] _TableGrowthProfile("Highlight table cap sides",Float)=0
 }
 SubShader {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  CBUFFER_START(UnityPerMaterial)
  half4 _BaseColor;
  float _PaletteBand,_SwayAmplitude,_SwayFrequency,_CoralVibrancy,_SubsurfaceGlow,_CausticsStrength,_IsKelp;
  half4 _RimColor;
  float _RimPower, _RimIntensity, _EdgeGlowBoost, _TableGrowthProfile;
  CBUFFER_END
  TEXTURE2D(_PaletteMap);SAMPLER(sampler_PaletteMap);
  float4 _CoastBoat;float _HarborV27Time;
  struct Attributes {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct Varyings {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float height:TEXCOORD2;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
  Varyings Vert(Attributes a){
   Varyings o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);
   float3 w=TransformObjectToWorld(a.p.xyz);float h=saturate(_IsKelp>.5?a.color.a:a.uv.y);
   float weight=_IsKelp>.5?h*h:h*.28;float t=_HarborV27Time*_SwayFrequency;
   w.x+=sin(t+w.x*.45+w.z*.38)*_SwayAmplitude*weight;
   w.z+=cos(t*.78-w.z*.52+w.x*.29)*_SwayAmplitude*.65*weight;
   float2 away=w.xz-_CoastBoat.xy;float push=saturate(1-length(away)/3.8)*saturate(_CoastBoat.w);
   w.xz+=normalize(away+float2(.001,.001))*(push*.28*weight);
   o.world=w;o.p=TransformWorldToHClip(w);o.n=TransformObjectToWorldNormal(a.n);o.height=h;o.color=a.color;return o;
  }
  ENDHLSL
  Pass {
   Name "ReefVibrantForward" Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   half4 Frag(Varyings i,half face:VFACE):SV_Target {
    UNITY_SETUP_INSTANCE_ID(i);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    half3 n=normalize(i.n)*(face>=0?1:-1);half3 baseCol;
    if(_IsKelp>.5)baseCol=lerp(half3(.08,.24,.12),half3(.30,.62,.25),smoothstep(.08,.92,i.height));
    else {half3 palette=SAMPLE_TEXTURE2D(_PaletteMap,sampler_PaletteMap,float2((_PaletteBand+.5)*.25,saturate(i.height*.85+.08))).rgb;baseCol=lerp(i.color.rgb,palette,saturate(_CoralVibrancy));}
    baseCol*=_BaseColor.rgb;
    float detail=1-smoothstep(100,150,distance(_WorldSpaceCameraPos,i.world));
    float pores=.96+.04*sin(i.world.y*36+sin(i.world.x*22)*1.3)*cos(i.world.z*32)*detail;
    half ndotl=saturate(dot(n,sun.direction))*.58+.42;half3 ambient=max(SampleSH(n),half3(.12,.18,.20));
    half tip=pow(max(i.height,.0001),1.6)*_SubsurfaceGlow*saturate(sun.direction.y+.5);
    half3 sss=_IsKelp>.5?half3(.32,.58,.20):baseCol*1.12;
    float2 cuv=i.world.xz*.42+float2(_HarborV27Time*.08,-_HarborV27Time*.06);
    float cw=saturate(sin(cuv.x*6.283+cos(cuv.y*6.283))*cos(cuv.y*6.283-sin(cuv.x*6.283)));
    float caustics=pow(cw,3)*_CausticsStrength*saturate(1-abs(i.world.y)/6.5)*detail;
    half3 color=baseCol*pores*(sun.color*ndotl*lerp(.45,1,sun.shadowAttenuation)+ambient);
    color+=sss*tip*.30+half3(.25,.60,.55)*caustics;
    // Only materials that opt in receive silhouette lighting.
    if (_IsKelp < .5 && (_RimIntensity > 0 || _EdgeGlowBoost > 0))
    {
     float growth = smoothstep(.32, .94, i.height);
     float fresnel = pow(1.0 - saturate(dot(n, GetWorldSpaceNormalizeViewDir(i.world))), _RimPower);
     // Table corals emphasise each tier's side, while anemones use the tube-tip UV ramp.
     float edge = lerp(growth, growth * smoothstep(.28, .86, 1.0 - abs(n.y)), _TableGrowthProfile);
     half3 tint = lerp(_RimColor.rgb, baseCol, .24);
     half3 shadowFill = half3(.055, .060, .100) * (1.0 - ndotl);
     color += shadowFill + tint * (fresnel * _RimIntensity * .36 + edge * _EdgeGlowBoost * .24);
    }
    return half4(color,1);
   }
   ENDHLSL
  }
  Pass {Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ColorMask 0 ZWrite On
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment FragDepth
   #pragma multi_compile_instancing
   half4 FragDepth(Varyings i):SV_Target{return 0;}
   ENDHLSL
  }
  Pass {Name "DepthNormals" Tags {"LightMode"="DepthNormals"} ZWrite On
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment FragNormals
   #pragma multi_compile_instancing
   half4 FragNormals(Varyings i,half face:VFACE):SV_Target{return half4(normalize(i.n)*(face>=0?1:-1),0);}
   ENDHLSL
  }
 }
}
