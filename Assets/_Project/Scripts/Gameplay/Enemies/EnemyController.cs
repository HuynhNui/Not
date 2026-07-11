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
        private static int _lastPhysicsSyncFrame = -1;

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
        [SerializeField] private float repeatedContactDamageCooldown = 0.5f;
        [SerializeField] private float contactDamageLeeway = 0.08f;
        [SerializeField] private float targetOffsetRadius;
        [SerializeField] private float separationRadius = 0.45f;
        [SerializeField] private float separationStrength = 0.45f;
        [SerializeField] private float crowdAvoidanceMinTargetDistance = 0.8f;
        [SerializeField] private WorldHealthBarView healthBarPrefab;
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 0.42f, 0f);
        [SerializeField] private float healthBarScaleMultiplier = 0.5f;

        private Transform _target;
        private MainPlayerUnit _playerUnit;
        private PlayerController _playerController;
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
        private float _nextContactDamageTime;
        private Vector3 _targetOffset;
        private Collider2D[] _contactColliders = Array.Empty<Collider2D>();
        private readonly Collider2D[] _overlapResults = new Collider2D[8];
        private readonly Collider2D[] _separationResults = new Collider2D[8];

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
        public PlayerController PlayerController => _playerController;
        public Camera GameplayCamera => _gameplayCamera;
        public PoolSystem PoolSystem => _poolSystem;

        private void Awake()
        {
            ConfigureContactPhysics();
        }

        public void Init(
            Transform target,
            MainPlayerUnit playerUnit,
            Camera gameplayCamera = null,
            PlayerController playerController = null)
        {
            _target = target;
            _playerUnit = playerUnit;
            _playerController = playerController != null
                ? playerController
                : playerUnit != null
                    ? playerUnit.GetComponentInParent<PlayerController>()
                    : null;
            _gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            ClearRuntimeStats();
            currentHealth = GetMaxHealth();
            _isActive = true;
            _movementEnabled = true;
            _canReceiveDamage = true;
            _hasArrivedAtHoldPosition = movementMode == EnemyMovementMode.ChaseTarget;
            _externalMoveSpeedMultiplier = 1f;
            _nextContactDamageTime = 0f;
            ResetTargetOffset();
            ConfigureContactPhysics();
            CacheContactColliders();
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

            PollPlayerContact();

            if (!_isActive)
            {
                return;
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
            _nextContactDamageTime = 0f;
            ResetTargetOffset();
            ConfigureContactPhysics();
            CacheContactColliders();
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
            if (_target == null && _playerController == null)
            {
                return;
            }

            Vector3 targetPosition = GetCurrentTargetPosition();
            Vector3 offset = GetDestroyOnPlayerHit() ? Vector3.zero : _targetOffset;
            targetPosition += offset;
            Vector3 toTarget = targetPosition - transform.position;
            float step = GetMoveSpeed() * Time.deltaTime;
            Vector3 moveDirection = toTarget.sqrMagnitude > 0.0001f
                ? toTarget.normalized
                : Vector3.down;
            Vector3 separation = toTarget.magnitude > Mathf.Max(0f, crowdAvoidanceMinTargetDistance)
                ? GetSeparationDirection()
                : Vector3.zero;

            if (separation.sqrMagnitude > 0.0001f)
            {
                moveDirection = (moveDirection + separation * Mathf.Max(0f, separationStrength)).normalized;
            }

            Vector3 nextPosition = transform.position + moveDirection * step;

            if (clampInsideCameraWidth && _gameplayCamera != null && _gameplayCamera.orthographic)
            {
                float halfWidth = _gameplayCamera.orthographicSize * _gameplayCamera.aspect;
                float minX = _gameplayCamera.transform.position.x - halfWidth + horizontalPadding;
                float maxX = _gameplayCamera.transform.position.x + halfWidth - horizontalPadding;
                nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
            }

            transform.position = nextPosition;
        }

        public Vector3 GetCurrentTargetPosition()
        {
            if (_playerController != null
                && _playerController.TryGetClosestAliveUnitContactPoint(
                    transform.position,
                    out _,
                    out Vector3 contactPoint))
            {
                return contactPoint;
            }

            return _target != null ? _target.position : transform.position;
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

            TryDamagePlayerUnit(ResolvePlayerUnit(other));
        }

        private void ConfigureContactPhysics()
        {
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
#if UNITY_6000_0_OR_NEWER
            body.angularDamping = 0.05f;
#else
            body.angularDrag = 0.05f;
#endif
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            body.interpolation = RigidbodyInterpolation2D.None;
            body.simulated = true;

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider2D collider = colliders[index];
                if (collider == null)
                {
                    continue;
                }

                collider.enabled = true;
                collider.isTrigger = true;
            }
        }

        private void PollPlayerContact()
        {
            if (!_isActive || (!GetDestroyOnPlayerHit() && Time.time < _nextContactDamageTime))
            {
                return;
            }

            if (_contactColliders == null || _contactColliders.Length == 0)
            {
                CacheContactColliders();
            }

            SyncPhysicsTransformsOncePerFrame();

            if (TryDamageClosestPlayerInReach())
            {
                return;
            }

            for (int colliderIndex = 0; colliderIndex < _contactColliders.Length; colliderIndex++)
            {
                Collider2D enemyCollider = _contactColliders[colliderIndex];
                if (enemyCollider == null || !enemyCollider.enabled)
                {
                    continue;
                }

                int overlapCount = enemyCollider.Overlap(ContactFilter2D.noFilter, _overlapResults);
                for (int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++)
                {
                    Collider2D overlap = _overlapResults[overlapIndex];
                    if (overlap == null || overlap.transform.IsChildOf(transform))
                    {
                        continue;
                    }

                    TryDamagePlayer(overlap);

                    if (!_isActive || (!GetDestroyOnPlayerHit() && Time.time < _nextContactDamageTime))
                    {
                        ClearOverlapResults(overlapCount);
                        return;
                    }
                }

                ClearOverlapResults(overlapCount);
            }
        }

        private bool TryDamageClosestPlayerInReach()
        {
            if (_playerController == null)
            {
                return false;
            }

            if (!_playerController.TryGetClosestAliveUnitContactPoint(
                transform.position,
                out PlayerUnit playerUnit,
                out Vector3 playerContactPoint))
            {
                return false;
            }

            Vector3 enemyContactPoint = GetClosestEnemyColliderPoint(playerContactPoint);
            float reach = Mathf.Max(0f, contactDamageLeeway);
            if ((enemyContactPoint - playerContactPoint).sqrMagnitude > reach * reach)
            {
                return false;
            }

            TryDamagePlayerUnit(playerUnit);
            return !_isActive || (!GetDestroyOnPlayerHit() && Time.time < _nextContactDamageTime);
        }

        private Vector3 GetClosestEnemyColliderPoint(Vector3 targetPosition)
        {
            if (_contactColliders == null || _contactColliders.Length == 0)
            {
                return transform.position;
            }

            Vector3 closestPoint = transform.position;
            float closestSqrDistance = float.PositiveInfinity;

            for (int index = 0; index < _contactColliders.Length; index++)
            {
                Collider2D enemyCollider = _contactColliders[index];
                if (enemyCollider == null || !enemyCollider.enabled)
                {
                    continue;
                }

                Vector2 colliderPoint = enemyCollider.ClosestPoint(targetPosition);
                Vector3 point = new Vector3(colliderPoint.x, colliderPoint.y, transform.position.z);
                float sqrDistance = (point - targetPosition).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                closestPoint = point;
            }

            return closestPoint;
        }

        private static PlayerUnit ResolvePlayerUnit(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            PlayerUnit hitPlayer = other.GetComponent<PlayerUnit>();
            return hitPlayer != null ? hitPlayer : other.GetComponentInParent<PlayerUnit>();
        }

        private void TryDamagePlayerUnit(PlayerUnit hitPlayer)
        {
            if (!_isActive || hitPlayer == null || hitPlayer.IsDead)
            {
                return;
            }

            hitPlayer.TakeDamage(GetContactDamage());

            if (GetDestroyOnPlayerHit())
            {
                Despawn();
                return;
            }

            _nextContactDamageTime = Time.time + Mathf.Max(0.01f, repeatedContactDamageCooldown);
        }

        private Vector3 GetSeparationDirection()
        {
            float radius = Mathf.Max(0f, separationRadius);
            if (radius <= 0f)
            {
                return Vector3.zero;
            }

            int overlapCount = Physics2D.OverlapCircle(transform.position, radius, ContactFilter2D.noFilter, _separationResults);
            Vector3 separation = Vector3.zero;

            for (int index = 0; index < overlapCount; index++)
            {
                Collider2D overlap = _separationResults[index];
                _separationResults[index] = null;

                if (overlap == null || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                EnemyController otherEnemy = overlap.GetComponentInParent<EnemyController>();
                if (otherEnemy == null || otherEnemy == this || !otherEnemy.IsActive)
                {
                    continue;
                }

                Vector3 away = transform.position - otherEnemy.transform.position;
                float sqrDistance = away.sqrMagnitude;
                if (sqrDistance <= 0.0001f)
                {
                    away = _targetOffset.sqrMagnitude > 0.0001f ? _targetOffset : Vector3.right;
                    sqrDistance = away.sqrMagnitude;
                }

                separation += away.normalized / Mathf.Max(0.05f, Mathf.Sqrt(sqrDistance));
            }

            return separation.sqrMagnitude > 0.0001f ? separation.normalized : Vector3.zero;
        }

        private void CacheContactColliders()
        {
            _contactColliders = GetComponentsInChildren<Collider2D>();
        }

        private void ResetTargetOffset()
        {
            float radius = Mathf.Max(0f, targetOffsetRadius);
            if (radius <= 0f)
            {
                _targetOffset = Vector3.zero;
                return;
            }

            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * radius;
            _targetOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
        }

        private static void SyncPhysicsTransformsOncePerFrame()
        {
            if (_lastPhysicsSyncFrame == Time.frameCount)
            {
                return;
            }

            Physics2D.SyncTransforms();
            _lastPhysicsSyncFrame = Time.frameCount;
        }

        private void ClearOverlapResults(int overlapCount)
        {
            for (int index = 0; index < overlapCount && index < _overlapResults.Length; index++)
            {
                _overlapResults[index] = null;
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
                    _healthBarInstance.SetScaleMultiplier(healthBarScaleMultiplier);
                }

                return;
            }

            Transform parent = healthBarAnchor != null ? healthBarAnchor : transform;
            _healthBarInstance = Instantiate(healthBarPrefab, parent);
            _healthBarInstance.name = healthBarPrefab.name;
            _healthBarInstance.Configure(healthBarOffset);
            _healthBarInstance.SetScaleMultiplier(healthBarScaleMultiplier);
        }
    }

    public enum EnemyMovementMode
    {
        ChaseTarget = 0,
        EnterAndHoldTopBand = 1
    }
}
