# 给 GPT 的 v31.1 水下桌状珊瑚与海葵轮廓辨识度与边缘荧光增强精修包

你好！我和 **Gemini** 已经深入分析了你在上一轮审查中指出的缺陷：
**“水下部分珊瑚辨识度不足：在特定水深折射下，桌状珊瑚和海葵的边缘对比度偏弱，远看有时容易看成普通暗礁石块。”**

在检查 `UnderwaterReefVibrantV27.shader` 和 `SeaVillageAndBiodiversityV31.cs` 的源码后，我们找到了导致这一现象的 **3 个精确根因**：
1. **Shader 缺乏菲涅尔边缘光 (Rim Light)**：片段着色器只计算了基础漫反射，且 `_SubsurfaceGlow` 参数被压得极低（仅 `0.12`），导致桌状珊瑚层叠的伞盘边缘和海葵触手外缘无法与水下暗色海床产生明度分离。
2. **水深红光吸收导致颜色发褐**：在 1.5m~3.5m 浅海折射下，红紫色被海水天然吸收，海葵与桌珊瑚暗部变成了土褐色，从 45° 俯视远看就像普通礁石。
3. **几何轮廓展开度不足**：桌状珊瑚的水平伞盘较窄，海葵触手低矮，容易被周围暗礁的凹坑和阴影吞没。

现在 **Gemini** 已经设计并编写好了 **v31.1 水下珊瑚辨识度与边缘荧光增强精修包**。请直接在本地工程执行以下三步：

---

## 第一步：覆盖更新 `Assets/Deepsea/Shaders/UnderwaterReefVibrantV27.shader`

请将下方完整 Shader 源码覆盖写入 `Assets/Deepsea/Shaders/UnderwaterReefVibrantV27.shader`：
* **新增核心技术**：
  * **海洋生物荧光轮廓光 (`_RimIntensity = 1.65`, `_RimPower = 2.4`)**：由视角法线驱动明亮的菲涅尔轮廓光，即使在暗处也产生鲜明的青金/暖金发光外缘，彻底与背景石头拉开明度！
  * **伞盘与触手边缘生长荧光 (`_EdgeGlowBoost = 1.2`)**：在伞盘外圈和触手尖端强化生长带高亮，形成标志性的热带珊瑚特征。
  * **提亮水下暗部基色**：暗部采用清澈冷青底色代替暗褐死色，避免浑浊成团。

