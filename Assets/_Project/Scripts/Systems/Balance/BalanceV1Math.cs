using _Project.Scripts.Data.Balance;
using UnityEngine;

namespace _Project.Scripts.Systems.Balance
{
    public static class BalanceV1Math
    {
        public const float DefaultFireSoftCapStart = 6f;
        public const float DefaultFireSoftCapMax = 18f;
        public const int DefaultBaseProjectileCount = 5;
        public const float DefaultProjectileCoverageCoefficient = 0.15f;
        public const float DefaultSquadCoverageCoefficient = 0.45f;

        public static float EffectiveFireRate(
            float rawFireRate,
            float softCapStart = DefaultFireSoftCapStart,
            float softCapMax = DefaultFireSoftCapMax)
        {
            float safeRaw = Mathf.Max(0f, rawFireRate);
            float safeStart = Mathf.Max(0f, softCapStart);
            float safeMax = Mathf.Max(safeStart, softCapMax);

            if (safeRaw <= safeStart || Mathf.Approximately(safeStart, safeMax))
            {
                return Mathf.Min(safeRaw, safeMax);
            }

            float range = safeMax - safeStart;
            float excess = safeRaw - safeStart;
            return safeStart + range * excess / (range + excess);
        }

        public static float EffectiveFireRate(float rawFireRate, CombatScalingConfig config)
        {
            return config == null
                ? EffectiveFireRate(rawFireRate)
                : EffectiveFireRate(rawFireRate, config.FireSoftCapStart, config.FireSoftCapMax);
        }

        public static float ProjectileFactor(
            int projectileCount,
            int baseProjectileCount = DefaultBaseProjectileCount,
            float coverageCoefficient = DefaultProjectileCoverageCoefficient)
        {
            int safeCount = Mathf.Max(1, projectileCount);
            int safeBaseCount = Mathf.Max(1, baseProjectileCount);
            float safeCoefficient = Mathf.Max(0f, coverageCoefficient);
            return Mathf.Max(
                0.01f,
                1f + safeCoefficient * (Mathf.Sqrt(safeCount) - Mathf.Sqrt(safeBaseCount)));
        }

        public static float ProjectileFactor(int projectileCount, CombatScalingConfig config)
        {
            return config == null
                ? ProjectileFactor(projectileCount)
                : ProjectileFactor(
                    projectileCount,
                    config.BaseProjectileCount,
                    config.ProjectileCoverageCoefficient);
        }

        public static float SquadFactor(
            int squadCount,
            float coverageCoefficient = DefaultSquadCoverageCoefficient)
        {
            int safeCount = Mathf.Max(1, squadCount);
            float safeCoefficient = Mathf.Max(0f, coverageCoefficient);
            return 1f + safeCoefficient * Mathf.Sqrt(safeCount - 1f);
        }

        public static float SquadFactor(int squadCount, CombatScalingConfig config)
        {
            return config == null
                ? SquadFactor(squadCount)
                : SquadFactor(squadCount, config.SquadCoverageCoefficient);
        }

        public static float FollowerDamageScale(
            int squadCount,
            float coverageCoefficient = DefaultSquadCoverageCoefficient)
        {
            int safeCount = Mathf.Max(1, squadCount);
            if (safeCount <= 1)
            {
                return 0f;
            }

            return (SquadFactor(safeCount, coverageCoefficient) - 1f) / (safeCount - 1f);
        }

        public static float FollowerDamageScale(int squadCount, CombatScalingConfig config)
        {
            return config == null
                ? FollowerDamageScale(squadCount)
                : FollowerDamageScale(squadCount, config.SquadCoverageCoefficient);
        }

        public static float DamagePerMainBullet(
            float damage,
            int projectileCount,
            int baseProjectileCount = DefaultBaseProjectileCount,
            float coverageCoefficient = DefaultProjectileCoverageCoefficient)
        {
            int safeCount = Mathf.Max(1, projectileCount);
            float safeDamage = Mathf.Max(0f, damage);
            float volleyDamage = safeDamage
                * Mathf.Max(1, baseProjectileCount)
                * ProjectileFactor(safeCount, baseProjectileCount, coverageCoefficient);
            return volleyDamage / safeCount;
        }

        public static float DamagePerMainBullet(
            float damage,
            int projectileCount,
            CombatScalingConfig config)
        {
            return config == null
                ? DamagePerMainBullet(damage, projectileCount)
                : DamagePerMainBullet(
                    damage,
                    projectileCount,
                    config.BaseProjectileCount,
                    config.ProjectileCoverageCoefficient);
        }

        public static float EffectiveDps(
            float damage,
            float rawFireRate,
            int projectileCount,
            int squadCount,
            float fireSoftCapStart = DefaultFireSoftCapStart,
            float fireSoftCapMax = DefaultFireSoftCapMax,
            int baseProjectileCount = DefaultBaseProjectileCount,
            float projectileCoverageCoefficient = DefaultProjectileCoverageCoefficient,
            float squadCoverageCoefficient = DefaultSquadCoverageCoefficient)
        {
            return Mathf.Max(0f, damage)
                * EffectiveFireRate(rawFireRate, fireSoftCapStart, fireSoftCapMax)
                * Mathf.Max(1, baseProjectileCount)
                * ProjectileFactor(
                    projectileCount,
                    baseProjectileCount,
                    projectileCoverageCoefficient)
                * SquadFactor(squadCount, squadCoverageCoefficient);
        }

        public static float EffectiveDps(
            float damage,
            float rawFireRate,
            int projectileCount,
            int squadCount,
            CombatScalingConfig config)
        {
            return config == null
                ? EffectiveDps(damage, rawFireRate, projectileCount, squadCount)
                : EffectiveDps(
                    damage,
                    rawFireRate,
                    projectileCount,
                    squadCount,
                    config.FireSoftCapStart,
                    config.FireSoftCapMax,
                    config.BaseProjectileCount,
                    config.ProjectileCoverageCoefficient,
                    config.SquadCoverageCoefficient);
        }

        public static float SquadDurabilityFactor(int squadCount, float followerHpRatio)
        {
            return 1f + Mathf.Max(0f, followerHpRatio) * Mathf.Max(0, squadCount - 1);
        }
    }
}
