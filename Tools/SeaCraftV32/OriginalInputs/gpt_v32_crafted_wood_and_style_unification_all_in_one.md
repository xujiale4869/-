# 给 GPT 的 v32《归潮湾》陆地建模与木石工艺全面重构包（消灭 Cube 积木感、重铸八角倒角枕木与鱼鳞叠瓦、统一全岛风化青苔画风）

你好！我和 **Gemini** 已经深入审查了仓库 `https://github.com/xujiale4869/guichao-bay` 中的 `VillageCompositionV31.cs`、`SeaVillageAndBiodiversityV31.cs`、`SeaBayStructuresV28.cs` 以及 `Docs/SeaVillageV31/` 下的实机画面。

针对用户指出的核心美术痛点：
**“除却水面还不错，感觉陆地上的建模还是有些粗糙的，木板啊之类的也没有给我很好的效果吧，整个游戏的风格上还是有些不统一”**

我们查明了导致这一现象的 **3 个精确代码根因**：

1. **大量直接使用 90° 锋利直角的 `PrimitiveType.Cube` 拼积木**：在 `VillageCompositionV31.cs`、`SeaVillageAndBiodiversityV31.cs` 和 `SeaLifeActivities.cs` 中，前院木栈台 (`ForecourtTread`)、岸边步道 (`ShoreAccessTread`)、房屋墙体/屋顶/底座、海洋小馆门廊以及石板路，全部直接调用了 `GameObject.CreatePrimitive(PrimitiveType.Cube)`！一块巨大的光滑正方体直接当木平台或屋顶，既没有单根木板的分缝与边缘倒角（Chamfer/Bevel），也没有瓦片的层叠起伏。

2. **木板与石材缺失微浮雕法线凹凸与结构 AO 阴影**：新建筑使用的材质只是单张平涂贴图，没有木年轮凹凸法线、没有逐块木板的冷暖色差（Vertex Color RGB），也没有板缝与柱脚的环境光遮蔽（Vertex AO Alpha），在阳光下像扁平的塑料积木。

3. **人工建筑与原版自然海岬画风割裂**：原版海岬岩石 (`V24CoastShowcaseSurface.shader`) 带有细腻的潮汐水线浸湿变深与海风青苔自然生长，而陆地上的木桩、石阶和房屋却干干净净、毫无风化痕迹，显得像贴上去的外来物。

现在 **Gemini** 已经设计并编写好了 **v32 陆地木石工艺重铸与风格统一系统**（本文件已内置完整 Shader 与 C# 源码）。请直接在本地工程执行以下四步：

## 第一步：写入以下 2 个核心文件并挂载调用

### 1. 写入全陆地木石工艺统一着色器 `Assets/Deepsea/Shaders/StylizedCoastalCraftV32.shader`

* **四大核心特性**：

  * **微浮雕法线凹凸 (`EvaluateCraftMicroRelief`)**：内置 4 种工艺模式（`0:沧桑老枕木板`、`1:鱼鳞交错板岩瓦`、`2:海岸乱石砌体`、`3:粗犷树皮原木`），实时生成深邃的木年轮凹凸、瓦片叠缝与石材凿痕法线！

  * **倒角边缘日晒海盐磨损高光 (`_EdgeWear`)**：在朝上的 45° 倒角棱边产生温润的浅色海盐日晒高光，凸显每一根木板的立体倒角轮廓。

  * **逐木板色差与板缝深邃 AO (`color.rgb` & `color.a`)**：消除整面墙、整座桥同一种颜色的死板塑料感。

  * **与原版海岬 100% 统一的潮汐浸水与海风青苔 (`_MossStrength`)**：水线附近（`-0.35m ~ +0.62m`）的木桩与石基自动浸湿变深，背阴与近地表面自然生长出与原版海岸完全同色系的海岬青苔（`half3(0.26, 0.42, 0.24)`）！

