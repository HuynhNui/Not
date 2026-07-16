using System;
using System.Collections.Generic;
using _Project.Scripts.Systems.ProgressionSystem;
using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "PlayerMetaEconomyConfig",
        menuName = "Chibi Pixel Gate/Balance/Player Meta Economy Config")]
    public sealed class PlayerMetaEconomyConfig : ScriptableObject
    {
        public const string Run45ConfigVersion = "economy-v1.4.0-run45";

        [SerializeField] private string configVersion = Run45ConfigVersion;
        [SerializeField] private float upgradeCostScale = 1f;
        [SerializeField] private List<PlayerMetaUpgradeCostTrack> costTracks = CreateRun45Tracks();

        public string ConfigVersion => configVersion;
        public float UpgradeCostScale => upgradeCostScale;
        public IReadOnlyList<PlayerMetaUpgradeCostTrack> CostTracks => costTracks;

        public int GetPurchaseCost(PlayerMetaUpgradeType type, int currentLevel)
        {
            PlayerMetaUpgradeCostTrack track = GetTrack(type);
            if (track == null)
            {
                return 0;
            }

            return track.GetPurchaseCost(currentLevel, upgradeCostScale);
        }

        public int GetTrackTotalCost(PlayerMetaUpgradeType type)
        {
            PlayerMetaUpgradeCostTrack track = GetTrack(type);
            return track != null ? track.GetTotalCost(upgradeCostScale) : 0;
        }

        public int GetFullTreeTotalCost()
        {
            EnsureDefaults();

            int total = 0;
            for (int index = 0; index < costTracks.Count; index++)
            {
                total += costTracks[index].GetTotalCost(upgradeCostScale);
            }

            return total;
        }

        public int GetCostCompleted(PlayerMetaUpgradeType type, int currentLevel)
        {
            PlayerMetaUpgradeCostTrack track = GetTrack(type);
            return track != null ? track.GetCostCompleted(currentLevel, upgradeCostScale) : 0;
        }

        public int GetPurchaseCount(PlayerMetaUpgradeType type)
        {
            PlayerMetaUpgradeCostTrack track = GetTrack(type);
            return track != null ? track.PurchaseCount : 0;
        }

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(configVersion))
            {
                configVersion = Run45ConfigVersion;
            }

            upgradeCostScale = Mathf.Max(0f, upgradeCostScale);
            EnsureDefaults();

            for (int index = 0; index < costTracks.Count; index++)
            {
                costTracks[index].ValidateValues();
            }
        }

        public static PlayerMetaEconomyConfig CreateRun45RuntimeConfig()
        {
            PlayerMetaEconomyConfig config = CreateInstance<PlayerMetaEconomyConfig>();
            config.configVersion = Run45ConfigVersion;
            config.upgradeCostScale = 1f;
            config.costTracks = CreateRun45Tracks();
            config.ValidateValues();
            return config;
        }

        private PlayerMetaUpgradeCostTrack GetTrack(PlayerMetaUpgradeType type)
        {
            EnsureDefaults();

            for (int index = 0; index < costTracks.Count; index++)
            {
                PlayerMetaUpgradeCostTrack track = costTracks[index];
                if (track != null && track.UpgradeType == type)
                {
                    return track;
                }
            }

            return null;
        }

        private void EnsureDefaults()
        {
            if (costTracks == null || costTracks.Count == 0)
            {
                costTracks = CreateRun45Tracks();
            }
        }

        private static List<PlayerMetaUpgradeCostTrack> CreateRun45Tracks()
        {
            return new List<PlayerMetaUpgradeCostTrack>
            {
                new PlayerMetaUpgradeCostTrack(PlayerMetaUpgradeType.Damage, 4000, 12000, 30000, 65000, 139000),
                new PlayerMetaUpgradeCostTrack(PlayerMetaUpgradeType.FireRate, 4000, 10000, 24000, 52000, 100000),
                new PlayerMetaUpgradeCostTrack(PlayerMetaUpgradeType.MaxHp, 3000, 8000, 20000, 40000, 69000),
                new PlayerMetaUpgradeCostTrack(PlayerMetaUpgradeType.ProjectileCount, 15000, 55000),
                new PlayerMetaUpgradeCostTrack(PlayerMetaUpgradeType.SquadSize, 12000, 48000, 140000)
            };
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }

    [Serializable]
    public sealed class PlayerMetaUpgradeCostTrack
    {
        [SerializeField] private PlayerMetaUpgradeType upgradeType;
        [SerializeField] private List<int> purchaseCosts = new List<int>();

        public PlayerMetaUpgradeCostTrack()
        {
        }

        public PlayerMetaUpgradeCostTrack(PlayerMetaUpgradeType upgradeType, params int[] purchaseCosts)
        {
            this.upgradeType = upgradeType;
            this.purchaseCosts = new List<int>(purchaseCosts);
        }

        public PlayerMetaUpgradeType UpgradeType => upgradeType;
        public IReadOnlyList<int> PurchaseCosts => purchaseCosts;
        public int PurchaseCount => purchaseCosts != null ? purchaseCosts.Count : 0;

        public int GetPurchaseCost(int currentLevel, float costScale)
        {
            if (purchaseCosts == null
                || currentLevel < 0
                || currentLevel >= purchaseCosts.Count)
            {
                return 0;
            }

            return ScaleCost(purchaseCosts[currentLevel], costScale);
        }

        public int GetTotalCost(float costScale)
        {
            if (purchaseCosts == null)
            {
                return 0;
            }

            int total = 0;
            for (int index = 0; index < purchaseCosts.Count; index++)
            {
                total += ScaleCost(purchaseCosts[index], costScale);
            }

            return total;
        }

        public int GetCostCompleted(int currentLevel, float costScale)
        {
            if (purchaseCosts == null)
            {
                return 0;
            }

            int purchaseCount = Mathf.Clamp(currentLevel, 0, purchaseCosts.Count);
            int total = 0;
            for (int index = 0; index < purchaseCount; index++)
            {
                total += ScaleCost(purchaseCosts[index], costScale);
            }

            return total;
        }

        public void ValidateValues()
        {
            purchaseCosts ??= new List<int>();
            for (int index = 0; index < purchaseCosts.Count; index++)
            {
                purchaseCosts[index] = Mathf.Max(0, purchaseCosts[index]);
            }
        }

        private static int ScaleCost(int cost, float costScale)
        {
            return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, cost) * Mathf.Max(0f, costScale)));
        }
    }
}
