# v14 基础模型与材质入口

本轮使用已有 Blender、贴图和原工程中的 FastNoiseLite，没有新增网上插件。按用户反馈停止增加珊瑚，先恢复 v12 水面核心，再处理基础模型与材质。

## 实际修改

- 完整替换灯塔的可见模型：收分塔身、真正凹入的拱窗与门洞、独立基座、完整灯室和屋顶，8 个运行时材质组。旧碰撞仍保留。
- 原有 2K 灰泥、石材的颜色/法线/粗糙度贴图按真实 UV 使用；灯室玻璃使用明确的透明混合，木板按板向分配 UV。
- 礁区采用分层截面与不规则裂隙，保留几何折角；新旧海床共用同一套材质计算，收回对浅滩的覆盖。
- 原地形仅改善渲染法线；原碰撞网格不变。珊瑚数量上限保持 512，按新礁面重新落点。

## 可重用制作入口

工程 Tools/CoastArtV14/Rebuild-CoastFoundation.ps1 默认只同步 accepted/LighthouseMeshes.json 冻结资产；加 -Build 后构建当前 v14。

加 -GenerateLighthouse 只在工具目录生成新稿，不替换已验收数据。生成过一次 UV 浮点尾差（最大约 2.39e-7），因此不承诺生成器跨次逐字节一致。交付与重建默认使用冻结 JSON，并核对 SHA-256。

AR_CoastLighthouse_master.blend 保留可编辑结构和材质；delivery.blend 已应用修改器并三角化。GLB 用于其他软件回读；Unity 精确输入为 LighthouseMeshes.json。轴向为 Blender(x,y,z) → Unity(x,z,-y)，运行时根节点转 180 度、沿用原有 0.9 比例。

礁面由 CoastShelfShape/CoastShelfBuild 生成，精确数据存为 ShelfMeshes.json。运行时鱼群检查从实际网格顶点缓存插值，不再用连续公式代替锐利台阶之间的实际三角面。

## 来源与使用范围

灯塔几何和礁面截面为本轮本地制作；既有贴图的来源和许可沿用工程 ThirdParty/CoastArtV4、CoastArtV7 与构建内 ThirdParty 中的原始记录。FastNoiseLite 沿用项目已有 MIT 版本，本轮没有更新其源码。

本工具包依赖现有 Unity 工程及其贴图，不能当作一个独立游戏工程。几何检查、导出回读、玩法检查和视觉是否达到原图是不同结论；当前整景仍未达到原图。
