# 深海灯塔：浅海与岸边更新 V1
本包是现有 Unity 工程的增量源码备份，依赖原工程，不是独立游戏。
## 本轮内容
- 连续海底与水深图统一细化；浅水透出沙底、石头和海草。
- 东侧浅滩布置 8 块水下石头、13 簇海草（169 片叶片），随水流摆动。
- 岛岸增加干沙、湿沙与草地的颜色过渡；局部岸浪向岸边推进。
- 在海底与岩石上应用水下焦散，调整阳光方向并收敛大面积柔糊高光。
- 统一旧岛底与连续海底的水下材质，移除局部叠加沙底，避免矩形接缝。
- 原有鱼群、飞鱼、尾流与局部入水涟漪保留。
## 在游戏中查看
桌面“深海灯塔－水面改进版”指向 Builds/ShoreV1/GuichaoBay.exe。
前往雾岬岛东侧登陆码头附近，可看到本轮精修浅滩。
桌面“深海灯塔－浅滩演示”可查看固定镜头实机录制。
## 验证范围
Verification/shore-v1/result.json：Windows 着色器支持、海底装饰可见性、海草保持水下、亮度与静止船只下的水面运动。
Verification/shore-v1/terrain-result.json：既有登陆与岛上步行检查。
所有新增装饰均无碰撞体；本轮未修改岛屿碰撞网格与登陆位置。
已人工检查港口、岛岸、近景和全景截图。
这些结果不代表完成长期游玩或帧率基准。
## 恢复或迁移
先备份同版本工程，再合并 Assets、Packages、ProjectSettings 内容。
需要 Unity 2022.3 / URP 14 与原有 Deepsea 游戏代码、Boat Attack 水面依赖。
构建入口：Deepsea.Editor.ShoreBuild.Build。
本包保留此前海洋生物更新的源码、鱼模型和许可；不包含完整旧工程、完整上游项目或 Windows 可执行程序。
## 来源
Boat Attack Water 的固定版本、焦散贴图与改编范围见 Assets/Deepsea/ThirdParty/BoatAttack/SHORE_SOURCE.md。
沙底材质、湿沙过渡、海草叶片、摆动、布景和岸浪为本项目制作。
鱼模型和群游算法的来源及许可保留在 Assets/Deepsea/ThirdParty/MarineLife。
