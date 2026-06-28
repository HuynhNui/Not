using System;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.Balance;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.ProgressionSystem
{
    /// <summary>
    /// Defines the five permanent player upgrades shown in the upgrade panel.
    /// </summary>
    public static class PlayerMetaUpgradeService
    {
        public const int MaxUpgradeLevel = PlayerMetaBalanceConfig.DefaultMaxLevel;
        public const int RuntimeProjectileCount = 1;
        [Obsolete("Balance v1 uses explicit level values instead of a shared multiplier.")]
        public const float UpgradeMultiplier = 1.5f;
        private static PlayerMetaBalanceConfig _balanceConfig;
        private static CombatScalingConfig _combatScalingConfig;

        public static readonly UpgradeDefinition[] Definitions =
        {
            new UpgradeDefinition(
                PlayerMetaUpgradeType.Damage,
                "DMG",
                "Damage",
                "Increases damage per bullet.",
                1f,
                false),
            new UpgradeDefinition(
                PlayerMetaUpgradeType.FireRate,
                "FIRE",
                "Fire Rate",
                "Increases shooting speed.",
                4f,
                false),
            new UpgradeDefinition(
                PlayerMetaUpgradeType.MaxHp,
                "HP",
                "Max HP",
                "Increases maximum health.",
                10f,
                false),
            new UpgradeDefinition(
                PlayerMetaUpgradeType.ProjectileCount,
                "BULLET",
                "Projectile Count",
                "Increases bullets per shot.",
                5f,
                true),
            new UpgradeDefinition(
                PlayerMetaUpgradeType.SquadSize,
                "PLAYER",
                "Squad Size",
                "Increases players and followers.",
                1f,
                true)
        };

        public static int GetLevel(PlayerMetaUpgradeType type)
        {
            return Mathf.Clamp(SaveService.Instance.GetUpgradeLevel(type), 0, MaxUpgradeLevel);
        }

        public static bool IsMaxLevel(PlayerMetaUpgradeType type)
        {
            return GetLevel(type) >= MaxUpgradeLevel;
        }

        public static bool IsSupportedUpgrade(PlayerMetaUpgradeType type)
        {
            for (int index = 0; index < Definitions.Length; index++)
            {
                if (Definitions[index].Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetCost(PlayerMetaUpgradeType type)
        {
            if (!IsSupportedUpgrade(type))
            {
                return 0;
            }

            int level = GetLevel(type);
            return level >= MaxUpgradeLevel ? 0 : GetLevelData(level + 1).Cost;
        }

        public static float GetCurrentValue(PlayerMetaUpgradeType type)
        {
            return GetValueForLevel(type, GetLevel(type));
        }

        public static float GetNextValue(PlayerMetaUpgradeType type)
        {
            int nextLevel = Mathf.Min(MaxUpgradeLevel, GetLevel(type) + 1);
            return GetValueForLevel(type, nextLevel);
        }

        public static float CalculateMaxValue(PlayerMetaUpgradeType type)
        {
            return GetValueForLevel(type, MaxUpgradeLevel);
        }

        public static float GetValueForLevel(PlayerMetaUpgradeType type, int level)
        {
            PlayerMetaLevelData levelData = GetLevelData(level);

            return type switch
            {
                PlayerMetaUpgradeType.Damage => levelData.Damage,
                PlayerMetaUpgradeType.FireRate => levelData.FireRate,
                PlayerMetaUpgradeType.MaxHp => levelData.MaxHp,
                PlayerMetaUpgradeType.ProjectileCount => levelData.ProjectileCount,
                PlayerMetaUpgradeType.SquadSize => levelData.SquadSize,
                _ => 0f
            };
        }

        public static void Configure(
            PlayerMetaBalanceConfig balanceConfig,
            CombatScalingConfig combatScalingConfig)
        {
            _balanceConfig = balanceConfig;
            _combatScalingConfig = combatScalingConfig;
        }

        public static string FormatValue(PlayerMetaUpgradeType type, float value)
        {
            UpgradeDefinition definition = GetDefinition(type);
            return definition.UsesWholeNumbers
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.##");
        }

        public static int GetPowerScore()
        {
            float damage = GetCurrentValue(PlayerMetaUpgradeType.Damage);
            float fireRate = GetCurrentValue(PlayerMetaUpgradeType.FireRate);
            int projectileCount = Mathf.RoundToInt(GetCurrentValue(PlayerMetaUpgradeType.ProjectileCount));
            int squadSize = Mathf.RoundToInt(GetCurrentValue(PlayerMetaUpgradeType.SquadSize));
            float effectiveDps = BalanceV1Math.EffectiveDps(
                damage,
                fireRate,
                projectileCount,
                squadSize,
                _combatScalingConfig);
            return Mathf.Max(0, Mathf.RoundToInt(effectiveDps * 100f));
        }

        public static bool TryPurchase(PlayerMetaUpgradeType type)
        {
            if (!IsSupportedUpgrade(type) || IsMaxLevel(type))
            {
                return false;
            }

            return SaveService.Instance.TryPurchaseUpgrade(type, GetCost(type));
        }

        public static void ApplyToPlayer(MainPlayerUnit mainPlayerUnit, PlayerController playerController)
        {
            if (mainPlayerUnit == null)
            {
                return;
            }

            mainPlayerUnit.SetDamage(GetCurrentValue(PlayerMetaUpgradeType.Damage));
            mainPlayerUnit.SetFireRate(GetCurrentValue(PlayerMetaUpgradeType.FireRate));
            mainPlayerUnit.SetMaxHp(GetCurrentValue(PlayerMetaUpgradeType.MaxHp), healByDelta: true);
            mainPlayerUnit.RestoreFullHealth();

            if (mainPlayerUnit.BulletSpawner != null)
            {
                mainPlayerUnit.BulletSpawner.SetProjectileCount(RuntimeProjectileCount);
            }

            if (playerController != null)
            {
                if (_combatScalingConfig != null)
                {
                    playerController.SetCombatScalingConfig(_combatScalingConfig);
                }

                playerController.SetSquadCount(
                    Mathf.RoundToInt(GetCurrentValue(PlayerMetaUpgradeType.SquadSize)),
                    1f);
            }

            SyncFollowersFromMain(playerController, mainPlayerUnit);
        }

        public static UpgradeDefinition GetDefinition(PlayerMetaUpgradeType type)
        {
            for (int index = 0; index < Definitions.Length; index++)
            {
                if (Definitions[index].Type == type)
                {
                    return Definitions[index];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported player upgrade type.");
        }

        private static PlayerMetaLevelData GetLevelData(int level)
        {
            int safeLevel = Mathf.Clamp(level, 0, MaxUpgradeLevel);
            return _balanceConfig != null
                ? _balanceConfig.GetLevelData(safeLevel)
                : PlayerMetaBalanceConfig.GetDefaultLevelData(safeLevel);
        }

        private static void SyncFollowersFromMain(PlayerController playerController, MainPlayerUnit mainPlayerUnit)
        {
            if (playerController == null || mainPlayerUnit == null)
            {
                return;
            }

            playerController.SyncFollowersFromMain(
                syncDamage: true,
                syncFireRate: true,
                syncMaxHp: true,
                healMaxHpByDelta: true,
                syncProjectileCount: true);
        }
    }

    public enum PlayerMetaUpgradeType
    {
        Damage,
        FireRate,
        MoveSpeed,
        MaxHp,
        ProjectileCount,
        SquadSize
    }

    public readonly struct UpgradeDefinition
    {
        public readonly PlayerMetaUpgradeType Type;
        public readonly string DisplayName;
        public readonly string StatName;
        public readonly string Description;
        public readonly float BaseValue;
        public readonly bool UsesWholeNumbers;

        public UpgradeDefinition(
            PlayerMetaUpgradeType type,
            string displayName,
            string statName,
            string description,
            float baseValue,
            bool usesWholeNumbers)
        {
            Type = type;
            DisplayName = displayName;
            StatName = statName;
            Description = description;
            BaseValue = baseValue;
            UsesWholeNumbers = usesWholeNumbers;
        }
    }
}
