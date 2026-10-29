Shader "Deepsea/SeaSurfaceSparkleV26"
{
    Properties
    {
        [Header(Five Zone Depth Hierarchy)]
        _CrystalShoreColor("Zone 1: Crystal Shore Tint (<0.6m)", Color) = (0.76, 0.90, 0.78, 1)
        _Shallow("Zone 2: Emerald Lagoon (0.6m - 2.8m)", Color) = (0.09, 0.66, 0.62, 1)
        _ReefMidColor("Zone 3: Turquoise Reef Shelf (2.8m - 6.5m)", Color) = (0.03, 0.38, 0.50, 1)
        _Deep("Zone 4: Deep Trench Indigo (>6.5m)", Color) = (0.008, 0.11, 0.21, 1)
        _DepthRampMap("Optional 5-Zone Depth Ramp LUT", 2D) = "white" {}

        [Header(Waves and Refraction)]
        _WaveMap("Fine Wave Normal Slopes", 2D) = "gray" {}
        _Refraction("Refraction Distortion", Range(0, 0.015)) = 0.0032
        _Visibility("Underwater Clarity", Range(0, 1)) = 0.85
        _SurfaceWeight("Surface Reflection Weight", Range(0, 1)) = 0.42

        [Header(Underwater Voronoi Caustics Web)]
        _CausticsMap("Voronoi Caustics & Sparkle (RGB)", 2D) = "black" {}
        _CausticsIntensity("Caustics Brightness on Sand/Reef", Range(0, 2.5)) = 1.15
        _CausticsScale("Caustics World Tiling", Range(0.05, 1.0)) = 0.24
        _CausticsSpeed("Caustics Flow Speed", Range(0, 1.5)) = 0.28
        _CausticsMaxDepth("Caustics Depth Fade (m)", Range(1, 15)) = 6.2

        [Header(Sun Sparkle and Shimmering Glints)]
        _GlintEnable("Enable Sun Sparkle", Range(0, 1)) = 1
        _GlintIntensity("Sparkle Peak Intensity", Range(0, 5)) = 2.65
        _GlintScale("Sparkle Cell Scale", Range(3, 30)) = 11.5
        _GlintSpeed("Sparkle Shimmer Speed", Range(0, 3)) = 0.85
        _SunPathColor("Warm Sun Glint Tint", Color) = (1.0, 0.94, 0.78, 1)

        [Header(Shoreline Surge Foam and Boat Wake)]
        _FoamMap("Shore Foam Cellular Noise", 2D) = "white" {}
        _FoamIntensity("Shore & Wake Foam Brightness", Range(0, 2)) = 0.92
        _FoamWidth("Shore Surge Band Width (m)", Range(0.05, 1.2)) = 0.42
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "SparkleLayeredWaterV26"
            Tags { "LightMode"="UniversalForward" }
            Blend One Zero
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #define _SURFACE_TYPE_TRANSPARENT 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_WaveMap);        SAMPLER(sampler_WaveMap);
            TEXTURE2D(_CausticsMap);    SAMPLER(sampler_CausticsMap);
            TEXTURE2D(_FoamMap);        SAMPLER(sampler_FoamMap);
            TEXTURE2D(_DepthRampMap);   SAMPLER(sampler_DepthRampMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _CrystalShoreColor;
                half4 _Shallow;
                half4 _ReefMidColor;
                half4 _Deep;
                float _Refraction;
                float _Visibility;
                float _SurfaceWeight;
                float _CausticsIntensity;
                float _CausticsScale;
                float _CausticsSpeed;
                float _CausticsMaxDepth;
                float _GlintEnable;
                float _GlintIntensity;
                float _GlintScale;
                float _GlintSpeed;
                half4 _SunPathColor;
                float _FoamIntensity;
                float _FoamWidth;
            CBUFFER_END

            // 兼容仓库 v22 / v24 全局变量
            float _WaterV22Time;
            float _OceanV24Time;
            float _CoastWaterDepthProbe;
            float _CoastWaterDetails;
            float _OceanV24Layer;
            float4 _CoastBoat;
            float4 _CoastBoatHeading;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 waveNormal : TEXCOORD1;
                float  eyeDepth   : TEXCOORD2;
                float  waveCrest  : TEXCOORD3;
            };

            // 融合 Gerstner 涌浪与三分量海湾谐波，兼顾远海长波运动感与内湾柔波起伏
            float3 EvaluateBayWaves(float2 p, float t, out float3 outNormal, out float outCrest)
            {
                // 主涌浪方向 (从右上方外海向内湾推进)
                float2 d1 = float2(-0.78, -0.62);
                float2 d2 = float2(-0.45,  0.89);
                float2 d3 = float2( 0.68, -0.73);
                float2 d4 = float2( 0.29, -0.95);

                float f1 = dot(p, d1) * 0.75 - t * 1.18;
                float f2 = dot(p, d2) * 1.12 - t * 1.52 + 0.85;
                float f3 = dot(p, d3) * 1.85 - t * 1.95 + 2.10;
                float f4 = dot(p, d4) * 3.10 - t * 2.45 + 1.40;

                float a1 = 0.042, a2 = 0.026, a3 = 0.015, a4 = 0.008;

                // Gerstner 水平挤压位移 (产生浪尖锐利、浪谷平缓的真实水波运动感)
                float2 dispXZ =
                    d1 * (a1 * 0.55 * cos(f1)) +
                    d2 * (a2 * 0.50 * cos(f2)) +
                    d3 * (a3 * 0.40 * cos(f3));

                float dispY =
                    a1 * sin(f1) +
                    a2 * sin(f2) +
                    a3 * sin(f3) +
                    a4 * sin(f4);

                float2 grad =
                    d1 * (a1 * 0.75 * cos(f1)) +
                    d2 * (a2 * 1.12 * cos(f2)) +
                    d3 * (a3 * 1.85 * cos(f3)) +
                    d4 * (a4 * 3.10 * cos(f4));

                outNormal = normalize(float3(-grad.x, 1.0, -grad.y));
                outCrest = saturate((dispY / (a1 + a2 + a3)) * 0.5 + 0.5);
                return float3(dispXZ.x, dispY, dispXZ.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float t = (_OceanV24Time > 0.001) ? _OceanV24Time : ((_WaterV22Time > 0.001) ? _WaterV22Time : _Time.y);

                float3 wNormal;
                float wCrest;
                float3 waveOffset = EvaluateBayWaves(world.xz, t, wNormal, wCrest);
                world += waveOffset;

                o.worldPos   = world;
                o.waveNormal = wNormal;
                o.waveCrest  = wCrest;
                o.positionCS = TransformWorldToHClip(world);
                o.eyeDepth   = -TransformWorldToView(world).z;
                return o;
            }

            float SampleDeviceDepth(float2 uv)
            {
                float d = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                d = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, d);
                #endif
                return d;
            }

            // 程序化 Voronoi 焦散兜底函数 (当材质未绑定贴图时依然能渲染出细腻焦散网格)
            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float ProceduralVoronoiCaustics(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float d1 = 8.0, d2 = 8.0;
                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        float2 o = Hash22(i + g);
                        float2 r = g + (0.15 + 0.70 * o) - f;
                        float d = dot(r, r);
                        if (d < d1) { d2 = d1; d1 = d; }
                        else if (d < d2) { d2 = d; }
                    }
                }
                float edge = max(0.0, sqrt(d2) - sqrt(d1));
                return exp(-edge * 10.5);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float rawDepth = SampleDeviceDepth(screenUV);
                float3 bottomWS = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float waterDepth = max(0.0, i.worldPos.y - bottomWS.y);

                if (_CoastWaterDepthProbe > 0.5)
                    return half4(saturate(waterDepth / 25.0), 0, 0, 1);

                clip(waterDepth - 0.0015);

                float t = (_OceanV24Time > 0.001) ? _OceanV24Time : ((_WaterV22Time > 0.001) ? _WaterV22Time : _Time.y);
                float detailScale = (_CoastWaterDetails > 0.01) ? _CoastWaterDetails : 1.0;
                float2 p = i.worldPos.xz;

                // -----------------------------------------------------------------
                // 1. 三尺度运动波纹法线场 (宏观涌浪 + 中频交叉交织波 + 高频微涟漪)
                // -----------------------------------------------------------------
                half2 rippleA = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.085 + float2(-t * 0.016, t * 0.009)).rg * 2.0 - 1.0;
                half2 rippleB = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.165 + float2(t * 0.012, -t * 0.014)).rg * 2.0 - 1.0;
                half2 microWave = float2(
                    sin(p.x * 1.45 + p.y * 0.85 - t * 2.1) + cos(p.y * 2.2 - p.x * 1.1 + t * 1.7),
                    cos(p.y * 1.55 - p.x * 0.95 - t * 1.9) - sin(p.x * 2.0 + p.y * 1.3 + t * 2.3)
                ) * 0.018;

                float2 combinedSlope = (rippleA * 0.085 + rippleB * 0.055 + microWave) * detailScale;
                float3 n = normalize(i.waveNormal + float3(combinedSlope.x, 0.0, combinedSlope.y));
                float3 v = GetWorldSpaceNormalizeViewDir(i.worldPos);
                float ndotv = saturate(dot(n, v));

                // -----------------------------------------------------------------
                // 2. 真实水下折射采样与岸线防溢色保护
                // -----------------------------------------------------------------
                float2 refractOffset = n.xz * _Refraction * smoothstep(0.05, 0.85, waterDepth);
                float2 refractedUV = saturate(screenUV + refractOffset);
                float refractedEye = LinearEyeDepth(SampleSceneDepth(refractedUV), _ZBufferParams);
                if (refractedEye < i.eyeDepth - 0.05)
                {
                    refractedUV = screenUV;
                }
                float3 refractedBottomWS = ComputeWorldSpacePosition(refractedUV, SampleDeviceDepth(refractedUV), UNITY_MATRIX_I_VP);
                float refractedDepth = max(0.0, i.worldPos.y - refractedBottomWS.y);
                float depthM = lerp(waterDepth, refractedDepth, 0.65);

                half3 seabedScene = SampleSceneColor(refractedUV);

                // -----------------------------------------------------------------
                // 3. 浅滩双层交错流动焦散光网 (投射在真实水底沙地、栈桥木桩与浅礁上)
                // -----------------------------------------------------------------
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(refractedBottomWS));
                // 根据水底位置与光线倾角计算焦散投影坐标，并叠加水面法线折射扭曲
                float2 causticsUV = refractedBottomWS.xz * _CausticsScale + n.xz * 0.18;
                float2 flow1 = causticsUV + float2(t * _CausticsSpeed * 0.14, t * _CausticsSpeed * 0.09);
                float2 flow2 = causticsUV * 1.28 + float2(-t * _CausticsSpeed * 0.11, t * _CausticsSpeed * 0.13) + float2(0.37, 0.61);

                half4 causticsTex1 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, flow1);
                half4 causticsTex2 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, flow2);
                float texCaustics = (causticsTex1.r + causticsTex2.g) * 0.65 + min(causticsTex1.r, causticsTex2.g) * 0.85;

                // 若未绑定贴图则启用内置高锐度 Voronoi 算法
                if (texCaustics < 0.005)
                {
                    float pc1 = ProceduralVoronoiCaustics(flow1 * 6.5);
                    float pc2 = ProceduralVoronoiCaustics(flow2 * 6.5);
                    texCaustics = (pc1 + pc2) * 0.58 + min(pc1, pc2) * 0.90;
                }

                // 焦散在 0.15m ~ 6.2m 浅水区最清晰，向极浅岸线与深海海沟平滑衰减
                float causticsDepthMask = smoothstep(0.04, 0.45, depthM) * exp(-depthM / max(1.0, _CausticsMaxDepth));
                float causticsShadow = lerp(0.25, 1.0, mainLight.shadowAttenuation);
                half3 causticsLight = _CrystalShoreColor.rgb * mainLight.color * (texCaustics * _CausticsIntensity * causticsDepthMask * causticsShadow);

                // 将焦散光网照亮水底沙床与珊瑚礁
                seabedScene += causticsLight * (0.45 + 0.55 * seabedScene);

                // -----------------------------------------------------------------
                // 4. 五级水深光学层次 (还原原图：暖金浅滩 -> 翡翠绿内湾 -> 孔雀青礁盘 -> 靛青深沟)
                // -----------------------------------------------------------------
                float z1 = saturate(depthM / 0.85);                        // 0.0 ~ 0.85m: 极浅清透岸滩
                float z2 = smoothstep(0.65, 3.20, depthM);                 // 0.65 ~ 3.2m: 翡翠薄荷绿内湾
                float z3 = smoothstep(2.60, 7.50, depthM);                 // 2.6 ~ 7.5m:  孔雀青蓝礁盘边缘
                float z4 = 1.0 - exp(-max(0.0, depthM - 5.5) * 0.24);      // >5.5m:       右侧深海鱼群海沟

                half3 waterBodyColor = lerp(_CrystalShoreColor.rgb, _Shallow.rgb, z1);
                waterBodyColor = lerp(waterBodyColor, _ReefMidColor.rgb, z2);
                waterBodyColor = lerp(waterBodyColor, _Deep.rgb, saturate(z3 * 0.72 + z4 * 0.55));

                // 朗伯-比尔定律 (Beer-Lambert): 红光最快被吸收，绿光次之，蓝青光穿透最深
                float opticalPath = min(depthM / max(0.38, abs(v.y)), 48.0);
                half3 absorption = exp(-opticalPath * half3(0.78, 0.16, 0.11));
                float clarity = _Visibility * exp(-depthM * 0.14) * lerp(0.52, 0.96, smoothstep(0.30, 0.85, ndotv));

                // 波峰次表面散射 (Subsurface Scattering): 阳光穿透波峰时泛出通透的翠绿青光
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                float backSun = saturate(dot(v, - normalize(sun.direction + float3(0, 0.35, 0))));
                float crestSSS = pow(i.waveCrest, 2.2) * (0.35 + 0.65 * backSun) * exp(-depthM * 0.18);
                waterBodyColor += half3(0.06, 0.32, 0.28) * crestSSS * sun.shadowAttenuation;

                half3 underwaterComposite = lerp(waterBodyColor, seabedScene * absorption + waterBodyColor * (1.0 - absorption), saturate(clarity));

                // -----------------------------------------------------------------
                // 5. 水面天光菲涅尔反射与涌浪条带运动感
                // -----------------------------------------------------------------
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndotv, 4.2);
                float3 reflDir = reflect(-v, n);
                half3 envRefl = GlossyEnvironmentReflection(reflDir, i.worldPos, 0.10, 1.0);
                half3 skyGrad = lerp(half3(0.08, 0.26, 0.34), half3(0.36, 0.56, 0.64), saturate(reflDir.y * 0.85 + 0.15));
                half3 surfaceReflection = max(envRefl * 0.58, skyGrad * 0.52);

                // 涌浪受光面与背光面明暗韵律 (塑造原图清晰可见的海面波浪体积起伏感)
                float waveShading = saturate(dot(n.xz, normalize(float2(-0.65, -0.75))) * 0.35 + 0.55);
                half3 finalColor = lerp(underwaterComposite, surfaceReflection, saturate(fresnel * _SurfaceWeight * 1.75));
                finalColor *= lerp(0.91, 1.09, waveShading);

                // -----------------------------------------------------------------
                // 6. 波光粼粼动态微面元太阳碎光 (Anisotropic Sun Sparkle & Shimmer)
                // -----------------------------------------------------------------
                float3 halfVec = normalize(sun.direction + v);
                float ndoth = saturate(dot(n, halfVec));

                // 基础柔和太阳高光道
                float broadSpec = pow(ndoth, 64.0) * 0.22;

                // 双频动态星芒微面元闪烁: 仅在波纹法线朝向太阳反射角且微面元晶格交汇时触发璀璨波光
                float2 glintUV1 = p * (_GlintScale * 0.12) + n.xz * 0.85 + float2(t * _GlintSpeed * 0.09, -t * _GlintSpeed * 0.07);
                float2 glintUV2 = p * (_GlintScale * 0.17) - n.xz * 0.65 + float2(-t * _GlintSpeed * 0.08, t * _GlintSpeed * 0.11);
                float glintCell1 = sin(glintUV1.x * 6.2831) * cos(glintUV1.y * 6.2831);
                float glintCell2 = cos((glintUV2.x + glintUV2.y) * 6.2831) * sin((glintUV2.x - glintUV2.y) * 6.2831);
                float sparkleFacet = saturate(glintCell1 * glintCell2 * 1.85);
                sparkleFacet = pow(sparkleFacet, 4.5);

                // 结合太阳高光圆锥包络与波峰曲率，形成真实波光粼粼感
                float sunCone = pow(ndoth, 14.0) * (0.25 + 0.75 * i.waveCrest);
                float totalSparkle = (broadSpec + sparkleFacet * sunCone * _GlintIntensity * _GlintEnable) * sun.shadowAttenuation * detailScale;
                finalColor += lerp(sun.color, _SunPathColor.rgb, 0.55) * totalSparkle;

                // -----------------------------------------------------------------
                // 7. 岸礁/栈桥多层涌浪泡沫环与小船 V 型扩散尾流
                // -----------------------------------------------------------------
                half4 foamSample = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, p * 0.38 + n.xz * 0.4 + float2(t * 0.03, -t * 0.02));
                float laceNoise = (foamSample.r > 0.01 && foamSample.r < 0.99)
                    ? (foamSample.r * 0.65 + foamSample.g * 0.35)
                    : (0.5 + 0.28 * sin(p.x * 4.2 + sin(p.y * 2.3) + t * 0.9) + 0.22 * cos(p.y * 5.6 - p.x * 2.1 - t * 0.7));

                // 第一重：紧贴岸岩与沙滩的交界白沫边
                float contactFoam = (1.0 - smoothstep(0.01, 0.12, depthM)) * smoothstep(0.32, 0.78, laceNoise);
                // 第二重：向岸涌动的律动泡沫环 (还原原图礁石周边的双层浪花)
                float surgePhase = frac(depthM / max(0.15, _FoamWidth) - t * 0.28 + laceNoise * 0.25);
                float surgeRing = sin(surgePhase * 3.14159) * (1.0 - smoothstep(0.05, _FoamWidth, depthM));
                surgeRing = smoothstep(0.52, 0.88, surgeRing * laceNoise);

                float shoreFoam = saturate(contactFoam * 0.95 + surgeRing * 0.65) * smoothstep(0.002, 0.015, depthM);

                // 小船船首破浪 + 船尾 V 型开尔文扩散白沫尾流 (Kelvin Wake)
                float2 boatDelta = p - _CoastBoat.xy;
                float2 heading = normalize(_CoastBoatHeading.xy + float2(0.00001, 0.0));
                float along = dot(boatDelta, heading);
                float side = dot(boatDelta, float2(heading.y, -heading.x));
                float boatSpeed = saturate(_CoastBoat.z / 3.0) * _CoastBoat.w;

                // 船首弧形激波
                float bowWave = exp(-pow((along - (2.05 - abs(side) * 0.42)) / 0.15, 2.0)) * (1.0 - smoothstep(0.65, 1.15, abs(side))) * boatSpeed;
                // 船尾 V 型双翼扩散波与中央螺旋桨湍流白沫带
                float sternDist = max(0.0, -along - 1.2);
                float wakeHalfWidth = 0.28 + sternDist * 0.26;
                float wakeFade = exp(-sternDist * 0.16) * step(along, -1.0) * (0.25 + 0.75 * boatSpeed);
                float vArm = exp(-pow((abs(side) - wakeHalfWidth) / (0.14 + sternDist * 0.04), 2.0)) * wakeFade;
                float propWash = exp(-pow(side / (0.22 + sternDist * 0.05), 2.0)) * exp(-sternDist * 0.24) * step(along, -1.1) * boatSpeed;
                float wakeFoam = saturate((vArm * 0.75 + propWash * 0.95) * (0.45 + 0.55 * laceNoise));

                float combinedFoam = saturate((shoreFoam + bowWave * 0.85 + wakeFoam) * _FoamIntensity);
                half3 foamColor = lerp(half3(0.76, 0.92, 0.94), half3(0.98, 0.99, 1.0), combinedFoam);
                finalColor = lerp(finalColor, foamColor, combinedFoam * 0.82);

                if (_OceanV24Layer > 0.5 && _OceanV24Layer < 1.5) finalColor = surfaceReflection;
                if (_OceanV24Layer > 1.5) finalColor = half3(clarity, clarity, clarity);

                return half4(max(finalColor, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}
