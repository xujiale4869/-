// =====================================================================
// 深海灯塔·归潮湾 (v25) — 鱼类与水族箱展缸 ScriptableObject 数据底座
// File: Assets/_Project/Scripts/Fishing/FishAndAquariumDataSO.cs
// =====================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame.Data
{
    public enum FishRarity
    {
        Common = 0,     // 常见 (近岸浅海)
        Uncommon = 1,   // 稀有 (外海岩礁)
        Rare = 2,       // 珍稀 (深海海沟)
        Legendary = 3   // 传说 (特殊天气/夜钓)
    }

    /// <summary>
    /// 单一鱼种的完整静态配置与视觉资产定义 (支持在 Unity Inspector 中直接配置与扩展)
    /// </summary>
    [Serializable]
    public class FishSpeciesEntry
    {
        [Header("基础标识 (兼容 v21 species ID: 0/1/2)")]
        public int speciesId;
        public string displayName = "银纹鱼";
        public string scientificName = "Sparus aurata";
        public FishRarity rarity = FishRarity.Common;

        [TextArea(2, 4)]
        public string description = "栖息于归潮湾浅石带的群游海鱼，体侧有七道银灰暗纹。";

        [Header("3D 视觉资产与尺寸配置")]
        public Mesh highDetailMesh;
        public Texture2D albedoTexture;
        public Color baseTint = Color.white;
        public Color iridescentRimColor = new Color(0.55f, 0.88f, 0.95f, 1.0f);
        [Range(0f, 1f)] public float smoothness = 0.78f;
        [Range(0f, 1f)] public float metallic = 0.25f;

        [Header("游动与脊椎摆尾物理参数")]
        [Tooltip("在展缸内的标准缩放倍率")]
        public float aquariumScale = 0.55f;
        [Tooltip("在野外鱼群中的标准缩放倍率")]
        public float wildScale = 0.72f;
        [Tooltip("巡游基础速度")]
        public float swimSpeed = 1.05f;
        [Tooltip("脊椎 S 型摆尾幅度")]
        public float tailWaveAmplitude = 0.085f;
        [Tooltip("脊椎 S 型摆尾频率")]
        public float tailWaveFrequency = 6.5f;

        [Header("垂钓与经济数值")]
        public int baseCoinValue = 18;
        [Range(0.2f, 2.5f)] public float tensionPullStrength = 1.0f;
    }

    /// <summary>
    /// 展缸与海湾生态造景主题配置
    /// </summary>
    [Serializable]
    public class AquariumTankStyleEntry
    {
        public string styleId = "StandardOakTank";
        public string displayName = "红枫榫卯生态大缸";
        public int unlockCost = 35;
        public int maxFishCapacity = 6;

        [Header("展缸高精度组件网格 (由 Blender v25 脚本生成)")]
        public Mesh frameMesh;      // AquariumTank_V25_Frame
        public Mesh interiorMesh;   // AquariumTank_V25_Interior
        public Mesh glassMesh;      // AquariumTank_V25_Glass

        [Header("水体光学与顶灯氛围参数")]
        public Color glassTint = new Color(0.42f, 0.82f, 0.88f, 0.11f);
        public Color waterVolumeFogColor = new Color(0.14f, 0.45f, 0.54f, 0.18f);
        public Color topLightColor = new Color(0.95f, 0.96f, 0.88f, 1.0f);
        public float topLightIntensity = 1.8f;
        public float causticsIntensity = 0.35f;
    }

}
