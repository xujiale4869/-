Shader "Deepsea/SeaLifeAquariumGlassV25"
{
    Properties
    {
        [MainColor] _BaseColor("Glass / Water Tint", Color) = (0.42, 0.84, 0.90, 0.09)
        _RimColor("Bevel Edge Highlight", Color) = (0.78, 0.96, 1.0, 0.42)
        _CausticsMap("Optional Caustics Map", 2D) = "black" {}
        _CausticsOffset("Caustics Scroll Offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            TEXTURE2D(_CausticsMap);
            SAMPLER(sampler_CausticsMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                float4 _CausticsOffset;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                o.uv         = v.uv;
                return o;
            }

            half4 frag(Varyings v) : SV_Target
            {
                half3 n = normalize(v.normalWS);
                half3 viewDir = normalize(_WorldSpaceCameraPos - v.positionWS);
                half fresnel = pow(1.0h - saturate(abs(dot(n, viewDir))), 2.6h);

                // 顶部暖灯向下衰减的柔和丁达尔水光梯度
                half verticalGlow = saturate((v.positionWS.y - 0.5h) * 0.55h);
                half2 cUV1 = v.uv * 2.2h + _CausticsOffset.xy;
                half2 cUV2 = v.uv * 1.7h + _CausticsOffset.zw;
                half caustics = (SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, cUV1).r +
                                 SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, cUV2).r) * 0.5h;

                half3 col = lerp(_BaseColor.rgb, _RimColor.rgb, fresnel * 0.65h) + caustics * 0.35h;
                col += half3(0.08h, 0.14h, 0.16h) * verticalGlow;
                half alpha = saturate(_BaseColor.a + fresnel * _RimColor.a * 0.32h + caustics * 0.12h);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
