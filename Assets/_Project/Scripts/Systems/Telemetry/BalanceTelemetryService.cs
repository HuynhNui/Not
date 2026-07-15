using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.Balance;
using _Project.Scripts.Systems.RunStatsSystem;
using UnityEngine;
using RuntimeEnemySpawnerSystem =
    _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeGateSystem =
    _Project.Scripts.Systems.GateSystem.GateSystem;

namespace _Project.Scripts.Systems.Telemetry
{
    public sealed class BalanceTelemetryService : MonoBehaviour
    {
        private const string TelemetryFolderName = "BalanceTelemetry";

        [SerializeField] private BalanceTelemetryConfig config;
        [SerializeField] private RunStatsTracker runStatsTracker;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MainPlayerUnit mainPlayerUnit;
        [SerializeField] private RuntimeEnemySpawnerSystem enemySpawnerSystem;
        [SerializeField] private RuntimeGateSystem gateSystem;

        private BalanceTelemetryWriter _writer;
        private bool _isRunActive;
        private bool _firstHitRecorded;
        private string _runId;
        private string _runStartedUtc;
        private float _nextSnapshotTime;
        private int _snapshotCount;
        private int _gateShownCount;
        private int _gateSelectedCount;
        private int _followerDeathCount;
        private int _promotionCount;
        private int _startingSquadCount;
        private int _previousSnapshotKills;
        private string _runMode = "standard";
        private string _benchmarkProfileId = string.Empty;
        private PlayerRunStartStats _configuredStartStats;
        private float _firstHitSeconds = -1f;
        private float _firstFollowerDeathSeconds = -1f;
        private float _peakEffectiveDpsEstimate;
        private float _peakEstimatedBaseProjectileEmissionsPerSecond;
        private float _peakDamage;
        private int _peakProjectileCount;
        private int _peakSquadCount;

        public string OutputDirectory => Path.Combine(
            Application.persistentDataPath,
            TelemetryFolderName,
            BalanceVersion);
        public bool IsRunActive => _isRunActive;
        public int SnapshotCount => _snapshotCount;

        public void Configure(
            BalanceTelemetryConfig telemetryConfig,
            RunStatsTracker statsTracker,
            PlayerController squad,
            MainPlayerUnit mainUnit,
            RuntimeEnemySpawnerSystem spawner,
            RuntimeGateSystem gates)
        {
            Unsubscribe();

            config = telemetryConfig;
            runStatsTracker = statsTracker;
            playerController = squad;
            mainPlayerUnit = mainUnit;
            enemySpawnerSystem = spawner;
            gateSystem = gates;

            Subscribe();
            EnsureWriter();
        }

        public void SetRunContext(
            string runMode,
            string benchmarkProfileId,
            PlayerRunStartStats startStats)
        {
            _runMode = string.IsNullOrWhiteSpace(runMode) ? "standard" : runMode.Trim();
            _benchmarkProfileId = benchmarkProfileId ?? string.Empty;
            _configuredStartStats = startStats;
        }

        public void BeginRun()
        {
            if (!ShouldCollectTelemetry())
            {
                return;
            }

            EnsureWriter();
            _writer.ClearBuffers();
            _runId = Guid.NewGuid().ToString("N");
            _runStartedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            _nextSnapshotTime = SnapshotIntervalSeconds;
            _snapshotCount = 0;
            _gateShownCount = 0;
            _gateSelectedCount = 0;
            _followerDeathCount = 0;
            _promotionCount = 0;
            _previousSnapshotKills = 0;
            _firstHitRecorded = false;
            _firstHitSeconds = -1f;
            _firstFollowerDeathSeconds = -1f;
            _peakEffectiveDpsEstimate = 0f;
            _peakEstimatedBaseProjectileEmissionsPerSecond = 0f;
            _peakDamage = 0f;
            _peakProjectileCount = 0;
            _peakSquadCount = 0;
            _startingSquadCount = playerController != null
                ? playerController.CurrentSquadCount
                : 0;
            _isRunActive = true;

            RecordEvent("run_start");
        }

