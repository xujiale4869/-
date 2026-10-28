# 海湾生活 v23：流动水面与惯性船控

日期：2026-10-28。基于已清理水面杂点的v22，接入GitHub波浪与水阻计算，并适配现有小木船和玩法。

## 启动与操作

桌面入口：深海灯塔-流动水面与船控-v23。

程序：Builds/SeaLifeNavigationV23/Guichao-SeaNavigation.exe。工作目录为Unity工程根目录。

- W向前推进；松开后滑行减速；S先制动，再倒车。
- A/D转向，转向力度与航速相关，舵向与转速有平滑过渡；倒车时反向响应。
- 原有F上下船、E活动、Tab鱼箱等操作继续使用。
- F7切换v22水材质及原推进算法与v23波浪/船控。两种状态共享v23碰撞轮廓、船身视觉层与尾迹，因此不是完整v22二进制的逐项还原。
- 原v22桌面入口及程序保留，可独立对照。

程序沿用v21产品名以承接正常存档，窗口标题仍包含v21。正常存档位置及数据格式未改变。不要运行旧版恢复脚本覆盖这轮新增的控制入口。

## 已接入内容

1. Boat Attack的Gerstner方向波计算，固定3层柔和波，波幅总上限0.122m；CPU浮漂查询通过反求水平位移跟随世界坐标。船身在四个采样位置响应俯仰/横摇。GPU沿用v22细分水网格，并插值顶点波面法线；不是完整流体模拟或逐像素解析曲面。
2. 波纹贴图按两个方向流动，波峰明暗随实际起伏移动，保留青蓝深浅吸收、0.0022折射及无随机碎光的基线。
3. Habrador的黏性水阻与摩擦系数公式，适配原水平刚体。推进、制动、侧向水阻、舵向平滑、转向惯性、船身转弯倾斜为本地实现。
4. 历史V形尾迹随真实前进速度产生，转弯后旧点留在世界位置，随时间向两边扩散，停船后过期；增加短中央扰水。最多112历史点，最长5.5秒。倒车的局部推水方向随实际行进方向改变。
5. 收尖凸碰撞轮廓与低摩擦接触，减少矩形船首卡角。已验测试障碍阻挡与倒退脱离；尚未做全海湾浅水路线验收。

## 编辑和构建

- 主源：Assets/Deepsea/Scripts/Runtime/SeaLifeNavigationV23/。
- Shader：Assets/Deepsea/Shaders/SeaLifeNavigationV23/。
- 材质：Assets/Deepsea/Resources/SeaLifeNavigationV23/。
- 场景：Assets/Deepsea/Scenes/CoastSeaLifeNavigationV23.unity。
- 构建：Deepsea.Editor.SeaLifeNavigationBuildV23.Build。
- 原SeaLifeRuntime.cs只有一处FixedUpdate路由增加；没有v23组件的场景继续运行原控制算法。改前源码在Before-20261028/SeaLifeRuntime.cs。
- Build打开已保存v23场景，缺失时才Prepare。显式Prepare会重新从v22生成v23场景和材质，后续场景编辑前先了解这一覆盖边界。
- Tools/SeaLifeNavigationV23/Rebuild-Navigation.ps1默认只核对accepted-manifest.json；-Build先校验再构建。没有自动恢复覆盖功能。
- accepted为本轮输入快照，依赖原v22及更早环境资产，不能当作完整Unity工程或逐字节二进制重现包。

未新增UPM依赖，未改URP版本、原水材质和旧场景。已使用的上游固定提交、原始源码和许可在ThirdParty/，程序Licenses/SeaLifeNavigationV23亦包含副本。

## 验收

正常可见窗口启动参数：-sea-navigation-check -navigation-review-dir Verification/独立新目录 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080。追加-navigation-record输出144帧、12fps的12秒引擎录帧，可用FFmpeg编码。该视频不是实时录屏或性能测量。

检查使用唯一临时存档，自动设置起点并通过真实输入变量驱动刚体，覆盖起步/滑行/制动/倒车/转向、尾迹、测试障碍、菜单阻断推进、钓鱼浮漂、暂停与有限存档回读。与人工完整游玩、全海湾浅水、完整水族馆回归、生态审计区分。

最终证据目录和结果见同目录reference-analysis.md。早期build-01启动时因Temp目录缺失触发Unity项目锁误报，已在确认无Unity进程后建立Temp并正常构建；没有终止其他编辑器进程。review01/02为过程结果；review02深水明暗过强稿已撤回，最终以review03为准。
