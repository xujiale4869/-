# 2.5D 海洋探索与海湾建设游戏 — 项目架构与双 AI 协作规范 (ARCHITECTURE.md)

## 1. 项目概述 (Project Overview)
* **引擎环境**：Unity (建议使用 Universal Render Pipeline - URP 支持 2.5D 光影与海面着色器)
* **核心视角**：2.5D 混合视角（航行时采用带倾角的第三人称追踪视角；海湾建设时平滑切换为俯视网格上帝视角）
* **三大核心玩法模块**：
  1. **船只航行与海洋探索 (Sailing & Navigation)**：船只物理推进、转向阻尼、海域切换、交互点探测（钓鱼点、漂流物、码头）。
  2. **钓鱼与收集系统 (Fishing & Inventory)**：抛竿与咬钩状态机、钓鱼小游戏判定、基于 `ScriptableObject` 的鱼类与物品图鉴库、船舱/仓库格子背包。
  3. **海湾建设与经营 (Bay Construction & Management)**：上帝视角下的网格系统（Grid Placement）、建筑物预制体生成、资源消耗与产出循环。

---

## 2. 双 AI 角色分工与协作铁律 (Dual-AI Collaboration Protocol)

为了避免两个 AI 同时修改同一份脚本导致代码覆盖或引用丢失，本项目采用 **“架构师 + 本地执行官”** 双轨协作模式：

### 角色分工
* **Gemini（首席架构师 & 核心系统设计师）**：
  * 负责全局系统设计、跨模块数据结构定义（`ScriptableObject` / 接口 `Interface` / 事件总线 `EventBus`）。
  * 负责编写复杂核心逻辑模块（如网格建造算法、钓鱼状态机、跨脚本重构）与完整独立 C# 脚本。
  * 负责通过 GitHub 仓库审查代码结构、排查跨文件疑难 Bug。
* **GPT + 本地电脑控制插件（本地工程执行官 & 集成助手）**：
  * 负责直接操作本地文件系统：创建目录结构、生成 `.gitignore` 与 `.gitattributes`、将代码写入 `Assets/_Project/Scripts/` 对应路径。
  * 负责单点功能修补、Unity Inspector 参数配置指导、本地 Git 提交（Commit）与推送（Push）。

### 协作四条铁律（两个 AI 必须严格遵守）
1. **单文件职责隔离**：严禁把所有逻辑塞进一个几千行的 `GameManager.cs`。每个脚本必须职责单一，按模块放入对应文件夹。
2. **命名空间与目录对齐**：所有项目自定义代码统一使用命名空间 `OceanGame.<ModuleName>`（例如 `OceanGame.Sailing`、`OceanGame.Fishing`、`OceanGame.Building`、`OceanGame.Core`）。
3. **模块间解耦通信**：
   * 船只模块、钓鱼模块、海湾建设模块之间**严禁直接硬编码互相引用内部私有变量**。
   * 跨模块数据统一通过 `Assets/_Project/Scripts/Core/` 下的公共接口或事件中心（`GameEvents.cs`）传递。
4. **每次改动先同步 Git**：每完成一个小功能模块或让 GPT 批量写入文件后，立即通过 Git / GitHub Desktop 提交（Commit）并推送（Push），保持云端仓库为最新状态。

---

## 3. 标准目录结构 (Directory Structure)

所有自定义资产统一收纳在 `Assets/_Project/` 目录下，与第三方插件严格隔离，方便 AI 快速定位文件：

