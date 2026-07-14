using System;
using System.Collections.Generic;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Gates;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.Balance;
using UnityEngine;
using Random = UnityEngine.Random;
using RuntimeEnemySpawnerSystem =
    _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimePoolSystem = _Project.Scripts.Systems.PoolSystem.PoolSystem;

namespace _Project.Scripts.Systems.GateSystem
{
    /// <summary>
    /// Manages gate presentation, activation flow, and upgrade routing during the run.
    /// </summary>
    public sealed class GateSystem : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private GateLogic gatePrefab;
        [SerializeField] private float spawnIntervalSeconds = 20f;
        [SerializeField] private GatePoolConfig gatePoolConfig;
        [SerializeField] private GateScalingProfile gateScalingProfile;
        [SerializeField] private bool useLegacyOfferGeneration;
        [SerializeField] private float spawnAboveCameraOffset = 1.25f;
        [SerializeField] private bool useViewportLanes = true;
        [SerializeField] private float viewportLaneMin = 0.12f;
        [SerializeField] private float viewportLaneMax = 0.88f;
        [SerializeField] private float gateHalfWidth = 0.75f;
        [SerializeField] private float laneSpacing = 2.2f;
        [SerializeField] private int gateCount = 3;
        [SerializeField] private float laneGapWorld = 0.08f;
        [SerializeField] private float gateHeightToWidth = 1.35f;
        [SerializeField, Range(0.25f, 2f)] private float gateSizeMultiplier = 1f;
        [SerializeField, Range(0.05f, 0.6f)] private float maxGateHeightViewport = 0.18f;
        [SerializeField] private float minGateWorldWidth = 0.55f;

        [Header("Viewport Safe Zone")]
        [SerializeField, Range(0f, 0.3f)] private float topReservedViewport = 0.18f;
        [SerializeField, Range(0f, 0.3f)] private float bottomReservedViewport = 0.16f;
        [SerializeField, Range(0f, 0.3f)] private float horizontalViewportPadding = 0.10f;
        [SerializeField] private float topSpawnMarginPixels = 12f;

        [Header("Configs (pick 3 each spawn)")]
        [SerializeField] private List<GateConfig> availableGateConfigs = new List<GateConfig>();

        [Header("Controlled random offers")]
        [SerializeField] private bool generateOffersAtRuntime = true;
        [SerializeField, Range(0f, 1f)] private float minimumBuffGateRatio = 0.34f;
        [SerializeField] private int maxProjectileCount = 50;
        [SerializeField] private int maxPlayerCount = 50;
        [SerializeField] private List<GateOfferRule> offerRules = new List<GateOfferRule>
        {
            new GateOfferRule(GateStatTarget.Damage, 1f, 1f, 0f, 2f, 0f, 999f, false),
            new GateOfferRule(GateStatTarget.FireRate, 1f, 1f, 1.25f, 2f, 0.25f, 20f, false),
            new GateOfferRule(GateStatTarget.MaxHp, 5f, 5f, 1.25f, 2f, 1f, 999f, false),
            new GateOfferRule(GateStatTarget.ProjectileCount, 1f, 1f, 0f, 2f, 1f, 50f, true),
            new GateOfferRule(GateStatTarget.PlayerCount, 1f, 1f, 0f, 2f, 1f, 50f, true)
        };

        [Header("Runtime references")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MainPlayerUnit mainPlayerUnit;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private RuntimePoolSystem poolSystem;
        [SerializeField] private RuntimeEnemySpawnerSystem enemySpawnerSystem;
        [SerializeField] private GateRuntimeEffectController runtimeEffectController;

        [SerializeField] private List<GateLogic> activeGates = new List<GateLogic>();

        private float _nextSpawnTime;
        private float _runElapsedSeconds;
        private int _gateSetCount;
        private bool _spawningEnabled;
        private bool _isGateSetActive;
        private bool _choiceLocked;
        private readonly List<GateConfig> _spawnConfigBuffer = new List<GateConfig>();
        private readonly List<GateOfferCandidate> _candidateBuffer = new List<GateOfferCandidate>();
        private readonly List<GateOfferCandidate> _buffCandidateBuffer = new List<GateOfferCandidate>();
        private readonly List<GateOfferCandidate> _neutralCandidateBuffer = new List<GateOfferCandidate>();
        private readonly List<BalanceGateCategory> _lastSpawnCategories =
            new List<BalanceGateCategory>();
        private readonly Dictionary<RuntimeGateConfigKey, GateConfig> _runtimeConfigCache =
            new Dictionary<RuntimeGateConfigKey, GateConfig>();
        private int _consecutiveMajorEligibleMisses;
        private int _majorEligibleRolls;
        private int _majorOffers;
        private int _majorPityForcedOffers;
        private int _maxConsecutiveMajorMisses;
        private bool _isBenchmarkMode;
        private static readonly string[] TutorialDefaultGateIds =
        {
            "stable_damage",
            "utility_repair",
            "risky_glass_cannon"
        };

        public event Action<int, int, GateConfig> GateShown;
        public event Action<int, GateConfig> GateSelected;
        public event Action<MajorGateRollTelemetry> MajorRollEvaluated;

        public float RunElapsedSeconds => _runElapsedSeconds;
        public int GateSetCount => _gateSetCount;
        public bool IsGateSetActive => _isGateSetActive;
        public float GateCadenceSeconds => gatePoolConfig != null
            ? gatePoolConfig.GateCadenceSeconds
            : GatePoolConfig.DefaultGateCadenceSeconds;
        public float MajorGateCadenceSeconds => gatePoolConfig != null
            ? gatePoolConfig.MajorGateCadenceSeconds
            : GatePoolConfig.DefaultMajorGateCadenceSeconds;
        public float CurrentMajorChance => gateScalingProfile != null
            ? gateScalingProfile.GetMajorChance(_runElapsedSeconds)
            : GetMajorChance(_runElapsedSeconds);
        public IReadOnlyList<BalanceGateCategory> LastSpawnCategories => _lastSpawnCategories;
        public string CurrentPhaseId => gateScalingProfile != null
            ? gateScalingProfile.EvaluatePhase(_runElapsedSeconds).PhaseId
            : string.Empty;
        public int MajorEligibleRolls => _majorEligibleRolls;
        public int MajorOffers => _majorOffers;
        public int MajorPityForcedOffers => _majorPityForcedOffers;
        public int MaxConsecutiveMajorMisses => _maxConsecutiveMajorMisses;
        public GateRunStatCaps CurrentRunStatCaps => gateScalingProfile != null
            ? gateScalingProfile.RunStatCaps
            : null;

        public void SetGatePoolConfig(GatePoolConfig value)
        {
            if (value != null)
            {
                gatePoolConfig = value;
                ClearRuntimeConfigCache();
            }
        }

        public void SetGateScalingProfile(GateScalingProfile value)
        {
            gateScalingProfile = value;
            gateScalingProfile?.ValidateValues();
            ClearRuntimeConfigCache();
            runtimeEffectController?.SetGateScalingProfile(gateScalingProfile);
        }

        public void SetBenchmarkMode(bool isBenchmarkMode)
        {
            _isBenchmarkMode = isBenchmarkMode && (Application.isEditor || Debug.isDebugBuild);
        }

        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            ResolveGameplayCamera();
            poolSystem ??= FindAnyObjectByType<RuntimePoolSystem>();
            enemySpawnerSystem ??= FindAnyObjectByType<RuntimeEnemySpawnerSystem>();
            EnsureDefaultOfferRules();

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (mainPlayerUnit == null)
            {
                mainPlayerUnit = FindAnyObjectByType<MainPlayerUnit>();
            }

            runtimeEffectController ??= GetComponent<GateRuntimeEffectController>();
            if (runtimeEffectController == null)
            {
                runtimeEffectController = gameObject.AddComponent<GateRuntimeEffectController>();
            }

            runtimeEffectController.Configure(mainPlayerUnit, playerController, enemySpawnerSystem);
            runtimeEffectController.SetGateScalingProfile(gateScalingProfile);
            _nextSpawnTime = GateCadenceSeconds;
            _runElapsedSeconds = 0f;
            _gateSetCount = 0;
            ResetMajorRuntimeState();
            _spawningEnabled = false;
            _isGateSetActive = false;
            _choiceLocked = false;
        }

