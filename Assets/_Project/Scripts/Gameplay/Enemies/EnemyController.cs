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
    public sealed class EnemyController : MonoBehaviour, IPoolable, IDamageable
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
        private bool _hasArrivedAtHoldPosition;
        private WorldHealthBarView _healthBarInstance;

        public event Action<EnemyController> Killed;
        public event Action<EnemyController> Spawned;
        public event Action<EnemyController> Despawned;

        public int ScoreValue => scoreValue;
        public int CoinReward => coinReward > 0 ? coinReward : scoreValue;
        public bool IsActive => _isActive;
        public bool HasArrivedAtHoldPosition => _hasArrivedAtHoldPosition;

        public void Init(Transform target, MainPlayerUnit playerUnit, Camera gameplayCamera = null)
        {
            _target = target;
            _playerUnit = playerUnit;
            _gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            currentHealth = GetMaxHealth();
            _isActive = true;
            _hasArrivedAtHoldPosition = movementMode == EnemyMovementMode.ChaseTarget;
            EnsureHealthBar();
            RefreshHealthBar();
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            MoveByMode();
            DespawnIfOutOfBounds();
        }

        public void Spawn()
        {
            currentHealth = GetMaxHealth();
            _isActive = true;
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

        public void TakeDamage(float damageAmount)
        {
            if (!_isActive)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, damageAmount));
            RefreshHealthBar();

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

            MainPlayerUnit hitPlayer = other.GetComponent<MainPlayerUnit>();

            if (hitPlayer == null && _playerUnit != null && other.transform == _playerUnit.transform)
            {
                hitPlayer = _playerUnit;
            }

            if (hitPlayer == null || hitPlayer.IsDead)
            {
                return;
            }

            hitPlayer.TakeDamage(GetContactDamage());

            if (destroyOnPlayerHit)
            {
                Despawn();
            }
        }

        private float GetMoveSpeed()
        {
            return unitData != null ? unitData.MoveSpeed : fallbackMoveSpeed;
        }

        private float GetMaxHealth()
        {
            return unitData != null ? unitData.MaxHealth : fallbackMaxHealth;
        }

        private float GetContactDamage()
        {
            return unitData != null ? unitData.ContactDamage : fallbackContactDamage;
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
