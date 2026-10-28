# 海岸美术 v13 工具入口

本轮没有增加常驻服务或需要账户的生成服务，直接复用本机 Blender 和 Unity。

- [FastNoiseLite](https://github.com/Auburn/FastNoiseLite)：原工程已安装 1.1.1，固定提交 785f37a9ad76e283586a379675085f2063ae03f7。本轮验证与上游源码一致（原文件多一个末尾换行），复用它生成连续礁台的边缘侵蚀。主体宽肩、坡面、岩洞开口是本项目设计的剖面。源码和许可在 Assets/Deepsea/ThirdParty/FastNoiseLite。
- [Procedural Coral Generator](https://github.com/NejcHirci/procedural-corals)：固定提交 25e89f97c2f9639b7a3de60406d2d293058857d2，MIT。参考其吸引点最近分枝、生长方向与消耗吸引点的逻辑，移植到 Blender Python；保留上游源码和版权，模型管线、固定种子、网格制作及配色为本地适配。没有整包安装 Three.js 网页应用。

工程 Tools/CoastArtV13/Rebuild-CoastArt.ps1 默认将 accepted 中的珊瑚 JSON 同步给 Unity。-Build 会调用 Deepsea.Editor.CoastMaterialsBuild.Build，生成 v13 独立程序，并由 C# 源码重建连续礁台。-GenerateCorals 调用 Blender 产生新稿；accepted 保持原有验收输出，新稿需要重新验收。

五种珊瑚原文件为 EN_CoralHabitat_master.blend；交付副本 EN_CoralHabitat_delivery.blend 将颜色转为线性顶点属性，可正确导出 GLB。Unity JSON 保留 sRGB 作者颜色，在游戏着色器内解码。不要把两种颜色空间混用。

连续海床的精确引擎顶点档为 ShelfMeshes.json；EN_ContinuousShelf_master.blend 是它的可编辑几何查看档，坐标换算记录在 shelf-coordinate-contract.json。资源为外观层，不是原物理地形。海床覆盖遮罩在颜色、深度和阴影通道都裁去重叠原面，灰模对照时恢复原地形。

展示镜头偏移为世界坐标 (1.8,-0.5,-2.8)。最终 reference.png 是调整后的构图；reference-baseline.png 恢复 v12 原镜头，用于严格前后对照。展示镜头变化不改变物理路线。

原船控、步行、上下船、地形碰撞、存档代码与场景保持原版。绿色海带保留。精确测试结果与剩余差距见本轮对照报告；没有通用审美分数。

本机已复跑 v13 珊瑚生成器，输出 JSON 与冻结验收文件 SHA-256 完全一致；默认同步入口也已检查不改变几何。该结果适用于当前代码和本机运行环境。