        private void Update()
        {
            if (!_spawningEnabled || mainPlayerUnit == null || mainPlayerUnit.IsDead)
            {
                return;
            }

            _runElapsedSeconds += Time.deltaTime;

            if (_isGateSetActive)
            {
                return;
            }

            if (_runElapsedSeconds >= _nextSpawnTime)
            {
                Spawn();
                _nextSpawnTime += GateCadenceSeconds;
            }
        }

        public void BeginRun()
        {
            ClearActiveGates();
            _runElapsedSeconds = 0f;
            _gateSetCount = 0;
            _nextSpawnTime = GateCadenceSeconds;
            ResetMajorRuntimeState();
            _spawningEnabled = true;
            runtimeEffectController?.BeginRun();
        }

        public void SetSpawningEnabled(bool isEnabled)
        {
            _spawningEnabled = isEnabled;
        }

        public GateLogic SpawnTutorialGate(Vector3 spawnPosition)
        {
            return SpawnTutorialGateById("major_recruit", spawnPosition);
        }

        public GateLogic SpawnTutorialGateById(string gateId, Vector3 spawnPosition)
        {
            if (gatePrefab == null || mainPlayerUnit == null)
            {
                return null;
            }

            ClearActiveGates();
            _choiceLocked = false;
            _isGateSetActive = true;
            ResolveGameplayCamera();

            if (!TryResolveTutorialGateConfig(gateId, out GateConfig config))
            {
                _isGateSetActive = false;
                return null;
            }

            float width = Mathf.Max(minGateWorldWidth, gateHalfWidth * 2f) * Mathf.Max(0.25f, gateSizeMultiplier);
            GateLogic instance = SpawnGateInstance(
                config,
                spawnPosition,
                spawnPosition.x,
                width,
                width * Mathf.Max(0.5f, gateHeightToWidth),
                0);

            if (instance == null)
            {
                _isGateSetActive = false;
            }

            return instance;
        }

        public IReadOnlyList<GateLogic> SpawnTutorialDefaultGateSet()
        {
            var spawnedGates = new List<GateLogic>();
            if (gatePrefab == null || mainPlayerUnit == null)
            {
                return spawnedGates;
            }

            ClearActiveGates();
            _choiceLocked = false;
            _isGateSetActive = true;
            ResolveGameplayCamera();

            IReadOnlyList<GateConfig> configs = GetTutorialDefaultGateConfigs();
            for (int index = 0; index < configs.Count; index++)
            {
                GateConfig config = configs[index];
                if (config == null)
                {
                    continue;
                }

                GateLaneLayout laneLayout = GetGateLaneLayout(index, configs.Count);
                Vector3 spawnPosition = new Vector3(
                    laneLayout.CenterX,
                    GetSpawnWorldY(laneLayout.GateHeight),
                    0f);
                GateLogic instance = SpawnGateInstance(
                    config,
                    spawnPosition,
                    laneLayout.CenterX,
                    laneLayout.GateWidth,
                    laneLayout.GateHeight,
                    index);
                if (instance != null)
                {
                    spawnedGates.Add(instance);
                }
            }

            if (spawnedGates.Count <= 0)
            {
                _isGateSetActive = false;
            }

            return spawnedGates;
        }

        public IReadOnlyList<GateConfig> GetTutorialDefaultGateConfigs()
        {
            var configs = new List<GateConfig>(TutorialDefaultGateIds.Length);
            for (int index = 0; index < TutorialDefaultGateIds.Length; index++)
            {
                if (TryResolveTutorialGateConfig(TutorialDefaultGateIds[index], out GateConfig config))
                {
                    configs.Add(config);
                }
            }

            return configs;
        }

        public bool TryResolveTutorialGateConfig(string gateId, out GateConfig config)
        {
            config = null;
            if (!TryFindBalanceGateEntry(gateId, out BalanceGateEntry entry))
            {
                return false;
            }

            config = GetOrCreateRuntimeConfig(entry);
            return config != null;
        }

        public void ClearTutorialGates()
        {
            ClearActiveGates();
            _choiceLocked = false;
        }

