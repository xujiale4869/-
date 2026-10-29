// =====================================================================
// 深海灯塔·归潮湾 (v26) — “雾岬岛·东岸”层次波光海面与水下生态增强控制器
// File: Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaWaterArtUpgradeV26.cs
// =====================================================================
// 核心功能:
// 1. 自动识别场景中的海面渲染器 (ShowcaseWater / Ocean / Water)，将海面材质无损升级为
//    Deepsea/SeaSurfaceSparkleV26 (支持按 F6 实时切换 v26 波光层次水面 / 原版水面对比！)
// 2. 自动加载 Tools/SeaSurfaceV26 生成的 Voronoi 焦散贴图、岸浪泡沫贴图与五级水深色板；
//    若本地尚未运行 Python 脚本，内置 C# 运行时 Voronoi 焦散贴图烘焙器自动兜底。
// 3. 还原原图水下生态层次细节：
//    - 在右侧深海海沟 (Trench) 生成流线型银色沙丁鱼群巡游螺旋带 (Silver Baitball Stream)
//    - 在浅滩翡翠湾沙底上方生成悠闲滑翔的蝠鲼 (Manta Ray) 剪影与浅礁暖橙雀鲷群
// =====================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deepsea
{
    [DefaultExecutionOrder(80)]
    public sealed class SeaWaterArtUpgradeV26 : MonoBehaviour
    {
        public static SeaWaterArtUpgradeV26 Active { get; private set; }

        [Header("v26 水体艺术开关 (按 F6 实时对比新旧水面)")]
        public bool v26WaterEnabled = true;
        public bool spawnAmbientMarineLife = true;

        private MeshRenderer _waterRenderer;
        private Material _originalWaterMaterial;
        private Material _v26WaterMaterial;

        private Texture2D _causticsTex;
        private Texture2D _foamTex;
        private float _clock;

        private Transform _marineEcologyRoot;
        private readonly List<SilverFishParticle> _trenchSchool = new List<SilverFishParticle>();
        private Transform _mantaRay;
        private float _mantaPhase;

        private struct SilverFishParticle
        {
            public Transform tr;
            public float pathT;
            public float speed;
            public float lateralOffset;
            public float verticalOffset;
            public float phase;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            // 自动挂载到包含海面的主控节点上
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

        private void Start()
        {
            LocateWaterRenderer();
            BuildV26WaterMaterial();
            ApplyWaterMode(v26WaterEnabled);

            if (spawnAmbientMarineLife)
            {
                SetupReferenceMarineEcology();
            }
        }

        private void LateUpdate()
        {
            _clock += Time.deltaTime;
            Shader.SetGlobalFloat("_OceanV24Time", _clock);
            Shader.SetGlobalFloat("_WaterV22Time", _clock);
            Shader.SetGlobalFloat("_CoastWaterDetails", 1.0f);

            // 按 F6 随时在同镜头下切换 v26 新水面与原版水面对比
            if (Input.GetKeyDown(KeyCode.F6))
            {
                v26WaterEnabled = !v26WaterEnabled;
                ApplyWaterMode(v26WaterEnabled);
            }

            // 防止其他脚本在 LateUpdate 中覆盖材质
            if (v26WaterEnabled && _waterRenderer != null && _v26WaterMaterial != null)
            {
                if (_waterRenderer.sharedMaterial != _v26WaterMaterial)
                {
                    _waterRenderer.sharedMaterial = _v26WaterMaterial;
                }
            }

            AnimateMarineEcology(Time.deltaTime);
        }

        private void LocateWaterRenderer()
        {
            // 优先查找 ShowcaseWater 或带有 SeaSurface / SeaLifeWater 材质的渲染器
            var allRenderers = FindObjectsOfType<MeshRenderer>();
            foreach (var r in allRenderers)
            {
                if (r == null || r.sharedMaterial == null) continue;
                string rName = r.gameObject.name;
                string sName = r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "";

                if (rName == "ShowcaseWater" || rName.Contains("Ocean") || rName.Contains("Water") ||
                    sName.Contains("SeaSurface") || sName.Contains("SeaLifeWater") || sName.Contains("CoastBlockoutWater"))
                {
                    // 排除展缸内部的小水块
                    if (r.bounds.size.x > 12f && r.bounds.size.z > 12f)
                    {
                        _waterRenderer = r;
                        _originalWaterMaterial = r.sharedMaterial;
                        break;
                    }
                }
            }
        }

        private void BuildV26WaterMaterial()
        {
            Shader v26Shader = Shader.Find("Deepsea/SeaSurfaceSparkleV26");
            if (v26Shader == null)
            {
                Debug.LogWarning("[SeaWaterArtUpgradeV26] Shader Deepsea/SeaSurfaceSparkleV26 not found, falling back to existing water shader.");
                return;
            }

            _v26WaterMaterial = new Material(v26Shader)
            {
                name = "SeaSurfaceSparkleV26_Material"
            };

            // 继承原水面材质的法线贴图 (_WaveMap)
            if (_originalWaterMaterial != null && _originalWaterMaterial.HasProperty("_WaveMap"))
            {
                Texture waveTex = _originalWaterMaterial.GetTexture("_WaveMap");
                if (waveTex != null) _v26WaterMaterial.SetTexture("_WaveMap", waveTex);
            }

            // 加载或运行时生成 Voronoi 焦散贴图与泡沫贴图
            _causticsTex = Resources.Load<Texture2D>("SeaSurfaceV26/Textures/WaterCaustics_V26");
            if (_causticsTex == null) _causticsTex = GenerateRuntimeVoronoiCaustics(256, 256);

            _foamTex = Resources.Load<Texture2D>("SeaSurfaceV26/Textures/ShoreFoamNoise_V26");
            if (_foamTex == null) _foamTex = _causticsTex;

            _v26WaterMaterial.SetTexture("_CausticsMap", _causticsTex);
            _v26WaterMaterial.SetTexture("_FoamMap", _foamTex);

            // 配置严格对齐《雾岬岛·东岸》原图的 5 级水深色彩与波光参数
            _v26WaterMaterial.SetColor("_CrystalShoreColor", new Color(0.76f, 0.90f, 0.78f, 1f)); // 浅滩透底暖沙绿
            _v26WaterMaterial.SetColor("_Shallow", new Color(0.09f, 0.66f, 0.62f, 1f));           // 栈桥内湾翡翠薄荷青
            _v26WaterMaterial.SetColor("_ReefMidColor", new Color(0.03f, 0.38f, 0.50f, 1f));      // 珊瑚礁台孔雀蓝
            _v26WaterMaterial.SetColor("_Deep", new Color(0.008f, 0.11f, 0.21f, 1f));             // 外海深沟午夜靛蓝

            _v26WaterMaterial.SetFloat("_Refraction", 0.0032f);
            _v26WaterMaterial.SetFloat("_Visibility", 0.86f);
            _v26WaterMaterial.SetFloat("_SurfaceWeight", 0.40f);
            _v26WaterMaterial.SetFloat("_CausticsIntensity", 1.22f);
            _v26WaterMaterial.SetFloat("_CausticsScale", 0.24f);
            _v26WaterMaterial.SetFloat("_CausticsSpeed", 0.28f);
            _v26WaterMaterial.SetFloat("_CausticsMaxDepth", 6.5f);
            _v26WaterMaterial.SetFloat("_GlintEnable", 1.0f);
            _v26WaterMaterial.SetFloat("_GlintIntensity", 2.75f);
            _v26WaterMaterial.SetFloat("_GlintScale", 11.5f);
            _v26WaterMaterial.SetFloat("_GlintSpeed", 0.85f);
            _v26WaterMaterial.SetFloat("_FoamIntensity", 0.95f);
            _v26WaterMaterial.SetFloat("_FoamWidth", 0.44f);
        }

        public void ApplyWaterMode(bool useV26)
        {
            if (_waterRenderer == null) LocateWaterRenderer();
            if (_waterRenderer == null || _v26WaterMaterial == null) return;

            _waterRenderer.sharedMaterial = useV26 ? _v26WaterMaterial : _originalWaterMaterial;
        }

        // =====================================================================
        // 还原原图标志性水下生态视觉焦点：
        // 1. 左下方翡翠浅滩沙底上方悠闲滑翔的深色蝠鲼 (Manta Ray)
        // 2. 右侧深海海沟中成群结队呈 S 型流线游弋的亮银色小鱼群带 (Silver Baitball Stream)
        // =====================================================================
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
            Material mantaMat = new Material(litShader) { name = "V26_MantaRayMat" };
            if (mantaMat.HasProperty("_BaseColor")) mantaMat.SetColor("_BaseColor", new Color(0.12f, 0.22f, 0.26f, 1f));
            if (mantaMat.HasProperty("_Smoothness")) mantaMat.SetFloat("_Smoothness", 0.72f);

            var mantaGo = new GameObject("V26_ShallowMantaRay");
            mantaGo.transform.SetParent(_marineEcologyRoot, false);
            mantaGo.transform.position = new Vector3(-3.5f, seaLevel - 0.85f, -4.2f);
            mantaGo.transform.localScale = Vector3.one * 0.95f;
            mantaGo.AddComponent<MeshFilter>().sharedMesh = mantaMesh;
            mantaGo.AddComponent<MeshRenderer>().sharedMaterial = mantaMat;
            _mantaRay = mantaGo.transform;

            // 2. 构建右侧深海海沟银鱼流线巡游群 (72 条高反光银鱼沿 S 型海沟轨迹游动)
            Mesh silverFishMesh = BuildSlenderSilverFishMesh();
            Material silverMat = new Material(litShader) { name = "V26_TrenchSilverSchoolMat" };
            if (silverMat.HasProperty("_BaseColor")) silverMat.SetColor("_BaseColor", new Color(0.82f, 0.94f, 0.98f, 1f));
            if (silverMat.HasProperty("_Smoothness")) silverMat.SetFloat("_Smoothness", 0.92f);
            if (silverMat.HasProperty("_Metallic")) silverMat.SetFloat("_Metallic", 0.65f);

            int count = 72;
            for (int i = 0; i < count; i++)
            {
                var fg = new GameObject("V26_TrenchSilverFish_" + i);
                fg.transform.SetParent(_marineEcologyRoot, false);
                float scale = UnityEngine.Random.Range(0.22f, 0.34f);
                fg.transform.localScale = new Vector3(scale * 0.7f, scale * 0.75f, scale * 1.25f);
                fg.AddComponent<MeshFilter>().sharedMesh = silverFishMesh;
                fg.AddComponent<MeshRenderer>().sharedMaterial = silverMat;

                _trenchSchool.Add(new SilverFishParticle
                {
                    tr = fg.transform,
                    pathT = i / (float)count,
                    speed = UnityEngine.Random.Range(0.035f, 0.052f),
                    lateralOffset = UnityEngine.Random.Range(-1.15f, 1.15f),
                    verticalOffset = UnityEngine.Random.Range(-1.4f, -0.45f),
                    phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
                });
            }
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
                _mantaRay.position = pos;
                Vector3 dir = nextPos - pos;
                if (dir.sqrMagnitude > 0.00001f)
                {
                    float bank = Mathf.Cos(_mantaPhase) * 12f;
                    _mantaRay.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, bank);
                }
            }

            // 2. 深海海沟银鱼流线带巡游
            for (int i = 0; i < _trenchSchool.Count; i++)
            {
                var f = _trenchSchool[i];
                if (f.tr == null) continue;
                f.pathT = Mathf.Repeat(f.pathT + dt * f.speed, 1.0f);

                Vector3 p0 = EvaluateTrenchSpline(f.pathT, seaLevel, f.lateralOffset, f.verticalOffset, f.phase + _clock * 2.4f);
                Vector3 p1 = EvaluateTrenchSpline(Mathf.Repeat(f.pathT + 0.015f, 1.0f), seaLevel, f.lateralOffset, f.verticalOffset, f.phase + _clock * 2.4f);
                f.tr.position = p0;
                Vector3 fwd = p1 - p0;
                if (fwd.sqrMagnitude > 0.00001f)
                {
                    f.tr.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                }
                _trenchSchool[i] = f;
            }
        }

        private Vector3 EvaluateTrenchSpline(float t, float seaLevel, float latOffset, float vertOffset, float wavePhase)
        {
            // 沿右侧深海海沟呈 S 型蜿蜒曲折的鱼群走廊
            float z = Mathf.Lerp(-12.0f, 14.0f, t);
            float mainCurveX = 8.5f + Mathf.Sin(t * Mathf.PI * 2.0f) * 2.6f;
            float ribbonSpread = latOffset * (0.65f + 0.35f * Mathf.Sin(t * Mathf.PI * 3f));
            float x = mainCurveX + ribbonSpread + Mathf.Sin(wavePhase) * 0.08f;
            float y = seaLevel + vertOffset + Mathf.Cos(wavePhase * 0.7f) * 0.06f;
            return new Vector3(x, y, z);
        }

        private static Mesh BuildMantaRayMesh()
        {
            // 菱形双翼流线蝠鲼 + 细长尾鞭
            var verts = new List<Vector3>
            {
                new Vector3( 0.00f,  0.02f,  0.58f), // 0: 吻端中央
                new Vector3(-0.18f,  0.01f,  0.65f), // 1: 左头鳍
                new Vector3( 0.18f,  0.01f,  0.65f), // 2: 右头鳍
                new Vector3(-0.92f,  0.04f,  0.05f), // 3: 左翼尖
                new Vector3( 0.92f,  0.04f,  0.05f), // 4: 右翼尖
                new Vector3( 0.00f,  0.07f,  0.10f), // 5: 背脊中心隆起
                new Vector3( 0.00f, -0.04f,  0.10f), // 6: 腹底中心
                new Vector3(-0.28f,  0.00f, -0.42f), // 7: 左后缘
                new Vector3( 0.28f,  0.00f, -0.42f), // 8: 右后缘
                new Vector3( 0.00f,  0.01f, -0.52f), // 9: 尾根
                new Vector3( 0.00f,  0.00f, -1.25f)  // 10: 细长尾刺尖
            };
            var uvs = new List<Vector2>();
            foreach (var v in verts)
            {
                uvs.Add(new Vector2(Mathf.InverseLerp(0.65f, -1.25f, v.z), 0.5f));
            }
            int[] tris =
            {
                // 背部
                0, 1, 5,  0, 5, 2,
                1, 3, 5,  2, 5, 4,
                3, 7, 5,  5, 8, 4,
                7, 9, 5,  5, 9, 8,
                7, 10, 9, 9, 10, 8,
                // 腹部
                0, 6, 1,  0, 2, 6,
                1, 6, 3,  2, 4, 6,
                3, 6, 7,  6, 4, 8,
                7, 6, 9,  6, 8, 9
            };
            var m = new Mesh { name = "V26_MantaRayMesh" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh BuildSlenderSilverFishMesh()
        {
            var verts = new List<Vector3>
            {
                new Vector3( 0.00f,  0.00f,  0.45f), // 0: 鼻尖
                new Vector3( 0.00f,  0.09f,  0.08f), // 1: 背脊
                new Vector3( 0.00f, -0.08f,  0.08f), // 2: 腹部
                new Vector3(-0.06f,  0.00f,  0.08f), // 3: 左侧
                new Vector3( 0.06f,  0.00f,  0.08f), // 4: 右侧
                new Vector3( 0.00f,  0.00f, -0.28f), // 5: 尾柄
                new Vector3( 0.00f,  0.11f, -0.45f), // 6: 上尾叶
                new Vector3( 0.00f, -0.11f, -0.45f)  // 7: 下尾叶
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
}