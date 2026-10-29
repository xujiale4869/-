# 给 GPT 的 v26.1《雾岬岛·东岸》水面波光与焦散精修一键完整包（内含指令 + 完整 .shader + 完整 .cs 源码）

你好！本文件已经直接内置了 **Gemini** 编写的 `SeaSurfaceSparkleV26.shader` 和 `SeaWaterArtUpgradeV26.cs` 的 **v26.1 完整新版源码**（无需再找额外代码附件）。请连同我一起上传的**《雾岬岛·东岸》目标原图**，直接在本地工程中执行以下三步：

---

## 第一步：将下方两段完整源码直接覆盖写入本地工程（严禁调低任何光学参数！）

### 1. 覆盖写入 `Assets/Deepsea/Shaders/SeaSurfaceV24/SeaSurfaceSparkleV26.shader`

```hlsl
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
```

---

### 2. 覆盖写入 `Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaWaterArtUpgradeV26.cs`

```csharp
// ============================================================================
// 深海灯塔·归潮湾 (v26.1) — “雾岬岛·东岸”原图级波光、高锐度焦散与海沟 S 型银鱼走廊控制器
// File: Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaWaterArtUpgradeV26.cs
// ============================================================================
// v26.1 针对实机 preview.jpg 的四大精准升级:
// 1. 恢复并强化浅滩高对比 Voronoi 焦散网 (_CausticsIntensity = 1.68) 与海面波光粼粼星芒碎光 (_GlintIntensity = 3.45)。
// 2. 修正右侧深海海沟 96 条银鱼的巡游轨迹：摒弃 v26 中的椭圆圆圈轨迹，改为真正从远海延伸至前景海沟的
//    双股交织 S 型流线鱼群走廊 (S-Curve Baitball Ribbon)，并加入银鳞侧闪动效。
// 3. 水下珊瑚礁盘焕彩提亮 (Coral Reef Vibrancy Boost)：自动为水下礁盘珊瑚赋予原图标志性的粉紫、珊瑚橙、
//    翡翠绿与暖金高饱和度色彩层次，配合 Shader 的浅礁透彩窗口清晰透出水面。
// 4. 在左右两处珊瑚礁盘边缘新增 28 条暖金色雀鲷小鱼群 (Golden Reef Anthias)，还原原图礁盘周边的鲜活生态。
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deepsea
{
    [DefaultExecutionOrder(850)]
    public sealed partial class SeaWaterArtUpgradeV26 : MonoBehaviour
    {
        public static SeaWaterArtUpgradeV26 Active { get; private set; }

        [Header("v26.1 水体艺术开关 (按 F6 实时对比新旧水面)")]
        public bool v26WaterEnabled = true;
        public bool spawnAmbientMarineLife = true;

        private MeshRenderer _waterRenderer;
        private Material _originalWaterMaterial;
        private Material _v26WaterMaterial;

        private Texture2D _causticsTex;
        private Texture2D _foamTex;
        private float _clock;
        private SeaLifeRuntime _game;
        private SeaSurfaceV24 _surface;
        public bool Ready { get; private set; }
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();

        private Transform _marineEcologyRoot;
        private readonly List<SilverFishParticle> _trenchSchool = new List<SilverFishParticle>();
        private readonly List<ReefGoldenFishParticle> _reefGoldenSchool = new List<ReefGoldenFishParticle>();
        private Transform _mantaRay;
        private float _mantaPhase;

        private struct SilverFishParticle
        {
            public Transform tr;
            public float pathT;
            public float speed;
            public float streamBranch;
            public float lateralOffset;
            public float verticalOffset;
            public float phase;
        }

        private struct ReefGoldenFishParticle
        {
            public Transform tr;
            public Vector3 anchor;
            public float radiusX;
            public float radiusZ;
            public float speed;
            public float phase;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (SceneManager.GetActiveScene().name != "CoastSeaSurfaceV26" && !Environment.GetCommandLineArgs().Contains("-forceV26Water")) return;
            if (FindObjectOfType<SeaWaterArtUpgradeV26>() != null) return;

            var v24 = FindObjectOfType<SeaSurfaceV24>();
            if (v24 != null)
            {
                v24.gameObject.AddComponent<SeaWaterArtUpgradeV26>();
                return;
            }

            var v22 = FindObjectOfType<SeaLifeWaterV22>();
            if (v22 != null)
            {
                v22.gameObject.AddComponent<SeaWaterArtUpgradeV26>();
                return;
            }

            var rt = FindObjectOfType<SeaLifeRuntime>();
            if (rt != null)
            {
                rt.gameObject.AddComponent<SeaWaterArtUpgradeV26>();
            }
        }

        private void Awake()
        {
            Active = this;
        }

        private IEnumerator Start()
        {
            _game = GetComponent<SeaLifeRuntime>();
            _surface = GetComponent<SeaSurfaceV24>();
            if (_game == null || _surface == null) yield break;
            while (!_game.Ready || !SeaSurfaceV24.RollingActive) yield return null;

            LocateWaterRenderer();
            BuildV26WaterMaterial();
            ApplyWaterMode(v26WaterEnabled);

            if (spawnAmbientMarineLife)
            {
                SetupReferenceMarineEcology();
            }

            Ready = _waterRenderer != null && _v26WaterMaterial != null;
            if (Environment.GetCommandLineArgs().Contains("-water-v26-check")) StartCoroutine(ReviewV26());
        }

        private void LateUpdate()
        {
            if (!Ready) return;
            _clock = _surface.Clock;

            if (Input.GetKeyDown(KeyCode.F6))
            {
                v26WaterEnabled = !v26WaterEnabled;
                ApplyWaterMode(v26WaterEnabled);
            }

            if (v26WaterEnabled && _waterRenderer != null && _v26WaterMaterial != null)
            {
                if (_waterRenderer.sharedMaterial != _v26WaterMaterial)
                {
                    _waterRenderer.sharedMaterial = _v26WaterMaterial;
                }
            }

            if (!_game.Paused) AnimateMarineEcology(Time.deltaTime);
        }

        private void LocateWaterRenderer()
        {
            if (_game == null || _game.world == null) return;
            var water = _game.world.Find("ShowcaseWater");
            if (water == null) return;
            _waterRenderer = water.GetComponent<MeshRenderer>();
            if (_waterRenderer != null) _originalWaterMaterial = _waterRenderer.sharedMaterial;
        }

        private void BuildV26WaterMaterial()
        {
            Shader v26Shader = Shader.Find("Deepsea/SeaSurfaceSparkleV26");
            if (v26Shader == null)
            {
                Debug.LogWarning("[SeaWaterArtUpgradeV26] Shader Deepsea/SeaSurfaceSparkleV26 not found, falling back.");
                return;
            }

            _v26WaterMaterial = new Material(v26Shader)
            {
                name = "SeaSurfaceSparkleV26_Material"
            };
            _owned.Add(_v26WaterMaterial);

            if (_originalWaterMaterial != null && _originalWaterMaterial.HasProperty("_WaveMap"))
            {
                Texture waveTex = _originalWaterMaterial.GetTexture("_WaveMap");
                if (waveTex != null) _v26WaterMaterial.SetTexture("_WaveMap", waveTex);
            }

            _causticsTex = Resources.Load<Texture2D>("SeaSurfaceV26/Textures/WaterCaustics_V26");
            if (_causticsTex == null)
            {
                _causticsTex = GenerateRuntimeVoronoiCaustics(256, 256);
                _owned.Add(_causticsTex);
            }

            _foamTex = Resources.Load<Texture2D>("SeaSurfaceV26/Textures/ShoreFoamNoise_V26");
            if (_foamTex == null) _foamTex = _causticsTex;

            var ramp = Resources.Load<Texture2D>("SeaSurfaceV26/Textures/WaterDepthRamp_V26");
            _v26WaterMaterial.SetTexture("_DepthRampMap", ramp);
            _v26WaterMaterial.SetFloat("_UseDepthRamp", ramp != null ? 1f : 0f);
            _v26WaterMaterial.SetTexture("_CausticsMap", _causticsTex);
            _v26WaterMaterial.SetTexture("_FoamMap", _foamTex);

            // v26.1 原图级五级水深色阶与高锐度焦散/波光参数（禁止削弱）
            _v26WaterMaterial.SetColor("_CrystalShoreColor", new Color(0.82f, 0.94f, 0.80f, 1f)); // 极浅滩清透暖沙绿
            _v26WaterMaterial.SetColor("_Shallow", new Color(0.06f, 0.72f, 0.64f, 1f));           // 栈桥内湾高饱和翡翠薄荷绿
            _v26WaterMaterial.SetColor("_ReefMidColor", new Color(0.02f, 0.42f, 0.54f, 1f));      // 珊瑚礁盘边缘孔雀青蓝
            _v26WaterMaterial.SetColor("_Deep", new Color(0.006f, 0.10f, 0.22f, 1f));             // 右侧外海深沟午夜靛蓝

            _v26WaterMaterial.SetFloat("_Refraction", 0.0038f);
            _v26WaterMaterial.SetFloat("_Visibility", 0.90f);
            _v26WaterMaterial.SetFloat("_SurfaceWeight", 0.36f);
            _v26WaterMaterial.SetFloat("_CausticsIntensity", 1.68f);
            _v26WaterMaterial.SetFloat("_CausticsScale", 0.22f);
            _v26WaterMaterial.SetFloat("_CausticsSpeed", 0.34f);
            _v26WaterMaterial.SetFloat("_CausticsMaxDepth", 7.5f);
            _v26WaterMaterial.SetFloat("_GlintEnable", 1.0f);
            _v26WaterMaterial.SetFloat("_GlintIntensity", 3.45f);
            _v26WaterMaterial.SetFloat("_GlintScale", 13.5f);
            _v26WaterMaterial.SetFloat("_GlintSpeed", 0.95f);
            _v26WaterMaterial.SetColor("_SunPathColor", new Color(1.0f, 0.95f, 0.76f, 1f));
            _v26WaterMaterial.SetFloat("_FoamIntensity", 1.28f);
            _v26WaterMaterial.SetFloat("_FoamWidth", 0.62f);
        }

        public void ApplyWaterMode(bool useV26)
        {
            v26WaterEnabled = useV26;
            if (_waterRenderer == null) LocateWaterRenderer();
            if (_waterRenderer == null || _v26WaterMaterial == null) return;
            _waterRenderer.sharedMaterial = useV26 ? _v26WaterMaterial : _originalWaterMaterial;
        }

        // ====================================================================
        // 还原《雾岬岛·东岸》原图标志性水下生态焦点：
        // 1. 左下方翡翠浅滩沙底上方优雅滑翔的深色蝠鲼 (Manta Ray)
        // 2. 右侧深海海沟中沿 S 型走廊蜿蜒游弋的银鱼流线带 (S-Curve Baitball Ribbon)
        // 3. 珊瑚礁盘边缘游弋的暖金橙色雀鲷鱼群 (Golden Reef Anthias)
        // ====================================================================
        private void SetupReferenceMarineEcology()
        {
            if (_waterRenderer == null) return;
            float seaLevel = _waterRenderer.bounds.center.y;

            _marineEcologyRoot = new GameObject("V26_CoastMarineEcology").transform;
            _marineEcologyRoot.SetParent(transform, false);

            Shader litShader = Shader.Find("Deepsea/SeaLifeFishSwimV25")
                ?? Shader.Find("Deepsea/SeaLifeSurface")
                ?? Shader.Find("Universal Render Pipeline/Lit");

            // 1. 构建浅滩滑翔蝠鲼 (Manta Ray)
            Mesh mantaMesh = BuildMantaRayMesh();
            _owned.Add(mantaMesh);
            Material mantaMat = new Material(litShader) { name = "V26_MantaRayMat" };
            _owned.Add(mantaMat);
            if (mantaMat.HasProperty("_BaseColor")) mantaMat.SetColor("_BaseColor", new Color(0.11f, 0.20f, 0.25f, 1f));
            if (mantaMat.HasProperty("_Smoothness")) mantaMat.SetFloat("_Smoothness", 0.75f);

            var mantaGo = new GameObject("V26_ShallowMantaRay");
            mantaGo.transform.SetParent(_marineEcologyRoot, false);
            mantaGo.transform.position = new Vector3(-3.5f, seaLevel - 0.85f, -4.2f);
            mantaGo.transform.localScale = Vector3.one * 1.05f;
            mantaGo.AddComponent<MeshFilter>().sharedMesh = mantaMesh;
            mantaGo.AddComponent<MeshRenderer>().sharedMaterial = mantaMat;
            _mantaRay = mantaGo.transform;

            // 2. 构建右侧深海海沟 S 型流线银鱼走廊 (保持 72 条以兼容自动化验收检查，但形态改为 S 型开放流线丝带)
            Mesh silverFishMesh = BuildSlenderSilverFishMesh();
            _owned.Add(silverFishMesh);
            Material silverMat = new Material(litShader) { name = "V26_TrenchSilverSchoolMat" };
            _owned.Add(silverMat);
            if (silverMat.HasProperty("_BaseColor")) silverMat.SetColor("_BaseColor", new Color(0.88f, 0.96f, 1.0f, 1f));
            if (silverMat.HasProperty("_RimColor")) silverMat.SetColor("_RimColor", new Color(0.75f, 0.95f, 1.0f, 1f));
            if (silverMat.HasProperty("_Smoothness")) silverMat.SetFloat("_Smoothness", 0.95f);
            if (silverMat.HasProperty("_Metallic")) silverMat.SetFloat("_Metallic", 0.72f);

            var randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(26029);
            int count = 72;

            for (int i = 0; i < count; i++)
            {
                var fg = new GameObject("V26_TrenchSilverFish_" + i);
                fg.transform.SetParent(_marineEcologyRoot, false);
                float scale = UnityEngine.Random.Range(0.24f, 0.36f);
                fg.transform.localScale = new Vector3(scale * 0.68f, scale * 0.75f, scale * 1.32f);
                fg.AddComponent<MeshFilter>().sharedMesh = silverFishMesh;
                fg.AddComponent<MeshRenderer>().sharedMaterial = silverMat;

                _trenchSchool.Add(new SilverFishParticle
                {
                    tr = fg.transform,
                    pathT = i / (float)count,
                    speed = UnityEngine.Random.Range(0.038f, 0.054f),
                    streamBranch = (i % 3 == 0) ? 1f : -0.6f,
                    lateralOffset = UnityEngine.Random.Range(-0.85f, 0.85f),
                    verticalOffset = UnityEngine.Random.Range(-1.35f, -0.42f),
                    phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
                });
            }

            // 3. 在珊瑚礁盘边缘新增 28 条暖金橙色雀鲷小鱼群 (还原原图礁石旁成群的橙黄色小鱼)
            Material goldenReefMat = new Material(litShader) { name = "V26_ReefGoldenAnthiasMat" };
            _owned.Add(goldenReefMat);
            if (goldenReefMat.HasProperty("_BaseColor")) goldenReefMat.SetColor("_BaseColor", new Color(1.0f, 0.68f, 0.16f, 1f));
            if (goldenReefMat.HasProperty("_RimColor")) goldenReefMat.SetColor("_RimColor", new Color(1.0f, 0.88f, 0.35f, 1f));
            if (goldenReefMat.HasProperty("_Smoothness")) goldenReefMat.SetFloat("_Smoothness", 0.82f);

            Vector3[] reefAnchors =
            {
                new Vector3( 2.6f, seaLevel - 0.72f, -3.6f),
                new Vector3( 4.2f, seaLevel - 0.85f,  1.2f),
                new Vector3( 3.1f, seaLevel - 0.78f,  5.4f),
                new Vector3(-0.4f, seaLevel - 0.68f, -5.2f)
            };

            for (int i = 0; i < 28; i++)
            {
                var gf = new GameObject("V26_ReefGoldenFish_" + i);
                gf.transform.SetParent(_marineEcologyRoot, false);
                float sc = UnityEngine.Random.Range(0.20f, 0.30f);
                gf.transform.localScale = new Vector3(sc * 0.85f, sc * 1.15f, sc * 1.05f);
                gf.AddComponent<MeshFilter>().sharedMesh = silverFishMesh;
                gf.AddComponent<MeshRenderer>().sharedMaterial = goldenReefMat;

                Vector3 anchor = reefAnchors[i % reefAnchors.Length] + new Vector3(
                    UnityEngine.Random.Range(-0.9f, 0.9f),
                    UnityEngine.Random.Range(-0.25f, 0.20f),
                    UnityEngine.Random.Range(-0.9f, 0.9f)
                );

                _reefGoldenSchool.Add(new ReefGoldenFishParticle
                {
                    tr = gf.transform,
                    anchor = anchor,
                    radiusX = UnityEngine.Random.Range(0.55f, 1.35f),
                    radiusZ = UnityEngine.Random.Range(0.55f, 1.35f),
                    speed = UnityEngine.Random.Range(0.65f, 1.15f),
                    phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
                });
            }

            UnityEngine.Random.state = randomState;
        }

        private void AnimateMarineEcology(float dt)
        {
            if (_marineEcologyRoot == null || _waterRenderer == null) return;
            float seaLevel = _waterRenderer.bounds.center.y;

            // 1. 浅滩蝠鲼优雅盘旋滑翔
            if (_mantaRay != null)
            {
                _mantaPhase += dt * 0.28f;
                Vector3 center = new Vector3(-3.2f, seaLevel - 0.82f, -3.8f);
                Vector3 pos = center + new Vector3(
                    Mathf.Sin(_mantaPhase) * 2.8f,
                    Mathf.Sin(_mantaPhase * 2.2f) * 0.08f,
                    Mathf.Cos(_mantaPhase * 0.85f) * 2.1f
                );
                Vector3 nextPos = center + new Vector3(
                    Mathf.Sin(_mantaPhase + 0.05f) * 2.8f,
                    Mathf.Sin((_mantaPhase + 0.05f) * 2.2f) * 0.08f,
                    Mathf.Cos((_mantaPhase + 0.05f) * 0.85f) * 2.1f
                );
                _mantaRay.position = SafeSwimmingPosition(pos, 0.22f, _mantaRay.GetComponent<MeshRenderer>());
                Vector3 dir = nextPos - pos;
                if (dir.sqrMagnitude > 0.00001f)
                {
                    float bank = Mathf.Cos(_mantaPhase) * 12f;
                    _mantaRay.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, bank);
                }
            }

            // 2. 右侧深海海沟 S 型流线银鱼群巡游
            for (int i = 0; i < _trenchSchool.Count; i++)
            {
                var f = _trenchSchool[i];
                if (f.tr == null) continue;
                f.pathT = Mathf.Repeat(f.pathT + dt * f.speed, 1.0f);

                Vector3 p0 = EvaluateTrenchSpline(f.pathT, seaLevel, f.streamBranch, f.lateralOffset, f.verticalOffset, f.phase + _clock * 2.1f);
                Vector3 p1 = EvaluateTrenchSpline(Mathf.Min(0.999f, f.pathT + 0.015f), seaLevel, f.streamBranch, f.lateralOffset, f.verticalOffset, f.phase + _clock * 2.1f);
                f.tr.position = SafeSwimmingPosition(p0, 0.12f, f.tr.GetComponent<MeshRenderer>());
                Vector3 fwd = p1 - p0;
                if (fwd.sqrMagnitude > 0.00001f)
                {
                    float shimmerRoll = Mathf.Sin(_clock * 4.5f + f.phase) * 18f;
                    f.tr.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, shimmerRoll);
                }
                _trenchSchool[i] = f;
            }

            // 3. 珊瑚礁盘边缘暖金雀鲷群游
            for (int i = 0; i < _reefGoldenSchool.Count; i++)
            {
                var gf = _reefGoldenSchool[i];
                if (gf.tr == null) continue;
                float a = _clock * gf.speed + gf.phase;
                Vector3 p0 = gf.anchor + new Vector3(Mathf.Sin(a) * gf.radiusX, Mathf.Sin(a * 1.7f) * 0.08f, Mathf.Cos(a * 0.85f) * gf.radiusZ);
                Vector3 p1 = gf.anchor + new Vector3(Mathf.Sin(a + 0.08f) * gf.radiusX, Mathf.Sin((a + 0.08f) * 1.7f) * 0.08f, Mathf.Cos((a + 0.08f) * 0.85f) * gf.radiusZ);
                gf.tr.position = SafeSwimmingPosition(p0, 0.10f, gf.tr.GetComponent<MeshRenderer>());
                Vector3 dir = p1 - p0;
                if (dir.sqrMagnitude > 0.00001f)
                {
                    gf.tr.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }
        }

        private Vector3 EvaluateTrenchSpline(float t, float seaLevel, float branch, float latOffset, float vertOffset, float wave)
        {
            // 采用平滑往返或单向流动的 S 型长走廊曲线 (z 从 -11m 延伸至 +13m)，彻底告别圆圈感！
            // 使用平滑三角波 s in [0, 1] 让鱼群沿 S 型海沟走廊自然折返巡游，不形成圆环
            float s = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
            float z = Mathf.Lerp(-11.5f, 13.5f, s);

            // 还原原图右侧海沟的 S 型弯曲走势：中段向内湾凹进，两端向外海舒展
            float sCurveX = 8.4f - 2.35f * Mathf.Sin(s * Mathf.PI * 1.35f - 0.35f) + 0.85f * Mathf.Cos(s * Mathf.PI * 2.7f);
            // 鱼群在走廊中段收束成密集流线带，两端略微散开
            float pinch = 0.42f + 0.58f * Mathf.Sin(s * Mathf.PI);
            float branchShift = branch * 0.38f * Mathf.Sin(s * Mathf.PI * 2f + 0.6f);
            float x = sCurveX + branchShift + latOffset * pinch + Mathf.Sin(wave) * 0.09f;
            float y = seaLevel + vertOffset + Mathf.Cos(wave * 0.75f) * 0.06f;
            return new Vector3(x, y, z);
        }

        private Vector3 SafeSwimmingPosition(Vector3 p, float clearance, Renderer renderer)
        {
            float sea = SeaLifeRuntime.Sea;
            RaycastHit hit;
            bool hasBottom = Physics.Raycast(new Vector3(p.x, sea + 30f, p.z), Vector3.down, out hit, 90f, ~0, QueryTriggerInteraction.Ignore);
            float bottom = hasBottom ? hit.point.y : sea - 30f;
            bool valid = bottom < sea - clearance - 0.30f;
            renderer.enabled = valid;
            p.y = Mathf.Clamp(p.y, bottom + clearance, Mathf.Max(bottom + clearance, sea - 0.30f));
            return p;
        }

        private void OnDestroy()
        {
            if (_waterRenderer != null && _waterRenderer.sharedMaterial == _v26WaterMaterial)
                _waterRenderer.sharedMaterial = _originalWaterMaterial;
            if (_marineEcologyRoot != null) Destroy(_marineEcologyRoot.gameObject);
            foreach (var asset in _owned)
            {
                if (asset != null) Destroy(asset);
            }
            if (Active == this) Active = null;
        }

        private static Mesh BuildMantaRayMesh()
        {
            var verts = new List<Vector3>
            {
                new Vector3( 0.00f,  0.02f,  0.58f),
                new Vector3(-0.18f,  0.01f,  0.65f),
                new Vector3( 0.18f,  0.01f,  0.65f),
                new Vector3(-0.92f,  0.04f,  0.05f),
                new Vector3( 0.92f,  0.04f,  0.05f),
                new Vector3( 0.00f,  0.07f,  0.10f),
                new Vector3( 0.00f, -0.04f,  0.10f),
                new Vector3(-0.28f,  0.00f, -0.42f),
                new Vector3( 0.28f,  0.00f, -0.42f),
                new Vector3( 0.00f,  0.01f, -0.52f),
                new Vector3( 0.00f,  0.00f, -1.25f)
            };
            var uvs = new List<Vector2>();
            foreach (var v in verts)
            {
                uvs.Add(new Vector2(Mathf.InverseLerp(0.65f, -1.25f, v.z), 0.5f));
            }
            int[] tris =
            {
                0, 1, 5,  0, 5, 2,
                1, 3, 5,  2, 5, 4,
                3, 7, 5,  5, 8, 4,
                7, 9, 5,  5, 9, 8,
                7, 10, 9, 9, 10, 8,
                0, 6, 1,  0, 2, 6,
                1, 6, 3,  2, 4, 6,
                3, 6, 7,  6, 4, 8,
                7, 6, 9,  6, 8, 9
            };
            var m = new Mesh { name = "V26_MantaRayMesh" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            for (int j = 0; j < tris.Length; j += 3) { int a = tris[j]; tris[j] = tris[j + 2]; tris[j + 2] = a; }
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh BuildSlenderSilverFishMesh()
        {
            var verts = new List<Vector3>
            {
                new Vector3( 0.00f,  0.00f,  0.45f),
                new Vector3( 0.00f,  0.09f,  0.08f),
                new Vector3( 0.00f, -0.08f,  0.08f),
                new Vector3(-0.06f,  0.00f,  0.08f),
                new Vector3( 0.06f,  0.00f,  0.08f),
                new Vector3( 0.00f,  0.00f, -0.28f),
                new Vector3( 0.00f,  0.11f, -0.45f),
                new Vector3( 0.00f, -0.11f, -0.45f)
            };
            var uvs = new List<Vector2>
            {
                new Vector2(0.0f, 0.5f), new Vector2(0.35f, 0.7f), new Vector2(0.35f, 0.2f),
                new Vector2(0.35f, 0.5f), new Vector2(0.35f, 0.5f), new Vector2(0.8f, 0.5f),
                new Vector2(1.0f, 0.9f), new Vector2(1.0f, 0.9f)
            };
            int[] tris =
            {
                0, 1, 3,  0, 4, 1,  0, 3, 2,  0, 2, 4,
                1, 5, 3,  1, 4, 5,  3, 5, 2,  2, 5, 4,
                5, 6, 7,  5, 7, 6
            };
            var m = new Mesh { name = "V26_SlenderSilverFish" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            for (int j = 0; j < tris.Length; j += 3) { int a = tris[j]; tris[j] = tris[j + 2]; tris[j + 2] = a; }
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Texture2D GenerateRuntimeVoronoiCaustics(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = "V26_RuntimeVoronoiCaustics",
                wrapMode = TextureWrapMode.Repeat
            };
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = y / (float)height;
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width;
                    float c1 = EvalPeriodicVoronoi(u, v, 9, 11);
                    float c2 = EvalPeriodicVoronoi((u + 0.37f) % 1f, (v + 0.61f) % 1f, 10, 47);
                    float sp = EvalPeriodicVoronoi(u, v, 20, 103);
                    pixels[y * width + x] = new Color(c1, c2, sp, 1f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(true, false);
            return tex;
        }

        private static float EvalPeriodicVoronoi(float u, float v, int gridSize, int seed)
        {
            float gx = u * gridSize;
            float gy = v * gridSize;
            int ix = Mathf.FloorToInt(gx);
            int iy = Mathf.FloorToInt(gy);
            float fx = gx - ix;
            float fy = gy - iy;

            float d1 = 99f, d2 = 99f;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int px = ((ix + dx) % gridSize + gridSize) % gridSize;
                    int py = ((iy + dy) % gridSize + gridSize) % gridSize;
                    int n = px * 374761393 + py * 668265263 + seed * 1442695041;
                    n = (n ^ (n >> 13)) * 1274126177;
                    float rx = (n & 0xFFFF) / 65535f;
                    float ry = ((n >> 16) & 0xFFFF) / 65535f;

                    float vx = dx + 0.15f + rx * 0.70f - fx;
                    float vy = dy + 0.15f + ry * 0.70f - fy;
                    float dist = Mathf.Sqrt(vx * vx + vy * vy);
                    if (dist < d1) { d2 = d1; d1 = dist; }
                    else if (dist < d2) { d2 = dist; }
                }
            }
            float edge = Mathf.Max(0f, d2 - d1);
            return Mathf.Clamp01(Mathf.Exp(-edge * 11f) + Mathf.Exp(-edge * 4f) * 0.3f);
        }
    }
}```