        public void Spawn()
        {
            if (gatePrefab == null || mainPlayerUnit == null)
            {
                return;
            }

            if (!generateOffersAtRuntime && (availableGateConfigs == null || availableGateConfigs.Count <= 0))
            {
                return;
            }

            ClearActiveGates();
            _choiceLocked = false;
            _isGateSetActive = true;

            ResolveGameplayCamera();

            int count = useLegacyOfferGeneration ? Mathf.Max(1, gateCount) : 3;
            BuildSpawnConfigs(count);

            if (generateOffersAtRuntime && _spawnConfigBuffer.Count <= 0)
            {
                _isGateSetActive = false;
                return;
            }

            for (int index = 0; index < count; index++)
            {
                GateConfig config = PickGateConfig(index);
                if (config == null)
                {
                    continue;
                }

                GateLaneLayout laneLayout = GetGateLaneLayout(index, count);
                float spawnY = GetSpawnWorldY(laneLayout.GateHeight);
                Vector3 spawnPosition = new Vector3(laneLayout.CenterX, spawnY, 0f);

                GateLogic instance = poolSystem != null
                    ? poolSystem.Spawn(gatePrefab, spawnPosition, Quaternion.identity)
                    : Instantiate(gatePrefab, spawnPosition, Quaternion.identity);

                if (instance == null)
                {
                    continue;
                }

                instance.Init(
                    config,
                    this,
                    mainPlayerUnit,
                    playerController,
                    gameplayCamera,
                    poolSystem,
                    laneLayout.CenterX,
                    laneLayout.GateWidth,
                    laneLayout.GateHeight);
                instance.Spawn();
                activeGates.Add(instance);
                GateShown?.Invoke(_gateSetCount, index, config);
            }
        }

        public void ApplyGateConfig(GateConfig config)
        {
            runtimeEffectController?.Apply(config);
        }

        public void HandleGateChosen(GateLogic chosen)
        {
            if (!_isGateSetActive || _choiceLocked || chosen == null)
            {
                return;
            }

            _choiceLocked = true;
            GateSelected?.Invoke(_gateSetCount, chosen.GateConfig);
            chosen.ApplyEffect();

            for (int index = activeGates.Count - 1; index >= 0; index--)
            {
                GateLogic gate = activeGates[index];
                if (gate == null)
                {
                    activeGates.RemoveAt(index);
                    continue;
                }

                if (gate != chosen)
                {
                    gate.Despawn();
                    activeGates.RemoveAt(index);
                }
            }

            if (chosen.ConsumeAfterUse)
            {
                chosen.Despawn();
            }

            activeGates.Clear();
            _isGateSetActive = false;
        }

        public void HandleGateExpired(GateLogic expired)
        {
            if (expired == null)
            {
                return;
            }

            activeGates.Remove(expired);
            if (activeGates.Count == 0)
            {
                _isGateSetActive = false;
                _choiceLocked = false;
            }
        }

        private GateConfig PickGateConfig(int indexHint)
        {
            if (_spawnConfigBuffer.Count > 0)
            {
                return indexHint >= 0 && indexHint < _spawnConfigBuffer.Count
                    ? _spawnConfigBuffer[indexHint]
                    : null;
            }

            if (availableGateConfigs == null || availableGateConfigs.Count <= 0)
            {
                return null;
            }

            // Best-effort unique pick for the three gates.
            // If there are fewer than 3 configs, duplicates are allowed.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                GateConfig candidate = availableGateConfigs[Random.Range(0, availableGateConfigs.Count)];
                if (candidate == null)
                {
                    continue;
                }

                if (availableGateConfigs.Count < 3)
                {
                    return candidate;
                }

                bool alreadyUsed = false;
                for (int gateIndex = 0; gateIndex < activeGates.Count; gateIndex++)
                {
                    GateLogic existing = activeGates[gateIndex];
                    if (existing != null && existing.GateConfig == candidate)
                    {
                        alreadyUsed = true;
                        break;
                    }
                }

                if (!alreadyUsed)
                {
                    return candidate;
                }
            }

