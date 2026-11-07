Shader "Deepsea/StylizedCoastalCraftV32"
{
    Properties
    {
        [MainColor] _BaseColor("Base Tint", Color) = (0.72, 0.60, 0.46, 1)
        _BaseMap("Optional Albedo Map", 2D) = "white" {}
        _CraftMode("Material Mode (0:PlankWood 1:SlateRoof 2:MasonryStone 3:LogTimber)", Float) = 0
        _GrainScale("Wood Grain / Stone Detail Scale", Range(0.2, 8.0)) = 1.8
        _NormalStrength("Micro Relief Normal Strength", Range(0, 2.0)) = 0.95
        _Roughness("Base Roughness", Range(0.1, 1.0)) = 0.72
        _EdgeWear("Chamfer Edge Sunlit Wear", Range(0, 1.0)) = 0.42
        _MossStrength("Coastal Moss & Tidal Wetness", Range(0, 1.0)) = 0.58
        _AOStrength("Vertex Cavity AO Strength", Range(0, 1.0)) = 0.65
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Back

        Pass
        {
            Name "CoastalCraftForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "HarborLightingV27.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _CraftMode;
                float _GrainScale;
                float _NormalStrength;
                float _Roughness;
                float _EdgeWear;
                float _MossStrength;
                float _AOStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR; // rgb = 逐板/逐石色相明度微扰 + 边缘磨损掩码, a = 烘焙结构环境光遮蔽 (AO)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                half fog          : TEXCOORD3;
                half4  vertColor  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                o.worldPos   = world;
                o.positionCS = TransformWorldToHClip(world);
                o.fog = ComputeFogFactor(o.positionCS.z);
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                o.uv         = input.uv;
                o.vertColor  = input.color;
                return o;
            }

            float EvaluateCraftMicroRelief(float3 world, float2 uv, float mode)
            {
                float2 grain = uv * _GrainScale;
                if (mode < .5 || mode > 2.5)
                {
                    // Long-axis UVs follow the actual board or beam, including after mesh combining.
                    float phase = grain.y * 39.0 + sin(grain.x * 4.0 + world.z * .6) * 1.6;
                    float fine = sin(grain.y * 118.0 + sin(grain.x * 6.0));
                    return .5 + sin(phase) * .31 + fine * .13;
                }
                float3 p = world * _GrainScale;
                if (mode < 1.5)
                {
                    // Geometry already provides the staggered slate joints.
                    return .5 + sin(p.x * 17.0 + p.z * 12.0) * .13
                              + sin(p.z * 33.0 + p.y * 9.0) * .12;
                }
                return .5 + sin(p.x * 14.0 + sin(p.z * 9.0)) * cos(p.y * 11.0) * .26
                          + sin(p.z * 32.0 - p.x * 21.0) * .12;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 geomN = normalize(i.normalWS);
                float height = EvaluateCraftMicroRelief(i.worldPos, i.uv, _CraftMode);
                // Screen-space surface gradient avoids a tangent flip across bevel faces.
                float3 a = ddx(i.worldPos), b = ddy(i.worldPos);
                float3 r1 = cross(b, geomN), r2 = cross(geomN, a);
                float det = dot(a, r1);
                float3 gradient = (ddx(height) * r1 + ddy(height) * r2)
                    * sign(det) / max(abs(det), 1e-8);
                gradient *= min(.0035 * _NormalStrength, .24 / max(length(gradient), 1e-3));
                float3 n = normalize(geomN - gradient);

                half3 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                half3 tint = dot(i.vertColor.rgb, half3(1,1,1)) > .05 ? i.vertColor.rgb : half3(1,1,1);
                float cavity = i.vertColor.a > .001 ? lerp(1.0, i.vertColor.a, _AOStrength) : 1.0;
                half3 albedo = texCol * _BaseColor.rgb * tint * lerp(.88,1.09,height) * lerp(.85,1.0,cavity);
                float edge = smoothstep(.15,.49,geomN.y) * (1.0-smoothstep(.78,.995,geomN.y));
                albedo += albedo * half3(.22,.18,.12) * edge * _EdgeWear;

                float aboveSea = i.worldPos.y - (-.22);
                float wet = (1.0-smoothstep(-.35,.62,aboveSea)) * _MossStrength;
                albedo = lerp(albedo, albedo * half3(.53,.62,.58), wet);
                float patch = .5 + .29 * sin(i.worldPos.x * 1.25 + cos(i.worldPos.z * .85) * 2.0)
                                 + .19 * cos(i.worldPos.z * 2.05 - i.worldPos.x * .95);
                float moss = smoothstep(.47,.83,patch) * (1.0-smoothstep(.25,3.4,aboveSea))
                    * lerp(.42,1.0,saturate(geomN.y)) * _MossStrength;
                albedo = lerp(albedo, half3(.26,.42,.24), moss*.43);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.normalTS = half3(0,0,1);
                surface.smoothness = clamp(1.0 - _Roughness + wet*.20,.08,.72);
                surface.occlusion = cavity;
                surface.alpha = 1.0;
                InputData input = (InputData)0;
                input.positionWS = i.worldPos;
                input.normalWS = n;
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.worldPos);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    input.shadowCoord = ComputeScreenPos(TransformWorldToHClip(i.worldPos));
                #else
                    input.shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                #endif
                input.bakedGI = max(half3(.12,.15,.17), SampleSH(n));
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                input.shadowMask = half4(1,1,1,1);
                half3 harbor = HarborDiffuseV27(i.worldPos,n) * albedo;
                half3 pbrColor = MixFog(UniversalFragmentPBR(input, surface).rgb + harbor, i.fog);
                return half4(pbrColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - worldPos);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                o.positionCS.z = min(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                o.positionCS.z = max(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                o.positionCS = TransformObjectToHClip(TransformObjectToWorld(input.positionOS.xyz));
                return o;
            }

            half4 DepthFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
