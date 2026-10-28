# 深海灯塔当前工程

原可玩版本仍为 v15；最新独立美术样板为 v20 局部岸岩与水面碎光，见 Docs/岸岩精修-v20.md，输出 Builds/CoastArtV20/MistCape-FineShore.exe。v19与更早版本均保留，以下历史记录仅供追溯。

历史海岸美术版本：v11，2026-10-23 完成独立程序验收。主要分析与制作记录在 `C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v11/reference-analysis.md`，项目内副本见 `Docs/原图对照与美术推进-v11.md`。

- 保留原船只控制、上下船、步行、物理地形和存档；新岩层、珊瑚和岸边收口为外观层，没有新增碰撞体。
- 海带保持用户认可的干净绿色；已改为按实际显示的岩面采样根部，避免外观层与物理岩体不同造成悬浮。
- 三维参考制作使用 `C:/Users/许嘉乐/.agents/skills/blender-image-to-3d/SKILL.md`；素材交付使用 `C:/Users/许嘉乐/.agents/skills/ai-game-art-pipeline/SKILL.md`；二维图标与界面风格可用 `C:/Users/许嘉乐/.agents/skills/style-loop/SKILL.md`。先读相应技能。
- Blender 为 `C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe`；图像脚本优先使用 Codex workspace dependencies 中带 NumPy/Pillow 的 Python。
- v11 最终检查：`Verification/coast-art-v11/result.json`；参考实机图：该目录下 `reference.png`。原生1080p固定镜头1200帧平均92.02 FPS，不能表述为全地图长期稳定表现。
- 中间 `coast-art-v11-failed-hidden-window` 是黑帧失败记录；`coast-art-v11-visual` 的较早偏黄画面不是最终版本。以后以最终验收目录为基线。
- 继续改进应优先处理：主礁台大形与重复轮廓、浅水的青蓝层次、灯塔和码头资产、前后景组织。尚未达到用户原图的美术质量。
- 编译入口：`Deepsea.Editor.CoastMaterialsBuild.Build`，输出 `Builds/CoastArtV11/GuichaoBay-ArtV11.exe`。独立验收参数：`-coast-materials -coast-ecology -whitebox-check -screen-fullscreen 0 -screen-width 1920 -screen-height 1080`，运行目录必须为工程根目录。

## 2026-10-23 当前进度：v12

已使用固定版本 Blender Rock Generator，接入本地入口 `C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3/Tools/CoastArtV12/Rebuild-ReefArt.ps1`。先读 `C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v12/reference-analysis.md` 与 Tools/CoastArtV12/README-tools.md。39 处高礁采用生成主体加单层扫描岩冠，近岸仍用扫描石；统一折射焦散，浅水改为青蓝。六种模型各 1600 面，0 警告、0 错误。最终实机原生1080p固定镜头1200帧平均 86.22 FPS，航行、步行、上下船、穿洞通过。证据为 Verification/coast-art-v12/result.json，桌面入口“深海灯塔-海岸美术-v12”。

继续改进需优先解决连续礁台/海床过渡、珊瑚群落、灯塔码头与前景；尚未达到原图质量。绿色海带、原玩法与物理地形继续保留。不要使用初稿或将 v12 当成已匹配原图。构建入口仍为 Deepsea.Editor.CoastMaterialsBuild.Build，输出 Builds/CoastArtV12/GuichaoBay-ArtV12.exe；独立程序要用可见窗口截图，Hidden 会黑帧。历史 v11 程序与备份已保留。

Rock Generator 默认同步 Tools/CoastArtV12/accepted 冻结资产，-Regenerate 才生成新稿。上游生成及几何处理未通过逐字节复现，生成新稿后要重验，不能当成相同已验收网格。

## 2026-10-23 当前版本 v13

最新报告：C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v13/reference-analysis.md。连续礁台8块共35044面，5种珊瑚模型5020面，最终512丛；98丛绿色海带；远沟108条小银鱼，总鱼数196。木板外观与灯具已补充，塔身增加风化。新旧海床使用覆盖遮罩在颜色/深度/阴影通道一致裁切；植物查可见网格。原物理地形、船控、步行、存档未改。

