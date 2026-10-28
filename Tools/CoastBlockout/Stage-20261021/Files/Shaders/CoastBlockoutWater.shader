Shader "Deepsea/BlockoutWater"
{
    Properties { _WaterColor("Technical water tint",Color)=(.27,.39,.46,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "TechnicalWater"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _WaterColor;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; };
            V Vert(A i) { V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o; }
            half4 Frag(V i):SV_Target
            {
                float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 bottom=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float thickness=max(0,i.world.y-bottom.y);
                float alpha=clamp(.24+thickness*.032,.24,.82);
                half3 color=lerp(_WaterColor.rgb,half3(.04,.08,.12),saturate(thickness/24));
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
