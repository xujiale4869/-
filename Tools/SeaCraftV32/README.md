# 归潮湾 v32 开发预检

基于 v31.1 的 `0b336eeaeb4843e540a017e5daed2cfdba53de1c`。四个 Blender 模型已通过几何检查。Windows Unity 编译、实机验收和正式发布待完成。

- 船匠工坊、海风酒馆和观测屋沿用原来的地点、朝向和碰撞体。使用独立叠瓦、搭接木墙、石砌台基以及各自的木、石、窗和金属材质槽。
- 前院、岸边通道、门廊和可建造栈桥在网格合并之前生成倒角板和板缝。板数按实际步幅、板宽和台面尺寸计算。
- 原主码头的木材保留现有网格及碰撞，只切换木材槽。铁件、绳索和灯光继续使用原资源。
- v32 沿用 v30 界面、v31.1 珊瑚和存档路径。新美术通过构建定义 `DEEPSEA_V32` 启用。

## 构建与实机核验

从项目根目录运行以下命令。Windows 版 Blender 与 Unity 2022.3.62f3c1 的路径可通过参数指定。

```powershell
./Tools/SeaCraftV32/Rebuild-Craft.ps1
./Tools/SeaVillageV31/Run-Review.ps1 -Craft -Evidence village-v31-craft-review01 -TableSample 10 -AnemoneSample 9
./Tools/SeaVillageV31/Run-Review.ps1 -Craft -Native -Evidence village-v31-craft-native01
```

构建目标为 `Builds/SeaCraftV32/Guichao-SeaCraftV32.exe`。生成的四份可编辑 `.blend` 在 `Tools/SeaCraftV32/Masters`。Unity 运行时读取烘焙网格与短材质槽清单。

在实机里检查 `v32-preview.jpg`、`v32-wood-and-house-craft-closeup.jpg` 和 `v32-slipway-craft.jpg` 的可见构图、模型衔接和材质效果，确认图像为 1920×1080。通过原有布局和普通启动的存档检查后，再归档图片与验收结果到 `Docs/SeaCraftV32`，更新最新版快捷方式并推送主分支。