        public void EndRun(RunStatsSnapshot snapshot)
        {
            if (!_isRunActive)
            {
                return;
            }

            CaptureSnapshot(force: true);
            RecordEvent("run_end");

            _writer.BufferSummary(new BalanceRunSummaryRow
            {
                runId = _runId,
                runStartedUtc = _runStartedUtc,
                runEndedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                buildVersion = Application.version,
                balanceVersion = BalanceVersion,
                runMode = _runMode,
                benchmarkProfileId = _benchmarkProfileId,
                survivalSeconds = snapshot.SurvivalTime,
                enemyKills = snapshot.EnemyKills,
                coinRewardPoints = runStatsTracker != null
                    ? runStatsTracker.CoinRewardPoints
                    : snapshot.CoinsEarned,
                coinsEarned = snapshot.CoinsEarned,
                score = snapshot.Score,
                walletCoins = snapshot.WalletCoins,
                startingDamage = _configuredStartStats.Damage,
                startingFireRate = _configuredStartStats.FireRate,
                startingMaxHp = _configuredStartStats.MaxHp,
                startingProjectileCount = _configuredStartStats.ProjectileCount,
                startingSquadCount = _startingSquadCount,
                endingSquadCount = playerController != null
                    ? playerController.CurrentSquadCount
                    : 0,
                gateShownCount = _gateShownCount,
                gateSelectedCount = _gateSelectedCount,
                firstHitSeconds = _firstHitSeconds,
                followerDeaths = _followerDeathCount,
                promotions = _promotionCount,
                snapshotCount = _snapshotCount,
                peakEffectiveDpsEstimate = _peakEffectiveDpsEstimate,
                peakEstimatedBaseProjectileEmissionsPerSecond =
                    _peakEstimatedBaseProjectileEmissionsPerSecond,
                peakDamage = _peakDamage,
                peakProjectileCount = _peakProjectileCount,
                peakSquadCount = _peakSquadCount,
                firstFollowerDeathSeconds = _firstFollowerDeathSeconds,
                endingDamage = mainPlayerUnit != null ? mainPlayerUnit.Damage : 0f,
                endingFireRate = mainPlayerUnit != null ? mainPlayerUnit.FireRate : 0f,
                endingMaxHp = mainPlayerUnit != null ? mainPlayerUnit.MaxHp : 0f,
                endingProjectileCount = mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null
                    ? mainPlayerUnit.BulletSpawner.ProjectileCount
                    : 0,
                endingEffectiveDpsEstimate = EstimateEffectiveDps(),
                endingTotalSquadCurrentHp = GetTotalSquadCurrentHp(),
                endingTotalSquadMaxHp = GetTotalSquadMaxHp(),
                endingIncomingDamageMultiplier = playerController != null
                    ? playerController.GateIncomingDamageMultiplier
                    : 1f,
                endingEnemyPressureMultiplier = enemySpawnerSystem != null
                    ? enemySpawnerSystem.GatePressureMultiplier
                    : 1f,
                endingEnemySpeedMultiplier = enemySpawnerSystem != null
                    ? enemySpawnerSystem.GateSpeedMultiplier
                    : 1f
            });

            _writer.Flush();
            _isRunActive = false;
        }

        public void Flush()
        {
            _writer?.Flush();
        }

        private void Update()
        {
            if (!_isRunActive || runStatsTracker == null)
            {
                return;
            }

            if (_snapshotCount >= MaxSnapshotsPerRun
                || runStatsTracker.SurvivalTime < _nextSnapshotTime)
            {
                return;
            }

            CaptureSnapshot(force: false);
            _nextSnapshotTime += SnapshotIntervalSeconds;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            Flush();
        }

        private void Subscribe()
        {
            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Damaged -= HandlePlayerDamaged;
                mainPlayerUnit.Damaged += HandlePlayerDamaged;
            }

            if (playerController != null)
            {
                playerController.FollowerDied -= HandleFollowerDied;
                playerController.FollowerDied += HandleFollowerDied;
                playerController.FollowerPromoted -= HandleFollowerPromoted;
                playerController.FollowerPromoted += HandleFollowerPromoted;
            }

            if (gateSystem != null)
            {
                gateSystem.GateShown -= HandleGateShown;
                gateSystem.GateShown += HandleGateShown;
                gateSystem.GateSelected -= HandleGateSelected;
                gateSystem.GateSelected += HandleGateSelected;
                gateSystem.MajorRollEvaluated -= HandleMajorRollEvaluated;
                gateSystem.MajorRollEvaluated += HandleMajorRollEvaluated;
            }
        }

        private void Unsubscribe()
        {
            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Damaged -= HandlePlayerDamaged;
            }

            if (playerController != null)
            {
                playerController.FollowerDied -= HandleFollowerDied;
                playerController.FollowerPromoted -= HandleFollowerPromoted;
            }