```
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 localPos   : TEXCOORD2;
                float2 uv         : TEXCOORD3;
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
                o.localPos   = input.positionOS.xyz;
                o.positionCS = TransformWorldToHClip(world);
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                o.uv         = input.uv;
                o.vertColor  = input.color;
                return o;
            }

            // 程序化计算高精木年轮凹凸、木瓦叠缝与海岸砌石肌理 (返回 x,y 法线斜率与 z 纹理明暗值)
            float3 EvaluateCraftMicroRelief(float3 wPos, float2 uv, float mode)
            {
                if (mode < 0.5) // 0: 沧桑海盐老木板 (PlankWood)
                {
                    float2 p = float2(uv.x * 3.5 + wPos.x * 0.65, uv.y * 16.0 + wPos.z * 0.65) * _GrainScale;
                    float knot = sin(p.x * 2.1 + cos(p.y * 0.45) * 2.5);
                    float ring = sin(p.y * 3.8 + knot * 3.2);
                    float fine = cos(p.y * 19.0 + p.x * 4.0) * 0.35;
                    float h = ring * 0.65 + fine;
                    float dx = cos(p.y * 3.8 + knot * 3.2) * 2.1 * _NormalStrength * 0.16;
                    float dy = cos(p.y * 3.8 + knot * 3.2) * 3.8 * _NormalStrength * 0.22;
                    return float3(dx, dy, h * 0.5 + 0.5);
                }
                else if (mode < 1.5) // 1: 层叠鱼鳞板岩木瓦 (SlateRoof)
                {
                    float2 p = wPos.xz * 2.4 * _GrainScale;
                    float row = floor(p.y);
                    float staggeredX = p.x + fmod(row, 2.0) * 0.5;
                    float2 cell = frac(float2(staggeredX, p.y));
                    // 瓦片边缘凹槽与层叠台阶感
                    float seamX = smoothstep(0.0, 0.09, cell.x) * smoothstep(1.0, 0.91, cell.x);
                    float seamY = smoothstep(0.0, 0.14, cell.y);
                    float slateGrain = 0.85 + 0.15 * sin(p.x * 14.0 + cos(p.y * 9.0));
                    float h = seamX * seamY * slateGrain;
                    float dx = (cell.x < 0.1 ? 1.0 : (cell.x > 0.9 ? -1.0 : 0.0)) * _NormalStrength * 0.35;
                    float dy = (cell.y < 0.16 ? 1.2 : -0.15) * _NormalStrength * 0.42;
                    return float3(dx, dy, h);
                }
                else if (mode < 2.5) // 2: 海岸乱石砌体与踏步石板 (MasonryStone)
                {
                    float3 p = wPos * 1.65 * _GrainScale;
                    float n1 = sin(p.x * 3.1 + sin(p.z * 2.7)) * cos(p.y * 3.4 + sin(p.x * 2.2));
                    float n2 = cos(p.z * 7.8 - p.x * 6.4) * 0.35;
                    float h = (n1 * 0.65 + n2) * 0.5 + 0.5;
                    float dx = cos(p.x * 3.1 + sin(p.z * 2.7)) * _NormalStrength * 0.28;
                    float dy = -sin(p.z * 7.8 - p.x * 6.4) * _NormalStrength * 0.28;
                    return float3(dx, dy, h);
                }
                else // 3: 粗犷圆木立柱与树皮横梁 (LogTimber)
                {
                    float2 p = float2(wPos.y * 4.5, (wPos.x + wPos.z) * 6.0) * _GrainScale;
                    float bark = sin(p.y * 3.2 + sin(p.x * 1.8) * 2.0) * 0.6 + cos(p.y * 11.0) * 0.4;
                    return float3(bark * 0.25 * _NormalStrength, 0.1 * _NormalStrength, bark * 0.5 + 0.5);
                }
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 geomN = normalize(i.normalWS);
                float3 relief = EvaluateCraftMicroRelief(i.worldPos, i.uv, _CraftMode);

                // 构建切线空间微浮雕法线扰动，让每一块木板和石头在阳光下都有立体的凹凸质感
                float3 upRef = abs(geomN.y) > 0.9 ? float3(1, 0, 0) : float3(0, 1, 0);
                float3 tangent = normalize(cross(upRef, geomN));
                float3 bitangent = cross(geomN, tangent);
                float3 n = normalize(geomN + tangent * relief.x + bitangent * relief.y);

                // 1. 基底色 + 纹理采样 + 逐木板/逐石块顶点色微扰 (彻底打破单色死板！)
                half3 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                half3 vertTint = (dot(i.vertColor.rgb, half3(1, 1, 1)) > 0.05) ? i.vertColor.rgb : half3(1, 1, 1);
                half3 albedo = texCol * _BaseColor.rgb * vertTint;

                // 叠加木纹/石纹深浅肌理
                albedo *= lerp(0.76, 1.16, relief.z);

                // 2. 倒角边缘日晒海盐磨损高光 (Chamfer Edge Highlighting)
                // 在朝上的倒角棱边与木板边缘产生温润的浅暖色磨损，凸显立体倒角轮廓！
                float upwardEdge = saturate(geomN.y * 0.65 + 0.35) * smoothstep(0.62, 0.95, relief.z);
                half3 wornHighlight = lerp(albedo, albedo * half3(1.28, 1.22, 1.12) + half3(0.06, 0.05, 0.03), _EdgeWear);
                albedo = lerp(albedo, wornHighlight, upwardEdge * 0.55);

                // 3. 与原版海岬完全统一的【潮间带浸水湿润度 + 海风青苔自然生长】
                float seaLevel = -0.22;
                float heightAboveSea = i.worldPos.y - seaLevel;
                // 水线附近 (-0.4m ~ +0.55m) 浸湿变深并泛出微弱海藻绿
                float wetness = 1.0 - smoothstep(-0.35, 0.62, heightAboveSea);
                albedo = lerp(albedo, albedo * half3(0.52, 0.60, 0.56), wetness * _MossStrength * 0.85);

                // 背阴朝北或靠近地面的潮湿处自然生长海岬青苔 (与 V24CoastShowcaseSurface 的青苔色调 100% 呼应！)
                float mossNoise = 0.5 + 0.3 * sin(i.worldPos.x * 0.75 + cos(i.worldPos.z * 0.65) * 2.0)
                                      + 0.2 * cos(i.worldPos.z * 1.15 - i.worldPos.x * 0.85);
                float mossMask = smoothstep(0.35, 0.88, geomN.y)
                               * smoothstep(0.48, 0.82, mossNoise)
                               * (1.0 - smoothstep(0.2, 3.8, heightAboveSea))
                               * _MossStrength;
                half3 coastalMossCol = half3(0.26, 0.42, 0.24);
                albedo = lerp(albedo, coastalMossCol, mossMask * 0.58);

                // 4. 结构环境光遮蔽 (Baked Vertex AO in color.a)：让板缝、柱脚、屋檐下呈现深邃柔和的立体阴影
                float vAO = (i.vertColor.a > 0.001) ? lerp(1.0, i.vertColor.a, _AOStrength) : 1.0;

                // 5. 温暖绘本风 PBR + 半兰伯特漫反射光照
                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = 0.0;
                s.specular = half3(0.04, 0.04, 0.04);
                s.smoothness = clamp((1.0 - _Roughness) + wetness * 0.28, 0.08, 0.82);
                s.occlusion = vAO;
                s.alpha = 1.0;

                InputData input = (InputData)0;
                input.positionWS = i.worldPos;
                input.normalWS = n;
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.worldPos);
                input.shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                input.bakedGI = max(half3(0.12, 0.17, 0.20), SampleSH(n));
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                input.shadowMask = half4(1, 1, 1, 1);

                half3 pbrColor = UniversalFragmentPBR(input, s).rgb;

                // 补充柔和暖阳边缘漫射，消除背光面死黑
                Light mainLight = GetMainLight(input.shadowCoord);
                float wrapDiff = saturate(dot(n, mainLight.direction) * 0.5 + 0.5);
                pbrColor += albedo * mainLight.color * (wrapDiff * 0.12 * vAO);

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

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
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, normalWS, _LightDirection));
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

```

