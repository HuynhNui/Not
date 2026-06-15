using System;
using _Project.Scripts.Data.ScriptableObjects.UnitData;
using _Project.Scripts.Interfaces;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;
using _Project.Scripts.Systems.UISystem;
using _Project.Scripts.Systems.PoolSystem;

namespace _Project.Scripts.Gameplay.Enemies
{
    /// <summary>
    /// Defines a pooled enemy unit that can spawn, move toward the player, and receive damage.
    /// </summary>
    public sealed class EnemyController : MonoBehaviour, IPoolable, IDamageable, IConditionalDamageable
    {
        [SerializeField] private UnitData unitData;
        [SerializeField] private float currentHealth = 1f;
        [SerializeField] private int scoreValue = 1;
        [SerializeField] private float fallbackMoveSpeed = 3f;
        [SerializeField] private float fallbackMaxHealth = 1f;
        [SerializeField] private float fallbackContactDamage = 1f;
        [SerializeField] private int coinReward = 1;
        [SerializeField] private bool destroyOnPlayerHit = true;
        [SerializeField] private bool despawnImmediatelyOnDeath = true;
        [SerializeField] private bool requireCameraVisibilityForDamage = true;
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.ChaseTarget;
        [SerializeField] private float enterMoveSpeed = 2.7f;
        [SerializeField, Range(0f, 1f)] private float topBandViewportY = 0.75f;
        [SerializeField] private bool clampInsideCameraWidth = true;
        [SerializeField] private float horizontalPadding = 0.25f;
        [SerializeField] private float despawnBelowCameraOffset = 1.5f;
        [SerializeField] private WorldHealthBarView healthBarPrefab;
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 0.42f, 0f);

        private Transform _target;
        private MainPlayerUnit _playerUnit;
        private Camera _gameplayCamera;
        private PoolSystem _poolSystem;
        private bool _isActive;
        private bool _movementEnabled = true;
        private bool _canReceiveDamage = true;
        private bool _hasArrivedAtHoldPosition;
        private WorldHealthBarView _healthBarInstance;
        private bool _hasRuntimeStats;
        private float _runtimeMaxHealth;
        private float _runtimeMoveSpeed;
        private float _runtimeContactDamage;
        private int _runtimeScoreValue;
        private int _runtimeCoinReward;
        private float _runtimeRewardPoints;
        private bool _hasRuntimeRewardPoints;
        private bool _runtimeDestroyOnPlayerHit;
        private float _externalMoveSpeedMultiplier = 1f;

        public event Action<EnemyController> Killed;
        public event Action<EnemyController> Spawned;
        public event Action<EnemyController> Despawned;
        public event Action<EnemyController, float, float> Damaged;

        public int ScoreValue => _hasRuntimeStats ? _runtimeScoreValue : scoreValue;
        public int CoinReward => GetCoinReward();
        public float RewardPoints => _hasRuntimeRewardPoints
            ? _runtimeRewardPoints
            : Mathf.Max(0f, GetCoinReward());
        public bool IsActive => _isActive;
        public bool HasArrivedAtHoldPosition => _hasArrivedAtHoldPosition;
        public float CurrentHealth => currentHealth;
        public Transform Target => _target;
        public MainPlayerUnit PlayerUnit => _playerUnit;
        public Camera GameplayCamera => _gameplayCamera;
        public PoolSystem PoolSystem => _poolSystem;

        public void Init(Transform target, MainPlayerUnit playerUnit, Camera gameplayCamera = null)
        {
            _target = target;
            _playerUnit = playerUnit;
            _gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            ClearRuntimeStats();
            currentHealth = GetMaxHealth();
            _isActive = true;
            _movementEnabled = true;
            _canReceiveDamage = true;
            _hasArrivedAtHoldPosition = movementMode == EnemyMovementMode.ChaseTarget;
            _externalMoveSpeedMultiplier = 1f;
            EnsureHealthBar();
            RefreshHealthBar();
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            if (_movementEnabled)
            {
                MoveByMode();
            }

            DespawnIfOutOfBounds();
        }

