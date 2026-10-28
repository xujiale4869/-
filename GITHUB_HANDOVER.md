# 深海灯塔 GitHub 交接

目标仓库：https://github.com/xujiale4869/- （私有）

## 分支与版本

| 分支 | 内容 |
| --- | --- |
| main | 既有 Unity 工程历史及 v11–v24 等场景、资源、源码与制作工具 |
| archive/unity-v0.1 | 早期 Unity 浅海工程快照 |
| archive/unity-v0.2 | 早期 Unity 海陆工程快照 |
| archive/lanternward-v0.1 | 早期独立原型源码 |
| archive/desktop-history | 历史目录中已展开的可编辑源码、资源与文档 |

v0.3 是现有工程目录名称，并非用户选定的最终游戏版本。各归档分支为整理日磁盘快照，不表示每个历史版本均有独立提交，也不替用户决定开发基线。

## 取回工程

本机安装 Git、Git LFS 以及 ProjectSettings/ProjectVersion.txt 指定的 Unity 编辑器后，在新目录执行：

```sh
git lfs install
git clone --recurse-submodules https://github.com/xujiale4869/-.git deepsea-lighthouse
cd deepsea-lighthouse
git lfs pull
```

私有仓库需要账号权限。若另一个 AI 无法读取私有仓库，应先配置其仓库访问连接。
查看早期工程时，请在新的克隆目录切换对应 archive 分支并执行 git lfs pull，避免覆盖当前工作目录。

## 协作入口

先读 AGENTS.md（现有工程状态和限制）、ARCHITECTURE.md（用户提供的新模块规范）、Docs/AllVersions-Handover.md（整理范围）。Assets/Deepsea 为已有系统；Assets/_Project 为后续协作预留，两者未批量迁移或重命名。

Library、Temp、Builds 和独立可执行程序按忽略规则留在本地。历史视频及打包备份也未全部纳入仓库，不能将源码上传称为所有桌面二进制备份已上云。重建脚本可能保留历史绝对路径，执行前须按新工程位置核对。

本次是文件整理、协作底座与版本控制操作，没有重新验证各版本的运行、玩法、美术或性能。主仓库57项结构与依赖检查通过，五个分支LFS指针检查通过；实际远程上传结果以执行回执和远端分支核对为准。
