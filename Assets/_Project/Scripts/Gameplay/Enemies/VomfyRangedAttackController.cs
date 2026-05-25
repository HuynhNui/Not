using _Project.Scripts.Systems.PoolSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    /// <summary>
    /// Shooter behavior layered on top of the shared EnemyController lifecycle.
    /// </summary>
    public sealed class VomfyRangedAttackController : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform firePoint;
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private float attackDuration = 5f;
        [SerializeField] private float idleDuration = 3f;
        [SerializeField] private float shotInterval = 0.2f;
        [SerializeField, Range(0f, 1f)] private float fireWindowStartNormalized = 0.45f;
        [SerializeField, Range(0f, 1f)] private float fireWindowEndNormalized = 0.75f;
        [SerializeField] private bool fireOncePerAttackLoop;
        [SerializeField] private float projectileDamage = 1f;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float deathDespawnDelay = 0.5f;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string hopStateName = "Hop";
        [SerializeField] private string attackStateName = "Attackaction";
        [SerializeField] private string deathStateName = "ow";

        private PoolSystem _poolSystem;
        private VomfyState _state = VomfyState.Inactive;
        private float _stateTimer;
        private float _nextShotTime;
        private float _previousAttackNormalizedTime;
        private int _lastFiredAttackLoop = -1;
        private bool _hasPreviousAttackNormalizedTime;

        private void Awake()
        {
            enemyController ??= GetComponent<EnemyController>();
            animator ??= GetComponentInChildren<Animator>();
            _poolSystem = FindAnyObjectByType<PoolSystem>();

            if (enemyController != null)
            {
                enemyController.Spawned -= HandleSpawned;
                enemyController.Spawned += HandleSpawned;
                enemyController.Killed -= HandleKilled;
                enemyController.Killed += HandleKilled;
                enemyController.Despawned -= HandleDespawned;
                enemyController.Despawned += HandleDespawned;
            }
        }

        private void OnDestroy()
        {
            if (enemyController == null)
            {
                return;
            }

            enemyController.Spawned -= HandleSpawned;
            enemyController.Killed -= HandleKilled;
            enemyController.Despawned -= HandleDespawned;
        }

        private void Update()
        {
            switch (_state)
            {
                case VomfyState.Entering:
                    UpdateEntering();
                    break;
                case VomfyState.Attacking:
                    UpdateAttacking();
                    break;
                case VomfyState.Idle:
                    UpdateIdle();
                    break;
                case VomfyState.Dying:
                    UpdateDying();
                    break;
            }
        }

        private void HandleSpawned(EnemyController enemy)
        {
            _state = VomfyState.Entering;
            _stateTimer = 0f;
            _nextShotTime = 0f;
            ResetAttackFireTracking();
            PlayState(hopStateName);
        }

        private void HandleKilled(EnemyController enemy)
        {
            if (_state == VomfyState.Dying)
            {
                return;
            }

            _state = VomfyState.Dying;
            _stateTimer = Mathf.Max(0f, deathDespawnDelay);
            PlayState(deathStateName);
        }

        private void HandleDespawned(EnemyController enemy)
        {
            _state = VomfyState.Inactive;
        }

        private void UpdateEntering()
        {
            if (enemyController == null || enemyController.HasArrivedAtHoldPosition)
            {
                EnterAttack();
            }
        }

        private void UpdateAttacking()
        {
            _stateTimer -= Time.deltaTime;
            TryFireFromAttackWindow();

            if (_stateTimer <= 0f)
            {
                EnterIdle();
            }
        }

        private void UpdateIdle()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                EnterAttack();
            }
        }

        private void UpdateDying()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                enemyController?.Despawn();
            }
        }

        private void EnterAttack()
        {
            _state = VomfyState.Attacking;
            _stateTimer = Mathf.Max(0f, attackDuration);
            _nextShotTime = 0f;
            ResetAttackFireTracking();
            PlayState(attackStateName);
        }

        private void EnterIdle()
        {
            _state = VomfyState.Idle;
            _stateTimer = Mathf.Max(0f, idleDuration);
            PlayState(idleStateName);
        }

        private void Shoot()
        {
            Transform spawnPoint = firePoint != null ? firePoint : transform;
            EnemyProjectile projectile = _poolSystem != null
                ? _poolSystem.Spawn(projectilePrefab, spawnPoint.position, Quaternion.identity)
                : Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);

            if (projectile == null)
            {
                return;
            }

            projectile.SetPoolSystem(_poolSystem);
            projectile.Init(projectileDamage, projectileSpeed);
            projectile.Spawn();
        }

        private void TryFireFromAttackWindow()
        {
            if (projectilePrefab == null || animator == null || string.IsNullOrWhiteSpace(attackStateName))
            {
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            int attackStateHash = Animator.StringToHash(attackStateName);

            if (stateInfo.shortNameHash != attackStateHash && stateInfo.fullPathHash != attackStateHash)
            {
                _hasPreviousAttackNormalizedTime = false;
                return;
            }

            float normalizedTime = Mathf.Max(0f, stateInfo.normalizedTime);
            int attackLoop = Mathf.FloorToInt(normalizedTime);
            float phase = normalizedTime - attackLoop;
            float windowStart = Mathf.Clamp01(Mathf.Min(fireWindowStartNormalized, fireWindowEndNormalized));
            float windowEnd = Mathf.Clamp01(Mathf.Max(fireWindowStartNormalized, fireWindowEndNormalized));

            bool isInsideWindow = phase >= windowStart && phase <= windowEnd;
            bool crossedWindowStart = false;

            if (_hasPreviousAttackNormalizedTime)
            {
                float previousNormalizedTime = Mathf.Max(0f, _previousAttackNormalizedTime);
                int previousLoop = Mathf.FloorToInt(previousNormalizedTime);
                float previousPhase = previousNormalizedTime - previousLoop;

                crossedWindowStart = attackLoop > previousLoop
                    ? phase >= windowStart || previousPhase < windowStart
                    : previousPhase < windowStart && phase >= windowStart;
            }

            _previousAttackNormalizedTime = normalizedTime;
            _hasPreviousAttackNormalizedTime = true;

            if (!isInsideWindow && !crossedWindowStart)
            {
                return;
            }

            if (fireOncePerAttackLoop && _lastFiredAttackLoop == attackLoop)
            {
                return;
            }

            if (!fireOncePerAttackLoop && Time.time < _nextShotTime)
            {
                return;
            }

            Shoot();
            _lastFiredAttackLoop = attackLoop;
            _nextShotTime = Time.time + Mathf.Max(0.01f, shotInterval);
        }

        private void ResetAttackFireTracking()
        {
            _previousAttackNormalizedTime = 0f;
            _lastFiredAttackLoop = -1;
            _hasPreviousAttackNormalizedTime = false;
        }

        private void PlayState(string stateName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            int stateHash = Animator.StringToHash(stateName);

            if (!animator.HasState(0, stateHash))
            {
                return;
            }

            animator.Play(stateHash, 0, 0f);
        }

        private enum VomfyState
        {
            Inactive = 0,
            Entering = 1,
            Attacking = 2,
            Idle = 3,
            Dying = 4
        }
    }
}