原生1080p新展示镜头1200帧平均 70.97 FPS，P95 16.09ms；航行、上下船、12点步行、穿洞和31360次可见海床鱼群路线检查通过。展示镜头相对v12偏移(1.8,-.5,-2.8)，严格前后对照用 reference-baseline.png。最新入口为桌面“深海灯塔-海岸美术-v13”。构建仍用 Deepsea.Editor.CoastMaterialsBuild.Build，输出 Builds/CoastArtV13/GuichaoBay-ArtV13.exe；独立验收要使用可见窗口，参数 -coast-materials -coast-ecology -whitebox-check。

GitHub：复用已存在 FastNoiseLite 1.1.1（同提交785f37a），珊瑚生长参考 NejcHirci/procedural-corals（25e89f9，MIT），保留源码/许可。工具入口 Tools/CoastArtV13/Rebuild-CoastArt.ps1：默认同步已验收珊瑚，-Build 构建，-GenerateCorals 产生需重验的新稿。海床C#配方在 CoastShelfShape/CoastShelfBuild，GLB海床是几何查看档，精确引擎数据在 ShelfMeshes.json。

仍须改善：岸脚硬边、陡壁破碎、浅水反射/浪沫、塔身和码头细节、暖冷光焦点。不要声称已达到原图。保留旧版程序和备份，中间失败稿不作为结果。

## 2026-10-23 用户对 v13 的反馈

用户指出与原图仍差距很大，小改动缺少整体提升；上一版本水面可以，v13 水面变差。v12/v13 同镜头截图与源码已核对：v13 浅水偏灰发白，水红色吸收 .95→.28、折射 .0022→.008、波纹斜率增强，同时替换可见海床材质和几何。将 v12 水面作为用户认可的保留基线，后续需按同镜头实机对照恢复，并单独适配新海床。此记录时尚未执行回退。

v13 工程检查通过不等于美术获认可。后续优先构图、地形大形、整体光照与材质统一，不能继续用装饰数量和工具接入代替可见的画质进步。


## 2026-10-24 当前 v14：基础模型与材质重做

用户要求停止堆装饰，先修基础。已恢复 v12 水面核心着色器与 .0022 折射；收回浅滩覆盖并统一新旧海床材质。新灯塔 19320 面、8 材质组、真实拱窗门洞；分层裂隙礁面 36292 面；原地形只调整渲染法线。珊瑚实际 511、上限512，干净绿色海带重采样后108，没有新增种类或投放尝试次数。

最新报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v14/reference-analysis.md。独立程序验收通过，原生1080p固定镜头1200帧平均 76.28 FPS，P95 14.66ms；31360次可见礁面路线检查无阻挡，航行、上下船、12点步行和穿洞通过。灯塔网格0警告0错误，灯塔/礁面GLB回读通过。原物理地形与核心玩法源码不变。

最新入口桌面“深海灯塔-基础美术-v14”；构建仍用 Deepsea.Editor.CoastMaterialsBuild.Build，输出 Builds/CoastArtV14/GuichaoBay-ArtV14.exe。工具 Tools/CoastArtV14/Rebuild-CoastFoundation.ps1 默认同步冻结模型，-GenerateLighthouse 仅生成需重验的新稿（UV存在约2.39e-7的浮点尾差，不承诺逐字节复现）。水面不得再次随意改动。

整景仍低于原图，用户尚未认可 v14 美术。下一步基础问题为岸线粗硬边、扫描岩体重复、步道与岸脚接合、礁壁过于陡直和整体主光层次；不要又转去增加装饰数量。失败验收和错误玻璃截图不是交付结果。


## 2026-10-24 当前 v15：岸线、石阶与礁面

最新报告 C:\Users\许嘉乐\Documents\ChatGPT\深海灯塔\art-v15\reference-analysis.md。可见岸脚细分至0.325m，299968面；原物理地形仍74992面，陆上步行带采样高度改动0。礁面8块36292面，重新设计宽肩、分段缓降、鞍部和岩层，陡面面积占比44.5%→31.1%（几何统计，不是审美评分）。石阶替换外观、地被色调和主光与环境光调整。坡壁材质试验因浅色平面问题已撤回，最终保留原材质结构。保留v12水核心哈希、0.0022折射与干净绿色海带，没有新增装饰种类或投放尝试。

最终原生1080p固定镜头1200帧平均72.66 FPS，P95 16.59ms；航行、上下船、12点往返步行、穿洞、31360次可见礁面鱼群检查通过。新地形/礁面GLB回读336260面、0错误；8文件源码配方在临时目录重建SHA256一致。冻结入口 Tools/CoastArtV15/Rebuild-CoastLandform.ps1（默认同步，-Build构建），程序 Builds/CoastArtV15/GuichaoBay-ArtV15.exe，桌面“深海灯塔-岸线基础-v15”。v14与修改前备份保留。

