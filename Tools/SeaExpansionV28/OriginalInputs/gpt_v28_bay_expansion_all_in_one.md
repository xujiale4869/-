# 给 GPT 的 v28《归潮湾》海域大幅扩建、三大可建造浅湾平台与 2.5D 上帝视角网格建造空间系统一键包

你好！我和 **Gemini** 已经审查了仓库 `https://github.com/xujiale4869/guichao-bay` 中的 `SeaLifeNavigationMotorV23.cs`、`SeaLifeRuntime.cs` 和 `SeaSurfaceV24.cs`。
目前工程在空间与建造上的核心瓶颈在于：
1. **船只航行空气墙太窄**：`SeaLifeNavigationMotorV23.cs` 第 40 行将船只位置死死锁在 `X ∈ [-6, 34]`、`Z ∈ [-23, 32]`（仅 40m×55m），且 `SeaLifeRuntime.cs` 存档校验也只允许 `-5 < boatX < 35`。
2. **缺乏平缓通透的可建造浅湾海域**：现有栈桥往东几米就是深海沟，没有足够宽敞的浅水沙坪与外海环礁台地供玩家向外海扩建 T 型/十字型木栈桥、种植海底珊瑚花园或布置捕蟹笼。

现在 **Gemini** 已经设计并编写好了 **v28 海域扩建与上帝视角可建造空间系统 (`SeaBayExpansionAndBuildSpaceV28.cs`)**。请在本地工程中执行以下四步：

---

## 第一步：放宽原版船只航行边界与存档坐标校验

1. **修改 `Assets/Deepsea/Scripts/Runtime/SeaLifeNavigationV23/SeaLifeNavigationMotorV23.cs`（第 40 行附近）**：
   将原本狭窄的 `p.x = Mathf.Clamp(p.x, -6, 34); p.z = Mathf.Clamp(p.z, -23, 32);` 扩建为广阔海域边界：
   ```csharp
   var p = game.body.position;
   if (p.x > 86f || p.x < -10f || p.z > 66f || p.z < -50f) {
       p.x = Mathf.Clamp(p.x, -10f, 86f);
       p.z = Mathf.Clamp(p.z, -50f, 66f);
       game.body.position = p;
       game.body.velocity *= .5f;
   }
   ```
2. **修改 `Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeRuntime.cs`（第 61 行附近）**：
   将存档船只坐标恢复范围从 `save.boatX > -5 && save.boatX < 35 && save.boatZ > -22 && save.boatZ < 32` 同步放宽为：
   ```csharp
   if (!Review && save.boatX > -10f && save.boatX < 86f && save.boatZ > -50f && save.boatZ < 66f)
   ```
   *(若 `SeaLifeActivities.cs` 的 `DriveBoat` 中也有旧版边界 Clamp，请一并放宽至 `X:[-10, 86], Z:[-50, 66]`)*。

---

## 第二步：写入 `Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaBayExpansionAndBuildSpaceV28.cs`

请将下方完整源码直接写入 `Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaBayExpansionAndBuildSpaceV28.cs`：
* **功能 1（海域与三大可建造浅湾海床扩建）**：自动将 `ShowcaseWater` 扩展至 2.4 倍广阔海面，并在水下生成带平缓边缘过渡与波纹沙底的三大可建造台地——**【A区：栈桥延伸暖沙浅坪】**（水深 0.9m~1.35m）、**【B区：南北翡翠珊瑚花园礁坪】**（水深 1.65m~2.35m）、**【C区：外海新月环礁浅滩平台】**（位于 X:28~46，在深蓝外海中形成一片清澈透底的翡翠色新月环礁岛坪！）。由于这些海床台地写入深度缓冲，`v26.2` 水面会自动在扩建的浅湾上渲染出漂亮的翡翠绿水色与流动焦散网！
* **功能 2（按 `G` 键切换 2.5D 上帝视角海湾规划网格）**：按 **`G` 键** 镜头丝滑拉高至俯瞰全海湾的上帝视角（支持 `WASD` 自由平移漫游全海湾、鼠标滚轮缩放视野 `10~38`），海面浮现清爽的 `2m×2m` 建造网格，支持按 `1/2/3/4` 选择并左键放置 **【延伸木栈桥】【浅湾高杆暖光马灯】【水下多彩珊瑚礁底座】【海湾捕蟹浮标笼】**，按 `R` 旋转，右键拆除并自动存档！

