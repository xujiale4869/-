Shader "Deepsea/SeaSurfaceV24" {
 Properties {
  _Shallow("Shallow body",Color)=(.012,.40,.48,1)
  _Deep("Deep body",Color)=(.004,.14,.22,1)
  _WaveMap("Fine wave slopes",2D)="gray"{}
  _Refraction("Refraction",Float)=.0032
  _Visibility("Underwater visibility",Range(0,1))=.78
  _SurfaceWeight("Surface weight",Range(0,1))=.48
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Name "RollingSeaSurface" Tags {"LightMode"="UniversalForward"} Blend One Zero ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   #include "OceanV24Waves.hlsl"
   TEXTURE2D(_WaveMap);SAMPLER(sampler_WaveMap);
   CBUFFER_START(UnityPerMaterial)
   half4 _Shallow,_Deep;float _Refraction,_Visibility,_SurfaceWeight;
   CBUFFER_END
   float4 _CoastBoat,_CoastBoatHeading;
   float _OceanV24Layer;
   struct A{float4 p:POSITION;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 param:TEXCOORD2;float eye:TEXCOORD3;};
   V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.param=o.world.xz;float3 dx,dz;o.world+=OceanV24Sample(o.param,_OceanV24Time,dx,dz);o.normal=normalize(cross(dz,dx));o.p=TransformWorldToHClip(o.world);o.eye=-TransformWorldToView(o.world).z;return o;}
   float Depth(float2 uv){float d=SampleSceneDepth(uv);
   #if !UNITY_REVERSED_Z
   d=lerp(UNITY_NEAR_CLIP_VALUE,1,d);
   #endif
   return d;}
   half4 Frag(V i):SV_Target {
    float2 uv=GetNormalizedScreenSpaceUV(i.p);
    float3 bottom=ComputeWorldSpacePosition(uv,Depth(uv),UNITY_MATRIX_I_VP);
    float depth=max(0,i.world.y-bottom.y);clip(depth-.002);
    float t=_OceanV24Time;float2 p=i.world.xz;
    half2 fineA=SAMPLE_TEXTURE2D(_WaveMap,sampler_WaveMap,p*.11+float2(-t*.018,t*.007)).rg*2-1;
    half2 fineB=SAMPLE_TEXTURE2D(_WaveMap,sampler_WaveMap,p*.19+float2(-t*.011,-t*.009)).rg*2-1;
    float3 n=normalize(i.normal+float3((fineA.x*.065+fineB.x*.035),0,(fineA.y*.065+fineB.y*.035)));
    float3 v=GetWorldSpaceNormalizeViewDir(i.world);float facing=saturate(dot(n,v));
    float2 offset=n.xz*_Refraction*smoothstep(.08,.65,depth);
    float2 shifted=saturate(uv+offset);
    // One inexpensive eye-depth rejection avoids pulling dry shore pixels through the waterline.
    if(LinearEyeDepth(SampleSceneDepth(shifted),_ZBufferParams)<i.eye-.08)shifted=uv;
    half3 scene=SampleSceneColor(shifted);
    float deep=1-exp(-depth*.26);
    half3 body=lerp(_Shallow.rgb,_Deep.rgb,deep);
    half3 transmission=exp(-min(depth/max(.48,v.y),35)*half3(.88,.19,.10));
    half3 under=scene*transmission+body*(1-transmission);
    float3 reflected=reflect(-v,n);
    half3 sky=lerp(half3(.02,.18,.26),half3(.12,.38,.48),saturate(reflected.y*.8+.1));
    // Reflection follows wave orientation, not a painted additive bright/dark sine texture.
    float front=saturate(dot(n.xz,normalize(float2(.45,.89)))*2.4+.45);
    half3 surface=body*(.74+.16*front)+sky*(.11+.14*saturate(reflected.y));
    float window=smoothstep(.43,.88,facing);
    float visibility=_Visibility*exp(-depth*.19)*lerp(.42,.92,window);
    visibility*=1-_SurfaceWeight*.55;
    float phase=dot(i.param,normalize(float2(.45,.89)))*(6.2831853/7.6)-t*sqrt(9.8*(6.2831853/7.6))*.78;
    float crest=pow(saturate(.5+.5*sin(phase)),7);
    visibility*=1-crest*.34;
    half3 c=lerp(surface,under,saturate(visibility));
    c=lerp(c,surface*1.14,crest*.17);
    Light sun=GetMainLight();float highlight=pow(saturate(dot(n,normalize(sun.direction+v))),80);
    c+=lerp(sun.color,half3(.64,.83,.9),.65)*highlight*.19;
    // Depth-intersection foam remains at the shoreline; no random glitter cells.
    float edge=.5+.28*sin(p.x*3.6+sin(p.y*1.4)-t)+.22*sin(p.y*4.8-p.x*1.5-t*.7);
    float foam=(1-smoothstep(.03,.16,depth))*smoothstep(.55,.9,edge)*smoothstep(.004,.02,depth);
    float2 heading=normalize(_CoastBoatHeading.xy+float2(.00001,0)),d=p-_CoastBoat.xy;
    float along=dot(d,heading),side=dot(d,float2(heading.y,-heading.x));
    float bow=exp(-pow((along-(2.05-abs(side)*.4))/.14,2))*(1-smoothstep(.6,1.05,abs(side)))*saturate(_CoastBoat.z/3.5)*_CoastBoat.w;
    c=lerp(c,half3(.44,.68,.70),saturate(foam*.28+bow*.18));
    if(_OceanV24Layer>.5&&_OceanV24Layer<1.5)c=surface;
    if(_OceanV24Layer>1.5)c=half3(visibility,visibility,visibility);
    return half4(max(c,0),1);
   }
   ENDHLSL
  }
 }
}
