# 红枫海岬 v19

独立Unity美术场景，原可玩版v15、旧样板v16/v17均保留。

入口 `Rebuild-RedMaple.ps1` 默认检查SHA256并同步已验收输入；加 `-Build` 才构建。冻结输入依赖当前工程v17的灯塔、码头、海床和海面等既有资产，不能脱离原工程当作完整项目。

构建类 `Deepsea.Editor.CoastMapleBuild.Build`；场景 `Assets/Deepsea/Scenes/CoastArtMaple.unity`；程序 `Builds/CoastArtV19/MistCape-RedMaple.exe`。

源文件和制作脚本在 `C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v19/`。手动运行build/01_maple_cape.py会生成新稿，必须重新检查，不保证跨环境逐字节复现。默认入口不运行生成器。

操作：1全景、2岸边、3礁台、4水下、5灯塔、6岸石；拖动观察、滚轮缩放、WASD平移；F7对照v17、F8暂停落叶、F4白模、F10隐藏说明。

自动验收必须使用可见窗口：`-showcase-check -review-tag final -screen-width 1920 -screen-height 1080 -screen-fullscreen 0`，工作目录为工程根目录。`-showcase-preview`只输出多镜头截图和运动检查，不采样帧率。验收附带144张实际逐帧画面，供检查落叶接触与漂流。
