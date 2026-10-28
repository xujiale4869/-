<!-- repository-integration-20261028 -->
> 当前生活试玩入口为 Builds/SeaLifeWaterV22/Guichao-SeaWater.exe，场景 Assets/Deepsea/Scenes/CoastSeaLifeWaterV22.unity。
> 本地主仓库、五个固定上游子仓库与Git LFS已建立。首次克隆需 --recurse-submodules，再执行 git lfs pull。
> 实际接入与候选状态见 [GitHub内容接入记录](Docs/GitHub内容接入-20261028.md)。以下旧玩法说明为历史版本，不代表已重新接入当前生活切片。

# 深海灯塔 · 归潮湾 v0.3

Unity 原生海陆探索原型。按 2026-10-16《浅海基础策划 v0.7》和 2026-10-15《UI 第一版设计》接入工具、可见居民备料和统一界面。

## 试玩

Windows：运行 Builds/Windows/GuichaoBay.exe，保留同目录 GuichaoBay_Data、UnityPlayer.dll 等文件。

- WASD：驾驶 / 步行；Shift：快走。
- F：低速靠近登陆岸下船；回到当前登陆点登船。
- E：最近对象交互、靠泊结算、采木 / 拾取、打开村仓。
- 1 工作斧、2 修理箱、3 割网钳、4 观测浮标、5 诱饵盒、6 望远镜；R 使用。
- Tab：背包与仓储、船只配装、居民协作。Esc：关闭当前窗口 / 暂停。M：海图。Q：短距探测。L：船灯。X：解开船载拖缆。
- 望远镜：R 打开后画面放大，鼠标扫视远处；R、移动或切换工具退出。
- 暂停页提供 100%、125%、150% UI 缩放和音量调节。

## 新内容怎么体验

1. 在归潮村低速按 F 下船，从主栈桥往村后走，经缓坡到本岛林地。靠近海松按 E 开始持续砍伐，移动取消；三次击打后留下树桩和一捆 2 原木。按 E 拾取。
2. 回村仓，通过背包选中原木后交付。随身原木上限 6，村仓上限 24。维修构件与原木分别记账；维修构件随身上限仍为 6。船舱原木有独立 8 根容量。
3. 前往雾岬岛东岸救出林舟，等他走到船边一起登船，返村按 E 靠泊安置。Tab → 居民协作 → 委托采木。他用自己的斧头走到本岛林地、采木、拾取、搬回村仓，交付 6 根后停止。你可继续出航；菜单暂停时工作也暂停。
4. 港口建设页用村仓 6 原木修复西侧装卸码头。新增平台可以行走，西侧可低速靠近后按 F 登岸。玩家自采和林舟交付的木料共用，材料扣除一次。

树木有限、不自动刷新。任务结束、仓库满、林地耗尽均有状态说明。取消任务不撤回已交付成果；手中的原木先送回。林舟一次只做一项工作，备料与节油保养互斥。当前原型不计算离线工作产出。

## 原有内容

连续自由航行；南、东两个雾岬岛岸与相通步行路线；观测站故障检查与修复、转天线读两类信号；林舟同行登船；青鹭号现场修复自动返村或船载拖回；两槽船载模块；东岸接应补给；回收、鱼群记录、灯塔维修、轻绞盘、观测平台和可选守港演练。

基础检查不再要求先选观察镜。维修页面可直接取用修理箱；手持工具各有独立模型与图标。背包、现场、港口、暂停只保留一个模态窗口，关闭时恢复打开前的暂停状态。查看库存不能远程取物。

## 工程

Unity 2022.3.62f3c1 / URP 14.0.11 / uGUI 1.0.0 / TextMeshPro 3.0.6。打开 Assets/Deepsea/Scenes/GuichaoBay.unity。运行时由 WorldBuilder、IslandTerrain 和 TimberWork 创建世界。

正式存档沿用 GuichaoStudio / 归潮湾 - 深海灯塔 / guichao-save-v1.json，兼容旧进度并补齐新增字段。原 v0.2 工程保留。正式试玩和新版本共用玩家进度；自动检查使用隔离进度，不写正式存档。

关键文件：UIRoot.cs（布局与统一窗口）、TimberWork.cs（有限树木、原木和居民工作）、ToolModels.cs（工具形象）、ExpeditionAssets.cs（物品规则）、ExpeditionController.cs（海陆交互）、GameRules.cs / SaveStore.cs（存档）。