仍明显低于原图，用户尚未认可。下一步应逐段塑造礁坡大岩块/凹口，改善扫描岸石重复、洞口大平面、码头结构和前景光影。不得把陡面比例、帧率或数量当作画面质量认可。中间草稿、浅色三角条纹稿和浅色坡面稿不作交付。

## 2026-10-24 v16 独立三维美术样板（原可玩版仍为v15）

用户要求整体提升并授权“请制作”，已完成独立新场景/程序，不等于原游戏已升级到v16。报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v16/reference-analysis.md；桌面入口“深海灯塔-独立美术样板-v16”。场景 Assets/Deepsea/Scenes/CoastArtShowcase.unity；构建入口 Deepsea.Editor.CoastShowcaseBuild.Build；程序 Builds/CoastArtV16/MistCape-ArtSample.exe。默认观察场景，不加载原航行、步行、穿洞和存档玩法。原v15程序保留。

新地形、扫描岩群、加宽转折礁台、码头/步道、松树；生成松针、木纹、珊瑚组织3张1254平方贴图。实际738144面、63网格组、608755索引顶点，311珊瑚、63干净绿色长带海带、178鱼。海带复用原网格/材质，v12水面核心与.0022折射未改；新海床和光照改变水面观感，不能称为逐像素相同。

最终可见窗口原生1080p，1200帧固定全景平均64.41FPS，P95 16.28ms；证据 Verification/coast-art-v16-final/result.json。6视角无黑帧/粉色/运行错误，水下远景边界已用一致的深水雾处理。28480次离线实际求值网格鱼群检查无阻挡。Blender0失败393警告（共点/分缝、封口多边形），GLB回读738144面0错误5张嵌入图。不要写成0警告、原玩法测试通过或用户认可美术。

329个原脚本/着色器与制作前备份逐字节一致。冻结9输入入口 Tools/CoastArtV16/Rebuild-ArtSample.ps1，默认恢复SHA256已检查输入，-Build构建，-GenerateGeometry才生成需重验新稿，不承诺生成器逐字节复现。Blender源文件只含新环境；灯塔、船、人、水与精确光照在Unity可编辑场景。GLB仅几何/部分材质查看档，整体抬高17.871675m，坐标见清单；Unity JSON无偏移。

样板仍明显低于原图，用户尚未认可；扫描岩石重复、岸脚接合、松树卡片感、地面空平、浅水焦散均匀、灯塔码头完成度和盘状珊瑚偏暗仍需改善。后续优先处理这些大形、基础资产和明暗组织；不要把新增数量、工具接入或技术检查当作质变证明。

## 2026-10-24 用户认可v16制作方向

用户实看后反馈“确实改善了很多，这个方向是对的”，并询问灯塔和岸石贴图。认可的是改善与方向，未表示已匹配原图。以后以v16独立样板构图继续；保留其水面核心与绿色海带。灯塔已有2K灰泥颜色/法线/ARM贴图，岸石已有扫描颜色/法线贴图、粗糙度目前统一.84；下一步应做有针对性的分层材质和几何接合。详细记录 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v16/user-feedback.md。本次问答未修改运行材质或构建。

## 2026-10-24 v17：认可方向上的灯塔和局部岸石精修

用户认可v16“改善很多、方向正确”，授权先做灯塔与旁侧一组岸石。v17仍是独立美术样板，原可玩版v15与美术样板v16均保留。报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v17/reference-analysis.md。桌面“深海灯塔-灯塔岸石精修-v17”；程序 Builds/CoastArtV17/MistCape-Materials.exe，场景 CoastArtRefined.unity，构建 Deepsea.Editor.CoastRefinementBuild.Build。

新灰白灰泥贴图1254平方，局部基座露石/窗下雨痕/轻微金属风化。43块真实基座石，4块岸石干湿/粗糙度/少量苔藓，359个底部顶点最大调整.08558m，3处干地接合补片；水下环状失败补片已去除。构图、太阳、后期、水核心、礁体和绿色海带不变。F7在同镜头切换真实v16/v17环境与塔材质，5/6为灯塔/岸石近景。

