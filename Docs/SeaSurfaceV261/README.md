# 深海灯塔 v26.1 接入记录

2026-10-29。使用用户提供的完整一体包接入现有 v26 场景，继承 v25 玩法与 v24 波形。当前入口为桌面“深海灯塔－最新版”，程序为 `Builds/SeaSurfaceV26/Guichao-SeaSurfaceV26.exe`。工程所在目录名 v0.3 是历史容器名，不代表当前游戏版本。

## 源码与参数

原包保存在 `Tools/SeaSurfaceV261/OriginalInputs/gpt_v26_1_all_in_one.md`。从中提取的 Shader 与主控制器原文保留，仅补终止换行，SHA-256 如下：

| 文件 | SHA-256 |
|---|---|
| SeaSurfaceSparkleV26.shader | ed7fa65ea361314cf33eea0624b97a5feadd27eb724b787ffd07452f0141af7a |
| SeaWaterArtUpgradeV26.cs | 42b6bb32f7d6547ad1aa49a390c233ba735e0bb3658243382ba08bac5ce1909c |

未下调原包亮度、焦散、碎光或泡沫参数。运行时与 Resources 序列化材质均核对：

| 参数 | 值 |
|---|---:|
| _CausticsIntensity | 1.68 |
| _CausticsScale | 0.22 |
| _CausticsSpeed | 0.34 |
| _GlintIntensity | 3.45 |
| _GlintScale | 13.5 |
| _GlintSpeed | 0.95 |
| _FoamIntensity | 1.28 |
| _FoamWidth | 0.62 |

原有焦散、泡沫、深度渐变与波形纹理继续使用；没有重新生成或改写这些贴图。原包创建 72 条银鱼、28 条金色礁鱼及蝠鲼；银鱼走纵向连续 S 路径。本轮没有另行重绘珊瑚资产。

新增独立 partial 文件 `SeaLegacySchoolIsolationV261.cs`，仅在 v26.1 控制器启用时停用名称精确匹配“沿用海岬鱼群表现”的 34 条旧 v21 环境鱼，禁用组件时恢复。第一轮截图发现这批旧鱼仍在绕圈，故增加此兼容处理；两个原包核心文件不因此修改。旧版程序保留原表现。

## 构建与实机验证

Unity 2022.3.62f3c1 / URP 14。最终 `build-02.log` 构建成功；Shader 编译错误 0。最终增量构建报告记录 1 项已有 OceanSurface 过时 API 警告；首轮完整构建仍有 24 项历史警告，不声称全工程无警告。构建摘要见 `Tools/SeaSurfaceV261/build.json`，本机日志留存于同目录。

最终证据为 [result.json](../SeaSurfaceV26/result.json) 与以下引擎截图：

- [原水材质对照](../SeaSurfaceV26/01-baseline.jpg)
- [v26.1 当前实机](../SeaSurfaceV26/preview.jpg)
- [下一水波时刻](../SeaSurfaceV26/03-next-wave.jpg)

review-02 的 34 项检查全部通过，运行错误 0、警告 0。覆盖资源连接、八项参数一致性、S 路径跨度与连续性、旧环形鱼群停用、安全水下位置、材质对照、暂停、既有船推进与隔离临时存档。F6 对照通过调用其共用切换方法验证，未声明操作系统键盘事件注入验收。对照只切换水材质，生态层在两侧保留。

| 本机固定镜头 / 原生 1920×1080 / 每状态 480 帧 | 平均 FPS | P95 帧耗时 |
|---|---:|---:|
| 原水材质 | 155.72 | 8.21 ms |
| v26.1 | 143.55 | 7.93 ms |

设备 GPU：Intel Arc 130T。两个采样时窗存在波动，不将差值当作精确 Shader 耗时，也不外推为全地图、长期或 4K 性能。72 条银鱼创建并处于可见状态；28 条金色礁鱼创建，其中 27 条处于可见状态，另 1 条由安全水下放置判定隐藏。

正常游玩存档在最终检查前后 SHA-256 相同，检查使用独立临时存档，没有覆盖玩家收藏。旧输入基线 268 项中，265 项哈希相同，3 项变化为原包主控制器、原包 Shader 与 QA 脚本。另有预期的 Water.mat 参数序列化更新及新增辅助文件；这不是全工程所有文件不变的声明。核对清单见 [baseline-verification.json](baseline-verification.json)，交付文件哈希见 `Tools/SeaSurfaceV261/accepted-manifest.json`。

首轮 v26.1 review-01 虽通过功能检查，但截图仍有旧鱼群叠加，不作最终画面依据。旧 v26.0 的截图、报告与结果保存在 [PreviousV26](PreviousV26/README.md)。

## 入口、旧版与重建

桌面“深海灯塔－最新版”和版本入口“深海灯塔-水面光学-v26.1”指向当前程序，使用 `-forceV26Water`。F6 切换水材质，F9 显示帧率。原 v26.0 完整程序备份在 `Builds/SeaSurfaceV26-v26.0-backup`，旧版本入口“深海灯塔-水面光学-v26”改指向此备份。更早版本保留。

需要重新同步原包参数并构建时，Unity batchmode 执行 `Deepsea.Editor.SeaSurfacePolishV261.SyncAndBuild`；辅助方法从原包控制器读取数值，更新现有 Resources 水材质并沿用现有场景构建。普通再构建使用 `Deepsea.Editor.SeaSurfaceBuildV26.Build`。两者不重新生成场景；不要把 `Tools/Unity-Check.cmd Import` 当作本轮导入命令。显式 `ImportAndBuild` 属于重生成步骤，会重建场景。

本轮没有收到目标原图本体，因此只确认原包接入、构建与上述范围内的实机表现，尚未完成原图匹配或用户美术认可。未做完整人工钓鱼、展缸及全海湾长期回归。
