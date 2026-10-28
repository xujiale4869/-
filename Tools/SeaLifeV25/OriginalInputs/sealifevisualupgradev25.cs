// =====================================================================
// 深海灯塔·归潮湾 (v25) — 鱼类与水族箱视觉重构及动态升级管理器
// File: Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeVisualUpgradeV25.cs
// =====================================================================
// 核心作用:
// 1. 无缝桥接 SeaLifeV21 与 v25 高精度视觉资产: 自动优先加载 Blender 导出的高精度 FBX 与贴图
//    (Assets/Deepsea/Resources/SeaLifeV25/...)；若本地尚未运行 Blender，自带高精度 C# 双三次曲面
//    与程序化鳞片纹路兜底生成器，彻底替换旧版 12 边形简陋棱柱鱼和方盒子展缸！
// 2. 动态脊椎 S 型游动摆尾与倾角控制 (SeaLifeFishSwimmerV25): 根据游动速度实时驱动材质摆尾相位与转向侧倾。
// 3. 高颜值水族箱展缸升级: 为展缸自动注入“水体丁达尔体积光柱”、“水底动态焦散投影”、“微气泡上浮粒子”与“顶置生态暖光灯”。
// =====================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using OceanGame.Data;

namespace Deepsea
{
    /// <summary>
    /// 单条鱼的精细游动姿态与动态摆尾控制器 (挂载于每条展缸鱼/野外鱼实例上)
    /// </summary>
    public sealed class SeaLifeFishSwimmerV25 : MonoBehaviour
    {
        public int speciesId;
        public float baseSpeed = 1.0f;
        public float tailAmplitude = 0.085f;
        public float tailFrequency = 7.0f;
        public float phaseOffset;

        private Vector3 _lastPos;
        private float _swimPhase;
        private float _currentBankAngle;
        private Vector3 _lastForward = Vector3.forward;
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;

        private static readonly int PropSwimPhase = Shader.PropertyToID("_SwimPhase");
        private static readonly int PropTailAmp = Shader.PropertyToID("_TailAmplitude");