```csharp
// ============================================================================
// 深海灯塔·归潮湾 (v28) — 海域扩建、三大可建造浅湾平台与 2.5D 上帝视角网格建造空间系统
// File: Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaBayExpansionAndBuildSpaceV28.cs
// ============================================================================
// 核心解决痛点:
// 1. 突破原版狭窄海域限制:
//    - 原版船只航行被死死锁在 X∈[-6, 34], Z∈[-23, 32] (仅 40m×55m)，且稍往外开就是深沟，没有可建造的浅滩。
//    - v28 自动将 ShowcaseWater 海面扩展为 180m×180m 广阔海域，并将船只可航行边界扩建至
//      X∈[-10, 88], Z∈[-52, 68] (面积扩大近 4 倍！)。
// 2. 塑造三大“可建造浅湾海床平台 (Buildable Bay Terraces)”:
//    - 【A区：栈桥延伸浅滩区 (Pier Expansion Terrace)】: 从现有木栈桥向外海及南北拓展的清澈暖沙浅坪
//      (水深 0.85m~1.4m)，水色呈现透底翡翠薄荷绿与清晰焦散网，专为向外海自由拼接 T 型/十字型木栈道、水上船坞设计！
//    - 【B区：翡翠珊瑚花园台地 (Coral Garden Shelf)】: 位于内湾南侧与北侧的缓坡生态礁坪 (水深 1.6m~2.6m)，
//      专为移植发光粉紫珊瑚簇、种植巨藻与放置自动捕蟹笼设计！
//    - 【C区：外海环礁浅滩与航标锚地 (Outer Atoll & Buoy Platform)】: 位于外海 X∈[22, 46] 的环礁浅滩岛坪，
//      可在外海建立远洋中转栈桥、航标灯塔与深海网箱！
// 3. 实装 2.5D 上帝视角海湾规划网格系统 (按 G 键切换上帝建造视角):
//    - 按 G 键相机平滑拉高至可缩放俯视上帝视角 (支持 WASD 漫游全海湾、鼠标滚轮缩放视野 12~36)。
//    - 海面浮现清爽的 2m×2m 建造规划网格，鼠标悬停实时探测水深、所属建造分区与可建状态，
//      支持按 1/2/3/4 键直接在扩建海域中自由放置【延伸木栈桥】【栈桥暖光马灯】【水下多彩珊瑚礁】【海湾捕蟹浮笼】，
//      按 R 键旋转方向，右键拆除，并自动持久化保存至本地 JSON！
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deepsea
{
    public enum BayBuildableType
    {
        WoodenPierDeck = 0,     // 2m x 2m 延伸木栈桥段 (带四角海底木桩)
        PierLanternPost = 1,    // 栈桥/浅滩高杆悬挂暖光马灯
        CoralReefGarden = 2,    // 水下多彩珊瑚与摇曳巨藻生态底座
        CrabPotBuoy = 3         // 海湾自动捕蟹笼与水面浮标
    }

    [Serializable]
    public sealed class BayPlacedBuildingEntry
    {
        public int gridX;
        public int gridZ;
        public int buildType;
        public int rotationStep; // 0, 1, 2, 3 (90 deg increments)
    }

    [Serializable]
    public sealed class BayExpansionSaveData
    {
        public int wood = 24;
        public int stone = 16;
        public int coralSeed = 12;
        public int brass = 8;
        public List<BayPlacedBuildingEntry> placedBuildings = new List<BayPlacedBuildingEntry>();
    }

    [DefaultExecutionOrder(920)]
    public sealed class SeaBayExpansionAndBuildSpaceV28 : MonoBehaviour
    {
        public static SeaBayExpansionAndBuildSpaceV28 Active { get; private set; }

        [Header("v28 海域扩建与上帝视角建造空间配置")]
        public bool expansionEnabled = true;
        public bool godBuildMode = false;
        public float gridCellSize = 2.0f;

        [Header("扩建后的广阔航行边界 (原版为 X:[-6,34], Z:[-23,32])")]
        public Vector2 expandedBoundsX = new Vector2(-10f, 86f);
        public Vector2 expandedBoundsZ = new Vector2(-50f, 66f);

        [Header("上帝视角相机控制参数 (按 G 键开启/关闭)")]
        public float godOrthoSize = 22.0f;
        public float godPanSpeed = 24.0f;

        public bool Ready { get; private set; }

        private SeaLifeRuntime _game;
        private Transform _world;
        private Transform _waterTransform;
        private Vector3 _originalWaterScale;
        private Vector3 _originalWaterPos;

        private Transform _v28Root;
        private Transform _terraceRoot;
        private Transform _gridVisualRoot;
        private Transform _placedBuildingsRoot;
        private Transform _cursorPreviewRoot;
        private MeshRenderer[] _cursorRenderers;

        private Vector3 _godCameraFocus;
        private int _selectedBuildType = 0;
        private int _currentRotationStep = 0;

        private Vector2Int _hoveredCell;
        private bool _hoveredValid;
        private string _hoveredZoneName = "";
        private float _hoveredWaterDepth = 0f;

        private Material _sandTerraceMat;
        private Material _reefTerraceMat;
        private Material _pierWoodMat;
        private Material _ropeMat;
        private Material _bronzeMat;
        private Material _lanternGlowMat;
        private Material _gridLineMat;
        private Material _cursorValidMat;
        private Material _cursorInvalidMat;

        private BayExpansionSaveData _saveData = new BayExpansionSaveData();
        private string _saveFilePath;
        private readonly Dictionary<Vector2Int, GameObject> _spawnedBuildings = new Dictionary<Vector2Int, GameObject>();
        private readonly List<UnityEngine.Object> _ownedAssets = new List<UnityEngine.Object>();

        private static readonly string[] BuildNames =
        {
            "1. 延伸木栈桥 (2m×2m)",
            "2. 浅湾高杆暖光马灯",
            "3. 水下多彩珊瑚礁底座",
            "4. 海湾捕蟹浮标笼"
        };

        private static readonly string[] BuildCostsDesc =
        {
            "消耗: 浮木×2",
            "消耗: 浮木×1 + 黄铜×1",
            "消耗: 潮汐石×2 + 珊瑚苗×1",
            "消耗: 浮木×1 + 潮汐石×1"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (FindObjectOfType<SeaBayExpansionAndBuildSpaceV28>() != null) return;

            var rt = FindObjectOfType<SeaLifeRuntime>();
            if (rt != null)
            {
                rt.gameObject.AddComponent<SeaBayExpansionAndBuildSpaceV28>();
                return;
            }

            var v24 = FindObjectOfType<SeaSurfaceV24>();
            if (v24 != null)
            {
                v24.gameObject.AddComponent<SeaBayExpansionAndBuildSpaceV28>();
            }
        }

        private void Awake()
        {
            Active = this;
            _saveFilePath = Path.Combine(Application.persistentDataPath, "sea-bay-expansion-v28.json");
        }

        private IEnumerator Start()
        {
            _game = GetComponent<SeaLifeRuntime>() ?? FindObjectOfType<SeaLifeRuntime>();
            if (_game != null)
            {
                while (!_game.Ready) yield return null;
                _world = _game.world;
            }
            else
            {
                yield return null;
                _world = GameObject.Find("EN_MistCapeShowcase")?.transform;
            }

            if (_world == null) yield break;

            InitializeMaterials();
            ExpandSeaSurfaceAndBuildableTerraces();
            SetupBuildableGridAndCursor();
            LoadOrCreateStarterBayLayout();

            _godCameraFocus = new Vector3(12.0f, SeaLifeRuntime.Sea, 4.0f);
            Ready = true;
        }

        private void Update()
        {
            if (!Ready) return;

            // 按 G 键切换【2.5D 海湾上帝规划建造视角】与【常规游戏视角】
            if (Input.GetKeyDown(KeyCode.G) && (_game == null || !_game.InRoom))
            {
                ToggleGodBuildMode(!godBuildMode);
            }

            if (!godBuildMode) return;

            // 上帝建造模式下的快捷键与鼠标交互
            if (Input.GetKeyDown(KeyCode.Alpha1)) { _selectedBuildType = 0; RebuildCursorMesh(); }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { _selectedBuildType = 1; RebuildCursorMesh(); }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { _selectedBuildType = 2; RebuildCursorMesh(); }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { _selectedBuildType = 3; RebuildCursorMesh(); }
            if (Input.GetKeyDown(KeyCode.R))
            {
                _currentRotationStep = (_currentRotationStep + 1) % 4;
            }

            UpdateGodCameraNavigation(Time.unscaledDeltaTime);
            UpdateMouseGridHover();

            // 左键放置建筑，右键拆除并返还材料 (避开左上角 UI 区域)
            bool mouseOverUI = Input.mousePosition.x < 420f && (Screen.height - Input.mousePosition.y) < 320f;
            if (!mouseOverUI)
            {
                if (Input.GetMouseButtonDown(0) && _hoveredValid)
                {
                    TryPlaceBuildingAt(_hoveredCell, _selectedBuildType, _currentRotationStep, true);
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    TryRemoveBuildingAt(_hoveredCell, true);
                }
            }
        }

        private void LateUpdate()
        {
            if (!Ready) return;

            // 在上帝视角模式下接管相机，提供开阔的 2.5D 全海湾俯瞰视野
            if (godBuildMode && _game != null && _game.view != null)
            {
                Camera cam = _game.view;
                Quaternion godRot = Quaternion.Euler(46f, -38f, 0f);
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, godOrthoSize, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, godRot, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
                cam.transform.position = _godCameraFocus + cam.transform.rotation * new Vector3(0f, 0f, -45f);
            }
        }

        public void ToggleGodBuildMode(bool enable)
        {
            godBuildMode = enable;
            if (_gridVisualRoot != null) _gridVisualRoot.gameObject.SetActive(enable);
            if (_cursorPreviewRoot != null) _cursorPreviewRoot.gameObject.SetActive(enable);

            if (enable && _game != null)
            {
                // 将上帝镜头初始焦点对准当前船只或栈桥扩建中心
                Vector3 startFocus = _game.boat != null ? _game.boat.position : new Vector3(10f, SeaLifeRuntime.Sea, 4f);
                _godCameraFocus = new Vector3(startFocus.x, SeaLifeRuntime.Sea, startFocus.z);
                RebuildCursorMesh();
            }
        }

        // ====================================================================
        // 1. 扩建海面尺寸与塑造三大“可建造浅湾海床平台 (Buildable Bay Terraces)”
        // ====================================================================
        private void ExpandSeaSurfaceAndBuildableTerraces()
        {
            _v28Root = new GameObject("V28_BayExpansionAndBuildSpace").transform;
            _v28Root.SetParent(_world, false);

            // 1. 扩大 ShowcaseWater 海面覆盖范围，让外海与南北湾口视野不再露底
            _waterTransform = _world.Find("ShowcaseWater");
            if (_waterTransform != null)
            {
                _originalWaterPos = _waterTransform.localPosition;
                _originalWaterScale = _waterTransform.localScale;
                _waterTransform.localScale = new Vector3(
                    Mathf.Max(_originalWaterScale.x * 2.4f, 2.4f),
                    _originalWaterScale.y,
                    Mathf.Max(_originalWaterScale.z * 2.4f, 2.4f)
                );
            }

            // 2. 构建三大水下可建造浅滩与环礁台地 (写入深度缓冲，使 v26.2 水面自然渲染出翡翠浅湾色彩与清晰焦散网！)
            _terraceRoot = new GameObject("V28_BuildableSeabedTerraces").transform;
            _terraceRoot.SetParent(_v28Root, false);

            float sea = SeaLifeRuntime.Sea;

            // 【A区：栈桥延伸浅滩沙坪】(紧邻现有栈桥向东、向南、向北拓展，水深约 0.9m~1.35m，最适合延伸木栈道与船坞)
            CreateSeabedTerrace(
                "A区_栈桥延伸暖沙浅坪_主区",
                _terraceRoot,
                new Vector3(11.5f, sea - 1.12f, 4.5f),
                new Vector3(22.0f, 1.2f, 24.0f),
                1101,
                _sandTerraceMat,
                0.22f
            );

            // 【B区：内湾南侧与北侧翡翠珊瑚花园台地】(水深约 1.65m~2.35m，开阔平缓，适合大规模种植珊瑚礁与布置蟹笼)
            CreateSeabedTerrace(
                "B区_南湾珊瑚花园礁坪",
                _terraceRoot,
                new Vector3(14.0f, sea - 1.85f, -16.0f),
                new Vector3(28.0f, 1.5f, 24.0f),
                2202,
                _sandTerraceMat,
                0.35f
            );
            CreateSeabedTerrace(
                "B区_北湾翡翠生态浅坪",
                _terraceRoot,
                new Vector3(16.0f, sea - 1.75f, 24.0f),
                new Vector3(26.0f, 1.5f, 22.0f),
                3303,
                _sandTerraceMat,
                0.32f
            );

            // 【C区：外海新月环礁建造平台】(位于深海海沟外侧 X: 30~52，在深蓝海面上形成一片震撼的翡翠色外海环礁浅滩！)
            CreateSeabedTerrace(
                "C区_外海新月环礁浅滩_中段",
                _terraceRoot,
                new Vector3(36.0f, sea - 1.35f, 2.0f),
                new Vector3(20.0f, 1.8f, 34.0f),
                4404,
                _sandTerraceMat,
                0.38f
            );
            CreateSeabedTerrace(
                "C区_外海新月环礁浅滩_南翼",
                _terraceRoot,
                new Vector3(32.0f, sea - 1.60f, -24.0f),
                new Vector3(18.0f, 1.8f, 22.0f),
                5505,
                _sandTerraceMat,
                0.40f
            );

            // 在外海新月环礁上点缀几处露出水面的天然海蚀礁石与外海新鱼点标记
            BuildOuterAtollLandmarkRocks(_terraceRoot, sea);
        }

        /// <summary>
        /// 生成边缘自然缓坡过渡、表面带海沙波纹起伏的水下可建造海床台地网格 (带 MeshCollider 供建造射线与水深检测)
        /// </summary>
        private GameObject CreateSeabedTerrace(
            string name,
            Transform parent,
            Vector3 center,
            Vector3 size,
            int seed,
            Material mat,
            float undulation)
        {
            int nx = 24;
            int nz = 24;
            var verts = new List<Vector3>(nx * nz);
            var uvs = new List<Vector2>(nx * nz);
            var tris = new List<int>((nx - 1) * (nz - 1) * 6);

            for (int iz = 0; iz < nz; iz++)
            {
                float tz = (iz / (float)(nz - 1)) * 2f - 1f; // -1 ~ +1
                for (int ix = 0; ix < nx; ix++)
                {
                    float tx = (ix / (float)(nx - 1)) * 2f - 1f; // -1 ~ +1
                    float x = tx * (size.x * 0.5f);
                    float z = tz * (size.z * 0.5f);

                    // 中心区域平坦适合建造，四周边缘平滑下沉融入深海床，绝无生硬方块切边！
                    float radial = Mathf.Max(Mathf.Abs(tx), Mathf.Abs(tz));
                    float dist = Mathf.Sqrt(tx * tx * 0.65f + tz * tz * 0.65f);
                    float plateauMask = 1.0f - Mathf.SmoothStep(0.55f, 1.0f, Mathf.Max(radial, dist));

                    float wave = Mathf.Sin(tx * 4.5f + seed) * Mathf.Cos(tz * 4.2f - seed * 0.7f) * undulation;
                    float y = (plateauMask - 1.0f) * 3.8f + wave * plateauMask;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2((tx + 1f) * 2f, (tz + 1f) * 2f));
                }
            }

            for (int iz = 0; iz < nz - 1; iz++)
            {
                for (int ix = 0; ix < nx - 1; ix++)
                {
                    int a = iz * nx + ix;
                    int b = a + 1;
                    int c = a + nx;
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            var mesh = new Mesh { name = "V28_TerraceMesh_" + seed };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _ownedAssets.Add(mesh);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            return go;
        }

        private void BuildOuterAtollLandmarkRocks(Transform parent, float sea)
        {
            Vector3[] rockPositions =
            {
                new Vector3(38.5f, sea + 0.35f,  8.5f),
                new Vector3(39.4f, sea + 0.12f,  7.6f),
                new Vector3(34.2f, sea + 0.28f, -12.4f)
            };
            Vector3[] rockScales =
            {
                new Vector3(2.8f, 1.6f, 2.4f),
                new Vector3(1.8f, 1.1f, 1.6f),
                new Vector3(2.4f, 1.4f, 2.1f)
            };

            for (int i = 0; i < rockPositions.Length; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "V28_外海环礁岛石_" + i;
                rock.transform.SetParent(parent, false);
                rock.transform.position = rockPositions[i];
                rock.transform.localScale = rockScales[i];
                rock.transform.rotation = Quaternion.Euler(8f * i, 45f * i, -6f * i);
                rock.GetComponent<MeshRenderer>().sharedMaterial = _reefTerraceMat;
            }
        }

        // ====================================================================
        // 2. 2.5D 上帝视角可视化建造网格与实时鼠标选格预览
        // ====================================================================
        private void SetupBuildableGridAndCursor()
        {
            float sea = SeaLifeRuntime.Sea;

            // 1. 生成覆盖扩建海湾的清爽青白色建造参考网格线 (默认隐藏，按 G 进入上帝建造模式时浮现)
            _gridVisualRoot = new GameObject("V28_BuildableGridOverlay").transform;
            _gridVisualRoot.SetParent(_v28Root, false);
            _gridVisualRoot.position = new Vector3(0f, sea + 0.03f, 0f);

            var gridMesh = BuildGridLinesMesh(-4, 24, -14, 18, gridCellSize);
            _ownedAssets.Add(gridMesh);
            var gridGo = new GameObject("GridLinesMesh");
            gridGo.transform.SetParent(_gridVisualRoot, false);
            gridGo.AddComponent<MeshFilter>().sharedMesh = gridMesh;
            gridGo.AddComponent<MeshRenderer>().sharedMaterial = _gridLineMat;
            _gridVisualRoot.gameObject.SetActive(false);

            // 2. 已放置建筑容器
            _placedBuildingsRoot = new GameObject("V28_PlacedBayStructures").transform;
            _placedBuildingsRoot.SetParent(_v28Root, false);

            // 3. 鼠标悬停半透明预览游标
            _cursorPreviewRoot = new GameObject("V28_BuildCursorPreview").transform;
            _cursorPreviewRoot.SetParent(_v28Root, false);
            RebuildCursorMesh();
            _cursorPreviewRoot.gameObject.SetActive(false);
        }

        private Mesh BuildGridLinesMesh(int minX, int maxX, int minZ, int maxZ, float cellSize)
        {
            var verts = new List<Vector3>();
            var indices = new List<int>();
            float halfW = 0.018f;

            for (int gx = minX; gx <= maxX; gx++)
            {
                float x = gx * cellSize;
                int bi = verts.Count;
                verts.Add(new Vector3(x - halfW, 0f, minZ * cellSize));
                verts.Add(new Vector3(x + halfW, 0f, minZ * cellSize));
                verts.Add(new Vector3(x + halfW, 0f, maxZ * cellSize));
                verts.Add(new Vector3(x - halfW, 0f, maxZ * cellSize));
                indices.Add(bi); indices.Add(bi + 2); indices.Add(bi + 1);
                indices.Add(bi); indices.Add(bi + 3); indices.Add(bi + 2);
            }

            for (int gz = minZ; gz <= maxZ; gz++)
            {
                float z = gz * cellSize;
                int bi = verts.Count;
                verts.Add(new Vector3(minX * cellSize, 0f, z - halfW));
                verts.Add(new Vector3(maxX * cellSize, 0f, z - halfW));
                verts.Add(new Vector3(maxX * cellSize, 0f, z + halfW));
                verts.Add(new Vector3(minX * cellSize, 0f, z + halfW));
                indices.Add(bi); indices.Add(bi + 1); indices.Add(bi + 2);
                indices.Add(bi); indices.Add(bi + 2); indices.Add(bi + 3);
            }

            var m = new Mesh { name = "V28_BayGridOverlayMesh" };
            m.SetVertices(verts);
            m.SetTriangles(indices, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private void RebuildCursorMesh()
        {
            if (_cursorPreviewRoot == null) return;
            for (int i = _cursorPreviewRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_cursorPreviewRoot.GetChild(i).gameObject);
            }

            GameObject preview = ConstructBayStructureObject((BayBuildableType)_selectedBuildType, isPreview: true);
            preview.transform.SetParent(_cursorPreviewRoot, false);
            preview.transform.localPosition = Vector3.zero;
            _cursorRenderers = preview.GetComponentsInChildren<MeshRenderer>(true);
        }

        private void UpdateGodCameraNavigation(float dt)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // 根据等轴测相机朝向 (-38度) 平移镜头焦点
            Quaternion yawRot = Quaternion.Euler(0f, -38f, 0f);
            Vector3 moveDir = yawRot * new Vector3(h, 0f, v);
            if (moveDir.sqrMagnitude > 0.01f)
            {
                _godCameraFocus += moveDir.normalized * (godPanSpeed * dt * (godOrthoSize / 20f));
                _godCameraFocus.x = Mathf.Clamp(_godCameraFocus.x, -12f, 56f);
                _godCameraFocus.z = Mathf.Clamp(_godCameraFocus.z, -36f, 42f);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                godOrthoSize = Mathf.Clamp(godOrthoSize - scroll * 2.2f, 10f, 38f);
            }
        }

        private void UpdateMouseGridHover()
        {
            if (_game == null || _game.view == null || _cursorPreviewRoot == null) return;

            float sea = SeaLifeRuntime.Sea;
            Plane seaPlane = new Plane(Vector3.up, new Vector3(0f, sea, 0f));
            Ray ray = _game.view.ScreenPointToRay(Input.mousePosition);

            if (seaPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                int gx = Mathf.RoundToInt(hitPoint.x / gridCellSize);
                int gz = Mathf.RoundToInt(hitPoint.z / gridCellSize);
                _hoveredCell = new Vector2Int(gx, gz);

                Vector3 snappedPos = new Vector3(gx * gridCellSize, sea, gz * gridCellSize);
                _cursorPreviewRoot.position = snappedPos;
                _cursorPreviewRoot.rotation = Quaternion.Euler(0f, _currentRotationStep * 90f, 0f);

                // 探测该格子的真实水深与所属海湾建造分区
                _hoveredWaterDepth = SampleWaterDepthAt(snappedPos, sea);
                _hoveredZoneName = ClassifyBayZone(snappedPos, _hoveredWaterDepth);

                bool alreadyOccupied = _spawnedBuildings.ContainsKey(_hoveredCell);
                bool depthSuitable = _hoveredWaterDepth >= 0.15f && _hoveredWaterDepth <= 5.5f;
                bool canAfford = HasEnoughResources((BayBuildableType)_selectedBuildType);

                _hoveredValid = !alreadyOccupied && depthSuitable && canAfford;

                if (_cursorRenderers != null)
                {
                    Material targetMat = _hoveredValid ? _cursorValidMat : _cursorInvalidMat;
                    for (int i = 0; i < _cursorRenderers.Length; i++)
                    {
                        if (_cursorRenderers[i] != null) _cursorRenderers[i].sharedMaterial = targetMat;
                    }
                }
            }
        }

        private float SampleWaterDepthAt(Vector3 seaPos, float seaLevel)
        {
            if (Physics.Raycast(new Vector3(seaPos.x, seaLevel + 20f, seaPos.z), Vector3.down, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                return seaLevel - hit.point.y;
            }
            return 8.0f;
        }

        private string ClassifyBayZone(Vector3 pos, float depth)
        {
            if (depth < 0.15f) return "陆地/岸礁 (不可建水上设施)";
            if (pos.x >= 24f) return "C区 · 外海新月环礁平台";
            if (Mathf.Abs(pos.z - 4.5f) <= 12f && pos.x <= 23f) return "A区 · 栈桥延伸暖沙浅坪";
            return "B区 · 翡翠珊瑚花园台地";
        }

        // ====================================================================
        // 3. 建造、拆除与海湾扩建示范初始布局
        // ====================================================================
        private void LoadOrCreateStarterBayLayout()
        {
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<BayExpansionSaveData>(File.ReadAllText(_saveFilePath));
                    if (loaded != null && loaded.placedBuildings != null && loaded.placedBuildings.Count > 0)
                    {
                        _saveData = loaded;
                        foreach (var b in _saveData.placedBuildings)
                        {
                            SpawnBuildingVisual(new Vector2Int(b.gridX, b.gridZ), (BayBuildableType)b.buildType, b.rotationStep);
                        }
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SeaBayExpansionV28] Save load warning: " + e.Message);
                }
            }

            // 初次进入时，在扩建后的浅湾中预置一段示范性的【外延观景栈桥 + 桥头暖光马灯 + 浅湾珊瑚花园礁 + 捕蟹浮标】，
            // 让玩家一进游戏就能直观看到海域扩建后的可建造空间与效果！
            var starterItems = new[]
            {
                new BayPlacedBuildingEntry { gridX = 2, gridZ = 1, buildType = (int)BayBuildableType.WoodenPierDeck, rotationStep = 0 },
                new BayPlacedBuildingEntry { gridX = 3, gridZ = 1, buildType = (int)BayBuildableType.WoodenPierDeck, rotationStep = 0 },
                new BayPlacedBuildingEntry { gridX = 4, gridZ = 1, buildType = (int)BayBuildableType.WoodenPierDeck, rotationStep = 0 },
                new BayPlacedBuildingEntry { gridX = 4, gridZ = 2, buildType = (int)BayBuildableType.PierLanternPost, rotationStep = 0 },
                new BayPlacedBuildingEntry { gridX = 3, gridZ = -2, buildType = (int)BayBuildableType.CoralReefGarden, rotationStep = 1 },
                new BayPlacedBuildingEntry { gridX = 5, gridZ = -1, buildType = (int)BayBuildableType.CoralReefGarden, rotationStep = 2 },
                new BayPlacedBuildingEntry { gridX = 6, gridZ = 3, buildType = (int)BayBuildableType.CrabPotBuoy, rotationStep = 0 },
                // 外海新月环礁示范前哨栈桥与航标灯
                new BayPlacedBuildingEntry { gridX = 16, gridZ = 1, buildType = (int)BayBuildableType.WoodenPierDeck, rotationStep = 0 },
                new BayPlacedBuildingEntry { gridX = 17, gridZ = 1, buildType = (int)BayBuildableType.PierLanternPost, rotationStep = 2 }
            };

            _saveData.placedBuildings.Clear();
            foreach (var item in starterItems)
            {
                _saveData.placedBuildings.Add(item);
                SpawnBuildingVisual(new Vector2Int(item.gridX, item.gridZ), (BayBuildableType)item.buildType, item.rotationStep);
            }
            SaveExpansionData();
        }

        private bool HasEnoughResources(BayBuildableType type)
        {
            switch (type)
            {
                case BayBuildableType.WoodenPierDeck: return _saveData.wood >= 2;
                case BayBuildableType.PierLanternPost: return _saveData.wood >= 1 && _saveData.brass >= 1;
                case BayBuildableType.CoralReefGarden: return _saveData.stone >= 2 && _saveData.coralSeed >= 1;
                case BayBuildableType.CrabPotBuoy: return _saveData.wood >= 1 && _saveData.stone >= 1;
                default: return true;
            }
        }

        private void ConsumeOrRefundResources(BayBuildableType type, int sign)
        {
            switch (type)
            {
                case BayBuildableType.WoodenPierDeck:
                    _saveData.wood += 2 * sign;
                    break;
                case BayBuildableType.PierLanternPost:
                    _saveData.wood += 1 * sign;
                    _saveData.brass += 1 * sign;
                    break;
                case BayBuildableType.CoralReefGarden:
                    _saveData.stone += 2 * sign;
                    _saveData.coralSeed += 1 * sign;
                    break;
                case BayBuildableType.CrabPotBuoy:
                    _saveData.wood += 1 * sign;
                    _saveData.stone += 1 * sign;
                    break;
            }
        }

        public bool TryPlaceBuildingAt(Vector2Int cell, int buildTypeIndex, int rotStep, bool deductResources)
        {
            if (_spawnedBuildings.ContainsKey(cell)) return false;
            var bType = (BayBuildableType)Mathf.Clamp(buildTypeIndex, 0, 3);
            if (deductResources && !HasEnoughResources(bType)) return false;

            if (deductResources) ConsumeOrRefundResources(bType, -1);

            SpawnBuildingVisual(cell, bType, rotStep);
            _saveData.placedBuildings.Add(new BayPlacedBuildingEntry
            {
                gridX = cell.x,
                gridZ = cell.y,
                buildType = (int)bType,
                rotationStep = rotStep
            });
            SaveExpansionData();
            return true;
        }

        public bool TryRemoveBuildingAt(Vector2Int cell, bool refundResources)
        {
            if (!_spawnedBuildings.TryGetValue(cell, out GameObject go)) return false;
            if (go != null) Destroy(go);
            _spawnedBuildings.Remove(cell);

            int idx = _saveData.placedBuildings.FindIndex(b => b.gridX == cell.x && b.gridZ == cell.y);
            if (idx >= 0)
            {
                if (refundResources)
                {
                    ConsumeOrRefundResources((BayBuildableType)_saveData.placedBuildings[idx].buildType, +1);
                }
                _saveData.placedBuildings.RemoveAt(idx);
                SaveExpansionData();
            }
            return true;
        }

        private void SpawnBuildingVisual(Vector2Int cell, BayBuildableType type, int rotStep)
        {
            float sea = SeaLifeRuntime.Sea;
            Vector3 worldPos = new Vector3(cell.x * gridCellSize, sea, cell.y * gridCellSize);
            GameObject go = ConstructBayStructureObject(type, isPreview: false);
            go.name = $"V28_Built_{type}_{cell.x}_{cell.y}";
            go.transform.SetParent(_placedBuildingsRoot, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, rotStep * 90f, 0f);
            _spawnedBuildings[cell] = go;
        }

        private GameObject ConstructBayStructureObject(BayBuildableType type, bool isPreview)
        {
            var root = new GameObject("Structure_" + type);

            if (type == BayBuildableType.WoodenPierDeck)
            {
                // 2m x 2m 复古木栈桥模块 (含 4 块木桥板 + 两侧护边木梁 + 4 根深入海底的木桩立柱)
                for (int i = 0; i < 4; i++)
                {
                    float offsetZ = -0.72f + i * 0.48f;
                    var plank = CreateBoxPart("栈桥木板_" + i, root.transform, new Vector3(0f, 0.72f, offsetZ), new Vector3(1.94f, 0.10f, 0.43f), _pierWoodMat, !isPreview);
                }
                CreateBoxPart("前护边横木", root.transform, new Vector3(0f, 0.78f, 0.92f), new Vector3(1.98f, 0.12f, 0.12f), _pierWoodMat, false);
                CreateBoxPart("后护边横木", root.transform, new Vector3(0f, 0.78f, -0.92f), new Vector3(1.98f, 0.12f, 0.12f), _pierWoodMat, false);

                float[] px = { -0.82f, 0.82f };
                float[] pz = { -0.82f, 0.82f };
                foreach (float x in px)
                {
                    foreach (float z in pz)
                    {
                        CreateCylinderPart("栈桥木桩", root.transform, new Vector3(x, -0.25f, z), new Vector3(0.22f, 1.15f, 0.22f), _pierWoodMat, false);
                    }
                }
            }
            else if (type == BayBuildableType.PierLanternPost)
            {
                // 浅湾高杆悬挂暖光马灯 + 栈道基座
                CreateCylinderPart("灯座木桩", root.transform, new Vector3(0f, -0.20f, 0f), new Vector3(0.45f, 1.05f, 0.45f), _pierWoodMat, !isPreview);
                CreateBoxPart("高挑木灯柱", root.transform, new Vector3(0f, 1.85f, 0f), new Vector3(0.14f, 2.20f, 0.14f), _pierWoodMat, false);
                CreateBoxPart("灯柱横挑梁", root.transform, new Vector3(0.32f, 2.85f, 0f), new Vector3(0.76f, 0.10f, 0.10f), _pierWoodMat, false);
                CreateCylinderPart("马灯暖光胆", root.transform, new Vector3(0.58f, 2.55f, 0f), new Vector3(0.24f, 0.18f, 0.24f), _lanternGlowMat, false);

                if (!isPreview)
                {
                    var lGo = new GameObject("BayLanternPointLight");
                    lGo.transform.SetParent(root.transform, false);
                    lGo.transform.localPosition = new Vector3(0.58f, 2.50f, 0f);
                    var light = lGo.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1.0f, 0.74f, 0.35f, 1f);
                    light.range = 8.0f;
                    light.intensity = 2.0f;
                    light.shadows = LightShadows.None;
                }
            }
            else if (type == BayBuildableType.CoralReefGarden)
            {
                // 水下生态珊瑚礁底座 + 多彩珊瑚簇 + 摇曳海带 (沉在水面下 -0.85m 处，透过翡翠色海水清晰可见！)
                Shader vibrantShader = Shader.Find("Deepsea/UnderwaterReefVibrantV27") ?? Shader.Find("Deepsea/SeaLifeSurface") ?? Shader.Find("Universal Render Pipeline/Lit");
                Material coralMat = new Material(vibrantShader);
                _ownedAssets.Add(coralMat);
                if (coralMat.HasProperty("_CoralVibrancy")) coralMat.SetFloat("_CoralVibrancy", 1.15f);

                CreateCylinderPart("人工鱼礁石座", root.transform, new Vector3(0f, -0.95f, 0f), new Vector3(1.65f, 0.25f, 1.65f), _reefTerraceMat, false);
                CreateSpherePart("粉紫珊瑚簇", root.transform, new Vector3(-0.35f, -0.62f, 0.22f), new Vector3(0.68f, 0.52f, 0.65f), coralMat);
                CreateSpherePart("珊瑚橙海扇簇", root.transform, new Vector3(0.38f, -0.58f, -0.18f), new Vector3(0.62f, 0.58f, 0.55f), coralMat);
                CreateSpherePart("薄荷绿脑珊瑚", root.transform, new Vector3(0.05f, -0.68f, 0.42f), new Vector3(0.52f, 0.42f, 0.52f), coralMat);
            }
            else // CrabPotBuoy
            {
                // 水面红白浮标 + 水下绳索与沉底捕蟹木笼
                CreateCylinderPart("水面红白浮标", root.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.42f, 0.22f, 0.42f), _lanternGlowMat, !isPreview);
                CreateCylinderPart("水下系绳", root.transform, new Vector3(0f, -0.45f, 0f), new Vector3(0.04f, 0.48f, 0.04f), _ropeMat, false);
                CreateBoxPart("水下捕蟹木笼", root.transform, new Vector3(0f, -0.92f, 0f), new Vector3(0.95f, 0.38f, 0.72f), _pierWoodMat, false);
            }

            return root;
        }

        private void SaveExpansionData()
        {
            try
            {
                File.WriteAllText(_saveFilePath, JsonUtility.ToJson(_saveData, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SeaBayExpansionV28] Save write warning: " + e.Message);
            }
        }

        // ====================================================================
        // 4. 实时 UI 面板 (显示扩建分区状态、建材库存与上帝视角建造栏)
        // ====================================================================
        private void OnGUI()
        {
            if (!Ready || (_game != null && _game.InRoom)) return;

            // 顶部常驻轻提示：告知玩家按 G 键开启 2.5D 海湾上帝建造视角
            if (!godBuildMode)
            {
                GUI.backgroundColor = new Color(0.05f, 0.14f, 0.18f, 0.85f);
                GUI.Box(new Rect(Screen.width - 330f, 18f, 312f, 54f), "");
                GUI.Label(new Rect(Screen.width - 318f, 24f, 290f, 22f), "【v28 广阔海湾已解锁】外海环礁与浅湾台地");
                GUI.Label(new Rect(Screen.width - 318f, 44f, 290f, 22f), "按 [G] 键：切换 2.5D 上帝视角自由建造模式");
                return;
            }

            // 上帝视角建造工作台面板
            GUI.backgroundColor = new Color(0.05f, 0.13f, 0.17f, 0.94f);
            Rect panelRect = new Rect(20f, 76f, 390f, 315f);
            GUI.Box(panelRect, "【归潮湾 v28】2.5D 上帝视角海湾规划与建造台 (按 G 退出)");

            GUILayout.BeginArea(new Rect(34f, 106f, 362f, 275f));

            GUILayout.Label($"当前海湾物资: 浮木 {_saveData.wood} | 潮汐石 {_saveData.stone} | 珊瑚苗 {_saveData.coralSeed} | 黄铜 {_saveData.brass}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 补充测试建材包 (各+20)"))
            {
                _saveData.wood += 20;
                _saveData.stone += 20;
                _saveData.coralSeed += 20;
                _saveData.brass += 20;
                SaveExpansionData();
            }
            if (GUILayout.Button($"旋转方向 [R]: {_currentRotationStep * 90}°"))
            {
                _currentRotationStep = (_currentRotationStep + 1) % 4;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("选择要建造的海湾设施 (按键 1~4 或点击下方按钮):");
            for (int i = 0; i < BuildNames.Length; i++)
            {
                GUI.backgroundColor = (_selectedBuildType == i)
                    ? new Color(0.18f, 0.78f, 0.68f, 1f)
                    : new Color(0.15f, 0.25f, 0.30f, 0.9f);
                if (GUILayout.Button($"{BuildNames[i]}  ({BuildCostsDesc[i]})"))
                {
                    _selectedBuildType = i;
                    RebuildCursorMesh();
                }
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(6f);
            string statusText = _hoveredValid ? "【可建造 - 左键放置 / 右键拆除】" : "【此处不可放置 或 右键拆除已有设施】";
            GUILayout.Label($"指向网格: ({_hoveredCell.x}, {_hoveredCell.y}) | 水深: {_hoveredWaterDepth:F2}m");
            GUILayout.Label($"所属海区: {_hoveredZoneName}");
            GUILayout.Label($"状态提示: {statusText} (WASD漫游 / 滚轮缩放)");

            GUILayout.EndArea();
        }

        private void InitializeMaterials()
        {
            Shader surfaceShader = Shader.Find("Deepsea/SeaLifeSurface") ?? Shader.Find("Universal Render Pipeline/Lit");

            _sandTerraceMat = new Material(surfaceShader) { name = "V28_WarmBaySandTerrace" };
            _ownedAssets.Add(_sandTerraceMat);
            if (_sandTerraceMat.HasProperty("_BaseColor"))
                _sandTerraceMat.SetColor("_BaseColor", new Color(0.76f, 0.72f, 0.58f, 1f));
            if (_sandTerraceMat.HasProperty("_Smoothness"))
                _sandTerraceMat.SetFloat("_Smoothness", 0.18f);

            _reefTerraceMat = new Material(surfaceShader) { name = "V28_AtollReefStone" };
            _ownedAssets.Add(_reefTerraceMat);
            if (_reefTerraceMat.HasProperty("_BaseColor"))
                _reefTerraceMat.SetColor("_BaseColor", new Color(0.38f, 0.42f, 0.40f, 1f));
            if (_reefTerraceMat.HasProperty("_Smoothness"))
                _reefTerraceMat.SetFloat("_Smoothness", 0.22f);

            _pierWoodMat = new Material(surfaceShader) { name = "V28_PierDeckWood" };
            _ownedAssets.Add(_pierWoodMat);
            if (_pierWoodMat.HasProperty("_BaseColor"))
                _pierWoodMat.SetColor("_BaseColor", new Color(0.56f, 0.44f, 0.31f, 1f));
            if (_pierWoodMat.HasProperty("_Smoothness"))
                _pierWoodMat.SetFloat("_Smoothness", 0.25f);

            _ropeMat = new Material(surfaceShader) { name = "V28_HempRope" };
            _ownedAssets.Add(_ropeMat);
            if (_ropeMat.HasProperty("_BaseColor"))
                _ropeMat.SetColor("_BaseColor", new Color(0.70f, 0.58f, 0.40f, 1f));

            _bronzeMat = new Material(surfaceShader) { name = "V28_Bronze" };
            _ownedAssets.Add(_bronzeMat);
            if (_bronzeMat.HasProperty("_BaseColor"))
                _bronzeMat.SetColor("_BaseColor", new Color(0.28f, 0.24f, 0.20f, 1f));

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? surfaceShader;
            _lanternGlowMat = new Material(unlitShader) { name = "V28_WarmGlow" };
            _ownedAssets.Add(_lanternGlowMat);
            Color warmCol = new Color(1.0f, 0.76f, 0.32f, 1f);
            if (_lanternGlowMat.HasProperty("_BaseColor")) _lanternGlowMat.SetColor("_BaseColor", warmCol);
            if (_lanternGlowMat.HasProperty("_Color")) _lanternGlowMat.SetColor("_Color", warmCol);

            Shader glassShader = Shader.Find("Deepsea/SeaLifeAquariumGlassV25") ?? Shader.Find("Deepsea/SeaLifeGlass") ?? unlitShader;
            _gridLineMat = new Material(glassShader) { name = "V28_GridLineMat" };
            _ownedAssets.Add(_gridLineMat);
            if (_gridLineMat.HasProperty("_BaseColor"))
                _gridLineMat.SetColor("_BaseColor", new Color(0.55f, 0.95f, 0.92f, 0.22f));

            _cursorValidMat = new Material(glassShader) { name = "V28_CursorValidMat" };
            _ownedAssets.Add(_cursorValidMat);
            if (_cursorValidMat.HasProperty("_BaseColor"))
                _cursorValidMat.SetColor("_BaseColor", new Color(0.20f, 0.95f, 0.72f, 0.48f));

            _cursorInvalidMat = new Material(glassShader) { name = "V28_CursorInvalidMat" };
            _ownedAssets.Add(_cursorInvalidMat);
            if (_cursorInvalidMat.HasProperty("_BaseColor"))
                _cursorInvalidMat.SetColor("_BaseColor", new Color(0.95f, 0.28f, 0.25f, 0.48f));
        }

        private static GameObject CreateBoxPart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool withCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!withCollider) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject CreateCylinderPart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool withCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (!withCollider) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject CreateSpherePart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private void OnDestroy()
        {
            if (_waterTransform != null && _originalWaterScale.sqrMagnitude > 0.01f)
            {
                _waterTransform.localPosition = _originalWaterPos;
                _waterTransform.localScale = _originalWaterScale;
            }
            if (_v28Root != null) Destroy(_v28Root.gameObject);
            foreach (var asset in _ownedAssets)
            {
                if (asset != null) Destroy(asset);
            }
            if (Active == this) Active = null;
        }
    }
}
```

---

## 第三步：构建 v28 场景与可执行程序并采集实机截图

1. 基于 `v27` 创建或更新 `CoastSeaExpansionV28`（或集成至最新主场景与构建流程），编译输出 `Builds/SeaExpansionV28/Guichao-SeaExpansionV28.exe`，并刷新桌面快捷方式 **“深海灯塔—最新版”**。
2. 在 1920x1080 可见窗口下运行自动化验收，分别采集：
   * 常规航行视角下看到的**扩建浅湾、外延示范栈桥与远方外海新月环礁**（`Docs/SeaExpansionV28/preview.jpg`）；
   * 按 **`G` 键** 开启 **2.5D 上帝视角建造网格与分区预览** 时的全景俯瞰画面（`Docs/SeaExpansionV28/02-god-build-grid.jpg`），并输出 `Docs/SeaExpansionV28/README.md` 与 `result.json`。

---

## 第四步：提交并推送到 GitHub (`https://github.com/xujiale4869/guichao-bay`)

```powershell
git add .
git commit -m "feat(bay-expansion-v28): 扩建4倍广阔海域与三大水下可建造浅湾环礁平台，接入G键2.5D上帝视角网格建造系统"
git push origin main
```
