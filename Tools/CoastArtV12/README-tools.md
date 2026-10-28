# 已接入的美术工具

Blender 官方旧版插件集中的 Rock Generator 已固定到提交 `b1fbf73a08cfb68b14bb328590a9bb6602d96916`，来源：
https://github.com/blender/blender-addons/tree/b1fbf73a08cfb68b14bb328590a9bb6602d96916/add_mesh_extra_objects/add_mesh_rocks

这不是声称上游原版支持 Blender 5.2。保留原始 `rockgen.upstream.py`，本地副本只做了两处兼容适配：旧边折痕接口改为 crease_edge 属性；旧法线操作替换，最终由 bmesh 重新计算法线。工具源码采用 GPL-2.0-or-later，完整许可和作者署名在 tools/ 中。

工具源码放在 Unity 项目的 Tools/CoastArtV12 下，不进入游戏运行时代码。Codex 可直接运行该目录中的 Rebuild-ReefArt.ps1，默认将 accepted 中冻结的已验收模型和焦散同步给 Unity；加 -Build 构建独立程序。加 -Regenerate 才会调用生成器产生新变体并同步，新变体需重新验收。参数允许指定本机 Blender、Python 路径，Python 需要 NumPy/Pillow。

实际重生成检查中，旧生成器加本地几何处理没有通过逐字节复现；不声称仅凭固定种子能还原相同网格。因此当前版本以已验收的顶点 JSON、GLB 和 Blender 文件为准，默认构建读取冻结输出，生成新稿不会改写 accepted 目录。

交付包含可编辑 .blend、GLB、精确顶点 JSON、1600 面预算审计、检查图。尺寸和背面均依据单张透视原画推断，不代表精确重建原图。

本轮亦评估了 versluis/Rock-Generator（旧 2.8 版本的整理仓库）与 daniel-ilett/water-urp（Unity 2019.3/URP7 示例），未将它们整包导入当前 URP14 工程。水体改动在现有工程内实现。
