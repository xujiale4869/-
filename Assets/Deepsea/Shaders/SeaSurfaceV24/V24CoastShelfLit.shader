Shader "Deepsea/V24CoastShelfLit" {
 Properties{_SandMap("Shallow sand",2D)="white"{} _BaseMap("Stone colour",2D)="white"{} _BumpMap("Stone normal",2D)="bump"{} _MaskMap("Stone ARM",2D)="white"{} _CausticMap("Caustic light",2D)="black"{}}
 SubShader {
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Back
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_SandMap);SAMPLER(sampler_SandMap);TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);TEXTURE2D(_CausticMap);SAMPLER(sampler_CausticMap);
 float _CoastWaterDetails,_OceanV24UnderLOD;
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 c:COLOR;};
 V Vert(A a){UNITY_SETUP_INSTANCE_ID(a);V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;return o;}


half3 OceanLowLight(float3 p,float3 n,half3 albedo){
 Light sun=GetMainLight(TransformWorldToShadowCoord(p));
 half3 ambient=max(half3(.075,.11,.13),SampleSH(n));
 return albedo*(ambient+sun.color*saturate(dot(n,sun.direction))*.96*sun.shadowAttenuation);
}

half3 OceanLow(V i){
 float3 n=normalize(i.n),w=pow(abs(n),4);w/=max(.001,dot(w,1));float3 p=i.world*.43;
 half3 stone=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*w.z;
 half lum=dot(stone,half3(.299,.587,.114));stone=pow(max(lerp(lum.xxx,stone,.30),.001),.72)*half3(1.08,1.16,1.18);
 half3 sand=lerp(half3(.20,.23,.21),SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.world.xz*.19).rgb*.72,.36);
 stone=lerp(sand,stone,saturate(i.c.r))*half3(.78,.88,.86);return OceanLowLight(i.world,n,stone);
}

 ENDHLSL
 Pass{Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_instancing
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 half4 Frag(V i):SV_Target{float budget=smoothstep(.25,.85,-.22-i.world.y)*_OceanV24UnderLOD*step(-.22,_WorldSpaceCameraPos.y);if(budget>.999)return half4(OceanLow(i),1);
 float3 geom=normalize(i.n),w=pow(abs(geom),4);w/=max(.001,w.x+w.y+w.z);float3 p=i.world*.43;
 half3 stone=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*w.z;
 float3 nx=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.zy)),ny=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xz)),nz=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xy));
 float3 detail=normalize(float3(sign(geom.x)*nx.z,nx.y,nx.x)*w.x+float3(ny.x,sign(geom.y)*ny.z,ny.y)*w.y+float3(nz.x,nz.y,sign(geom.z)*nz.z)*w.z);
 float3 n=normalize(lerp(geom,detail,.58));
 float variation=.91+.09*sin(i.world.x*.81+sin(i.world.z*.67));
 float sediment=smoothstep(.45,.88,geom.y)*(.5+.5*sin(i.world.x*.39+i.world.z*.71));
 half lum=dot(stone,half3(.299,.587,.114));stone=pow(max(lerp(lum.xxx,stone,.30),.001),.72)*half3(1.08,1.16,1.18)*variation;
 stone=lerp(stone,stone*half3(.76,.89,.70),sediment*.18);
 half3 sand=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.world.xz*.19).rgb;
 sand=lerp(half3(.20,.23,.21),sand*.72,.36);stone=lerp(sand,stone,saturate(i.c.r));
 float wet=1-smoothstep(-.35,.55,i.world.y);stone*=lerp(half3(1,1,1),half3(.78,.88,.86),wet);
 Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));half3 ambient=max(lerp(half3(.09,.12,.13),half3(.12,.18,.20),saturate(i.c.r)),SampleSH(n));
 half3 c=stone*(ambient+sun.color*(.06+.94*saturate(dot(n,sun.direction)))*sun.shadowAttenuation);
 float waterDepth=-.22-i.world.y;
 if(waterDepth>.15&&waterDepth<12){float2 uv=i.world.xz*.09;float caustic=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,uv+float2(_Time.y*.003,_Time.y*.002)).r;c+=half3(.16,.25,.23)*caustic*exp(-waterDepth*.22)*saturate(geom.y)*sun.shadowAttenuation*_CoastWaterDetails;}
 float under=saturate((-.22-_WorldSpaceCameraPos.y)*.4);c=lerp(c,half3(.018,.10,.13),(1-exp(-distance(_WorldSpaceCameraPos,i.world)*.038))*under);
 if(budget>0)c=lerp(c,OceanLow(i),budget);return half4(c,1);
 }
 ENDHLSL
 }
 Pass{Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Depth
 #pragma multi_compile_instancing
 half4 Depth(V i):SV_Target{return 0;}
 ENDHLSL
 }
 Pass{Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex ShadowVert
 #pragma fragment Depth
 #pragma multi_compile_instancing
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
