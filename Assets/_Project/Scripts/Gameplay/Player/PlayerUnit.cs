using _Project.Scripts.Data.ScriptableObjects.PlayerConfigs;
using _Project.Scripts.Gameplay.Combat;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Shared runtime logic for any player-controlled squad member.
    /// Input stays outside of this class so main and follower units can share combat behavior.
    /// </summary>
    public class PlayerUnit : MonoBehaviour
    {
        [SerializeField] private PlayerUnitConfig unitConfig;
        [SerializeField] private float damage = 1f;
        [SerializeField] private float fireRate = 4f;
        [SerializeField] private BulletSpawner bulletSpawner;

        protected bool IsInitialized { get; private set; }

        public float Damage => damage;
        public float FireRate => fireRate;
        public BulletSpawner BulletSpawner => bulletSpawner;

        protected virtual void Awake()
        {
            Initialize();
        }

        public virtual void Initialize()
        {
            ApplyUnitConfig();

            if (IsInitialized)
            {
                SyncSpawnerStats();
                return;
            }

            IsInitialized = true;
            SyncSpawnerStats();
        }

        public virtual void Shoot()
        {
            if (bulletSpawner == null)
            {
                return;
            }

            bulletSpawner.Shoot();
        }

        public virtual void SetDamage(float value)
        {
            damage = Mathf.Max(0f, value);
            SyncSpawnerStats();
        }

        public virtual void SetFireRate(float value)
        {
            fireRate = Mathf.Max(0f, value);
            SyncSpawnerStats();
        }

        public void AddBulletModifier(BulletModifierConfig modifierConfig)
        {
            if (bulletSpawner == null)
            {
                return;
            }

            bulletSpawner.AddModifier(modifierConfig);
        }

        public void RemoveBulletModifier(BulletModifierConfig modifierConfig)
        {
            if (bulletSpawner == null)
            {
                return;
            }

            bulletSpawner.RemoveModifier(modifierConfig);
        }

        public void ClearBulletModifiers()
        {
            if (bulletSpawner == null)
            {
                return;
            }

            bulletSpawner.ClearModifiers();
        }

        protected void AssignBulletSpawner(BulletSpawner spawner)
        {
            bulletSpawner = spawner;
            SyncSpawnerStats();
        }

        protected void SyncSpawnerStats()
        {
            if (bulletSpawner == null)
            {
                return;
            }

            bulletSpawner.Initialize(damage, fireRate);

            if (unitConfig != null)
            {
                bulletSpawner.SetBulletSpeed(unitConfig.BulletSpeed);
            }
        }

        private void ApplyUnitConfig()
        {
            if (unitConfig == null)
            {
                return;
            }

            damage = Mathf.Max(0f, unitConfig.Damage);
            fireRate = Mathf.Max(0f, unitConfig.FireRate);
        }
    }
}
