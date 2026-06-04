using _Project.Scripts.Data.ScriptableObjects.PlayerConfigs;
using _Project.Scripts.Gameplay.Combat;
using _Project.Scripts.Interfaces;
using _Project.Scripts.Systems.UISystem;
using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Shared runtime logic for any player-controlled squad member.
    /// Input stays outside of this class so main and follower units can share combat behavior.
    /// </summary>
    public class PlayerUnit : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerUnitConfig unitConfig;
        [SerializeField] private float damage = 1f;
        [SerializeField] private float fireRate = 4f;
        [SerializeField] private BulletSpawner bulletSpawner;
        [SerializeField] private float maxHp = 10f;
        [SerializeField] private WorldHealthBarView healthBarPrefab;
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 0.33f, 0f);

        private float _currentHp;
        private bool _isDead;
        private WorldHealthBarView _healthBarInstance;

        protected bool IsInitialized { get; private set; }

        public event Action<PlayerUnit> Died;

        public float Damage => damage;
        public float FireRate => fireRate;
        public BulletSpawner BulletSpawner => bulletSpawner;
        public float CurrentHp => _currentHp;
        public float MaxHp => maxHp;
        public bool IsDead => _isDead;

        protected virtual void Awake()
        {
            Initialize();
        }

        public virtual void Initialize()
        {
            if (bulletSpawner == null)
            {
                bulletSpawner = GetComponent<BulletSpawner>();
            }

            ApplyUnitConfig();

            if (IsInitialized)
            {
                SyncSpawnerStats();
                EnsureHealthBar();
                RefreshHealthBar();
                return;
            }

            IsInitialized = true;
            SyncSpawnerStats();
            EnsureHealthBar();

            if (_currentHp <= 0f && !_isDead)
            {
                _currentHp = Mathf.Max(1f, maxHp);
            }

            RefreshHealthBar();
        }

        public virtual void Shoot()
        {
            if (_isDead || bulletSpawner == null)
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

        public virtual void TakeDamage(float value)
        {
            if (_isDead)
            {
                return;
            }

            _currentHp = Mathf.Max(0f, _currentHp - Mathf.Max(0f, value));
            RefreshHealthBar();

            if (_currentHp <= 0f)
            {
                Die();
            }
        }

        public void RestoreFullHealth()
        {
            SetCurrentHp(maxHp);
        }

        public void SetCurrentHp(float value)
        {
            _isDead = false;
            _currentHp = Mathf.Clamp(value, 0f, maxHp);

            if (_currentHp <= 0f)
            {
                _currentHp = Mathf.Max(1f, maxHp);
            }

            RefreshHealthBar();
        }

        public void SetMaxHp(float value, bool healByDelta = false)
        {
            float previousMax = maxHp;
            maxHp = Mathf.Max(1f, value);

            if (healByDelta)
            {
                _currentHp += maxHp - previousMax;
            }

            _currentHp = Mathf.Clamp(_currentHp, 0f, maxHp);

            if (_currentHp <= 0f && maxHp > 0f && !_isDead)
            {
                _currentHp = maxHp;
            }

            RefreshHealthBar();
        }

        public void ReviveWithStateFrom(PlayerUnit source)
        {
            if (source == null)
            {
                return;
            }

            transform.position = source.transform.position;
            SetDamage(source.Damage);
            SetFireRate(source.FireRate);
            SetMaxHp(source.MaxHp);
            _isDead = false;
            _currentHp = Mathf.Clamp(source.CurrentHp, 1f, maxHp);

            if (bulletSpawner != null && source.BulletSpawner != null)
            {
                bulletSpawner.SetProjectileCount(source.BulletSpawner.ProjectileCount);
                bulletSpawner.SetVisualTierDamage(source.BulletSpawner.VisualTierDamage);
                bulletSpawner.SetBulletSpeed(source.BulletSpawner.BulletSpeed);
            }

            RefreshHealthBar();
        }

        public void ConfigureRuntimeFrom(PlayerUnit template, bool restoreFullHealth)
        {
            if (template == null)
            {
                return;
            }

            healthBarPrefab = template.healthBarPrefab;
            healthBarOffset = template.healthBarOffset;
            healthBarAnchor = null;
            SetDamage(template.Damage);
            SetFireRate(template.FireRate);
            SetMaxHp(template.MaxHp);

            if (restoreFullHealth)
            {
                RestoreFullHealth();
            }

            EnsureHealthBar();
            RefreshHealthBar();
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
            bulletSpawner.SetVisualTierDamage(damage);

            if (unitConfig != null)
            {
                bulletSpawner.SetBulletSpeed(unitConfig.BulletSpeed);
            }
        }

        protected virtual void ApplyUnitConfig()
        {
            if (unitConfig == null)
            {
                return;
            }

            damage = Mathf.Max(0f, unitConfig.Damage);
            fireRate = Mathf.Max(0f, unitConfig.FireRate);
            maxHp = Mathf.Max(1f, unitConfig.MaxHealth);
        }

        protected virtual void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            RefreshHealthBar();
            Died?.Invoke(this);
        }

        private void RefreshHealthBar()
        {
            EnsureHealthBar();
            _healthBarInstance?.SetNormalized(maxHp <= 0f ? 0f : _currentHp / maxHp);
        }

        private void EnsureHealthBar()
        {
            if (_healthBarInstance != null || healthBarPrefab == null)
            {
                if (_healthBarInstance != null)
                {
                    _healthBarInstance.Configure(healthBarOffset);
                }

                return;
            }

            Transform parent = healthBarAnchor != null ? healthBarAnchor : transform;
            _healthBarInstance = Instantiate(healthBarPrefab, parent);
            _healthBarInstance.name = healthBarPrefab.name;
            _healthBarInstance.Configure(healthBarOffset);
        }
    }
}
