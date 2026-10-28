# 深海灯塔 · 海草与海鸟联动 V1

这是现有 Unity 2022.3.62f3c1 / URP 工程的增量源码包，不是完整游戏工程。
合入现有 v0.3 工程根目录，保留 Assets 的相对路径及 .meta，执行 Deepsea.Editor.EcologyBuild.Build。

东岸新增 18 条小鱼与两只海鸟，保留港湾和原观察点的 42 条鱼。
鱼群以实际海草簇的中心为栖息地，经过聚集、表层觅食、受惊、回归四种状态。
海鸟依据鱼群实时位置和表层活动发起低掠；鸟影触发鱼群散开，触水复用波纹与小水花。
船只靠近会惊鱼，并中止尚未完成的低掠；离开后鱼群逐渐回到海草附近。
海草共享缓慢变化的水流方向，船只运动产生的局部弯曲会平滑衰减。
新装饰和生物没有碰撞体，不更改登陆点和任务目标。

鸟模型来自 NVJOB Boids，固定版本及 MIT 许可见 ThirdParty/NVJOB/SOURCE.md。
鸟的 URP 着色器、振翅、目标选择与生态联动为原创代码。
鱼模型及群游算法保留 MarineLife 来源与许可，水面保留 Boat Attack 来源与许可。

桌面“深海灯塔－水面改进版”应指向 Builds/EcologyV1/GuichaoBay.exe。
开发验证使用 -ecology-check 参数，独立验证进度，不覆盖正常游戏存档。
Verification/ecology-v1/result.json 记录实际 Windows 独立版测试；它不代表长期玩家测试或完整性能基准。
