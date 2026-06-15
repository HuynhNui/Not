using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "EnemyRoleConfig_v1",
        menuName = "Chibi Pixel Gate/Balance/Enemy Role Config")]
    public sealed class EnemyRoleConfig : ScriptableObject
    {
        [SerializeField] private string configVersion = CombatScalingConfig.DefaultConfigVersion;
        [SerializeField] private BalanceEnemyRole role;
        [SerializeField] private string enemyId = "basic";
        [SerializeField] private float baseHp = 2f;
        [SerializeField] private float baseSpeed = 2.8f;
        [SerializeField] private float baseDamage = 0.5f;
        [SerializeField] private float rewardPoints = 0.2f;
        [SerializeField] private int scoreValue = 1;
        [SerializeField] private float unlockTimeSeconds;
        [SerializeField] private float threatCost;
        [SerializeField] private float spawnWeightMultiplier = 1f;
        [SerializeField] private string poolKey = "enemy_basic";

        public string ConfigVersion => configVersion;
        public BalanceEnemyRole Role => role;
        public string EnemyId => enemyId;
        public float BaseHp => baseHp;
        public float BaseSpeed => baseSpeed;
        public float BaseDamage => baseDamage;
        public float RewardPoints => rewardPoints;
        public int ScoreValue => scoreValue;
        public float UnlockTimeSeconds => unlockTimeSeconds;
        public float ThreatCost => threatCost;
        public float SpawnWeightMultiplier => spawnWeightMultiplier;
        public string PoolKey => poolKey;

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(configVersion))
            {
                configVersion = CombatScalingConfig.DefaultConfigVersion;
            }

            enemyId = string.IsNullOrWhiteSpace(enemyId) ? role.ToString().ToLowerInvariant() : enemyId.Trim();
            poolKey = string.IsNullOrWhiteSpace(poolKey) ? $"enemy_{enemyId}" : poolKey.Trim();
            baseHp = Mathf.Max(0.01f, baseHp);
            baseSpeed = Mathf.Max(0f, baseSpeed);
            baseDamage = Mathf.Max(0f, baseDamage);
            rewardPoints = Mathf.Max(0f, rewardPoints);
            scoreValue = Mathf.Max(0, scoreValue);
            unlockTimeSeconds = Mathf.Max(0f, unlockTimeSeconds);
            threatCost = Mathf.Max(0f, threatCost);
            spawnWeightMultiplier = Mathf.Max(0f, spawnWeightMultiplier);
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }

    public enum BalanceEnemyRole
    {
        Basic = 0,
        Chomboom = 1,
        Vomfy = 2,
        Swarmer = 3,
        Tanker = 4,
        Elite = 5
    }

    public static class EnemyRoleBalanceDefaults
    {
        public static float GetUnlockTimeSeconds(BalanceEnemyRole role)
        {
            return role switch
            {
                BalanceEnemyRole.Chomboom => 30f,
                BalanceEnemyRole.Vomfy => 90f,
                BalanceEnemyRole.Swarmer => 120f,
                BalanceEnemyRole.Elite => 180f,
                BalanceEnemyRole.Tanker => 210f,
                _ => 0f
            };
        }

        public static float GetThreatCost(BalanceEnemyRole role)
        {
            return role switch
            {
                BalanceEnemyRole.Swarmer => 0.25f,
                BalanceEnemyRole.Chomboom => 1.5f,
                BalanceEnemyRole.Vomfy => 2f,
                BalanceEnemyRole.Tanker => 3f,
                BalanceEnemyRole.Elite => 8f,
                _ => 0f
            };
        }

        public static float GetRewardPoints(BalanceEnemyRole role)
        {
            return role switch
            {
                BalanceEnemyRole.Swarmer => 0.1f,
                BalanceEnemyRole.Chomboom => 0.75f,
                BalanceEnemyRole.Vomfy => 1f,
                BalanceEnemyRole.Tanker => 2f,
                BalanceEnemyRole.Elite => 15f,
                _ => 0.2f
            };
        }

        public static bool CanFitThreat(float currentThreat, float candidateThreat, float threatBudget)
        {
            float candidate = Mathf.Max(0f, candidateThreat);
            return candidate <= 0f
                || Mathf.Max(0f, currentThreat) + candidate <= Mathf.Max(0f, threatBudget) + 0.0001f;
        }
    }
}
