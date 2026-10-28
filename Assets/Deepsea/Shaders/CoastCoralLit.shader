Shader "Deepsea/CoastCoralLit" {
 Properties{_CausticMap("Caustic light",2D)="black"{}}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_CausticMap);SAMPLER(sampler_CausticMap);float _CoastWaterDetails;
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;half4 c:COLOR;};
 V Vert(A a){UNITY_SETUP_INSTANCE_ID(a);V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;return o;}
 ENDHLSL
 Pass{Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_instancing
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 half4 Frag(V i,bool front:SV_IsFrontFace):SV_Target{
 float3 n=normalize(i.n)*(front?1:-1);float depth=max(0,-.22-i.world.y);
 Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
 half3 ambient=max(half3(.11,.16,.18),SampleSH(n));
 float diffuse=saturate(dot(n,sun.direction));
 float pore=.94+.035*sin(i.world.x*61+sin(i.world.y*39))*sin(i.world.z*51);
 half3 base=pow(max(i.c.rgb,.001),2.2)*pore;
 half3 c=base*(ambient+sun.color*(.12+.88*diffuse)*sun.shadowAttenuation);
 c+=base*half3(.04,.065,.055)*saturate(dot(-n,sun.direction));
 float caustic=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,i.world.xz*.065+_Time.y*float2(.003,.0015)).r;
 c+=base*half3(.13,.19,.15)*caustic*exp(-depth*.21)*saturate(n.y)*_CoastWaterDetails;
 c*=lerp(half3(1,1,1),half3(.40,.72,.78),saturate(depth/14)*.50);
 float under=saturate((-.22-_WorldSpaceCameraPos.y)*.4);
 c=lerp(c,half3(.018,.10,.13),(1-exp(-distance(_WorldSpaceCameraPos,i.world)*.038))*under);
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
 half4 Depth(V i):SV_Target{return 0;}
 ENDHLSL
 }
 }
}
