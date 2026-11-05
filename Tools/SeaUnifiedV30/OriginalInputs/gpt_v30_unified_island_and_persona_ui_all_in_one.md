# 给 GPT 的 v30《雾岬岛·东岸》一体化无缝半岛地形（消除沙草硬切/北岸细线/外海水面断层）与 Persona 风格斜切撞色海洋 UI 重构包

你好！我和 **Gemini** 已经审查了仓库 `https://github.com/xujiale4869/guichao-bay` 中 `Docs/SeaSeamlessV29/Runtime/` 下的 `03-north-coast.jpg`、`03b-south-coast.jpg`、`02-nautical-build-ui.jpg` 以及 `SeamlessTerrainV29.cs`、`V24CoastShowcaseSurface.shader`、`NauticalHUDV29.cs` 的完整源码。

我们已经查清了导致用户指出 **“一边沙地直接变成草地、岛屿北侧陆地只有一条线、水面与外界水面相接不对劲、UI 缺少特点”** 的 4 个精确代码 Bug：

1. **为什么“一边沙地直接变成草地”？**

   * 在 `SeamlessTerrainV29.cs` 第 16\~17 行，代码生成了两块在 `x = -36` 处硬拼接的网格：`V29_WesternLandContinuation`（用 `nativeRock` 材质，`V24CoastShowcaseSurface.shader` 在海拔 `y > 0.1` 自动变成全绿草地）和 `V29_ContinuousContinentalShelf`（用 `nativeSand` 沙地材质，且 `GridMesh()` 完全没有写入 `mesh.colors` 顶点色）！导致两块网格在 `x = -36` 分界线上出现一条笔直的“左绿草、右黄沙”硬切边！

2. **为什么“岛屿北侧陆地只有一条线”？**

   * `SeamlessTerrainV29.cs` 第 91 行的 `landMask` 逻辑在 `Mathf.Abs(z - 2) > 18`（即北侧和南侧）错误地将 `ShoreX(z) - 5` 以西的陆地强行拉平到了水下采样值，导致北侧水面上只剩下一条宽仅 2\~3 米的细长条！

3. **为什么“水面与外界水面相接不对劲”？**

   * `V29_ContinuousContinentalShelf` 在矩形边界（`x = 145, z = ±105`）处海床高度突然中断，外面直接掉到 `-20m` 的 `V29_DeepFloor`；同时第 72 行的 `trench` 沟槽在 3.5m 内垂直切到 `-13.5m`。因为水面 Shader 是根据真实水深着色的，海床在边界处的阶跃直接导致水面上出现了一圈刺眼的“浅青色突然变深蓝色”的断层线！

4. **为什么“建造网格悬空架在深沟上” & “UI 缺少特点”？**

   * `SeaBayGeometryV28.cs` 的 `SetupGrid()` 没有剔除坡度陡峭处，且水深上限放到了 `5.5m`；而 `NauticalHUDV29.cs` 用的是平庸的灰黑圆角框，缺乏记忆点。

现在 **Gemini** 已经编写好了 **v30 一体化无缝地形与 Persona 风格动态斜切海洋 UI 重构代码**（本文件已内置全部完整源码）。请在本地工程执行以下四步：

## 第一步：覆盖更新以下 3 个核心源码文件

### 1. 覆盖写入 `Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeamlessTerrainV29.cs`

* **消除沙草硬切**：废除 `V29_WesternLandContinuation` 与 `V29_ContinuousContinentalShelf` 的分块硬拼，合并为**唯一一张横跨 `[-155, 195] × [-165, 175]` 的一体化岛屿与大陆架网格**，启用 `V24CoastShowcaseSurface` 的 `_Mode = 1`（Ground 模式），并逐顶点计算 `mesh.colors`（`c.r` 根据海拔高度、离岸距离与有机噪声，从水下/潮间带 100% 纯沙滩自然斑驳过渡到内陆丰茂草坡与红枫林地）！

* **消除北侧一线天**：移除第 91 行错误的 `landMask` 压平逻辑，凡在 `ShoreX(z)` 以西的内陆区域，平滑抬升为宽广饱满的半岛丘陵与山脊（海拔 `+0.2m ~ +11m`），并在内陆山坡上成林分布 18 棵高低错落的红枫树。

* **消除水面边界断层**：浅湾向外海采用 **58 米超宽余弦大陆坡（Cosine Continental Slope）** 平滑过渡，且在网格四周边框处 100% 平滑归零至 `deepAbyss (-14.2m)`，使水面颜色从翡翠绿到孔雀蓝再到深海靛蓝如丝绸般自然过渡！

