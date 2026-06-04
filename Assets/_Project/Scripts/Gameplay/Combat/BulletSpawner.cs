using _Project.Scripts.Systems.PoolSystem;
using System;
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
        [SerializeField] private int projectileCount = 1;
        [SerializeField] private float burstSpread = 0.35f;
        [SerializeField] private bool forceVerticalDirection = true;
        [SerializeField] private PoolSystem poolSystem;
        [SerializeField] private float visualTierDamage;
        [SerializeField] private List<BulletVisualTier> visualTiers = new List<BulletVisualTier>();
        [SerializeField] private List<BulletModifierConfig> defaultModifierConfigs = new List<BulletModifierConfig>();

        private readonly List<BulletModifierConfig> _runtimeModifierConfigs = new List<BulletModifierConfig>();
        private readonly List<BulletModifierConfig> _activeModifierBuffer = new List<BulletModifierConfig>();
        private float _nextShotTime;

        public float FireRate => fireRate;
        public float Damage => damage;
        public float BulletSpeed => bulletSpeed;
        public int ProjectileCount => projectileCount;
        public float VisualTierDamage => visualTierDamage;

        public void ConfigureFromTemplate(BulletSpawner template)
        {
            if (template == null)
            {
                return;
            }

            bulletPrefab = template.bulletPrefab;
            fireRate = template.fireRate;
            damage = template.damage;
            bulletSpeed = template.bulletSpeed;
            projectileCount = template.projectileCount;
            burstSpread = template.burstSpread;
            forceVerticalDirection = template.forceVerticalDirection;
            poolSystem = template.poolSystem != null ? template.poolSystem : FindAnyObjectByType<PoolSystem>();
            visualTierDamage = template.visualTierDamage;

            visualTiers.Clear();
            visualTiers.AddRange(template.visualTiers);

            defaultModifierConfigs.Clear();
            defaultModifierConfigs.AddRange(template.defaultModifierConfigs);
            _runtimeModifierConfigs.Clear();
            _runtimeModifierConfigs.AddRange(template._runtimeModifierConfigs);
        }

        public void SetFirePoint(Transform value)
        {
            firePoint = value;
        }

        public void Initialize(float initialDamage, float initialFireRate)
        {
            poolSystem ??= FindAnyObjectByType<PoolSystem>();
            damage = Mathf.Max(0f, initialDamage);
            visualTierDamage = Mathf.Max(0f, visualTierDamage);
            fireRate = Mathf.Max(0f, initialFireRate);
        }

        public void SetDamage(float value)
        {
            damage = Mathf.Max(0f, value);
        }

        public void SetVisualTierDamage(float value)
        {
            visualTierDamage = Mathf.Max(0f, value);
        }

        public void SetFireRate(float value)
        {
            fireRate = Mathf.Max(0f, value);
        }

        public void SetBulletSpeed(float value)
        {
            bulletSpeed = Mathf.Max(0f, value);
        }

        public void SetProjectileCount(int value)
        {
            projectileCount = Mathf.Max(1, value);
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
            Quaternion rotation = forceVerticalDirection
                ? Quaternion.LookRotation(Vector3.forward, Vector3.up)
                : spawnPoint.rotation;

            int shots = Mathf.Max(1, projectileCount);
            float startOffset = -(shots - 1) * 0.5f * burstSpread;

            for (int shotIndex = 0; shotIndex < shots; shotIndex++)
            {
                Vector3 shotPosition = spawnPoint.position + Vector3.right * (startOffset + shotIndex * burstSpread);
                SpawnBullet(shotPosition, rotation, damage, bulletSpeed, BuildModifierConfigBuffer());
            }

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
            if (GetBulletPrefabForCurrentTier() == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction.normalized);
            SpawnBullet(position, rotation, childDamage, childSpeed, BuildModifierConfigBuffer(sourceConfigs, excludedModifier));
        }

        private bool CanShoot()
        {
            return GetBulletPrefabForCurrentTier() != null && fireRate > 0f && Time.time >= _nextShotTime;
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
            Bullet prefab = GetBulletPrefabForCurrentTier();

            if (prefab == null)
            {
                return null;
            }

            Bullet spawnedBullet = poolSystem != null
                ? poolSystem.Spawn(prefab, position, rotation)
                : Instantiate(prefab, position, rotation);

            if (spawnedBullet == null)
            {
                return null;
            }

            spawnedBullet.SetPoolSystem(poolSystem);
            spawnedBullet.Init(bulletDamage, projectileSpeed);
            spawnedBullet.Configure(this, modifierConfigs);
            spawnedBullet.Spawn();
            return spawnedBullet;
        }

        private Bullet GetBulletPrefabForCurrentTier()
        {
            Bullet selectedPrefab = bulletPrefab;
            float selectedMinDamage = float.NegativeInfinity;

            for (int index = 0; index < visualTiers.Count; index++)
            {
                BulletVisualTier tier = visualTiers[index];

                if (tier == null || tier.BulletPrefab == null || visualTierDamage < tier.MinDamage)
                {
                    continue;
                }

                if (tier.MinDamage < selectedMinDamage)
                {
                    continue;
                }

                selectedMinDamage = tier.MinDamage;
                selectedPrefab = tier.BulletPrefab;
            }

            return selectedPrefab;
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

        [Serializable]
        private sealed class BulletVisualTier
        {
            [SerializeField] private float minDamage;
            [SerializeField] private Bullet bulletPrefab;

            public float MinDamage => Mathf.Max(0f, minDamage);
            public Bullet BulletPrefab => bulletPrefab;
        }
    }
}