```text
Assets/
└── _Project/
    ├── Art/                        # 美术资源（2D 精灵、3D 模型、材质、Shader）
    ├── Audio/                      # 音效与背景音乐
    ├── Prefabs/                    # 预制体
    │   ├── Boat/                   # 船只相关预制体
    │   ├── Buildings/              # 海湾建筑预制体
    │   ├── Environment/            # 海洋、岛屿、钓鱼点预制体
    │   └── UI/                     # 界面预制体
    ├── Data/                       # ScriptableObject 数据资产实例
    │   ├── Fish/                   # 鱼类配置数据 (.asset)
    │   ├── Items/                  # 建材与物品配置数据 (.asset)
    │   └── Buildings/              # 建筑配方与属性数据 (.asset)
    ├── Scenes/                     # 游戏场景
    │   ├── MainScene.unity         # 主游戏场景（海洋+海湾无缝或分区）
    │   └── SandboxTest.unity       # AI 功能测试沙盒场景
    └── Scripts/                    # 核心 C# 源代码
        ├── Core/                   # 全局核心（GameManager, EventBus, CameraController, SaveSystem）
        ├── Sailing/                # 船只航行（BoatController, WaterBuoyancy, InteractionDetector）
        ├── Fishing/                # 钓鱼玩法（FishingStateMachine, FishDataSO, FishingMiniGame）
        ├── Inventory/              # 背包与资源（InventoryManager, ItemDataSO, CargoGrid）
        ├── Building/               # 海湾建设（GridManager, BuildingPlacer, BuildingDataSO）
        └── UI/                     # UI 视图控制（UIManager, HUDView, BuildMenuView）
```

---

## 4. 核心模块接口契约 (Core Module Contracts)

后续无论是 Gemini 还是 GPT 编写具体代码，均需遵循以下核心类命名与职责约定：

| 模块 | 核心脚本文件名 | 命名空间 | 核心职责说明 |
| :--- | :--- | :--- | :--- |
| **核心与相机** | `CameraModeController.cs` | `OceanGame.Core` | 管理 `SailingMode`（航行追踪视角）与 `BuildMode`（海湾俯视上帝视角）的平滑切换 |
| **核心事件** | `GameEvents.cs` | `OceanGame.Core` | 定义全局静态事件（如 `OnEnterBay`、`OnFishCaught`、`OnBuildingPlaced`、`OnCameraModeChanged`） |
| **船只控制** | `BoatController.cs` | `OceanGame.Sailing` | 处理船只前进、加减速、转向惯性与停靠海湾锁定状态 |
| **钓鱼系统** | `FishingController.cs` | `OceanGame.Fishing` | 控制抛竿、等待咬钩、收竿小游戏与掉落鱼类结算 |
| **数据定义** | `FishDataSO.cs` / `BuildingDataSO.cs` | `OceanGame.Data` | 继承自 `ScriptableObject`，用于在 Unity 面板中可视化配置鱼类和海湾建筑属性 |
| **海湾建设** | `BayGridManager.cs` | `OceanGame.Building` | 管理海湾 2.5D 建造网格坐标转换、占地碰撞检测与建筑实例化 |

---

## 5. Git 与 GitHub 同步约定 (Git Workflow)
1. **Unity 编辑器必改设置**：
   * `Edit -> Project Settings -> Editor -> Asset Serialization -> Mode` 必须设为 **Force Text**。
   * `Edit -> Project Settings -> Editor -> Version Control -> Mode` 必须设为 **Visible Meta Files**。
2. **提交规范**：
   * 每次让 AI 完成一个独立任务（如“初始化目录结构”或“完成船只移动脚本”）并在 Unity 中确认无红色编译报错（Console Error）后，进行一次 Git Commit。
   * Commit 信息简明扼要，例如：`init: 初始化项目目录与架构文档`、`feat(sailing): 实现船只基础航行与视角切换`。


## 6. 现有工程兼容说明（2026-10-28）
本文件是用户提供的后续协作规范，不表示所有契约已实现。已有游戏使用 Assets/Deepsea 与 Deepsea 命名空间，包含多代独立场景与样板。整理阶段保留全部既有 GUID、脚本、场景和命名空间；新增 Assets/_Project 为后续模块预留，不自动迁移旧系统。后续接入 OceanGame 模块前先确认与现有实现的适配方式。
v0.3 是工程容器名称，内含 v11–v24 等制作版本，不代表用户已选定它们为最终版本。main 作为既有工程历史的延续；早期独立工程分别归档在 archive 分支。
.gitattributes 沿用已验证的字节保留与 Git LFS 规则（* -text），避免换行重写破坏冻结输入哈希；ProjectSettings 保持可读文本。Force Text 和 Visible Meta Files 单独检查。