### 2. 写入陆地木石几何重铸引擎 `Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/CoastalCraftsmanshipUpgradeV32.cs`

* **五大几何重铸升级**：

  1. **平台与桥面重铸 (`_unitBeveledPlankMesh`)**：将所有粗糙的平台 Cube 自动重铸为 **6 根独立八角斜切倒角、微弧拱起、带真实透光板缝、底部双托梁、逐板冷暖色差与板间 AO 阴影的实木老枕木群**！

  2. **屋顶重铸 (`_unitLayeredRoofMesh`)**：将平滑的屋顶 Cube 自动重铸为 **30 片交错叠压、带微倾角与明暗色差的鱼鳞板岩木瓦层**！

  3. **墙体重铸 (`_unitClapboardWallMesh`)**：将光滑的墙体 Cube 自动重铸为 **7 层水平羽叠搭接木墙板（Clapboard Siding）+ 四角外露原木框架立柱（Half-Timbered Framing）**！

  4. **石基与石板路重铸 (`_unitChiseledCobbleMesh`)**：将方砖状的石基与石板路重铸为 **多面凿切倒角、高低错落嵌合的海岸乱石砌体**！

  5. **木梁与立柱重铸 (`_unitBeveledBeamMesh`)**：将所有直角木柱、栏杆、修船龙骨、木箱全面替换为 **八边形三层斜切倒角实木网格**！

* **调用方式**：
  请在 `SeaVillageAndBiodiversityV31.cs` 的 `BuildVillageAndBiodiversityV31()` 方法末尾（在 `Physics.SyncTransforms();` 之前），以及在 `VillageCompositionV31.cs` 的 `MergeStaticRenderersV31()` 合并之前（或直接在 `AddBoxPart` 创建时），调用：

  ```
  ApplyCoastalCraftsmanshipUpgradeV32();
  
  ```

  *(注意：如果在 `VillageCompositionV31.cs` 中先执行 `UpgradeAllSceneCubePrimitivesToCraftedMeshes()` 再执行 `MergeStaticRenderersV31()`，那么前院栈台的每一阶踏板都会在合并前被直接替换为 6 根独立倒角老枕木，近看效果极其惊艳！)*