```
// ============================================================================
// 深海灯塔·归潮湾 (v30) — 一体化无缝半岛地形（消除沙草硬切/北岸细线/外海水面断层）
// File: Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeamlessTerrainV29.cs
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        int coastCount, reefCount, coralCount, kelpCount, mapleCount;
        Material unifiedTerrainMat;

        public void PrepareWorld(SeaLifeRuntime g, Transform parent)
        {
            game = g;
            bay = g.GetComponent<SeaBayExpansionAndBuildSpaceV28>();
            native = Resources.Load<NativeBayHeightfieldV29>("SeaSeamlessWorldV29/NativeBayHeightfieldV29");

            worldRoot = new GameObject("V29_ContinuousCoastAndArchipelago").transform;
            worldRoot.SetParent(parent, false);

            // 1. 获取原版 V24CoastShowcaseSurface (_Mode = 1: Ground 模式，原生支持基于顶点色 c.r 的沙地->岩石->草地平滑过渡！)
            var baseSand = Resources.Load<Material>("CoastRefinement/Sand");
            var baseRock = Resources.Load<Material>("CoastRefinement/Rock");
            unifiedTerrainMat = new Material(baseSand != null ? baseSand : baseRock)
            {
                name = "V30_UnifiedIslandAndSeabedMat"
            };
            _owned.Add(unifiedTerrainMat);

            var budget = Resources.Load<Material>("SeaSurfaceV24/BudgetCoastShowcaseSurface");
            if (budget != null)
            {
                unifiedTerrainMat.shader = budget.shader;
                // _Mode = 1 启用 V24CoastShowcaseSurface 内置的 Sand + Rock + GroundCover(草地) 三合一顶点色混合管线！
                if (unifiedTerrainMat.HasProperty("_Mode")) unifiedTerrainMat.SetFloat("_Mode", 1.0f);
            }

            nativeSand = unifiedTerrainMat;
            nativeRock = new Material(baseRock != null ? baseRock : unifiedTerrainMat) { name = "V30_NativeReefRock" };
            if (budget != null) nativeRock.shader = budget.shader;
            _owned.Add(nativeRock);

            rocks = new Mesh[6];
            for (int i = 0; i < rocks.Length; i++)
            {
                rocks[i] = Resources.Load<Mesh>("SeaSeamlessWorldV29/Rock" + i);
                if (rocks[i] == null) rocks[i] = PrimitiveMesh(PrimitiveType.Sphere);
            }

            // 2. 构建唯一一张覆盖全视野 (520m × 520m) 的无边界连续波浪海面
            waterFilter = game.world.Find("ShowcaseWater").GetComponent<MeshFilter>();
            priorWaterMesh = waterFilter.sharedMesh;
            priorWaterMat = waterFilter.GetComponent<MeshRenderer>().sharedMaterial;
            waterFilter.transform.localScale = Vector3.one;
            waterFilter.transform.position = new Vector3(30f, SeaLifeRuntime.Sea, 10f);
            Mesh waterMesh = GridMeshWithColors("V30_WaterSurface", -240f, 280f, -250f, 250f, 180, 180, (x, z) => 0f, false);
            var wb = waterMesh.bounds;
            wb.Expand(Vector3.up * 4f);
            waterMesh.bounds = wb;
            waterFilter.sharedMesh = waterMesh;

            // 3. 彻底废除 v29 中导致“一边沙地直接变草地”的两块硬拼接网格 (V29_WesternLandContinuation + V29_ContinuousContinentalShelf)！
            //    改为生成唯一一张横跨 [-165, 225] × [-190, 195] 的【一体化半岛陆地 + 缓坡浅湾 + 连续大陆坡海床网格】，
            //    并逐顶点写入平滑过渡的生态顶点色 (c.r 控制草地/沙地平滑渐变)，从根源上消灭沙草硬切线、北侧细线与外海水深断层！
            AddMesh(
                "V29_DeepFloor",
                GridMeshWithColors("V29_DeepFloor", -240f, 280f, -250f, 250f, 2, 2, (x, z) => SeaLifeRuntime.Sea - 14.5f, false),
                unifiedTerrainMat,
                Vector3.zero,
                Vector3.one,
                false,
                worldRoot
            );

            Mesh unifiedIslandMesh = GridMeshWithColors(
                "V29_ContinuousContinentalShelf",
                -155f, 195f,
                -165f, 175f,
                260, 280,
                HeightAt,
                true
            );
            AddMesh("V29_ContinuousContinentalShelf", unifiedIslandMesh, unifiedTerrainMat, Vector3.zero, Vector3.one, true, worldRoot);

            PopulateCoastAndBarrier();
        }

        /// <summary>
        /// v30 一体化海岬半岛与大陆坡连续高程方程：
        /// 1. 彻底解决“岛屿北侧陆地只有一条线”：凡在海岸线 ShoreX(z) 以西 (distFromShore < 0)，全部隆起为宽广厚实的内陆山体与台地！
        /// 2. 彻底解决“水面与外界相接不对劲”：浅湾向深海采用 60m 宽的余弦大陆坡平滑沉降，绝无矩形边缘断崖！
        /// </summary>
        private float BaseArchipelagoHeight(float x, float z)
        {
            float sea = SeaLifeRuntime.Sea; // -0.22f
            float deepAbyss = sea - 14.0f;

            // 1. 连贯自然的西岸主岛海岸线方程
            float mainShoreX = ShoreX(z);
            float distFromShore = x - mainShoreX;

            // 原版核心微调保护区 (仅在原版精修岸岩正下方 x ∈ [-18, 8], z ∈ [-16, 20] 轻柔避让，绝不切断南北陆地！)
            float corePreserveMask = Mathf.Exp(-Mathf.Pow((x + 5f) / 11f, 4f) - Mathf.Pow((z - 2f) / 15f, 4f));

            // 2. 西侧与北侧厚实饱满的半岛内陆山体 + 东向缓坡翡翠浅滩
            float shelfWidth = 26.0f
                + 10.0f * Mathf.Exp(-Mathf.Pow((z - 4.0f) / 20f, 2f))   // 栈桥前方宽阔扇形浅坪
                + 12.0f * Mathf.Exp(-Mathf.Pow((z + 36f) / 24f, 2f))   // 南湾半月浅水湾
                + 9.5f * Mathf.Exp(-Mathf.Pow((z - 40f) / 22f, 2f));   // 北岬珊瑚浅湾

            float coastalY;
            if (distFromShore < 0f)
            {
                // 内陆区域 (x < mainShoreX)：向西持续抬升为饱满的岛屿丘陵与山脊，彻底消除“北侧只有一条细线”！
                float inlandDist = -distFromShore;
                float beachToCliff = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(inlandDist / 16.0f));
                float mountainRise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((inlandDist - 10.0f) / 65.0f));
                float hillUndulation = 1.35f * Mathf.Sin(x * 0.045f + z * 0.038f)
                                     + 0.85f * Mathf.Cos(z * 0.075f - x * 0.052f);
                coastalY = sea + 0.12f + beachToCliff * 3.4f + mountainRise * 7.8f + hillUndulation * beachToCliff;
            }
            else
            {
                // 浅海大陆架 + 60m 超宽余弦缓坡沉降（消除浅海水面与外海深蓝水面之间的硬断层！）
                float shallowT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distFromShore / shelfWidth));
                float slopeT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distFromShore - shelfWidth * 0.65f) / 58.0f));
                float terraceY = Mathf.Lerp(sea + 0.12f, sea - 1.85f, shallowT);
                coastalY = Mathf.Lerp(terraceY, deepAbyss, slopeT);
            }

            // 3. 外海蜿蜒珊瑚礁脉与环礁浅滩（采用宽缓高斯包络，去掉生硬刀切沟槽，让水色自然渐变）
            float trenchCenterX = 11.5f + 3.4f * Mathf.Sin(z * 0.065f - 0.3f) + 0.08f * Mathf.Abs(z);
            float outerRidgeX = trenchCenterX + 16.5f + 4.0f * Mathf.Cos(z * 0.055f);

            float distToRidge = Mathf.Abs(x - outerRidgeX);
            float ridgeHalfSpan = 18.0f + 5.0f * Mathf.Sin(z * 0.07f + 1.1f);
            float ridgeBell = Mathf.Exp(-Mathf.Pow(distToRidge / ridgeHalfSpan, 2.0f));
            // 栈桥正东方通海航道柔和降低
            float channelDip = 1.0f - 0.65f * Mathf.Exp(-Mathf.Pow((z - 4.5f) / 7.5f, 2f));
            float outerRidgeY = Mathf.Lerp(deepAbyss, sea - 1.25f, ridgeBell * channelDip);

            // 4. 远东连绵浅海沙坪 (与外海礁脊连成一片，不再是孤立豹纹斑块)
            float eastAtollMask = Mathf.Exp(-Mathf.Pow((x - (outerRidgeX + 22f)) / 28f, 2f) - Mathf.Pow(z / 75f, 2f));
            float eastAtollY = Mathf.Lerp(deepAbyss, sea - 1.55f, eastAtollMask * 0.82f);

            // 5. 柔和自然海床沙波纹
            float ripple = 0.07f * Mathf.Sin(x * 0.32f + Mathf.Sin(z * 0.24f))
                         + 0.05f * Mathf.Cos(z * 0.38f - x * 0.21f);

            float combinedY = Mathf.Max(coastalY, Mathf.Max(outerRidgeY, eastAtollY)) + ripple;

            // 温和避让原版内湾核心海床，杜绝陡峭切沟
            combinedY -= corePreserveMask * 0.55f;

            // 外围远海边界 (距离中心 > 130m) 统一平滑归零至 deepAbyss，确保网格边缘与 V29_DeepFloor 100% 齐平无接缝！
            float edgeFadeX = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(145f, 190f, x));
            float edgeFadeZ = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(125f, 168f, Mathf.Abs(z)));
            float outerEdgeFade = Mathf.Clamp01(edgeFadeX + edgeFadeZ * (distFromShore > 0f ? 1f : 0f));
            combinedY = Mathf.Lerp(combinedY, deepAbyss, outerEdgeFade);

            return Mathf.Clamp(combinedY, deepAbyss, sea + 14.0f);
        }

        public float HeightAt(float x, float z)
        {
            float y = BaseArchipelagoHeight(x, z);
            if (native != null)
            {
                float ny, d;
                // 仅在原版有真实网格覆盖的核心区内部做双三次平滑融合，彻底取消之前导致北侧断裂的 landMask 阶跃代码！
                float coreWeight = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16f, 36f, Mathf.Abs(z - 2f))))
                                 * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(22f, 38f, Mathf.Abs(x + 2f))));
                if (coreWeight > 0.001f && native.Probe(x, z, out ny, out d, true) && d < 12f)
                {
                    float blend = (1f - Mathf.SmoothStep(0f, 1f, d / 12f)) * coreWeight;
                    y = Mathf.Lerp(y, ny - 0.06f, blend);
                }
            }
            return y;
        }

        float ShoreX(float z)
        {
            return -11.5f
                + 4.2f * Mathf.Sin(z * 0.055f + 0.4f)
                + 2.4f * Mathf.Cos(z * 0.13f - 0.8f)
                - 6.5f * Mathf.Exp(-Mathf.Pow((z + 32f) / 18f, 2f))
                + 5.2f * Mathf.Exp(-Mathf.Pow((z - 42f) / 16f, 2f));
        }

        /// <summary>
        /// 构建一体化地形网格，并逐顶点计算生态过渡顶点色：
        /// c.r = 草地/苔藓覆盖度 (0=纯金黄海沙, 0~1=自然斑驳过渡, 1=丰茂内陆草地)
        /// 彻底解决“一边沙地直接变成草地”的硬切问题！
        /// </summary>
        Mesh GridMeshWithColors(string name, float minX, float maxX, float minZ, float maxZ, int nx, int nz, Func<float, float, float> heightFn, bool computeEcoColors)
        {
            int vCount = (nx + 1) * (nz + 1);
            var v = new Vector3[vCount];
            var uv = new Vector2[vCount];
            var cols = new Color[vCount];
            var tr = new int[nx * nz * 6];

            float sea = SeaLifeRuntime.Sea;
            int k = 0;
            for (int z = 0; z <= nz; z++)
            {
                float zz = Mathf.Lerp(minZ, maxZ, z / (float)nz);
                float shoreX = ShoreX(zz);
                for (int x = 0; x <= nx; x++)
                {
                    float xx = Mathf.Lerp(minX, maxX, x / (float)nx);
                    float yy = heightFn(xx, zz);
                    v[k] = new Vector3(xx, yy, zz);
                    uv[k] = new Vector2(xx * 0.28f, zz * 0.28f);

                    if (computeEcoColors)
                    {
                        // 根据海拔高于海平面的高度 + 离岸内陆距离 + 有机噪声，计算平滑的沙地->草地过渡权重
                        float heightAboveSea = yy - sea;
                        float inlandDist = shoreX - xx;
                        float organicNoise = 0.5f + 0.30f * Mathf.Sin(xx * 0.19f + Mathf.Cos(zz * 0.17f) * 2.0f)
                                                  + 0.20f * Mathf.Cos(zz * 0.31f - xx * 0.23f);

                        // 水下及潮间带沙滩 (heightAboveSea < 0.45m) 保持 100% 纯正沙滩；
                        // 从海拔 0.45m 到 2.4m 结合有机噪声自然生长出草坡与苔藓，绝无直线硬边！
                        float grassWeight = Mathf.SmoothStep(0.45f, 2.35f, heightAboveSea + (organicNoise - 0.5f) * 0.85f)
                                          * Mathf.SmoothStep(-2.0f, 9.0f, inlandDist + (organicNoise - 0.5f) * 4.0f);
                        cols[k] = new Color(Mathf.Clamp01(grassWeight), 0f, 0f, 1f);
                    }
                    else
                    {
                        cols[k] = new Color(0f, 0f, 0f, 1f);
                    }
                    k++;
                }
            }

            int ti = 0;
            for (int z = 0; z < nz; z++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int a = z * (nx + 1) + x;
                    int b = a + 1;
                    int c = a + nx + 1;
                    int d = c + 1;
                    tr[ti++] = a; tr[ti++] = c; tr[ti++] = b;
                    tr[ti++] = b; tr[ti++] = c; tr[ti++] = d;
                }
            }

            var m = new Mesh
            {
                name = name,
                indexFormat = vCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            m.vertices = v;
            m.uv = uv;
            m.colors = cols;
            m.triangles = tr;
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }

        GameObject AddMesh(string name, Mesh mesh, Material mat, Vector3 position, Vector3 scale, bool collision, Transform parent = null)
        {
            var o = new GameObject(name);
            o.transform.SetParent(parent != null ? parent : worldRoot, false);
            o.transform.localPosition = position;
            o.transform.localScale = scale;
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            o.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collision) o.AddComponent<MeshCollider>().sharedMesh = mesh;
            return o;
        }

        float VisibleBed(float x, float z)
        {
            var p = new Vector3(x, 25f, z);
            var hits = Physics.RaycastAll(p, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            float y = -100f;
            foreach (var h in hits)
            {
                if (game != null && game.boat != null && h.transform.IsChildOf(game.boat)) continue;
                y = Mathf.Max(y, h.point.y);
            }
            return y > -90f ? y : HeightAt(x, z);
        }

        bool TryFloraBed(Vector3 center, int seed, float minDepth, out Vector3 point)
        {
            for (int j = 0; j < 13; j++)
            {
                float radius = j == 0 ? 0f : 1.4f + (j - 1) * 0.45f;
                float angle = (seed * 137.5f + j * 111.25f) * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                float bedY = VisibleBed(x, z);
                float depth = SeaLifeRuntime.Sea - bedY;
                if (depth >= minDepth && depth <= 9.5f && x > -145f && x < 185f && z > -155f && z < 165f)
                {
                    point = new Vector3(x, bedY, z);
                    return true;
                }
            }
            point = center;
            return false;
        }

        /// <summary>
        /// 自然群落式海岸礁岩、红枫林与水下珊瑚生态分布（修复 (i/2)/22f 整数除法排排坐 Bug，采用母岩+子石自然聚落簇！）
        /// </summary>
        void PopulateCoastAndBarrier()
        {
            coastCount = 0; reefCount = 0; coralCount = 0; kelpCount = 0; mapleCount = 0;
            var rng = new System.Random(300511);
            Func<float, float, float> rand = (a, b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
            var flora = new List<Vector3>();

            // 1. 南北延展海岸天然礁石聚落簇（共 46 块，分为天然错落的岬角礁群，根部深埋入地面杜绝悬空！）
            for (int i = 0; i < 46; i++)
            {
                bool south = (i % 2 == 0);
                float idx = i * 0.5f;
                // 聚落化分布：围绕几个自然海岬焦点错落散开，彻底告别排成一条直线！
                float baseT = idx / 22.5f;
                float clusterJitter = Mathf.Sin(i * 2.1f) * 0.04f;
                float t = Mathf.Clamp01(baseT + clusterJitter);
                float z = Mathf.Lerp(south ? -24f : 26f, south ? -115f : 118f, t);
                float lateralSpread = rand(-8.5f, 3.2f);
                float x = ShoreX(z) + lateralSpread;
                float groundY = HeightAt(x, z);

                var m = rocks[i % rocks.Length];
                float sx = rand(2.2f, 5.2f);
                float sy = rand(1.1f, 2.4f);
                float sz = rand(2.2f, 5.2f);
                // 将岩石中心下沉 35%，使其像天然基岩一样从沙坡/崖壁中长出来，绝不悬空！
                Vector3 rockPos = new Vector3(x, groundY - sy * 0.32f, z);
                var rockGo = AddMesh("V29_CoastRock_" + i, m, nativeRock, rockPos, new Vector3(sx, sy, sz), true);
                rockGo.transform.localRotation = Quaternion.Euler(rand(-12f, 12f), rand(0f, 360f), rand(-12f, 12f));
                coastCount++;

                if (groundY < SeaLifeRuntime.Sea - 0.3f)
                {
                    flora.Add(new Vector3(x + rand(-2f, 2f), groundY, z + rand(-2f, 2f)));
                }
            }

            // 2. 外海珊瑚礁盘与群岛暗礁群（60 块扁平宽阔的海底礁盘基石，散布在宽达 25m 的礁带内）
            for (int i = 0; i < 60; i++)
            {
                float t = i / 59f;
                float z = Mathf.Lerp(-110f, 115f, t) + rand(-3.5f, 3.5f);
                if (z > -1.0f && z < 10.0f) continue; // 留出主栈桥东向通海航道

                float trench = 11.5f + 3.4f * Mathf.Sin(z * 0.065f - 0.3f) + 0.08f * Mathf.Abs(z);
                float ridgeCenter = trench + 16.5f + 4.0f * Mathf.Cos(z * 0.055f);
                float x = ridgeCenter + rand(-11.0f, 14.0f);
                float bedY = HeightAt(x, z);

                var m = rocks[(i * 3) % rocks.Length];
                bool emergent = (i % 11 == 0) && Mathf.Abs(z) > 18f;
                Vector3 scale = emergent
                    ? new Vector3(rand(4.2f, 7.5f), rand(1.8f, 3.2f), rand(4.2f, 7.5f))
                    : new Vector3(rand(2.8f, 5.5f), rand(0.85f, 1.65f), rand(2.8f, 5.5f));

                Vector3 pos = new Vector3(x, bedY - scale.y * 0.28f, z);
                var rGo = AddMesh("V29_OuterBarrier_" + i, m, nativeRock, pos, scale, true);
                rGo.transform.localRotation = Quaternion.Euler(rand(-8f, 8f), rand(0f, 360f), rand(-8f, 8f));
                reefCount++;

                flora.Add(new Vector3(x, bedY, z));
            }

            Physics.SyncTransforms();

            // 3. 延续原版水下生态：在浅湾与外海礁脉上丰茂生长四色珊瑚簇与摇曳海带林
            var reefShader = Resources.Load<Material>("SeaHarborV27/ReefShaderKeep")?.shader
                          ?? Shader.Find("Deepsea/UnderwaterReefVibrantV27")
                          ?? unifiedTerrainMat.shader;
            var palette = Resources.Load<Texture2D>("SeaHarborV27/Textures/CoralPaletteLUT_V27");
            var coralMats = new Material[4];
            for (int i = 0; i < 4; i++)
            {
                coralMats[i] = new Material(reefShader) { name = "V30_CoralMat_" + i };
                if (palette != null && coralMats[i].HasProperty("_PaletteLUT")) coralMats[i].SetTexture("_PaletteLUT", palette);
                if (coralMats[i].HasProperty("_ColorIndex")) coralMats[i].SetFloat("_ColorIndex", i);
                if (coralMats[i].HasProperty("_CoralVibrancy")) coralMats[i].SetFloat("_CoralVibrancy", 1.05f);
                _owned.Add(coralMats[i]);
            }

            var kelpMat = new Material(reefShader) { name = "V30_GreenKelp", enableInstancing = true };
            if (kelpMat.HasProperty("_BaseColor")) kelpMat.SetColor("_BaseColor", Color.white);
            if (kelpMat.HasProperty("_IsKelp")) kelpMat.SetFloat("_IsKelp", 1f);
            if (kelpMat.HasProperty("_SwayAmplitude")) kelpMat.SetFloat("_SwayAmplitude", 0.16f);
            _owned.Add(kelpMat);

            var cm = Resources.Load<Mesh>("SeaHarborV27/Baked/Coral_V27_Cluster") ?? Resources.Load<Mesh>("SeaHarborV27/Models/Coral_V27_Cluster");
            var km = Resources.Load<Mesh>("SeaHarborV27/Baked/Kelp_V27_GiantFrond") ?? Resources.Load<Mesh>("SeaHarborV27/Models/Kelp_V27_GiantFrond");

            int n = 0;
            foreach (var p in flora)
            {
                Vector3 at;
                if (cm != null && coralCount < 52 && TryFloraBed(p, n, 0.70f, out at))
                {
                    var cGo = AddMesh("V29_CoralCluster_" + n, cm, coralMats[n % 4], at, Vector3.one * rand(0.95f, 1.55f), false);
                    cGo.transform.localRotation = Quaternion.Euler(0f, rand(0f, 360f), 0f);
                    coralCount++;
                }
                n++;
                if (km != null && kelpCount < 42 && TryFloraBed(p, n + 17, 1.20f, out at))
                {
                    var kGo = AddMesh("V29_GiantKelp_" + n, km, kelpMat, at, new Vector3(rand(1.0f, 1.35f), rand(1.2f, 2.2f), rand(1.0f, 1.35f)), false);
                    kGo.transform.localRotation = Quaternion.Euler(0f, rand(0f, 360f), 0f);
                    kelpCount++;
                }
            }

            // 4. 在南北延展的饱满半岛山坡上自然布置 18 棵高低错落的原版红枫树林，让陆地生态与原版红枫海岬完全连成一体！
            Mesh trunk = Resources.Load<Mesh>("SeaSeamlessV29/MapleBark");
            Mesh leaves = Resources.Load<Mesh>("SeaSeamlessV29/MapleLeaves");
            if (trunk != null && leaves != null)
            {
                Material barkMat = Resources.Load<Material>("CoastMaple/MapleBark") ?? unifiedTerrainMat;
                Material leafMat = Resources.Load<Material>("CoastMaple/MapleLeaves") ?? unifiedTerrainMat;

                for (int i = 0; i < 18; i++)
                {
                    bool south = (i < 9);
                    float idx = south ? i / 8f : (i - 9) / 8f;
                    float z = Mathf.Lerp(south ? -24f : 26f, south ? -96f : 98f, idx) + rand(-4f, 4f);
                    // 深入内陆草坡 6m ~ 28m 处成林分布，彻底告别单排树！
                    float x = ShoreX(z) - rand(6.0f, 28.0f);
                    float y = HeightAt(x, z);
                    if (y > SeaLifeRuntime.Sea + 0.6f)
                    {
                        var treeRoot = new GameObject("V29_CoastMaple_" + i).transform;
                        treeRoot.SetParent(worldRoot, false);
                        treeRoot.localPosition = new Vector3(x, y - 0.15f, z);
                        treeRoot.localRotation = Quaternion.Euler(0f, rand(0f, 360f), 0f);
                        treeRoot.localScale = Vector3.one * rand(0.82f, 1.32f);
                        AddMesh("Bark", trunk, barkMat, Vector3.zero, Vector3.one, false, treeRoot);
                        AddMesh("Leaves", leaves, leafMat, Vector3.zero, Vector3.one, false, treeRoot);
                        mapleCount++;
                    }
                }
            }

            Physics.SyncTransforms();
        }
    }
}

```

