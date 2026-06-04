using System;
using System.Collections.Generic;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Gates;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;
using Random = UnityEngine.Random;
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
        [SerializeField] private float spawnAboveCameraOffset = 1.25f;
        [SerializeField] private bool useViewportLanes = true;
        [SerializeField] private float viewportLaneMin = 0.12f;
        [SerializeField] private float viewportLaneMax = 0.88f;
        [SerializeField] private float gateHalfWidth = 0.75f;
        [SerializeField] private float laneSpacing = 2.2f;
        [SerializeField] private int gateCount = 3;
        [SerializeField] private float laneGapWorld = 0.08f;
        [SerializeField] private float gateHeightToWidth = 1.35f;
        [SerializeField, Range(0.05f, 0.3f)] private float maxGateHeightViewport = 0.18f;
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

        [SerializeField] private List<GateLogic> activeGates = new List<GateLogic>();

        private float _nextSpawnTime;
        private bool _isGateSetActive;
        private bool _choiceLocked;
        private readonly List<GateConfig> _spawnConfigBuffer = new List<GateConfig>();
        private readonly List<GateOfferCandidate> _candidateBuffer = new List<GateOfferCandidate>();
        private readonly List<GateOfferCandidate> _buffCandidateBuffer = new List<GateOfferCandidate>();
        private readonly List<GateOfferCandidate> _neutralCandidateBuffer = new List<GateOfferCandidate>();

        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            ResolveGameplayCamera();
            poolSystem ??= FindAnyObjectByType<RuntimePoolSystem>();
            EnsureDefaultOfferRules();

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (mainPlayerUnit == null)
            {
                mainPlayerUnit = FindAnyObjectByType<MainPlayerUnit>();
            }

            _nextSpawnTime = Time.time + Mathf.Max(0.01f, spawnIntervalSeconds);
            _isGateSetActive = false;
            _choiceLocked = false;
        }

        private void Update()
        {
            if (mainPlayerUnit == null || mainPlayerUnit.IsDead)
            {
                return;
            }

            if (_isGateSetActive)
            {
                return;
            }

            if (Time.time >= _nextSpawnTime)
            {
                Spawn();
                _nextSpawnTime = Time.time + Mathf.Max(0.01f, spawnIntervalSeconds);
            }
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

            int count = Mathf.Max(1, gateCount);
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
            }
        }

        public void ApplyEffect()
        {
        }

        public void HandleGateChosen(GateLogic chosen)
        {
            if (!_isGateSetActive || _choiceLocked || chosen == null)
            {
                return;
            }

            _choiceLocked = true;
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

            float width = Mathf.Max(minGateWorldWidth, gateHalfWidth * 2f);
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
            float gateWidth = Mathf.Max(0.1f, laneWidth);
            float maxHeight = GetPlayfieldWorldHeight() * Mathf.Clamp(maxGateHeightViewport, 0.05f, 0.3f);
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
