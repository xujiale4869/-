# 深海灯塔 · 岸上地表与植被 v8

本包是既有 Unity 2022.3 / URP 14 工程的源码补丁及实机验证记录，不是完整工程。基于已验证的 v7，制作岸上草地、泥土、路径和岩石顶部的材质衔接，并补充低草与灌木。第三阶段整体美术仍在制作，参考图效果尚未全部达成。

## 本轮实现

- 岸上地表使用 Grass Path 2 的 1K 漫反射、OpenGL 法线和 ARM 贴图，并混合项目原有 Forest Ground 04 土壤贴图。海底沙地、水面、船与人物沿用 v7。
- 512×512 线性 RGBA 控制图根据原岸线、原步行路线、灯塔前空地和多尺度噪声绘制草地覆盖、磨损路径及植被密度。相同控制函数用于植被摆放，避免地表和植株互不衔接。
- 岩石上表面接入同一地表控制和草地贴图，垂直岩面继续保留原扫描岩石 UV 和纹理。
- 新增 Poly Haven 的 CC0 Grass Medium 01 和 Shrub 02。保留原 UV、漫反射与透明轮廓，选用低细节网格并减面，以实例化方式渲染；低草和灌木不额外投射阴影。
- 将原有蕨类制作成独立的减面外观资源，保持原始资源和摆放不变，给新增植被留出渲染预算。
- 新植被通过实际关卡碰撞表面定位，过滤过陡坡面、海面、完整步行通道及灯塔门前空地。没有添加碰撞体或修改已验收地形。

## 操作与对照

桌面入口仍为“深海灯塔－材质光照制作版”。

WASD 移动/航行，F 上下船；F3 开关本轮地表和新增植被，F4 白模对照，F2 生态，F9 水面。F3 对照仍使用同一版本的减面蕨类；原 v7 独立播放器保留用于严格跨版本对照。

## 安装和构建

在已验证的 v7 工程中关闭 Unity 编辑器，运行 `Install-CoastArtV8.ps1`，然后运行 `Build-CoastArt.ps1`。需要可运行的 `python` 命令，以重建沿用的船模转换数据。

脚本从官方来源获取并校验所需模型和贴图。新增资产使用 FBX 和独立贴图，不需要新增 Unity 插件。源码安装前先核对哈希并备份，包括原 CoastMaterials、CoastHero 和已有 CoastHabitat 资源。

构建入口为 `Deepsea.Editor.CoastMaterialsBuild.Build`。不要调用会重建已验收关卡的 `CoastBlockoutBuild.Build`。

输出：`Builds/CoastArtV8/GuichaoBay-ArtV8.exe`

运行参数：`-coast-materials -coast-ecology -screen-fullscreen 0 -screen-width 1920 -screen-height 1080`

高分辨率窗口继续将三维渲染像素预算限制到约 1080p。

## 验证与范围

`evidence/result.json` 记录原生 1920×1080、renderScale=1、固定参考镜头、1200 帧无上限采样结果。截图编码不计入性能采样；航行、步行和上下船恢复实际游玩 60 帧上限。该数据不是全地图最低帧率承诺。

继续检查原航行、12 个步行路径点、上下船、白模切换、船与人物材质、鱼群穿洞及 334 个已验收文件；新增检查包括岸上植被数量、路径净空和蕨类网格预算。`habitat-assets.json` 记录实际选用的模型、减面前后面数和尺寸。实机图代表当前版本，参考图仅作为美术目标。

## 来源

- https://polyhaven.com/a/grass_medium_01 ，CC0，低草模型及漫反射/Alpha。
- https://polyhaven.com/a/shrub_02 ，CC0，灌木模型及漫反射/Alpha。
- https://polyhaven.com/a/grass_path_2 ，CC0，地表漫反射、法线与 ARM。

原始下载地址、校验值和许可记录保存在项目 `Assets/Deepsea/ThirdParty/CoastArtV8`，也随播放器保留。原有扫描岩石、树木、蕨类、土壤、船与衣料沿用项目已有许可记录。

本包不捆绑第三方原始大文件，构建时按记录从官方来源获取。人物造型、海底植物和整体光照仍有继续统一和精修的空间。
