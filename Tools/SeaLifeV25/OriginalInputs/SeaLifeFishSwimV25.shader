Shader "Deepsea/SeaLifeFishSwimV25"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Albedo (RGB) Smoothness (A)", 2D) = "white" {}
        _RimColor("Iridescent Rim Color", Color) = (0.52, 0.88, 0.94, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.78
        _Metallic("Metallic", Range(0, 1)) = 0.25
        _TailAmplitude("Tail Wave Amplitude", Range(0, 0.25)) = 0.085
        _TailFrequency("Tail Wave Spatial Freq", Range(1, 12)) = 5.5
        _SwimPhase("Swim Phase (Per-Instance)", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half4 _RimColor;
                half _Smoothness;
                half _Metallic;
                float _TailAmplitude;
                float _TailFrequency;
                float _SwimPhase;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 pos = v.positionOS.xyz;

                // uv.x 编码从鱼头 (0.0) 到鱼尾 (1.0) 的归一化轴向位置
                float axial = saturate(v.uv.x);
                // 鱼头保持刚性稳定，越往尾柄与尾鳍摆动幅度呈二次方增大
                float mask = pow(axial, 1.65);
                float phase = _SwimPhase != 0.0 ? _SwimPhase : (_Time.y * 6.5);
                float wave = sin(phase - axial * _TailFrequency) * _TailAmplitude * mask;

                pos.x += wave;
                // 尾鳍上下叶伴随轻微流体柔性扭转
                pos.x += v.positionOS.y * cos(phase - axial * _TailFrequency) * (_TailAmplitude * 0.28 * mask);

                o.positionWS = TransformObjectToWorld(pos);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                o.uv         = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings v) : SV_Target
            {
                half3 n = normalize(v.normalWS);
                half3 viewDir = normalize(_WorldSpaceCameraPos - v.positionWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(v.positionWS));

                half4 texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, v.uv);
                half3 albedo = texSample.rgb * _BaseColor.rgb;
                half smoothness = _Smoothness * lerp(0.75h, 1.15h, texSample.a);

                half ndotl = saturate(dot(n, light.direction));
                half shadow = lerp(0.38h, 1.0h, light.shadowAttenuation);
                half3 ambient = max(SampleSH(n), half3(0.14h, 0.18h, 0.20h));
                half3 diffuse = ambient + light.color * ndotl * shadow;

                // 湿润鱼鳞双层高光 + 侧线虹彩菲涅尔边缘光
                half3 h = normalize(light.direction + viewDir);
                half spec = pow(saturate(dot(n, h)), 48.0h) * smoothness;
                half fresnel = pow(1.0h - saturate(abs(dot(n, viewDir))), 2.8h);
                half3 iridescence = _RimColor.rgb * fresnel * 0.42h;

                // 鱼鳍区域 (v.uv.y > 0.72) 增加轻微透光次表面散射感
                half finMask = step(0.72h, v.uv.y);
                half backLight = saturate(dot(-n, light.direction)) * finMask * 0.35h;

                half3 finalRGB = albedo * (diffuse + backLight) + light.color * spec * lerp(0.12h, 0.48h, _Metallic) + iridescence;
                return half4(finalRGB, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 vertDepth(float4 p : POSITION) : SV_POSITION { return TransformObjectToHClip(p.xyz); }
            half4 fragDepth() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
