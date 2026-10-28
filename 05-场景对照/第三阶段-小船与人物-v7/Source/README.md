# 深海灯塔 · 小船与渔人外观 v7

这是 Unity 2022.3 / URP 14 现有工程的源码补丁与实机验证记录，不是完整工程。基于已验证的 v6，在原玩法上替换小船与人物外观。第三阶段整体美术仍在制作，参考图效果尚未全部达成。

## 本轮实现

- 木船采用 Innerscene 原创 CC0 Rowing boat：保留模型 UV、嵌入木纹与其他材质纹理，以及船肋、底板、座板、桨架、收起的双桨和金属配件。原 77 个部分按 7 种材质合并，并从约 10.1 万个三角形减到 1.8 万个，保留原 UV；船壳为暗青涂装，内部保留木纹。
- 船模按已验收外观尺寸适配，船只控制器、碰撞体、刚体、水面起伏和航线均沿用原实现。
- 新制带骨骼的渔人外观：有帽檐、帽冠、外套、口袋、背包、手臂、裤腿和靴子；衣料使用 Poly Haven 的 CC0 Rough Linen 1K 漫反射与法线贴图。
- 船上坐姿与上岸步行共用人物模型；上岸时隐藏船上人物；步行摆臂和腿部动作跟随实际移动速度。这是轻量关节动画，尚未具备完整的成品角色动画、面部或手部细节。
- 外观为可切换的附加层。F4 恢复已验收白模；不向新模型添加碰撞体。

## 安装和构建

在已有 v6 工程中关闭 Unity 编辑器，运行 `Install-CoastArtV7.ps1`，然后运行 `Build-CoastArt.ps1`。需要可运行的 `python` 命令，GLB 转换只使用 Python 标准库。安装脚本先核对源码哈希并备份，再修改项目。

构建脚本从官方来源下载并校验木船 GLB、布料贴图和沿用的岩石/墙面贴图；不需要手动安装 glTF 导入插件。`Convert-Boat.py` 烘焙 GLB 节点变换、转换坐标并保留 KHR_texture_transform，随后编辑器生成 Unity 网格、材质、骨骼和 prefab。

构建入口为 `Deepsea.Editor.CoastMaterialsBuild.Build`。不要调用会重建已验收关卡的 `CoastBlockoutBuild.Build`。

输出：`Builds/CoastArtV7/GuichaoBay-ArtV7.exe`

启动参数：`-coast-materials -coast-ecology -screen-fullscreen 0 -screen-width 1920 -screen-height 1080`

WASD 移动/航行，F 上下船，F4 白模对照，F2 生态，F9 水面。高分辨率窗口继续将三维渲染像素预算限制到约 1080p。

## 验证

`evidence/result.json` 记录原生 1920×1080、renderScale=1、固定参考镜头、1200 帧无上限采样结果。截图编码不计入性能采样。航行和步行验证恢复实际游玩 60 帧上限。此数据不是全地图最低帧率承诺。

新增检查涵盖新模型材质槽未被旧外观逻辑覆盖、船模外观尺寸、新增碰撞体数量、上岸后船上人物隐藏和 F4 白模切换；继续检查原航行、12 个步行点、上下船、鱼群路径和 334 个已验收文件。`Verify-Final.ps1` 复核生成后的岩石外观是否侵入鱼群通道。实机截图代表当前版本，参考图仅代表美术目标。

## 来源

- 木船：https://www.innerscene.com/tools/library/3d-parts/rowing-boat-d14b7296 ，Innerscene 原创，CC0。
- 衣料：https://polyhaven.com/a/rough_linen ，CC0。
- GLB SHA256 和贴图来源/校验值保存在 Unity 工程 `Assets/Deepsea/ThirdParty/CoastArtV7` 的 source-record.json、cloth-sources.json 与 SOURCE-LICENSE.txt，并随播放器保留。

本源码包不捆绑第三方原始大文件，构建时从官方来源获取。人物网格和附加外观代码随本包提供。
