Shader "Deepsea/NaturalFoliage" {
 SubShader {Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Off
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;float2 uv:TEXCOORD0;};
 struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 c:COLOR;float fog:TEXCOORD2;};
 V vert(A a){V o;float3 p=TransformObjectToWorld(a.p.xyz);float sway=a.uv.x*a.uv.x;p.x+=sway*sin(_Time.y*.7+p.y*.5+a.uv.y)*.32;p.z+=sway*cos(_Time.y*.5+a.uv.y)*.15;o.world=p;o.p=TransformWorldToHClip(p);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;o.fog=ComputeFogFactor(o.p.z);return o;}
 ENDHLSL
 Pass {Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 half4 frag(V i):SV_Target{Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));float n=abs(dot(normalize(i.n),sun.direction));float3 c=i.c.rgb*(.42+.68*n*sun.shadowAttenuation);float rim=pow(1-abs(dot(normalize(i.n),GetWorldSpaceNormalizeViewDir(i.world))),3);c+=i.c.rgb*rim*.17;return half4(MixFog(c,i.fog),1);}
 ENDHLSL }
 Pass {Name "DepthOnly" Tags{"LightMode"="DepthOnly"}ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment depth
 half4 depth(V i):SV_Target{return 0;}
 ENDHLSL }
 UsePass "Deepsea/CoastalGround/ShadowCaster"
 }
}
