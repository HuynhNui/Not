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

            if (Time.time >= _nextSpawnTime)
            {
                Spawn();
                _nextSpawnTime = Time.time + GetCurrentSpawnInterval();
            }
        }

        public void Spawn()
        {
            if (!_spawningEnabled || playerUnit == null)
            {
                return;
            }

            EnemyController selectedPrefab = SelectEnemyPrefab();

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
            enemyInstance.Killed -= HandleEnemyKilled;
            enemyInstance.Killed += HandleEnemyKilled;
            enemyInstance.Spawn();
        }

        public void SetSpawningEnabled(bool isEnabled)
        {
            _spawningEnabled = isEnabled;
        }

        private float GetCurrentSpawnInterval()
        {
            AnimationCurve activeDifficultyCurve = spawnConfig != null ? spawnConfig.DifficultyCurve : difficultyCurve;
            float activeBaseInterval = spawnConfig != null ? spawnConfig.BaseSpawnInterval : baseSpawnInterval;
            float activeMinimumInterval = spawnConfig != null ? spawnConfig.MinimumSpawnInterval : minimumSpawnInterval;
            float difficultyMultiplier = Mathf.Max(0.01f, activeDifficultyCurve.Evaluate(_elapsedTime));
            float scaledInterval = activeBaseInterval / difficultyMultiplier;
            return Mathf.Max(activeMinimumInterval, scaledInterval);
        }

        private bool HasSpawnableEnemy()
        {
            if (enemyPrefab != null)
            {
                return true;
            }

            if (spawnEntries == null)
            {
                return false;
            }

            for (int index = 0; index < spawnEntries.Count; index++)
            {
                EnemySpawnEntry entry = spawnEntries[index];

                if (entry != null && entry.Prefab != null && entry.GetWeight() > 0f && _elapsedTime >= entry.UnlockAfterSeconds)
                {
                    return true;
                }
            }

            return false;
        }

        private EnemyController SelectEnemyPrefab()
        {
            if (spawnEntries == null || spawnEntries.Count == 0)
            {
                return enemyPrefab;
            }

            float totalWeight = 0f;

            for (int index = 0; index < spawnEntries.Count; index++)
            {
                EnemySpawnEntry entry = spawnEntries[index];

                if (entry == null || entry.Prefab == null || _elapsedTime < entry.UnlockAfterSeconds)
                {
                    continue;
                }

                totalWeight += entry.GetWeight();
            }

            if (totalWeight <= 0f)
            {
                return enemyPrefab;
            }

            float roll = Random.Range(0f, totalWeight);
            float accumulatedWeight = 0f;

            for (int index = 0; index < spawnEntries.Count; index++)
            {
                EnemySpawnEntry entry = spawnEntries[index];

                if (entry == null || entry.Prefab == null || _elapsedTime < entry.UnlockAfterSeconds)
                {
                    continue;
                }

                accumulatedWeight += entry.GetWeight();

                if (roll <= accumulatedWeight)
                {
                    return entry.Prefab;
                }
            }

            return enemyPrefab;
        }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            if (enemy != null)
            {
                enemy.Killed -= HandleEnemyKilled;
            }

            EnemyKilled?.Invoke(enemy);
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

        public EnemyController Prefab => prefab;
        public float UnlockAfterSeconds => Mathf.Max(0f, unlockAfterSeconds);

        public float GetWeight()
        {
            return Mathf.Max(0f, spawnWeight);
        }
    }
}
