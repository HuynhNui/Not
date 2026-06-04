using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Combat;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Gates
{
    /// <summary>
    /// Applies gate/door config changes to the player squad (main unit + followers).
    /// </summary>
    public static class GateEffectApplier
    {
        private const int MaxProjectileCount = 50;
        private const int MaxPlayerCount = 50;

        public static void Apply(GateConfig config, MainPlayerUnit mainUnit, PlayerController squad)
        {
            if (config == null || mainUnit == null)
            {
                return;
            }

            switch (config.StatTarget)
            {
                case GateStatTarget.Damage:
                    ApplyToSquadDamage(config, mainUnit, squad);
                    break;
                case GateStatTarget.FireRate:
                    ApplyToSquadFireRate(config, mainUnit, squad);
                    break;
                case GateStatTarget.MaxHp:
                    ApplyMaxHp(config, mainUnit, squad);
                    break;
                case GateStatTarget.ProjectileCount:
                    ApplyWeaponEffect(config, mainUnit, squad);
                    break;
                case GateStatTarget.PlayerCount:
                    ApplyPlayerCount(config, squad);
                    break;
            }
        }

        private static void ApplyToSquadDamage(GateConfig config, MainPlayerUnit mainUnit, PlayerController squad)
        {
            float newDamage = ApplyOperation(mainUnit.Damage, config.OperationType, config.Amount);
            mainUnit.SetDamage(newDamage);
            squad?.SyncFollowersFromMain(
                syncDamage: true,
                syncFireRate: false,
                syncMaxHp: false,
                healMaxHpByDelta: false,
                syncProjectileCount: false);
        }

        private static void ApplyToSquadFireRate(GateConfig config, MainPlayerUnit mainUnit, PlayerController squad)
        {
            float newFireRate = ApplyOperation(mainUnit.FireRate, config.OperationType, config.Amount);
            mainUnit.SetFireRate(newFireRate);
            squad?.SyncFollowersFromMain(
                syncDamage: false,
                syncFireRate: true,
                syncMaxHp: false,
                healMaxHpByDelta: false,
                syncProjectileCount: false);
        }

        private static void ApplyMaxHp(GateConfig config, MainPlayerUnit mainUnit, PlayerController squad)
        {
            float newMaxHp = ApplyOperation(mainUnit.MaxHp, config.OperationType, config.Amount);
            mainUnit.SetMaxHp(newMaxHp, healByDelta: config.OperationType == GateOperationType.Add);
            squad?.SyncFollowersFromMain(
                syncDamage: false,
                syncFireRate: false,
                syncMaxHp: true,
                healMaxHpByDelta: config.OperationType == GateOperationType.Add,
                syncProjectileCount: false);
        }

        private static void ApplyWeaponEffect(GateConfig config, MainPlayerUnit mainUnit, PlayerController squad)
        {
            switch (config.StatTarget)
            {
                case GateStatTarget.ProjectileCount:
                    ApplyProjectileCount(config, mainUnit, squad);
                    break;
            }
        }

        private static void ApplyProjectileCount(GateConfig config, MainPlayerUnit mainUnit, PlayerController squad)
        {
            BulletSpawner spawner = mainUnit.BulletSpawner;
            if (spawner == null)
            {
                return;
            }

            int current = spawner.ProjectileCount;
            int next = Mathf.RoundToInt(ApplyOperation(current, config.OperationType, config.Amount));
            int clampedNext = Mathf.Clamp(next, 1, MaxProjectileCount);
            spawner.SetProjectileCount(clampedNext);
            squad?.SyncFollowersFromMain(
                syncDamage: false,
                syncFireRate: false,
                syncMaxHp: false,
                healMaxHpByDelta: false,
                syncProjectileCount: true);
        }

        private static void ApplyPlayerCount(GateConfig config, PlayerController squad)
        {
            if (squad == null)
            {
                return;
            }

            int current = Mathf.Max(1, squad.CurrentSquadCount);
            int next = Mathf.RoundToInt(ApplyOperation(current, config.OperationType, config.Amount));
            squad.SetSquadCount(Mathf.Clamp(next, 1, MaxPlayerCount));
        }

        private static float ApplyOperation(float baseValue, GateOperationType operationType, float amount)
        {
            float safeAmount = Mathf.Abs(amount);

            return operationType switch
            {
                GateOperationType.Add => baseValue + safeAmount,
                GateOperationType.Subtract => Mathf.Max(0f, baseValue - safeAmount),
                GateOperationType.Multiply => baseValue * Mathf.Max(0f, safeAmount),
                GateOperationType.Divide => safeAmount <= 0f ? baseValue : baseValue / safeAmount,
                _ => baseValue
            };
        }
    }
}
