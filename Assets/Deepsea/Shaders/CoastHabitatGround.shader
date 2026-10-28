Shader "Deepsea/CoastHabitatGround"
{
 Properties {
  _BaseMap("Rock or wood",2D)="white"{} _BumpMap("Rock normal",2D)="bump"{} _SandMap("Sand",2D)="white"{}
  _CausticMap("Underwater caustics",2D)="black"{} _MaskMap("AO Roughness Metal",2D)="white"{} _UseMask("Use surface mask",Float)=0
  _HabitatControl("Habitat paint",2D)="black"{} _CoverMap("Ground cover",2D)="white"{} _CoverBump("Ground normal",2D)="bump"{} _CoverMask("Ground ARM",2D)="white"{} _SoilMap("Forest soil",2D)="white"{}
  _BaseColor("Tint",Color)=(1,1,1,1) _SandColor("Sand tint",Color)=(1,.94,.82,1)
  _Scale("World texture scale",Float)=.36 _Kind("0 rock 1 ground 2 wood 3 solid",Float)=0
  _BumpStrength("Normal",Range(0,1))=.28 _Moss("Lichen",Range(0,1))=.15
  _TextureContrast("Texture contrast",Range(0,1))=.7 _ObjectMapping("Object mapping",Float)=0
  _ReplacementShelf("Replacement shelf surface",Float)=0
 }
 SubShader {
 Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Back
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);TEXTURE2D(_SandMap);SAMPLER(sampler_SandMap);
 TEXTURE2D(_CausticMap);SAMPLER(sampler_CausticMap);
 TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);
 TEXTURE2D(_HabitatControl);SAMPLER(sampler_HabitatControl);TEXTURE2D(_CoverMap);SAMPLER(sampler_CoverMap);TEXTURE2D(_CoverBump);SAMPLER(sampler_CoverBump);TEXTURE2D(_CoverMask);SAMPLER(sampler_CoverMask);TEXTURE2D(_SoilMap);SAMPLER(sampler_SoilMap);
 float _CoastWaterDetails,_CoastHabitatDetails;
 TEXTURE2D(_CoastShelfMask);SAMPLER(sampler_CoastShelfMask);float _CoastShelfEnabled;
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_SandColor;float _Scale,_Kind,_BumpStrength,_Moss,_TextureContrast,_ObjectMapping,_UseMask,_ReplacementShelf;
 CBUFFER_END
 void OriginalShelfVisibility(float3 p){
  float2 uv=(p.xz-float2(38,50))/float2(68,92);
  if(_ReplacementShelf<.5&&_CoastShelfEnabled>.5&&all(uv>=0)&&all(uv<=1)){float cover=SAMPLE_TEXTURE2D_LOD(_CoastShelfMask,sampler_CoastShelfMask,uv,0).r;clip(.5-cover);}
 }
 struct A{float4 p:POSITION;float3 n:NORMAL;half4 colour:COLOR;};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float3 mapping:TEXCOORD2;float fog:TEXCOORD3;half4 colour:COLOR;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);
 float3 scale=float3(length(UNITY_MATRIX_M._m00_m10_m20),length(UNITY_MATRIX_M._m01_m11_m21),length(UNITY_MATRIX_M._m02_m12_m22));
 o.mapping=lerp(o.world,a.p.xyz*scale,_ObjectMapping);o.fog=ComputeFogFactor(o.p.z);o.colour=a.colour;return o;}
 half3 SampleRock(float3 p,float3 w){return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy*_Scale).rgb*w.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz*_Scale).rgb*w.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy*_Scale).rgb*w.z;}
 float3 SampleBump(float3 p,float3 n,float3 w){
 float3 nx=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.zy*_Scale));
 float3 ny=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xz*_Scale));
 float3 nz=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xy*_Scale));
 return normalize(float3(sign(n.x)*nx.z,nx.y,nx.x)*w.x+float3(ny.x,sign(n.y)*ny.z,ny.y)*w.y+float3(nz.x,nz.y,sign(n.z)*nz.z)*w.z);
 }
 ENDHLSL
 Pass{
 Name "FoundationLit" Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 half4 Frag(V i):SV_Target{OriginalShelfVisibility(i.world);
 float3 n=normalize(i.n),geom=n,w=pow(abs(n),4);w/=max(.001,w.x+w.y+w.z);
 float2 rockUV=(w.x>w.y&&w.x>w.z?i.mapping.zy:w.y>w.z?i.mapping.xz:i.mapping.xy)*_Scale;
 half3 albedo=_BaseColor.rgb;float sand=0,ao=1,roughness=.85;
 if(i.world.y>.12&&geom.y>.36&&_CoastHabitatDetails>.5){
 float2 controlUV=(i.world.xz-float2(0,42))/float2(142,112);
 half4 control=SAMPLE_TEXTURE2D(_HabitatControl,sampler_HabitatControl,controlUV);
 float2 uv=i.world.xz*.34+float2(sin(i.world.z*.15),cos(i.world.x*.13))*.045;
 half3 cover=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,uv).rgb;
 half3 soil=SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,i.world.xz*.26).rgb;
 half3 coverARM=SAMPLE_TEXTURE2D(_CoverMask,sampler_CoverMask,uv).rgb;
 float growth=saturate(control.r*1.28)*smoothstep(.38,.80,geom.y);float variation=.88+control.b*.22;
 cover=dot(cover,half3(.299,.587,.114))*half3(.29,.365,.17);
 soil=lerp(dot(soil,half3(.299,.587,.114)).xxx,soil,.65)*half3(.58,.56,.43);
 albedo=lerp(soil,cover,growth)*variation;
 float3 detail=UnpackNormal(SAMPLE_TEXTURE2D(_CoverBump,sampler_CoverBump,uv));
 n=normalize(lerp(geom,normalize(float3(detail.x,detail.z,detail.y)),.32*growth));
 ao=lerp(1,lerp(.65,1,coverARM.r),growth);roughness=lerp(.95,coverARM.g,growth);
 }else{
 half3 tex=lerp(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,rockUV).rgb,SampleRock(i.mapping,w),_ReplacementShelf);half lum=dot(tex,half3(.299,.587,.114));tex=lerp(lum.xxx,tex,.62);
 albedo*=lerp(half3(.5,.5,.5),tex,_TextureContrast);
 float3 raw=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,rockUV));
 float3 bump=normalize(w.x>w.y&&w.x>w.z?float3(sign(n.x)*raw.z,raw.y,raw.x):w.y>w.z?float3(raw.x,sign(n.y)*raw.z,raw.y):float3(raw.x,raw.y,sign(n.z)*raw.z));
 if(_UseMask>.5){half3 mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,rockUV).rgb;ao=lerp(.55,1,mask.r);roughness=mask.g;}
 {sand=smoothstep(.55,.9,geom.y)*(1-smoothstep(.15,1.45,i.world.y))*smoothstep(-18,-5,i.world.y);sand*=lerp(1,1-saturate(i.colour.r),_ReplacementShelf);
 half3 grit=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.world.xz*.16).rgb;grit=lerp(half3(.53,.56,.52),grit,.55)*_SandColor.rgb;albedo=lerp(albedo,grit,sand);}
 bump=lerp(bump,SampleBump(i.mapping,n,w),_ReplacementShelf);
 n=normalize(lerp(n,bump,_BumpStrength*(1-sand*.85)));
 // Broad, irregular growth reads as terrain cover, with texture only at the edge.
 float patch=.5+.24*sin(i.world.x*.37+sin(i.world.z*.29)*1.8)+.18*sin(i.world.z*.61-i.world.x*.19)+.08*(lum-.5);
 float lichen=_Moss*smoothstep(.36,.83,geom.y)*smoothstep(.4,1.8,i.world.y)*smoothstep(.27,.66,patch);
 half3 moss=half3(.115,.19,.092)*(.82+lum*.44);
 albedo=lerp(albedo,moss,lichen);
 }
 float wet=(1-smoothstep(-.35,.65,i.world.y));
 albedo*=lerp(half3(1,1,1),half3(.78,.90,.84),wet);
 Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));float diffuse=saturate(dot(n,sun.direction));
 // Water scatters light into a submerged bank's shadow; retain the shadow's shape without
 // the opaque black stripe produced by a tall dry-rock shadow over the bright shallow bed.
 sun.shadowAttenuation=lerp(sun.shadowAttenuation,1,(1-smoothstep(-1.2,-.18,i.world.y))*.28);
 half3 ambient=max(half3(.09,.12,.13),SampleSH(n));
 ao*=lerp(1,i.colour.g,_ReplacementShelf);
 half3 c=albedo*(ambient*ao+sun.color*(.1+.9*diffuse)*sun.shadowAttenuation);
 float3 halfDir=normalize(sun.direction+GetWorldSpaceNormalizeViewDir(i.world));
 c+=sun.color*pow(saturate(dot(n,halfDir)),lerp(lerp(80,12,roughness),90,wet))*(.014+(1-roughness)*.035+wet*.13)*sun.shadowAttenuation;
 float deep=saturate((-i.world.y-.7)/16.3);c=lerp(c,c*half3(.35,.68,.71),deep*.65);
 // Project two drifting light patterns onto submerged, upward-facing geometry.
 float waterDepth=-.22-i.world.y;
 if(waterDepth>.10&&waterDepth<14&&geom.y>.15){
 float2 uv=i.world.xz*.11;
 uv+=.055*float2(sin(i.world.z*.72+sin(i.world.x*.33)+_Time.y*.29),cos(i.world.x*.67+sin(i.world.z*.41)-_Time.y*.23));
 float a=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,uv+float2(_Time.y*.003,_Time.y*.0015)).r;
 float b=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,uv*1.17-float2(_Time.y*.002,_Time.y*.0025)).r;
 float fade=smoothstep(.10,.65,waterDepth)*exp(-waterDepth*.16)*(1-smoothstep(9,14,waterDepth))*saturate(geom.y);
 float patches=smoothstep(.24,.72,.5+.28*sin(i.world.x*.44+i.world.z*.38+sin(i.world.z*.27)-_Time.y*.31));
 c+=half3(.14,.23,.24)*max(a,b*.85)*patches*fade*sun.shadowAttenuation*_CoastWaterDetails;
 }
 float underCamera=saturate((-.22-_WorldSpaceCameraPos.y)*.4);float waterFog=(1-exp(-distance(_WorldSpaceCameraPos,i.world)*.038))*underCamera;
 c=lerp(c,half3(.018,.10,.13),waterFog);
 return half4(MixFog(c,i.fog),1);
 }
 ENDHLSL
 }
 Pass{Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Depth
 half4 Depth(V i):SV_Target{OriginalShelfVisibility(i.world);return 0;}
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
 half4 Depth(V i):SV_Target{OriginalShelfVisibility(i.world);return 0;}
 ENDHLSL
 }
 }
}
