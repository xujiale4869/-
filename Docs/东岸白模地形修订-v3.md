# 东岸白模地形修订 v3
日期：2026-10-21。

本版继续依据用户红线修改礁台、岩洞和浅湾，以消除重复高柱、规整门洞及薄台边缘的人工感。目标图为AI示意，Screenshots-Actual内为Unity实际运行截图。

## 当前改动
- 保留v2的两岸转折与收窄，增加厚实侧礁台和局部低断口；岩体采用更宽的断面与接近平面的台顶。
- 浅湾增加四处岸脚宽岩台、海底隆起和低洼槽；岸线增添凹湾与突出部。
- 去掉首轮截图中人工感明显的薄盖板凸缘，改用有厚度的岩阶。
- 复用原工程CoastSlice/boulder_01，先用工程内已有UnityMeshSimplifier统一减面，再用于主礁体、岸壁、侧岩台、前景和岸脚；保留平滑法线，避免细碎扫描细节与简化大块地形混用，以及密集三角面明暗噪声。所有实体仍共用同一灰材质。
- 岩洞由规则拱门改成更宽、更低的不对称裂隙，内部有五段深度变化，并与洞顶错断岩板衔接。
- 22个低面数灰色鱼形测试体沿贯通路线往返。它们用于检验空间，尚不是成品鱼群或完整生态。
- 船、码头、灯塔的原坐标和原角色控制器继续使用。

## 体验
桌面入口：深海灯塔－白模验收版。
WASD航行/步行；靠近栈桥、低速时F上下船；滚轮缩放。
F5正交/弱透视；F6参考/跟随；F7岩洞观察；F8帧率；F9水面开关；F10界面；Home复位。
交互版限60帧，白模不写入正常游戏进度。

## 原工程与共用资源
独立场景：Assets/Deepsea/Scenes/CoastBlockout.unity。
共用Prefab：Assets/Deepsea/Resources/CoastBlockout/EastCoast.prefab。
共用几何源：Assets/Deepsea/Scripts/Runtime/CoastBlockoutLayout.cs、CoastBlockoutRock.cs、CoastBlockoutGeometry.cs。
构建器生成白模使用的可读归一化扫描副本；原扫描资源保持不变。水下扫描形体与程序生成岩体均使用同一组三角形用于显示、MeshCollider与垂直海底查询；洞内通行另用真实物理空间采样检查，不能用垂直高度场代替。
当前4块连续地形，共74992个地形三角形、180个岩体，其中77处复用统一减面后的扫描形体；扫描源由8264面减至702面。
原制作版仍指向CoastSliceV2；原GuichaoBay.unity与V2程序已核对接入前SHA256，保持一致。
本轮源文件备份：Tools/CoastBlockout/Before-Natural-v3-20261021。

## 验证与证据
最新完整记录：Verification/coast-blockout-natural-v3/result.json。
包括160个带空间余量的中心路线采样、3520个实际鱼体空间采样，洞内拱顶/侧壁射线，海底几何与物理高度一致性，原船控制器航行，上下船以及码头至灯塔12个往返路径点。
完整物理检查：passed=true；船由原控制器航行15.749米、人物往返42.599米，12个路径点全部达到，3520个实际鱼体空间采样全部畅通。
1080p接电、预热180帧后测1200帧、解除限帧、计时不含截图写盘：当前白模固定镜头平均169.50FPS，P95为7.371ms；不代表完整美术版的全程帧率。
完整检查后只修正共有顶点的显示法线，不改顶点位置或三角形；最终渲染记录为Verification/coast-blockout-natural-visual/visual-result.json。
最新实机截图位于Verification/coast-blockout-natural-visual，reference.png、no-water.png、orthographic.png及cave-no-water.png均由实际Unity程序运行截图。
构建记录：Verification/coast-blockout-natural-build.log。
历史首版记录保留在Verification/coast-blockout-v1，不应与本版结果混用。

## 验收边界
本版继续改善灰模结构，所有实体仍用同一灰材质；没有进入正式贴图和生态制作。最终自然度、地形分层和构图以实机与目标图的目视对照为准。自动检查通过只证明其覆盖的行为与几何条件，不等于第1、2阶段全部美术验收完成。
正式制作版尚未替换成白模；验收后复用同一套EastCoast几何，再做材质与生态接入。

## 构建
完整重生成与构建：Deepsea.Editor.CoastBlockoutBuild.Build。
完整检查参数：-whitebox-check；仅渲染复核：-whitebox-visual。
本包包含生成源文件、两个Shader、安装/构建脚本、AI目标图、真实运行截图与检查记录，不包含完整UnityPlayer二进制；可执行版保存在用户原工程Builds/CoastBlockoutV1。
本轮未下载第三方素材；复用原工程的boulder_01扫描资源和UnityMeshSimplifier，原许可证随构建ThirdParty保留。本包必须在已有CoastSlice资产的原项目中构建。
