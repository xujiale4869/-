# v26.2 水面焦散、波光与 F7 调参回执

按用户提供的 gpt_v26_2_sweetspot_all_in_one 接入 v26.1 工程。默认焦散 0.92、碎光 1.85、聚焦指数 72、泡沫 0.95；全部14项光学默认值、五项颜色和 Water.mat 已同步，原有四份纹理引用保留。

F7 打开/关闭五滑块调参台，包含“清雅柔和 / 黄金平衡 / 璀璨烈日”三档预设。F6 继续切换原/新水面材质，F9 显示帧率。调参只作用于本次运行，重新启动恢复黄金平衡。

Shader 按附件原文提取。C# 光学、波形和生态代码采用附件；实机发现调参台与标题栏重叠后，将面板下移68px，增加实色背景并恢复GUI颜色状态，另补v26.2启动提示。SeaWaterTuningInputV262 配合原UI的指针判断，避免拖动 IMGUI 滑块时触发投钩/收线。原 F7 保护与旧环形鱼群隔离 partial 保留。

## 构建与验证

最终构建：Tools/SeaSurfaceV262/build-02.log，Unity 2022.3.62f3c1 / URP14。构建成功，Shader错误0；增量构建仍有1项已有 OceanSurface 过时API警告，不称全工程无警告。

最终自动检查：Verification/water-v262-review02。44项通过，运行错误0、警告0，覆盖资源/13项运行时与材质数值/三档预设/鱼群与安全位置/F6材质路由/暂停/既有船推进/独立临时存档。程序不是完整人工钓鱼或水族馆回归，图像检查也不代表用户已认可美术。

另有 Verification/water-v262-input01 的11项操作检查通过：通过Windows实际键盘鼠标输入，F7打开面板、鼠标改变焦散滑块、点击黄金平衡恢复五项值、F7关闭面板；在真实取竿状态下操作，仍保持待投钩状态，没有误投钩。原生输入驱动在每次操作前检查目标游戏窗口焦点，日志见 Tools/SeaSurfaceV262/input-01.log。其余四个滑块未逐个做OS鼠标拖动；三预设的五项参数均经程序检查。

| 1920×1080，不限帧，固定镜头，每状态480帧 | 平均FPS | P95帧耗时 |
|---|---:|---:|
| 原水材质对照 | 128.64 | 8.58ms |
| v26.2 | 120.65 | 8.87ms |

设备为 Intel Arc 130T，画面renderScale继承v24的1。首轮同光学代码短窗为约142FPS，采样存在波动；不把单次差值当精确Shader耗时，不外推全地图、长期或4K。第一轮截图中的面板重叠已修订，最终采用review02截图。

270项旧源码/Shader/场景/设置输入中266项哈希一致；4项预期变化为主控制器、光学Shader、QA和SeaLifeUI指针判断。Water.mat另有参数更新；新脚本和文档另列。正常玩家存档在全部检查前后SHA256相同，所有测试写独立临时存档。

## 交付入口

- 当前程序：Builds/SeaSurfaceV26/Guichao-SeaSurfaceV26.exe。
- 桌面“深海灯塔－最新版”和“深海灯塔-水面光学-v26.2”指向当前程序。
- 原v26.1完整程序备份：Builds/SeaSurfaceV26-v26.1-backup，其版本快捷入口改指备份。
- 更早的v26.0及其他版本保留。
- 本目录 Runtime 保存最终检查截图与结果，NativeInput 保存原生输入结果；PreviousV261 是旧版证据。
- 按交接更新 Docs/SeaSurfaceV26/preview.jpg、03-next-wave.jpg、result.json；同时保存原水对照与调参面板图。

重建：Deepsea.Editor.SeaSurfacePolishV262.SyncAndBuild 同步当前核心默认值后构建现有场景；普通 SeaSurfaceBuildV26.Build 保留已保存材质/场景。历史 v26.1 SyncAndBuild 仍断言旧参数，不用于当前版。不要无意运行 ImportAndBuild 重新生成场景。

自动检查启动参数：-forceV26Water -water-v26-check -water-v26-dir Verification/water-v262-custom -screen-fullscreen 0 -screen-width 1920 -screen-height 1080，工作目录为工程根目录。原始附件在 Tools/SeaSurfaceV262/OriginalInputs。尚未完成目标原图匹配、全地图长期性能或用户美术认可。
