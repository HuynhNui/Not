using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "PlayerMetaBalanceConfig_v1",
        menuName = "Chibi Pixel Gate/Balance/Player Meta Balance Config")]
    public sealed class PlayerMetaBalanceConfig : ScriptableObject
    {
        public const int DefaultMaxLevel = 5;
        private static readonly PlayerMetaLevelData[] DefaultLevels =
        {
            new PlayerMetaLevelData(0, 1f, 4f, 10f, 5, 1, 0),
            new PlayerMetaLevelData(1, 1.1f, 4.4f, 11.5f, 6, 2, 100),
            new PlayerMetaLevelData(2, 1.2f, 4.8f, 13f, 8, 3, 250),
            new PlayerMetaLevelData(3, 1.3f, 5.2f, 15f, 10, 5, 550),
            new PlayerMetaLevelData(4, 1.42f, 5.8f, 17.5f, 13, 8, 1100),
            new PlayerMetaLevelData(5, 1.55f, 6.4f, 20f, 16, 12, 2200)
        };

        [SerializeField] private string configVersion = CombatScalingConfig.DefaultConfigVersion;
        [SerializeField] private List<PlayerMetaLevelData> levels = CreateDefaultLevels();

        public string ConfigVersion => configVersion;
        public IReadOnlyList<PlayerMetaLevelData> Levels => levels;
        public int MaxLevel => levels == null ? 0 : Mathf.Max(0, levels.Count - 1);

        public PlayerMetaLevelData GetLevelData(int level)
        {
            EnsureDefaults();
            return levels[Mathf.Clamp(level, 0, levels.Count - 1)];
        }

        public static PlayerMetaLevelData GetDefaultLevelData(int level)
        {
            return DefaultLevels[Mathf.Clamp(level, 0, DefaultMaxLevel)];
        }

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(configVersion))
            {
                configVersion = CombatScalingConfig.DefaultConfigVersion;
            }

            EnsureDefaults();
            levels.Sort((left, right) => left.Level.CompareTo(right.Level));

            for (int index = 0; index < levels.Count; index++)
            {
                levels[index].Validate(index);
            }
        }

        private static List<PlayerMetaLevelData> CreateDefaultLevels()
        {
            var result = new List<PlayerMetaLevelData>(DefaultLevels.Length);
            for (int index = 0; index < DefaultLevels.Length; index++)
            {
                result.Add(DefaultLevels[index].Copy());
            }

            return result;
        }

        private void EnsureDefaults()
        {
            if (levels == null || levels.Count == 0)
            {
                levels = CreateDefaultLevels();
            }
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }

    [Serializable]
    public sealed class PlayerMetaLevelData
    {
        [SerializeField] private int level;
        [SerializeField] private float damage;
        [SerializeField] private float fireRate;
        [SerializeField] private float maxHp;
        [SerializeField] private int projectileCount;
        [SerializeField] private int squadSize;
        [SerializeField] private int cost;

        public PlayerMetaLevelData(
            int level,
            float damage,
            float fireRate,
            float maxHp,
            int projectileCount,
            int squadSize,
            int cost)
        {
            this.level = level;
            this.damage = damage;
            this.fireRate = fireRate;
            this.maxHp = maxHp;
            this.projectileCount = projectileCount;
            this.squadSize = squadSize;
            this.cost = cost;
        }

        public int Level => level;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float MaxHp => maxHp;
        public int ProjectileCount => projectileCount;
        public int SquadSize => squadSize;
        public int Cost => cost;

        public PlayerMetaLevelData Copy()
        {
            return new PlayerMetaLevelData(
                level,
                damage,
                fireRate,
                maxHp,
                projectileCount,
                squadSize,
                cost);
        }

        public void Validate(int fallbackLevel)
        {
            level = Mathf.Max(0, fallbackLevel);
            damage = Mathf.Max(0f, damage);
            fireRate = Mathf.Max(0f, fireRate);
            maxHp = Mathf.Max(1f, maxHp);
            projectileCount = Mathf.Max(1, projectileCount);
            squadSize = Mathf.Max(1, squadSize);
            cost = Mathf.Max(0, cost);
        }
    }
}
