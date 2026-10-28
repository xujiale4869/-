Shader "Deepsea/BlockoutClay"
{
    Properties { _BaseColor("Clay",Color)=(.55,.56,.57,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        CBUFFER_END
        struct A { float4 p:POSITION; float3 n:NORMAL; };
        struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 n:TEXCOORD1; };
        V Vert(A a) { V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);return o; }
        ENDHLSL
        Pass
        {
            Name "ForwardClay"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            half4 Frag(V i):SV_Target
            {
                half3 n=normalize(i.n);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                half diffuse=saturate(dot(n,sun.direction));
                half3 ambient=max(half3(.20,.21,.22),SampleSH(n));
                return half4(_BaseColor.rgb*(ambient+sun.color*(.15+.85*diffuse)*sun.shadowAttenuation),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Depth
            half4 Depth(V i):SV_Target {return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment Depth
            float3 _LightDirection;
            V ShadowVert(A a)
            {
                V o=Vert(a);o.p=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.n),_LightDirection));
                #if UNITY_REVERSED_Z
                o.p.z=min(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
                #else
                o.p.z=max(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 Depth(V i):SV_Target {return 0;}
            ENDHLSL
        }
    }
}
