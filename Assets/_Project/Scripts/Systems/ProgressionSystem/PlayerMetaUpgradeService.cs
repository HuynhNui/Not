using System;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.ProgressionSystem
{
    /// <summary>
    /// Defines the five permanent player upgrades shown in the upgrade panel.
    /// </summary>
    public static class PlayerMetaUpgradeService
    {
        public const int MaxUpgradeLevel = 5;
        public const float UpgradeMultiplier = 1.5f;

        private static readonly int[] UpgradeCosts =
        {
            100,
            200,
            500,
            1500,
            5000
        };

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
            return level >= MaxUpgradeLevel ? 0 : UpgradeCosts[level];
        }

        public static float GetCurrentValue(PlayerMetaUpgradeType type)
        {
            UpgradeDefinition definition = GetDefinition(type);
            return CalculateValue(definition.BaseValue, GetLevel(type), definition.UsesWholeNumbers);
        }

        public static float GetNextValue(PlayerMetaUpgradeType type)
        {
            UpgradeDefinition definition = GetDefinition(type);
            int nextLevel = Mathf.Min(MaxUpgradeLevel, GetLevel(type) + 1);
            return CalculateValue(definition.BaseValue, nextLevel, definition.UsesWholeNumbers);
        }

        public static float CalculateMaxValue(PlayerMetaUpgradeType type)
        {
            UpgradeDefinition definition = GetDefinition(type);
            return CalculateValue(definition.BaseValue, MaxUpgradeLevel, definition.UsesWholeNumbers);
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
            float projectileCount = GetCurrentValue(PlayerMetaUpgradeType.ProjectileCount);
            float squadSize = GetCurrentValue(PlayerMetaUpgradeType.SquadSize);
            return Mathf.Max(0, Mathf.RoundToInt(damage * fireRate * projectileCount * squadSize * 100f));
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
                mainPlayerUnit.BulletSpawner.SetProjectileCount(
                    Mathf.RoundToInt(GetCurrentValue(PlayerMetaUpgradeType.ProjectileCount)));
            }

            if (playerController != null)
            {
                playerController.SetSquadCount(
                    Mathf.RoundToInt(GetCurrentValue(PlayerMetaUpgradeType.SquadSize)));
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

        private static float CalculateValue(float baseValue, int level, bool useWholeNumbers)
        {
            float value = Mathf.Max(0f, baseValue);
            int safeLevel = Mathf.Clamp(level, 0, MaxUpgradeLevel);

            for (int index = 0; index < safeLevel; index++)
            {
                value *= UpgradeMultiplier;
                if (useWholeNumbers)
                {
                    value = Mathf.CeilToInt(value);
                }
            }

            return value;
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
