Shader "Deepsea/CoastalGround" {
 Properties {_SurfaceKind("Land / seabed / stone",Float)=1 _BaseColor("Stone",Color)=(.42,.48,.39,1)}
 SubShader {
  Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
  Pass {
   Name "Forward"
   Tags{"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "../ThirdParty/BoatAttack/BayCaustics.hlsl"
   #include "Includes/SurfWash.hlsl"
   #include "Includes/CoastDetail.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;float _SurfaceKind;
   CBUFFER_END
   struct A{float4 p:POSITION;float3 n:NORMAL;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;};
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){
    float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
   }
   V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.p=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.p.z);return o;}
   float _BayNaturalCoast; half4 frag(V i):SV_Target{
    float macro=Noise(i.world.xz*.085);
    float3 sand=lerp(float3(.61,.61,.44),float3(.77,.73,.55),macro);
    float3 c=sand;
    float footprint=max(length(ddx(i.world.xz)),length(ddy(i.world.xz)));
    float grain=(1-smoothstep(.05,.20,footprint))*sin(i.world.x*.9+i.world.z*1.8+sin(i.world.z*.23)*1.1)*.012;
    // Identical submerged colour on the finite island and the continuous seabed.
    float3 bedSand=sand+grain;
    bedSand+=CoastSandRidge(i.world.xz,footprint)*float3(.032,.029,.019)*CoastDetailPocket(i.world.xz);
    bedSand=lerp(bedSand,float3(.32,.39,.32)*(1+macro*.12),CoastStoneFoot(i.world.xz)*.66);
    if(_SurfaceKind<.5){
     float3 grass=lerp(float3(.23,.34,.24),float3(.31,.42,.27),macro); grass=lerp(grass,lerp(float3(.09,.21,.16),float3(.34,.43,.22),Noise(i.world.xz*.19)),_BayNaturalCoast);
     float coastDetail=CoastDetailPocket(i.world.xz);
     float patch=Noise(i.world.xz*.26);
     float jitter=((patch-.5)*.34+sin(i.world.z*.42)*.11)*coastDetail;
     float beach=1-smoothstep(lerp(.24,.12,coastDetail),lerp(.78,.52,coastDetail),i.world.y+(macro-.5)*.12+jitter);
     float3 drySand=lerp(sand,lerp(float3(.47,.46,.31),float3(.65,.60,.43),patch),coastDetail);
     c=lerp(grass,drySand,beach); float slope=1-saturate(normalize(i.n).y);float exposed=smoothstep(.10,.42,slope)*smoothstep(.8,2.8,i.world.y); float3 rock=lerp(float3(.16,.23,.23),float3(.40,.43,.32),Noise(i.world.xz*.55)); c=lerp(c,rock,exposed*_BayNaturalCoast);
     float wash=.28+sin(_Time.y*.85+i.world.z*.13)*.055;
     float wet=1-smoothstep(-.19,wash,i.world.y);
     float localWet=max(1-smoothstep(-.19,-.08,i.world.y),SurfWetness(i.world.xz)*(1-smoothstep(.24,.45,i.world.y)));
     wet=lerp(wet,localWet,SurfPocket(i.world.xz)*_BaySurfEnabled);
     c=lerp(c,c*float3(.66,.75,.74),wet*.65);
     float tideline=(1-smoothstep(.03,.42,i.world.y+jitter))*smoothstep(-.32,-.12,i.world.y);
     c=lerp(c,c*float3(.77,.83,.78),tideline*coastDetail*.48);
     // The submerged part of the finite island mesh must match the endless sand bed.
     c=lerp(c,bedSand,1-smoothstep(-.24,-.12,i.world.y));
    }else if(_SurfaceKind>1.5){
     float stone=SurfNoise(i.world.xz*1.7+i.world.y*.3);
     float strata=sin(i.world.y*13+SurfNoise(i.world.xz*.65)*2.5)*.025;
     float broad=SurfNoise(i.world.xz*.48+i.world.y*.24); c=lerp(_BaseColor.rgb*float3(.69,.74,.78),_BaseColor.rgb*float3(1.22,1.12,.94),broad); c*=.84+stone*.22+strata;
     float wetStone=(1-smoothstep(-.12,.30,i.world.y))*smoothstep(-.40,-.16,i.world.y);
     c=lerp(c,c*float3(.66,.76,.76),wetStone*_BaySurfEnabled);
     float tide=.06+sin(_Time.y*.9+i.world.x*.4)*.04;
     float damp=(1-smoothstep(tide-.10,tide+.25,i.world.y))*_BayCoastDetail;
     c*=1-damp*.15;
     float algae=(1-smoothstep(-.03,.15,i.world.y))*smoothstep(-.35,-.16,i.world.y);
     c=lerp(c,c*float3(.78,.91,.72),algae*_BayCoastDetail*.45);
    }
    else c=bedSand;
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    float baseAmbient=_SurfaceKind>1.5?.38:lerp(.64,.46,_BayNaturalCoast); float lighting=baseAmbient+(.99-baseAmbient)*saturate(dot(normalize(i.n),sun.direction))*sun.shadowAttenuation;
    c*=lighting*lerp(float3(.94,1,1.02),sun.color,.25);
    c+=BayCaustics(i.world,sun.direction)*sun.shadowAttenuation;
    if(_SurfaceKind>1.5){
     float wet= (1-smoothstep(-.10,.32,i.world.y))*smoothstep(-.35,-.16,i.world.y)*_BaySurfEnabled;
     float3 view=GetWorldSpaceNormalizeViewDir(i.world);
     c+=pow(saturate(dot(normalize(i.n),normalize(view+sun.direction))),80)*wet*.14*sun.color;
    }
    return half4(MixFog(c,i.fog),1);
   }
   ENDHLSL
  }
  Pass {
   Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex DepthVert
   #pragma fragment DepthFrag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float4 DepthVert(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}
   half4 DepthFrag():SV_Target{return 0;}
   ENDHLSL
  }
  Pass {
   Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex ShadowVert
   #pragma fragment ShadowFrag
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   float3 _LightDirection;float3 _LightPosition;
   struct ShadowInput{float4 p:POSITION;float3 n:NORMAL;};
   float4 ShadowVert(ShadowInput a):SV_POSITION{
    float3 p=TransformObjectToWorld(a.p.xyz),n=TransformObjectToWorldNormal(a.n);
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 light=normalize(_LightPosition-p);
    #else
    float3 light=_LightDirection;
    #endif
    float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,light));
    #if UNITY_REVERSED_Z
    clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE);
    #else
    clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE);
    #endif
    return clip;
   }
   half4 ShadowFrag():SV_Target{return 0;}
   ENDHLSL
  }
 }
}

