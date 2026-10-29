# 给 GPT（本地电脑控制插件）的 v26“雾岬岛·东岸”水面光学层次与波光升级交接指令

> **使用方法**：直接复制下方分割线以后的全部内容，发送给具备本地电脑控制/终端权限的 GPT。

---

你好！我和 **Gemini** 已经针对我上传的《雾岬岛·东岸》理想水面目标原图，并结合仓库中现有的 `SeaLifeWaterV22.shader` 与 `SeaSurfaceV24.shader` 进行了深度光学拆解。
目前工程中旧版水面（v22/v24）与目标原图的核心差距在于：
1. **缺少水底网状流动焦散（Caustics Web）**：旧版 Shader 仅对海床做了 Beer-Lambert 颜色衰减，没有在浅滩白沙、岸石脚下和木栈桥水下桩体上投射高对比度的双层交错 Voronoi 焦散亮网。
2. **水深色彩过渡只有两级（`_Shallow` -> `_Deep`）**：缺少原图中从“极浅滩透底暖绿沙色” -> “内湾明亮翡翠薄荷色” -> “珊瑚礁盘边缘孔雀青蓝” -> “外海海沟深邃午夜靛蓝”的 **四级水深光学层次** 与波峰次表面散射（Crest SSS）。
3. **波光粼粼感（Sun Sparkle）被抑制**：`SeaSurfaceV24.shader` 注释掉了微面元闪烁，导致中远海面缺乏阳光照射下波光粼粼的动态星芒碎光。
4. **岸浪泡沫与船尾流过弱**：原图岸礁周围有两层律动的网孔状浪花环，小船后方有清晰的 V 型开尔文扩散白沫尾流（Kelvin Wake），而旧版只有极淡的 1 层边缘线。

现在 Gemini 已经编写好了整套 **v26 水面光学层次与波光升级包**，请调用你的**本地文件写入与终端命令行工具**，在我的本地主工程中自动执行以下 4 个步骤：

---

### 第一步：写入以下 3 个核心文件到工程指定路径

1. **水体光学贴图全自动烘焙脚本 (纯 Python，无第三方依赖)**
   * 目标路径：`Tools/SeaSurfaceV26/generate_water_optics_v26.py`
2. **v26 五级水深层次 + 双层 Voronoi 焦散网 + 波光粼粼微面元 + V 型尾流 Shader**
   * 目标路径：`Assets/Deepsea/Shaders/SeaSurfaceV24/SeaSurfaceSparkleV26.shader`
3. **v26 水体艺术与水下生态（浅滩滑翔蝠鲼 + 深海海沟银鱼流线群）动态升级控制器**
   * 目标路径：`Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaWaterArtUpgradeV26.cs`

---

### 第二步：在本地终端运行 Python 脚本生成 3 张无缝光学贴图

请在工程根目录下运行以下命令（可使用系统 Python、Codex Python 或 Blender 自带 Python）：

```powershell
python Tools/SeaSurfaceV26/generate_water_optics_v26.py "Assets/Deepsea/Resources/SeaSurfaceV26/Textures"
```

确认在 `Assets/Deepsea/Resources/SeaSurfaceV26/Textures/` 下成功生成以下 3 张贴图：
* `WaterCaustics_V26.png`（384x384 四方无缝平铺双通道 Voronoi 焦散亮网 + 微面元闪烁核）
* `ShoreFoamNoise_V26.png`（256x256 四方无缝平铺岸际网孔泡沫与湍流贴图）
* `WaterDepthRamp_V26.png`（256x16 五级水深光学吸收渐变色板）

---

### 第三步：Unity 编译检查与实机截图对照验收

1. 运行 Unity 资源导入与编译检查（`Tools/Unity-Check.cmd Import`），确保 `SeaSurfaceSparkleV26.shader` 与 `SeaWaterArtUpgradeV26.cs` **0 编译错误、0 Shader 报错**。
2. `SeaWaterArtUpgradeV26.cs` 带有 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` 自动注入机制，并完美兼容 `_OceanV24Time`、`_WaterV22Time`、`_CoastBoat` 等全部现有全局变量：
   * 进入游戏后会自动将海面升级为 `SeaSurfaceSparkleV26` 材质，并生成原图标志性的**浅滩滑翔蝠鲼**与**深海海沟 S 型银鱼群流线带**。
   * 支持在实机运行中按 **`F6` 键** 一键切换“v26 层次波光水面”与“原版水面”做同镜头实时对照！
3. 请在可见窗口（1920x1080）下运行海湾场景，采集同镜头下的实机画面保存至 `Docs/SeaSurfaceV26/preview.jpg`（以及 `result.json`），让我直观看到浅滩焦散网、四级水深色彩过渡、中远海波光粼粼和岸礁浪花的实际效果。

---

### 第四步：提交并推送到 GitHub

验收完成后，请执行：
```powershell
git add .
git commit -m "feat(water-v26): 实现雾岬岛东岸五级水深光学层次、双层Voronoi流动焦散网、波光粼粼微面元闪烁与海沟生态"
git push origin main
```

完成后请输出一份简短的**《v26 水面美术升级执行回执》**！
