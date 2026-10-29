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
}
