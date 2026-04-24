using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Projectile spawner owned by a player unit.
    /// Stores runtime stats and modifier configs so external systems can upgrade behavior without changing firing code.
    /// </summary>
    public sealed class BulletSpawner : MonoBehaviour
    {
        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 4f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private float bulletSpeed = 12f;
        [SerializeField] private List<BulletModifierConfig> defaultModifierConfigs = new List<BulletModifierConfig>();

        private readonly List<BulletModifierConfig> _runtimeModifierConfigs = new List<BulletModifierConfig>();
        private readonly List<BulletModifierConfig> _activeModifierBuffer = new List<BulletModifierConfig>();
        private float _nextShotTime;

        public float FireRate => fireRate;
        public float Damage => damage;
        public float BulletSpeed => bulletSpeed;

        public void Initialize(float initialDamage, float initialFireRate)
        {
            damage = Mathf.Max(0f, initialDamage);
            fireRate = Mathf.Max(0f, initialFireRate);
        }

        public void SetDamage(float value)
        {
            damage = Mathf.Max(0f, value);
        }

        public void SetFireRate(float value)
        {
            fireRate = Mathf.Max(0f, value);
        }

        public void SetBulletSpeed(float value)
        {
            bulletSpeed = Mathf.Max(0f, value);
        }

        public void AddModifier(BulletModifierConfig modifierConfig)
        {
            if (modifierConfig == null)
            {
                return;
            }

            _runtimeModifierConfigs.Add(modifierConfig);
        }

        public void RemoveModifier(BulletModifierConfig modifierConfig)
        {
            if (modifierConfig == null)
            {
                return;
            }

            _runtimeModifierConfigs.Remove(modifierConfig);
        }

        public void ClearModifiers()
        {
            _runtimeModifierConfigs.Clear();
        }

        public void Shoot()
        {
            if (!CanShoot())
            {
                return;
            }

            Transform spawnPoint = firePoint != null ? firePoint : transform;
            SpawnBullet(spawnPoint.position, spawnPoint.rotation, damage, bulletSpeed, BuildModifierConfigBuffer());
            _nextShotTime = Time.time + GetShotInterval();
        }

        public void SpawnChildBullet(
            Vector3 position,
            Vector3 direction,
            float childDamage,
            float childSpeed,
            IReadOnlyList<BulletModifierConfig> sourceConfigs,
            BulletModifierConfig excludedModifier)
        {
            if (bulletPrefab == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction.normalized);
            SpawnBullet(position, rotation, childDamage, childSpeed, BuildModifierConfigBuffer(sourceConfigs, excludedModifier));
        }

        private bool CanShoot()
        {
            return bulletPrefab != null && fireRate > 0f && Time.time >= _nextShotTime;
        }

        private float GetShotInterval()
        {
            return fireRate <= 0f ? float.MaxValue : 1f / fireRate;
        }

        private Bullet SpawnBullet(
            Vector3 position,
            Quaternion rotation,
            float bulletDamage,
            float projectileSpeed,
            IReadOnlyList<BulletModifierConfig> modifierConfigs)
        {
            if (bulletPrefab == null)
            {
                return null;
            }

            Bullet spawnedBullet = Instantiate(bulletPrefab, position, rotation);
            spawnedBullet.Init(bulletDamage, projectileSpeed);
            spawnedBullet.Configure(this, modifierConfigs);
            spawnedBullet.Spawn();
            return spawnedBullet;
        }

        private IReadOnlyList<BulletModifierConfig> BuildModifierConfigBuffer()
        {
            _activeModifierBuffer.Clear();
            _activeModifierBuffer.AddRange(defaultModifierConfigs);
            _activeModifierBuffer.AddRange(_runtimeModifierConfigs);
            return _activeModifierBuffer;
        }

        private IReadOnlyList<BulletModifierConfig> BuildModifierConfigBuffer(
            IReadOnlyList<BulletModifierConfig> sourceConfigs,
            BulletModifierConfig excludedModifier)
        {
            _activeModifierBuffer.Clear();

            if (sourceConfigs == null)
            {
                return _activeModifierBuffer;
            }

            for (int index = 0; index < sourceConfigs.Count; index++)
            {
                BulletModifierConfig config = sourceConfigs[index];

                if (config == null || config == excludedModifier)
                {
                    continue;
                }

                _activeModifierBuffer.Add(config);
            }

            return _activeModifierBuffer;
        }
    }
}
