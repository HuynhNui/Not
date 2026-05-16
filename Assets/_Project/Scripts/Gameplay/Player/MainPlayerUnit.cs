using _Project.Scripts.Data.ScriptableObjects.PlayerConfigs;
using _Project.Scripts.Interfaces;
using _Project.Scripts.Systems.UISystem;
using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Main squad unit with health and death handling.
    /// Combat behavior remains inherited from PlayerUnit.
    /// </summary>
    public sealed class MainPlayerUnit : PlayerUnit, IDamageable
    {
        [SerializeField] private PlayerUnitConfig mainUnitConfig;
        [SerializeField] private float maxHp = 10f;
        [SerializeField] private WorldHealthBarView healthBarPrefab;
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 0.33f, 0f);

        private float _currentHp;
        private bool _isDead;
        private WorldHealthBarView _healthBarInstance;

        public event Action<MainPlayerUnit> Died;

        public float CurrentHp => _currentHp;
        public float MaxHp => maxHp;
        public bool IsDead => _isDead;

        public override void Initialize()
        {
            ApplyUnitConfig();
            base.Initialize();
            EnsureHealthBar();

            if (_currentHp <= 0f && !_isDead)
            {
                _currentHp = Mathf.Max(1f, maxHp);
            }

            RefreshHealthBar();
        }

        public void TakeDamage(float value)
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
            _isDead = false;
            _currentHp = Mathf.Max(1f, maxHp);
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

            if (_currentHp <= 0f && maxHp > 0f)
            {
                _currentHp = maxHp;
            }

            RefreshHealthBar();
        }

        public void Die()
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

        private void ApplyUnitConfig()
        {
            if (mainUnitConfig == null)
            {
                return;
            }

            maxHp = Mathf.Max(1f, mainUnitConfig.MaxHealth);
            SetDamage(mainUnitConfig.Damage);
            SetFireRate(mainUnitConfig.FireRate);
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
