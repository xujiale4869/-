# 灯塔与岸石精修 v17

在用户认可的v16独立样板上，精修灯塔与旁边4块岸石。原游戏可玩版仍为v15，原v16程序、入口和339个制作前脚本/着色器保持不变。

程序：`Builds/CoastArtV17/MistCape-Materials.exe`。Unity场景：`Assets/Deepsea/Scenes/CoastArtRefined.unity`。私有资源在Resources/CoastRefinement。1全景、2岸边、3礁台、4水下、5灯塔、6岸石；F7用同一镜头切换v16环境/灯塔与精修环境/灯塔。船、鱼和植物摆动继续运行，截图不是逐像素相同时间冻结。

灯塔采用新灰白灰泥底色、既有细颗粒法线与独立的基座露石/窗下雨痕/金属风化遮罩。43块真实倒角石砌基座与原门洞留出通路。4块岸石局部干湿、源粗糙度和少量苔藓层次；359个底部顶点最多调整.08558m，3处只在干地出现的矿物/土壤接合补片。礁体、海床、树、木栈道和海带的几何保留v16；58项同名环境网格逐字节一致。珊瑚311、干净绿色海带63、鱼178，没有新增投放。

水面核心SHA256 `4a7fdfa9164a208a8136c192cb3284ea335d2b60e5ec03f7fe7a652ac02c015c` 和折射.0022保留。相机、太阳和后期设置相同，水面没有跟随材质精修一起调整。

`Rebuild-MaterialStudy.ps1` 默认恢复6个SHA256冻结输入并检查v16几何/源粗糙度依赖；默认恢复已测试。`-Build`通过 `Deepsea.Editor.CoastRefinementBuild.Build` 生成新样板，不使用旧游戏构建入口。`-GenerateDraft`才产生新稿，需要本工作区v14/v16 Blender源文件与Blender5.2.2 LTS，并设置/使用COAST_WORKSPACE_ROOT；不会重新生成图像、不会替换accepted。新稿仍要重新验收，不承诺跨环境逐字节复现。

最终证据：Verification/coast-art-v17-final。`-showcase-check -review-tag final`在可见窗口采集同镜头前后与近景截图及1200帧性能；`-showcase-preview`仅检查截图，不测性能。Hidden窗口可能黑帧。

源文件分为岸石/基座主档和灯塔主档，位于工作区art-v17。前者有149281面；GLB空场回读面数一致、0错误，模型检查0警告0失败。后者沿用已存在的塔形，主要更新灰泥底图；精确的局部风化和所有角色/水/光在Unity场景。GLB是部分材质的几何查看档，具体偏移见exports/asset-manifest.json，不宣称携带完整Unity着色器。

新生成贴图为1254×1254灰泥底色；材质使用混合来源的细颗粒法线和程序遮罩，没有声称新图附带匹配烘焙法线/粗糙度。既有岩石源为CC0；本轮无新增GitHub依赖或下载。来源见source-ledger.json与许可副本。

样板仍有重复扫描岩形、较硬的部分岩脚、偏平的植被卡片和较规整的门窗。技术检查不是最终美术认可，也不是完整航行/步行玩法集成。
