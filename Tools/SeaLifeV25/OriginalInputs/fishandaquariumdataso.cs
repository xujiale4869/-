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

    /// <summary>
    /// 鱼类与水族箱全局数据库资产 (ScriptableObject)
    /// 既可在编辑器创建 .asset 配置文件，也支持运行时自动从 Resources/SeaLifeV25 构建默认实例
    /// </summary>
    [CreateAssetMenu(fileName = "FishAndAquariumDatabase", menuName = "OceanGame/Fish & Aquarium Database (v25)")]
    public class FishAndAquariumDatabaseSO : ScriptableObject
    {
        public List<FishSpeciesEntry> fishSpecies = new List<FishSpeciesEntry>();
        public AquariumTankStyleEntry defaultTankStyle = new AquariumTankStyleEntry();

        public FishSpeciesEntry GetFishById(int id)
        {
            if (fishSpecies == null || fishSpecies.Count == 0) return null;
            for (int i = 0; i < fishSpecies.Count; i++)
            {
                if (fishSpecies[i].speciesId == id) return fishSpecies[i];
            }
            return fishSpecies[Mathf.Clamp(id, 0, fishSpecies.Count - 1)];
        }

        /// <summary>
        /// 当工程尚未手动创建 .asset 时，自动生成包含 v21 三大鱼种 (银纹鱼/蓝鳍鱼/金背鱼) 的默认高精数据库
        /// </summary>
        public static FishAndAquariumDatabaseSO CreateRuntimeDefault()
        {
            var db = CreateInstance<FishAndAquariumDatabaseSO>();
            db.name = "RuntimeFishAndAquariumDatabase_V25";

            db.fishSpecies = new List<FishSpeciesEntry>
            {
                new FishSpeciesEntry
                {
                    speciesId = 0,
                    displayName = "银纹鱼",
                    scientificName = "Diplodus sargus",
                    rarity = FishRarity.Common,
                    description = "栖息在红枫海岬浅礁带的灵巧鲷鱼，体侧泛着青银光泽与七道暗纹。",
                    baseTint = new Color(0.88f, 0.95f, 0.96f, 1f),
                    iridescentRimColor = new Color(0.52f, 0.90f, 0.92f, 1f),
                    smoothness = 0.78f,
                    metallic = 0.28f,
                    aquariumScale = 0.52f,
                    wildScale = 0.68f,
                    swimSpeed = 1.15f,
                    tailWaveAmplitude = 0.085f,
                    tailWaveFrequency = 7.2f,
                    baseCoinValue = 15,
                    tensionPullStrength = 0.9f
                },
                new FishSpeciesEntry
                {
                    speciesId = 1,
                    displayName = "蓝鳍鱼",
                    scientificName = "Thunnus thynnus",
                    rarity = FishRarity.Uncommon,
                    description = "游弋于外海深水沟的流线型掠食鱼，背脊呈深邃钴蓝，尾鳍带有亮金尖端。",
                    baseTint = new Color(0.85f, 0.92f, 0.98f, 1f),
                    iridescentRimColor = new Color(0.25f, 0.68f, 1.0f, 1f),
                    smoothness = 0.85f,
                    metallic = 0.35f,
                    aquariumScale = 0.56f,
                    wildScale = 0.75f,
                    swimSpeed = 1.35f,
                    tailWaveAmplitude = 0.095f,
                    tailWaveFrequency = 8.0f,
                    baseCoinValue = 28,
                    tensionPullStrength = 1.35f
                },
                new FishSpeciesEntry
                {
                    speciesId = 2,
                    displayName = "金背鱼",
                    scientificName = "Chaetodon auriga",
                    rarity = FishRarity.Rare,
                    description = "穿梭于珊瑚丛中的高背彩鱼，背鳍修长如丝，鳞片在展缸灯光下呈现琥珀金辉。",
                    baseTint = new Color(1.0f, 0.94f, 0.82f, 1f),
                    iridescentRimColor = new Color(1.0f, 0.78f, 0.28f, 1f),
                    smoothness = 0.74f,
                    metallic = 0.22f,
                    aquariumScale = 0.50f,
                    wildScale = 0.65f,
                    swimSpeed = 0.92f,
                    tailWaveAmplitude = 0.075f,
                    tailWaveFrequency = 6.2f,
                    baseCoinValue = 42,
                    tensionPullStrength = 1.15f
                }
            };

            return db;
        }
    }
}