        public void Spawn()
        {
            currentHealth = GetMaxHealth();
            _isActive = true;
            _movementEnabled = true;
            _canReceiveDamage = true;
            _hasArrivedAtHoldPosition = movementMode == EnemyMovementMode.ChaseTarget;
            EnsureHealthBar();
            RefreshHealthBar();
            Spawned?.Invoke(this);
        }

        public void Despawn()
        {
            _isActive = false;
            Despawned?.Invoke(this);

            if (_poolSystem != null)
            {
                _poolSystem.Release(this);
                return;
            }

            Destroy(gameObject);
        }

        public void SetPoolSystem(PoolSystem poolSystem)
        {
            _poolSystem = poolSystem;
        }

        public EnemyRuntimeStats CreateBaseRuntimeStats()
        {
            return new EnemyRuntimeStats(
                GetBaseMaxHealth(),
                GetBaseMoveSpeed(),
                GetBaseContactDamage(),
                scoreValue,
                coinReward > 0 ? coinReward : scoreValue,
                destroyOnPlayerHit);
        }

        public void ApplyRuntimeStats(EnemyRuntimeStats stats)
        {
            _hasRuntimeStats = true;
            _runtimeMaxHealth = stats.MaxHealth;
            _runtimeMoveSpeed = stats.MoveSpeed;
            _runtimeContactDamage = stats.ContactDamage;
            _runtimeScoreValue = stats.ScoreValue;
            _runtimeCoinReward = stats.CoinReward;
            _runtimeDestroyOnPlayerHit = stats.DestroyOnPlayerHit;
            currentHealth = GetMaxHealth();
            RefreshHealthBar();
        }

        public void SetRewardPoints(float rewardPoints)
        {
            _hasRuntimeRewardPoints = true;
            _runtimeRewardPoints = Mathf.Max(0f, rewardPoints);
        }

        public void SetMovementEnabled(bool isEnabled)
        {
            _movementEnabled = isEnabled;
        }

        public void SetExternalMoveSpeedMultiplier(float multiplier)
        {
            _externalMoveSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetDamageReceivingEnabled(bool isEnabled)
        {
            _canReceiveDamage = isEnabled;
        }

        public void TakeDamage(float damageAmount)
        {
            if (!CanReceiveDamageFrom(null))
            {
                return;
            }

            float appliedDamage = Mathf.Max(0f, damageAmount);

            if (appliedDamage <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);
            RefreshHealthBar();
            Damaged?.Invoke(this, appliedDamage, currentHealth);

            if (currentHealth <= 0f)
            {
                _isActive = false;
                Killed?.Invoke(this);

                if (despawnImmediatelyOnDeath)
                {
                    Despawn();
                }
            }
        }

        public bool CanReceiveDamageFrom(GameObject damageSource)
        {
            return _isActive
                && _canReceiveDamage
                && (!requireCameraVisibilityForDamage || IsInsideGameplayCamera());
        }

        public bool IsInsideGameplayCamera(float viewportPadding = 0f)
        {
            if (_gameplayCamera == null)
            {
                return true;
            }

            Vector3 viewportPosition = _gameplayCamera.WorldToViewportPoint(transform.position);
            float padding = Mathf.Max(0f, viewportPadding);

            return viewportPosition.z >= 0f
                && viewportPosition.x >= -padding
                && viewportPosition.x <= 1f + padding
                && viewportPosition.y >= -padding
                && viewportPosition.y <= 1f + padding;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void MoveByMode()
        {
            if (movementMode == EnemyMovementMode.EnterAndHoldTopBand)
            {
                MoveToTopBand();
                return;
            }

            MoveTowardsTarget();
        }

        private void MoveTowardsTarget()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                _target.position,
                GetMoveSpeed() * Time.deltaTime);

            if (clampInsideCameraWidth && _gameplayCamera != null && _gameplayCamera.orthographic)
            {
                float halfWidth = _gameplayCamera.orthographicSize * _gameplayCamera.aspect;
                float minX = _gameplayCamera.transform.position.x - halfWidth + horizontalPadding;
                float maxX = _gameplayCamera.transform.position.x + halfWidth - horizontalPadding;
                nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
            }

            transform.position = nextPosition;
        }

