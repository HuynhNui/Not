using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    public readonly struct EnemyRunScaling
    {
        public static readonly EnemyRunScaling Identity = new EnemyRunScaling(1f, 1f, 1f, 1f);

        public readonly float HealthMultiplier;
        public readonly float MoveSpeedMultiplier;
        public readonly float DamageMultiplier;
        public readonly float ProjectileSpeedMultiplier;

        public EnemyRunScaling(
            float healthMultiplier,
            float moveSpeedMultiplier,
            float damageMultiplier,
            float projectileSpeedMultiplier)
        {
            HealthMultiplier = Mathf.Max(0.01f, healthMultiplier);
            MoveSpeedMultiplier = Mathf.Max(0f, moveSpeedMultiplier);
            DamageMultiplier = Mathf.Max(0f, damageMultiplier);
            ProjectileSpeedMultiplier = Mathf.Max(0f, projectileSpeedMultiplier);
        }
    }

    public readonly struct EnemyRuntimeStats
    {
        public readonly float MaxHealth;
        public readonly float MoveSpeed;
        public readonly float ContactDamage;
        public readonly int ScoreValue;
        public readonly int CoinReward;
        public readonly bool DestroyOnPlayerHit;

        public EnemyRuntimeStats(
            float maxHealth,
            float moveSpeed,
            float contactDamage,
            int scoreValue,
            int coinReward,
            bool destroyOnPlayerHit)
        {
            MaxHealth = Mathf.Max(0.01f, maxHealth);
            MoveSpeed = Mathf.Max(0f, moveSpeed);
            ContactDamage = Mathf.Max(0f, contactDamage);
            ScoreValue = Mathf.Max(0, scoreValue);
            CoinReward = Mathf.Max(0, coinReward);
            DestroyOnPlayerHit = destroyOnPlayerHit;
        }

        public EnemyRuntimeStats Scale(EnemyRunScaling scaling)
        {
            return new EnemyRuntimeStats(
                MaxHealth * scaling.HealthMultiplier,
                MoveSpeed * scaling.MoveSpeedMultiplier,
                ContactDamage * scaling.DamageMultiplier,
                ScoreValue,
                CoinReward,
                DestroyOnPlayerHit);
        }
    }

    public interface IEnemyRuntimeTunable
    {
        void ApplyRunScaling(EnemyRunScaling scaling);
    }
}