最终可见窗口原生1080p固定镜头1200帧平均62.21FPS，P95 18.84ms。环境743425面68组618510索引顶点；较v16增加5281面/5组。岸石/基座源档149281面，模型0警告0失败，GLB回读面数一致0错误；不把整个旧场景历史警告报成0。339旧脚本/着色器、v16冻结9输入和58项其他环境网格逐字节一致。新冻结入口Tools/CoastArtV17/Rebuild-MaterialStudy.ps1默认恢复6输入且已测试，-Build构建，-GenerateDraft为需重验新稿。完整源档含岸石/基座与灯塔两份Blender文件，精确风化在Unity。

未集成原航行/步行/存档，没有新玩法通过声明；本轮未重测沿用的鱼群路线。v17尚未由用户评价，仍有扫描岩形重复、岩脚硬边、门窗简化与植被卡片感，不能称为已匹配原图。后续在此方向继续，不退回旧构图和水面。

## 当前 v20：局部岸岩与独立水面碎光对照

用户授权“直接精做一段岸岩并导入实机对照”。报告 `art-v20/reference-analysis.md`；桌面“深海灯塔-岸岩精修-v20”，程序 `Builds/CoastArtV20/MistCape-FineShore.exe`，场景 `CoastArtFineShore.unity`，构建 `Deepsea.Editor.CoastFineShoreBuild.Build`。仍是独立样板，原可玩v15、v19样板均保留，未整合原玩法。

只替换木阶右侧Headland03/Foot03，使用397472面高模、6656面LOD0、3992面LOD1及四张2048烘焙图。法线是真实高低模投射，AO为隔离低模几何烘焙；底色复用v19图投射，粗糙度分层。Tree00抬高1.6267566m并重建7根，其他树不变。6看局部，F7真v19/v20对照，F9独立切碎光，F8暂停落叶。

原水核心SHA256仍4a7fdfa9164a208a8136c192cb3284ea335d2b60e5ec03f7fe7a652ac02c015c、.0022折射与水材质原文件未改；新增克隆水材质与独立shader，用真实太阳方向的统计微面元补充动态高光。实际参数2.4/8/.52/.9/.25/.6（强度/尺度/分布/密度/覆盖/速度），无全局太阳或曝光改动。首稿过稀的碎光和偏规整白亮的岩稿不是最终结果。

最终证据 `Verification/coast-art-v20-delivery/`，由已复核review2原始采集复制，未重复测试。原生1080p岸边1200帧平均65.4819FPS、P95 16.6558ms；关闭碎光另1200帧66.0107FPS、P95 16.6347ms，两时窗差值不等于精确shader耗时。12600落叶状态、7230新岩体空气路径、4200水面漂流、28480鱼群路线检查通过。局部模型0失败、1项3个0.1mm内近点提示；Tree00两网格0失败0提示；GLB回读6656面0错误、3嵌入图（含合并AO/粗糙度）。GLB抬高1.1088836m，blend/JSON无偏移。

184项旧脚本/着色器/水材质/v19场景与v19冻结14输入哈希一致；场景检查121对象、107网格、113材质槽及其余锚点/目标通过。QualitySettings/ProjectSettings不变，GraphicsSettings仅保留2个新增shader引用。工具 `Tools/CoastArtV20/Rebuild-FineShore.ps1` 默认校验恢复11冻结输入，-Build构建；已实测默认恢复。新生成需重验，不承诺跨环境逐字节生成。可编辑源、同镜头对照、12秒实际144帧/12fps视频均在art-v20。

仍有部分直线断缝、大平面和拼装感；其余海岸未精修，整体仍明显低于概念图。碎光方向与强度尚待用户实看反馈，不能把检查通过或帧率当作美术认可。SSAO逐帧噪点改变，前后截图仅同镜头/同水波时间，不声称逐像素相同。

## 2026-10-28 v21：海湾生活首轮可玩接入

用户认可分镜效果并授权尝试接入现有美术。现已在v20红枫海岬基础上新增独立入口，报告 `art-v21/reference-analysis.md`。桌面“深海灯塔-海湾生活-v21”，程序 `Builds/SeaLifeV21/Guichao-SeaLife.exe`，场景 `CoastSeaLife.unity`，构建 `Deepsea.Editor.SeaLifeBuild.Build`。旧v15程序与v20美术样板均保留；不加载旧GameDirector，不恢复旧救援/建设链。

本轮可玩：独立船控与上下船/步行、三处鱼区投钩/浮漂/提竿/收线张力、保留或放流、潮池四个拾贝点、固定吊箱船锚、小馆步行房间和三个固定展缸位、放鱼/取回/近景观赏、出售与独立存档。新鱼/缸体/房间是可编辑C#配方原型资产，未做城镇经营、自由缸体编辑、多岛地图或完整生态。继续保留红枫、水面核心/水材质和干净绿色海带；新跟随镜头与景观鱼表现不等于v20逐像素相同。

