using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanGame.Data {
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