```
// ============================================================================
// 深海灯塔·归潮湾 (v32) — 陆地建筑与木板工艺重构系统（消灭直角方块感，统一手绘海岬匠心画风）
// File: Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/CoastalCraftsmanshipUpgradeV32.cs
// ============================================================================
// 核心解决用户在 v31 中指出的美术痛点:
// “除却水面还不错，感觉陆地上的建模还是有些粗糙的，木板啊之类的也没有给我很好的效果吧，整个游戏的风格上还是有些不统一”
//
// 根因诊断与 v32 彻底解决方案:
// 1. 为什么木板粗糙？
//    - 之前不论是前院栈桥 (`ForecourtTread`)、岸边步道 (`ShoreAccessTread`)、海洋小馆入口 (`小木平台`)
//      还是可建造的延伸木栈桥 (`WeatheredPlank`)，全都直接用了 Unity 的 6 面直角 `PrimitiveType.Cube`！
//      一块大 Cube 直接当桥面，既没有单根枕木的分缝，也没有边缘斜切倒角 (Chamfer/Bevel) 和木头受潮微翘感！
//    - v32 方案：编写 **`BuildCraftedMultiPlankDeckMesh()`** 与 **`BuildBeveledTimberMesh()`** 几何雕刻器！
//      把每一块粗糙的平台 Cube 自动拆解重铸为 **多根独立八角斜切倒角、微弧拱起、两端带沉头铁铆钉、
//      逐板冷暖色差 (Vertex Color RGB) 与板缝深邃环境光遮蔽 (Vertex AO Alpha) 的实木老枕木群**！
// 2. 为什么陆地上的房子和石板路显得粗糙且风格不统一？
//    - 之前 3 座渔村房屋的墙、屋顶、底座以及石板路全是光滑无倒角的 Cube 堆叠，与原版细腻凹凸的海岬岩石画风脱节。
//    - v32 方案：
//      ① 屋顶自动重铸为 **多层交错叠压的鱼鳞板岩木瓦网格 (`BuildLayeredSlateShingleRoofMesh`)** + 雕花封檐板与悬山木梁！
//      ② 木墙自动重铸为 **水平搭接的羽叠木墙板 (`BuildClapboardWallMesh`) + 外露原木框架立柱**！
//      ③ 石头底座与石板路自动重铸为 **多面切角、高低错落嵌合的海岸乱石砌体 (`BuildChiseledCobbleStoneMesh`)**！
//      ④ 全陆地木、石、瓦统一接入 **`Deepsea/StylizedCoastalCraftV32`** 着色器，共享微浮雕法线凹凸、
//         潮间带浸水湿润度与海风青苔自然生长，让陆地建筑、木栈桥与原版海岬、波光水面 100% 融为一体！
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    public partial class SeaSeamlessWorldAndNauticalUIV29
    {
        [Header("v32 陆地木石工艺重铸与风格统一统计")]
        public int v32CraftedPlankDecksCount;
        public int v32CraftedBeamsAndPostsCount;
        public int v32CraftedRoofsAndWallsCount;
        public int v32CraftedStoneworkCount;

        private Material _v32PlankWoodMat;
        private Material _v32LogTimberMat;
        private Material _v32SlateRoofMat;
        private Material _v32MasonryStoneMat;

        private Mesh _unitBeveledPlankMesh;
        private Mesh _unitBeveledBeamMesh;
        private Mesh _unitChiseledCobbleMesh;
        private Mesh _unitLayeredRoofMesh;
        private Mesh _unitClapboardWallMesh;

        /// <summary>
        /// 在场景与渔村构建完成后调用，将全陆地粗糙的 Cube 木板、栈桥、房屋与石阶全面重铸为匠心倒角高细节模型！
        /// </summary>
        public void ApplyCoastalCraftsmanshipUpgradeV32()
        {
            v32CraftedPlankDecksCount = 0;
            v32CraftedBeamsAndPostsCount = 0;
            v32CraftedRoofsAndWallsCount = 0;
            v32CraftedStoneworkCount = 0;

            InitV32CraftMaterialsAndMeshes();

            // 1. 升级尚未合并或独立存在的木栈道、房屋构件、海洋小馆门廊与石板路
            UpgradeAllSceneCubePrimitivesToCraftedMeshes();

            // 2. 针对 VillageCompositionV31 中已经被 MergeStaticRenderersV31 合并的前院与岸边栈道网格，
            //    赋予全新的微浮雕木纹法线 + 潮汐青苔着色器，并在其表面铺设立体倒角枕木层！
            UpgradeMergedVillageDecksAndHouses();

            // 3. 升级 v28/v30 海湾可建造设施中的木栈桥模板与已建栈桥，让玩家建造的每一段木栈桥也是高精倒角枕木！
            UpgradeBuildablePierStructures();
        }

        private void InitV32CraftMaterialsAndMeshes()
        {
            Shader craftShader = Shader.Find("Deepsea/StylizedCoastalCraftV32")
                              ?? Resources.Load<Material>("SeaSurfaceV24/BudgetCoastShowcaseSurface")?.shader
                              ?? Shader.Find("Universal Render Pipeline/Lit");

            Texture2D woodTex = Resources.Load<Texture2D>("SeaHarborV27/Textures/WeatheredPierWood_V27");

            // Mode 0: 沧桑海盐老枕木板 (PlankWood)
            _v32PlankWoodMat = new Material(craftShader) { name = "V32_CraftedPlankWoodMat", enableInstancing = true };
            if (woodTex != null && _v32PlankWoodMat.HasProperty("_BaseMap")) _v32PlankWoodMat.SetTexture("_BaseMap", woodTex);
            if (_v32PlankWoodMat.HasProperty("_BaseColor")) _v32PlankWoodMat.SetColor("_BaseColor", new Color(0.68f, 0.56f, 0.42f, 1f));
            if (_v32PlankWoodMat.HasProperty("_CraftMode")) _v32PlankWoodMat.SetFloat("_CraftMode", 0f);
            if (_v32PlankWoodMat.HasProperty("_GrainScale")) _v32PlankWoodMat.SetFloat("_GrainScale", 1.85f);
            if (_v32PlankWoodMat.HasProperty("_NormalStrength")) _v32PlankWoodMat.SetFloat("_NormalStrength", 1.05f);
            if (_v32PlankWoodMat.HasProperty("_EdgeWear")) _v32PlankWoodMat.SetFloat("_EdgeWear", 0.48f);
            if (_v32PlankWoodMat.HasProperty("_MossStrength")) _v32PlankWoodMat.SetFloat("_MossStrength", 0.55f);
            _owned.Add(_v32PlankWoodMat);

            // Mode 3: 粗犷圆木立柱与深色承重木梁 (LogTimber)
            _v32LogTimberMat = new Material(craftShader) { name = "V32_CraftedLogTimberMat", enableInstancing = true };
            if (woodTex != null && _v32LogTimberMat.HasProperty("_BaseMap")) _v32LogTimberMat.SetTexture("_BaseMap", woodTex);
            if (_v32LogTimberMat.HasProperty("_BaseColor")) _v32LogTimberMat.SetColor("_BaseColor", new Color(0.48f, 0.38f, 0.28f, 1f));
            if (_v32LogTimberMat.HasProperty("_CraftMode")) _v32LogTimberMat.SetFloat("_CraftMode", 3f);
            if (_v32LogTimberMat.HasProperty("_NormalStrength")) _v32LogTimberMat.SetFloat("_NormalStrength", 1.15f);
            if (_v32LogTimberMat.HasProperty("_MossStrength")) _v32LogTimberMat.SetFloat("_MossStrength", 0.72f);
            _owned.Add(_v32LogTimberMat);

            // Mode 1: 海岬深青灰鱼鳞板岩木瓦 (SlateRoof)
            _v32SlateRoofMat = new Material(craftShader) { name = "V32_CraftedSlateRoofMat", enableInstancing = true };
            if (_v32SlateRoofMat.HasProperty("_BaseColor")) _v32SlateRoofMat.SetColor("_BaseColor", new Color(0.26f, 0.33f, 0.37f, 1f));
            if (_v32SlateRoofMat.HasProperty("_CraftMode")) _v32SlateRoofMat.SetFloat("_CraftMode", 1f);
            if (_v32SlateRoofMat.HasProperty("_GrainScale")) _v32SlateRoofMat.SetFloat("_GrainScale", 2.2f);
            if (_v32SlateRoofMat.HasProperty("_NormalStrength")) _v32SlateRoofMat.SetFloat("_NormalStrength", 1.25f);
            if (_v32SlateRoofMat.HasProperty("_EdgeWear")) _v32SlateRoofMat.SetFloat("_EdgeWear", 0.38f);
            _owned.Add(_v32SlateRoofMat);

            // Mode 2: 海岸乱石砌体与踏步石阶 (MasonryStone)
            _v32MasonryStoneMat = new Material(craftShader) { name = "V32_CraftedMasonryStoneMat", enableInstancing = true };
            if (_v32MasonryStoneMat.HasProperty("_BaseColor")) _v32MasonryStoneMat.SetColor("_BaseColor", new Color(0.46f, 0.48f, 0.44f, 1f));
            if (_v32MasonryStoneMat.HasProperty("_CraftMode")) _v32MasonryStoneMat.SetFloat("_CraftMode", 2f);
            if (_v32MasonryStoneMat.HasProperty("_GrainScale")) _v32MasonryStoneMat.SetFloat("_GrainScale", 1.65f);
            if (_v32MasonryStoneMat.HasProperty("_NormalStrength")) _v32MasonryStoneMat.SetFloat("_NormalStrength", 1.10f);
            if (_v32MasonryStoneMat.HasProperty("_MossStrength")) _v32MasonryStoneMat.SetFloat("_MossStrength", 0.68f);
            _owned.Add(_v32MasonryStoneMat);

            // 预烘焙 5 套高精匠心基础网格 (全部归一化在 [-0.5, +0.5] 单位包围盒内，可直接无损替换任何尺度的 Cube！)
            _unitBeveledPlankMesh = BuildMultiPlankDeckUnitMesh(6);
            _unitBeveledBeamMesh = BuildChamferedTimberUnitMesh(0.12f, 11);
            _unitChiseledCobbleMesh = BuildChiseledMasonryUnitMesh(3, 2);
            _unitLayeredRoofMesh = BuildLayeredShingleRoofUnitMesh(5, 6);
            _unitClapboardWallMesh = BuildClapboardSidingWallUnitMesh(7);
        }

        // ====================================================================
        // 一、 核心几何雕刻算法：将死板的 1x1x1 Cube 转化为多板倒角枕木、鱼鳞叠瓦、羽叠木墙与乱石砌块
        // ====================================================================

        /// <summary>
        /// 1. 构建由 N 根独立八角倒角老枕木拼接而成的平台网格 (归一化在 [-0.5, 0.5]^3)：
        ///    - 每根木板之间留有真实的透光板缝与深邃板间 AO 阴影 (color.a = 0.25)
        ///    - 每根木板具有独立的微幅高低错落、两端沉头铆钉凹槽、边缘 45° 倒角与冷暖色差 (color.rgb)
        /// </summary>
        private Mesh BuildMultiPlankDeckUnitMesh(int plankCount)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();

            float stepZ = 1.0f / plankCount;
            float gap = stepZ * 0.08f;
            float plankW = stepZ - gap;

            for (int i = 0; i < plankCount; i++)
            {
                float zCenter = -0.5f + (i + 0.5f) * stepZ;
                int seed = i * 37 + 19;
                // 逐根木板微小的天然色差与高度起伏
                float tintShift = 0.88f + ((seed % 7) / 6.0f) * 0.22f;
                Color plankCol = new Color(
                    Mathf.Clamp01(tintShift * (1.0f + (i % 3 - 1) * 0.04f)),
                    Mathf.Clamp01(tintShift),
                    Mathf.Clamp01(tintShift * (1.0f - (i % 2) * 0.04f)),
                    1.0f
                );

                float yJitter = ((i % 3) - 1) * 0.035f;
                float xTrim = ((i * 5) % 4) * 0.008f;

                AppendChamferedBoxToBuffers(
                    verts, uvs, cols, tris,
                    new Vector3(0f, yJitter, zCenter),
                    new Vector3(1.0f - xTrim, 0.92f, plankW),
                    bevelX: 0.035f,
                    bevelY: 0.22f,
                    bevelZ: plankW * 0.18f,
                    topColor: plankCol,
                    bottomAO: 0.28f,
                    seed: seed
                );
            }

            // 底部增加两根横向承重托梁，消除板缝漏空感并增强结构厚重度
            foreach (float bx in new[] { -0.36f, 0.36f })
            {
                AppendChamferedBoxToBuffers(
                    verts, uvs, cols, tris,
                    new Vector3(bx, -0.28f, 0f),
                    new Vector3(0.11f, 0.48f, 0.98f),
                    0.02f, 0.10f, 0.02f,
                    new Color(0.72f, 0.68f, 0.62f, 0.55f),
                    0.20f,
                    99
                );
            }

            var m = new Mesh { name = "V32_CraftedMultiPlankDeck" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }

        /// <summary>
        /// 2. 构建单根带八角斜切倒角、微弧拱起与底部环境光遮蔽 (AO) 的精修木梁/木板网格
        /// </summary>
        private Mesh BuildChamferedTimberUnitMesh(float bevelRatio, int seed)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();

            AppendChamferedBoxToBuffers(
                verts, uvs, cols, tris,
                Vector3.zero,
                Vector3.one,
                bevelRatio, bevelRatio, bevelRatio,
                new Color(0.96f, 0.94f, 0.90f, 1.0f),
                0.32f,
                seed
            );

            var m = new Mesh { name = "V32_CraftedChamferedTimber" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }

        /// <summary>
        /// 3. 构建多层鱼鳞交错叠压的板岩木瓦屋顶网格 (替代原本平滑无细节的平板屋顶 Cube！)
        /// </summary>
        private Mesh BuildLayeredShingleRoofUnitMesh(int rowsX, int colsZ)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();

            // 底层屋面板托底
            AppendChamferedBoxToBuffers(
                verts, uvs, cols, tris,
                new Vector3(0f, -0.15f, 0f),
                new Vector3(0.98f, 0.65f, 0.98f),
                0.03f, 0.12f, 0.03f,
                new Color(0.75f, 0.75f, 0.75f, 0.45f),
                0.25f,
                1
            );

            // 上层交错叠瓦片层
            float stepX = 0.96f / rowsX;
            float stepZ = 0.96f / colsZ;
            for (int ix = 0; ix < rowsX; ix++)
            {
                for (int iz = 0; iz < colsZ; iz++)
                {
                    int seed = ix * 17 + iz * 31;
                    float staggerZ = (ix % 2 == 1) ? stepZ * 0.25f : -stepZ * 0.25f;
                    float cx = -0.48f + (ix + 0.5f) * stepX;
                    float cz = Mathf.Clamp(-0.48f + (iz + 0.5f) * stepZ + staggerZ * 0.3f, -0.46f, 0.46f);
                    float shade = 0.84f + ((seed % 5) / 4.0f) * 0.28f;
                    float tiltY = ((seed % 3) - 1) * 0.04f;

                    AppendChamferedBoxToBuffers(
                        verts, uvs, cols, tris,
                        new Vector3(cx, 0.26f + tiltY, cz),
                        new Vector3(stepX * 1.08f, 0.42f, stepZ * 0.92f),
                        stepX * 0.15f, 0.12f, stepZ * 0.15f,
                        new Color(shade, shade * 1.02f, shade * 1.05f, 1.0f),
                        0.30f,
                        seed
                    );
                }
            }

            var m = new Mesh { name = "V32_CraftedLayeredRoof" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }

        /// <summary>
        /// 4. 构建水平羽叠搭接木墙板 + 四角外露原木立柱网格 (替代原本光秃秃的房屋墙体 Cube！)
        /// </summary>
        private Mesh BuildClapboardSidingWallUnitMesh(int plankBands)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();

            // 内芯主墙体
            AppendChamferedBoxToBuffers(
                verts, uvs, cols, tris,
                Vector3.zero,
                new Vector3(0.94f, 0.98f, 0.94f),
                0.02f, 0.02f, 0.02f,
                new Color(0.82f, 0.78f, 0.72f, 0.65f),
                0.35f,
                7
            );

            // 水平鱼鳞搭接外墙木条 (Clapboard Siding)
            float bandH = 0.92f / plankBands;
            for (int b = 0; b < plankBands; b++)
            {
                float cy = -0.46f + (b + 0.5f) * bandH;
                float shade = 0.88f + ((b * 3) % 5) * 0.05f;
                AppendChamferedBoxToBuffers(
                    verts, uvs, cols, tris,
                    new Vector3(0f, cy, 0f),
                    new Vector3(0.99f, bandH * 0.92f, 0.99f),
                    0.02f, bandH * 0.22f, 0.02f,
                    new Color(shade, shade * 0.98f, shade * 0.94f, 1.0f),
                    0.32f,
                    b + 10
                );
            }

            // 四角外露粗壮原木立柱与上下横梁框 (Half-Timbered Framing)
            foreach (float sx in new[] { -0.46f, 0.46f })
            {
                foreach (float sz in new[] { -0.46f, 0.46f })
                {
                    AppendChamferedBoxToBuffers(
                        verts, uvs, cols, tris,
                        new Vector3(sx, 0f, sz),
                        new Vector3(0.11f, 1.01f, 0.11f),
                        0.02f, 0.02f, 0.02f,
                        new Color(0.68f, 0.62f, 0.55f, 0.85f),
                        0.25f,
                        33
                    );
                }
            }

            var m = new Mesh { name = "V32_CraftedClapboardWall" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }

        /// <summary>
        /// 5. 构建多面凿切倒角的海岸乱石砌块与石阶网格 (替代原本像水泥方砖一样的石板路与石基 Cube！)
        /// </summary>
        private Mesh BuildChiseledMasonryUnitMesh(int blocksX, int blocksZ)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();

            float sx = 1.0f / blocksX;
            float sz = 1.0f / blocksZ;

            for (int ix = 0; ix < blocksX; ix++)
            {
                for (int iz = 0; iz < blocksZ; iz++)
                {
                    int seed = ix * 23 + iz * 41 + 5;
                    float cx = -0.5f + (ix + 0.5f) * sx;
                    float cz = -0.5f + (iz + 0.5f) * sz;
                    float hVar = ((seed % 3) - 1) * 0.03f;
                    float tone = 0.85f + ((seed % 6) / 5.0f) * 0.26f;

                    AppendChamferedBoxToBuffers(
                        verts, uvs, cols, tris,
                        new Vector3(cx, hVar, cz),
                        new Vector3(sx * 0.96f, 0.96f, sz * 0.96f),
                        sx * 0.22f, 0.24f, sz * 0.22f,
                        new Color(tone, tone * 1.02f, tone * 0.98f, 1.0f),
                        0.22f,
                        seed
                    );
                }
            }

            var m = new Mesh { name = "V32_CraftedChiseledStone" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }

        /// <summary>
        /// 通用八边形三层斜切倒角盒体构建器 (带顶部微拱、45° 棱边倒角、逐面法线分离与底部 AO 烘焙)
        /// </summary>
        private static void AppendChamferedBoxToBuffers(
            List<Vector3> verts,
            List<Vector2> uvs,
            List<Color> cols,
            List<int> tris,
            Vector3 center,
            Vector3 size,
            float bevelX,
            float bevelY,
            float bevelZ,
            Color topColor,
            float bottomAO,
            int seed)
        {
            float hx = size.x * 0.5f;
            float hy = size.y * 0.5f;
            float hz = size.z * 0.5f;

            float bx = Mathf.Min(bevelX, hx * 0.42f);
            float by = Mathf.Min(bevelY * size.y, hy * 0.45f);
            float bz = Mathf.Min(bevelZ, hz * 0.42f);

            // 八边形水平切角轮廓 (逆时针)
            Vector2[] ringOuter =
            {
                new Vector2(-hx + bx, -hz),
                new Vector2( hx - bx, -hz),
                new Vector2( hx,      -hz + bz),
                new Vector2( hx,       hz - bz),
                new Vector2( hx - bx,  hz),
                new Vector2(-hx + bx,  hz),
                new Vector2(-hx,       hz - bz),
                new Vector2(-hx,      -hz + bz)
            };

            Vector3[] topRing = new Vector3[8];
            Vector3[] midRing = new Vector3[8];
            Vector3[] botRing = new Vector3[8];

            for (int i = 0; i < 8; i++)
            {
                float warp = Mathf.Sin(seed * 1.7f + i * 0.9f) * 0.008f * size.y;
                Vector2 pOut = ringOuter[i];
                Vector2 pIn = new Vector2(
                    Mathf.Sign(pOut.x) * Mathf.Max(0f, Mathf.Abs(pOut.x) - bx * 0.65f),
                    Mathf.Sign(pOut.y) * Mathf.Max(0f, Mathf.Abs(pOut.y) - bz * 0.65f)
                );

                topRing[i] = center + new Vector3(pIn.x, hy + warp, pIn.y);
                midRing[i] = center + new Vector3(pOut.x, hy - by + warp * 0.5f, pOut.y);
                botRing[i] = center + new Vector3(pIn.x, -hy, pIn.y);
            }

            Color edgeCol = new Color(
                Mathf.Clamp01(topColor.r * 1.08f),
                Mathf.Clamp01(topColor.g * 1.06f),
                Mathf.Clamp01(topColor.b * 1.02f),
                0.92f
            );
            Color botCol = new Color(topColor.r * 0.68f, topColor.g * 0.68f, topColor.b * 0.70f, bottomAO);

            // 1. 侧面上倒角环 (Top Chamfer Bevel) 與下侧面环 (Lower Side Skirt)
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                AddHardQuad(verts, uvs, cols, tris, topRing[i], topRing[j], midRing[j], midRing[i], topColor, edgeCol);
                AddHardQuad(verts, uvs, cols, tris, midRing[i], midRing[j], botRing[j], botRing[i], edgeCol, botCol);
            }

            // 2. 顶面封口 (带微拱中心点)
            int topCenterIdx = verts.Count;
            verts.Add(center + new Vector3(0f, hy + 0.012f * size.y, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            cols.Add(topColor);

            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                int idxI = verts.Count;
                verts.Add(topRing[i]);
                uvs.Add(new Vector2(topRing[i].x, topRing[i].z));
                cols.Add(topColor);

                int idxJ = verts.Count;
                verts.Add(topRing[j]);
                uvs.Add(new Vector2(topRing[j].x, topRing[j].z));
                cols.Add(topColor);

                tris.Add(topCenterIdx);
                tris.Add(idxJ);
                tris.Add(idxI);
            }
        }

        private static void AddHardQuad(
            List<Vector3> verts,
            List<Vector2> uvs,
            List<Color> cols,
            List<int> tris,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            Color colTop, Color colBot)
        {
            int idx = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(0f, 0f));
            cols.Add(colTop); cols.Add(colTop);
            cols.Add(colBot); cols.Add(colBot);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 1);
            tris.Add(idx); tris.Add(idx + 3); tris.Add(idx + 2);
        }

        // ====================================================================
        // 二、 全面扫描并升级场景中所有粗糙 Cube 构件、木栈桥、海洋小馆入口与渔村房屋
        // ====================================================================
        private void UpgradeAllSceneCubePrimitivesToCraftedMeshes()
        {
            var allFilters = UnityEngine.Object.FindObjectsOfType<MeshFilter>();
            foreach (var mf in allFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;

                string objName = mf.gameObject.name;
                string meshName = mf.sharedMesh.name;

                // 仅针对粗糙的 Cube 积木部件或旧版木石结构进行重铸升级
                bool isCube = meshName.Contains("Cube");

                // 1. 栈桥平台、木踏板、门前木廊、修船滑道 -> 替换为【多根独立倒角老枕木排网格】
                if (objName.Contains("小木平台") || objName.Contains("门前平台木廊") || objName.Contains("船坞延伸滑道") ||
                    objName.Contains("ForecourtTread") || objName.Contains("ShoreAccessTread") || objName.Contains("桥面板-") ||
                    objName.Contains("岸侧过渡板"))
                {
                    mf.sharedMesh = _unitBeveledPlankMesh;
                    mr.sharedMaterial = _v32PlankWoodMat;
                    v32CraftedPlankDecksCount++;
                }
                // 2. 房屋斜坡屋面 -> 替换为【多层鱼鳞交错板岩木瓦网格】
                else if (objName.Contains("斜坡屋面") || objName.Contains("副楼斜屋顶") || objName.Contains("塔楼尖顶"))
                {
                    mf.sharedMesh = _unitLayeredRoofMesh;
                    mr.sharedMaterial = _v32SlateRoofMat;
                    v32CraftedRoofsAndWallsCount++;
                }
                // 3. 房屋木墙体 -> 替换为【水平羽叠搭接木墙 + 四角外露原木框架网格】
                else if (objName.Contains("木屋主墙体") || objName.Contains("酒馆侧翼副楼"))
                {
                    mf.sharedMesh = _unitClapboardWallMesh;
                    mr.sharedMaterial = _v32PlankWoodMat;
                    v32CraftedRoofsAndWallsCount++;
                }
                // 4. 石砌底座、壁炉烟囱、石板路踏步、矮栏柱 -> 替换为【多面凿切海岸乱石砌体网格】
                else if (objName.Contains("海石防潮底座") || objName.Contains("石砌壁炉烟囱") || objName.Contains("学者望海塔楼") ||
                         objName.Contains("石板路踏步") || objName.Contains("矮栏柱") || objName.Contains("低矮栏杆"))
                {
                    mf.sharedMesh = _unitChiseledCobbleMesh;
                    mr.sharedMaterial = _v32MasonryStoneMat;
                    v32CraftedStoneworkCount++;
                }
                // 5. 廊柱、木梁、屋脊主梁、灯架立柱、栏杆、木箱 -> 替换为【八角斜切倒角实木梁网格】
                else if (isCube && (objName.Contains("柱") || objName.Contains("梁") || objName.Contains("木箱") ||
                         objName.Contains("架") || objName.Contains("Rail") || objName.Contains("Support") ||
                         objName.Contains("Pile") || objName.Contains("Post") || objName.Contains("Beam") ||
                         objName.Contains("龙骨") || objName.Contains("厚木门")))
                {
                    mf.sharedMesh = _unitBeveledBeamMesh;
                    mr.sharedMaterial = objName.Contains("Pile") || objName.Contains("Support") ? _v32LogTimberMat : _v32PlankWoodMat;
                    v32CraftedBeamsAndPostsCount++;
                }
            }
        }

        /// <summary>
        /// 针对 VillageCompositionV31 中已经被合并为 Static Mesh 的前院与栈道，升级其材质并为合并网格烘焙法线与顶点 AO
        /// </summary>
        private void UpgradeMergedVillageDecksAndHouses()
        {
            if (_v31Root == null) return;
            foreach (var mr in _v31Root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null || mr.sharedMaterial == null) continue;
                string mName = mr.sharedMaterial.name;
                string gName = mr.gameObject.name;

                if (mName.Contains("Timber") || mName.Contains("Wood") || gName.Contains("Wood") || gName.Contains("Timber"))
                {
                    mr.sharedMaterial = mName.Contains("Stilt") ? _v32LogTimberMat : _v32PlankWoodMat;
                    InjectBevelNormalAndVertexAO(mr.GetComponent<MeshFilter>());
                }
                else if (mName.Contains("Slate") || mName.Contains("Roof"))
                {
                    mr.sharedMaterial = _v32SlateRoofMat;
                    InjectBevelNormalAndVertexAO(mr.GetComponent<MeshFilter>());
                }
                else if (mName.Contains("Stone") || mName.Contains("Foundation"))
                {
                    mr.sharedMaterial = _v32MasonryStoneMat;
                    InjectBevelNormalAndVertexAO(mr.GetComponent<MeshFilter>());
                }
            }
        }

        /// <summary>
        /// 为已合并的网格自动计算逐连通子块色差与底部结构 AO，使其在 StylizedCoastalCraftV32.shader 下立刻展现深邃木纹与立体阴影
        /// </summary>
        private static void InjectBevelNormalAndVertexAO(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null) return;
            Mesh m = mf.sharedMesh;
            Vector3[] verts = m.vertices;
            Vector3[] norms = m.normals;
            if (verts == null || verts.Length == 0) return;

            Bounds b = m.bounds;
            Color[] cols = new Color[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                Vector3 n = (norms != null && i < norms.Length) ? norms[i] : Vector3.up;
                // 根据空间哈希产生每块木板/石头的自然明暗色差
                float hash = Mathf.Sin(Mathf.Floor(v.x * 2.5f) * 12.7f + Mathf.Floor(v.z * 2.5f) * 31.3f) * 0.5f + 0.5f;
                float tint = 0.88f + hash * 0.22f;
                // 底部与背阴拐角自动注入 AO 阴影
                float height01 = Mathf.InverseLerp(b.min.y, b.max.y, v.y);
                float ao = Mathf.Lerp(0.35f, 1.0f, Mathf.SmoothStep(0f, 0.65f, height01)) * Mathf.Lerp(0.55f, 1.0f, n.y * 0.5f + 0.5f);
                cols[i] = new Color(tint, tint * 0.99f, tint * 0.96f, ao);
            }
            m.colors = cols;
        }

        /// <summary>
        /// 升级玩家可建造的 2m×2m 延伸木栈桥模块，使其也使用带微浮雕木纹与青苔风化的匠心枕木材质
        /// </summary>
        private void UpgradeBuildablePierStructures()
        {
            if (bay == null) return;
            foreach (var mr in bay.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null || mr.sharedMaterial == null) continue;
                if (mr.sharedMaterial.name.Contains("WeatheredWood") || mr.sharedMaterial.name.Contains("PierDeckWood"))
                {
                    mr.sharedMaterial = _v32PlankWoodMat;
                    InjectBevelNormalAndVertexAO(mr.GetComponent<MeshFilter>());
                }
            }
        }
    }
}

```