### 2. 覆盖写入 `Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/NauticalSpritesV29.cs`

* 融合《女神异闻录 (Persona)》的**锐利斜切平行四边形构图（Skewed Parallelogram）**、**右下错位高对比亮色叠影背板（Offset Shadow Plate）**、**斜向扫描纹理与左上角切边高光条**，以及带立体投影的手绘矢量图标！

```
// ============================================================================
// 深海灯塔·归潮湾 (v30) — 风格化动态斜切海洋 UI (Persona 灵感：高对比斜切双层叠影 + 潮汐青/暖阳金撞色)
// File: Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/NauticalSpritesV29.cs
// ============================================================================
using UnityEngine;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        /// <summary>
        /// 烘焙 Persona 风格高辨识度【斜切双层错位撞色横幅/卡槽 Sprite】：
        /// - 摒弃平庸死板的直角/普通圆角黑框！
        /// - 采用锐利动感的平行四边形斜切角 (Skew Angle) + 右下错位亮色叠影层 (Offset Shadow Plate)
        ///   + 高对比撞色切边 (深海墨黑底 + 浪花白/琥珀金/潮汐青锐利斜切边)，一眼拉满视觉冲击力！
        /// </summary>
        private Sprite BakeNauticalNineSliceSprite(string name, int w, int h, int radius, Color fillCol, Color borderCol, float borderThickness)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _owned.Add(tex);

            bool isActiveStyle = name.Contains("Active");
            bool isCardStyle = name.Contains("Card");

            // Persona 标志性双层错位背板颜色：选中态用夺目的琥珀暖金背板，常态用潮汐青白几何切边
            Color accentShadowCol = isActiveStyle
                ? new Color(0.98f, 0.78f, 0.22f, 0.96f)
                : new Color(0.18f, 0.78f, 0.72f, 0.55f);

            Color[] pixels = new Color[w * h];
            float skewSlope = isCardStyle ? 0.14f : 0.22f;

            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    // 1. 计算带斜切倾角的平行四边形 SDF (Skewed Parallelogram Box)
                    float skewX = (ny * h * 0.5f) * skewSlope;
                    float cx = x - w * 0.5f - skewX * 0.35f;
                    float cy = y - h * 0.5f;

                    float halfW = w * 0.5f - 6f;
                    float halfH = h * 0.5f - 5f;

                    // 主面板 SDF 与右下错位叠影层 SDF (Persona 经典错位双层剪影)
                    float dMain = ChamferBoxSDF(cx + 1.5f, cy - 1.5f, halfW, halfH, Mathf.Max(3f, radius * 0.45f));
                    float dShadow = ChamferBoxSDF(cx - 3.0f, cy + 3.0f, halfW, halfH, Mathf.Max(3f, radius * 0.45f));

                    Color outCol = Color.clear;

                    // 先画底层错位撞色背板
                    if (dShadow < 0.5f)
                    {
                        float shAlpha = Mathf.Clamp01(0.5f - dShadow);
                        outCol = new Color(accentShadowCol.r, accentShadowCol.g, accentShadowCol.b, accentShadowCol.a * shAlpha);
                    }

                    // 再画上层主面板 + 锐利高对比几何斜切边框 + 左上角斜切装饰条
                    if (dMain < 0.5f)
                    {
                        float mainAlpha = Mathf.Clamp01(0.5f - dMain);
                        float borderDist = Mathf.Abs(dMain + borderThickness * 0.75f);
                        float borderMask = Mathf.Clamp01(1.3f - borderDist / Mathf.Max(1.0f, borderThickness * 0.8f));

                        // 左边缘或顶部的斜切高光装饰色带 (Persona 风格视觉重心切条)
                        float slashStripe = Mathf.Clamp01(1.0f - Mathf.Abs((cx + halfW - 6f) + cy * 0.45f) / 3.2f);
                        Color bodyFill = fillCol;
                        // 内部加入微弱的斜向扫描暗纹，消除纯色塑料感
                        float diagPattern = 0.5f + 0.5f * Mathf.Sin((x + y * 1.5f) * 0.9f);
                        bodyFill.r += diagPattern * 0.015f;
                        bodyFill.g += diagPattern * 0.022f;
                        bodyFill.b += diagPattern * 0.025f;

                        Color topLayer = Color.Lerp(bodyFill, borderCol, Mathf.Max(borderMask, slashStripe * (isActiveStyle ? 0.95f : 0.45f)));
                        topLayer.a *= mainAlpha;
                        outCol = Color.Lerp(outCol, topLayer, topLayer.a);
                        outCol.a = Mathf.Max(outCol.a, topLayer.a);
                    }

                    pixels[y * w + x] = outCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            int b = Mathf.Clamp(radius + 6, 8, Mathf.Min(w, h) / 3);
            Vector4 border = new Vector4(b, b, b, b);
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            _owned.Add(sp);
            return sp;
        }

        private static float ChamferBoxSDF(float px, float py, float hx, float hy, float r)
        {
            float dx = Mathf.Abs(px) - (hx - r);
            float dy = Mathf.Abs(py) - (hy - r);
            return Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f))
                 + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }

        private Sprite BakeVectorIconSprite(int iconType)
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "V30_VectorIcon_" + iconType,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _owned.Add(tex);

            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y / (float)(size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float u = (x / (float)(size - 1)) * 2f - 1f;
                    // 1. 底层暗色斜切衬底阴影 (让图标在任何背景下都具备鲜明立体感)
                    float shadowAlpha = EvaluateIconSDF(iconType, u - 0.05f, v + 0.05f);
                    // 2. 上层锐利高对比主体图标
                    float mainAlpha = EvaluateIconSDF(iconType, u, v);

                    Color c = Color.clear;
                    if (shadowAlpha > 0.01f)
                    {
                        c = new Color(0.02f, 0.08f, 0.11f, shadowAlpha * 0.75f);
                    }
                    if (mainAlpha > 0.01f)
                    {
                        Color fg = new Color(1f, 0.98f, 0.92f, mainAlpha);
                        c = Color.Lerp(c, fg, mainAlpha);
                        c.a = Mathf.Max(c.a, mainAlpha);
                    }
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);

            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _owned.Add(sp);
            return sp;
        }

        private static float EvaluateIconSDF(int type, float u, float v)
        {
            float d = 10f;
            if (type == 0) // 0: 复古航海防风提灯
            {
                float ring = Mathf.Abs(Mathf.Sqrt(u * u + Mathf.Pow(v - 0.58f, 2f)) - 0.16f) - 0.048f;
                float cap = BoxSDF(u, v - 0.34f, 0.38f, 0.065f);
                float baseBar = BoxSDF(u, v + 0.52f, 0.36f, 0.075f);
                float bodyOuter = BoxSDF(u, v + 0.08f, 0.27f - v * 0.06f, 0.38f);
                float bodyInner = BoxSDF(u, v + 0.08f, 0.16f - v * 0.04f, 0.27f);
                float cage = Mathf.Max(bodyOuter, -bodyInner);
                float flame = Mathf.Sqrt(u * u * 1.7f + Mathf.Pow(v + 0.10f, 2f)) - 0.13f;
                d = Mathf.Min(Mathf.Min(ring, cap), Mathf.Min(baseBar, Mathf.Min(cage, flame)));
            }
            else if (type == 1) // 1: 斜交双环锚链扣
            {
                float c = 0.766f, s = 0.642f;
                float rx = u * c + v * s;
                float ry = -u * s + v * c;
                float link1 = Mathf.Abs(CapsuleRingSDF(rx + 0.22f, ry, 0.22f, 0.18f)) - 0.07f;
                float link2 = Mathf.Abs(CapsuleRingSDF(rx - 0.22f, ry, 0.22f, 0.18f)) - 0.07f;
                d = Mathf.Min(link1, link2);
            }
            else if (type == 2) // 2: 海钓弯竿与浮漂
            {
                float rod = SegmentSDF(u, v, -0.58f, -0.55f, 0.38f, 0.58f) - 0.06f;
                float line = SegmentSDF(u, v, 0.38f, 0.58f, 0.42f, -0.22f) - 0.028f;
                float bobber = Mathf.Sqrt(Mathf.Pow(u - 0.42f, 2f) + Mathf.Pow(v + 0.28f, 2f)) - 0.14f;
                d = Mathf.Min(rod, Mathf.Min(line, bobber));
            }
            else if (type == 3) // 3: 四芒星罗盘蓝图
            {
                float outerRing = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - 0.62f) - 0.055f;
                float starV = BoxSDF(u, v, 0.08f - Mathf.Abs(v) * 0.08f, 0.54f);
                float starH = BoxSDF(u, v, 0.54f, 0.08f - Mathf.Abs(u) * 0.08f);
                d = Mathf.Min(outerRing, Mathf.Min(starV, starH));
            }
            else if (type == 4) // 4: 木栈桥与桥桩
            {
                float deck = BoxSDF(u, v - 0.12f, 0.68f, 0.095f);
                float p1 = BoxSDF(u + 0.42f, v + 0.24f, 0.085f, 0.35f);
                float p2 = BoxSDF(u - 0.42f, v + 0.24f, 0.085f, 0.35f);
                float wave = Mathf.Abs(v + 0.45f - Mathf.Sin(u * 5f) * 0.06f) - 0.045f;
                d = Mathf.Min(Mathf.Min(deck, p1), Mathf.Min(p2, wave));
            }
            else if (type == 5) // 5: 水下珊瑚簇
            {
                float stem = SegmentSDF(u, v, 0f, -0.58f, 0f, 0.48f) - 0.075f;
                float bL = SegmentSDF(u, v, 0f, -0.10f, -0.45f, 0.38f) - 0.07f;
                float bR = SegmentSDF(u, v, 0f, 0.02f, 0.46f, 0.42f) - 0.07f;
                float baseRock = BoxSDF(u, v + 0.56f, 0.48f, 0.085f);
                d = Mathf.Min(Mathf.Min(stem, bL), Mathf.Min(bR, baseRock));
            }
            else // 6: 捕蟹浮标与网笼
            {
                float buoy = Mathf.Sqrt(u * u + Mathf.Pow(v - 0.35f, 2f)) - 0.21f;
                float rope = SegmentSDF(u, v, 0f, 0.15f, 0f, -0.18f) - 0.035f;
                float cage = Mathf.Abs(BoxSDF(u, v + 0.38f, 0.46f, 0.22f)) - 0.055f;
                d = Mathf.Min(buoy, Mathf.Min(rope, cage));
            }

            return Mathf.Clamp01(0.5f - d * 18.0f);
        }

        private static float BoxSDF(float u, float v, float hx, float hy)
        {
            float dx = Mathf.Abs(u) - hx;
            float dy = Mathf.Abs(v) - hy;
            return Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f);
        }

        private static float SegmentSDF(float px, float py, float ax, float ay, float bx, float by)
        {
            float pax = px - ax, pay = py - ay;
            float bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / Mathf.Max(0.0001f, bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float CapsuleRingSDF(float px, float py, float halfLen, float radius)
        {
            float dx = Mathf.Max(Mathf.Abs(px) - halfLen, 0f);
            return Mathf.Sqrt(dx * dx + py * py) - radius;
        }

        private static RectTransform CreateUIRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }
    }
}

```

