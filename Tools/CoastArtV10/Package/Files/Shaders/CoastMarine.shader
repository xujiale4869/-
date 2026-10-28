Shader "Deepsea/CoastMarine" {
 Properties{_BaseMap("Leaf albedo or mineral surface",2D)="white"{} _BumpMap("Coral micro normal",2D)="bump"{} _Tint("Tint",Color)=(1,1,1,1) _Kind("0 kelp 1 eelgrass 2 coral",Float)=0 _Sway("Current sway",Float)=.1}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _Tint;float _Kind,_Sway;
 CBUFFER_END
 float _CoastWaterDetails;
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 c:COLOR;};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;half4 c:COLOR;};
 V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);float weight=saturate(a.c.a);weight*=weight;
 float wave=sin(_Time.y*.62+o.world.z*.28+o.world.x*.20);
 o.world.x+=wave*_Sway*weight;o.world.z+=cos(_Time.y*.47+o.world.x*.25)*_Sway*.55*weight;
 o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.c=a.c;return o;}
 ENDHLSL
 Pass{Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 half4 Frag(V i,bool front:SV_IsFrontFace):SV_Target{
 float3 geom=normalize(i.n)*(front?1:-1),n=geom;float2 uv=i.uv;
 if(_Kind>1.5){float3 w=abs(geom);uv=(w.x>w.y&&w.x>w.z?i.world.zy:w.y>w.z?i.world.xz:i.world.xy)*3.5;}
 half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;half lum=dot(tex,half3(.299,.587,.114));half3 base;
 if(_Kind>1.5){
   base=lerp(half3(.65,.65,.65),lerp(lum.xxx,tex,.3),.50)*lerp(dot(i.c.rgb,half3(.299,.587,.114)).xxx,i.c.rgb,.48)*_Tint.rgb*1.6;
   float3 detail=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv));float3 axis=abs(geom.y)>.8?float3(1,0,0):normalize(cross(float3(0,1,0),geom));n=normalize(geom+axis*detail.x*.22+cross(geom,axis)*detail.y*.22);
 }else{
   base=lerp(lum.xxx,tex,.68)*_Tint.rgb*lerp(half3(.56,.74,.65),half3(.94,1.04,.96),i.c.r);
   // Fronds have real curved cross sections; the texture adds surface grain rather than a silhouette.
 }
 Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));float light=saturate((dot(n,sun.direction)+.18)/1.18);
 half3 ambient=max(half3(.075,.13,.14),SampleSH(n));half3 c=base*(ambient+sun.color*(.08+.92*light)*sun.shadowAttenuation);
 if(_Kind<1.5)c+=base*sun.color*saturate(dot(-geom,sun.direction))*.20*sun.shadowAttenuation;
 float3 h=normalize(sun.direction+GetWorldSpaceNormalizeViewDir(i.world));c+=sun.color*pow(saturate(dot(n,h)),_Kind>1.5?20:42)*(_Kind>1.5?.012:.035)*sun.shadowAttenuation;
 float depth=max(0,-.22-i.world.y);c*=lerp(half3(1,1,1),half3(.32,.61,.65),saturate(depth/14)*.70);
 float caustic=.5+.5*sin(i.world.x*2.8+sin(i.world.z*2.2)+_Time.y*.38);c+=base*half3(.07,.14,.12)*caustic*exp(-depth*.30)*saturate(geom.y)*_CoastWaterDetails;
 float under=saturate((-.22-_WorldSpaceCameraPos.y)*.4);c=lerp(c,half3(.018,.10,.13),(1-exp(-distance(_WorldSpaceCameraPos,i.world)*.038))*under);
 return half4(c,1);
 }
 ENDHLSL
 }
 Pass{Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Depth
 half4 Depth(V i):SV_Target{return 0;}
 ENDHLSL
 }
 }
}