首次导入如需重建 UI 字库：菜单工具入口或执行 Deepsea.Editor.UIPrepare.Run。发行包保留了字库、TMP 必要资源和图标，不依赖用户安装中文字体或运行时联网。

验证入口：Tools/Unity-Check.cmd Import / UI / Play / Expedition / Build；UI 用真实场景检查工具、物资、暂停、居民采木交付与码头，Windows 可通过显式 -deepsea-check 参数运行隔离检查并输出截图，正常运行不会启动此检查。

## 许可

游戏代码、模型生成代码及多数着色器为本项目编写。水体另含 Unity Boat Attack 波浪源码与贴图，详见 Assets/Deepsea/ThirdParty/BoatAttack/SOURCE.md 与 LICENSE.md。UI 使用 Unity 原生包；未添加 UI Extensions、FairyGUI 或 REA。

Lucide 离线 PNG 由对应 SVG 转换；版本固定于 Assets/Deepsea/Resources/UI/Lucide-version.txt，完整许可保留在 Lucide-LICENSE.txt。中文字库 Guichao Sans 是 Noto Sans SC 的裁剪与静态化版本，按 OFL 许可重新命名，完整许可在 Noto-OFL.txt。Unity/TMP 必要资源保留其许可证和目录。

当前仍为几何造型的可玩原型；轻型手持牵引钩、岛外资源运输、石料采集、接管居民和更完整生活系统未在本版加入。自动流程检查不代表趣味性或长期数值平衡已验证。


## 水面改进（2026-10-16）

已选择性接入 Unity-Technologies/boat-attack-water：原版 GerstnerWaves.hlsl、WaterNormals.tif（RG/BA 双法线数据）和 basicFoam.png，保留 Unity Companion License。没有导入完整预览包，也没有安装 Crest 或 Ocean-URP。

海面改为四组方向波浪、双尺度法线、半分辨率平面反射、水深吸收与岸边泡沫。URP 改为不透明物体之后复制深度，并补齐渲染器资源；适配正交镜头。船体视觉起伏与尾流使用同一波高采样，保持原有平面刚体驾驶规则。远景衰减细节，船尾由细线改为逐渐扩散的泡沫带。

水面测试入口：Deepsea.Editor.WaterCheck.Run；新试玩构建入口：Deepsea.Editor.WaterCheck.Build。前后截图与结果位于 Verification/water-before、Verification/water-after。新版本输出 Builds/WaterPreview/GuichaoBay.exe，原 Builds/Windows 试玩包保留。


### 浅海修正 v2

修复物体边缘深度被误当作海域水深的配色问题：海域颜色改用连续海床高度图，物体深度只参与少量透射和接触泡沫。折射重新采样深度，拒绝从水面以上的前景读取偏移颜色。雾岬岛旧矩形网格外的水下坡面延伸，并补全无碰撞的连续海床。

水色改为蓝绿浅海，浅滩呈绿松石色，随海床逐渐过渡到湾外蓝色；减弱灰暗雾色。保留 Boat Attack 波浪和贴图，调整主波方向与振幅，增加米级定向流动、缓慢浪峰亮部、浅水光纹和逐像素法线，减少三角形明暗面。

最新画面检查位于 Verification/water-shallow-v2：港口、岛屿全景、东岸、船边近景、航行，以及停船固定镜头连续画面 motion-00 至 motion-08。water-motion.mp4 为连续帧演示。截图与像素变化检查不能替代用户对美感的判断。


浅海 V2 Windows 试玩输出：Builds/ShallowWaterV2/GuichaoBay.exe。显式参数 -water-visual-check 会使用隔离进度，采集 Windows 实际窗口画面并退出；正常启动不会开启采集。输出 Verification/water-player-shallow-v2，检查入口不会改写正式玩家进度。


### 浅海 V3（2026-10-17）

继续细化浅海 V2：平面反射生成 mip 链并按粗糙水面过滤，减轻低多边形岛岸倒影的硬边；降低贴图引起的大块明暗，强化随主浪推进的亮部，保留蓝绿浅海渐变。修正海床高度图的 texel 中心坐标。

构建输出 Builds/ShallowWaterV3/GuichaoBay.exe。显式 -water-visual-check -water-video 参数启用隔离画面检查与 24 fps 固定模拟步长连续帧采集，正常游戏不启用。输出 Verification/water-player-shallow-v3。

浅海 V3 已完成 Windows 画面与连续运动检查。桌面水面改进版指向 ShallowWaterV3，水面演示指向 Verification/water-player-shallow-v3/water-motion-24fps.mp4。