最终仅用 `Verification/sea-life-v21-delivery/`（副本 `art-v21/runtime/delivery/`）与 `art-v21/runtime/review-06.log`，73项检查通过；真实控制器步行路线、潮池接地、钓鱼/打捞/建缸/收藏唯一性/容量/金币不足/存档回读/720p按钮边界通过，运行错误0警告0，不声称全工程0警告。原生1080p固定镜头600帧、锁60FPS平均59.9867FPS/P95 16.6759ms，不能直接比较旧版不限帧采样。部分起点由自动验收设置；额外展鱼和容量鱼是明确临时测试样本，不进正常游玩存档。实际窗口另抽查E取竿、鼠标投钩、咬钩和E提竿；检测到用户输入后停止追加自动输入，没有人工完整收线验收声明。

170项旧文件哈希一致；冻结33项本轮输入在 `Tools/SeaLifeV21/accepted`，`Rebuild-SeaLife.ps1` 默认恢复/校验已实测，-VerifyOnly只检查，-Build构建；依赖原v20资产，不是全工程备份或逐字节构建复现承诺。Noto Sans SC 2.002字体元数据、SHA与OFL许可随包保存。独立视觉judge因账户额度失败，没有独立结论或评分；限定UI自查为provisional，不代替三维美术验收。用户尚未评价v21实机；新房间、缸内水光和鱼类细节仍需精修。



## 2026-10-28 v22：水面首轮修订
用户选择先处理水面后已完成独立版。报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v22/reference-analysis.md，桌面“深海灯塔-水面修订-v22”，程序 Builds/SeaLifeWaterV22/Guichao-SeaWater.exe，场景 CoastSeaLifeWaterV22.unity。F7原/新水面对照；窗口仍沿用v21产品名和正常存档目录。移除新shader的随机碎光层，新增34869顶点/68992面缓浪网格、船身视觉响应、历史尾迹与浮漂/落叶高度接入，原控制器和碰撞不变。
最终 Verification/sea-water-v22-review03/ 与 delivery 副本，38项检查通过；原生1080p锁60固定镜头600帧59.98834FPS/P95 16.67730ms，运行0错误0警告，不代表全工程或全地图。旧源/水材质/旧场景/设置清单199项哈希一致。Tools/SeaLifeWaterV22/Rebuild-Water.ps1默认只校验23项冻结输入，不自动恢复覆盖；-Build使用保存好的v22场景，显式Prepare才重新生成。前两轮尾迹草稿不作交付。浅水搁浅、物理水阻/划桨、UI、生态与钓鱼动作未重做，用户尚未评价新水面观感。具体边界以报告和工具说明为准。

## 2026-10-28 Git仓库与第三方内容实际保存

桌面实际Unity工程已初始化Git主仓库（main）。五个既有上游工具采用固定提交子模块保存，来源在.gitmodules；三项UPM本地引用保持原路径。BoatAttack、Tiled Directional Flow、FastNoiseLite、NVJOB、Lucide的实际文件和许可已纳入主仓库，不把保留参考等同于当前场景启用。

Tools/AgentSkillSources保存五个已有美术/UI工具的185个文件，来源/版本/文件SHA256在sources.lock.json；原用户技能安装未改。上游.gitattributes以.upstream归档，映射可还原。Git属性保留原文件字节，大资产使用LFS，ProjectSettings保持普通可读文本。

本轮仓库检查57项通过，Unity依赖检查21项通过、当前v22场景221项引用可解析。本机从提交435e3be克隆主仓库、恢复五个子仓库与LFS、核对源码和工具快照通过；证据Verification/repository-integration/。这不代表新增船控/水面功能或v23实机验收。v23同时在制作，整理期间出现的后续改动须按git status另行核对，不能当成已验收稳定版。

远端尚未配置，GitHub推送未完成；需要明确目标仓库及可写连接后再推送，不能把本地提交称为GitHub已保存。详情Docs/GitHub内容接入-20261028.md。新增整理脚本是本轮维护入口，日常只读检查用Tools/RepositoryIntegration/verify_repository.py；不要反复执行一次性导入脚本覆盖档案。

