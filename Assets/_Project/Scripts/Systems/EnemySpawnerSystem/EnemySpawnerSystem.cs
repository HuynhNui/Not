using System;
using System.Collections.Generic;
using _Project.Scripts.Data.ScriptableObjects.SpawnConfigs;
using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;
using Random = UnityEngine.Random;
using RuntimePoolSystem = _Project.Scripts.Systems.PoolSystem.PoolSystem;

namespace _Project.Scripts.Systems.EnemySpawnerSystem
{
    /// <summary>
    /// Controls enemy wave pacing, spawn positions, and difficulty scaling over time.
    /// </summary>
    public sealed class EnemySpawnerSystem : MonoBehaviour
    {
        [SerializeField] private EnemySpawnConfig spawnConfig;
        [SerializeField] private RunProgressionConfig runProgressionConfig;
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private List<EnemySpawnEntry> spawnEntries = new List<EnemySpawnEntry>();
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 1f, 60f, 2f);
        [SerializeField] private MainPlayerUnit playerUnit;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private RuntimePoolSystem poolSystem;
        [SerializeField] private float baseSpawnInterval = 1.5f;
        [SerializeField] private float minimumSpawnInterval = 0.35f;
        [SerializeField] private float spawnYOffset = 1f;
        [SerializeField] private float horizontalSpawnPadding = 0.35f;

        private float _elapsedTime;
        private float _nextSpawnTime;
        private bool _spawningEnabled = true;
        private readonly HashSet<EnemyController> _activeEnemies = new HashSet<EnemyController>();
        private readonly List<EnemyController> _enemyRemovalBuffer = new List<EnemyController>();

        public event Action<EnemyController> EnemyKilled;

        public void Init()
        {
            gameplayCamera ??= Camera.main;
            poolSystem ??= FindAnyObjectByType<RuntimePoolSystem>();

            if (playerUnit == null)
            {
                playerUnit = FindAnyObjectByType<MainPlayerUnit>();
            }

            _elapsedTime = 0f;
            _nextSpawnTime = 0f;
            _activeEnemies.Clear();
        }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            if (!_spawningEnabled || playerUnit == null || playerUnit.IsDead || !HasSpawnableEnemy())
            {
                return;
            }

            _elapsedTime += Time.deltaTime;

            if (GetActiveEnemyCount() >= GetCurrentMaxActiveEnemies())
            {
                return;
            }

