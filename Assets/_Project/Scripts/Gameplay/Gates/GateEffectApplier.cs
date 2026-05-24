using System.Collections.Generic;
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
                    ApplyMaxHp(config, mainUnit);
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
            SyncFollowersFromMain(mainUnit, squad, syncDamage: true, syncFireRate: false);
        }

        private static void ApplyToSquadFireRate(GateConfig config, MainPlayerUnit mainUnit, PlayerController squad)
        {
            float newFireRate = ApplyOperation(mainUnit.FireRate, config.OperationType, config.Amount);
            mainUnit.SetFireRate(newFireRate);
            SyncFollowersFromMain(mainUnit, squad, syncDamage: false, syncFireRate: true);
        }

        private static void ApplyMaxHp(GateConfig config, MainPlayerUnit mainUnit)
        {
            float newMaxHp = ApplyOperation(mainUnit.MaxHp, config.OperationType, config.Amount);
            mainUnit.SetMaxHp(newMaxHp, healByDelta: config.OperationType == GateOperationType.Add);
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
            SyncFollowersProjectileCount(squad, clampedNext);
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

        private static void SyncFollowersFromMain(
            MainPlayerUnit mainUnit,
            PlayerController squad,
            bool syncDamage,
            bool syncFireRate)
        {
            if (squad == null)
            {
                return;
            }

            IReadOnlyList<FollowerUnit> followers = squad.Followers;
            for (int index = 0; index < followers.Count; index++)
            {
                FollowerUnit follower = followers[index];
                if (follower == null)
                {
                    continue;
                }

                if (syncDamage)
                {
                    follower.SetDamage(mainUnit.Damage);
                }

                if (syncFireRate)
                {
                    follower.SetFireRate(mainUnit.FireRate);
                }
            }
        }

        private static void SyncFollowersProjectileCount(PlayerController squad, int projectileCount)
        {
            if (squad == null)
            {
                return;
            }

            IReadOnlyList<FollowerUnit> followers = squad.Followers;
            for (int index = 0; index < followers.Count; index++)
            {
                FollowerUnit follower = followers[index];
                if (follower == null || follower.BulletSpawner == null)
                {
                    continue;
                }

                follower.BulletSpawner.SetProjectileCount(projectileCount);
            }
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