            if (gateSystem != null)
            {
                gateSystem.GateShown -= HandleGateShown;
                gateSystem.GateSelected -= HandleGateSelected;
                gateSystem.MajorRollEvaluated -= HandleMajorRollEvaluated;
            }
        }

        private void HandlePlayerDamaged(PlayerUnit unit, float damage)
        {
            if (!_isRunActive || _firstHitRecorded)
            {
                return;
            }

            _firstHitRecorded = true;
            _firstHitSeconds = ElapsedSeconds;
            RecordEvent("first_hit", value: damage);
        }

        private void HandleFollowerDied(FollowerUnit follower)
        {
            if (!_isRunActive)
            {
                return;
            }

            _followerDeathCount++;
            if (_firstFollowerDeathSeconds < 0f)
            {
                _firstFollowerDeathSeconds = ElapsedSeconds;
            }

            RecordEvent(
                "follower_death",
                value: follower != null ? follower.CurrentHp : 0f);
        }

        private void HandleFollowerPromoted(FollowerUnit follower)
        {
            if (!_isRunActive)
            {
                return;
            }

            _promotionCount++;
            RecordEvent(
                "promotion",
                value: follower != null ? follower.CurrentHp : 0f);
        }

        private void HandleGateShown(int gateSet, int laneIndex, GateConfig gate)
        {
            if (!_isRunActive)
            {
                return;
            }

            _gateShownCount++;
            RecordGateEvent("gate_shown", gateSet, laneIndex, gate);
        }

        private void HandleGateSelected(int gateSet, GateConfig gate)
        {
            if (!_isRunActive)
            {
                return;
            }

            _gateSelectedCount++;
            GateEffectPreviewResult preview = PreviewGate(gate);
            RecordGateEvent("gate_selected", gateSet, -1, gate, preview);
        }

        private void HandleMajorRollEvaluated(_Project.Scripts.Systems.GateSystem.MajorGateRollTelemetry telemetry)
        {
            if (!_isRunActive)
            {
                return;
            }

            RecordEvent(
                "major_roll",
                gateSet: telemetry.GateSet,
                value: telemetry.Chance,
                majorRollEligible: telemetry.IsEligible,
                majorRollSpawned: telemetry.Spawned,
                majorRollForced: telemetry.WasForced,
                majorConsecutiveMisses: telemetry.ConsecutiveMisses,
                majorFailureReason: telemetry.FailureReason);
        }

        private void CaptureSnapshot(bool force)
        {
            if (!_isRunActive
                || runStatsTracker == null
                || _snapshotCount >= MaxSnapshotsPerRun)
            {
                return;
            }

            float elapsed = ElapsedSeconds;
            if (force
                && _snapshotCount > 0
                && Mathf.Abs(elapsed - (_nextSnapshotTime - SnapshotIntervalSeconds)) < 0.01f)
            {
                return;
            }

            int enemyKills = runStatsTracker.EnemyKills;
            int projectileCount = mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null
                ? mainPlayerUnit.BulletSpawner.ProjectileCount
                : 0;
            int squadCount = playerController != null ? playerController.CurrentSquadCount : 0;
            float damage = mainPlayerUnit != null ? mainPlayerUnit.Damage : 0f;
            float fireRate = mainPlayerUnit != null ? mainPlayerUnit.FireRate : 0f;
            CombatScalingConfig combatConfig = mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null
                ? mainPlayerUnit.BulletSpawner.CurrentCombatScalingConfig
                : null;
            float projectileFactor = BalanceV1Math.ProjectileFactor(projectileCount, combatConfig);
            float squadFactor = BalanceV1Math.SquadFactor(squadCount, combatConfig);
            float followerDamageScale = BalanceV1Math.FollowerDamageScale(squadCount, combatConfig);
            float mainDamagePerProjectile = BalanceV1Math.DamagePerMainBullet(
                damage,
                projectileCount,
                combatConfig);
            float effectiveDpsEstimate = BalanceV1Math.EffectiveDps(
                damage,
                fireRate,
                projectileCount,
                squadCount,
                combatConfig);
            float estimatedBaseProjectileEmissionsPerSecond =
                BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                    fireRate,
                    projectileCount,
                    squadCount,
                    combatConfig);
            int killsSincePreviousSnapshot = Mathf.Max(0, enemyKills - _previousSnapshotKills);
            _previousSnapshotKills = enemyKills;

            _peakEffectiveDpsEstimate = Mathf.Max(_peakEffectiveDpsEstimate, effectiveDpsEstimate);
            _peakEstimatedBaseProjectileEmissionsPerSecond = Mathf.Max(
                _peakEstimatedBaseProjectileEmissionsPerSecond,
                estimatedBaseProjectileEmissionsPerSecond);
            _peakDamage = Mathf.Max(_peakDamage, damage);
            _peakProjectileCount = Mathf.Max(_peakProjectileCount, projectileCount);
            _peakSquadCount = Mathf.Max(_peakSquadCount, squadCount);

            _writer.BufferSnapshot(new BalanceRunSnapshotRow
            {
                runId = _runId,
                elapsedSeconds = elapsed,
                enemyKills = enemyKills,
                coinRewardPoints = runStatsTracker.CoinRewardPoints,
                roundedRunCoins = runStatsTracker.CoinsEarned,
                score = runStatsTracker.Score,
                squadCount = squadCount,
                currentHp = mainPlayerUnit != null ? mainPlayerUnit.CurrentHp : 0f,
                maxHp = mainPlayerUnit != null ? mainPlayerUnit.MaxHp : 0f,
                totalSquadCurrentHp = GetTotalSquadCurrentHp(),
                totalSquadMaxHp = GetTotalSquadMaxHp(),
                damage = damage,
                fireRate = fireRate,
                projectileCount = projectileCount,
                effectiveDpsEstimate = effectiveDpsEstimate,
                estimatedBaseProjectileEmissionsPerSecond =
                    estimatedBaseProjectileEmissionsPerSecond,
                projectileFactor = projectileFactor,
                squadFactor = squadFactor,
                followerDamageScale = followerDamageScale,
                mainDamagePerProjectile = mainDamagePerProjectile,
                killsSincePreviousSnapshot = killsSincePreviousSnapshot,
                activeEnemies = enemySpawnerSystem != null
                    ? enemySpawnerSystem.ActiveEnemyCount
                    : 0,
                visibleEnemies = enemySpawnerSystem != null
                    ? enemySpawnerSystem.VisibleEnemyCount
                    : 0,
                activeThreat = enemySpawnerSystem != null
                    ? enemySpawnerSystem.CurrentActiveThreat
                    : 0f,
                activeEnemyCap = enemySpawnerSystem != null
                    ? enemySpawnerSystem.CurrentMaxActiveEnemies
                    : 0,
                minimumVisibleEnemies = enemySpawnerSystem != null
                    ? enemySpawnerSystem.CurrentMinimumVisibleEnemies
                    : 0,
                rawSpawnPerSecond = enemySpawnerSystem != null
                    ? enemySpawnerSystem.CurrentRawSpawnPerSecond
                    : 0f,
                threatBudget = enemySpawnerSystem != null
                    ? enemySpawnerSystem.CurrentThreatBudget
                    : 0f,
                activeEnemyRatio = enemySpawnerSystem != null && enemySpawnerSystem.CurrentMaxActiveEnemies > 0
                    ? enemySpawnerSystem.ActiveEnemyCount / (float)enemySpawnerSystem.CurrentMaxActiveEnemies
                    : 0f,
                visibleEnemyRatio = enemySpawnerSystem != null && enemySpawnerSystem.CurrentMinimumVisibleEnemies > 0
                    ? enemySpawnerSystem.VisibleEnemyCount / (float)enemySpawnerSystem.CurrentMinimumVisibleEnemies
                    : 0f,
                incomingDamageMultiplier = playerController != null
                    ? playerController.GateIncomingDamageMultiplier
                    : 1f,
                enemyPressureMultiplier = enemySpawnerSystem != null
                    ? enemySpawnerSystem.GatePressureMultiplier
                    : 1f,
                enemySpeedMultiplier = enemySpawnerSystem != null
                    ? enemySpawnerSystem.GateSpeedMultiplier
                    : 1f,
                gateSetCount = gateSystem != null ? gateSystem.GateSetCount : 0,
                gatePhase = gateSystem != null ? gateSystem.CurrentPhaseId : string.Empty,
                majorEligibleRolls = gateSystem != null ? gateSystem.MajorEligibleRolls : 0,
                majorOffers = gateSystem != null ? gateSystem.MajorOffers : 0,
                majorPityForcedOffers = gateSystem != null ? gateSystem.MajorPityForcedOffers : 0,
                maxConsecutiveMajorMisses = gateSystem != null ? gateSystem.MaxConsecutiveMajorMisses : 0
            });

            _snapshotCount++;
            RecordEvent("snapshot_15s");
        }

        private void RecordGateEvent(
            string eventName,
            int gateSet,
            int laneIndex,
            GateConfig gate,
            GateEffectPreviewResult? preview = null)
        {
            GateRuntimeEffect primaryEffect = null;
            GateRuntimeEffect secondaryEffect = null;
            GateRuntimeEffect drawbackEffect = null;
            if (gate != null && gate.RuntimeEffects != null)
            {
                for (int index = 0; index < gate.RuntimeEffects.Count; index++)
                {
                    GateRuntimeEffect effect = gate.RuntimeEffects[index];
                    if (effect == null)
                    {
                        continue;
                    }

                    if (effect.IsDrawback)
                    {
                        drawbackEffect ??= effect;
                    }
                    else if (primaryEffect == null)
                    {
                        primaryEffect = effect;
                    }
                    else
                    {
                        secondaryEffect ??= effect;
                    }
                }
            }

            RecordEvent(
                eventName,
                gate != null ? gate.GateId : string.Empty,
                gate != null ? gate.Category.ToString() : string.Empty,
                gate != null ? gate.GetDisplayText() : string.Empty,
                gateSet,
                laneIndex,
                gate != null ? gate.Amount : 0f,
                gatePhase: gate != null ? gate.ResolvedPhaseId : string.Empty,
                primaryEffectType: primaryEffect != null ? primaryEffect.EffectType.ToString() : string.Empty,
                primaryMagnitude: primaryEffect != null ? primaryEffect.Magnitude : 0f,
                primaryDuration: primaryEffect != null ? primaryEffect.DurationSeconds : 0f,
                secondaryEffectType: secondaryEffect != null ? secondaryEffect.EffectType.ToString() : string.Empty,
                secondaryMagnitude: secondaryEffect != null ? secondaryEffect.Magnitude : 0f,
                secondaryDuration: secondaryEffect != null ? secondaryEffect.DurationSeconds : 0f,
                drawbackEffectType: drawbackEffect != null ? drawbackEffect.EffectType.ToString() : string.Empty,
                drawbackMagnitude: drawbackEffect != null ? drawbackEffect.Magnitude : 0f,
                drawbackDuration: drawbackEffect != null ? drawbackEffect.DurationSeconds : 0f,
                beforeDamage: preview.HasValue ? preview.Value.Before.Damage : 0f,
                beforeFireRate: preview.HasValue ? preview.Value.Before.FireRate : 0f,
                beforeMaxHp: preview.HasValue ? preview.Value.Before.MaxHp : 0f,
                beforeProjectileCount: preview.HasValue ? preview.Value.Before.ProjectileCount : 0,
                beforeSquadCount: preview.HasValue ? preview.Value.Before.SquadCount : 0,
                beforeEffectiveDps: preview.HasValue ? EstimateEffectiveDps(preview.Value.Before) : 0f,
                beforeEstimatedBaseProjectileEmissionsPerSecond: preview.HasValue
                    ? EstimateBaseProjectileEmissions(preview.Value.Before)
                    : 0f,
                afterDamage: preview.HasValue ? preview.Value.After.Damage : 0f,
                afterFireRate: preview.HasValue ? preview.Value.After.FireRate : 0f,
                afterMaxHp: preview.HasValue ? preview.Value.After.MaxHp : 0f,
                afterProjectileCount: preview.HasValue ? preview.Value.After.ProjectileCount : 0,
                afterSquadCount: preview.HasValue ? preview.Value.After.SquadCount : 0,
                afterEffectiveDps: preview.HasValue ? EstimateEffectiveDps(preview.Value.After) : 0f,
                afterEstimatedBaseProjectileEmissionsPerSecond: preview.HasValue
                    ? EstimateBaseProjectileEmissions(preview.Value.After)
                    : 0f,
                wasCapped: preview.HasValue && preview.Value.WasCapped);
        }

        private void RecordEvent(
            string eventName,
            string gateId = "",
            string gateCategory = "",
            string gateLabel = "",
            int gateSet = 0,
            int laneIndex = -1,
            float value = 0f,
            string gatePhase = "",
            string primaryEffectType = "",
            float primaryMagnitude = 0f,
            float primaryDuration = 0f,
            string secondaryEffectType = "",
            float secondaryMagnitude = 0f,
            float secondaryDuration = 0f,
            string drawbackEffectType = "",
            float drawbackMagnitude = 0f,
            float drawbackDuration = 0f,
            float beforeDamage = 0f,
            float beforeFireRate = 0f,
            float beforeMaxHp = 0f,
            int beforeProjectileCount = 0,
            int beforeSquadCount = 0,
            float beforeEffectiveDps = 0f,
            float beforeEstimatedBaseProjectileEmissionsPerSecond = 0f,
            float afterDamage = 0f,
            float afterFireRate = 0f,
            float afterMaxHp = 0f,
            int afterProjectileCount = 0,
            int afterSquadCount = 0,
            float afterEffectiveDps = 0f,
            float afterEstimatedBaseProjectileEmissionsPerSecond = 0f,
            bool wasCapped = false,
            bool majorRollEligible = false,
            bool majorRollSpawned = false,
            bool majorRollForced = false,
            int majorConsecutiveMisses = 0,
            string majorFailureReason = "")
        {
            _writer.BufferEvent(new BalanceTelemetryEvent
            {
                eventName = eventName,
                runId = _runId,
                utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                buildVersion = Application.version,
                balanceVersion = BalanceVersion,
                runMode = _runMode,
                benchmarkProfileId = _benchmarkProfileId,
                elapsedSeconds = ElapsedSeconds,
                gateId = gateId,
                gateCategory = gateCategory,
                gateLabel = gateLabel,
                gatePhase = gatePhase,
                gateSet = gateSet,
                laneIndex = laneIndex,
                value = value,
                primaryEffectType = primaryEffectType,
                primaryMagnitude = primaryMagnitude,
                primaryDuration = primaryDuration,
                secondaryEffectType = secondaryEffectType,
                secondaryMagnitude = secondaryMagnitude,
                secondaryDuration = secondaryDuration,
                drawbackEffectType = drawbackEffectType,
                drawbackMagnitude = drawbackMagnitude,
                drawbackDuration = drawbackDuration,
                beforeDamage = beforeDamage,
                beforeFireRate = beforeFireRate,
                beforeMaxHp = beforeMaxHp,
                beforeProjectileCount = beforeProjectileCount,
                beforeSquadCount = beforeSquadCount,
                beforeEffectiveDps = beforeEffectiveDps,
                beforeEstimatedBaseProjectileEmissionsPerSecond =
                    beforeEstimatedBaseProjectileEmissionsPerSecond,
                afterDamage = afterDamage,
                afterFireRate = afterFireRate,
                afterMaxHp = afterMaxHp,
                afterProjectileCount = afterProjectileCount,
                afterSquadCount = afterSquadCount,
                afterEffectiveDps = afterEffectiveDps,
                afterEstimatedBaseProjectileEmissionsPerSecond =
                    afterEstimatedBaseProjectileEmissionsPerSecond,
                wasCapped = wasCapped,
                majorRollEligible = majorRollEligible,
                majorRollSpawned = majorRollSpawned,
                majorRollForced = majorRollForced,
                majorConsecutiveMisses = majorConsecutiveMisses,
                majorFailureReason = majorFailureReason,
                enemyKills = runStatsTracker != null ? runStatsTracker.EnemyKills : 0,
                squadCount = playerController != null ? playerController.CurrentSquadCount : 0
            });
        }

        private GateEffectPreviewResult PreviewGate(GateConfig gate)
        {
            GateStatSnapshot before = GateStatSnapshot.FromRuntime(mainPlayerUnit, playerController);
            GateRunStatCaps caps = gateSystem != null ? gateSystem.CurrentRunStatCaps : null;
            return GateEffectPreview.Preview(gate, before, caps, 50, 50);
        }

        private float EstimateEffectiveDps()
        {
            int projectileCount = mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null
                ? mainPlayerUnit.BulletSpawner.ProjectileCount
                : 0;
            int squadCount = playerController != null ? playerController.CurrentSquadCount : 0;
            return EstimateEffectiveDps(new GateStatSnapshot(
                mainPlayerUnit != null ? mainPlayerUnit.Damage : 0f,
                mainPlayerUnit != null ? mainPlayerUnit.FireRate : 0f,
                mainPlayerUnit != null ? mainPlayerUnit.MaxHp : 0f,
                projectileCount,
                squadCount));
        }

        private float EstimateEffectiveDps(GateStatSnapshot snapshot)
        {
            CombatScalingConfig combatConfig = mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null
                ? mainPlayerUnit.BulletSpawner.CurrentCombatScalingConfig
                : null;
            return BalanceV1Math.EffectiveDps(
                snapshot.Damage,
                snapshot.FireRate,
                snapshot.ProjectileCount,
                snapshot.SquadCount,
                combatConfig);
        }

        private float EstimateBaseProjectileEmissions(GateStatSnapshot snapshot)
        {
            CombatScalingConfig combatConfig = mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null
                ? mainPlayerUnit.BulletSpawner.CurrentCombatScalingConfig
                : null;
            return BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                snapshot.FireRate,
                snapshot.ProjectileCount,
                snapshot.SquadCount,
                combatConfig);
        }

        private float GetTotalSquadCurrentHp()
        {
            float total = mainPlayerUnit != null && !mainPlayerUnit.IsDead
                ? mainPlayerUnit.CurrentHp
                : 0f;
            if (playerController == null || playerController.Followers == null)
            {
                return total;
            }

            for (int index = 0; index < playerController.Followers.Count; index++)
            {
                FollowerUnit follower = playerController.Followers[index];
                if (follower != null && !follower.IsDead)
                {
                    total += follower.CurrentHp;
                }
            }

            return total;
        }

        private float GetTotalSquadMaxHp()
        {
            float total = mainPlayerUnit != null && !mainPlayerUnit.IsDead
                ? mainPlayerUnit.MaxHp
                : 0f;
            if (playerController == null || playerController.Followers == null)
            {
                return total;
            }

            for (int index = 0; index < playerController.Followers.Count; index++)
            {
                FollowerUnit follower = playerController.Followers[index];
                if (follower != null && !follower.IsDead)
                {
                    total += follower.MaxHp;
                }
            }

            return total;
        }

        private void EnsureWriter()
        {
            _writer = new BalanceTelemetryWriter(
                OutputDirectory,
                config == null || config.ExportCsv,
                config == null || config.ExportJsonl);
        }

        private bool ShouldCollectTelemetry()
        {
            if (config != null && !config.DevelopmentBuildOnly)
            {
                return true;
            }

            return Application.isEditor || Debug.isDebugBuild;
        }

        private float ElapsedSeconds => runStatsTracker != null
            ? runStatsTracker.SurvivalTime
            : 0f;
        private float SnapshotIntervalSeconds => config != null
            ? config.SnapshotIntervalSeconds
            : 15f;
        private int MaxSnapshotsPerRun => config != null
            ? config.MaxSnapshotsPerRun
            : 80;
        private string BalanceVersion => config != null
            ? config.TelemetryConfigVersion
            : CombatScalingConfig.DefaultConfigVersion;
    }

    public sealed class BalanceTelemetryWriter
    {
        public const string SummaryFileName = "run_summary.csv";
        public const string SnapshotFileName = "run_snapshot_15s.csv";
        public const string EventFileName = "gate_events.jsonl";

        private static readonly string[] SummaryHeader =
        {
            "run_id", "run_started_utc", "run_ended_utc", "build_version",
            "balance_version", "run_mode", "benchmark_profile_id",
            "survival_seconds", "enemy_kills", "coin_reward_points",
            "coins_earned", "score", "wallet_coins", "starting_damage",
            "starting_fire_rate", "starting_max_hp", "starting_projectile_count",
            "starting_squad", "ending_squad", "gates_shown", "gates_selected",
            "first_hit_seconds", "follower_deaths", "promotions", "snapshot_count",
            "peak_effective_dps_estimate", "peak_estimated_base_projectile_emissions_per_second",
            "peak_damage", "peak_projectile_count",
            "peak_squad_count", "first_follower_death_seconds", "ending_damage",
            "ending_fire_rate", "ending_max_hp", "ending_projectile_count",
            "ending_effective_dps_estimate", "ending_total_squad_current_hp",
            "ending_total_squad_max_hp", "ending_incoming_damage_multiplier",
            "ending_enemy_pressure_multiplier", "ending_enemy_speed_multiplier"
        };

        private static readonly string[] SnapshotHeader =
        {
            "run_id", "elapsed_seconds", "enemy_kills", "coin_reward_points",
            "rounded_run_coins", "score", "squad_count", "current_hp", "max_hp",
            "total_squad_current_hp", "total_squad_max_hp", "damage", "fire_rate",
            "projectile_count", "effective_dps_estimate",
            "estimated_base_projectile_emissions_per_second",
            "projectile_factor", "squad_factor", "follower_damage_scale",
            "main_damage_per_projectile", "kills_since_previous_snapshot",
            "active_enemies", "visible_enemies", "active_threat",
            "active_enemy_cap", "minimum_visible_enemies", "raw_spawn_per_second",
            "threat_budget", "active_enemy_ratio", "visible_enemy_ratio",
            "incoming_damage_multiplier", "enemy_pressure_multiplier",
            "enemy_speed_multiplier", "gate_set_count",
            "gate_phase", "major_eligible_rolls", "major_offers",
            "major_pity_forced_offers", "max_consecutive_major_misses"
        };

        private readonly string _directoryPath;
        private readonly bool _exportCsv;
        private readonly bool _exportJsonl;
        private readonly List<string> _summaryRows = new List<string>();
        private readonly List<string> _snapshotRows = new List<string>();
        private readonly List<string> _eventRows = new List<string>();
        private bool _hasWarned;

        public BalanceTelemetryWriter(
            string directoryPath,
            bool exportCsv = true,
            bool exportJsonl = true)
        {
            _directoryPath = directoryPath;
            _exportCsv = exportCsv;
            _exportJsonl = exportJsonl;
        }

        public string SummaryPath => Path.Combine(_directoryPath, SummaryFileName);
        public string SnapshotPath => Path.Combine(_directoryPath, SnapshotFileName);
        public string EventPath => Path.Combine(_directoryPath, EventFileName);
        public int BufferedSummaryCount => _summaryRows.Count;
        public int BufferedSnapshotCount => _snapshotRows.Count;
        public int BufferedEventCount => _eventRows.Count;

        public void BufferSummary(BalanceRunSummaryRow row)
        {
            if (row != null)
            {
                _summaryRows.Add(row.ToCsv());
            }
        }

        public void BufferSnapshot(BalanceRunSnapshotRow row)
        {
            if (row != null)
            {
                _snapshotRows.Add(row.ToCsv());
            }
        }

        public void BufferEvent(BalanceTelemetryEvent telemetryEvent)
        {
            if (telemetryEvent != null)
            {
                _eventRows.Add(JsonUtility.ToJson(telemetryEvent));
            }
        }

        public void Flush()
        {
            try
            {
                Directory.CreateDirectory(_directoryPath);

                if (_exportCsv)
                {
                    AppendCsv(SummaryPath, SummaryHeader, _summaryRows);
                    AppendCsv(SnapshotPath, SnapshotHeader, _snapshotRows);
                }

                if (_exportJsonl && _eventRows.Count > 0)
                {
                    File.AppendAllLines(EventPath, _eventRows, Encoding.UTF8);
                }

                ClearBuffers();
            }
            catch (Exception exception)
            {
                if (!_hasWarned)
                {
                    _hasWarned = true;
                    Debug.LogWarning($"Balance telemetry write failed: {exception.Message}");
                }
            }
        }

        public void ClearBuffers()
        {
            _summaryRows.Clear();
            _snapshotRows.Clear();
            _eventRows.Clear();
        }

        public static string EscapeCsv(string value)
        {
            string safeValue = value ?? string.Empty;
            if (!safeValue.Contains(",")
                && !safeValue.Contains("\"")
                && !safeValue.Contains("\r")
                && !safeValue.Contains("\n"))
            {
                return safeValue;
            }

            return $"\"{safeValue.Replace("\"", "\"\"")}\"";
        }

        private static void AppendCsv(
            string path,
            IReadOnlyList<string> header,
            IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            bool needsHeader = !File.Exists(path) || new FileInfo(path).Length == 0;
            using var writer = new StreamWriter(path, append: true, Encoding.UTF8);

            if (needsHeader)
            {
                writer.WriteLine(string.Join(",", header));
            }

            for (int index = 0; index < rows.Count; index++)
            {
                writer.WriteLine(rows[index]);
            }
        }
    }

    [Serializable]
    public sealed class BalanceTelemetryEvent
    {
        public string eventName;
        public string runId;
        public string utc;
        public string buildVersion;
        public string balanceVersion;
        public string runMode;
        public string benchmarkProfileId;
        public float elapsedSeconds;
        public string gateId;
        public string gateCategory;
        public string gateLabel;
        public string gatePhase;
        public int gateSet;
        public int laneIndex;
        public float value;
        public string primaryEffectType;
        public float primaryMagnitude;
        public float primaryDuration;
        public string secondaryEffectType;
        public float secondaryMagnitude;
        public float secondaryDuration;
        public string drawbackEffectType;
        public float drawbackMagnitude;
        public float drawbackDuration;
        public float beforeDamage;
        public float beforeFireRate;
        public float beforeMaxHp;
        public int beforeProjectileCount;
        public int beforeSquadCount;
        public float beforeEffectiveDps;
        public float beforeEstimatedBaseProjectileEmissionsPerSecond;
        public float afterDamage;
        public float afterFireRate;
        public float afterMaxHp;
        public int afterProjectileCount;
        public int afterSquadCount;
        public float afterEffectiveDps;
        public float afterEstimatedBaseProjectileEmissionsPerSecond;
        public bool wasCapped;
        public bool majorRollEligible;
        public bool majorRollSpawned;
        public bool majorRollForced;
        public int majorConsecutiveMisses;
        public string majorFailureReason;
        public int enemyKills;
        public int squadCount;
    }

    public sealed class BalanceRunSummaryRow
    {
        public string runId;
        public string runStartedUtc;
        public string runEndedUtc;
        public string buildVersion;
        public string balanceVersion;
        public string runMode;
        public string benchmarkProfileId;
        public float survivalSeconds;
        public int enemyKills;
        public float coinRewardPoints;
        public int coinsEarned;
        public int score;
        public int walletCoins;
        public float startingDamage;
        public float startingFireRate;
        public float startingMaxHp;
        public int startingProjectileCount;
        public int startingSquadCount;
        public int endingSquadCount;
        public int gateShownCount;
        public int gateSelectedCount;
        public float firstHitSeconds;
        public int followerDeaths;
        public int promotions;
        public int snapshotCount;
        public float peakEffectiveDpsEstimate;
        public float peakEstimatedBaseProjectileEmissionsPerSecond;
        public float peakDamage;
        public int peakProjectileCount;
        public int peakSquadCount;
        public float firstFollowerDeathSeconds;
        public float endingDamage;
        public float endingFireRate;
        public float endingMaxHp;
        public int endingProjectileCount;
        public float endingEffectiveDpsEstimate;
        public float endingTotalSquadCurrentHp;
        public float endingTotalSquadMaxHp;
        public float endingIncomingDamageMultiplier;
        public float endingEnemyPressureMultiplier;
        public float endingEnemySpeedMultiplier;

        public string ToCsv()
        {
            return string.Join(",",
                BalanceTelemetryWriter.EscapeCsv(runId),
                BalanceTelemetryWriter.EscapeCsv(runStartedUtc),
                BalanceTelemetryWriter.EscapeCsv(runEndedUtc),
                BalanceTelemetryWriter.EscapeCsv(buildVersion),
                BalanceTelemetryWriter.EscapeCsv(balanceVersion),
                BalanceTelemetryWriter.EscapeCsv(runMode),
                BalanceTelemetryWriter.EscapeCsv(benchmarkProfileId),
                F(survivalSeconds),
                enemyKills,
                F(coinRewardPoints),
                coinsEarned,
                score,
                walletCoins,
                F(startingDamage),
                F(startingFireRate),
                F(startingMaxHp),
                startingProjectileCount,
                startingSquadCount,
                endingSquadCount,
                gateShownCount,
                gateSelectedCount,
                F(firstHitSeconds),
                followerDeaths,
                promotions,
                snapshotCount,
                F(peakEffectiveDpsEstimate),
                F(peakEstimatedBaseProjectileEmissionsPerSecond),
                F(peakDamage),
                peakProjectileCount,
                peakSquadCount,
                F(firstFollowerDeathSeconds),
                F(endingDamage),
                F(endingFireRate),
                F(endingMaxHp),
                endingProjectileCount,
                F(endingEffectiveDpsEstimate),
                F(endingTotalSquadCurrentHp),
                F(endingTotalSquadMaxHp),
                F(endingIncomingDamageMultiplier),
                F(endingEnemyPressureMultiplier),
                F(endingEnemySpeedMultiplier));
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    public sealed class BalanceRunSnapshotRow
    {
        public string runId;
        public float elapsedSeconds;
        public int enemyKills;
        public float coinRewardPoints;
        public int roundedRunCoins;
        public int score;
        public int squadCount;
        public float currentHp;
        public float maxHp;
        public float totalSquadCurrentHp;
        public float totalSquadMaxHp;
        public float damage;
        public float fireRate;
        public int projectileCount;
        public float effectiveDpsEstimate;
        public float estimatedBaseProjectileEmissionsPerSecond;
        public float projectileFactor;
        public float squadFactor;
        public float followerDamageScale;
        public float mainDamagePerProjectile;
        public int killsSincePreviousSnapshot;
        public int activeEnemies;
        public int visibleEnemies;
        public float activeThreat;
        public int activeEnemyCap;
        public int minimumVisibleEnemies;
        public float rawSpawnPerSecond;
        public float threatBudget;
        public float activeEnemyRatio;
        public float visibleEnemyRatio;
        public float incomingDamageMultiplier;
        public float enemyPressureMultiplier;
        public float enemySpeedMultiplier;
        public int gateSetCount;
        public string gatePhase;
        public int majorEligibleRolls;
        public int majorOffers;
        public int majorPityForcedOffers;
        public int maxConsecutiveMajorMisses;

        public string ToCsv()
        {
            return string.Join(",",
                BalanceTelemetryWriter.EscapeCsv(runId),
                F(elapsedSeconds),
                enemyKills,
                F(coinRewardPoints),
                roundedRunCoins,
                score,
                squadCount,
                F(currentHp),
                F(maxHp),
                F(totalSquadCurrentHp),
                F(totalSquadMaxHp),
                F(damage),
                F(fireRate),
                projectileCount,
                F(effectiveDpsEstimate),
                F(estimatedBaseProjectileEmissionsPerSecond),
                F(projectileFactor),
                F(squadFactor),
                F(followerDamageScale),
                F(mainDamagePerProjectile),
                killsSincePreviousSnapshot,
                activeEnemies,
                visibleEnemies,
                F(activeThreat),
                activeEnemyCap,
                minimumVisibleEnemies,
                F(rawSpawnPerSecond),
                F(threatBudget),
                F(activeEnemyRatio),
                F(visibleEnemyRatio),
                F(incomingDamageMultiplier),
                F(enemyPressureMultiplier),
                F(enemySpeedMultiplier),
                gateSetCount,
                BalanceTelemetryWriter.EscapeCsv(gatePhase),
                majorEligibleRolls,
                majorOffers,
                majorPityForcedOffers,
                maxConsecutiveMajorMisses);
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
