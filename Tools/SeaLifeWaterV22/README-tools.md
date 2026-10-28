# 海湾生活 v22：水面修订入口

## 启动与对照

- 桌面入口：`深海灯塔-水面修订-v22`。
- 程序：`C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3/Builds/SeaLifeWaterV22/Guichao-SeaWater.exe`。
- 工作目录：`C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3`。
- 正常参数：`-screen-fullscreen 0 -screen-width 1920 -screen-height 1080`。
- F7：同一场景中切换v21原水材质与v22新水面、船身视觉响应和尾迹。每次切换清理历史尾迹，需重新航行几秒观察。
- 原有WASD、上下船、钓鱼、鱼箱操作继续沿用。

本版为承接进度，保留v21产品名和原存档位置，窗口标题仍显示“海湾生活 v21”；请以程序路径或桌面v22入口区分。不会把旧进度当作新存档迁移或重置。

## 可编辑输入

实际工程中的源文件为制作主源；本目录stage是交付快照。

- `Assets/Deepsea/Scripts/Runtime/SeaLifeWaterV22/`：水面、船身视觉子层、历史水纹、漂叶绘制，以及同一partial类中的浮漂/水纹接入和独立验收。
- `Assets/Deepsea/Shaders/SeaLifeWaterV22/`：新水shader、共享波形公式、尾迹shader。
- `Assets/Deepsea/Resources/SeaLifeWaterV22/`：新水材质、尾迹材质、水网格。
- `Assets/Deepsea/Scenes/CoastSeaLifeWaterV22.unity`：新生活场景；v21场景保留。
- `Assets/Deepsea/Scripts/Editor/SeaLifeWaterV22/SeaLifeWaterBuildV22.cs`：构建入口。

水面细分34869顶点、68992三角面；主要可玩海域网格间距约0.65m，外围使用较粗间距覆盖原300×270m范围。三层解析正弦波幅总上限0.08m；不是完整流体模拟。CPU浮漂/落叶/船身与GPU水面使用同一公式；GPU以细分三角面插值，非逐像素解析曲面的几何精度承诺。

新增代码复用现有波纹贴图、落叶网格和材质，不引入新第三方包；历史GitHub尾迹参考仅用于理解实现思路，本轮代码为本地编写，没有复制BoatAttack资产或源码。

## 构建和冻结

项目工具：`Tools/SeaLifeWaterV22/Rebuild-Water.ps1`。默认只校验冻结输入，不自动覆盖改动；`-Build`先校验后构建。若修改了源码，校验会停止，需先检查新改动并建立新版本输入清单。

冻结资产位于同目录`accepted/`，清单为`accepted-manifest.json`。这只是本轮输入快照，依赖现有v21/v20资产；不是完整Unity工程备份，也不承诺构建产物逐字节相同。

构建方法：`Deepsea.Editor.SeaLifeWaterBuildV22.Build`。正常构建直接打开已保存的v22场景，不从v20或v21重建覆盖。只有场景缺失时才首次创建；显式`Prepare`会重新生成v22场景和水资源，后续手工场景编辑前需了解这一边界。

文档区`Install-And-Build.ps1`用于本轮从stage导入源码，不是后续日常构建入口；再次运行会用stage覆盖本轮新增源码。`prepare-shader.ps1`用于从保留的旧水shader准备独立新稿，也不是默认构建步骤。

## 验证范围

新验收参数：`-sea-water-check -water-review-dir Verification/独立新目录`。可追加`-water-record`输出144帧、12fps的引擎录帧。必须使用可见窗口，不使用隐藏窗口截图。

验收把活动数据换到唯一临时存档，输出路径在result.json中。检查实际船控、历史尾迹、停船消散、钓鱼浮漂、暂停以及有限的保存回读；不冒称完整手工游玩、全地图航路、浅水搁浅修复、完整水族馆回归或生态路线重测。

F7基线使用旧材质、固定尾迹参数与平水表现，但同样使用新版细分水网格。因此性能采样用于本程序两种显示状态的有限比较，不等于原版v21二进制的独立性能对比。

## 后续仍需处理

水面视觉仍需用户实看反馈，尤其波浪幅度、深水反射和水纹强度；不把测试通过写成美术已获认可。船的原推进、阻力和碰撞规则未重做，浅水搁浅另行处理。UI、生态分布和钓鱼动作不在本轮范围。
