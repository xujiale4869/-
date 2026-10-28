# 给 GPT（本地电脑控制插件）的 v25 鱼类与水族箱展缸升级交接指令

> **使用方法**：直接复制下方分割线以后的全部内容，发送给具备本地电脑控制/终端权限的 GPT。

你好！我和 **Gemini** 已经完成了对仓库 `https://github.com/xujiale4869/-` 中 `Assets/Deepsea/Scripts/Runtime/SeaLifeV21/` 源码的深度审查。
针对《制作交接-海湾生活-v21-2026-10-28.md》中指出的核心痛点——**“v21 中的鱼体（12边形简陋棱柱）和玻璃展缸（几个基础 Box + 单颗圆球主石）属于早期 C# 几何拼凑原型，视觉完成度低”**，Gemini 已经设计并编写好了整套 **v25“鱼类与水族箱展缸”视觉与数据底座升级包**。

请立即调用你的**本地文件写入与终端命令行工具**，在我的本地主工程（`深海灯塔-Unity海陆-v0.3`）中自动落地以下 5 个步骤，全程无需破坏或删除 `v21` 原有代码与存档结构：

### 第一步：写入以下 4 个核心文件到工程指定路径

请将我附带的（或 Gemini 生成的）以下 4 个文件准确写入对应目录：

1. **Blender 全自动建模与贴图生成脚本**

   * 目标路径：`Tools/SeaLifeV25/generate_fish_and_aquarium_v25.py`

2. **鱼类与水族箱 ScriptableObject 数据底座**

   * 目标路径：`Assets/_Project/Scripts/Fishing/FishAndAquariumDataSO.cs`

3. **鱼类脊椎正弦波摆尾与虹彩鳞片 Shader**

   * 目标路径：`Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeFishSwimV25.shader`

4. **超白倒角玻璃、水体丁达尔光与沙底动态焦散 Shader**

   * 目标路径：`Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeAquariumGlassV25.shader`

5. **v25 鱼类与水族箱动态升级与资产注入控制器**

   * 目标路径：`Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeVisualUpgradeV25.cs`

### 第二步：在本地终端调用 Blender 自动生成高精度 3D 鱼类与水族箱 FBX + PBR 贴图

在工程根目录下执行以下命令（使用 `AGENTS.md` 中记录的本地 Blender 路径）：

```
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b -P Tools/SeaLifeV25/generate_fish_and_aquarium_v25.py -- --out-dir "Assets/Deepsea/Resources/SeaLifeV25"

```

执行完成后，请检查 `Assets/Deepsea/Resources/SeaLifeV25/` 下是否已成功生成以下 9 个文件：

* `Textures/Fish_0_SilverBream_Albedo.png`（银纹鱼 512x512 菱形鳞片+7道暗银纹贴图）

* `Textures/Fish_1_BluefinTuna_Albedo.png`（蓝鳍鱼 512x512 深海钴蓝+金色侧线贴图）

* `Textures/Fish_2_GoldButterfly_Albedo.png`（金背鱼 512x512 琥珀金+斜纹眼带贴图）

* `Models/Fish_0_SilverBream.fbx`（高细节流线型银纹鱼模型，含分叉新月尾鳍、背鳍、胸鳍、腹鳍、臀鳍与立体鱼眼）

* `Models/Fish_1_BluefinTuna.fbx`（高细节纺锤型蓝鳍鱼模型）

* `Models/Fish_2_GoldButterfly.fbx`（高细节圆盘高背型金背珊瑚蝶模型）

* `Models/AquariumTank_V25_Frame.fbx`（榫卯倒角原木底柜 + 黄铜包角立柱 + 顶部悬浮式暖光灯架）

* `Models/AquariumTank_V25_Interior.fbx`（起伏波纹暖砂床 + 5 块多孔层叠风化礁石群）

* `Models/AquariumTank_V25_Glass.fbx`（带倒角厚度的四面超白玻璃腔体）

### 第三步：运行 Unity 编译与实机自动验证

1. 运行 `Tools/Unity-Check.cmd Import`（或调用 Unity 编辑器刷新 `AssetDatabase`），确保新生成的 FBX 模型、PNG 贴图、2 个新 Shader 和 2 个新 C# 脚本 **0 编译错误（0 Console Error）**。

2. `SeaLifeVisualUpgradeV25.cs` 带有 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` 自动挂载机制，进入 `CoastSeaLife.unity` 场景时会自动：

   * 将 `SeaLifeRuntime` 内部的 `fishMeshes` 和 `fishMaterials` 替换为 v25 高精度鱼模型与脊椎摆尾材质；

   * 为所有鱼实例挂载 `SeaLifeFishSwimmerV25`（实现根据游速自适应 S 型脊椎摆尾与转弯侧倾）；

   * 将海洋小馆内的展缸 A/B/C 自动升级为带黄铜包角、顶部悬浮暖灯、5 块层叠风化礁石、水体丁达尔体积光和沙床动态焦散光斑的精修展缸。

3. 如果需要打包独立可执行文件预览，可构建至 `Builds/SeaLifeV25/Guichao-SeaLifeV25.exe`（同时完整保留 `v15`、`v20`、`v21` 原构建程序不覆盖）。

### 第四步：提交并推送到 GitHub

验证无编译报错后，请执行：

```
git add .
git commit -m "feat(sealife-v25): 重做3D鱼类与水族箱展缸高精度模型、鳞片摆尾Shader与SO数据底座"
git push origin main

```

完成后请向我输出一份简短的《v25 鱼类与展缸升级回执》（包含 Blender 导出结果、Unity 编译检查状态及最新 Commit Hash）！