            if (Time.time >= _nextSpawnTime)
            {
                Spawn();
                _nextSpawnTime = Time.time + GetCurrentSpawnInterval();
            }
        }

        public void BeginRun()
        {
            _elapsedTime = 0f;
            _nextSpawnTime = Time.time;
            _activeEnemies.Clear();
        }

        public void Spawn()
        {
            if (!_spawningEnabled || playerUnit == null)
            {
                return;
            }

            int batchSize = GetCurrentSpawnBatchSize();
            for (int spawnIndex = 0; spawnIndex < batchSize; spawnIndex++)
            {
                if (GetActiveEnemyCount() >= GetCurrentMaxActiveEnemies())
                {
                    return;
                }

                SpawnSingleEnemy();
            }
        }

        public void SetSpawningEnabled(bool isEnabled)
        {
            _spawningEnabled = isEnabled;
        }

        private void SpawnSingleEnemy()
        {
            if (!_spawningEnabled || playerUnit == null)
            {
                return;
            }

            EnemySpawnEntry selectedEntry = SelectEnemyEntry();
            EnemyController selectedPrefab = selectedEntry != null ? selectedEntry.Prefab : enemyPrefab;

            if (selectedPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            EnemyController enemyInstance = poolSystem != null
                ? poolSystem.Spawn(selectedPrefab, spawnPosition, Quaternion.identity)
                : Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

            if (enemyInstance == null)
            {
                return;
            }

            enemyInstance.SetPoolSystem(poolSystem);
            enemyInstance.Init(playerUnit.transform, playerUnit, gameplayCamera);
            ApplyRunProgression(enemyInstance);
            enemyInstance.Killed -= HandleEnemyKilled;
            enemyInstance.Killed += HandleEnemyKilled;
            enemyInstance.Despawned -= HandleEnemyDespawned;
            enemyInstance.Despawned += HandleEnemyDespawned;
            enemyInstance.Spawn();
            TrackEnemy(enemyInstance);
        }

        private float GetCurrentSpawnInterval()
        {
            if (runProgressionConfig != null)
            {
                return runProgressionConfig.GetSpawnInterval(_elapsedTime);
            }

            if (spawnConfig == null)
            {
                return Mathf.Min(GetLegacySpawnInterval(), RunProgressionConfig.GetDefaultSpawnInterval(_elapsedTime));
            }

            return RunProgressionConfig.GetDefaultSpawnInterval(_elapsedTime);
        }

        private float GetLegacySpawnInterval()
        {
            AnimationCurve activeDifficultyCurve = spawnConfig != null ? spawnConfig.DifficultyCurve : difficultyCurve;
            float activeBaseInterval = spawnConfig != null ? spawnConfig.BaseSpawnInterval : baseSpawnInterval;
            float activeMinimumInterval = spawnConfig != null ? spawnConfig.MinimumSpawnInterval : minimumSpawnInterval;
            float difficultyMultiplier = Mathf.Max(0.01f, activeDifficultyCurve.Evaluate(_elapsedTime));
            float scaledInterval = activeBaseInterval / difficultyMultiplier;
            return Mathf.Max(activeMinimumInterval, scaledInterval);
        }

        private int GetCurrentMaxActiveEnemies()
        {
            if (runProgressionConfig != null)
            {
                return runProgressionConfig.GetMaxActiveEnemies(_elapsedTime);
            }

            return RunProgressionConfig.GetDefaultMaxActiveEnemies(_elapsedTime);
        }

        private int GetCurrentSpawnBatchSize()
        {
            if (runProgressionConfig != null)
            {
                return runProgressionConfig.GetSpawnBatchSize(_elapsedTime);
            }

            return RunProgressionConfig.GetDefaultSpawnBatchSize(_elapsedTime);
        }

        private EnemyRunScaling GetCurrentEnemyRunScaling()
        {
            if (runProgressionConfig != null)
            {
                return runProgressionConfig.GetEnemyRunScaling(_elapsedTime);
            }

            return RunProgressionConfig.GetDefaultEnemyRunScaling(_elapsedTime);
        }

        private bool HasSpawnableEnemy()
        {
            if (spawnEntries == null || spawnEntries.Count == 0)
            {
                return enemyPrefab != null;
            }

            for (int index = 0; index < spawnEntries.Count; index++)
            {
                EnemySpawnEntry entry = spawnEntries[index];

                if (entry != null
                    && entry.Prefab != null
                    && entry.GetWeight(_elapsedTime, runProgressionConfig) > 0f
                    && _elapsedTime >= entry.GetUnlockAfterSeconds(runProgressionConfig))
                {
                    return true;
                }
            }

            return false;
        }

        private EnemySpawnEntry SelectEnemyEntry()
        {
            if (spawnEntries == null || spawnEntries.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;

            for (int index = 0; index < spawnEntries.Count; index++)
            {
                EnemySpawnEntry entry = spawnEntries[index];

                if (entry == null
                    || entry.Prefab == null
                    || _elapsedTime < entry.GetUnlockAfterSeconds(runProgressionConfig))
                {
                    continue;
                }

                totalWeight += entry.GetWeight(_elapsedTime, runProgressionConfig);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float accumulatedWeight = 0f;

            for (int index = 0; index < spawnEntries.Count; index++)
            {
                EnemySpawnEntry entry = spawnEntries[index];

                if (entry == null
                    || entry.Prefab == null
                    || _elapsedTime < entry.GetUnlockAfterSeconds(runProgressionConfig))
                {
                    continue;
                }

                accumulatedWeight += entry.GetWeight(_elapsedTime, runProgressionConfig);

                if (roll <= accumulatedWeight)
                {
                    return entry;
                }
            }

            return null;
        }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            UntrackEnemy(enemy);
            EnemyKilled?.Invoke(enemy);
        }

        private void HandleEnemyDespawned(EnemyController enemy)
        {
            UntrackEnemy(enemy);
        }

        private void TrackEnemy(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            _activeEnemies.Add(enemy);
        }

        private void UntrackEnemy(EnemyController enemy)
        {
            if (enemy == null)
            {
                _activeEnemies.Remove(enemy);
                return;
            }

            _activeEnemies.Remove(enemy);
            enemy.Killed -= HandleEnemyKilled;
            enemy.Despawned -= HandleEnemyDespawned;
        }

        private int GetActiveEnemyCount()
        {
            if (_activeEnemies.Count <= 0)
            {
                return 0;
            }

            _enemyRemovalBuffer.Clear();

            foreach (EnemyController enemy in _activeEnemies)
            {
                if (enemy == null || !enemy.IsActive)
                {
                    _enemyRemovalBuffer.Add(enemy);
                }
            }

            for (int index = 0; index < _enemyRemovalBuffer.Count; index++)
            {
                UntrackEnemy(_enemyRemovalBuffer[index]);
            }

            _enemyRemovalBuffer.Clear();
            return _activeEnemies.Count;
        }

        private void ApplyRunProgression(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyRunScaling scaling = GetCurrentEnemyRunScaling();
            enemy.ApplyRuntimeStats(enemy.CreateBaseRuntimeStats().Scale(scaling));
            ApplyRuntimeTuning(enemy, scaling);
        }

        private static void ApplyRuntimeTuning(EnemyController enemy, EnemyRunScaling scaling)
        {
            MonoBehaviour[] behaviours = enemy.GetComponents<MonoBehaviour>();

            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IEnemyRuntimeTunable tunable)
                {
                    tunable.ApplyRunScaling(scaling);
                }
            }
        }

        private Vector3 GetSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                if (spawnPoint != null)
                {
                    return spawnPoint.position;
                }
            }

            if (gameplayCamera != null && gameplayCamera.orthographic)
            {
                float halfHeight = gameplayCamera.orthographicSize;
                float halfWidth = halfHeight * gameplayCamera.aspect;
                float spawnX = Random.Range(
                    gameplayCamera.transform.position.x - halfWidth + GetHorizontalSpawnPadding(),
                    gameplayCamera.transform.position.x + halfWidth - GetHorizontalSpawnPadding());

                return new Vector3(
                    spawnX,
                    gameplayCamera.transform.position.y + halfHeight + GetSpawnYOffset(),
                    0f);
            }

            return transform.position;
        }

        private float GetSpawnYOffset()
        {
            return spawnConfig != null ? spawnConfig.SpawnYOffset : spawnYOffset;
        }

        private float GetHorizontalSpawnPadding()
        {
            return spawnConfig != null ? spawnConfig.HorizontalSpawnPadding : horizontalSpawnPadding;
        }
    }

    [Serializable]
    public sealed class EnemySpawnEntry
    {
        [SerializeField] private EnemyController prefab;
        [SerializeField] private float spawnWeight = 1f;
        [SerializeField] private float unlockAfterSeconds;
        [SerializeField] private EnemyProgressionRole progressionRole = EnemyProgressionRole.Auto;

        public EnemyController Prefab => prefab;

        public float GetUnlockAfterSeconds(RunProgressionConfig progressionConfig)
        {
            EnemyProgressionRole role = ResolveProgressionRole();

            if (progressionConfig != null)
            {
                return progressionConfig.GetUnlockAfterSeconds(role, unlockAfterSeconds);
            }

            return RunProgressionConfig.GetDefaultUnlockAfterSeconds(role, unlockAfterSeconds);
        }

        public float GetWeight(float elapsedSeconds, RunProgressionConfig progressionConfig)
        {
            EnemyProgressionRole role = ResolveProgressionRole();

            if (progressionConfig != null)
            {
                return progressionConfig.GetSpawnWeight(role, elapsedSeconds, spawnWeight);
            }

            return RunProgressionConfig.GetDefaultSpawnWeight(role, elapsedSeconds, spawnWeight);
        }

        private EnemyProgressionRole ResolveProgressionRole()
        {
            if (progressionRole != EnemyProgressionRole.Auto || prefab == null)
            {
                return progressionRole;
            }

            if (prefab.GetComponent<VomfyRangedAttackController>() != null)
            {
                return EnemyProgressionRole.Ranged;
            }

            if (prefab.GetComponent<ChomboomController>() != null)
            {
                return EnemyProgressionRole.ExploderMelee;
            }

            return EnemyProgressionRole.BasicMelee;
        }
    }
}
