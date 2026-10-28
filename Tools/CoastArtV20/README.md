# v20 冻结输入

默认运行 Rebuild-FineShore.ps1 校验并恢复11项已验证输入，-Build额外构建独立程序。不会重新生成模型或贴图。

程序：Builds/CoastArtV20/MistCape-FineShore.exe。构建入口：Deepsea.Editor.CoastFineShoreBuild.Build。

原可玩v15、v19场景与程序保留；此入口只生成独立美术样板。6看精修岸段，F7对照v19，F9切换水面碎光，F8暂停落叶。

可编辑Blender与完整制作记录：C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v20/reference-analysis.md。

四图：2048平方PNG，底色sRGB；normal/AO/roughness为线性。Unity法线按NormalMap导入，其余按相应色彩空间导入，CompressedHQ、mipmap与Trilinear。引擎几何JSON无坐标偏移；GLB仅本轮局部查看档，垂直抬高1.1088836m。

运行验收使用可见窗口，工作目录为工程根目录，参数-showcase-check -review-tag自选名称。最终证据为Verification/coast-art-v20-delivery；旧草稿不作最终结果。

重做资产后必须重新检查，不承诺生成过程逐字节复现。当前仍有直缝与拼装感，未获用户美术认可。
