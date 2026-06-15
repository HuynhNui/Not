using System;
using System.Collections.Generic;
using _Project.Scripts.Data.Balance;
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
        private const int MaxSpawnBurstPerFrame = 8;

        [SerializeField] private EnemySpawnConfig spawnConfig;
        [SerializeField] private RunProgressionConfig runProgressionConfig;
        [SerializeField] private RunPressureConfig runPressureConfig;
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
        [SerializeField] private int minimumVisibleEnemies = 10;
        [SerializeField, Range(0f, 1f)] private float visibleFloorSpawnViewportY = 0.92f;
        [SerializeField] private float visibleFloorSpawnPadding = 0.35f;

        private float _elapsedTime;
        private float _nextSpawnTime;
        private float _spawnAccumulator;
        private float _activeThreat;
        private float _gateSpeedMultiplier = 1f;
        private float _gatePressureMultiplier = 1f;
        private bool _spawningEnabled = true;
        private readonly HashSet<EnemyController> _activeEnemies = new HashSet<EnemyController>();
        private readonly Dictionary<EnemyController, float> _activeThreatCosts =
            new Dictionary<EnemyController, float>();
        private readonly List<EnemyController> _enemyRemovalBuffer = new List<EnemyController>();

        public event Action<EnemyController> EnemyKilled;

        public int ActiveEnemyCount => GetActiveEnemyCount();
        public int VisibleEnemyCount => GetVisibleEnemyCount();
        public int CurrentMaxActiveEnemies => GetCurrentMaxActiveEnemies();
        public int CurrentMinimumVisibleEnemies => GetCurrentPressure().MinimumVisible;
        public float CurrentThreatBudget => GetCurrentPressure().ThreatBudget;
        public float CurrentActiveThreat
        {
            get
            {
                GetActiveEnemyCount();
                return _activeThreat;
            }
        }
        public float CurrentRawSpawnPerSecond => GetCurrentPressure().SpawnPerSecond;
        public float ElapsedTime => _elapsedTime;
        public float GateSpeedMultiplier => _gateSpeedMultiplier;
        public float GatePressureMultiplier => _gatePressureMultiplier;

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
            _spawnAccumulator = 0f;
            _activeThreat = 0f;
            _gateSpeedMultiplier = 1f;
            _gatePressureMultiplier = 1f;
            _activeEnemies.Clear();
            _activeThreatCosts.Clear();
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
            RunPressureSnapshot pressure = GetCurrentPressure();

            TopUpVisibleEnemies(pressure);

            if (GetActiveEnemyCount() >= pressure.ActiveCap)
            {
                return;
            }

            _spawnAccumulator = Mathf.Min(
                MaxSpawnBurstPerFrame,
                _spawnAccumulator + pressure.SpawnPerSecond * Time.deltaTime);

            int spawnedThisFrame = 0;
            while (_spawnAccumulator >= 1f
                && GetActiveEnemyCount() < pressure.ActiveCap
                && spawnedThisFrame < MaxSpawnBurstPerFrame)
            {
                if (!SpawnSingleEnemy(GetSpawnPosition(), false, pressure))
                {
                    _spawnAccumulator = 0f;
                    break;
                }

                _spawnAccumulator -= 1f;
                spawnedThisFrame++;
            }
        }

        public void BeginRun()
        {
            _elapsedTime = 0f;
            _nextSpawnTime = Time.time;
            _spawnAccumulator = 0f;
            _activeThreat = 0f;
            _gateSpeedMultiplier = 1f;
            _gatePressureMultiplier = 1f;
            _activeEnemies.Clear();
            _activeThreatCosts.Clear();
        }

        public void Spawn()
        {
            if (!_spawningEnabled || playerUnit == null)
            {
                return;
            }

            RunPressureSnapshot pressure = GetCurrentPressure();
            int batchSize = Mathf.Max(1, GetCurrentSpawnBatchSize());
            for (int spawnIndex = 0; spawnIndex < batchSize; spawnIndex++)
            {
                if (GetActiveEnemyCount() >= pressure.ActiveCap)
                {
                    return;
                }

                SpawnSingleEnemy(GetSpawnPosition(), false, pressure);
            }
        }

        public void SetSpawningEnabled(bool isEnabled)
        {
            _spawningEnabled = isEnabled;
        }

        private bool SpawnSingleEnemy(
            Vector3 spawnPosition,
            bool basicOnly,
            RunPressureSnapshot pressure)
        {
            if (!_spawningEnabled || playerUnit == null)
            {
                return false;
            }

            EnemySpawnEntry selectedEntry = SelectEnemyEntry(basicOnly, pressure.ThreatBudget);
            EnemyController selectedPrefab = selectedEntry != null ? selectedEntry.Prefab : enemyPrefab;

            if (selectedPrefab == null)
            {
                return false;
            }

            EnemyController enemyInstance = poolSystem != null
                ? poolSystem.Spawn(selectedPrefab, spawnPosition, Quaternion.identity)
                : Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

            if (enemyInstance == null)
            {
                return false;
            }

            enemyInstance.SetPoolSystem(poolSystem);
            enemyInstance.Init(playerUnit.transform, playerUnit, gameplayCamera);
            enemyInstance.SetRewardPoints(
                selectedEntry != null
                    ? selectedEntry.GetRewardPoints()
                    : EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Basic));
            enemyInstance.SetExternalMoveSpeedMultiplier(_gateSpeedMultiplier);
            ApplyRunProgression(enemyInstance);
            enemyInstance.Killed -= HandleEnemyKilled;
            enemyInstance.Killed += HandleEnemyKilled;
            enemyInstance.Despawned -= HandleEnemyDespawned;
            enemyInstance.Despawned += HandleEnemyDespawned;
            enemyInstance.Spawn();
            TrackEnemy(enemyInstance, selectedEntry != null ? selectedEntry.GetThreatCost() : 0f);
            return true;
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
            return GetCurrentPressure().ActiveCap;
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
            RunPressureSnapshot pressure = GetCurrentPressure();
            return new EnemyRunScaling(
                pressure.HpMultiplier,
                pressure.SpeedMultiplier,
                pressure.DamageMultiplier,
                pressure.SpeedMultiplier);
        }

        private RunPressureSnapshot GetCurrentPressure()
        {
            RunPressureSnapshot pressure = runPressureConfig != null
                ? runPressureConfig.Evaluate(_elapsedTime)
                : RunPressureConfig.EvaluateDefault(_elapsedTime);

            return ScalePressure(pressure, _gatePressureMultiplier);
        }

        public void SetBalanceConfiguration(
            RunPressureConfig pressureConfig,
            IReadOnlyList<EnemyRoleConfig> roleConfigs)
        {
            if (pressureConfig != null)
            {
                runPressureConfig = pressureConfig;
            }

            if (spawnEntries == null || roleConfigs == null)
            {
                return;
            }

            for (int entryIndex = 0; entryIndex < spawnEntries.Count; entryIndex++)
            {
                EnemySpawnEntry entry = spawnEntries[entryIndex];
                if (entry == null)
                {
                    continue;
                }

                BalanceEnemyRole role = entry.ResolveBalanceRole();
                for (int configIndex = 0; configIndex < roleConfigs.Count; configIndex++)
                {
                    EnemyRoleConfig roleConfig = roleConfigs[configIndex];
                    if (roleConfig != null && roleConfig.Role == role)
                    {
                        entry.SetRoleConfig(roleConfig);
                        break;
                    }
                }
            }
        }

        public void SetGateSpeedMultiplier(float multiplier)
        {
            _gateSpeedMultiplier = Mathf.Max(0f, multiplier);

            foreach (EnemyController enemy in _activeEnemies)
            {
                enemy?.SetExternalMoveSpeedMultiplier(_gateSpeedMultiplier);
            }
        }

        public void SetGatePressureMultiplier(float multiplier)
        {
            _gatePressureMultiplier = Mathf.Max(0.1f, multiplier);
        }

        private static RunPressureSnapshot ScalePressure(
            RunPressureSnapshot pressure,
            float multiplier)
        {
            float safeMultiplier = Mathf.Max(0.1f, multiplier);
            int activeCap = Mathf.Max(1, Mathf.CeilToInt(pressure.ActiveCap * safeMultiplier));
            int minimumVisible = Mathf.Clamp(
                Mathf.CeilToInt(pressure.MinimumVisible * safeMultiplier),
                0,
                activeCap);

            return new RunPressureSnapshot(
                activeCap,
                minimumVisible,
                pressure.ThreatBudget * safeMultiplier,
                pressure.SpawnPerSecond * safeMultiplier,
                pressure.HpMultiplier,
                pressure.DamageMultiplier,
                pressure.SpeedMultiplier);
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
                    && entry.GetBalanceWeight() > 0f
                    && _elapsedTime >= entry.GetBalanceUnlockAfterSeconds())
                {
                    return true;
                }
            }

            return false;
        }

        private EnemySpawnEntry SelectEnemyEntry(bool basicOnly, float threatBudget)
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
                    || (basicOnly && !entry.MatchesBalanceRole(BalanceEnemyRole.Basic))
                    || _elapsedTime < entry.GetBalanceUnlockAfterSeconds()
                    || !EnemyRoleBalanceDefaults.CanFitThreat(
                        _activeThreat,
                        entry.GetThreatCost(),
                        threatBudget))
                {
                    continue;
                }

                totalWeight += entry.GetBalanceWeight();
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
                    || (basicOnly && !entry.MatchesBalanceRole(BalanceEnemyRole.Basic))
                    || _elapsedTime < entry.GetBalanceUnlockAfterSeconds()
                    || !EnemyRoleBalanceDefaults.CanFitThreat(
                        _activeThreat,
                        entry.GetThreatCost(),
                        threatBudget))
                {
                    continue;
                }

                accumulatedWeight += entry.GetBalanceWeight();

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

        private void TrackEnemy(EnemyController enemy, float threatCost)
        {
            if (enemy == null)
            {
                return;
            }

            if (!_activeEnemies.Add(enemy))
            {
                return;
            }

            float clampedThreatCost = Mathf.Max(0f, threatCost);
            _activeThreatCosts[enemy] = clampedThreatCost;
            _activeThreat += clampedThreatCost;
        }

        private void UntrackEnemy(EnemyController enemy)
        {
            if (enemy == null)
            {
                _activeEnemies.Remove(enemy);
                return;
            }

            _activeEnemies.Remove(enemy);

            if (_activeThreatCosts.TryGetValue(enemy, out float threatCost))
            {
                _activeThreat = Mathf.Max(0f, _activeThreat - threatCost);
                _activeThreatCosts.Remove(enemy);
            }

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

        private int GetVisibleEnemyCount()
        {
            if (GetActiveEnemyCount() <= 0)
            {
                return 0;
            }

            int visibleCount = 0;

            foreach (EnemyController enemy in _activeEnemies)
            {
                if (enemy != null && enemy.IsActive && enemy.IsInsideGameplayCamera())
                {
                    visibleCount++;
                }
            }

            return visibleCount;
        }

        private void TopUpVisibleEnemies(RunPressureSnapshot pressure)
        {
            int targetVisibleCount = Mathf.Clamp(
                pressure.MinimumVisible,
                0,
                pressure.ActiveCap);

            if (targetVisibleCount <= 0)
            {
                return;
            }

            int activeCount = GetActiveEnemyCount();
            int maxActiveEnemies = pressure.ActiveCap;

            if (activeCount >= maxActiveEnemies)
            {
                return;
            }

            int missingVisibleEnemies = targetVisibleCount - GetVisibleEnemyCount();

            if (missingVisibleEnemies <= 0)
            {
                return;
            }

            int spawnCount = Mathf.Min(missingVisibleEnemies, maxActiveEnemies - activeCount);

            for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
            {
                if (!SpawnSingleEnemy(GetVisibleFloorSpawnPosition(), true, pressure))
                {
                    return;
                }
            }
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

        private Vector3 GetVisibleFloorSpawnPosition()
        {
            if (gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return GetSpawnPosition();
            }

            float halfHeight = gameplayCamera.orthographicSize;
            float halfWidth = halfHeight * gameplayCamera.aspect;
            Vector3 cameraPosition = gameplayCamera.transform.position;
            float horizontalPadding = Mathf.Max(0f, visibleFloorSpawnPadding);
            float spawnViewportY = Mathf.Clamp01(visibleFloorSpawnViewportY);

            float spawnX = Random.Range(
                cameraPosition.x - halfWidth + horizontalPadding,
                cameraPosition.x + halfWidth - horizontalPadding);
            float spawnY = cameraPosition.y - halfHeight + (halfHeight * 2f * spawnViewportY);

            return new Vector3(spawnX, spawnY, 0f);
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
        [SerializeField] private bool overrideBalanceRole;
        [SerializeField] private BalanceEnemyRole balanceRole = BalanceEnemyRole.Basic;
        [SerializeField] private EnemyRoleConfig roleConfig;

        public EnemyController Prefab => prefab;

        public void SetRoleConfig(EnemyRoleConfig value)
        {
            roleConfig = value;
        }

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

        public bool MatchesProgressionRole(EnemyProgressionRole role)
        {
            return ResolveProgressionRole() == role;
        }

        public float GetBalanceUnlockAfterSeconds()
        {
            return roleConfig != null
                ? roleConfig.UnlockTimeSeconds
                : Mathf.Max(
                    unlockAfterSeconds,
                    EnemyRoleBalanceDefaults.GetUnlockTimeSeconds(ResolveBalanceRole()));
        }

        public float GetThreatCost()
        {
            return roleConfig != null
                ? roleConfig.ThreatCost
                : EnemyRoleBalanceDefaults.GetThreatCost(ResolveBalanceRole());
        }

        public float GetRewardPoints()
        {
            return roleConfig != null
                ? roleConfig.RewardPoints
                : EnemyRoleBalanceDefaults.GetRewardPoints(ResolveBalanceRole());
        }

        public float GetBalanceWeight()
        {
            float multiplier = roleConfig != null ? roleConfig.SpawnWeightMultiplier : 1f;
            return Mathf.Max(0f, spawnWeight) * Mathf.Max(0f, multiplier);
        }

        public bool MatchesBalanceRole(BalanceEnemyRole role)
        {
            return ResolveBalanceRole() == role;
        }

        public BalanceEnemyRole ResolveBalanceRole()
        {
            if (roleConfig != null)
            {
                return roleConfig.Role;
            }

            if (overrideBalanceRole)
            {
                return balanceRole;
            }

            return ResolveProgressionRole() switch
            {
                EnemyProgressionRole.ExploderMelee => BalanceEnemyRole.Chomboom,
                EnemyProgressionRole.Ranged => BalanceEnemyRole.Vomfy,
                _ => BalanceEnemyRole.Basic
            };
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
