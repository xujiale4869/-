# v26 水面光学升级执行回执

基于用户提供的 Python、Shader、C# 三份代码及交接文档完成接入。两份控制器附件逐字节一致，仅安装一份并改为与类名匹配的 SeaWaterArtUpgradeV26.cs。原始附件保存在 Tools/SeaSurfaceV26/OriginalInputs。

## 入口

- 场景：Assets/Deepsea/Scenes/CoastSeaSurfaceV26.unity，继承现有 v25 鱼类与展缸场景。
- 程序：Builds/SeaSurfaceV26/Guichao-SeaSurfaceV26.exe。
- 桌面：深海灯塔－最新版；总文件夹的版本启动入口内另有 v26 专用快捷方式。
- F6：切换 v26 与 v24 原水面材质；F9：帧率。F6 两侧保留同一批新增生态，不是整个旧版二进制还原。
- 正常游玩沿用现有 v21 产品名和存档位置。测试使用唯一临时存档，正常存档 SHA256 前后相同。

## 已接入与必要适配

384×384 线性焦散双通道贴图、256×256 线性泡沫贴图、256×16 sRGB 深度渐变色板；前两张 Repeat，色板 Clamp。关闭 Unity 非二次幂自动缩放，贴图保持原尺寸，使用无压缩采样与适当 mipmap。

控制器等待 v24 海面和运行时就绪，精确定位 ShowcaseWater；只在 v26 场景或显式 -water-v26 参数下自动注入。引用资源材质避免 Shader.Find 对应 Shader 在构建中被剥离。五段渐变色板实际接入，新 Shader 继续使用现有 OceanV24Waves.hlsl 波形，确保船只/浮漂沿用的查询与可见海面一致，不另写全局波浪时钟。

浅水双层焦散、分层水色、浪峰透光、基于太阳角度的碎光、岸浪和速度约束尾流已接入。原稿正弦点阵造成规则亮点，改为抗锯齿的非规则贴图微面元，并收窄太阳反射角、降低焦散与高光强度；未改原 v24 材质。原稿逐像素 Voronoi 回退分支已去掉，避免把暗纹理像素误当成缺失贴图而执行昂贵循环。

新增蝠鲼和72条银鱼沿闭合路径运动；恢复全局随机状态，按碰撞地形检查水下位置，释放运行时自有资源，并同步暂停。外形仍是原包程序化原型，未做高精模型或真实生物游动动画。它们尚未完成全海湾路径验收。

263项旧脚本、Shader、场景及项目设置基线中262项 SHA256 一致。唯一改动是 SeaSurfaceV24.cs 的 F7 条件增加 SeaWaterArtUpgradeV26.Active==null，防止 v26 场景误切旧模式导致物理与波形不一致；旧场景无 v26 组件时仍走原逻辑。精确差异与正常存档检查见 baseline-verification.json。

## 验证结果与范围

build-02 成功，新增 Shader 编译错误0；全包构建仍有24项警告，包括原 OceanSurface.cs 过时API与旧Shader警告，不称全工程零警告。

review-02 在可见1920×1080原生窗口完成21项检查，result.json passed=true；运行错误0、警告0。检查涵盖实际资源尺寸、主水面定位、材质切换路径、生态创建、画面非黑帧/非粉色、暂停冻结、原船推进和临时存档写入。材质切换调用与F6相同的接口，未注入操作系统键盘。

Intel Arc 130T GPU固定镜头480帧采样：对照138.90 FPS/P95 8.31ms，v26 129.49 FPS/P95 8.70ms。该采样不代表全地图、长期游玩或4K性能。截图01-baseline.jpg、preview.jpg、03-next-wave.jpg为实际引擎帧；没有以生成图代替实机。

未收到《雾岬岛·东岸》目标原图本体，本轮只能对现有场景做功能与画面检查，不能确认已匹配目标原图，或代替用户的美术认可。新增生态、透明感、碎光强度及岸浪仍需实看评价；没有完整人工钓鱼、展缸与全海湾回归的声明。

首轮draft01因导入缩放失败，图像还含规则碎光，不作最终交付依据，保留在本地被忽略的 Staging/draft01 供追溯。

## 重建

在工程根目录运行 Python：

```powershell
python Tools/SeaSurfaceV26/generate_water_optics_v26.py "Assets/Deepsea/Resources/SeaSurfaceV26/Textures"
```

Unity编辑器方法 Deepsea.Editor.SeaSurfaceBuildV26.ImportAndBuild 会导入贴图、从v25重建v26场景并构建；日常只构建使用 Deepsea.Editor.SeaSurfaceBuildV26.Build。原 Tools/Unity-Check.cmd 没有真正的 Import 分支，会运行旧UI生成方法，因此本轮采用独立入口执行实际编译及 ShaderUtil 检查。

验证程序参数：-water-v26-check -water-v26-dir "Docs/SeaSurfaceV26" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080。运行工作目录必须为工程根目录；该检查会用临时存档并在完成后退出。
