# 海湾生活 v21

这是在红枫海岬 v20 美术场景上增加的独立可玩切片。新入口不加载旧 GameDirector，不接续旧救援/建设链。v15 原可玩程序和 v20 美术程序保留。

## 编辑和构建

- 新场景：Assets/Deepsea/Scenes/CoastSeaLife.unity。
- 新代码：Assets/Deepsea/Scripts/Runtime/SeaLifeV21。
- 构建：Deepsea.Editor.SeaLifeBuild.Build。
- 程序：Builds/SeaLifeV21/Guichao-SeaLife.exe。
- Tools/SeaLifeV21/Rebuild-SeaLife.ps1 默认恢复并验证本轮冻结输入；-VerifyOnly 只检查；-Build 在恢复后构建。冻结项含本轮代码、材质、字体及新场景，不含全工程依赖或第三方包。

缸体、拾贝点、吊箱和房间由可编辑C#配方生成；鱼的简化模型由 MakeFish 创建，均是玩法验证用资产。没有新Blender模型或AI生成贴图。源字体为本机未修改的 Noto Sans SC Regular 2.002；授权和来源记录随包保存。场景依赖现有v20资产。

## 游玩

WASD开船；鱼影附近减速按E取竿，左键选水面投钩；浮漂下沉按E，平稳时按住左键收线、挣扎时松开。选保留或放流。1选择钓竿，2选择挂钩；蓝色小浮标旁E准备吊箱、按住左键绞盘。码头附近F下船；WASD步行，贝壳/小馆门牌旁E交互。

室内B布置，三个固定摆缸位，首缸免费、后续35金币。E或Tab管理随身鱼与选中的缸；V观赏，F回码头。码头可出售随身鱼。随身与每缸容量均6条；贝壳与打捞船锚可作布景。Esc暂停/取消当前操作，F10隐藏界面。

## 存档和验收

正常进度使用新产品目录中的 sea-life-v21.json，并保留.bak。-sea-life-check 使用独立临时测试存档，从空集合开始；画面中的两条额外展鱼及容量测试鱼为明确测试样本，不进入正常游玩存档。

验收用可见窗口：-sea-life-check -screen-fullscreen 0 -screen-width 1920 -screen-height 1080；工作目录设为工程根目录。证据在 Verification/sea-life-v21-delivery。运行检查调用真实控制器/玩法方法、部分界面按钮；另外进行原生窗口输入抽查。自动检查会直接设置部分场景起点，不等于整个海湾所有路线已测。

本轮不是完整开放世界或城镇经营系统；未做NPC经营、自由缸体编辑、多岛地图或完整生态。房间与道具可继续精修。性能采样锁60帧，不能直接与旧美术程序的不限帧采样作比较。界面检查仅为限定状态的临时评审，不代表完整二维风格基准验证。