            return availableGateConfigs[Random.Range(0, availableGateConfigs.Count)];
        }

        private void BuildSpawnConfigs(int count)
        {
            _spawnConfigBuffer.Clear();

            if (!useLegacyOfferGeneration)
            {
                BuildBalanceV1SpawnConfigs();
                return;
            }

            if (!generateOffersAtRuntime)
            {
                return;
            }

            EnsureDefaultOfferRules();
            BuildCandidateBuffers();

            int minimumBuffCount = Mathf.Clamp(
                Mathf.CeilToInt(count * minimumBuffGateRatio),
                0,
                count);

            for (int index = 0; index < minimumBuffCount; index++)
            {
                if (!TryTakeRandomCandidate(_buffCandidateBuffer, out GateOfferCandidate candidate))
                {
                    break;
                }

                _spawnConfigBuffer.Add(CreateRuntimeConfig(candidate));
                RemoveMatchingCandidate(_neutralCandidateBuffer, candidate);
            }

            while (_spawnConfigBuffer.Count < count)
            {
                if (!TryTakeRandomCandidate(_neutralCandidateBuffer, out GateOfferCandidate candidate))
                {
                    break;
                }

                _spawnConfigBuffer.Add(CreateRuntimeConfig(candidate));
                RemoveMatchingCandidate(_buffCandidateBuffer, candidate);
            }

            while (_spawnConfigBuffer.Count < count && availableGateConfigs != null && availableGateConfigs.Count > 0)
            {
                GateConfig fallback = availableGateConfigs[Random.Range(0, availableGateConfigs.Count)];
                if (fallback != null)
                {
                    _spawnConfigBuffer.Add(fallback);
                }
                else
                {
                    break;
                }
            }
        }

        private void BuildBalanceV1SpawnConfigs()
        {
            _lastSpawnCategories.Clear();
            _gateSetCount++;

            var categories = new List<BalanceGateCategory>
            {
                BalanceGateCategory.Stable,
                BalanceGateCategory.Utility,
                BalanceGateCategory.Risky
            };

            MajorRollResult majorRoll = EvaluateMajorForCurrentSet(Random.value);
            if (majorRoll.MajorSpawned)
            {
                categories[0] = BalanceGateCategory.Major;
            }

            for (int index = 0; index < categories.Count; index++)
            {
                BalanceGateCategory category = categories[index];
                BalanceGateEntry entry = PickBalanceEntry(
                    category,
                    _runElapsedSeconds,
                    requireApplicable: true);
                if (entry == null && category == BalanceGateCategory.Major)
                {
                    category = BalanceGateCategory.Stable;
                    entry = PickBalanceEntry(category, _runElapsedSeconds, requireApplicable: true);
                }

                if (entry == null)
                {
                    entry = PickBalanceEntry(category, _runElapsedSeconds, requireApplicable: false);
                }

                if (entry == null && category != BalanceGateCategory.Stable)
                {
                    category = BalanceGateCategory.Stable;
                    entry = PickBalanceEntry(category, _runElapsedSeconds, requireApplicable: true)
                        ?? PickBalanceEntry(category, _runElapsedSeconds, requireApplicable: false);
                }

                if (entry == null)
                {
                    continue;
                }

                ResolvedGateEntry resolved = ResolveGateEntry(entry, _runElapsedSeconds);
                if (!resolved.IsValid)
                {
                    continue;
                }

                _lastSpawnCategories.Add(category);
                _spawnConfigBuffer.Add(GetOrCreateRuntimeConfig(resolved, _runElapsedSeconds));
            }
        }

        private BalanceGateEntry PickBalanceEntry(
            BalanceGateCategory category,
            float elapsedSeconds,
            bool requireApplicable)
        {
            IReadOnlyList<BalanceGateEntry> source = gatePoolConfig != null
                && gatePoolConfig.Entries != null
                && gatePoolConfig.Entries.Count > 0
                    ? gatePoolConfig.Entries
                    : GatePoolConfig.CreateDefaultEntries();

            float totalWeight = 0f;
            for (int index = 0; index < source.Count; index++)
            {
                BalanceGateEntry entry = source[index];
                if (entry != null
                    && entry.Category == category
                    && elapsedSeconds >= entry.MinTimeSeconds
                    && IsEntryAllowedForCurrentRun(entry)
                    && (!requireApplicable || IsGateApplicable(entry, elapsedSeconds)))
                {
                    totalWeight += GetResolvedOfferWeight(entry, elapsedSeconds);
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float accumulated = 0f;
            for (int index = 0; index < source.Count; index++)
            {
                BalanceGateEntry entry = source[index];
                if (entry == null
                    || entry.Category != category
                    || elapsedSeconds < entry.MinTimeSeconds
                    || !IsEntryAllowedForCurrentRun(entry)
                    || (requireApplicable && !IsGateApplicable(entry, elapsedSeconds)))
                {
                    continue;
                }

                accumulated += GetResolvedOfferWeight(entry, elapsedSeconds);
                if (roll <= accumulated)
                {
                    return entry;
                }
            }

            return null;
        }

        private GateConfig GetOrCreateRuntimeConfig(BalanceGateEntry entry)
        {
            ResolvedGateEntry resolved = ResolveGateEntry(entry, _runElapsedSeconds);
            return GetOrCreateRuntimeConfig(resolved, _runElapsedSeconds);
        }

        private ResolvedGateEntry ResolveGateEntry(BalanceGateEntry entry, float elapsedSeconds)
        {
            return gateScalingProfile != null
                ? gateScalingProfile.Resolve(entry, elapsedSeconds)
                : ResolvedGateEntry.FromBase(entry, string.Empty);
        }

        private GateConfig GetOrCreateRuntimeConfig(ResolvedGateEntry entry, float elapsedSeconds)
        {
            if (!entry.IsValid)
            {
                return null;
            }

            var key = new RuntimeGateConfigKey(entry, elapsedSeconds);
            if (_runtimeConfigCache.TryGetValue(key, out GateConfig cached)
                && cached != null)
            {
                return cached;
            }

            GateConfig config = ScriptableObject.CreateInstance<GateConfig>();
            config.name = string.IsNullOrWhiteSpace(entry.PhaseId)
                ? $"RuntimeGate_{entry.GateId}"
                : $"RuntimeGate_{entry.GateId}_{entry.PhaseId}";
            config.ConfigureRuntime(entry, elapsedSeconds);
            _runtimeConfigCache[key] = config;
            return config;
        }

        private bool IsGateApplicable(BalanceGateEntry entry, float elapsedSeconds)
        {
            if (entry == null)
            {
                return false;
            }

            GateConfig config = GetOrCreateRuntimeConfig(
                ResolveGateEntry(entry, elapsedSeconds),
                elapsedSeconds);
            var before = GateStatSnapshot.FromRuntime(mainPlayerUnit, playerController);
            GateRunStatCaps caps = gateScalingProfile != null
                ? gateScalingProfile.RunStatCaps
                : null;
            GateEffectPreviewResult preview = GateEffectPreview.Preview(
                config,
                before,
                caps,
                maxProjectileCount,
                maxPlayerCount);

            if (preview.HasStatChange)
            {
                return true;
            }

            return entry.EffectType == BalanceEffectType.HealMissingHpRatio
                || entry.EffectType == BalanceEffectType.BarrierHits
                || entry.EffectType == BalanceEffectType.EnemySpeedMultiplier
                || entry.EffectType == BalanceEffectType.CoinRewardMultiplier;
        }

        private bool HasApplicableEntry(BalanceGateCategory category, float elapsedSeconds)
        {
            IReadOnlyList<BalanceGateEntry> source = gatePoolConfig != null
                && gatePoolConfig.Entries != null
                && gatePoolConfig.Entries.Count > 0
                    ? gatePoolConfig.Entries
                    : GatePoolConfig.CreateDefaultEntries();

            for (int index = 0; index < source.Count; index++)
            {
                BalanceGateEntry entry = source[index];
                if (entry != null
                    && entry.Category == category
                    && elapsedSeconds >= entry.MinTimeSeconds
                    && IsEntryAllowedForCurrentRun(entry)
                    && IsGateApplicable(entry, elapsedSeconds))
                {
                    return true;
                }
            }

            return false;
        }

        private MajorRollResult EvaluateMajorForCurrentSet(float randomValue)
        {
            bool eligible = IsMajorEligibilitySet(
                _gateSetCount,
                GateCadenceSeconds,
                MajorGateCadenceSeconds);
            float chance = CurrentMajorChance;
            MajorGateSettings settings = gateScalingProfile != null
                ? gateScalingProfile.MajorSettings
                : new MajorGateSettings();
            MajorRollResult result = MajorRollResult.Evaluate(
                eligible,
                chance,
                randomValue,
                _consecutiveMajorEligibleMisses,
                settings.GuaranteedAfterEligibleMisses,
                HasApplicableEntry(BalanceGateCategory.Major, _runElapsedSeconds));

            if (result.IsEligible)
            {
                _majorEligibleRolls++;
                _consecutiveMajorEligibleMisses = result.ConsecutiveMissesAfter;
                _maxConsecutiveMajorMisses = Mathf.Max(
                    _maxConsecutiveMajorMisses,
                    _consecutiveMajorEligibleMisses);

                if (result.MajorSpawned)
                {
                    _majorOffers++;
                }

                if (result.WasForced)
                {
                    _majorPityForcedOffers++;
                }
            }

            MajorRollEvaluated?.Invoke(new MajorGateRollTelemetry(
                _gateSetCount,
                _runElapsedSeconds,
                eligible,
                chance,
                randomValue,
                result.MajorSpawned,
                result.WasForced,
                _consecutiveMajorEligibleMisses,
                result.FailureReason));

            return result;
        }

        private bool IsEntryAllowedForCurrentRun(BalanceGateEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            return !_isBenchmarkMode
                || !string.Equals(entry.GateId, "risky_bounty", StringComparison.Ordinal);
        }

        private float GetResolvedOfferWeight(BalanceGateEntry entry, float elapsedSeconds)
        {
            if (entry == null)
            {
                return 0f;
            }

            ResolvedGateEntry resolved = ResolveGateEntry(entry, elapsedSeconds);
            return Mathf.Max(0f, entry.Weight) * resolved.OfferWeightMultiplier;
        }

        private GateLogic SpawnGateInstance(
            GateConfig config,
            Vector3 spawnPosition,
            float laneWorldX,
            float targetWorldWidth,
            float targetWorldHeight,
            int gateIndex)
        {
            if (config == null)
            {
                return null;
            }

            GateLogic instance = poolSystem != null
                ? poolSystem.Spawn(gatePrefab, spawnPosition, Quaternion.identity)
                : Instantiate(gatePrefab, spawnPosition, Quaternion.identity);

            if (instance == null)
            {
                return null;
            }

            float safeWidth = Mathf.Max(0.1f, targetWorldWidth);
            float safeHeight = Mathf.Max(0.1f, targetWorldHeight);
            instance.Init(
                config,
                this,
                mainPlayerUnit,
                playerController,
                gameplayCamera,
                poolSystem,
                laneWorldX,
                safeWidth,
                safeHeight);
            instance.Spawn();
            activeGates.Add(instance);
            GateShown?.Invoke(_gateSetCount, gateIndex, config);
            return instance;
        }

        private bool TryFindBalanceGateEntry(string gateId, out BalanceGateEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(gateId))
            {
                return false;
            }

            if (TryFindBalanceGateEntryInSource(gatePoolConfig != null ? gatePoolConfig.Entries : null, gateId, out entry))
            {
                return true;
            }

            return TryFindBalanceGateEntryInSource(GatePoolConfig.CreateDefaultEntries(), gateId, out entry);
        }

        private static bool TryFindBalanceGateEntryInSource(
            IReadOnlyList<BalanceGateEntry> source,
            string gateId,
            out BalanceGateEntry entry)
        {
            entry = null;
            if (source == null)
            {
                return false;
            }

            for (int index = 0; index < source.Count; index++)
            {
                BalanceGateEntry candidate = source[index];
                if (candidate != null && string.Equals(candidate.GateId, gateId, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool IsMajorEligibilitySet(
            int gateSetNumber,
            float gateCadenceSeconds,
            float majorCadenceSeconds)
        {
            int setsPerMajor = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    Mathf.Max(gateCadenceSeconds, majorCadenceSeconds)
                    / Mathf.Max(0.01f, gateCadenceSeconds)));
            return gateSetNumber > 0 && gateSetNumber % setsPerMajor == 0;
        }

        public static float GetMajorChance(float elapsedSeconds)
        {
            if (elapsedSeconds < 60f)
            {
                return 0f;
            }

            if (elapsedSeconds < 180f)
            {
                return 0.25f;
            }

            if (elapsedSeconds < 300f)
            {
                return 0.4f;
            }

            return 0.6f;
        }

        public static bool ShouldSpawnMajor(
            int gateSetNumber,
            float elapsedSeconds,
            float gateCadenceSeconds,
            float majorCadenceSeconds,
            float randomValue)
        {
            return MajorRollResult.Evaluate(
                    IsMajorEligibilitySet(
                        gateSetNumber,
                        gateCadenceSeconds,
                        majorCadenceSeconds),
                    GetMajorChance(elapsedSeconds),
                    randomValue,
                    0,
                    0,
                    true)
                .MajorSpawned;
        }

        private void BuildCandidateBuffers()
        {
            _candidateBuffer.Clear();
            _buffCandidateBuffer.Clear();
            _neutralCandidateBuffer.Clear();

            for (int ruleIndex = 0; ruleIndex < offerRules.Count; ruleIndex++)
            {
                GateOfferRule rule = offerRules[ruleIndex];
                if (rule == null || !rule.Enabled)
                {
                    continue;
                }

                AddCandidateIfAllowed(rule, GateOperationType.Add);
                AddCandidateIfAllowed(rule, GateOperationType.Subtract);
                AddCandidateIfAllowed(rule, GateOperationType.Multiply);
                AddCandidateIfAllowed(rule, GateOperationType.Divide);
            }
        }

        private void EnsureDefaultOfferRules()
        {
            maxProjectileCount = Mathf.Max(1, maxProjectileCount);
            maxPlayerCount = Mathf.Max(1, maxPlayerCount);

            if (offerRules == null)
            {
                offerRules = new List<GateOfferRule>();
            }

            AddDefaultOfferRuleIfMissing(GateStatTarget.Damage, 1f, 1f, 0f, 2f, 0f, 999f, false);
            AddDefaultOfferRuleIfMissing(GateStatTarget.FireRate, 1f, 1f, 1.25f, 2f, 0.25f, 20f, false);
            AddDefaultOfferRuleIfMissing(GateStatTarget.MaxHp, 5f, 5f, 1.25f, 2f, 1f, 999f, false);
            AddDefaultOfferRuleIfMissing(GateStatTarget.ProjectileCount, 1f, 1f, 0f, 2f, 1f, maxProjectileCount, true);
            AddDefaultOfferRuleIfMissing(GateStatTarget.PlayerCount, 1f, 1f, 0f, 2f, 1f, maxPlayerCount, true);
        }

        private void AddDefaultOfferRuleIfMissing(
            GateStatTarget statTarget,
            float addAmount,
            float subtractAmount,
            float multiplyAmount,
            float divideAmount,
            float minValue,
            float maxValue,
            bool wholeNumber)
        {
            for (int index = 0; index < offerRules.Count; index++)
            {
                GateOfferRule rule = offerRules[index];
                if (rule != null && rule.StatTarget == statTarget)
                {
                    return;
                }
            }

            offerRules.Add(new GateOfferRule(
                statTarget,
                addAmount,
                subtractAmount,
                multiplyAmount,
                divideAmount,
                minValue,
                maxValue,
                wholeNumber));
        }

        private void AddCandidateIfAllowed(GateOfferRule rule, GateOperationType operationType)
        {
            if (!ShouldOfferOperation(rule.StatTarget, operationType))
            {
                return;
            }

            float currentValue = GetCurrentStatValue(rule.StatTarget);
            float amount = rule.GetAmount(operationType);
            float minValue = rule.GetMinValue(maxProjectileCount, maxPlayerCount);
            float maxValue = rule.GetMaxValue(maxProjectileCount, maxPlayerCount);

            if (amount <= 0f || !IsCandidateAllowed(currentValue, operationType, amount, minValue, maxValue, rule.WholeNumber))
            {
                return;
            }

            GateOfferCandidate candidate = new GateOfferCandidate(rule.StatTarget, operationType, amount);
            _candidateBuffer.Add(candidate);

            if (candidate.IsBuff)
            {
                _buffCandidateBuffer.Add(candidate);
            }

            _neutralCandidateBuffer.Add(candidate);
        }

        private static bool ShouldOfferOperation(GateStatTarget statTarget, GateOperationType operationType)
        {
            if (operationType != GateOperationType.Multiply)
            {
                return true;
            }

            return statTarget switch
            {
                GateStatTarget.Damage => false,
                GateStatTarget.ProjectileCount => false,
                GateStatTarget.PlayerCount => false,
                _ => true
            };
        }

        private bool IsCandidateAllowed(
            float currentValue,
            GateOperationType operationType,
            float amount,
            float minValue,
            float maxValue,
            bool wholeNumber)
        {
            float safeCurrent = wholeNumber ? Mathf.Round(currentValue) : currentValue;

            if ((operationType == GateOperationType.Subtract || operationType == GateOperationType.Divide)
                && safeCurrent <= minValue + Mathf.Epsilon)
            {
                return false;
            }

            if ((operationType == GateOperationType.Add || operationType == GateOperationType.Multiply)
                && safeCurrent >= maxValue - Mathf.Epsilon)
            {
                return false;
            }

            float result = ApplyOperationPreview(safeCurrent, operationType, amount);
            if (wholeNumber)
            {
                result = Mathf.Round(result);
            }

            if (result < minValue || result > maxValue)
            {
                return false;
            }

            return !Mathf.Approximately(result, safeCurrent);
        }

        private float GetCurrentStatValue(GateStatTarget statTarget)
        {
            if (mainPlayerUnit == null)
            {
                return 0f;
            }

            return statTarget switch
            {
                GateStatTarget.Damage => mainPlayerUnit.Damage,
                GateStatTarget.FireRate => mainPlayerUnit.FireRate,
                GateStatTarget.MaxHp => mainPlayerUnit.MaxHp,
                GateStatTarget.ProjectileCount => mainPlayerUnit.BulletSpawner != null
                    ? mainPlayerUnit.BulletSpawner.ProjectileCount
                    : 1f,
                GateStatTarget.PlayerCount => playerController != null
                    ? playerController.CurrentSquadCount
                    : 1f,
                _ => 0f
            };
        }

        private static float ApplyOperationPreview(float baseValue, GateOperationType operationType, float amount)
        {
            float safeAmount = Mathf.Abs(amount);

            return operationType switch
            {
                GateOperationType.Add => baseValue + safeAmount,
                GateOperationType.Subtract => baseValue - safeAmount,
                GateOperationType.Multiply => baseValue * Mathf.Max(0f, safeAmount),
                GateOperationType.Divide => safeAmount <= 0f ? baseValue : baseValue / safeAmount,
                _ => baseValue
            };
        }

        private static bool TryTakeRandomCandidate(List<GateOfferCandidate> source, out GateOfferCandidate candidate)
        {
            candidate = default;

            if (source == null || source.Count <= 0)
            {
                return false;
            }

            int index = Random.Range(0, source.Count);
            candidate = source[index];
            source.RemoveAt(index);
            return true;
        }

        private static void RemoveMatchingCandidate(List<GateOfferCandidate> source, GateOfferCandidate candidate)
        {
            for (int index = source.Count - 1; index >= 0; index--)
            {
                if (source[index].Matches(candidate))
                {
                    source.RemoveAt(index);
                }
            }
        }

        private static GateConfig CreateRuntimeConfig(GateOfferCandidate candidate)
        {
            GateConfig config = ScriptableObject.CreateInstance<GateConfig>();
            config.ConfigureRuntime(candidate.StatTarget, candidate.OperationType, candidate.Amount);
            return config;
        }

        private void ResetMajorRuntimeState()
        {
            _consecutiveMajorEligibleMisses = 0;
            _majorEligibleRolls = 0;
            _majorOffers = 0;
            _majorPityForcedOffers = 0;
            _maxConsecutiveMajorMisses = 0;
        }

        private void ClearRuntimeConfigCache()
        {
            foreach (GateConfig config in _runtimeConfigCache.Values)
            {
                if (config != null)
                {
                    Destroy(config);
                }
            }

            _runtimeConfigCache.Clear();
        }

        private void OnDestroy()
        {
            ClearRuntimeConfigCache();
        }

        private void ResolveGameplayCamera()
        {
            if (gameplayCamera != null)
            {
                return;
            }

            gameplayCamera = Camera.main;

            if (gameplayCamera == null)
            {
                gameplayCamera = FindAnyObjectByType<Camera>();
            }
        }

        private float GetSpawnWorldY(float gateHeight)
        {
            if (gameplayCamera != null && gameplayCamera.orthographic)
            {
                float zDistance = GetViewportZDistance();
                float topWorldY = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, zDistance)).y;
                float marginWorld = GetViewportWorldHeight() * Mathf.Max(0f, topSpawnMarginPixels) / Mathf.Max(1f, Screen.height);
                return topWorldY + Mathf.Max(0f, gateHeight) * 0.5f + Mathf.Max(0f, spawnAboveCameraOffset) + marginWorld;
            }

            float fallbackOffset = Mathf.Max(0.01f, spawnAboveCameraOffset);
            return mainPlayerUnit != null ? mainPlayerUnit.transform.position.y + fallbackOffset : transform.position.y + fallbackOffset;
        }

        private GateLaneLayout GetGateLaneLayout(int laneIndex, int totalLanes)
        {
            if (useViewportLanes && TryGetViewportLaneLayout(laneIndex, totalLanes, out GateLaneLayout viewportLayout))
            {
                return viewportLayout;
            }

            float centerX = gameplayCamera != null
                ? gameplayCamera.transform.position.x
                : 0f;

            float sizeMultiplier = Mathf.Max(0.25f, gateSizeMultiplier);
            float width = Mathf.Max(minGateWorldWidth, gateHalfWidth * 2f) * sizeMultiplier;
            if (totalLanes > 1)
            {
                width = Mathf.Min(width, Mathf.Max(0.1f, laneSpacing - Mathf.Max(0f, laneGapWorld)));
            }

            return new GateLaneLayout(
                centerX + GetLaneOffsetX(laneIndex, totalLanes),
                width,
                width * Mathf.Max(0.5f, gateHeightToWidth));
        }

        private bool TryGetViewportLaneLayout(int laneIndex, int totalLanes, out GateLaneLayout layout)
        {
            layout = default;

            if (gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return false;
            }

            totalLanes = Mathf.Max(1, totalLanes);
            float zDistance = GetViewportZDistance();

            float laneMinViewport = GetSafeLaneMinViewport();
            float laneMaxViewport = GetSafeLaneMaxViewport(laneMinViewport);
            float playfieldCenterViewportY = GetPlayfieldCenterViewportY();

            Vector3 worldMin = gameplayCamera.ViewportToWorldPoint(
                new Vector3(laneMinViewport, playfieldCenterViewportY, zDistance));
            Vector3 worldMax = gameplayCamera.ViewportToWorldPoint(
                new Vector3(laneMaxViewport, playfieldCenterViewportY, zDistance));

            float left = Mathf.Min(worldMin.x, worldMax.x);
            float right = Mathf.Max(worldMin.x, worldMax.x);
            float availableWidth = Mathf.Max(0.1f, right - left);
            float gap = totalLanes <= 1
                ? 0f
                : Mathf.Clamp(laneGapWorld, 0f, availableWidth * 0.12f);
            float laneWidth = Mathf.Max(0.1f, (availableWidth - gap * (totalLanes - 1)) / totalLanes);
            float sizeMultiplier = Mathf.Max(0.25f, gateSizeMultiplier);
            float desiredGateWidth = Mathf.Max(0.1f, laneWidth * sizeMultiplier);
            float maxGateWidthWithoutOverlap = totalLanes <= 1
                ? desiredGateWidth
                : laneWidth;
            float gateWidth = Mathf.Min(desiredGateWidth, maxGateWidthWithoutOverlap);
            float maxHeight = GetPlayfieldWorldHeight() * Mathf.Clamp(maxGateHeightViewport, 0.05f, 0.6f);
            float gateHeight = Mathf.Min(gateWidth * Mathf.Max(0.5f, gateHeightToWidth), maxHeight);
            float centerX = left + laneWidth * 0.5f + laneIndex * (laneWidth + gap);

            layout = new GateLaneLayout(centerX, gateWidth, gateHeight);
            return true;
        }

        private float GetSafeLaneMinViewport()
        {
            return Mathf.Clamp01(Mathf.Min(Mathf.Clamp01(viewportLaneMin), horizontalViewportPadding));
        }

        private float GetSafeLaneMaxViewport(float laneMinViewport)
        {
            float laneMaxViewport = Mathf.Clamp01(Mathf.Max(Mathf.Clamp01(viewportLaneMax), 1f - horizontalViewportPadding));
            return Mathf.Max(laneMinViewport + 0.01f, laneMaxViewport);
        }

        private float GetPlayfieldCenterViewportY()
        {
            float bottom = Mathf.Clamp01(bottomReservedViewport);
            float top = Mathf.Clamp01(1f - topReservedViewport);
            return (bottom + Mathf.Max(bottom + 0.01f, top)) * 0.5f;
        }

        private float GetPlayfieldWorldHeight()
        {
            if (gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return 1f;
            }

            float zDistance = GetViewportZDistance();
            float bottom = Mathf.Clamp01(bottomReservedViewport);
            float top = Mathf.Clamp01(1f - topReservedViewport);
            top = Mathf.Max(bottom + 0.01f, top);
            float bottomY = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, bottom, zDistance)).y;
            float topY = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, top, zDistance)).y;
            return Mathf.Max(0.1f, Mathf.Abs(topY - bottomY));
        }

        private float GetViewportWorldHeight()
        {
            if (gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return 1f;
            }

            return Mathf.Max(0.1f, gameplayCamera.orthographicSize * 2f);
        }

        private float GetViewportZDistance()
        {
            if (gameplayCamera == null)
            {
                return 10f;
            }

            return Mathf.Abs(gameplayCamera.transform.position.z);
        }

        private float GetLaneOffsetX(int laneIndex, int totalLanes)
        {
            if (totalLanes <= 1)
            {
                return 0f;
            }

            float centerIndex = (totalLanes - 1) * 0.5f;
            return (laneIndex - centerIndex) * laneSpacing;
        }

        private void ClearActiveGates()
        {
            for (int index = activeGates.Count - 1; index >= 0; index--)
            {
                GateLogic gate = activeGates[index];
                if (gate == null)
                {
                    continue;
                }

                gate.Despawn();
            }

            activeGates.Clear();
            _isGateSetActive = false;
        }
    }

    [Serializable]
    public sealed class GateOfferRule
    {
        [SerializeField] private GateStatTarget statTarget;
        [SerializeField] private bool enabled = true;
        [SerializeField] private float addAmount = 1f;
        [SerializeField] private float subtractAmount = 1f;
        [SerializeField] private float multiplyAmount = 2f;
        [SerializeField] private float divideAmount = 2f;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = 999f;
        [SerializeField] private bool wholeNumber;

        public GateOfferRule(
            GateStatTarget statTarget,
            float addAmount,
            float subtractAmount,
            float multiplyAmount,
            float divideAmount,
            float minValue,
            float maxValue,
            bool wholeNumber)
        {
            this.statTarget = statTarget;
            this.addAmount = addAmount;
            this.subtractAmount = subtractAmount;
            this.multiplyAmount = multiplyAmount;
            this.divideAmount = divideAmount;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.wholeNumber = wholeNumber;
        }

        public GateStatTarget StatTarget => statTarget;
        public bool Enabled => enabled;
        public bool WholeNumber => wholeNumber;

        public float GetAmount(GateOperationType operationType)
        {
            return operationType switch
            {
                GateOperationType.Add => addAmount,
                GateOperationType.Subtract => subtractAmount,
                GateOperationType.Multiply => multiplyAmount,
                GateOperationType.Divide => divideAmount,
                _ => 0f
            };
        }

        public float GetMinValue(int maxProjectileCount, int maxPlayerCount)
        {
            return statTarget switch
            {
                GateStatTarget.ProjectileCount => Mathf.Max(1f, minValue),
                GateStatTarget.PlayerCount => Mathf.Max(1f, minValue),
                _ => minValue
            };
        }

        public float GetMaxValue(int maxProjectileCount, int maxPlayerCount)
        {
            return statTarget switch
            {
                GateStatTarget.ProjectileCount => Mathf.Max(1f, maxProjectileCount),
                GateStatTarget.PlayerCount => Mathf.Max(1f, maxPlayerCount),
                _ => maxValue
            };
        }
    }

    internal readonly struct GateOfferCandidate
    {
        public readonly GateStatTarget StatTarget;
        public readonly GateOperationType OperationType;
        public readonly float Amount;

        public GateOfferCandidate(GateStatTarget statTarget, GateOperationType operationType, float amount)
        {
            StatTarget = statTarget;
            OperationType = operationType;
            Amount = amount;
        }

        public bool IsBuff => OperationType == GateOperationType.Add || OperationType == GateOperationType.Multiply;

        public bool Matches(GateOfferCandidate other)
        {
            return StatTarget == other.StatTarget
                && OperationType == other.OperationType
                && Mathf.Approximately(Amount, other.Amount);
        }
    }

    internal readonly struct RuntimeGateConfigKey : IEquatable<RuntimeGateConfigKey>
    {
        private readonly string _gateId;
        private readonly string _phaseId;
        private readonly int _elapsedBucket;
        private readonly BalanceGateCategory _category;
        private readonly BalanceEffectType _effectType;
        private readonly int _magnitude;
        private readonly int _duration;
        private readonly BalanceEffectType _secondaryEffectType;
        private readonly int _secondaryMagnitude;
        private readonly int _secondaryDuration;
        private readonly BalanceEffectType _drawbackType;
        private readonly int _drawbackMagnitude;
        private readonly int _drawbackDuration;

        public RuntimeGateConfigKey(ResolvedGateEntry entry, float elapsedSeconds)
        {
            _gateId = entry.GateId ?? string.Empty;
            _phaseId = entry.PhaseId ?? string.Empty;
            _elapsedBucket = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds));
            _category = entry.Category;
            _effectType = entry.EffectType;
            _magnitude = Quantize(entry.Magnitude);
            _duration = Quantize(entry.DurationSeconds);
            _secondaryEffectType = entry.SecondaryEffectType;
            _secondaryMagnitude = Quantize(entry.SecondaryMagnitude);
            _secondaryDuration = Quantize(entry.SecondaryDurationSeconds);
            _drawbackType = entry.DrawbackType;
            _drawbackMagnitude = Quantize(entry.DrawbackMagnitude);
            _drawbackDuration = Quantize(entry.DrawbackDurationSeconds);
        }

        public bool Equals(RuntimeGateConfigKey other)
        {
            return string.Equals(_gateId, other._gateId, StringComparison.Ordinal)
                && string.Equals(_phaseId, other._phaseId, StringComparison.Ordinal)
                && _elapsedBucket == other._elapsedBucket
                && _category == other._category
                && _effectType == other._effectType
                && _magnitude == other._magnitude
                && _duration == other._duration
                && _secondaryEffectType == other._secondaryEffectType
                && _secondaryMagnitude == other._secondaryMagnitude
                && _secondaryDuration == other._secondaryDuration
                && _drawbackType == other._drawbackType
                && _drawbackMagnitude == other._drawbackMagnitude
                && _drawbackDuration == other._drawbackDuration;
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeGateConfigKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(_gateId);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(_phaseId);
                hash = hash * 31 + _elapsedBucket;
                hash = hash * 31 + (int)_category;
                hash = hash * 31 + (int)_effectType;
                hash = hash * 31 + _magnitude;
                hash = hash * 31 + _duration;
                hash = hash * 31 + (int)_secondaryEffectType;
                hash = hash * 31 + _secondaryMagnitude;
                hash = hash * 31 + _secondaryDuration;
                hash = hash * 31 + (int)_drawbackType;
                hash = hash * 31 + _drawbackMagnitude;
                hash = hash * 31 + _drawbackDuration;
                return hash;
            }
        }

        private static int Quantize(float value)
        {
            return Mathf.RoundToInt(value * 1000f);
        }
    }

    public readonly struct MajorGateRollTelemetry
    {
        public readonly int GateSet;
        public readonly float ElapsedSeconds;
        public readonly bool IsEligible;
        public readonly float Chance;
        public readonly float RandomValue;
        public readonly bool Spawned;
        public readonly bool WasForced;
        public readonly int ConsecutiveMisses;
        public readonly string FailureReason;

        public MajorGateRollTelemetry(
            int gateSet,
            float elapsedSeconds,
            bool isEligible,
            float chance,
            float randomValue,
            bool spawned,
            bool wasForced,
            int consecutiveMisses,
            string failureReason)
        {
            GateSet = gateSet;
            ElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            IsEligible = isEligible;
            Chance = Mathf.Clamp01(chance);
            RandomValue = Mathf.Clamp01(randomValue);
            Spawned = spawned;
            WasForced = wasForced;
            ConsecutiveMisses = Mathf.Max(0, consecutiveMisses);
            FailureReason = failureReason ?? string.Empty;
        }
    }

    internal readonly struct GateLaneLayout
    {
        public readonly float CenterX;
        public readonly float GateWidth;
        public readonly float GateHeight;

        public GateLaneLayout(float centerX, float gateWidth, float gateHeight)
        {
            CenterX = centerX;
            GateWidth = gateWidth;
            GateHeight = gateHeight;
        }
    }
}
