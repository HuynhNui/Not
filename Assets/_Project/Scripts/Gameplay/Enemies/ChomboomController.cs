using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.PoolSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    /// <summary>
    /// Bomb enemy behavior layered on top of the shared EnemyController lifecycle.
    /// </summary>
    public sealed class ChomboomController : MonoBehaviour, IEnemyRuntimeTunable
    {
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private Animator animator;
        [SerializeField] private ChomboomBoomFx boomFxPrefab;
        [SerializeField] private float triggerRadius = 0.45f;
        [SerializeField] private float armingDuration = 2f;
        [SerializeField] private float hurtDuration = 0.4f;
        [SerializeField] private float explosionDamage = 2f;
        [SerializeField] private float explosionRadius = 1.75f;
        [SerializeField] private string walkDownStateName = "walk d";
        [SerializeField] private string walkLeftStateName = "walk s1";
        [SerializeField] private string walkRightStateName = "walk s2";
        [SerializeField] private string hurtDownStateName = "hurt d";
        [SerializeField] private string hurtLeftStateName = "hurt s1";
        [SerializeField] private string hurtRightStateName = "hurt s2";
        [SerializeField] private string boomDownStateName = "boom d";
        [SerializeField] private string boomLeftStateName = "boom s1";
        [SerializeField] private string boomRightStateName = "boom s2";

        private PoolSystem _poolSystem;
        private ChomboomState _state = ChomboomState.Inactive;
        private ChomboomDirection _lastDirection = ChomboomDirection.Down;
        private float _stateTimer;
        private string _currentAnimationState;
        private bool _hasBaseRuntimeValues;
        private float _baseExplosionDamage;

        private void Awake()
        {
            CacheBaseRuntimeValues();
            enemyController ??= GetComponent<EnemyController>();
            animator ??= GetComponentInChildren<Animator>();
            _poolSystem = FindAnyObjectByType<PoolSystem>();

            if (enemyController != null)
            {
                enemyController.Spawned -= HandleSpawned;
                enemyController.Spawned += HandleSpawned;
                enemyController.Damaged -= HandleDamaged;
                enemyController.Damaged += HandleDamaged;
                enemyController.Killed -= HandleKilled;
                enemyController.Killed += HandleKilled;
                enemyController.Despawned -= HandleDespawned;
                enemyController.Despawned += HandleDespawned;
            }
        }

        public void ApplyRunScaling(EnemyRunScaling scaling)
        {
            CacheBaseRuntimeValues();
            explosionDamage = _baseExplosionDamage * scaling.DamageMultiplier;
        }

        private void OnDestroy()
        {
            if (enemyController == null)
            {
                return;
            }

            enemyController.Spawned -= HandleSpawned;
            enemyController.Damaged -= HandleDamaged;
            enemyController.Killed -= HandleKilled;
            enemyController.Despawned -= HandleDespawned;
        }

        private void Update()
        {
            switch (_state)
            {
                case ChomboomState.Chasing:
                    UpdateChasing();
                    break;
                case ChomboomState.Hurt:
                    UpdateHurt();
                    break;
                case ChomboomState.Arming:
                    UpdateArming();
                    break;
            }
        }

        private void HandleSpawned(EnemyController enemy)
        {
            _state = ChomboomState.Chasing;
            _stateTimer = 0f;
            _lastDirection = ChomboomDirection.Down;
            _currentAnimationState = null;

            enemyController?.SetMovementEnabled(true);
            enemyController?.SetDamageReceivingEnabled(true);
            UpdateFacingFromTarget();
            PlayState(GetWalkStateName(_lastDirection));
        }

        private void HandleDamaged(EnemyController enemy, float damageAmount, float currentHealth)
        {
            if (_state == ChomboomState.Arming || currentHealth <= 0f)
            {
                return;
            }

            _state = ChomboomState.Hurt;
            _stateTimer = Mathf.Max(0f, hurtDuration);
            PlayState(GetHurtStateName(_lastDirection), forceRestart: true);
        }

        private void HandleKilled(EnemyController enemy)
        {
            EnterArming();
        }

        private void HandleDespawned(EnemyController enemy)
        {
            _state = ChomboomState.Inactive;
            _currentAnimationState = null;
        }

        private void UpdateChasing()
        {
            UpdateFacingFromTarget();
            PlayState(GetWalkStateName(_lastDirection));
            TryArmFromProximity();
        }

        private void UpdateHurt()
        {
            TryArmFromProximity();

            if (_state == ChomboomState.Arming)
            {
                return;
            }

            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                _state = ChomboomState.Chasing;
                UpdateFacingFromTarget();
                PlayState(GetWalkStateName(_lastDirection));
            }
        }

        private void UpdateArming()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer > 0f)
            {
                return;
            }

            SpawnBoomFx();
            enemyController?.Despawn();
        }

        private void TryArmFromProximity()
        {
            PlayerUnit playerUnit = GetClosestAlivePlayerUnitInRadius();

            if (playerUnit == null)
            {
                return;
            }

            EnterArming();
        }

        private PlayerUnit GetClosestAlivePlayerUnitInRadius()
        {
            PlayerUnit[] playerUnits = FindObjectsByType<PlayerUnit>();
            PlayerUnit closestUnit = null;
            float closestDistance = Mathf.Max(0f, triggerRadius);

            for (int index = 0; index < playerUnits.Length; index++)
            {
                PlayerUnit playerUnit = playerUnits[index];

                if (playerUnit == null || playerUnit.IsDead)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, playerUnit.transform.position);

                if (distance > closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                closestUnit = playerUnit;
            }

            return closestUnit;
        }

        private void EnterArming()
        {
            if (_state == ChomboomState.Arming)
            {
                return;
            }

            _state = ChomboomState.Arming;
            _stateTimer = Mathf.Max(0f, armingDuration);
            enemyController?.SetMovementEnabled(false);
            enemyController?.SetDamageReceivingEnabled(false);
            PlayState(GetBoomStateName(_lastDirection), forceRestart: true);
        }

        private void SpawnBoomFx()
        {
            if (boomFxPrefab == null)
            {
                return;
            }

            PoolSystem activePoolSystem = enemyController != null && enemyController.PoolSystem != null
                ? enemyController.PoolSystem
                : _poolSystem;

            ChomboomBoomFx fx = activePoolSystem != null
                ? activePoolSystem.Spawn(boomFxPrefab, transform.position, Quaternion.identity)
                : Instantiate(boomFxPrefab, transform.position, Quaternion.identity);

            if (fx == null)
            {
                return;
            }

            fx.SetPoolSystem(activePoolSystem);
            fx.Init(enemyController != null ? enemyController.PlayerUnit : null, explosionDamage, explosionRadius);
            fx.Spawn();
        }

        private void CacheBaseRuntimeValues()
        {
            if (_hasBaseRuntimeValues)
            {
                return;
            }

            _baseExplosionDamage = explosionDamage;
            _hasBaseRuntimeValues = true;
        }

        private void UpdateFacingFromTarget()
        {
            if (enemyController == null || enemyController.Target == null)
            {
                return;
            }

            Vector3 directionToTarget = enemyController.Target.position - transform.position;

            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float absX = Mathf.Abs(directionToTarget.x);
            float absY = Mathf.Abs(directionToTarget.y);

            if (absX >= absY * 0.35f && absX > 0.05f)
            {
                _lastDirection = directionToTarget.x < 0f
                    ? ChomboomDirection.Left
                    : ChomboomDirection.Right;
                return;
            }

            _lastDirection = ChomboomDirection.Down;
        }

        private void PlayState(string stateName, bool forceRestart = false)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            if (!forceRestart && _currentAnimationState == stateName)
            {
                return;
            }

            _currentAnimationState = stateName;
            animator.Play(stateName, 0, 0f);
        }

        private string GetWalkStateName(ChomboomDirection direction)
        {
            return direction switch
            {
                ChomboomDirection.Left => walkLeftStateName,
                ChomboomDirection.Right => walkRightStateName,
                _ => walkDownStateName
            };
        }

        private string GetHurtStateName(ChomboomDirection direction)
        {
            return direction switch
            {
                ChomboomDirection.Left => hurtLeftStateName,
                ChomboomDirection.Right => hurtRightStateName,
                _ => hurtDownStateName
            };
        }

        private string GetBoomStateName(ChomboomDirection direction)
        {
            return direction switch
            {
                ChomboomDirection.Left => boomLeftStateName,
                ChomboomDirection.Right => boomRightStateName,
                _ => boomDownStateName
            };
        }

        private enum ChomboomState
        {
            Inactive = 0,
            Chasing = 1,
            Hurt = 2,
            Arming = 3
        }

        private enum ChomboomDirection
        {
            Down = 0,
            Left = 1,
            Right = 2
        }
    }
}