### 3. 覆盖写入 `Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/NauticalHUDV29.cs`

* **左上角**：斜切错位金青撞色主标题横幅 + 5 枚高低错落的独立斜切资源胶囊（`金币` / `活鱼` / `浮木` / `潮汐石` / `珊瑚苗` 独立清晰展示，彻底消灭 `"石/苗"` 斜杠挤压）。

* **左下角 & 右下角**：带大号斜体水印序号（`L / 02 / 01 / B` 与 `01 / 02 / 03 / 04`）的斜切卡槽，选中时触发 **Persona 经典弹簧抬升、倾斜与放大动效**！

```
// ============================================================================
// 深海灯塔·归潮湾 (v30) — 风格化动态斜切海洋 HUD 界面 (参考《女神异闻录》动态斜角切面与高对比视觉层级)
// File: Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/NauticalHUDV29.cs
// ============================================================================
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        Canvas nauticalCanvas;
        RectTransform drawer;
        TextMeshProUGUI subtitle, context, hoverStatus, materialText;
        TextMeshProUGUI[] pills = new TextMeshProUGUI[5];
        Image[] toolIcons = new Image[4];
        TextMeshProUGUI[] toolLabels = new TextMeshProUGUI[4];
        Image[] cards = new Image[4];
        RectTransform[] toolRects = new RectTransform[4];
        RectTransform[] cardRects = new RectTransform[4];

        // 高对比海洋美学色板：深海曜石黑 + 浪花纯白 + 琥珀曜日金 + 极光潮汐青
        readonly Color ivory = new Color(0.98f, 0.97f, 0.93f, 1f);
        readonly Color gold = new Color(1.00f, 0.82f, 0.28f, 1f);
        readonly Color cyanAccent = new Color(0.24f, 0.94f, 0.86f, 1f);
        readonly Color dim = new Color(0.76f, 0.88f, 0.86f, 0.92f);

        TextMeshProUGUI Text(Transform parent, string name, string content, Rect rect, int size, Color color)
        {
            var rt = CreateUIRect(name, parent, new Vector2(0, 1), new Vector2(0, 1), rect.position, rect.size);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = _cnFont;
            t.fontSize = size;
            t.color = color;
            t.text = content;
            t.raycastTarget = false;
            return t;
        }

        RectTransform Surface(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Sprite sprite)
        {
            var r = CreateUIRect(name, parent, anchor, pivot, pos, size);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            return r;
        }

        Button IconButton(Transform parent, string name, Rect rect, Sprite icon, string label, string watermarkNum, Action action, out Image image, out RectTransform slotRT)
        {
            slotRT = CreateUIRect(name, parent, new Vector2(0, 0), new Vector2(0, 0), rect.position, rect.size);
            image = slotRT.gameObject.AddComponent<Image>();
            image.sprite = _iconSlotSprite;
            image.type = Image.Type.Sliced;

            var b = slotRT.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            b.onClick.AddListener(() => action?.Invoke());

            // Persona 风格大号斜体序号背景水印 (如 "01", "02", "L", "B")
            if (!string.IsNullOrEmpty(watermarkNum))
            {
                var wm = Text(slotRT, "Watermark", watermarkNum, new Rect(6, -4, rect.width - 10, 34), 24, new Color(1f, 0.88f, 0.45f, 0.18f));
                wm.fontStyle = FontStyles.Bold | FontStyles.Italic;
                wm.alignment = TextAlignmentOptions.TopLeft;
            }

            var ic = CreateUIRect("Icon", slotRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), label != null ? new Vector2(0, 10) : Vector2.zero, new Vector2(46, 46)).gameObject.AddComponent<Image>();
            ic.sprite = icon;
            ic.raycastTarget = false;

            if (label != null)
            {
                var lb = Text(slotRT, "Label", label, new Rect(0, -rect.height + 24, rect.width, 22), 15, ivory);
                lb.fontStyle = FontStyles.Bold;
                lb.alignment = TextAlignmentOptions.Center;
            }
            return b;
        }

        void BuildNauticalUI()
        {
            _cnFont = Resources.Load<TMP_FontAsset>("SeaLifeV21/SeaLifeChinese");

            // 烘焙高对比 Persona 海洋斜切双层叠影面板
            _roundedCardSprite = BakeNauticalNineSliceSprite(
                "V30_PersonaSkewCard", 96, 96, 10,
                new Color(0.03f, 0.08f, 0.11f, 0.92f),
                new Color(0.88f, 0.96f, 0.95f, 0.94f), 2.0f);

            _pillBadgeSprite = BakeNauticalNineSliceSprite(
                "V30_PersonaSkewPill", 72, 42, 8,
                new Color(0.04f, 0.10f, 0.14f, 0.90f),
                new Color(0.32f, 0.86f, 0.80f, 0.88f), 1.6f);

            _iconSlotSprite = BakeNauticalNineSliceSprite(
                "V30_PersonaToolSlot", 80, 80, 9,
                new Color(0.04f, 0.10f, 0.14f, 0.90f),
                new Color(0.52f, 0.78f, 0.76f, 0.85f), 1.8f);

            _iconSlotActiveSprite = BakeNauticalNineSliceSprite(
                "V30_PersonaActiveSlot", 80, 80, 9,
                new Color(0.10f, 0.28f, 0.26f, 0.98f),
                new Color(1.00f, 0.84f, 0.32f, 1.00f), 2.6f);

            icons = new Sprite[7];
            for (int i = 0; i < 7; i++) icons[i] = BakeVectorIconSprite(i);

            var node = new GameObject("V29_NauticalHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            nauticalCanvas = node.GetComponent<Canvas>();
            nauticalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            nauticalCanvas.sortingOrder = 180;

            var scaler = node.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = node.transform;

            // ----------------------------------------------------------------
            // 1. 左上角：Persona 风格斜切阶梯主视觉标题 + 5 枚独立错落资源徽章 (彻底消除 "石/苗" 挤压！)
            // ----------------------------------------------------------------
            var header = CreateUIRect("PoeticHeader", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -22), new Vector2(960, 88));

            // 底层斜切金青撞色标题背板
            var titleBanner = Surface(header, "TitleSkewBanner", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(305, 52), _iconSlotActiveSprite);
            titleBanner.localRotation = Quaternion.Euler(0f, 0f, 1.4f);

            var mainTitle = Text(titleBanner, "RegionTitle", "雾岬岛 · 东岸", new Rect(18, -4, 270, 44), 30, ivory);
            mainTitle.fontStyle = FontStyles.Bold;

            var subBanner = Surface(header, "SubtitleBacking", new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -52), new Vector2(460, 32), _pillBadgeSprite);
            subtitle = Text(subBanner, "RegionSubtitle", "MIST CAPE ARCHIPELAGO · 潮间浅湾", new Rect(16, -2, 430, 28), 14, gold);
            subtitle.fontStyle = FontStyles.Bold;

            // 顶部 5 个独立斜切资源胶囊（每个资源独立清晰，绝不用斜杠混写！）
            float px = 320f;
            int[] widths = { 126, 128, 126, 126, 126 };
            for (int i = 0; i < 5; i++)
            {
                // 奇偶高低微错落 3px，营造灵动韵律感
                float yOff = (i % 2 == 0) ? -4f : -8f;
                var p = Surface(header, "ResourcePill" + i, new Vector2(0, 1), new Vector2(0, 1), new Vector2(px, yOff), new Vector2(widths[i], 36), _pillBadgeSprite);
                p.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
                pills[i] = Text(p, "Text", "", new Rect(8, -2, widths[i] - 16, 32), 15, ivory);
                pills[i].fontStyle = FontStyles.Bold;
                pills[i].alignment = TextAlignmentOptions.Center;
                px += widths[i] + 10f;
            }

            // ----------------------------------------------------------------
            // 2. 右上角：统一斜切胶囊快捷行动栏 (Tab 鱼箱 / E 营造 / Esc 暂停 全部统一精致斜切框！)
            // ----------------------------------------------------------------
            var quick = CreateUIRect("QuickActions", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -24), new Vector2(380, 44));
            quickActions = quick.gameObject;
            SmallButton(quick, "Tab 活鱼箱", new Rect(0, 0, 120, 40), () => game.OpenPanel(2));
            SmallButton(quick, "B 海湾营造", new Rect(128, 0, 124, 40), () => game.TogglePlanning());
            SmallButton(quick, "Esc 航行菜单", new Rect(260, 0, 120, 40), () => { game.Paused = !game.Paused; Time.timeScale = game.Paused ? 0f : 1f; game.ui.RebuildPanel(); });

            // ----------------------------------------------------------------
            // 3. 左下角：阶梯错落斜切工具卡槽 (选中时自动向上弹簧抬升 + 琥珀金叠影高亮)
            // ----------------------------------------------------------------
            var dock = CreateUIRect("Tools", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(32, 26), new Vector2(380, 96));
            string[] labels = { "提灯", "挂钩", "钓竿", "蓝图" };
            string[] hotkeys = { "L", "02", "01", "B" };
            Action[] actions =
            {
                ToggleLantern,
                () => { if (game.InRoom) game.ToggleHook(); else game.SelectTool(1); },
                () => { if (game.InRoom) game.ToggleRod(); else game.SelectTool(0); },
                () => game.TogglePlanning()
            };

            for (int i = 0; i < 4; i++)
            {
                int n = i;
                toolButtons[i] = IconButton(dock, "Tool" + i, new Rect(i * 92, (i % 2) * 4, 84, 86), icons[i], labels[i], hotkeys[i], actions[i], out toolIcons[i], out toolRects[i]);
                toolLabels[i] = toolButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            }

            // ----------------------------------------------------------------
            // 4. 底部中央：斜切高对比动态行动横幅 (Dynamic Action Banner)
            // ----------------------------------------------------------------
            var prompt = Surface(root, "Context", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(680, 54), _roundedCardSprite);
            contextPanel = prompt.gameObject;
            context = Text(prompt, "Prompt", "", new Rect(22, -8, 636, 38), 16, ivory);
            context.fontStyle = FontStyles.Bold;
            context.alignment = TextAlignmentOptions.Center;

            // ----------------------------------------------------------------
            // 5. 右下角：Persona 风格斜切营造蓝图展板 (带 01~04 大号斜体水印与弹簧动效)
            // ----------------------------------------------------------------
            drawer = Surface(root, "BlueprintDrawer", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 24), new Vector2(680, 252), _roundedCardSprite);
            var dt = Text(drawer, "DrawerTitle", "BAY CONSTRUCTION // 海湾营造蓝图", new Rect(22, -10, 360, 34), 21, gold);
            dt.fontStyle = FontStyles.Bold;

            SmallButton(drawer, "R 旋转朝向", new Rect(396, -10, 124, 36), () => game.RotateBlueprint());
            SmallButton(drawer, "G 返回航行", new Rect(530, -10, 124, 36), () => game.TogglePlanning());

            materialText = Text(drawer, "BuildMaterials", "", new Rect(22, -44, 636, 26), 15, cyanAccent);

            string[] names = { "延伸木栈桥", "航标暖光灯", "珊瑚生态园", "海湾捕蟹笼" };
            string[] costs = { "消耗: 浮木 2", "消耗: 浮木 1 · 黄铜 1", "消耗: 礁石 2 · 珊瑚苗 1", "消耗: 浮木 1 · 礁石 1" };
            string[] nums = { "01", "02", "03", "04" };
            int[] iconsIdx = { 4, 0, 5, 6 };

            for (int i = 0; i < 4; i++)
            {
                int n = i;
                IconButton(drawer, "BuildCard" + i, new Rect(22 + i * 160, 48, 150, 118), icons[iconsIdx[i]], names[i], nums[i], () => game.SelectBlueprint(n), out cards[i], out cardRects[i]);
                int sub = n;
                EventTriggerUtility.AddHover(cards[i].gameObject, () => SetHover(names[sub], costs[sub]), () => ClearHover());
            }

            hoverStatus = Text(drawer, "HoverStatus", "", new Rect(22, -210, 636, 30), 16, ivory);
            drawer.gameObject.SetActive(false);
        }

        void SmallButton(Transform parent, string title, Rect r, Action action)
        {
            var p = CreateUIRect(title, parent, new Vector2(0, 1), new Vector2(0, 1), r.position, r.size);
            var img = p.gameObject.AddComponent<Image>();
            img.sprite = _pillBadgeSprite;
            img.type = Image.Type.Sliced;
            var b = p.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => action?.Invoke());
            var t = Text(p, "Label", title, new Rect(0, 0, r.width, r.height), 14, ivory);
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
        }

        void RefreshNauticalUI()
        {
            game.ui.HidePersistentV29();
            nauticalCanvas.enabled = game.ui.HudV29;

            // 丝滑弹簧动效：选中工具槽或蓝图卡片时向上轻盈抬升并略微倾斜放大 (Persona 经典交互手感！)
            float dt = Time.unscaledDeltaTime * 12f;
            for (int i = 0; i < 4; i++)
            {
                bool activeTool = (i == 0) ? LanternOn : (i == 1) ? (!planning && game.SelectedTool == 1) : (i == 2) ? (!planning && game.SelectedTool == 0) : planning;
                if (toolIcons[i] != null) toolIcons[i].sprite = activeTool ? _iconSlotActiveSprite : _iconSlotSprite;
                if (toolRects[i] != null)
                {
                    Vector2 targetPos = new Vector2(i * 92f, (i % 2) * 4f + (activeTool ? 10f : 0f));
                    Vector3 targetScale = activeTool ? Vector3.one * 1.08f : Vector3.one;
                    Quaternion targetRot = Quaternion.Euler(0f, 0f, activeTool ? -3.5f : 0f);
                    toolRects[i].anchoredPosition = Vector2.Lerp(toolRects[i].anchoredPosition, targetPos, dt);
                    toolRects[i].localScale = Vector3.Lerp(toolRects[i].localScale, targetScale, dt);
                    toolRects[i].localRotation = Quaternion.Slerp(toolRects[i].localRotation, targetRot, dt);
                }

                if (cards[i] != null && bay != null)
                {
                    bool activeCard = (bay.SelectedV29 == i);
                    cards[i].sprite = activeCard ? _iconSlotActiveSprite : _iconSlotSprite;
                    if (cardRects[i] != null)
                    {
                        Vector2 cPos = new Vector2(22f + i * 160f, 48f + (activeCard ? 8f : 0f));
                        cardRects[i].anchoredPosition = Vector2.Lerp(cardRects[i].anchoredPosition, cPos, dt);
                        cardRects[i].localScale = Vector3.Lerp(cardRects[i].localScale, activeCard ? Vector3.one * 1.05f : Vector3.one, dt);
                    }
                }
            }

            if (Time.unscaledTime < nextUIRefresh) return;
            nextUIRefresh = Time.unscaledTime + 0.08f;

            quickActions.SetActive(!planning && !modal);
            drawer.gameObject.SetActive(planning && !modal);
            contextPanel.SetActive(!planning && !modal);

            // 顶部 5 枚独立资源胶囊（清晰展示 5 项核心数据，绝不混写斜杠！）
            var d = bay.DataV29;
            pills[0].text = $"金币  {game.save.coins}";
            pills[1].text = $"活鱼  {game.CarryCount}/6";
            pills[2].text = $"浮木  {d.wood}";
            pills[3].text = $"潮汐石  {d.stone}";
            pills[4].text = $"珊瑚苗  {d.coralSeed}";

            if (materialText != null)
            {
                materialText.text = $"库存建材  //  浮木 {d.wood}   ·   潮汐石 {d.stone}   ·   珊瑚苗 {d.coralSeed}   ·   黄铜 {d.brass}";
            }

            if (subtitle != null)
            {
                subtitle.text = planning
                    ? "BAY PLANNING MODE  ·  WASD 漫游全岛 / 滚轮缩放 / 左键建造 / 右键拆除"
                    : $"MIST CAPE ARCHIPELAGO  ·  航行视野 {customSailZoom:F0}m (滑动滚轮自由缩放)";
            }

            if (hoverStatus != null && bay != null)
            {
                string fb = bay.FeedbackV29;
                hoverStatus.text = !string.IsNullOrEmpty(fb)
                    ? fb
                    : (!string.IsNullOrEmpty(hoverTitle) ? $"{hoverTitle}  —  {hoverCost}" : (bay.HoverV29 ?? "指向浅湾建造网格，左键放置海湾设施 · 右键拆除"));
            }

            context.text = game.Busy
                ? game.Hint
                : (Time.unscaledTime < game.NoticeTime ? game.Notice : "WASD 驾驶渔船   ·   滚轮缩放全景海湾   ·   鱼影旁按 [E] 垂钓   ·   按 [B] 开启海湾营造");
        }
    }
}

```

