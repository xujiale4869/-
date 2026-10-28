# 海面层次与性能 v24

桌面：深海灯塔-海面层次与性能-v24。F7对照水面，F9帧率；WASD、E活动、F上下船沿用。默认不限帧。

本轮重做滚动波浪、浅水遮透和水下细节预算。1080p同镜头不限帧约148FPS；4K离屏场景约56FPS，仍未达到稳定60或高刷。实测范围与取舍见reference-analysis.md和JSON。

preview.mp4为实际引擎录帧的8秒30fps演示；不作为帧率证据。源码在Assets中，依赖既有v23工程资产；source-backup保存修改前的v23主类。构建入口为Deepsea.Editor.SeaSurfaceBuildV24.Build，打开已保存场景与材质。不要运行旧版恢复脚本覆盖这轮更改。