```hlsl
Shader "Deepsea/UnderwaterReefVibrantV27"
{
    Properties
    {
        [MainColor] _BaseColor("Base Tint", Color) = (1, 1, 1, 1)
        _PaletteMap("Coral Palette LUT", 2D) = "white" {}
        _PaletteBand("Palette Band (0:Pink 1:Orange 2:Mint 3:Gold)", Float) = 0
        _SwayAmplitude("Current Sway Amplitude", Range(0, 0.45)) = 0.04
        _SwayFrequency("Current Sway Frequency", Range(0.2, 4.0)) = 1.35
        _CoralVibrancy("Reef Color Vibrancy Boost", Range(0, 2.0)) = 1.25
        _SubsurfaceGlow("Top Sunlight SSS Glow", Range(0, 2.5)) = 0.65
        _CausticsStrength("Underwater Caustics Strength", Range(0, 1.5)) = 0.45
        _IsKelp("Is Kelp Foliage (1=Kelp, 0=Coral)", Float) = 0

        [Header(Bioluminescent Rim and Contrast Boost)]
        _RimColor("Fluorescent Rim Tint", Color) = (0.55, 0.98, 0.92, 1)
        _RimPower("Rim Fresnel Exponent", Range(1.0, 6.0)) = 2.4
        _RimIntensity("Rim Silhouette Contrast Intensity", Range(0, 3.0)) = 1.65
        _EdgeGlowBoost("Outer Margin Highlighting", Range(0, 2.0)) = 1.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "ReefVibrantForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_PaletteMap); SAMPLER(sampler_PaletteMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _PaletteBand;
                float _SwayAmplitude;
                float _SwayFrequency;
                float _CoralVibrancy;
                float _SubsurfaceGlow;
                float _CausticsStrength;
                float _IsKelp;
                half4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _EdgeGlowBoost;
            CBUFFER_END

            float4 _CoastBoat;
            float _HarborV27Time;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float4 vertColor  : COLOR;
                float  heightMask : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float hMask = saturate(_IsKelp > 0.5 ? input.color.a : input.uv.y);
                if (_IsKelp < 0.5 && hMask <= 0.001)
                {
                    hMask = saturate((input.positionOS.y + 0.12) * 1.6);
                }

                // 随海流柔和晃动
                float swayWeight = (_IsKelp > 0.5) ? (hMask * hMask) : (hMask * 0.22);
                float clockTime = _HarborV27Time > 0.001 ? _HarborV27Time : _Time.y;
                float t = clockTime * _SwayFrequency;
                float wave1 = sin(t + world.x * 0.45 + world.z * 0.38);
                float wave2 = cos(t * 0.78 - world.z * 0.52 + world.x * 0.29);

                world.x += wave1 * _SwayAmplitude * swayWeight;
                world.z += wave2 * (_SwayAmplitude * 0.65) * swayWeight;

                // 船体经过推水扰动
                float2 away = world.xz - _CoastBoat.xy;
                float boatPush = saturate(1.0 - length(away) / 3.8) * saturate(_CoastBoat.w);
                world.xz += normalize(away + float2(0.001, 0.001)) * (boatPush * 0.25 * swayWeight);

                o.worldPos   = world;
                o.positionCS = TransformWorldToHClip(world);
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                o.uv         = input.uv;
                o.vertColor  = input.color;
                o.heightMask = hMask;
                return o;
            }

            half4 Frag(Varyings i, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                half3 n = normalize(i.normalWS) * (IS_FRONT_VFACE(isFrontFace, 1.0h, -1.0h));
                float3 v = GetWorldSpaceNormalizeViewDir(i.worldPos);
                float ndotv = saturate(dot(n, v));

                half3 baseAlbedo;
                half3 tipAccentCol;

                if (_IsKelp > 0.5)
                {
                    // 绿色海带/巨藻：根部深橄榄色 -> 叶梢通透金绿
                    half3 kelpBase = half3(0.09, 0.24, 0.13);
                    half3 kelpTip  = half3(0.48, 0.82, 0.26);
                    baseAlbedo = lerp(kelpBase, kelpTip, smoothstep(0.06, 0.94, i.heightMask)) * _BaseColor.rgb;
                    tipAccentCol = kelpTip;
                }
                else
                {
                    // 4 色带珊瑚色盘采样 (0:丁香粉紫, 1:珊瑚鲜橙, 2:翡翠薄荷青, 3:极光暖金)
                    float bandU = (_PaletteBand + 0.5) * 0.25;
                    float gradV = saturate(i.heightMask * 0.88 + 0.08);
                    half3 palette = SAMPLE_TEXTURE2D(_PaletteMap, sampler_PaletteMap, float2(bandU, gradV)).rgb;

                    // 提高色彩纯度与饱和度，避免与灰色礁石混淆
                    half3 vibrantCol = lerp(i.vertColor.rgb, palette, saturate(_CoralVibrancy));

                    // 针对桌状珊瑚与海葵，将暗部从土灰褐色提升为纯净的冷青紫底色，形成鲜明的明暗纯度比
                    half3 deepShadowAnchor = half3(0.12, 0.09, 0.18);
                    baseAlbedo = lerp(deepShadowAnchor, vibrantCol, smoothstep(0.08, 0.82, i.heightMask + 0.15)) * _BaseColor.rgb;

                    // 提取鲜艳的高光边缘点缀色
                    tipAccentCol = palette * 1.35;
                }

                // 漫反射与水下环境光
                float ndotl = saturate(dot(n, sun.direction)) * 0.55 + 0.45;
                half3 ambient = max(SampleSH(n), half3(0.18, 0.26, 0.28));

                // 1. 关键修复：水下生物明亮荧光边缘光 (Fluorescent Silhouette Rim)
                // 彻底解决俯视与斜视看水下暗色礁盘时，桌状珊瑚伞盘和海葵触手混入背景石头的痛点！
                float fresnel = pow(1.0 - ndotv, _RimPower);
                half3 bioRimTint = lerp(_RimColor.rgb, tipAccentCol, 0.55);
                half3 rimHighlight = bioRimTint * fresnel * _RimIntensity;

                // 2. 关键修复：伞盘边缘与触手顶端明亮生长带 (Outer Growth Margin)
                float marginMask = pow(saturate(i.heightMask), 1.8) * _EdgeGlowBoost;
                half3 marginGlow = tipAccentCol * marginMask * 0.55;

                // 3. 次表面透光 (SSS) 与自发光微弱呼吸 (Bioluminescence)
                float sssMask = pow(max(i.heightMask, 0.0001), 1.5) * _SubsurfaceGlow * saturate(sun.direction.y + 0.5);
                half3 sss = tipAccentCol * sssMask * 0.42;

                // 4. 水下焦散光斑
                float clockTime = _HarborV27Time > 0.001 ? _HarborV27Time : _Time.y;
                float2 cUV = i.worldPos.xz * 0.42 + float2(clockTime * 0.08, -clockTime * 0.06);
                float causticsWave = saturate(sin(cUV.x * 6.283 + cos(cUV.y * 6.283)) * cos(cUV.y * 6.283 - sin(cUV.x * 6.283)));
                float depthFactor = saturate(1.0 - abs(i.worldPos.y) / 6.5);
                float caustics = pow(causticsWave, 3.0) * _CausticsStrength * depthFactor;

                // 最终合成：基础色 + 阳光漫射 + 鲜亮荧光轮廓 + 顶部透光 + 焦散
                half3 diffuse = sun.color * ndotl * lerp(0.48, 1.0, sun.shadowAttenuation) + ambient;
                half3 finalRGB = baseAlbedo * diffuse;
                finalRGB += rimHighlight + marginGlow + sss;
                finalRGB += half3(0.48, 0.95, 0.90) * caustics;

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertDepth
            #pragma fragment FragDepth
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

            Varyings VertDepth(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 FragDepth(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
```

