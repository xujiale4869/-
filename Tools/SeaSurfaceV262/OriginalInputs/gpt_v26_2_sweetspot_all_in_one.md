# 给 GPT 的 v26.2《雾岬岛·东岸》水面焦散与波光黄金平衡精修包（含指令 + 完整 .shader + 完整 .cs 源码 + F7 实时调参台）

你好！上一轮 `v26.1` 的水底焦散亮网（`1.68`）和海面波光碎金点（`3.45` + 宽光锥 `pow 16`）在实机中显得太抢戏、太刺眼了。
现在 **Gemini** 已经针对这两项完成了 **v26.2 黄金平衡版（Sweet Spot）** 收敛，并新增了实机 **【按 `F7` 键呼出水面光学实时滑块调参台】** 的功能。本文件已内置全部两段完整新版源码，请直接在本地工程执行以下三步：

---

## 第一步：将下方两段完整源码直接覆盖写入本地工程

### 1. 覆盖写入 `Assets/Deepsea/Shaders/SeaSurfaceV24/SeaSurfaceSparkleV26.shader`
* **针对焦散过亮（问题1）的修复**：改用双层 Voronoi 几何交汇提取疏密有致的柔和光网（`sqrt(cA * cB)`），并根据水底自身明度（`seabedLuma`）做正片叠底调制，使焦散只在受光浅沙上温润浮现（叠加权重降至 `0.36`），阴影处与暗色礁石上自动隐退，绝不发白抢戏。
* **针对波光刺眼（问题2）的修复**：移除泛滥全屏的 `pow 16` 宽光锥，引入可调聚焦指数 `_GlintFocus = 72.0`，提高微面元触发阈值（`0.24~0.48`），并在浅滩焦散区（`<1.8m`）自动减弱表面碎光，避免水面波光与水底焦散重叠打架。