        private void MoveToTopBand()
        {
            if (_gameplayCamera == null || !_gameplayCamera.orthographic)
            {
                _hasArrivedAtHoldPosition = true;
                return;
            }

            float targetY = _gameplayCamera.transform.position.y
                - _gameplayCamera.orthographicSize
                + (_gameplayCamera.orthographicSize * 2f * topBandViewportY);

            Vector3 nextPosition = transform.position;
            nextPosition.y = Mathf.MoveTowards(
                transform.position.y,
                targetY,
                Mathf.Max(0f, enterMoveSpeed) * Time.deltaTime);

            if (clampInsideCameraWidth)
            {
                float halfWidth = _gameplayCamera.orthographicSize * _gameplayCamera.aspect;
                float minX = _gameplayCamera.transform.position.x - halfWidth + horizontalPadding;
                float maxX = _gameplayCamera.transform.position.x + halfWidth - horizontalPadding;
                nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
            }

            transform.position = nextPosition;
            _hasArrivedAtHoldPosition = Mathf.Abs(transform.position.y - targetY) <= 0.02f;
        }

        private void DespawnIfOutOfBounds()
        {
            if (_gameplayCamera == null || !_gameplayCamera.orthographic)
            {
                return;
            }

            float bottomLimit = _gameplayCamera.transform.position.y - _gameplayCamera.orthographicSize - despawnBelowCameraOffset;

            if (transform.position.y < bottomLimit)
            {
                Despawn();
            }
        }

        private void TryDamagePlayer(Collider2D other)
        {
            if (!_isActive || other == null)
            {
                return;
            }

            PlayerUnit hitPlayer = other.GetComponent<PlayerUnit>();

            if (hitPlayer == null)
            {
                hitPlayer = other.GetComponentInParent<PlayerUnit>();
            }

            if (hitPlayer == null || hitPlayer.IsDead)
            {
                return;
            }

            hitPlayer.TakeDamage(GetContactDamage());

            if (GetDestroyOnPlayerHit())
            {
                Despawn();
            }
        }

        private float GetMoveSpeed()
        {
            float baseSpeed = _hasRuntimeStats ? _runtimeMoveSpeed : GetBaseMoveSpeed();
            return baseSpeed * _externalMoveSpeedMultiplier;
        }

        private float GetMaxHealth()
        {
            return _hasRuntimeStats ? _runtimeMaxHealth : GetBaseMaxHealth();
        }

        private float GetContactDamage()
        {
            return _hasRuntimeStats ? _runtimeContactDamage : GetBaseContactDamage();
        }

        private float GetBaseMoveSpeed()
        {
            return unitData != null ? unitData.MoveSpeed : fallbackMoveSpeed;
        }

        private float GetBaseMaxHealth()
        {
            return unitData != null ? unitData.MaxHealth : fallbackMaxHealth;
        }

        private float GetBaseContactDamage()
        {
            return unitData != null ? unitData.ContactDamage : fallbackContactDamage;
        }

        private bool GetDestroyOnPlayerHit()
        {
            return _hasRuntimeStats ? _runtimeDestroyOnPlayerHit : destroyOnPlayerHit;
        }

        private int GetCoinReward()
        {
            if (_hasRuntimeStats)
            {
                return _runtimeCoinReward > 0 ? _runtimeCoinReward : _runtimeScoreValue;
            }

            return coinReward > 0 ? coinReward : scoreValue;
        }

        private void ClearRuntimeStats()
        {
            _hasRuntimeStats = false;
            _runtimeMaxHealth = 0f;
            _runtimeMoveSpeed = 0f;
            _runtimeContactDamage = 0f;
            _runtimeScoreValue = 0;
            _runtimeCoinReward = 0;
            _runtimeRewardPoints = 0f;
            _hasRuntimeRewardPoints = false;
            _runtimeDestroyOnPlayerHit = false;
        }

        private void RefreshHealthBar()
        {
            EnsureHealthBar();
            _healthBarInstance?.SetNormalized(GetMaxHealth() <= 0f ? 0f : currentHealth / GetMaxHealth());
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

    public enum EnemyMovementMode
    {
        ChaseTarget = 0,
        EnterAndHoldTopBand = 1
    }
}
