Shader "Deepsea/SeaSurfaceSparkleV26"
{
    Properties
    {
        [Header(Five Zone Depth Hierarchy)]
        _CrystalShoreColor("Zone 1: Crystal Shore Tint (<0.6m)", Color) = (0.82, 0.94, 0.80, 1)
        _Shallow("Zone 2: Emerald Lagoon (0.6m - 2.8m)", Color) = (0.06, 0.72, 0.64, 1)
        _ReefMidColor("Zone 3: Turquoise Reef Shelf (2.8m - 6.5m)", Color) = (0.02, 0.42, 0.54, 1)
        _Deep("Zone 4: Deep Trench Indigo (>6.5m)", Color) = (0.006, 0.10, 0.22, 1)
        _DepthRampMap("Optional 5-Zone Depth Ramp LUT", 2D) = "white" {}
        _UseDepthRamp("Use Baked Depth Ramp", Float) = 0

        [Header(Waves and Refraction)]
        _WaveMap("Fine Wave Normal Slopes", 2D) = "gray" {}
        _Refraction("Refraction Distortion", Range(0, 0.015)) = 0.0038
        _Visibility("Underwater Clarity", Range(0, 1)) = 0.90
        _SurfaceWeight("Surface Reflection Weight", Range(0, 1)) = 0.36

        [Header(Underwater Voronoi Caustics Web)]
        _CausticsMap("Voronoi Caustics & Sparkle (RGB)", 2D) = "black" {}
        _CausticsIntensity("Caustics Brightness on Sand/Reef", Range(0, 3.5)) = 1.68
        _CausticsScale("Caustics World Tiling", Range(0.05, 1.0)) = 0.22
        _CausticsSpeed("Caustics Flow Speed", Range(0, 1.5)) = 0.34
        _CausticsMaxDepth("Caustics Depth Fade (m)", Range(1, 15)) = 7.5

        [Header(Sun Sparkle and Shimmering Glints)]
        _GlintEnable("Enable Sun Sparkle", Range(0, 1)) = 1
        _GlintIntensity("Sparkle Peak Intensity", Range(0, 6)) = 3.45
        _GlintScale("Sparkle Cell Scale", Range(3, 30)) = 13.5
        _GlintSpeed("Sparkle Shimmer Speed", Range(0, 3)) = 0.95
        _SunPathColor("Warm Sun Glint Tint", Color) = (1.0, 0.95, 0.76, 1)

        [Header(Shoreline Surge Foam and Boat Wake)]
        _FoamMap("Shore Foam Cellular Noise", 2D) = "white" {}
        _FoamIntensity("Shore & Wake Foam Brightness", Range(0, 2.5)) = 1.28
        _FoamWidth("Shore Surge Band Width (m)", Range(0.05, 1.5)) = 0.62
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
                float _UseDepthRamp;
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

            // 兼容仓库 v22 / v24 全局变量与 OceanV24Waves 船只/浮漂高度场
            float _WaterV22Time;
            #include "OceanV24Waves.hlsl"
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

            float3 EvaluateBayWaves(float2 p, float t, out float3 outNormal, out float outCrest)
            {
                // 与现有船只和钓鱼浮漂共享同一物理高度场，保证 100% 同步
                float3 dx, dz;
                float3 displacement = OceanV24Sample(p, t, dx, dz);
                outNormal = normalize(cross(dz, dx));
                outCrest = saturate(0.5 + displacement.y / 0.52);
                return displacement;
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

                // -------------------------------------------------------------
                // 1. 三尺度动态波纹法线场（宏观涌浪 + 中频交叉交织波 + 高频微涟漪）
                //    适度提升法线斜率权重 (0.115 / 0.082)，让水面微波起伏与运动感清晰可读
                // -------------------------------------------------------------
                half2 rippleA = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.088 + float2(-t * 0.022, t * 0.015)).rg * 2.0 - 1.0;
                half2 rippleB = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.175 + float2(t * 0.017, -t * 0.024)).rg * 2.0 - 1.0;
                half2 rippleC = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.340 + float2(-t * 0.031, -t * 0.019)).rg * 2.0 - 1.0;

                float2 combinedSlope = (rippleA * 0.115 + rippleB * 0.082 + rippleC * 0.045) * detailScale;
                float3 n = normalize(i.waveNormal + float3(combinedSlope.x, 0.0, combinedSlope.y));
                float3 v = GetWorldSpaceNormalizeViewDir(i.worldPos);
                float ndotv = saturate(dot(n, v));

                // -------------------------------------------------------------
                // 2. 真实水下折射采样与岸线防溢色保护
                // -------------------------------------------------------------
                float2 refractOffset = n.xz * _Refraction * smoothstep(0.04, 0.75, waterDepth);
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

                // -------------------------------------------------------------
                // 3. 浅滩高锐度双层交错 Voronoi 焦散亮网 (还原原图清晰锐利的亮金/青白水底光网)
                // -------------------------------------------------------------
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(refractedBottomWS));
                // 按主光源方向做水底投射视差偏移，叠加水面法线折射扭曲
                float2 sunProjOffset = mainLight.direction.xz * (depthM * 0.18);
                float2 causticsUV = (refractedBottomWS.xz - sunProjOffset) * _CausticsScale + n.xz * 0.26;
                float2 flow1 = causticsUV + float2(t * _CausticsSpeed * 0.16, t * _CausticsSpeed * 0.11);
                float2 flow2 = causticsUV * 1.24 + float2(-t * _CausticsSpeed * 0.14, t * _CausticsSpeed * 0.15) + float2(0.37, 0.81);

                half4 causticsTex1 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, flow1);
                half4 causticsTex2 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, flow2);

                // 使用高次幂提取锐利晶格亮丝，加上双层交汇亮点，杜绝雾蒙蒙的平涂感
                float ridge1 = pow(saturate(causticsTex1.r), 1.55);
                float ridge2 = pow(saturate(causticsTex2.g), 1.55);
                float sharpWeb = (ridge1 + ridge2) * 0.62 + pow(saturate(ridge1 * ridge2 * 2.1), 1.35) * 1.25;

                // 焦散在 0.08m ~ 7.5m 浅水与中浅礁盘区清晰投射
                float causticsDepthMask = smoothstep(0.02, 0.28, depthM) * exp(-depthM / max(1.5, _CausticsMaxDepth));
                float causticsShadow = lerp(0.32, 1.0, mainLight.shadowAttenuation);
                half3 causticsTint = lerp(half3(0.98, 0.96, 0.78), half3(0.48, 0.98, 0.92), saturate(depthM / 3.0));
                half3 causticsLight = causticsTint * mainLight.color * (sharpWeb * _CausticsIntensity * causticsDepthMask * causticsShadow);

                // 将锐利焦散光网叠加到水底沙地、木桩与浅礁上
                seabedScene += causticsLight * (0.55 + 0.65 * seabedScene);

                // -------------------------------------------------------------
                // 4. 五级水深光学层次 + 水下珊瑚礁高透彩窗口 (Coral Color Preservation Window)
                // -------------------------------------------------------------
                float z1 = saturate(depthM / 0.90);                    // 0.0 ~ 0.9m: 极浅清透暖沙岸滩
                float z2 = smoothstep(0.60, 3.10, depthM);             // 0.6 ~ 3.1m: 高明度翡翠薄荷绿内湾
                float z3 = smoothstep(2.50, 7.20, depthM);             // 2.5 ~ 7.2m: 孔雀青蓝礁盘边缘
                float z4 = 1.0 - exp(-max(0.0, depthM - 5.2) * 0.26);  // >5.2m:      右侧深海鱼群海沟

                half3 waterBodyColor = lerp(_CrystalShoreColor.rgb, _Shallow.rgb, z1);
                waterBodyColor = lerp(waterBodyColor, _ReefMidColor.rgb, z2);
                waterBodyColor = lerp(waterBodyColor, _Deep.rgb, saturate(z3 * 0.74 + z4 * 0.58));

                if (_UseDepthRamp > 0.5)
                {
                    half3 rampCol = SAMPLE_TEXTURE2D(_DepthRampMap, sampler_DepthRampMap, float2(saturate(depthM / 11.5), 0.5)).rgb;
                    waterBodyColor = lerp(waterBodyColor, rampCol, 0.45);
                }

                // 关键优化：在 0.5m ~ 4.8m 的浅礁带大幅降低红/橙/紫光吸收率 (0.38 vs 旧版 0.78)，
                // 让水下粉紫、橙黄、翠绿珊瑚礁与鱼群的鲜明色彩像原图一样清澈透出！
                float opticalPath = min(depthM / max(0.45, abs(v.y)), 45.0);
                float3 attenCoeff = lerp(float3(0.32, 0.10, 0.07), float3(0.82, 0.18, 0.11), smoothstep(1.5, 6.5, depthM));
                half3 absorption = exp(-opticalPath * attenCoeff);
                float clarity = _Visibility * exp(-depthM * 0.115) * lerp(0.58, 0.98, smoothstep(0.25, 0.82, ndotv));

                // 波峰次表面散射 (Subsurface Scattering): 阳光穿透浪峰泛出通透翡翠光
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                float backSun = saturate(dot(v, -normalize(sun.direction + float3(0, 0.35, 0))));
                float crestSSS = pow(i.waveCrest, 1.9) * (0.42 + 0.58 * backSun) * exp(-depthM * 0.16);
                waterBodyColor += half3(0.08, 0.38, 0.32) * crestSSS * sun.shadowAttenuation;

                half3 underwaterComposite = lerp(waterBodyColor, seabedScene * absorption + waterBodyColor * (1.0 - absorption), saturate(clarity));

                // -------------------------------------------------------------
                // 5. 水面天光菲涅尔反射与涌浪条带运动感
                // -------------------------------------------------------------
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndotv, 4.4);
                float3 reflDir = reflect(-v, n);
                half3 envRefl = GlossyEnvironmentReflection(reflDir, i.worldPos, 0.08, 1.0);
                half3 skyGrad = lerp(half3(0.07, 0.28, 0.36), half3(0.38, 0.60, 0.68), saturate(reflDir.y * 0.85 + 0.15));
                half3 surfaceReflection = max(envRefl * 0.60, skyGrad * 0.54);

                // 涌浪受光面与背光面明暗韵律（增强海面波浪向前推进的运动体积感）
                float waveShading = saturate(dot(n.xz, normalize(float2(-0.65, -0.75))) * 0.45 + 0.52);
                half3 finalColor = lerp(underwaterComposite, surfaceReflection, saturate(fresnel * _SurfaceWeight + 0.04));
                finalColor *= lerp(0.88, 1.12, waveShading);

                // -------------------------------------------------------------
                // 6. 波光粼粼动态微面元太阳碎光 (Multi-Scale Organic Sun Sparkle)
                //    采用无规则 Voronoi 微面元双层交汇 + 三级太阳光锥包络，既无规则网格伪影，又璀璨夺目！
                // -------------------------------------------------------------
                float3 halfVec = normalize(sun.direction + v);
                float ndoth = saturate(dot(n, halfVec));

                // 第一级：柔和太阳主光道
                float broadSpec = pow(ndoth, 42.0) * 0.28;

                // 第二级：大范围波峰鳞光闪烁 (复刻原图右上角外海与小船周边的波光粼粼)
                float2 glintUV1 = p * (_GlintScale * 0.095) + n.xz * 1.15 + float2(t * _GlintSpeed * 0.08, -t * _GlintSpeed * 0.09);
                float2 glintUV2 = p * (_GlintScale * 0.145) - n.xz * 0.90 + float2(-t * _GlintSpeed * 0.07, t * _GlintSpeed * 0.11);
                float maskA = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1).b;
                float maskB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV2).b;
                float maskC = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1 * 0.53 + float2(0.41, 0.19)).r;

                float facet = maskA * maskB * (0.55 + 0.45 * maskC);
                float fWidth = max(fwidth(facet), 0.015);
                // 降低触发阈值 (0.14~0.28)，让闪烁碎金点饱满丰富
                float sparkleMid = smoothstep(0.14 - fWidth, 0.28 + fWidth, facet);
                float sparkleCore = smoothstep(0.26 - fWidth, 0.42 + fWidth, facet);

                // 放宽太阳反射圆锥指数 (pow 16 + pow 48)，让画面中上部的海浪斜坡都能沐浴在波光中
                float wideSunCone = pow(ndoth, 16.0) * (0.35 + 0.65 * i.waveCrest);
                float coreSunCone = pow(ndoth, 48.0) * (0.40 + 0.60 * i.waveCrest);

                float totalSparkle = (
                    broadSpec +
                    (sparkleMid * wideSunCone * 0.55 + sparkleCore * coreSunCone * 1.15) * _GlintIntensity * _GlintEnable
                ) * sun.shadowAttenuation * detailScale;

                finalColor += lerp(sun.color, _SunPathColor.rgb, 0.62) * totalSparkle;

                // -------------------------------------------------------------
                // 7. 岸礁/栈桥多层涌浪白沫环与小船 V 型扩散尾流
                // -------------------------------------------------------------
                half4 foamSample = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, p * 0.36 + n.xz * 0.45 + float2(t * 0.04, -t * 0.03));
                float laceNoise = (foamSample.r > 0.01 && foamSample.r < 0.99)
                    ? (foamSample.r * 0.68 + foamSample.g * 0.32)
                    : (0.5 + 0.28 * sin(p.x * 4.2 + sin(p.y * 2.3) + t * 0.9) + 0.22 * cos(p.y * 5.6 - p.x * 2.1 - t * 1.3));

                // 第一重：紧贴岸岩、栈桥木桩与沙滩的高亮撞击白沫边
                float contactFoam = (1.0 - smoothstep(0.01, 0.18, depthM)) * smoothstep(0.24, 0.68, laceNoise);
                // 第二重：向岸律动推进的双层破碎泡沫环（还原原图礁石周边的层叠白浪）
                float surgePhase = frac(depthM / max(0.18, _FoamWidth) - t * 0.32 + laceNoise * 0.28);
                float surgeRing = sin(surgePhase * 3.14159) * (1.0 - smoothstep(0.06, _FoamWidth, depthM));
                surgeRing = smoothstep(0.38, 0.76, surgeRing * laceNoise);

                float shoreFoam = saturate(contactFoam * 1.05 + surgeRing * 0.80) * smoothstep(0.002, 0.014, depthM);

                // 小船船首破浪 + 船尾 V 型开尔文扩散白沫尾流 (即使小船静止轻晃也保留柔和环浪，航行时拉出长尾流)
                float2 boatDelta = p - _CoastBoat.xy;
                float2 heading = normalize(_CoastBoatHeading.xy + float2(0.00001, 0.0));
                float along = dot(boatDelta, heading);
                float side = dot(boatDelta, float2(heading.y, -heading.x));
                float activeSpeed = max(0.32, saturate(_CoastBoat.z / 2.8)) * max(0.45, _CoastBoat.w);

                // 船首弧形激波与船身水线白沫环
                float hullRing = sqrt(pow(along / 2.25, 2.0) + pow(side / 0.98, 2.0));
                float hullFoam = exp(-pow((hullRing - 1.0) / 0.12, 2.0)) * (0.45 + 0.55 * laceNoise) * 0.65;
                float bowWave = exp(-pow((along - (2.05 - abs(side) * 0.42)) / 0.16, 2.0)) * (1.0 - smoothstep(0.65, 1.85, abs(side))) * activeSpeed;

                // 船尾 V 型双翼扩散波与中央螺旋桨湍流白沫带
                float sternDist = max(0.0, -along - 1.05);
                float wakeHalfWidth = 0.26 + sternDist * 0.28;
                float wakeFade = exp(-sternDist * 0.14) * step(along, -0.95) * activeSpeed;
                float vArm = exp(-pow((abs(side) - wakeHalfWidth) / (0.16 + sternDist * 0.045), 2.0)) * wakeFade;
                float propWash = exp(-pow(side / (0.24 + sternDist * 0.055), 2.0)) * exp(-sternDist * 0.20) * step(along, -0.95) * activeSpeed;
                float wakeFoam = saturate((vArm * 0.85 + propWash * 1.05 + hullFoam) * (0.40 + 0.60 * laceNoise));

                float combinedFoam = saturate((shoreFoam + bowWave * 0.90 + wakeFoam) * _FoamIntensity);
                half3 foamColor = lerp(half3(0.82, 0.95, 0.96), half3(1.0, 1.0, 1.0), combinedFoam);
                finalColor = lerp(finalColor, foamColor, saturate(combinedFoam * 0.88));

                if (_OceanV24Layer > 0.5 && _OceanV24Layer < 1.5) finalColor = surfaceReflection;
                if (_OceanV24Layer > 1.5) finalColor = half3(clarity, clarity, clarity);

                return half4(max(finalColor, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}
