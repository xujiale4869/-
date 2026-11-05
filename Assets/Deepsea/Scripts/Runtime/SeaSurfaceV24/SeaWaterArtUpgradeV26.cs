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
}
