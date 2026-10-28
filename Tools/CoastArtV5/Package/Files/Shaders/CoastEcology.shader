Shader "Deepsea/CoastEcology"
{
 Properties{_BaseMap("Texture",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _Cutoff("Alpha clip",Float)=0 _VertexColor("Vertex colour",Float)=1 _Sway("Sway metres",Float)=0 _Fish("Fish tail",Float)=0 _Emission("Emission",Float)=0}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="AlphaTest"} Cull Off
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _Tint;float _Cutoff,_VertexColor,_Sway,_Fish,_Emission;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;float4 c:COLOR;};
 V Vert(A a){UNITY_SETUP_INSTANCE_ID(a);V o;float3 local=a.p.xyz;
 if(_Fish>.5)local.x+=sin(_Time.y*7+UNITY_MATRIX_M._m03*2+local.z*5)*.12*saturate((-local.z-.1)*2);
 o.world=TransformObjectToWorld(local);float weight=saturate(a.uv.y);
 o.world.x+=sin(_Time.y*.75+o.world.z*.42+o.world.x*.23)*_Sway*weight*weight;
 o.world.z+=cos(_Time.y*.56+o.world.x*.39)*_Sway*.5*weight*weight;
 o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.c=a.c;return o;}
 ENDHLSL
 Pass{Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_instancing
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 half4 Frag(V i):SV_Target{
 half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);clip(tex.a-_Cutoff);
 half3 colour=tex.rgb*_Tint.rgb*lerp(half3(1,1,1),i.c.rgb,_VertexColor);
 float3 n=normalize(i.n);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
 float diffuse=saturate((dot(n,sun.direction)+.25)/1.25);half3 ambient=max(half3(.12,.16,.17),SampleSH(n));
 half3 c=colour*(ambient+sun.color*(.14+.86*diffuse)*sun.shadowAttenuation);
 if(_Cutoff>.05){float back=saturate(dot(-n,sun.direction));c+=colour*sun.color*back*.13*sun.shadowAttenuation;}
 c+=colour*_Emission;
 float depth=max(0,-.22-i.world.y);c*=lerp(half3(1,1,1),half3(.39,.69,.72),saturate(depth/15)*.65);
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
 #pragma multi_compile_instancing
 half4 Depth(V i):SV_Target{clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);return 0;}
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
 half4 Depth(V i):SV_Target{clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);return 0;}
 ENDHLSL
 }
 }
}