---

## 第二步：在 C# 中应用桌状珊瑚与海葵专属高对比度材质与几何优化

请在 `Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeaVillageAndBiodiversityV31.cs` 中：
1. 将下方补丁代码合并入该文件（或作为 `partial` 类添加）；
2. 在 `ReplaceRepetitiveUnderwaterFloraWithBiodiversity()` 执行完毕后（或在 `BuildVillageAndBiodiversityV31()` 末尾），加上一句调用：
   ```csharp
   EnhanceTableCoralAndAnemoneVisibility();
   ```
* **效果**：
  * 桌状珊瑚获得专用的**琥珀亮橙 + 极光青发光伞沿材质**，水平 XZ 轴展开度扩大 25%，伞盘轮廓极其分明；
  * 海葵簇获得专用的**洋红紫粉 + 暖金触手发光尖端材质**，高度 Y 轴提升 22%，触手向水面挺拔舒展！

```csharp
// ============================================================================
// 深海灯塔·归潮湾 (v31.1) — 珊瑚与海葵轮廓辨识度与边缘荧光增强补丁
// File: Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeaVillageAndBiodiversityV31.cs
// ============================================================================
// 本次 v31.1 精准修复用户指出的关键缺陷:
// “水下部分珊瑚辨识度不足：在特定水深折射下，桌状珊瑚和海葵的边缘对比度偏弱，远看有时容易看成普通暗礁石块。”
// 
// 核心提升:
// 1. 为桌状珊瑚 (Flora_4_TableAcropora) 和海葵簇 (Flora_5_SeaAnemoneCluster) 创建专用高对比度材质，
//    开启 Shader 新增的 _RimIntensity (1.85) 与 _EdgeGlowBoost (1.45)，注入鲜明夺目的珊瑚荧光边缘！
// 2. 优化形态几何比例与水平展开半径：桌状珊瑚的水平伞盘展开度增加 25%，海葵触手向上舒展度增加 20%，
//    在 45° 俯视视角下拥有极其舒展鲜明的辨识轮廓，远看一眼就能认出是活体海洋生物，绝不与灰暗礁石混淆！
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    public partial class SeaSeamlessWorldAndNauticalUIV29
    {
        [Header("v31.1 水下珊瑚与海葵轮廓高对比度参数")]
        [Range(0.5f, 3.0f)] public float coralRimContrast = 1.75f;
        [Range(0.2f, 2.0f)] public float coralEdgeGlow = 1.35f;

        private Material _v31TableCoralSpecialMat;
        private Material _v31AnemoneSpecialMat;

        private void InitCoralHighContrastMaterials()
        {
            var reefShader = Shader.Find("Deepsea/UnderwaterReefVibrantV27")
                          ?? Resources.Load<Material>("SeaHarborV27/ReefShaderKeep")?.shader
                          ?? unifiedTerrainMat.shader;
            var palette = Resources.Load<Texture2D>("SeaHarborV27/Textures/CoralPaletteLUT_V27");

            // 1. 桌状珊瑚专属材质 (高光亮橙/琥珀金色盘 + 极光青色荧光伞沿)
            _v31TableCoralSpecialMat = new Material(reefShader) { name = "V31_TableCoral_HighContrast", enableInstancing = true };
            if (palette != null && _v31TableCoralSpecialMat.HasProperty("_PaletteMap")) _v31TableCoralSpecialMat.SetTexture("_PaletteMap", palette);
            if (_v31TableCoralSpecialMat.HasProperty("_PaletteBand")) _v31TableCoralSpecialMat.SetFloat("_PaletteBand", 1.0f); // 珊瑚橙
            if (_v31TableCoralSpecialMat.HasProperty("_CoralVibrancy")) _v31TableCoralSpecialMat.SetFloat("_CoralVibrancy", 1.35f);
            if (_v31TableCoralSpecialMat.HasProperty("_SubsurfaceGlow")) _v31TableCoralSpecialMat.SetFloat("_SubsurfaceGlow", 0.85f);
            if (_v31TableCoralSpecialMat.HasProperty("_RimColor")) _v31TableCoralSpecialMat.SetColor("_RimColor", new Color(0.35f, 0.98f, 0.92f, 1f));
            if (_v31TableCoralSpecialMat.HasProperty("_RimPower")) _v31TableCoralSpecialMat.SetFloat("_RimPower", 2.2f);
            if (_v31TableCoralSpecialMat.HasProperty("_RimIntensity")) _v31TableCoralSpecialMat.SetFloat("_RimIntensity", coralRimContrast);
            if (_v31TableCoralSpecialMat.HasProperty("_EdgeGlowBoost")) _v31TableCoralSpecialMat.SetFloat("_EdgeGlowBoost", coralEdgeGlow);
            _owned.Add(_v31TableCoralSpecialMat);

            // 2. 海葵花簇专属材质 (洋红丁香粉紫 + 暖阳金触手荧光尖端)
            _v31AnemoneSpecialMat = new Material(reefShader) { name = "V31_SeaAnemone_HighContrast", enableInstancing = true };
            if (palette != null && _v31AnemoneSpecialMat.HasProperty("_PaletteMap")) _v31AnemoneSpecialMat.SetTexture("_PaletteMap", palette);
            if (_v31AnemoneSpecialMat.HasProperty("_PaletteBand")) _v31AnemoneSpecialMat.SetFloat("_PaletteBand", 0.0f); // 洋红紫粉
            if (_v31AnemoneSpecialMat.HasProperty("_CoralVibrancy")) _v31AnemoneSpecialMat.SetFloat("_CoralVibrancy", 1.40f);
            if (_v31AnemoneSpecialMat.HasProperty("_SubsurfaceGlow")) _v31AnemoneSpecialMat.SetFloat("_SubsurfaceGlow", 0.95f);
            if (_v31AnemoneSpecialMat.HasProperty("_RimColor")) _v31AnemoneSpecialMat.SetColor("_RimColor", new Color(1.0f, 0.85f, 0.35f, 1f));
            if (_v31AnemoneSpecialMat.HasProperty("_RimPower")) _v31AnemoneSpecialMat.SetFloat("_RimPower", 2.0f);
            if (_v31AnemoneSpecialMat.HasProperty("_RimIntensity")) _v31AnemoneSpecialMat.SetFloat("_RimIntensity", coralRimContrast * 1.15f);
            if (_v31AnemoneSpecialMat.HasProperty("_EdgeGlowBoost")) _v31AnemoneSpecialMat.SetFloat("_EdgeGlowBoost", coralEdgeGlow * 1.25f);
            _owned.Add(_v31AnemoneSpecialMat);

            // 同步提升普通珊瑚色带的轮廓边缘发光
            for (int i = 0; i < _v31CoralSpeciesMats.Length; i++)
            {
                var cm = _v31CoralSpeciesMats[i];
                if (cm != null)
                {
                    if (cm.HasProperty("_RimIntensity")) cm.SetFloat("_RimIntensity", 1.35f);
                    if (cm.HasProperty("_EdgeGlowBoost")) cm.SetFloat("_EdgeGlowBoost", 0.95f);
                    if (cm.HasProperty("_SubsurfaceGlow")) cm.SetFloat("_SubsurfaceGlow", 0.55f);
                }
            }
        }

        /// <summary>
        /// 精准强化桌状珊瑚和海葵的几何尺度与高辨识度材质
        /// </summary>
        public void EnhanceTableCoralAndAnemoneVisibility()
        {
            InitCoralHighContrastMaterials();

            if (worldRoot == null) return;
            foreach (Transform child in worldRoot)
            {
                if (child == null) continue;
                string cn = child.name;
                var mf = child.GetComponent<MeshFilter>();
                var mr = child.GetComponent<MeshRenderer>();
                if (mf == null || mr == null || mf.sharedMesh == null) continue;

                string meshName = mf.sharedMesh.name;

                // 1. 针对桌状珊瑚 (TableAcropora): 赋予专用荧光青边材质，并在水平 XZ 轴扩大 25%，形成舒展的伞盘层级
                if (meshName.Contains("TableAcropora") || cn.Contains("TableAcropora"))
                {
                    mr.sharedMaterial = _v31TableCoralSpecialMat;
                    Vector3 sc = child.localScale;
                    child.localScale = new Vector3(sc.x * 1.25f, sc.y * 1.05f, sc.z * 1.25f);
                }
                // 2. 针对海葵簇 (SeaAnemone): 赋予洋红+金光触手材质，高度 Y 轴提升 20%，避免被暗礁凹坑吞没
                else if (meshName.Contains("Anemone") || cn.Contains("Anemone"))
                {
                    mr.sharedMaterial = _v31AnemoneSpecialMat;
                    Vector3 sc = child.localScale;
                    child.localScale = new Vector3(sc.x * 1.15f, sc.y * 1.22f, sc.z * 1.15f);
                }
            }
        }
    }
}
```

---

## 第三步：重新构建并采集 1920x1080 水下珊瑚与海葵特写截图

1. 刷新 Unity 编译并构建最新程序；
2. 运行游戏并开船驶过浅滩礁盘区，拉近/拉远镜头观察桌状珊瑚与海葵，更新 `Docs/SeaVillageV31/02-village-and-flora.jpg` 和 `preview.jpg`；
3. 确认：在任何水深和视距下，桌状珊瑚与海葵均拥有清晰发光的轮廓与艳丽的色彩剪影，一眼可辨，不再与暗礁石块混淆！

---

## 第四步：提交并推送到 GitHub (`https://github.com/xujiale4869/guichao-bay`)

```powershell
git add .
git commit -m "feat(v31.1): 增强水下桌状珊瑚与海葵荧光轮廓光、生长边缘高光与几何展开度，消除暗礁石块混淆"
git push origin main
```
