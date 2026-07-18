using _Project.Scripts.Data.Balance;
using _Project.Scripts.Systems.Balance;
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
        [SerializeField] private CombatScalingConfig combatScalingConfig;
        [SerializeField] private int projectileSortingOrderOffset = 500;
        [SerializeField] private List<BulletVisualTier> visualTiers = new List<BulletVisualTier>();
        [SerializeField] private List<BulletModifierConfig> defaultModifierConfigs = new List<BulletModifierConfig>();

        private static readonly string[] OfficialBulletPrefabNames =
        {
            "Bullet_Tier_00",
            "Bullet_Tier_10",
            "Bullet_Tier_20",
            "Bullet_Tier_50",
            "Bullet_Tier_100"
        };

        private readonly List<BulletModifierConfig> _runtimeModifierConfigs = new List<BulletModifierConfig>();
        private readonly List<BulletModifierConfig> _activeModifierBuffer = new List<BulletModifierConfig>();
        private float _nextShotTime;
        private float _shooterDamageScale = 1f;

        public event Action<BulletSpawner, int> VolleyFired;

        public float FireRate => fireRate;
        public float EffectiveFireRate => BalanceV1Math.EffectiveFireRate(fireRate, combatScalingConfig);
        public float Damage => damage;
        public float DamagePerProjectile => Mathf.Max(0f, damage) * _shooterDamageScale;
        public float BulletSpeed => bulletSpeed;
        public int ProjectileCount => projectileCount;
        public float VisualTierDamage => visualTierDamage;
        public float ShooterDamageScale => _shooterDamageScale;
        public CombatScalingConfig CurrentCombatScalingConfig => combatScalingConfig;
        public Transform FirePoint => firePoint;

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
            combatScalingConfig = template.combatScalingConfig;
            projectileSortingOrderOffset = template.projectileSortingOrderOffset;
            _shooterDamageScale = template._shooterDamageScale;

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

        public void SetCombatScalingConfig(CombatScalingConfig value)
        {
            combatScalingConfig = value;
        }

        public void SetShooterDamageScale(float value)
        {
            _shooterDamageScale = Mathf.Max(0f, value);
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
            if (firePoint == null)
            {
                Debug.LogError($"{name}: BulletSpawner requires a FirePoint reference.", this);
                return;
            }

            if (!CanShoot())
            {
                return;
            }

            Quaternion rotation = forceVerticalDirection
                ? Quaternion.LookRotation(Vector3.forward, Vector3.up)
                : firePoint.rotation;

            int shots = Mathf.Max(1, projectileCount);
            float shotDamage = DamagePerProjectile;
            float startOffset = -(shots - 1) * 0.5f * burstSpread;
            Vector3 center = firePoint.position;

            int spawnedCount = 0;
            for (int shotIndex = 0; shotIndex < shots; shotIndex++)
            {
                Vector3 shotPosition = center + Vector3.right * (startOffset + shotIndex * burstSpread);
                if (SpawnBullet(shotPosition, rotation, shotDamage, bulletSpeed, BuildModifierConfigBuffer()) != null)
                {
                    spawnedCount++;
                }
            }

            _nextShotTime = Time.time + GetShotInterval();
            if (spawnedCount > 0)
            {
                VolleyFired?.Invoke(this, spawnedCount);
            }
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
            return GetBulletPrefabForCurrentTier() != null
                && EffectiveFireRate > 0f
                && Time.time >= _nextShotTime;
        }

        private float GetShotInterval()
        {
            float effectiveFireRate = EffectiveFireRate;
            return effectiveFireRate <= 0f ? float.MaxValue : 1f / effectiveFireRate;
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
            ApplyProjectileSorting(spawnedBullet);
            spawnedBullet.Spawn();
            return spawnedBullet;
        }

        private void ApplyProjectileSorting(Bullet spawnedBullet)
        {
            if (spawnedBullet == null)
            {
                return;
            }

            SpriteRenderer shooterRenderer = GetComponent<SpriteRenderer>();
            if (shooterRenderer == null)
            {
                return;
            }

            SpriteRenderer[] projectileRenderers = spawnedBullet.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < projectileRenderers.Length; index++)
            {
                SpriteRenderer projectileRenderer = projectileRenderers[index];
                if (projectileRenderer == null)
                {
                    continue;
                }

                projectileRenderer.sortingLayerID = shooterRenderer.sortingLayerID;
                projectileRenderer.sortingOrder = shooterRenderer.sortingOrder + projectileSortingOrderOffset;
            }
        }

        public int ResolveVisualTierIndex(float visualDamage)
        {
            int selectedIndex = -1;
            float selectedMinDamage = float.NegativeInfinity;
            float safeVisualDamage = Mathf.Max(0f, visualDamage);

            for (int index = 0; index < visualTiers.Count; index++)
            {
                BulletVisualTier tier = visualTiers[index];

                if (tier == null || tier.BulletPrefab == null || safeVisualDamage < tier.MinDamage)
                {
                    continue;
                }

                if (tier.MinDamage < selectedMinDamage)
                {
                    continue;
                }

                selectedMinDamage = tier.MinDamage;
                selectedIndex = index;
            }

            return selectedIndex;
        }

        private Bullet GetBulletPrefabForCurrentTier()
        {
            int tierIndex = ResolveVisualTierIndex(visualTierDamage);
            if (tierIndex >= 0 && tierIndex < visualTiers.Count)
            {
                return visualTiers[tierIndex].BulletPrefab;
            }

            Bullet lowestTierPrefab = GetLowestValidVisualTierPrefab();
            return lowestTierPrefab != null ? lowestTierPrefab : bulletPrefab;
        }

        private Bullet GetLowestValidVisualTierPrefab()
        {
            Bullet selectedPrefab = null;
            float selectedMinDamage = float.PositiveInfinity;

            for (int index = 0; index < visualTiers.Count; index++)
            {
                BulletVisualTier tier = visualTiers[index];
                if (tier == null || tier.BulletPrefab == null)
                {
                    continue;
                }

                if (tier.MinDamage >= selectedMinDamage)
                {
                    continue;
                }

                selectedMinDamage = tier.MinDamage;
                selectedPrefab = tier.BulletPrefab;
            }

            return selectedPrefab;
        }

        private void OnValidate()
        {
            ValidateOfficialBulletPrefab(bulletPrefab, "bulletPrefab");

            for (int index = 0; index < visualTiers.Count; index++)
            {
                BulletVisualTier tier = visualTiers[index];
                ValidateOfficialBulletPrefab(
                    tier != null ? tier.BulletPrefab : null,
                    $"visualTiers[{index}].bulletPrefab");
            }
        }

        private void ValidateOfficialBulletPrefab(Bullet prefab, string fieldName)
        {
            if (prefab == null || IsOfficialBulletPrefabName(prefab.name))
            {
                return;
            }

            Debug.LogError(
                $"{name}: {fieldName} must reference an official Bullet_Tier_X prefab, not '{prefab.name}'.",
                this);
        }

        private static bool IsOfficialBulletPrefabName(string prefabName)
        {
            for (int index = 0; index < OfficialBulletPrefabNames.Length; index++)
            {
                if (string.Equals(prefabName, OfficialBulletPrefabNames[index], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