## 2026-10-28 v23：GitHub波浪与惯性船控
用户授权查找GitHub水面流动和船控实现并直接应用，不兼容部分自行实现。已在v22基线接入Boat Attack Gerstner公式与Habrador水阻公式，固定提交、原始源码及许可见Tools/SeaLifeNavigationV23/ThirdParty。UPM/URP版本未改；推进、制动、舵向平滑、转向惯性、随波船身与历史尾迹为适配实现。
最新报告C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v23/reference-analysis.md。桌面“深海灯塔-流动水面与船控-v23”和“深海灯塔-v23航行演示”。程序Builds/SeaLifeNavigationV23/Guichao-SeaNavigation.exe；场景CoastSeaLifeNavigationV23.unity；构建Deepsea.Editor.SeaLifeNavigationBuildV23.Build。原v22程序保留，产品名/存档沿用v21。W推进、松键滑行、S制动倒车、A/D平滑转向；F7为v22水材质/原推进算法对照，共享新碰撞/尾迹/视觉层，不是完整旧版二进制还原。
最终Verification/sea-navigation-v23-review03，46项自动检查通过，运行0错误0警告；1080p锁60固定600帧帧率和P95见最终报告。实际引擎144帧/12fps演示，不是实时性能证据。57项旧文件清单中，仅SeaLifeRuntime.cs增加一处FixedUpdate路由，其余56哈希一致；不是全工程所有资产哈希声明。build-04成功，有20项构建警告，不报全工程无警告。工具Rebuild-Navigation.ps1默认只核对冻结输入，不自动覆盖；accepted依赖旧环境，非全工程备份。显式Prepare会从v22重新生成v23场景。
已测人工测试障碍阻挡与倒退脱离，没有全海湾浅水逐段验收；UI、生态群落与钓鱼人物动作未重做，没有完整人工钓鱼/水族馆回归或用户已认可美术的声明。

## 2026-10-28 v24：海面层次与性能样板
用户明确否定v23的波浪滚动感、海面/水下层次与性能。本轮独立v24样板以海面为主、浅水隐现为目标，增加方向性滚动波、深度/朝向/浪峰控制的可见度、青蓝反光。旧美术/干净绿色海带保留，用户尚未评价本轮效果；不将检查通过当作美术认可。
桌面“深海灯塔-海面层次与性能-v24”和“深海灯塔-v24海浪演示”。报告C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v24/reference-analysis.md；程序Builds/SeaSurfaceV24/Guichao-SeaSurface.exe；场景CoastSeaSurfaceV24.unity；默认构建Deepsea.Editor.SeaSurfaceBuildV24.Build。F7对照水面，F9帧率。正常产品名/存档沿用v21。
实际取消限帧，原v23状态同镜头1080p58.53–65.03FPS；v24最终147.55–148.92FPS，主画面renderScale=1。FXAA替4倍MSAA、半分辨率折射颜色副本、水下细节分级和单次接触阴影滤波。保留阴影与原AO采样精度/强度；画面存在抗锯齿和水下细节取舍。61项原输入清单只允许v23主类两个可选钩子变化，其余60项哈希一致；不是全工程哈希声明。
4K窗口请求被限制为2880x1800，采用3840x2160离屏场景加本机窗口呈现，B10G11R11_UFloatPack32、renderScale=1；最终约56.42FPS/P95 19.35ms，未达到稳定60或4K高刷，不称为4K显示器原生测试。最终证据Verification/sea-surface-v24-review05和sea-surface-v24-4k-review05；32项1080p检查通过，两次运行0错误0警告，build-05成功但仍有历史构建警告。
视频8秒240张1080p引擎帧按30fps固定步长录制，交付缩为720p；不是实时录屏或性能证据。测试使用唯一临时存档。未做全海湾浅水/全地图长期性能、完整人工钓鱼/水族馆回归，UI/生态/钓鱼人物动作未重做。
新增四份水下材质shader与运行时克隆，原材质/shader不改；0.25–0.85m水深渐变降低法线/遮罩/BRDF细节，岸上及水下相机保留完整路径。新增源码/Shader/资源在SeaSurfaceV24目录；v23主类只加波形采样和F7两个可选钩子，备份Tools/SeaSurfaceV24/Before/SeaLifeNavigationV23.cs。构建使用已验证Unity方法，冻结清单用于核对，不自动恢复覆盖；显式Prepare/TuneAndBuild/PrepareBudgetAndBuild属于生成步骤，会覆盖对应v24资源。accepted/源码包依赖既有v23环境，并非完整工程。