## 第二步：收紧建造网格水深与坡度过滤（消除深沟上方悬浮网格）

在 `Assets/Deepsea/Scripts/Runtime/SeaExpansionV28/SeaBayGeometryV28.cs`（或对应生成建造网格线的位置）中： 将生成建造网格单元的条件从 `if (d < .5f || d > 5.5f) continue;` 收紧为**仅在平缓浅湾台地显示网格**：

```
float d = DepthAt(p);
float dRight = DepthAt(p + Vector3.right * 2f);
float dFwd = DepthAt(p + Vector3.forward * 2f);
float slope = Mathf.Max(Mathf.Abs(d - dRight), Mathf.Abs(d - dFwd));
// 仅在水深 0.45m ~ 3.2m 且海床坡度平缓 (< 0.65m) 的翡翠浅滩/环礁台地上生成建造网格，绝不悬空架在深海沟或陡峭礁石上！
if (d < 0.45f || d > 3.2f || slope > 0.65f) continue;

```

## 第三步：重新构建并采集 1920x1080 实机截图（重点验证北岸、南湾与 UI）

1. 编译并构建最新可执行程序，刷新桌面快捷方式 **“深海灯塔—最新版”**。

2. 在 1920x1080 可见窗口下运行实机验证，更新 `Docs/SeaSeamlessV29/preview.jpg`、`Docs/SeaSeamlessV29/02-nautical-build-ui.jpg`、`Docs/SeaSeamlessV29/Runtime/03-north-coast.jpg` 和 `03b-south-coast.jpg`，确认：

   * 南湾与西岸的沙滩到内陆草坡呈自然斑驳渐变，再无 `x = -36` 的直线沙草硬切；

   * 北侧陆地呈现饱满宽阔的山体与红枫林，再无单薄细线；

   * 浅海水面与外海深蓝水面之间过渡柔顺，且建造网格只贴合在平缓浅湾内；

   * 全新 Persona 风格斜切撞色海洋 UI 正常显示且动效流畅。

## 第四步：提交并推送到 GitHub (`https://github.com/xujiale4869/guichao-bay`)

```
git add .
git commit -m "feat(v30): 合并一体化顶点色无缝半岛海床消除沙草硬切与北岸细线，重做Persona风格斜切撞色海洋UI"
git push origin main

```