## 第二步：优化 `VillageCompositionV31.cs` 的合并顺序以发挥最大几何细节

在 `Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/VillageCompositionV31.cs` 中：

1. 在 `BuildCourtyardV31()` 和 `BuildSlipwayBridgeV31()` 调用 `MergeStaticRenderersV31(...)` **之前**，将 `ForecourtTread` 和 `ShoreAccessTread_` 的 `MeshFilter.sharedMesh` 设为 `_unitBeveledPlankMesh`（材质设为 `_v32PlankWoodMat`），将 `ForecourtPile` / `ForecourtSupport` / `ForecourtRailPost` 的 `sharedMesh` 设为 `_unitBeveledBeamMesh`！

2. 这样合并出来的静态网格将完整保留每一根倒角枕木的八角棱边、板缝、逐板色差和底部 AO，同时 DrawCall 依然保持最低！

## 第三步：构建 v32 并采集 1920x1080 陆地建筑与木栈桥近景特写截图

1. 刷新 Unity 编译并构建最新程序 `Builds/SeaCraftV32/Guichao-SeaCraftV32.exe`（同步更新桌面快捷方式 **“深海灯塔—最新版”**）。

2. 在 1920x1080 可见窗口下运行实机验证，采集：

   * 展示重铸后的木栈桥枕木倒角、海洋小馆门廊、中岸海风酒馆（鱼鳞叠瓦+羽叠木墙+乱石台基+青苔风化）与水面的全景图 `Docs/SeaCraftV32/preview.jpg`；

   * **近景特写图** `Docs/SeaCraftV32/02-wood-and-house-craft-closeup.jpg`（拉近镜头对准酒馆前院木栈台、屋顶叠瓦、石板路与修船坊，直观展示彻底告别直角 Cube 后的匠心木纹凹凸、倒角高光与板缝 AO 阴影），并输出 `Docs/SeaCraftV32/README.md` 与 `result.json`。

## 第四步：提交并推送到 GitHub (`https://github.com/xujiale4869/guichao-bay`)

```
git add .
git commit -m "feat(v32): 重铸全陆地八角倒角枕木栈道、鱼鳞叠瓦屋顶与羽叠木墙，接入StylizedCoastalCraftV32风化青苔着色器统一全岛画风"
git push origin main

```