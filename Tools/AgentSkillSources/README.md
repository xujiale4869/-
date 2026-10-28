# 已安装美术技能的固定源码
这里保存此前已安装的五个技能的原文件副本，未修改用户技能目录，也未声称运行了新的美术生产。
sources.lock.json 记录原始仓库、提交、子目录及文件数量。
这些是制作工具，不是 Unity 运行组件；当前安装仍在用户 .agents/skills 目录。
现有 Tools/CoastArtV12、CoastArtV13 等为实际资产生产入口，不能用技能安装代替资产导入和实机验收。

Impeccable与UI UX Pro Max也保存了现有安装的原文件副本；原上游提交本轮未确定，来源锁中明确为空，不伪造固定版本。五个工具快照逐文件保存SHA256。

为保留原文件字节，上游技能自带的.gitattributes以.gitattributes.upstream保存，避免其换行策略作用于档案；映射记录在sources.lock.json的archivedPolicyFiles。还原为独立技能时按映射恢复文件名，原内容未修改。
