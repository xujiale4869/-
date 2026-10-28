Shader "Deepsea/Ocean"
{
 Properties {
  _DeepColor("Bay blue",Color)=(.025,.27,.38,1)
  _ShallowColor("Sunlit turquoise",Color)=(.12,.66,.53,1)
  _WaveHeight("Swell",Float)=.15
  _NormalMap("Boat Attack packed normals",2D)="gray"{}
  _FoamMap("Boat Attack foam",2D)="white"{}
  _Bathymetry("Continuous seabed elevation",2D)="black"{}
 }
 SubShader{
 Tags{"RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20"}
 Pass{
 Tags{"LightMode"="UniversalForward"}
 ZWrite On Cull Back
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
 #include "../ThirdParty/BoatAttack/GerstnerWaves.hlsl"
 #include "Includes/SurfWash.hlsl"
 TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
 TEXTURE2D(_FoamMap);SAMPLER(sampler_FoamMap);
 TEXTURE2D(_Bathymetry);SAMPLER(sampler_Bathymetry);
 TEXTURE2D(_BayReflection);SAMPLER(sampler_BayReflection);
 float _BathymetryReady,_BayReflectionReady;
 float4 _BayBoat,_BayBoatDirection;
 float4 _BayRipples[12];int _BayRippleCount;
 CBUFFER_START(UnityPerMaterial)
 float4 _DeepColor,_ShallowColor;float _WaveHeight;
 CBUFFER_END
 struct A{float4 positionOS:POSITION;};
 struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 screen:TEXCOORD2;float fog:TEXCOORD3;float wave:TEXCOORD4;};
 V vert(A v){
  V o;float3 p=TransformObjectToWorld(v.positionOS.xyz);WaveStruct w;SampleWaves(p,1,w);
  p+=w.position*float3(.14,1,.14);o.world=p;o.normal=normalize(w.normal);o.wave=w.position.y;
  o.positionCS=TransformWorldToHClip(p);o.screen=ComputeScreenPos(o.positionCS);
  o.fog=ComputeFogFactor(o.positionCS.z);return o;
 }
 float ClearDepth(float raw){
  #if UNITY_REVERSED_Z
  return raw<.00001?1:0;
  #else
  return raw>.99999?1:0;
  #endif
 }
 float3 SceneWorld(float2 uv,float raw){
  #if !UNITY_REVERSED_Z
  raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
  #endif
  return ComputeWorldSpacePosition(uv,raw,UNITY_MATRIX_I_VP);
 }
 half4 frag(V i):SV_Target{
  float2 p=i.world.xz;float t=_Time.y;
  float2 uv=i.screen.xy/i.screen.w;
  float footprint=max(length(ddx(p)),length(ddy(p)));
  float detail=1-smoothstep(.16,.7,footprint);
  // Meter-scale flow is visible at the normal gameplay zoom; fine ripples are secondary.
  float2 drift=float2(.62,.24)*t;
  float2 a=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,(p-drift)*.055).xy*2-1;
  float2 b=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,(p-float2(-.12,.28)*t)*.16).zw*2-1;
  float2 slope=a*.20+b*.095*detail;
  // Evaluate normals per pixel so coarse sea-grid triangles never become colour facets.
  WaveStruct fineWave;SampleWaves(float3(p.x,0,p.y),1,fineWave);
  float3 n=normalize(float3(fineWave.normal.x*.8+slope.x,fineWave.normal.y,fineWave.normal.z*.8+slope.y));
  // Local impacts travel outward, perturbing highlights and leaving broken rings.
  float rippleFoam=0;float2 rippleSlope=0;
  [loop]for(int ri=0;ri<_BayRippleCount;ri++){
   float4 eventData=_BayRipples[ri];float age=t-eventData.z;
   if(eventData.w>0&&age>=0&&age<3.4){
    float2 delta=p-eventData.xy;float d=length(delta);
    float front=d-(.12+age*1.7);
    float ring=exp(-front*front*32);
    float inner=exp(-pow((d-(.1+age*1.35))*.95,2)*22);
    float fade=pow(saturate(1-age/3.4),1.7)*eventData.w;
    rippleSlope+=delta/max(d,.01)*front*ring*fade*1.5;
    float breakup=saturate(.38+a.x*.9+b.y*.7+sin(dot(p,float2(4.3,2.8))+ri*1.9)*.28);
    rippleFoam+=(ring*.12+inner*.025)*fade*breakup;
   }
  }
  n=normalize(n+float3(rippleSlope.x,0,rippleSlope.y));
  float3 view=GetWorldSpaceNormalizeViewDir(i.world);
  float raw=SampleSceneDepth(uv);
  float3 bottom=SceneWorld(uv,raw);
  float thickness=ClearDepth(raw)>.5?40:max(.001,i.world.y-bottom.y);
  // Regional water colour is tied to the continuous seabed, never to a passing hull.
  float2 seabedUV=(p/700+.5)*(512.0/513.0)+.5/513.0;
  float floorHeight=SAMPLE_TEXTURE2D(_Bathymetry,sampler_Bathymetry,seabedUV).r;
  float depth=lerp(10,max(.1,i.world.y-floorHeight),_BathymetryReady);
  float shallow=exp(-depth*.16);
  float3 c=lerp(_DeepColor.rgb,_ShallowColor.rgb,shallow);
  float2 refrUV=saturate(uv+slope*.0012*saturate(thickness));
  float displacedRaw=SampleSceneDepth(refrUV);
  float3 displacedWorld=SceneWorld(refrUV,displacedRaw);
  float safe=step(displacedWorld.y,i.world.y-.025)*(1-ClearDepth(displacedRaw));
  refrUV=lerp(uv,refrUV,safe);
  float3 trans=SampleSceneColor(refrUV);
  // Absorption reveals sand and vegetation; passing hulls never drive regional colour.
  float terrainContact=1-smoothstep(.35,2.5,max(0,bottom.y-floorHeight));
  float below=step(bottom.y,i.world.y-.001)*(1-ClearDepth(raw));
  float transWeight=lerp(.16,.80,terrainContact)*exp(-thickness*.30);
  transWeight=lerp(transWeight,.96,1-smoothstep(.005,.09,thickness))*below;
  float3 attenuation=exp(-thickness*float3(.28,.09,.07));
  float3 transmitted=trans*attenuation+_ShallowColor.rgb*(1-attenuation)*.30;
  c=lerp(c,transmitted,transWeight);
  float3 r=reflect(-view,n);
  float3 sky=lerp(float3(.14,.47,.61),float3(.56,.77,.84),smoothstep(-.1,1,r.y));
  float3 planar=SAMPLE_TEXTURE2D_LOD(_BayReflection,sampler_BayReflection,saturate(uv+slope*.008),2.0).rgb;
  float fresnel=.035+.22*pow(1-saturate(dot(n,view)),4);
  c=lerp(c,lerp(sky,planar,_BayReflectionReady*.23),.045+fresnel*.70);
  Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
  float halfAngle=saturate(dot(n,normalize(view+sun.direction)));
  float broad=pow(halfAngle,64);
  float glint=pow(halfAngle,190)*detail;
  c+=sun.color*(broad*.035+glint*.08)*sun.shadowAttenuation;
  // Slow swell shading and broken highlights remain visible when the boat is still.
  c+=float3(.005,.012,.012)*(a.x*.7+a.y*.3);
  float crest=smoothstep(.015,.13,fineWave.position.y)*(.45+.55*smoothstep(-.2,.65,a.y));
  c+=float3(.015,.028,.027)*crest;
  // Refracted light patterns fade with depth and merge into the same turquoise palette.
  float2 ca=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,p*.095-float2(.024,.011)*t).xy*2-1;
  float2 cb=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,p*.12+float2(.015,-.019)*t).zw*2-1;
  float caustic=pow(saturate(1-abs(ca.x+cb.y)*3.5),7);
  c+=float3(.008,.016,.009)*caustic*exp(-depth*.45)*(1-exp(-depth*2));
  c*=.94+.06*sun.shadowAttenuation;
  float2 foamUV=p*.20-float2(.09,.035)*t;
  float foamNoise=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,foamUV).r;
  foamNoise*=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,float2(foamUV.y,-foamUV.x)*1.37+.31).r*1.7;
  float shore=(1-smoothstep(.08,.55,thickness))*(1-ClearDepth(raw));
  float surge=.55+.45*sin(t*1.6-depth*3.6+a.x);
  float foam=shore*smoothstep(.22,.65,foamNoise)*surge*.48;
  // Sparse shoreward fronts over the gently sloping east-shore sand pocket.
  float coastRadius=length((p-float2(8,94))/float2(38,42));
  float eastPocket=smoothstep(35,43,p.x)*(1-smoothstep(63,69,p.x));
  eastPocket*=smoothstep(76,82,p.y)*(1-smoothstep(107,113,p.y));
  float surfArea=eastPocket*smoothstep(.87,.94,coastRadius)*(1-smoothstep(.7,2.1,depth));
  float incoming=pow(saturate(sin(depth*4.8+t*.85+sin(p.y*.24)*.35)),10);
  foam+=surfArea*incoming*smoothstep(.16,.57,foamNoise)*.28*(1-SurfPocket(p)*_BaySurfEnabled);
  foam=max(foam,SurfFoam(p,foamNoise)*.62+SurfRockFoam(p,foamNoise));
  float2 delta=p-_BayBoat.xz;float along=dot(delta,_BayBoatDirection.xy);
  float side=abs(dot(delta,float2(_BayBoatDirection.y,-_BayBoatDirection.x)));
  float bow=exp(-pow((along-1.7)*2,2)-pow((side-.65)*3,2))*saturate(_BayBoat.w/5);
  foam+=bow*smoothstep(.2,.65,foamNoise)*.65;
  c=lerp(c,float3(.72,.88,.80),saturate(foam+rippleFoam));
  return half4(MixFog(c,i.fog),1);
 }
 ENDHLSL
 }
 }
}