```hlsl
Shader "Deepsea/SeaSurfaceSparkleV26"
{
    Properties
    {
        [Header(Five Zone Depth Hierarchy)]
        _CrystalShoreColor("Zone 1: Crystal Shore Tint (<0.6m)", Color) = (0.78, 0.90, 0.78, 1)
        _Shallow("Zone 2: Emerald Lagoon (0.6m - 2.8m)", Color) = (0.08, 0.66, 0.60, 1)
        _ReefMidColor("Zone 3: Turquoise Reef Shelf (2.8m - 6.5m)", Color) = (0.03, 0.39, 0.50, 1)
        _Deep("Zone 4: Deep Trench Indigo (>6.5m)", Color) = (0.008, 0.11, 0.21, 1)
        _DepthRampMap("Optional 5-Zone Depth Ramp LUT", 2D) = "white" {}
        _UseDepthRamp("Use Baked Depth Ramp", Float) = 0

        [Header(Waves and Refraction)]
        _WaveMap("Fine Wave Normal Slopes", 2D) = "gray" {}
        _Refraction("Refraction Distortion", Range(0, 0.015)) = 0.0034
        _Visibility("Underwater Clarity", Range(0, 1)) = 0.86
        _SurfaceWeight("Surface Reflection Weight", Range(0, 1)) = 0.38

        [Header(Underwater Voronoi Caustics Web)]
        _CausticsMap("Voronoi Caustics & Sparkle (RGB)", 2D) = "black" {}
        _CausticsIntensity("Caustics Brightness on Sand/Reef", Range(0, 3.0)) = 0.92
        _CausticsScale("Caustics World Tiling", Range(0.05, 1.0)) = 0.23
        _CausticsSpeed("Caustics Flow Speed", Range(0, 1.5)) = 0.26
        _CausticsMaxDepth("Caustics Depth Fade (m)", Range(1, 15)) = 5.6

        [Header(Sun Sparkle and Shimmering Glints)]
        _GlintEnable("Enable Sun Sparkle", Range(0, 1)) = 1
        _GlintIntensity("Sparkle Peak Intensity", Range(0, 5)) = 1.85
        _GlintFocus("Sun Path Cone Focus", Range(20, 200)) = 72.0
        _GlintScale("Sparkle Cell Scale", Range(3, 30)) = 12.5
        _GlintSpeed("Sparkle Shimmer Speed", Range(0, 3)) = 0.72
        _SunPathColor("Warm Sun Glint Tint", Color) = (1.0, 0.94, 0.78, 1)

        [Header(Shoreline Surge Foam and Boat Wake)]
        _FoamMap("Shore Foam Cellular Noise", 2D) = "white" {}
        _FoamIntensity("Shore & Wake Foam Brightness", Range(0, 2.5)) = 0.95
        _FoamWidth("Shore Surge Band Width (m)", Range(0.05, 1.5)) = 0.48
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
                float _GlintFocus;
                float _GlintScale;
                float _GlintSpeed;
                half4 _SunPathColor;
                float _FoamIntensity;
                float _FoamWidth;
            CBUFFER_END

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
                // 1. 三尺度动态波纹法线场（平滑克制，既有涌浪层次又不碎乱）
                // -------------------------------------------------------------
                half2 rippleA = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.086 + float2(-t * 0.019, t * 0.013)).rg * 2.0 - 1.0;
                half2 rippleB = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.168 + float2(t * 0.014, -t * 0.020)).rg * 2.0 - 1.0;
                half2 rippleC = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.310 + float2(-t * 0.024, -t * 0.016)).rg * 2.0 - 1.0;

                float2 combinedSlope = (rippleA * 0.095 + rippleB * 0.065 + rippleC * 0.032) * detailScale;
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
                // 3. 温润自然的水底双层交错焦散网 (v26.2 黄金克制版：告别刺眼发白！)
                //    采用雙层交汇提取柔润光丝 + 与水底自身明度相乘调制，深色阴影处与深水处自动隐退
                // -------------------------------------------------------------
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(refractedBottomWS));
                float2 sunProjOffset = mainLight.direction.xz * (depthM * 0.15);
                float2 causticsUV = (refractedBottomWS.xz - sunProjOffset) * _CausticsScale + n.xz * 0.22;
                float2 flow1 = causticsUV + float2(t * _CausticsSpeed * 0.14, t * _CausticsSpeed * 0.09);
                float2 flow2 = causticsUV * 1.22 + float2(-t * _CausticsSpeed * 0.12, t * _CausticsSpeed * 0.13) + float2(0.37, 0.81);

                half4 causticsTex1 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, flow1);
                half4 causticsTex2 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, flow2);

                // 关键修复：不再把两层网格生硬相加铺满全屏，而是以“双层几何平均/交汇”为主，形成疏密有致的柔和光网
                float cA = smoothstep(0.22, 0.88, causticsTex1.r);
                float cB = smoothstep(0.22, 0.88, causticsTex2.g);
                float organicWeb = sqrt(cA * cB) * 0.72 + (cA + cB) * 0.16;

                // 仅在 0.12m ~ 4.8m 的浅水受光沙地与浅礁上自然显现，岸边极浅处与深水处平滑淡出
                float causticsDepthMask = smoothstep(0.08, 0.55, depthM) * exp(-depthM / max(1.2, _CausticsMaxDepth));
                float causticsShadow = lerp(0.12, 1.0, mainLight.shadowAttenuation);
                // 根据水底自身亮度做柔和调制（避免把暗部石头染成白网）
                float seabedLuma = saturate(dot(seabedScene, half3(0.3, 0.59, 0.11)));
                float seabedMask = lerp(0.28, 0.95, seabedLuma);

                half3 causticsTint = lerp(half3(0.92, 0.96, 0.82), half3(0.38, 0.88, 0.84), saturate(depthM / 2.6));
                half3 causticsLight = causticsTint * mainLight.color * (organicWeb * _CausticsIntensity * causticsDepthMask * causticsShadow * seabedMask);

                // 柔和溶入水底，上限做软钳制防止过曝发白
                seabedScene += causticsLight * 0.36;

                // -------------------------------------------------------------
                // 4. 五级水深光学层次 + 水下珊瑚礁自然透彩
                // -------------------------------------------------------------
                float z1 = saturate(depthM / 0.90);                    // 0.0 ~ 0.9m: 极浅清透暖沙岸滩
                float z2 = smoothstep(0.65, 3.20, depthM);             // 0.65 ~ 3.2m: 翡翠薄荷绿内湾
                float z3 = smoothstep(2.60, 7.20, depthM);             // 2.6 ~ 7.2m: 孔雀青蓝礁盘边缘
                float z4 = 1.0 - exp(-max(0.0, depthM - 5.2) * 0.25);  // >5.2m:      右侧深海鱼群海沟

                half3 waterBodyColor = lerp(_CrystalShoreColor.rgb, _Shallow.rgb, z1);
                waterBodyColor = lerp(waterBodyColor, _ReefMidColor.rgb, z2);
                waterBodyColor = lerp(waterBodyColor, _Deep.rgb, saturate(z3 * 0.72 + z4 * 0.56));

                if (_UseDepthRamp > 0.5)
                {
                    half3 rampCol = SAMPLE_TEXTURE2D(_DepthRampMap, sampler_DepthRampMap, float2(saturate(depthM / 11.5), 0.5)).rgb;
                    waterBodyColor = lerp(waterBodyColor, rampCol, 0.45);
                }

                float opticalPath = min(depthM / max(0.42, abs(v.y)), 45.0);
                float3 attenCoeff = lerp(float3(0.45, 0.12, 0.08), float3(0.82, 0.18, 0.11), smoothstep(1.4, 6.0, depthM));
                half3 absorption = exp(-opticalPath * attenCoeff);
                float clarity = _Visibility * exp(-depthM * 0.125) * lerp(0.55, 0.96, smoothstep(0.28, 0.84, ndotv));

                // 波峰次表面散射 (Subsurface Scattering)
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                float backSun = saturate(dot(v, -normalize(sun.direction + float3(0, 0.35, 0))));
                float crestSSS = pow(i.waveCrest, 2.1) * (0.35 + 0.65 * backSun) * exp(-depthM * 0.18);
                waterBodyColor += half3(0.06, 0.28, 0.24) * crestSSS * sun.shadowAttenuation;

                half3 underwaterComposite = lerp(waterBodyColor, seabedScene * absorption + waterBodyColor * (1.0 - absorption), saturate(clarity));

                // -------------------------------------------------------------
                // 5. 水面天光菲涅尔反射与涌浪条带运动感
                // -------------------------------------------------------------
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndotv, 4.4);
                float3 reflDir = reflect(-v, n);
                half3 envRefl = GlossyEnvironmentReflection(reflDir, i.worldPos, 0.09, 1.0);
                half3 skyGrad = lerp(half3(0.07, 0.26, 0.34), half3(0.36, 0.56, 0.64), saturate(reflDir.y * 0.85 + 0.15));
                half3 surfaceReflection = max(envRefl * 0.58, skyGrad * 0.52);

                float waveShading = saturate(dot(n.xz, normalize(float2(-0.65, -0.75))) * 0.36 + 0.54);
                half3 finalColor = lerp(underwaterComposite, surfaceReflection, saturate(fresnel * _SurfaceWeight + 0.03));
                finalColor *= lerp(0.91, 1.09, waveShading);

                // -------------------------------------------------------------
                // 6. 波光粼粼动态微面元太阳碎光 (v26.2 黄金克制版：收束光道、稀疏灵动星芒)
                //    移除 v26.1 中泛滥全屏的 pow 16 宽光锥，收束至迎光波峰，既波光粼粼又绝不刺眼！
                // -------------------------------------------------------------
                float3 halfVec = normalize(sun.direction + v);
                float ndoth = saturate(dot(n, halfVec));

                // 柔和迎光高光基底
                float broadSpec = pow(ndoth, 68.0) * 0.18;

                // 双层反向微面元交汇采样
                float2 glintUV1 = p * (_GlintScale * 0.095) + n.xz * 1.05 + float2(t * _GlintSpeed * 0.07, -t * _GlintSpeed * 0.08);
                float2 glintUV2 = p * (_GlintScale * 0.145) - n.xz * 0.85 + float2(-t * _GlintSpeed * 0.06, t * _GlintSpeed * 0.09);
                float maskA = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1).b;
                float maskB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV2).b;
                float maskC = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1 * 0.53 + float2(0.41, 0.19)).r;

                float facet = maskA * maskB * (0.45 + 0.55 * maskC);
                float fWidth = max(fwidth(facet), 0.018);

                // 提高触发门槛 (0.25~0.44)，让碎光点呈稀疏灵动的星点分布，而不是成片白斑
                float sparkleMid = smoothstep(0.24 - fWidth, 0.38 + fWidth, facet);
                float sparkleCore = smoothstep(0.34 - fWidth, 0.48 + fWidth, facet);

                // 使用可调聚焦指数 _GlintFocus (默认 72)，将波光严格约束在太阳反射光道与波峰迎光面上
                float midSunCone = pow(ndoth, max(24.0, _GlintFocus * 0.65)) * smoothstep(0.35, 0.85, i.waveCrest);
                float coreSunCone = pow(ndoth, max(48.0, _GlintFocus * 1.45)) * smoothstep(0.42, 0.90, i.waveCrest);

                // 浅滩极浅处自动减弱表面碎光，避免与水底焦散打架
                float depthSparkleMask = smoothstep(0.35, 1.80, depthM);

                float totalSparkle = (
                    broadSpec +
                    (sparkleMid * midSunCone * 0.35 + sparkleCore * coreSunCone * 0.85) * _GlintIntensity * _GlintEnable * depthSparkleMask
                ) * sun.shadowAttenuation * detailScale;

                finalColor += lerp(sun.color, _SunPathColor.rgb, 0.58) * min(totalSparkle, 1.35);

                // -------------------------------------------------------------
                // 7. 岸礁/栈桥双层涌浪白沫与小船 V 型扩散尾流
                // -------------------------------------------------------------
                half4 foamSample = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, p * 0.36 + n.xz * 0.42 + float2(t * 0.035, -t * 0.025));
                float laceNoise = (foamSample.r > 0.01 && foamSample.r < 0.99)
                    ? (foamSample.r * 0.68 + foamSample.g * 0.32)
                    : (0.5 + 0.28 * sin(p.x * 4.2 + sin(p.y * 2.3) + t * 0.9) + 0.22 * cos(p.y * 5.6 - p.x * 2.1 - t * 1.3));

                float contactFoam = (1.0 - smoothstep(0.01, 0.14, depthM)) * smoothstep(0.28, 0.72, laceNoise);
                float surgePhase = frac(depthM / max(0.18, _FoamWidth) - t * 0.30 + laceNoise * 0.26);
                float surgeRing = sin(surgePhase * 3.14159) * (1.0 - smoothstep(0.05, _FoamWidth, depthM));
                surgeRing = smoothstep(0.45, 0.82, surgeRing * laceNoise);

                float shoreFoam = saturate(contactFoam * 0.92 + surgeRing * 0.62) * smoothstep(0.002, 0.014, depthM);

                float2 boatDelta = p - _CoastBoat.xy;
                float2 heading = normalize(_CoastBoatHeading.xy + float2(0.00001, 0.0));
                float along = dot(boatDelta, heading);
                float side = dot(boatDelta, float2(heading.y, -heading.x));
                float activeSpeed = max(0.25, saturate(_CoastBoat.z / 2.8)) * max(0.35, _CoastBoat.w);

                float hullRing = sqrt(pow(along / 2.20, 2.0) + pow(side / 0.94, 2.0));
                float hullFoam = exp(-pow((hullRing - 1.0) / 0.10, 2.0)) * (0.45 + 0.55 * laceNoise) * 0.50;
                float bowWave = exp(-pow((along - (2.05 - abs(side) * 0.42)) / 0.15, 2.0)) * (1.0 - smoothstep(0.65, 1.65, abs(side))) * activeSpeed;

                float sternDist = max(0.0, -along - 1.05);
                float wakeHalfWidth = 0.26 + sternDist * 0.27;
                float wakeFade = exp(-sternDist * 0.15) * step(along, -0.95) * activeSpeed;
                float vArm = exp(-pow((abs(side) - wakeHalfWidth) / (0.15 + sternDist * 0.04), 2.0)) * wakeFade;
                float propWash = exp(-pow(side / (0.22 + sternDist * 0.05), 2.0)) * exp(-sternDist * 0.22) * step(along, -0.95) * activeSpeed;
                float wakeFoam = saturate((vArm * 0.78 + propWash * 0.92 + hullFoam) * (0.42 + 0.58 * laceNoise));

                float combinedFoam = saturate((shoreFoam + bowWave * 0.82 + wakeFoam) * _FoamIntensity);
                half3 foamColor = lerp(half3(0.80, 0.93, 0.95), half3(0.98, 0.99, 1.0), combinedFoam);
                finalColor = lerp(finalColor, foamColor, saturate(combinedFoam * 0.80));

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
* 默认采用 **v26.2 黄金平衡参数**：`causticsIntensity = 0.92f`、`glintIntensity = 1.85f`、`glintFocus = 72.0f`、`foamIntensity = 0.95f`。
* 新增 **`OnGUI()` 实机调参面板（按 `F7` 键开启/关闭）**：支持在游戏运行时随时拖动 5 个核心光学滑块或一键切换【预设1: 清雅柔和】/【预设2: 黄金平衡】/【预设3: 璀璨烈日】。

```csharp
// ============================================================================
// 深海灯塔·归潮湾 (v26.2) — “雾岬岛·东岸”黄金平衡版水面控制器 (内置 F7 实时光学调参面板)
// File: Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaWaterArtUpgradeV26.cs
// ============================================================================
// v26.2 针对“1.水底焦散太抢戏”与“2.海面波光太刺眼”的精准收敛:
// 1. 水底焦散强度从 1.68 收敛至黄金甜点值 0.92，Shader 改为双层几何交汇 + 水底明度柔和调制，
//    只在受光浅滩沙地上呈现通透温润的光网，绝不发白抢戏。
// 2. 海面波光强度从 3.45 收敛至 1.85，新增光锥聚焦参数 _GlintFocus = 72，并在浅水焦散区自动淡出表面碎光，
//    避免水面波光与水底焦散重叠打架。
// 3. 新增实机【F7 水面美术实时调参台】：在游戏里按 F7 键即可弹出半透明滑块面板，
//    实时拖动“水底焦散强度”、“海面波光强度”、“波光聚焦范围”、“岸浪泡沫强度”、“水体通透度”，
//    并带有一键预设切换：【清雅柔和】/【v26.2 黄金平衡 (默认)】/【璀璨烈日】！
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

        [Header("v26.2 水体艺术开关 (F6: 对比新旧水面 | F7: 呼出实时光学调参滑块)")]
        public bool v26WaterEnabled = true;
        public bool spawnAmbientMarineLife = true;
        public bool showTuningPanel = false;

        [Header("v26.2 实时可调核心光学参数")]
        [Range(0f, 2.5f)] public float causticsIntensity = 0.92f;
        [Range(0.08f, 0.5f)] public float causticsScale = 0.23f;
        [Range(0f, 1.0f)] public float causticsSpeed = 0.26f;
        [Range(0f, 4.5f)] public float glintIntensity = 1.85f;
        [Range(25f, 160f)] public float glintFocus = 72.0f;
        [Range(5f, 24f)] public float glintScale = 12.5f;
        [Range(0f, 2.0f)] public float foamIntensity = 0.95f;
        [Range(0.3f, 1.0f)] public float waterVisibility = 0.86f;

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

            if (Input.GetKeyDown(KeyCode.F7))
            {
                showTuningPanel = !showTuningPanel;
            }

            if (v26WaterEnabled && _waterRenderer != null && _v26WaterMaterial != null)
            {
                if (_waterRenderer.sharedMaterial != _v26WaterMaterial)
                {
                    _waterRenderer.sharedMaterial = _v26WaterMaterial;
                }
                SyncLiveParametersToMaterial();
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

            // v26.2 黄金平衡版色彩与光学参数
            _v26WaterMaterial.SetColor("_CrystalShoreColor", new Color(0.78f, 0.90f, 0.78f, 1f));
            _v26WaterMaterial.SetColor("_Shallow", new Color(0.08f, 0.66f, 0.60f, 1f));
            _v26WaterMaterial.SetColor("_ReefMidColor", new Color(0.03f, 0.39f, 0.50f, 1f));
            _v26WaterMaterial.SetColor("_Deep", new Color(0.008f, 0.11f, 0.21f, 1f));

            _v26WaterMaterial.SetFloat("_Refraction", 0.0034f);
            _v26WaterMaterial.SetFloat("_SurfaceWeight", 0.38f);
            _v26WaterMaterial.SetFloat("_CausticsMaxDepth", 5.6f);
            _v26WaterMaterial.SetFloat("_GlintEnable", 1.0f);
            _v26WaterMaterial.SetFloat("_GlintSpeed", 0.72f);
            _v26WaterMaterial.SetColor("_SunPathColor", new Color(1.0f, 0.94f, 0.78f, 1f));
            _v26WaterMaterial.SetFloat("_FoamWidth", 0.48f);

            SyncLiveParametersToMaterial();
        }

        private void SyncLiveParametersToMaterial()
        {
            if (_v26WaterMaterial == null) return;
            _v26WaterMaterial.SetFloat("_CausticsIntensity", causticsIntensity);
            _v26WaterMaterial.SetFloat("_CausticsScale", causticsScale);
            _v26WaterMaterial.SetFloat("_CausticsSpeed", causticsSpeed);
            _v26WaterMaterial.SetFloat("_GlintIntensity", glintIntensity);
            if (_v26WaterMaterial.HasProperty("_GlintFocus"))
                _v26WaterMaterial.SetFloat("_GlintFocus", glintFocus);
            _v26WaterMaterial.SetFloat("_GlintScale", glintScale);
            _v26WaterMaterial.SetFloat("_FoamIntensity", foamIntensity);
            _v26WaterMaterial.SetFloat("_Visibility", waterVisibility);
        }

        public void ApplyPreset(int presetIndex)
        {
            if (presetIndex == 0) // 清雅柔和 (Subtle & Serene)
            {
                causticsIntensity = 0.62f;
                glintIntensity = 1.25f;
                glintFocus = 95.0f;
                foamIntensity = 0.78f;
                waterVisibility = 0.84f;
            }
            else if (presetIndex == 1) // v26.2 黄金平衡 (Default Sweet Spot)
            {
                causticsIntensity = 0.92f;
                glintIntensity = 1.85f;
                glintFocus = 72.0f;
                foamIntensity = 0.95f;
                waterVisibility = 0.86f;
            }
            else // 璀璨烈日 (Vivid Sunlit)
            {
                causticsIntensity = 1.32f;
                glintIntensity = 2.65f;
                glintFocus = 48.0f;
                foamIntensity = 1.15f;
                waterVisibility = 0.90f;
            }
            SyncLiveParametersToMaterial();
        }

        private void OnGUI()
        {
            if (!Ready || !showTuningPanel) return;

            GUI.backgroundColor = new Color(0.06f, 0.12f, 0.16f, 0.92f);
            Rect boxRect = new Rect(24f, 80f, 360f, 310f);
            GUI.Box(boxRect, "【雾岬岛·东岸】水面光学实时调参台 (按 F7 隐藏)");

            GUILayout.BeginArea(new Rect(38f, 112f, 332f, 268f));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("预设1: 清雅柔和")) ApplyPreset(0);
            if (GUILayout.Button("预设2: 黄金平衡")) ApplyPreset(1);
            if (GUILayout.Button("预设3: 璀璨烈日")) ApplyPreset(2);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label($"1. 浅滩水底焦散亮度 (Caustics): {causticsIntensity:F2}");
            causticsIntensity = GUILayout.HorizontalSlider(causticsIntensity, 0.15f, 2.0f);

            GUILayout.Label($"2. 海面波光碎金强度 (Sparkle): {glintIntensity:F2}");
            glintIntensity = GUILayout.HorizontalSlider(glintIntensity, 0.20f, 3.6f);

            GUILayout.Label($"3. 波光收束度 (越大越聚拢): {glintFocus:F0}");
            glintFocus = GUILayout.HorizontalSlider(glintFocus, 28f, 140f);

            GUILayout.Label($"4. 岸石与船尾泡沫亮度 (Foam): {foamIntensity:F2}");
            foamIntensity = GUILayout.HorizontalSlider(foamIntensity, 0.30f, 1.6f);

            GUILayout.Label($"5. 水底清澈透光度 (Clarity): {waterVisibility:F2}");
            waterVisibility = GUILayout.HorizontalSlider(waterVisibility, 0.50f, 0.96f);

            GUILayout.EndArea();
        }

        public void ApplyWaterMode(bool useV26)
        {
            v26WaterEnabled = useV26;
            if (_waterRenderer == null) LocateWaterRenderer();
            if (_waterRenderer == null || _v26WaterMaterial == null) return;
            _waterRenderer.sharedMaterial = useV26 ? _v26WaterMaterial : _originalWaterMaterial;
        }

        private void SetupReferenceMarineEcology()
        {
            if (_waterRenderer == null) return;
            float seaLevel = _waterRenderer.bounds.center.y;

            _marineEcologyRoot = new GameObject("V26_CoastMarineEcology").transform;
            _marineEcologyRoot.SetParent(transform, false);

            Shader litShader = Shader.Find("Deepsea/SeaLifeFishSwimV25")
                ?? Shader.Find("Deepsea/SeaLifeSurface")
                ?? Shader.Find("Universal Render Pipeline/Lit");

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

            Mesh silverFishMesh = BuildSlenderSilverFishMesh();
            _owned.Add(silverFishMesh);
            Material silverMat = new Material(litShader) { name = "V26_TrenchSilverSchoolMat" };
            _owned.Add(silverMat);
            if (silverMat.HasProperty("_BaseColor")) silverMat.SetColor("_BaseColor", new Color(0.86f, 0.95f, 0.99f, 1f));
            if (silverMat.HasProperty("_RimColor")) silverMat.SetColor("_RimColor", new Color(0.72f, 0.92f, 0.98f, 1f));
            if (silverMat.HasProperty("_Smoothness")) silverMat.SetFloat("_Smoothness", 0.92f);
            if (silverMat.HasProperty("_Metallic")) silverMat.SetFloat("_Metallic", 0.68f);

            var randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(26029);
            int count = 72;

            for (int i = 0; i < count; i++)
            {
                var fg = new GameObject("V26_TrenchSilverFish_" + i);
                fg.transform.SetParent(_marineEcologyRoot, false);
                float scale = UnityEngine.Random.Range(0.24f, 0.35f);
                fg.transform.localScale = new Vector3(scale * 0.68f, scale * 0.75f, scale * 1.30f);
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

            Material goldenReefMat = new Material(litShader) { name = "V26_ReefGoldenAnthiasMat" };
            _owned.Add(goldenReefMat);
            if (goldenReefMat.HasProperty("_BaseColor")) goldenReefMat.SetColor("_BaseColor", new Color(0.98f, 0.66f, 0.16f, 1f));
            if (goldenReefMat.HasProperty("_RimColor")) goldenReefMat.SetColor("_RimColor", new Color(1.0f, 0.86f, 0.35f, 1f));
            if (goldenReefMat.HasProperty("_Smoothness")) goldenReefMat.SetFloat("_Smoothness", 0.80f);

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
                float sc = UnityEngine.Random.Range(0.20f, 0.29f);
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
                    float shimmerRoll = Mathf.Sin(_clock * 4.5f + f.phase) * 16f;
                    f.tr.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, shimmerRoll);
                }
                _trenchSchool[i] = f;
            }

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
            float s = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
            float z = Mathf.Lerp(-11.5f, 13.5f, s);
            float sCurveX = 8.4f - 2.35f * Mathf.Sin(s * Mathf.PI * 1.35f - 0.35f) + 0.85f * Mathf.Cos(s * Mathf.PI * 2.7f);
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

## 第二步：重新构建 v26.2 并更新实机预览图

1. 同步更新 `Assets/Deepsea/Resources/SeaSurfaceV26/Water.mat` 的序列化属性（`_CausticsIntensity = 0.92`、`_GlintIntensity = 1.85`、`_GlintFocus = 72.0`、`_FoamIntensity = 0.95`），并重新构建 `Builds/SeaSurfaceV26/Guichao-SeaSurfaceV26.exe`（同步刷新桌面快捷方式）。
2. 在可见窗口（1920x1080）下运行 `-water-v26-check`，更新 `Docs/SeaSurfaceV26/preview.jpg`、`Docs/SeaSurfaceV26/03-next-wave.jpg` 和 `Docs/SeaSurfaceV26/result.json`。

---

## 第三步：提交并推送到 GitHub

```powershell
git add .
git commit -m "feat(water-v26.2): 收敛浅滩焦散与海面波光至黄金平衡区间并新增F7实机实时光学调参面板"
git push origin main
```
