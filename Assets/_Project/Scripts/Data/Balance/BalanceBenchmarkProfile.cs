using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    public enum BalanceBenchmarkPreset
    {
        FullMetaStart,
        OldRunCapStart,
        DamageForwardCapStart
    }

    [CreateAssetMenu(
        fileName = "BalanceBenchmarkProfile",
        menuName = "Chibi Pixel Gate/Balance/Balance Benchmark Profile")]
    public sealed class BalanceBenchmarkProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "full-meta-elite-squad-v1";
        [SerializeField] private bool enabled = true;
        [SerializeField] private float startingDamage = 3.00f;
        [SerializeField] private float startingFireRate = 6.40f;
        [SerializeField] private float startingMaxHp = 20f;
        [SerializeField] private int startingProjectileCount = 3;
        [SerializeField] private int startingSquadSize = 4;
        [SerializeField] private bool suppressSaveCommit = true;
        [SerializeField] private bool suppressWalletReward = true;
        [SerializeField] private bool suppressStoryProgress = true;
        [SerializeField] private bool suppressTutorialProgress = true;

        public string ProfileId => profileId;
        public bool Enabled => enabled;
        public float StartingDamage => startingDamage;
        public float StartingFireRate => startingFireRate;
        public float StartingMaxHp => startingMaxHp;
        public int StartingProjectileCount => startingProjectileCount;
        public int StartingSquadSize => startingSquadSize;
        public bool SuppressSaveCommit => suppressSaveCommit;
        public bool SuppressWalletReward => suppressWalletReward;
        public bool SuppressStoryProgress => suppressStoryProgress;
        public bool SuppressTutorialProgress => suppressTutorialProgress;

        public bool IsActive => enabled && (Application.isEditor || Debug.isDebugBuild);

        public PlayerRunStartStats ToRunStartStats()
        {
            return new PlayerRunStartStats(
                startingDamage,
                startingFireRate,
                startingMaxHp,
                startingProjectileCount,
                startingSquadSize);
        }

        private void OnValidate()
        {
            profileId = string.IsNullOrWhiteSpace(profileId)
                ? "full-meta-elite-squad-v1"
                : profileId.Trim();
            startingDamage = Mathf.Max(0.01f, startingDamage);
            startingFireRate = Mathf.Max(0.01f, startingFireRate);
            startingMaxHp = Mathf.Max(1f, startingMaxHp);
            startingProjectileCount = Mathf.Max(1, startingProjectileCount);
            startingSquadSize = Mathf.Max(1, startingSquadSize);
        }
    }

    public readonly struct PlayerRunStartStats
    {
        public readonly float Damage;
        public readonly float FireRate;
        public readonly float MaxHp;
        public readonly int ProjectileCount;
        public readonly int SquadSize;

        public PlayerRunStartStats(
            float damage,
            float fireRate,
            float maxHp,
            int projectileCount,
            int squadSize)
        {
            Damage = Mathf.Max(0.01f, damage);
            FireRate = Mathf.Max(0.01f, fireRate);
            MaxHp = Mathf.Max(1f, maxHp);
            ProjectileCount = Mathf.Max(1, projectileCount);
            SquadSize = Mathf.Max(1, squadSize);
        }
    }
}
