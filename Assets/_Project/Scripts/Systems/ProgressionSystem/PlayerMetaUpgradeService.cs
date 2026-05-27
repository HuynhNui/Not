using System;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;

namespace _Project.Scripts.Systems.ProgressionSystem
{
    /// <summary>
    /// Lightweight PlayerPrefs-backed meta upgrades for the first UI progression pass.
    /// </summary>
    public static class PlayerMetaUpgradeService
    {
        private const string LevelKeyPrefix = "MetaUpgrade.Level.";

        public static readonly UpgradeDefinition[] Definitions =
        {
            new UpgradeDefinition(PlayerMetaUpgradeType.Damage, "DAMAGE", 25, 1f, "+{0:0} damage"),
            new UpgradeDefinition(PlayerMetaUpgradeType.FireRate, "FIRE RATE", 30, 0.2f, "+{0:0.0}/s"),
            new UpgradeDefinition(PlayerMetaUpgradeType.MoveSpeed, "MOVE SPEED", 20, 0.35f, "+{0:0.00} speed"),
            new UpgradeDefinition(PlayerMetaUpgradeType.MaxHp, "HP", 25, 2f, "+{0:0} hp"),
            new UpgradeDefinition(PlayerMetaUpgradeType.ProjectileCount, "PROJECTILES", 60, 1f, "+{0:0} projectile")
        };

        public static int GetLevel(PlayerMetaUpgradeType type)
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(GetLevelKey(type), 0));
        }

        public static int GetCost(PlayerMetaUpgradeType type)
        {
            UpgradeDefinition definition = GetDefinition(type);
            int level = GetLevel(type);
            return Mathf.Max(1, Mathf.RoundToInt(definition.BaseCost * (level + 1) * (1f + level * 0.35f)));
        }

        public static float GetBonus(PlayerMetaUpgradeType type)
        {
            return GetDefinition(type).ValuePerLevel * GetLevel(type);
        }

        public static bool TryPurchase(PlayerMetaUpgradeType type, int walletCoins)
        {
            int cost = GetCost(type);
            if (walletCoins < cost)
            {
                return false;
            }

            PlayerPrefs.SetInt(GetLevelKey(type), GetLevel(type) + 1);
            PlayerPrefs.Save();
            return true;
        }

        public static void ApplyToPlayer(MainPlayerUnit mainPlayerUnit, PlayerController playerController)
        {
            if (mainPlayerUnit == null)
            {
                return;
            }

            mainPlayerUnit.SetDamage(mainPlayerUnit.Damage + GetBonus(PlayerMetaUpgradeType.Damage));
            mainPlayerUnit.SetFireRate(mainPlayerUnit.FireRate + GetBonus(PlayerMetaUpgradeType.FireRate));
            mainPlayerUnit.SetMaxHp(mainPlayerUnit.MaxHp + GetBonus(PlayerMetaUpgradeType.MaxHp), healByDelta: true);
            mainPlayerUnit.RestoreFullHealth();

            if (mainPlayerUnit.BulletSpawner != null)
            {
                int projectileBonus = Mathf.RoundToInt(GetBonus(PlayerMetaUpgradeType.ProjectileCount));
                mainPlayerUnit.BulletSpawner.SetProjectileCount(mainPlayerUnit.BulletSpawner.ProjectileCount + projectileBonus);
            }

            if (playerController != null && playerController.PlayerMovement != null)
            {
                playerController.PlayerMovement.SetMoveSpeed(playerController.PlayerMovement.MoveSpeed + GetBonus(PlayerMetaUpgradeType.MoveSpeed));
            }
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

            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        private static string GetLevelKey(PlayerMetaUpgradeType type)
        {
            return LevelKeyPrefix + type;
        }
    }

    public enum PlayerMetaUpgradeType
    {
        Damage,
        FireRate,
        MoveSpeed,
        MaxHp,
        ProjectileCount
    }

    public readonly struct UpgradeDefinition
    {
        public readonly PlayerMetaUpgradeType Type;
        public readonly string DisplayName;
        public readonly int BaseCost;
        public readonly float ValuePerLevel;
        public readonly string ValueFormat;

        public UpgradeDefinition(
            PlayerMetaUpgradeType type,
            string displayName,
            int baseCost,
            float valuePerLevel,
            string valueFormat)
        {
            Type = type;
            DisplayName = displayName;
            BaseCost = baseCost;
            ValuePerLevel = valuePerLevel;
            ValueFormat = valueFormat;
        }
    }
}