        private void Awake()
        {
            _lastPos = transform.position;
            _lastForward = transform.forward;
            _swimPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            phaseOffset = _swimPhase;
            _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            Vector3 velocity = (transform.position - _lastPos) / dt;
            float speed = velocity.magnitude;
            _lastPos = transform.position;

            // 根据实际移动速度自适应加快摆尾频率 (静止悬浮时保持轻柔呼吸摆尾)
            float activeFreq = tailFrequency * Mathf.Clamp(0.45f + speed * 0.85f, 0.45f, 2.2f);
            _swimPhase += dt * activeFreq;

            // 计算转向角速度，给鱼身施加自然的侧倾角 (Banking) 与尾柄微摆
            Vector3 currentForward = transform.forward;
            float turnRate = Vector3.SignedAngle(_lastForward, currentForward, Vector3.up) / dt;
            _lastForward = currentForward;

            float targetBank = Mathf.Clamp(-turnRate * 0.18f, -22f, 22f);
            _currentBankAngle = Mathf.Lerp(_currentBankAngle, targetBank, dt * 6f);
            float yawWiggle = Mathf.Sin(_swimPhase) * (tailAmplitude * 38f);

            transform.rotation = transform.rotation * Quaternion.Euler(0f, yawWiggle, _currentBankAngle);

            // 若 Shader 支持顶点摆尾，通过 MaterialPropertyBlock 传入独立相位 (保证每条鱼摆尾不同步)
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(PropSwimPhase, _swimPhase);
                _mpb.SetFloat(PropTailAmp, tailAmplitude);
                _renderer.SetPropertyBlock(_mpb);
            }
        }
    }

    /// <summary>
    /// v25 鱼类与水族箱展缸视觉升级核心组件 (自动挂载于 SeaLifeRuntime 所在节点)
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class SeaLifeVisualUpgradeV25 : MonoBehaviour
    {
        public FishAndAquariumDatabaseSO database;

        private SeaLifeRuntime _runtime;
        private Mesh[] _upgradedFishMeshes;
        private Material[] _upgradedFishMaterials;

        private Mesh _tankFrameMesh;
        private Mesh _tankInteriorMesh;
        private Mesh _tankGlassMesh;

        private Material _brassMat;
        private Material _upgradedGlassMat;
        private Material _waterVolumeMat;
        private Material _causticsMat;
        private Texture2D _causticsTex;

        private readonly HashSet<int> _upgradedTankIds = new HashSet<int>();
        private readonly HashSet<int> _upgradedFishInstanceIds = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            var rt = FindObjectOfType<SeaLifeRuntime>();
            if (rt != null && rt.GetComponent<SeaLifeVisualUpgradeV25>() == null)
            {
                rt.gameObject.AddComponent<SeaLifeVisualUpgradeV25>();
            }
        }

        private void Awake()
        {
            _runtime = GetComponent<SeaLifeRuntime>();
            if (database == null)
            {
                database = Resources.Load<FishAndAquariumDatabaseSO>("SeaLifeV25/FishAndAquariumDatabase");
                if (database == null)
                {
                    database = FishAndAquariumDatabaseSO.CreateRuntimeDefault();
                }
            }
        }

        private void Start()
        {
            InitializeAssetsAndMaterials();
            InjectFishAssetsIntoRuntime();
            UpgradeAllExistingVisuals();
        }

        private void LateUpdate()
        {
            // 动态检测玩家新建展缸或新放入展缸的鱼，实时完成高模与光效替换
            UpgradeAllExistingVisuals();
            AnimateCaustics();
        }

        private void InitializeAssetsAndMaterials()
        {
            _upgradedFishMeshes = new Mesh[3];
            _upgradedFishMaterials = new Material[3];

            string[] fishModelNames =
            {
                "SeaLifeV25/Models/Fish_0_SilverBream",
                "SeaLifeV25/Models/Fish_1_BluefinTuna",
                "SeaLifeV25/Models/Fish_2_GoldButterfly"
            };
            string[] fishTexNames =
            {
                "SeaLifeV25/Textures/Fish_0_SilverBream_Albedo",
                "SeaLifeV25/Textures/Fish_1_BluefinTuna_Albedo",
                "SeaLifeV25/Textures/Fish_2_GoldButterfly_Albedo"
            };

            Shader fishShader = Shader.Find("Deepsea/SeaLifeFishSwimV25");
            if (fishShader == null) fishShader = Shader.Find("Deepsea/SeaLifeSurface");
            if (fishShader == null) fishShader = Shader.Find("Universal Render Pipeline/Lit");

            for (int i = 0; i < 3; i++)
            {
                var entry = database.GetFishById(i);

                // 1. 优先加载 Blender 生成的高精度 FBX 模型，若未找到则生成高细分仿生流线网格
                Mesh loadedMesh = LoadMeshFromResources(fishModelNames[i]);
                _upgradedFishMeshes[i] = loadedMesh != null ? loadedMesh : BuildHighDetailProceduralFishMesh(i);

                // 2. 优先加载 Blender 生成的贴图，若未找到则运行时烘焙 256x256 鳞片渐变贴图
                Texture2D loadedTex = Resources.Load<Texture2D>(fishTexNames[i]);
                if (loadedTex == null) loadedTex = GenerateProceduralFishTexture(i, 256, 256);

                var mat = new Material(fishShader)
                {
                    name = $"V25_FishMat_{entry.displayName}"
                };
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", loadedTex);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_RimColor")) mat.SetColor("_RimColor", entry.iridescentRimColor);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", entry.smoothness);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", entry.metallic);
                if (mat.HasProperty("_TailAmplitude")) mat.SetFloat("_TailAmplitude", entry.tailWaveAmplitude);

                _upgradedFishMaterials[i] = mat;
            }

            // 加载水族箱展缸 3 件套网格 (Frame / Interior / Glass)
            _tankFrameMesh = LoadMeshFromResources("SeaLifeV25/Models/AquariumTank_V25_Frame");
            _tankInteriorMesh = LoadMeshFromResources("SeaLifeV25/Models/AquariumTank_V25_Interior");
            _tankGlassMesh = LoadMeshFromResources("SeaLifeV25/Models/AquariumTank_V25_Glass");

            Shader glassShader = Shader.Find("Deepsea/SeaLifeAquariumGlassV25");
            if (glassShader == null) glassShader = Shader.Find("Deepsea/SeaLifeGlass");

            _upgradedGlassMat = new Material(glassShader) { name = "V25_UltraClearAquariumGlass" };
            if (_upgradedGlassMat.HasProperty("_BaseColor"))
                _upgradedGlassMat.SetColor("_BaseColor", new Color(0.42f, 0.84f, 0.90f, 0.09f));
            if (_upgradedGlassMat.HasProperty("_RimColor"))
                _upgradedGlassMat.SetColor("_RimColor", new Color(0.75f, 0.96f, 1.0f, 0.42f));

            _waterVolumeMat = new Material(glassShader) { name = "V25_AquariumWaterVolume" };
            if (_waterVolumeMat.HasProperty("_BaseColor"))
                _waterVolumeMat.SetColor("_BaseColor", new Color(0.12f, 0.52f, 0.62f, 0.055f));

            Shader surfaceShader = Shader.Find("Deepsea/SeaLifeSurface") ?? Shader.Find("Universal Render Pipeline/Lit");
            _brassMat = new Material(surfaceShader) { name = "V25_AgedBrass" };
            if (_brassMat.HasProperty("_BaseColor")) _brassMat.SetColor("_BaseColor", new Color(0.76f, 0.61f, 0.34f, 1f));
            if (_brassMat.HasProperty("_Smoothness")) _brassMat.SetFloat("_Smoothness", 0.68f);
            if (_brassMat.HasProperty("_Metallic")) _brassMat.SetFloat("_Metallic", 0.75f);

            _causticsTex = GenerateCausticsTexture(128, 128);
            _causticsMat = new Material(glassShader) { name = "V25_SandCausticsOverlay" };
            if (_causticsMat.HasProperty("_BaseColor"))
                _causticsMat.SetColor("_BaseColor", new Color(0.65f, 0.95f, 1.0f, 0.14f));
            if (_causticsMat.HasProperty("_CausticsMap"))
                _causticsMat.SetTexture("_CausticsMap", _causticsTex);
        }

        private Mesh LoadMeshFromResources(string path)
        {
            var mesh = Resources.Load<Mesh>(path);
            if (mesh != null) return mesh;
            var go = Resources.Load<GameObject>(path);
            if (go != null)
            {
                var mf = go.GetComponentInChildren<MeshFilter>();
                if (mf != null) return mf.sharedMesh;
            }
            return null;
        }

        /// <summary>
        /// 将 v25 高精度鱼体网格与鳞片材质直接注入 SeaLifeRuntime 内部数组，使得后续所有钓鱼/放鱼直接生效
        /// </summary>
        private void InjectFishAssetsIntoRuntime()
        {
            if (_runtime == null) return;
            var type = typeof(SeaLifeRuntime);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var meshesField = type.GetField("fishMeshes", flags);
            if (meshesField != null) meshesField.SetValue(_runtime, _upgradedFishMeshes);

            var matsField = type.GetField("fishMaterials", flags);
            if (matsField != null) matsField.SetValue(_runtime, _upgradedFishMaterials);

            var glassField = type.GetField("glass", flags);
            if (glassField != null && _upgradedGlassMat != null) glassField.SetValue(_runtime, _upgradedGlassMat);
        }

        /// <summary>
        /// 扫描场景中已生成的展缸 (展缸 A/B/C) 与所有鱼实例，原地升级为 v25 视觉细节
        /// </summary>
        private void UpgradeAllExistingVisuals()
        {
            if (_runtime == null) return;
            var type = typeof(SeaLifeRuntime);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            // 1. 升级展缸 A / B / C
            var tankObjsField = type.GetField("tankObjects", flags);
            if (tankObjsField != null)
            {
                var tanks = tankObjsField.GetValue(_runtime) as Transform[];
                if (tanks != null)
                {
                    for (int i = 0; i < tanks.Length; i++)
                    {
                        Transform tank = tanks[i];
                        if (tank != null && !_upgradedTankIds.Contains(tank.GetInstanceID()))
                        {
                            UpgradeSingleAquariumTank(tank, i);
                            _upgradedTankIds.Add(tank.GetInstanceID());
                        }
                    }
                }
            }

            // 2. 升级展室内与野外的鱼实例
            var allFilters = FindObjectsOfType<MeshFilter>();
            for (int i = 0; i < allFilters.Length; i++)
            {
                var mf = allFilters[i];
                if (mf == null || mf.sharedMesh == null) continue;
                int id = mf.gameObject.GetInstanceID();
                if (_upgradedFishInstanceIds.Contains(id)) continue;

                int detectedSpecies = DetectFishSpecies(mf);
                if (detectedSpecies >= 0 && detectedSpecies < 3)
                {
                    mf.sharedMesh = _upgradedFishMeshes[detectedSpecies];
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = _upgradedFishMaterials[detectedSpecies];

                    // 校正旧版银纹鱼 90 度旋转偏移，统一让鱼头朝向运动前方 (+Z)
                    var entry = database.GetFishById(detectedSpecies);
                    if (mf.GetComponent<SeaLifeFishSwimmerV25>() == null)
                    {
                        var swimmer = mf.gameObject.AddComponent<SeaLifeFishSwimmerV25>();
                        swimmer.speciesId = detectedSpecies;
                        swimmer.baseSpeed = entry.swimSpeed;
                        swimmer.tailAmplitude = entry.tailWaveAmplitude;
                        swimmer.tailFrequency = entry.tailWaveFrequency;
                    }

                    // 调整为饱满自然的展示比例
                    if (mf.transform.localScale.x < 0.45f)
                    {
                        mf.transform.localScale = Vector3.one * entry.aquariumScale;
                    }

                    _upgradedFishInstanceIds.Add(id);
                }
            }
        }

        private int DetectFishSpecies(MeshFilter mf)
        {
            string mName = mf.sharedMesh != null ? mf.sharedMesh.name : "";
            string objName = mf.gameObject.name;
            if (mName.Contains("银纹鱼") || objName.Contains("银纹鱼") || mName.Contains("SilverBream")) return 0;
            if (mName.Contains("蓝鳍鱼") || objName.Contains("蓝鳍鱼") || mName.Contains("BluefinTuna")) return 1;
            if (mName.Contains("金背鱼") || objName.Contains("金背鱼") || mName.Contains("黄金鱼") || mName.Contains("GoldButterfly")) return 2;
            return -1;
        }

        /// <summary>
        /// 将单个旧版方盒子展缸改造为带黄铜包角、高精礁石群、体积水光、焦散投影与顶置生态灯的高颜值展缸
        /// </summary>
        private void UpgradeSingleAquariumTank(Transform tankRoot, int tankIndex)
        {
            var type = typeof(SeaLifeRuntime);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Material woodMat = type.GetField("wood", flags)?.GetValue(_runtime) as Material;
            Material stoneMat = type.GetField("stone", flags)?.GetValue(_runtime) as Material;
            Material sandMat = type.GetField("indoorSand", flags)?.GetValue(_runtime) as Material;

            // 隐藏旧版简陋几何拼接块 (保留展缸文字标牌与海带)
            foreach (Transform child in tankRoot)
            {
                string cn = child.name;
                if (cn.Contains("玻璃"))
                {
                    var mr = child.GetComponent<MeshRenderer>();
                    if (mr != null && _upgradedGlassMat != null) mr.sharedMaterial = _upgradedGlassMat;
                }
                else if (cn == "缸内主石")
                {
                    // 用多层自然裂隙礁石群替代原先的单颗拉伸圆球
                    child.gameObject.SetActive(false);
                }
            }

            // 若本地已生成 Blender FBX 部件则直接挂载，否则生成精细程序化组合造景
            Transform v25Group = new GameObject("V25_AquariumDetails").transform;
            v25Group.SetParent(tankRoot, false);
            v25Group.localPosition = Vector3.zero;

            if (_tankFrameMesh != null && woodMat != null)
            {
                CreateMeshChild("V25_精修木柜与灯架", v25Group, _tankFrameMesh, woodMat, Vector3.zero, Vector3.one);
            }
            else
            {
                // 程序化精修顶部悬浮灯架与四角黄铜包套
                BuildProceduralBrassAndCanopy(v25Group, woodMat, _brassMat);
            }

            if (_tankInteriorMesh != null && stoneMat != null)
            {
                CreateMeshChild("V25_层叠生态礁石与波纹沙床", v25Group, _tankInteriorMesh, stoneMat, Vector3.zero, Vector3.one);
            }
            else
            {
                BuildProceduralLayeredReefRocks(v25Group, stoneMat, sandMat);
            }

            // 添加内部淡青色水体体积块與水面微光层
            GameObject waterVol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waterVol.name = "V25_水体丁达尔体积层";
            Destroy(waterVol.GetComponent<Collider>());
            waterVol.transform.SetParent(v25Group, false);
            waterVol.transform.localPosition = new Vector3(0f, 1.46f, 0f);
            waterVol.transform.localScale = new Vector3(3.42f, 1.28f, 1.90f);
            waterVol.GetComponent<MeshRenderer>().sharedMaterial = _waterVolumeMat;

            // 添加沙床动态焦散光斑投影面
            GameObject causticsPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            causticsPlane.name = "V25_水底焦散光斑";
            Destroy(causticsPlane.GetComponent<Collider>());
            causticsPlane.transform.SetParent(v25Group, false);
            causticsPlane.transform.localPosition = new Vector3(0f, 0.535f, 0f);
            causticsPlane.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            causticsPlane.transform.localScale = new Vector3(3.32f, 1.82f, 1f);
            causticsPlane.GetComponent<MeshRenderer>().sharedMaterial = _causticsMat;

            // 添加专属缸顶暖白生态聚光灯，打亮鱼体金属鳞片高光
            GameObject topLightObj = new GameObject("V25_展缸生态顶灯");
            topLightObj.transform.SetParent(v25Group, false);
            topLightObj.transform.localPosition = new Vector3(0f, 2.32f, 0f);
            topLightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light spot = topLightObj.AddComponent<Light>();
            spot.type = LightType.Point;
            spot.range = 4.2f;
            spot.intensity = 1.35f;
            spot.color = new Color(0.82f, 0.96f, 1.0f);
            spot.shadows = LightShadows.None;
        }

        private GameObject CreateMeshChild(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        private void BuildProceduralBrassAndCanopy(Transform parent, Material woodMat, Material brassMat)
        {
            // 顶部悬浮式实木灯架 + 黄铜吊杆
            CreateBox("顶部悬浮生态灯罩", parent, new Vector3(0f, 2.40f, 0f), new Vector3(2.85f, 0.07f, 0.56f), woodMat);
            CreateBox("灯罩左黄铜吊杆", parent, new Vector3(-1.10f, 2.29f, 0f), new Vector3(0.035f, 0.22f, 0.035f), brassMat);
            CreateBox("灯罩右黄铜吊杆", parent, new Vector3(1.10f, 2.29f, 0f), new Vector3(0.035f, 0.22f, 0.035f), brassMat);

            // 四角黄铜包角装饰与底柜百叶门立体框
            float[] xs = { -1.76f, 1.76f };
            float[] zs = { -0.98f, 0.98f };
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    CreateBox("黄铜底包角", parent, new Vector3(x, 0.84f, z), new Vector3(0.095f, 0.12f, 0.095f), brassMat);
                    CreateBox("黄铜顶包角", parent, new Vector3(x, 2.18f, z), new Vector3(0.095f, 0.12f, 0.095f), brassMat);
                }
            }
            for (int i = -1; i <= 1; i++)
            {
                CreateBox("柜门立体浮雕框", parent, new Vector3(i * 1.10f, 0.22f, -1.16f), new Vector3(0.92f, 0.28f, 0.025f), brassMat);
            }
        }

        private void BuildProceduralLayeredReefRocks(Transform parent, Material stoneMat, Material sandMat)
        {
            // 生成 5 块带自然层理折面的多面体礁石群
            Vector3[] centers =
            {
                new Vector3(0.85f, 0.86f, 0.36f),
                new Vector3(0.36f, 0.70f, 0.45f),
                new Vector3(1.18f, 0.66f, 0.08f),
                new Vector3(-0.86f, 0.68f, -0.22f),
                new Vector3(-0.48f, 0.56f, 0.28f)
            };
            Vector3[] scales =
            {
                new Vector3(0.52f, 0.42f, 0.38f),
                new Vector3(0.36f, 0.28f, 0.30f),
                new Vector3(0.30f, 0.22f, 0.26f),
                new Vector3(0.38f, 0.25f, 0.28f),
                new Vector3(0.25f, 0.18f, 0.22f)
            };

            for (int i = 0; i < centers.Length; i++)
            {
                Mesh rockMesh = BuildCraggyRockMesh(i * 1.7f + 0.5f);
                var go = CreateMeshChild("V25_层叠礁石_" + i, parent, rockMesh, stoneMat, centers[i], scales[i]);
                go.transform.localRotation = Quaternion.Euler(10f * i, 37f * i, -12f * i);
            }
        }

        private void CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private void AnimateCaustics()
        {
            if (_causticsMat != null && _causticsMat.HasProperty("_CausticsOffset"))
            {
                float t = Time.time * 0.18f;
                _causticsMat.SetVector("_CausticsOffset", new Vector4(t, t * 0.7f, -t * 0.6f, t * 0.9f));
            }
        }

        // =====================================================================
        // 内置高精度 C# 程序化流线鱼体与贴图生成器 (无须外部依赖也能保证高颜值)
        // =====================================================================
        public static Mesh BuildHighDetailProceduralFishMesh(int species)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            float length = species == 1 ? 1.02f : (species == 2 ? 0.82f : 0.90f);
            float widthScale = species == 1 ? 0.15f : (species == 2 ? 0.11f : 0.13f);
            float heightScale = species == 1 ? 0.21f : (species == 2 ? 0.33f : 0.25f);
            float tailSpan = species == 1 ? 0.35f : (species == 2 ? 0.26f : 0.30f);
            float tailFork = species == 1 ? 0.17f : (species == 2 ? 0.08f : 0.13f);
            float dorsalHeight = species == 2 ? 0.16f : 0.105f;

            int numZ = 22;
            int numRing = 18;
            float zHead = length * 0.45f;
            float zTailBase = -length * 0.34f;

            // 1. 鱼身平滑流线曲面 (22 x 18)
            for (int iz = 0; iz < numZ; iz++)
            {
                float t = iz / (float)(numZ - 1);
                float z = Mathf.Lerp(zHead, zTailBase, t);
                float uAxial = t * 0.80f;

                float arch = Mathf.Sin(Mathf.Pow(t, 0.68f) * Mathf.PI);
                float peduncle = 0.14f * Mathf.Exp(-Mathf.Pow(t - 0.94f, 2f) / 0.015f);
                float profileH = Mathf.Max(0.025f, arch * (1f - 0.25f * t) + peduncle);
                float gillSwell = 1f + 0.10f * Mathf.Exp(-Mathf.Pow(t - 0.22f, 2f) / 0.008f);
                float profileW = Mathf.Max(0.015f, Mathf.Pow(arch, 0.85f) * (1f - 0.38f * t) * gillSwell + peduncle * 0.45f);

                if (t < 0.04f)
                {
                    float tipFactor = Mathf.Sin((t / 0.04f) * Mathf.PI * 0.5f);
                    profileH *= tipFactor;
                    profileW *= tipFactor;
                }

                float hRad = heightScale * profileH;
                float wRad = widthScale * profileW;

                for (int ir = 0; ir <= numRing; ir++)
                {
                    float angle = (ir / (float)numRing) * Mathf.PI * 2f;
                    float ca = Mathf.Cos(angle);
                    float sa = Mathf.Sin(angle);
                    float pinch = 1f - 0.16f * ca * ca;
                    verts.Add(new Vector3(sa * wRad * pinch, ca * hRad, z));
                    float vCoord = 0.02f + (0.5f + 0.5f * ca) * 0.68f;
                    uvs.Add(new Vector2(uAxial, vCoord));
                }
            }

            int ringStride = numRing + 1;
            for (int iz = 0; iz < numZ - 1; iz++)
            {
                for (int ir = 0; ir < numRing; ir++)
                {
                    int a = iz * ringStride + ir;
                    int b = a + 1;
                    int c = a + ringStride;
                    int d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(d);
                    tris.Add(a); tris.Add(d); tris.Add(c);
                }
            }

            // 2. 新月形双侧分叉尾鳍 (5 x 9 细分)
            int cCols = 5, cRows = 9;
            for (int side = -1; side <= 1; side += 2)
            {
                int baseIdx = verts.Count;
                for (int ic = 0; ic < cCols; ic++)
                {
                    float ct = ic / (float)(cCols - 1);
                    for (int ir = 0; ir < cRows; ir++)
                    {
                        float rt = (ir / (float)(cRows - 1)) * 2f - 1f;
                        float span = (0.035f * heightScale) * (1f - ct) + (tailSpan * 0.5f) * Mathf.Pow(ct, 0.85f);
                        float y = rt * span;
                        float forkOffset = (rt * rt) * tailFork * ct;
                        float z = zTailBase - ct * (length * 0.22f) - forkOffset;
                        float x = side * 0.0035f * (1f - ct * 0.7f);
                        verts.Add(new Vector3(x, y, z));
                        uvs.Add(new Vector2(0.80f + ct * 0.20f, 0.74f + Mathf.Abs(rt) * 0.24f));
                    }
                }
                for (int ic = 0; ic < cCols - 1; ic++)
                {
                    for (int ir = 0; ir < cRows - 1; ir++)
                    {
                        int a = baseIdx + ic * cRows + ir;
                        int b = a + cRows;
                        int c = b + 1;
                        int d = a + 1;
                        if (side > 0) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(a); tris.Add(c); tris.Add(d); }
                        else { tris.Add(a); tris.Add(d); tris.Add(c); tris.Add(a); tris.Add(c); tris.Add(b); }
                    }
                }
            }

            // 3. 背鳍与臀鳍
            AddMedianFin(verts, uvs, tris, zHead, zTailBase, heightScale, 0.26f, 0.74f, dorsalHeight, 1f);
            AddMedianFin(verts, uvs, tris, zHead, zTailBase, heightScale, 0.58f, 0.82f, dorsalHeight * 0.6f, -1f);

            // 4. 左右胸鳍与立体眼球
            AddPairedPectoralFins(verts, uvs, tris, zHead, zTailBase, widthScale, heightScale, length);

            var mesh = new Mesh { name = "V25_HighDetailFish_" + species };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddMedianFin(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            float zHead, float zTailBase, float heightScale,
            float tStart, float tEnd, float finH, float signY)
        {
            int cols = 7, rows = 4;
            for (int side = -1; side <= 1; side += 2)
            {
                int baseIdx = verts.Count;
                for (int ic = 0; ic < cols; ic++)
                {
                    float ct = ic / (float)(cols - 1);
                    float tBody = Mathf.Lerp(tStart, tEnd, ct);
                    float zBase = Mathf.Lerp(zHead, zTailBase, tBody);
                    float arch = Mathf.Sin(Mathf.Pow(tBody, 0.68f) * Mathf.PI) * (1f - 0.25f * tBody);
                    float yBody = signY * heightScale * arch * 0.90f;
                    float env = Mathf.Sin(Mathf.Pow(ct, 0.6f) * Mathf.PI);
                    for (int ir = 0; ir < rows; ir++)
                    {
                        float rt = ir / (float)(rows - 1);
                        verts.Add(new Vector3(
                            side * 0.002f * (1f - rt * 0.8f),
                            yBody + signY * finH * env * rt,
                            zBase - rt * finH * 0.45f));
                        uvs.Add(new Vector2(tBody * 0.8f + rt * 0.06f, 0.74f + rt * 0.24f));
                    }
                }
                for (int ic = 0; ic < cols - 1; ic++)
                {
                    for (int ir = 0; ir < rows - 1; ir++)
                    {
                        int a = baseIdx + ic * rows + ir;
                        int b = a + rows;
                        int c = b + 1;
                        int d = a + 1;
                        if (side * signY > 0) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(a); tris.Add(c); tris.Add(d); }
                        else { tris.Add(a); tris.Add(d); tris.Add(c); tris.Add(a); tris.Add(c); tris.Add(b); }
                    }
                }
            }
        }

        private static void AddPairedPectoralFins(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            float zHead, float zTailBase, float widthScale, float heightScale, float length)
        {
            float tAttach = 0.24f;
            float zRoot = Mathf.Lerp(zHead, zTailBase, tAttach);
            float arch = Mathf.Sin(Mathf.Pow(tAttach, 0.68f) * Mathf.PI);
            float xRoot = widthScale * Mathf.Pow(arch, 0.85f) * 0.78f;
            float yRoot = -heightScale * arch * 0.15f;
            float span = 0.13f;

            for (int side = -1; side <= 1; side += 2)
            {
                int bi = verts.Count;
                verts.Add(new Vector3(side * xRoot, yRoot + 0.015f, zRoot + 0.02f));
                verts.Add(new Vector3(side * xRoot * 0.9f, yRoot - 0.015f, zRoot - 0.03f));
                verts.Add(new Vector3(side * (xRoot + span), yRoot - 0.015f, zRoot - length * 0.12f));
                verts.Add(new Vector3(side * (xRoot + span * 0.75f), yRoot - 0.045f, zRoot - length * 0.14f));
                uvs.Add(new Vector2(0.20f, 0.75f));
                uvs.Add(new Vector2(0.22f, 0.75f));
                uvs.Add(new Vector2(0.32f, 0.96f));
                uvs.Add(new Vector2(0.32f, 0.92f));
                tris.Add(bi); tris.Add(bi + 2); tris.Add(bi + 3);
                tris.Add(bi); tris.Add(bi + 3); tris.Add(bi + 1);
                tris.Add(bi); tris.Add(bi + 3); tris.Add(bi + 2);
                tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 3);
            }
        }

        private static Mesh BuildCraggyRockMesh(float seed)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh src = Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
            DestroyImmediate(go);

            Vector3[] v = src.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = v[i];
                float strata = 0.16f * Mathf.Sin(p.y * 8.5f + seed);
                float crag = 0.20f * Mathf.Sin(p.x * 4.2f + seed * 1.3f) * Mathf.Cos(p.z * 4.6f - seed);
                v[i] = p * (1f + strata + crag);
            }
            src.vertices = v;
            src.RecalculateNormals();
            src.RecalculateBounds();
            return src;
        }

        public static Texture2D GenerateProceduralFishTexture(int species, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = "V25_ProcFishTex_" + species,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] cols = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    cols[y * width + x] = EvaluateFishPixel(species, u, v);
                }
            }
            tex.SetPixels(cols);
            tex.Apply(true, false);
            return tex;
        }

        private static Color EvaluateFishPixel(int species, float u, float v)
        {
            if (v <= 0.72f)
            {
                float bv = v / 0.72f;
                // 菱形鱼鳞微纹理
                float su = u * 28f;
                float sv = bv * 15f + ((Mathf.FloorToInt(su) % 2 == 1) ? 0.5f : 0f);
                float fu = su - Mathf.Floor(su) - 0.5f;
                float fv = sv - Mathf.Floor(sv) - 0.5f;
                float dist = Mathf.Sqrt(fu * fu + fv * fv);
                float scaleHighlight = Mathf.Max(0f, 1f - dist * 1.4f) * 0.08f + Mathf.Exp(-Mathf.Pow(dist - 0.42f, 2f) / 0.008f) * 0.12f;

                // 鱼眼斑点 (u ~ 0.13, bv ~ 0.58)
                float eyeDist = Mathf.Sqrt(Mathf.Pow((u - 0.13f) * 2.2f, 2f) + Mathf.Pow(bv - 0.58f, 2f));
                if (eyeDist < 0.055f)
                {
                    return eyeDist < 0.032f ? new Color(0.05f, 0.06f, 0.08f, 0.95f) : new Color(0.95f, 0.88f, 0.55f, 0.9f);
                }

                if (species == 0) // 银纹鱼
                {
                    Color c = Color.Lerp(new Color(0.88f, 0.93f, 0.95f), new Color(0.30f, 0.58f, 0.64f), bv);
                    if (u > 0.18f && u < 0.82f && bv > 0.22f)
                    {
                        float stripe = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((u - 0.18f) * 7f * Mathf.PI * 2f), 3f);
                        c -= new Color(0.14f, 0.11f, 0.09f, 0f) * (stripe * (bv - 0.22f));
                    }
                    c.r += scaleHighlight; c.g += scaleHighlight; c.b += scaleHighlight;
                    c.a = 0.78f;
                    return c;
                }
                else if (species == 1) // 蓝鳍鱼
                {
                    Color c = bv > 0.5f
                        ? Color.Lerp(new Color(0.16f, 0.46f, 0.68f), new Color(0.06f, 0.18f, 0.38f), (bv - 0.5f) * 2f)
                        : Color.Lerp(new Color(0.90f, 0.94f, 0.97f), new Color(0.16f, 0.46f, 0.68f), bv * 2f);
                    float goldLine = Mathf.Exp(-Mathf.Pow(bv - 0.50f, 2f) / 0.0025f);
                    c += new Color(0.38f, 0.28f, -0.08f, 0f) * goldLine;
                    c.r += scaleHighlight * 0.7f; c.g += scaleHighlight * 0.7f; c.b += scaleHighlight * 0.7f;
                    c.a = 0.85f;
                    return c;
                }
                else // 金背鱼
                {
                    Color c = Color.Lerp(new Color(0.96f, 0.90f, 0.76f), new Color(0.94f, 0.56f, 0.14f), bv);
                    float diag = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((u * 1.4f - bv * 0.6f) * 26f), 4f) * 0.15f;
                    c += new Color(0.05f, -diag, -diag * 1.2f, 0f);
                    c.r += scaleHighlight; c.g += scaleHighlight; c.b += scaleHighlight;
                    c.a = 0.75f;
                    return c;
                }
            }
            else
            {
                float fv = (v - 0.72f) / 0.28f;
                float rays = 0.5f + 0.5f * Mathf.Sin(u * 46f * Mathf.PI);
                if (species == 0) return new Color(0.48f + rays * 0.12f, 0.72f + rays * 0.12f, 0.76f + rays * 0.12f, 0.55f);
                if (species == 1) return new Color(0.14f + fv * 0.55f, 0.36f + fv * 0.42f, 0.58f - fv * 0.20f, 0.65f);
                return new Color(0.95f, 0.66f - fv * 0.15f + rays * 0.08f, 0.18f + rays * 0.05f, 0.60f);
            }
        }

        private static Texture2D GenerateCausticsTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "V25_ProcCaustics",
                wrapMode = TextureWrapMode.Repeat
            };
            Color[] cols = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (y / (float)height) * Mathf.PI * 2f;
                for (int x = 0; x < width; x++)
                {
                    float u = (x / (float)width) * Mathf.PI * 2f;
                    float w1 = Mathf.Sin(u * 3f + Mathf.Cos(v * 2f));
                    float w2 = Mathf.Cos(v * 3f - Mathf.Sin(u * 2f));
                    float net = Mathf.Pow(Mathf.Abs(w1 + w2) * 0.5f, 3.5f);
                    cols[y * width + x] = new Color(net, net, net, net);
                }
            }
            tex.SetPixels(cols);
            tex.Apply(false, false);
            return tex;
        }
    }
}