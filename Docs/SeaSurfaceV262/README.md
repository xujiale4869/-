# v26.2 焦散与波光收敛——待实机验证

本次按用户提供的 `gpt_v26_2_sweetspot_all_in_one.md` 准备。基线为 main 的 `bdc59d90fb518850c4c6d3429c8588f18d8a29d7`（v26.1）。

两份核心 Shader/C# 从附件原文提取，仅补终止换行。默认焦散为 0.92、碎光为 1.85、聚焦指数为 72、泡沫为 0.95；F7 调参台带五个滑块和三档预设。其余新增默认值与颜色也同步至现有 Water.mat，四份纹理引用保留。

现有 v24 F7 处理已经受 v26 Active 判空保护，未再修改。保留 SeaWaterReviewV26 与旧环形鱼群隔离 partial。额外用 SeaWaterTuningInputV262 和 SeaLifeUI 的可选指针判断拦截调参区鼠标输入，避免 IMGUI 滑块点击穿透至投竿/收线逻辑；关闭面板或没有 v26 控制器时恢复原判断。

新增 `Deepsea.Editor.SeaSurfacePolishV262.SyncAndBuild` 从当前核心 C# 解析全部 14 项默认值，检查 Shader 与纹理，再沿用现有场景构建。不要调用 v26.1 专用 SyncAndBuild，它仍断言旧版强度。当前 v26 场景只序列化两个已有开关，没有旧的强度字段覆盖新默认值，因此本次无需改写整份场景。

**状态：电脑端离线，本分支尚未进行 Unity 编译、可见窗口检查、性能采样或桌面程序更新。** 未复制旧版截图/结果来冒充 v26.2。Docs/SeaSurfaceV26 中的旧证据仍属于 v26.1。

已完成的是原文提取比对、材质/运行时默认参数一致性、既有 partial/F7 路由检查及变更范围核对。没有将这些检查表述为 Unity 编译通过。

电脑上线后，先检查本地工作区与此基线的差异，并备份原 v26.1 程序，再同步本分支及执行：

1. Unity batchmode 调用 `Deepsea.Editor.SeaSurfacePolishV262.SyncAndBuild`，检查编译与 Shader 日志。
2. 在可见窗口用 `-forceV26Water -water-v26-check -water-v26-dir Docs/SeaSurfaceV262/Runtime -screen-fullscreen 0 -screen-width 1920 -screen-height 1080` 启动，工作目录为工程根目录。现有 QA 已更新到 v26.2 默认值，并加入三档预设与调参台截图；它尚未运行。
3. 手动检查 F7 开关与全部五个滑块、三档预设；在已取竿时拖动面板，确认不触发投钩/收线；检查 F6 对照与 F9 帧率。
4. 检查焦散/波光收敛后的真实画面、波浪运动、暂停、1080p帧耗时与正常存档校验值。通过后再更新正式预览、桌面入口和 main。

原始交接文件在 `Tools/SeaSurfaceV262/OriginalInputs`。运行/材质默认值清单在同目录 `optical-defaults.json`，其中构建与实机状态为 pending。
