# v16 独立三维美术样板

2026-10-24。用户要求产生整体提升，授权制作独立样板。本目录制作、构建新的美术观察场景；原 v15 游戏保持为可玩基线。技术冻结表示保存已检查输入，不表示用户已经认可美术。

## 打开

桌面入口：深海灯塔-独立美术样板-v16。

程序：`Builds/CoastArtV16/MistCape-ArtSample.exe`，工作目录为工程根目录。1全景，2岸边，3礁台，4水下；左键拖动，滚轮缩放，WASD平移，Q/E升降，F4检查几何，F10隐藏文字。船和鱼为环境动态展示，没有接入原航行、上下船、步行、穿洞和存档控制。

可编辑场景：`Assets/Deepsea/Scenes/CoastArtShowcase.unity`。新环境、旧灯塔/船/人物、镜头、水面、灯光与后期均已保存到场景。运行时补充鱼群和环境摆动。Resources/CoastShowcase 只服务此样板；原 BayPipeline 没有被替换，样板使用自己的管线和环境遮蔽配置。

## 制作与重建

`Rebuild-ArtSample.ps1` 默认校验并恢复冻结的9项新输入（JSON压缩保存、3张贴图、3个C#文件、2个着色器）。先验证压缩文件，再解压并校验原始SHA256。

加 `-Build` 使用 `Deepsea.Editor.CoastShowcaseBuild.Build` 构建独立样板。此入口不同于原游戏的 CoastMaterialsBuild。构建过程暂用样板管线与产品名，结束时恢复原设置。

加 `-GenerateGeometry` 才运行本地 Blender 新稿配方，输出在本目录的 exports/review 与 Blender 主档，同时更新样板的 ThirdParty 输入。这不是冻结恢复，不承诺跨版本逐字节复现，需要重新进行画面、几何、鱼群路线、实机检查。冻结 accepted 不会自动被新稿覆盖。重新生成贴图需要图像生成服务，不属于这个本地脚本。

配方 `build/01_scene.py` 与 `02_life.py` 制作新地形、岩岸、礁台、码头、松树、珊瑚等；`03_route_audit.py` 检查实际可见网格上的鱼群路线；`04_prepare_delivery.py` 打包源贴图与GLB查看档。Blender显式使用本机5.2.2 LTS完整路径。

## 证据与边界

以 `Verification/coast-art-v16-final/` 为最终实机证据，draft-a至draft-e均为过程稿；只有 `-showcase-check` 采集1200帧，`-showcase-preview` 的fps=0不是性能测试。可见窗口原生1920×1080验收，Hidden可能产生黑帧。

原水面着色器SHA256为 `4a7fdfa9164a208a8136c192cb3284ea335d2b60e5ec03f7fe7a652ac02c015c`，折射0.0022。新海床和灯光会改变最终水面观感，不能承诺与v12逐像素相同。新珊瑚材质有针对保留水体红光吸收的颜色补偿；未回写原水面。

Blender主档包含新环境，Unity场景另包含已有灯塔、船、人物、精确水面和灯光。GLB供几何/部分材质查看，不携带完整Unity水体、三平面地面和后期；GLB为了标准回读把最低点抬至0，具体偏移和坐标恢复见 exports/asset-manifest.json。精确引擎几何是JSON。

本次没有安装新的GitHub依赖。扫描岩体和部分贴图复用工程已有CC0资源，许可副本见 SOURCE-LICENSE-existing.txt；3张原创生成贴图与用途见 source-ledger.json。没有用生成的整幅场景图替代三维几何。

模型验证存在UV接缝共点、植物相交叶片与可编辑封口多边形警告；并非0警告。无负缩放、零面积面或导入失败。实际检查和剩余美术差距以完整报告为准。
