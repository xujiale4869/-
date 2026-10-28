Shader "Deepsea/CoastFoundation"
{
 Properties {
  _BaseMap("Rock or wood",2D)="white"{} _BumpMap("Rock normal",2D)="bump"{} _SandMap("Sand",2D)="white"{}
  _CausticMap("Underwater caustics",2D)="black"{} _MaskMap("AO Roughness Metal",2D)="white"{} _UseMask("Use surface mask",Float)=0
  _BaseColor("Tint",Color)=(1,1,1,1) _SandColor("Sand tint",Color)=(1,.94,.82,1)
  _Scale("World texture scale",Float)=.36 _Kind("0 rock 1 ground 2 wood 3 solid",Float)=0
  _BumpStrength("Normal",Range(0,1))=.28 _Moss("Lichen",Range(0,1))=.15
  _TextureContrast("Texture contrast",Range(0,1))=.7 _ObjectMapping("Object mapping",Float)=0
 }
 SubShader {
 Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Back
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);TEXTURE2D(_SandMap);SAMPLER(sampler_SandMap);
 TEXTURE2D(_CausticMap);SAMPLER(sampler_CausticMap);
 TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);
 float _CoastWaterDetails;
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_SandColor;float _Scale,_Kind,_BumpStrength,_Moss,_TextureContrast,_ObjectMapping,_UseMask;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float3 mapping:TEXCOORD2;float fog:TEXCOORD3;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);
 float3 scale=float3(length(UNITY_MATRIX_M._m00_m10_m20),length(UNITY_MATRIX_M._m01_m11_m21),length(UNITY_MATRIX_M._m02_m12_m22));
 o.mapping=lerp(o.world,a.p.xyz*scale,_ObjectMapping);o.fog=ComputeFogFactor(o.p.z);return o;}
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
 half4 Frag(V i):SV_Target{
 float3 n=normalize(i.n),geom=n,w=pow(abs(n),4);w/=max(.001,w.x+w.y+w.z);
 half3 albedo=_BaseColor.rgb;float sand=0,ao=1,roughness=.85;
 if(_UseMask>.5){
 float2 maskUV=w.x>w.y&&w.x>w.z?i.mapping.zy:w.y>w.z?i.mapping.xz:i.mapping.xy;
 half3 mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,maskUV*_Scale).rgb;
 ao=lerp(.55,1,mask.r);roughness=mask.g;
 }
 if(_Kind<1.5){
 half3 tex=SampleRock(i.mapping,w);half lum=dot(tex,half3(.299,.587,.114));tex=lerp(lum.xxx,tex,.62);
 albedo*=lerp(half3(.5,.5,.5),tex,_TextureContrast);
 float3 bump=SampleBump(i.mapping,n,w);
 if(_Kind>.5){sand=smoothstep(.55,.9,geom.y)*(1-smoothstep(.15,1.45,i.world.y))*smoothstep(-18,-5,i.world.y);
 half3 grit=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.world.xz*.2).rgb*_SandColor.rgb;albedo=lerp(albedo,grit,sand);}
 n=normalize(lerp(n,bump,_BumpStrength*(1-sand*.85)));
 // Broad, irregular growth reads as terrain cover, with texture only at the edge.
 float patch=.5+.24*sin(i.world.x*.37+sin(i.world.z*.29)*1.8)+.18*sin(i.world.z*.61-i.world.x*.19)+.08*(lum-.5);
 float lichen=_Moss*smoothstep(.36,.83,geom.y)*smoothstep(.4,1.8,i.world.y)*smoothstep(.27,.66,patch);
 half3 moss=half3(.115,.19,.092)*(.82+lum*.44);
 albedo=lerp(albedo,moss,lichen);
 }else if(_Kind<2.5){
 float2 woodUV=abs(geom.y)>.5?i.mapping.xz:abs(geom.x)>.5?i.mapping.zy:i.mapping.xy;
 half3 wood=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,woodUV*float2(_Scale*.38,_Scale*1.45)).rgb;
 half luma=dot(wood,half3(.299,.587,.114));wood=lerp(luma.xxx,wood,.35);
 float grain=.93+.07*sin(woodUV.x*83+sin(woodUV.y*2.8)*1.7);
 float board=.92+.08*sin(UNITY_MATRIX_M._m03*7.13+UNITY_MATRIX_M._m23*2.3);
 albedo*=lerp(half3(.38,.36,.30),wood,_TextureContrast)*grain*board;
 }else if(_Kind>3.5){
 // Limewash mottling and rain stains; no high-frequency stone normal on the wall.
 half3 tex=SampleRock(i.mapping,w);half lum=dot(tex,half3(.299,.587,.114));
 n=normalize(lerp(n,SampleBump(i.mapping,n,w),_BumpStrength));
 float streak=pow(saturate(.5+.5*sin(i.world.x*12.8+i.world.z*8.3+sin(i.world.x*27))),5);
 float age=.18*streak+.32*(1-smoothstep(4.7,7.4,i.world.y));
 float weather=.5+.23*sin(i.world.x*3.1+sin(i.world.y*1.4)*1.2)+.21*sin(i.world.z*4.8+i.world.y*.7);
 float chipped=smoothstep(.62,.83,weather)*(1-smoothstep(5.4,8.1,i.world.y));
 albedo*=lerp(half3(.67,.65,.56),lerp(lum.xxx,tex,.45),.58)*(1-age*.45);
 albedo=lerp(albedo,half3(.21,.23,.18)*(1+.25*lum),chipped*.65);
 }
 float wet=(1-smoothstep(-.35,.65,i.world.y))*step(_Kind,2.5);
 albedo*=lerp(half3(1,1,1),half3(.62,.73,.70),wet);
 Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));float diffuse=saturate(dot(n,sun.direction));
 half3 ambient=max(half3(.09,.12,.13),SampleSH(n));
 half3 c=albedo*(ambient*ao+sun.color*(.1+.9*diffuse)*sun.shadowAttenuation);
 float3 halfDir=normalize(sun.direction+GetWorldSpaceNormalizeViewDir(i.world));
 c+=sun.color*pow(saturate(dot(n,halfDir)),lerp(lerp(80,12,roughness),90,wet))*(.014+(1-roughness)*.035+wet*.13)*sun.shadowAttenuation;
 float deep=saturate((-i.world.y-.7)/16.3);c=lerp(c,c*half3(.35,.68,.71),deep*.65);
 // Project two drifting light patterns onto submerged, upward-facing geometry.
 float waterDepth=-.22-i.world.y;
 if(waterDepth>.10&&waterDepth<14&&geom.y>.15&&_Kind<1.5){
 float2 uv=i.world.xz*.065;
 uv+=.030*float2(sin(i.world.z*.72+sin(i.world.x*.33)+_Time.y*.29),cos(i.world.x*.67+sin(i.world.z*.41)-_Time.y*.23));
 float a=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,uv+float2(_Time.y*.003,_Time.y*.0015)).r;
 float b=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,uv*1.17-float2(_Time.y*.002,_Time.y*.0025)).r;
 float fade=smoothstep(.10,.65,waterDepth)*exp(-waterDepth*.16)*(1-smoothstep(9,14,waterDepth))*saturate(geom.y);
 float patches=.45+.40*sin(i.world.x*.44+i.world.z*.38+sin(i.world.z*.27)-_Time.y*.31);
 c+=half3(.085,.125,.105)*(.65*a+.35*b)*patches*fade*sun.shadowAttenuation*_CoastWaterDetails;
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
