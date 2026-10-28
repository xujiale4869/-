# v25 鱼类与展缸升级回执

已在 v24 海面与船控场景上增加独立 v25 入口，保留原收藏规则与 schema 1 存档。

## 交付

- 场景：`Assets/Deepsea/Scenes/CoastSeaLifeV25.unity`
- 程序：`Builds/SeaLifeV25/Guichao-SeaLifeV25.exe`
- 桌面“深海灯塔-鱼类与展缸-v25”：正常游戏，沿用原收藏存档。
- 桌面“深海灯塔-v25展缸演示”：三个展缸、九条临时样本鱼，不写入正常收藏。
- 三种鱼 FBX、三件展缸 FBX、三张 512×512 RGBA 贴图已由 Blender 5.2.2 LTS 导出；原始输入保存在 `Tools/SeaLifeV25/OriginalInputs`。
- 鱼体每种 793 个导入顶点、1260 个三角形，UV.x 作为头尾动画权重。尾部摆动、转弯侧倾和每鱼独立相位已接入。
- 展缸分为木材/黄铜/灯具、沙床/礁石、透明玻璃、水面与贴合沙床的焦散层。新外观保留原底柜碰撞体。
- ScriptableObject 数据库：`Assets/Deepsea/Resources/SeaLifeV25/FishAndAquariumDatabase.asset`。经济/稀有度字段是数据底座，本轮没有替换原鱼价和容量规则。

## 兼容修正

1. 等待 SeaLifeRuntime.Ready 后加载，避免启动顺序覆盖新网格/材质。
2. 用鱼与展缸创建钩子更新新实例；删除每帧全场景查找和反射。旧场景未挂 v25 组件时为空操作。
3. 运动路径继续拥有朝向；摆尾不累积根节点偏航，暂停立即冻结。
4. 修复 Blender 反向重复面异常、PNG 上下翻转、退化鼻尖与鱼眼贴图取样。
5. 用 FBX 四个坐标标记烘焙 Unity 网格，核对单位、朝向与材质槽；展缸底柜和沙床校准到实际 v21 尺寸。
6. 将数据库主类放入同名脚本以支持 Unity ScriptableObject 资源绑定。
7. 玻璃按缸局部高度计算，不受展室世界坐标影响；水光使用单层水面与沙床焦散，避免透明立方体六面叠加。

## 最终验证

最终来源：`Verification/sea-life-v25-review02`，构建日志 `Tools/SeaLifeV25/build-02.log`。review01 是暂停检查失败的历史记录，不能作为最终交付。
60 项自动检查通过，运行错误 0、警告 0。Unity 构建成功；仍有原 OceanSurface.cs 的过时 API 编译警告。Blender 有 use_nodes 的弃用提示，不报告为全工程无警告。
已检查真实刚体推进、取竿/投钩/咬钩/提竿/张力收线/保留、首缸免费与金币不足、三种鱼放入/取回、6条容量上限、海带主题、三缸重建、独立相位、暂停与原存档回读。
256 项旧输入中仅 SeaLifeActivities.cs、SeaLifeAquarium.cs 各增加一个可选创建钩子，其余 254 项哈希一致；完整文本回查仅有这两处变化。正常玩家存档 SHA256 前后相同。

1920×1080、不限帧、同一海面镜头，各预热80帧并采样600帧：旧鱼137.88 FPS / P95 8.23ms；v25鱼136.99 FPS / P95 9.12ms。新鱼单缸近景369.23 FPS / P95 3.19ms。设备为 Intel Arc 130T；这些是短时固定镜头样本，不是全地图、长期卡顿或4K验收。
`preview.mp4` 为144张真实引擎帧按24fps固定步长录制的6秒演示，分享版1280×720；不是实时性能证据。

本轮完成资产接入，不代表最终美术获用户认可。当前为程序化鳞片底色与轻量玻璃/水光，未制作完整PBR贴图组、真实体积散射或微气泡；未重做UI、钓鱼人物动画与海湾生态分布。

## 再次构建

普通构建使用 `Deepsea.Editor.SeaLifeV25Build.Build`，保留已保存的 v25 资源与场景。
只有需要重新生成时才运行 Blender 脚本，再调用 `Deepsea.Editor.SeaLifeV25Build.ImportAndBuild`；后者会重建 v25 材质、网格、数据库及场景。不要用历史版本恢复脚本覆盖本轮源码。
Blender 命令：`blender.exe -b -P Tools/SeaLifeV25/generate_fish_and_aquarium_v25.py -- --out-dir Assets/Deepsea/Resources/SeaLifeV25`。
运行自动检查：程序参数 `-v25-check -v25-evidence Verification/sea-life-v25-custom -screen-fullscreen 0 -screen-width 1920 -screen-height 1080`，工作目录为工程根目录。
