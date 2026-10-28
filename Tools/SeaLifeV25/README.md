# SeaLifeV25

实际接入与验证说明见 ../../Docs/SeaLifeV25/README.md。

OriginalInputs 保留用户提交的原稿；生成器与实际运行源码已经过兼容修正。
BlenderSources 是可编辑源，Resources/SeaLifeV25 是 Unity 使用的 FBX、贴图、材质和烘焙网格。
默认构建入口 Deepsea.Editor.SeaLifeV25Build.Build 不重新生成资源。
显式 ImportAndBuild 会重建本轮资产与场景，需要重新验证。
正常存档和旧版本程序保留；-v25-gallery 只使用临时展示样本。
