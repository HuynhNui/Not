using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "CombatScalingConfig_v1",
        menuName = "Chibi Pixel Gate/Balance/Combat Scaling Config")]
    public sealed class CombatScalingConfig : ScriptableObject
    {
        public const string DefaultConfigVersion = "balance-v1.0.0";

        [SerializeField] private string configVersion = DefaultConfigVersion;
        [SerializeField] private float fireSoftCapStart = 6f;
        [SerializeField] private float fireSoftCapMax = 18f;
        [SerializeField] private int baseProjectileCount = 5;
        [SerializeField] private float projectileCoverageCoefficient = 0.15f;
        [SerializeField] private float squadCoverageCoefficient = 0.45f;
        [SerializeField, Range(0f, 1f)] private float followerHpRatio = 0.25f;
        [SerializeField, Range(0f, 1f)] private float recruitSpawnHpRatio = 0.5f;

        public string ConfigVersion => configVersion;
        public float FireSoftCapStart => fireSoftCapStart;
        public float FireSoftCapMax => fireSoftCapMax;
        public int BaseProjectileCount => baseProjectileCount;
        public float ProjectileCoverageCoefficient => projectileCoverageCoefficient;
        public float SquadCoverageCoefficient => squadCoverageCoefficient;
        public float FollowerHpRatio => followerHpRatio;
        public float RecruitSpawnHpRatio => recruitSpawnHpRatio;

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(configVersion))
            {
                configVersion = DefaultConfigVersion;
            }

            fireSoftCapStart = Mathf.Max(0f, fireSoftCapStart);
            fireSoftCapMax = Mathf.Max(fireSoftCapStart, fireSoftCapMax);
            baseProjectileCount = Mathf.Max(1, baseProjectileCount);
            projectileCoverageCoefficient = Mathf.Max(0f, projectileCoverageCoefficient);
            squadCoverageCoefficient = Mathf.Max(0f, squadCoverageCoefficient);
            followerHpRatio = Mathf.Clamp01(followerHpRatio);
            recruitSpawnHpRatio = Mathf.Clamp01(recruitSpawnHpRatio);
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }
}
