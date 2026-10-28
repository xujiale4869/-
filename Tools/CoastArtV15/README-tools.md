# v15 工具入口

实际入口：C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3\Tools\CoastArtV15\Rebuild-CoastLandform.ps1。

默认同步已验收的本地美术源码、8块礁面、遮罩及相关材质，每个文件检查SHA256。加 `-Build` 构建当前v15；构建不等于美术已获认可。旧版物理层和控制器不属于同步清单。

修改前v14完整备份在 Tools/CoastArtV15/Before-20261024。v14、v13、v12程序保留。

源码配方按 build/01_foundation.py、02_refine.py、03_geology.py 顺序执行，01从v14备份重新开始。支持COAST_PROJECT_ROOT指向独立草稿目录；review/recipe-check.json已验证8文件与当前源代码逐字节一致。普通操作使用冻结同步，无须重新运行配方。04_archive.py将实际引擎JSON生成Blender档和GLB；05_review.py生成真实截图联系表。它们的工作目录是各自父目录对应的art-v15档案。

执行程序必须使用可见窗口。完整检查参数：-coast-materials -coast-ecology -whitebox-check。生成新稿后需要重新检查。最终证据目录 Verification/coast-art-v15；中间草稿有单独标签，不作为交付图。

GLB和Blender是几何查看档，预览材质为中性灰；运行时仍使用Unity世界坐标投射的沙石/植被材质。坐标换算及最低点偏移在exports/asset-manifest.json，运行时碰撞不从GLB重建。
