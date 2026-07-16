using System;
using System.Collections.Generic;
using _Project.Scripts.Systems.Balance;
using _Project.Scripts.Systems.ProgressionSystem;
using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "PlayerMetaEconomyConfig",
        menuName = "Chibi Pixel Gate/Balance/Player Meta Economy Config")]
    public sealed class PlayerMetaEconomyConfig : ScriptableObject
    {
        public const string Run45ConfigVersion = PlayerProgressionMilestones.ConfigVersion;

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

    public static class PlayerProgressionMilestones
    {
        public const string ConfigVersion = "economy-v1.4.1-run45-progression";
        public const int FullTreeCost = 850000;

        private static readonly PlayerProgressionCheckpoint[] Checkpoints =
        {
            new PlayerProgressionCheckpoint(5, 1, 1, 1, 0, 0, 3, 12500, 11000, 1500),
            new PlayerProgressionCheckpoint(10, 1, 1, 1, 1, 1, 5, 40000, 38000, 2000),
            new PlayerProgressionCheckpoint(15, 2, 2, 2, 1, 1, 8, 78000, 68000, 10000),
            new PlayerProgressionCheckpoint(20, 2, 2, 3, 1, 2, 10, 138000, 136000, 2000),
            new PlayerProgressionCheckpoint(25, 3, 2, 3, 2, 2, 12, 223000, 221000, 2000),
            new PlayerProgressionCheckpoint(30, 3, 3, 4, 2, 2, 14, 333000, 285000, 48000),
            new PlayerProgressionCheckpoint(35, 4, 4, 4, 2, 2, 16, 468000, 402000, 66000),
            new PlayerProgressionCheckpoint(40, 4, 4, 5, 2, 3, 18, 623000, 611000, 12000),
            new PlayerProgressionCheckpoint(45, 4, 5, 5, 2, 3, 19, 793000, 711000, 82000)
        };

        public static IReadOnlyList<PlayerProgressionCheckpoint> ReferenceCheckpoints => Checkpoints;

        public static bool TryGetCheckpoint(int runNumber, out PlayerProgressionCheckpoint checkpoint)
        {
            for (int index = 0; index < Checkpoints.Length; index++)
            {
                if (Checkpoints[index].RunNumber == runNumber)
                {
                    checkpoint = Checkpoints[index];
                    return true;
                }
            }

            checkpoint = default;
            return false;
        }
    }

    [Serializable]
    public readonly struct PlayerProgressionCheckpoint
    {
        public readonly int RunNumber;
        public readonly int DamageLevel;
        public readonly int FireRateLevel;
        public readonly int MaxHpLevel;
        public readonly int ProjectileCountLevel;
        public readonly int SquadSizeLevel;
        public readonly int TargetPurchases;
        public readonly int TargetCumulativeIncome;
        public readonly int TargetSpent;
        public readonly int TargetWalletReserve;

        public PlayerProgressionCheckpoint(
            int runNumber,
            int damageLevel,
            int fireRateLevel,
            int maxHpLevel,
            int projectileCountLevel,
            int squadSizeLevel,
            int targetPurchases,
            int targetCumulativeIncome,
            int targetSpent,
            int targetWalletReserve)
        {
            RunNumber = runNumber;
            DamageLevel = damageLevel;
            FireRateLevel = fireRateLevel;
            MaxHpLevel = maxHpLevel;
            ProjectileCountLevel = projectileCountLevel;
            SquadSizeLevel = squadSizeLevel;
            TargetPurchases = targetPurchases;
            TargetCumulativeIncome = targetCumulativeIncome;
            TargetSpent = targetSpent;
            TargetWalletReserve = targetWalletReserve;
        }

        public float DamageValue => PlayerMetaBalanceConfig.GetDefaultLevelData(DamageLevel).Damage;
        public float FireRateValue => PlayerMetaBalanceConfig.GetDefaultLevelData(FireRateLevel).FireRate;
        public float MaxHpValue => PlayerMetaBalanceConfig.GetDefaultLevelData(MaxHpLevel).MaxHp;
        public int ProjectileCountValue => PlayerMetaBalanceConfig.GetDefaultLevelData(ProjectileCountLevel).ProjectileCount;
        public int SquadSizeValue => PlayerMetaBalanceConfig.GetDefaultLevelData(SquadSizeLevel).SquadSize;

        public float EstimateDps(CombatScalingConfig combatConfig)
        {
            return BalanceV1Math.EffectiveDps(
                DamageValue,
                FireRateValue,
                ProjectileCountValue,
                SquadSizeValue,
                combatConfig);
        }

        public float EstimateEmissions(CombatScalingConfig combatConfig)
        {
            return BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                FireRateValue,
                ProjectileCountValue,
                SquadSizeValue,
                combatConfig);
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