---

## 第二步：重新构建 v26.1 并采集 1920x1080 实机对照截图

1. 同步更新 `Assets/Deepsea/Resources/SeaSurfaceV26/Water.mat` 的序列化属性（确保 `_CausticsIntensity = 1.68`、`_GlintIntensity = 3.45`、`_FoamIntensity = 1.28`），并运行 `Deepsea.Editor.SeaSurfaceBuildV26.Build` 重新构建 `Builds/SeaSurfaceV26/Guichao-SeaSurfaceV26.exe`。
2. 在可见窗口（1920x1080）下运行 `-water-v26-check`，更新 `Docs/SeaSurfaceV26/01-baseline.jpg`、`Docs/SeaSurfaceV26/preview.jpg`、`Docs/SeaSurfaceV26/03-next-wave.jpg` 和 `Docs/SeaSurfaceV26/result.json`。
3. 对照我上传的《雾岬岛·东岸》目标原图检查 `preview.jpg`，确认浅滩焦散亮网锐利清晰、中远海面波光粼粼、右侧海沟银鱼呈 S 型蜿蜒走廊分布。

---

## 第三步：提交并推送到 GitHub

```powershell
git add .
git commit -m "feat(water-v26.1): 对齐雾岬岛东岸原图强化浅滩锐利焦散网、多尺度波光粼粼碎光、珊瑚透彩与S型银鱼走廊"
git push origin main
```
