using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Player;
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
        private float _firstHitSeconds = -1f;

        public string OutputDirectory => Path.Combine(
            Application.persistentDataPath,
            TelemetryFolderName);
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
            _firstHitRecorded = false;
            _firstHitSeconds = -1f;
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
                survivalSeconds = snapshot.SurvivalTime,
                enemyKills = snapshot.EnemyKills,
                coinRewardPoints = runStatsTracker != null
                    ? runStatsTracker.CoinRewardPoints
                    : snapshot.CoinsEarned,
                coinsEarned = snapshot.CoinsEarned,
                score = snapshot.Score,
                walletCoins = snapshot.WalletCoins,
                startingSquadCount = _startingSquadCount,
                endingSquadCount = playerController != null
                    ? playerController.CurrentSquadCount
                    : 0,
                gateShownCount = _gateShownCount,
                gateSelectedCount = _gateSelectedCount,
                firstHitSeconds = _firstHitSeconds,
                followerDeaths = _followerDeathCount,
                promotions = _promotionCount,
                snapshotCount = _snapshotCount
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
            RecordGateEvent("gate_selected", gateSet, -1, gate);
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

            _writer.BufferSnapshot(new BalanceRunSnapshotRow
            {
                runId = _runId,
                elapsedSeconds = elapsed,
                enemyKills = runStatsTracker.EnemyKills,
                coinRewardPoints = runStatsTracker.CoinRewardPoints,
                roundedRunCoins = runStatsTracker.CoinsEarned,
                score = runStatsTracker.Score,
                squadCount = playerController != null ? playerController.CurrentSquadCount : 0,
                currentHp = mainPlayerUnit != null ? mainPlayerUnit.CurrentHp : 0f,
                maxHp = mainPlayerUnit != null ? mainPlayerUnit.MaxHp : 0f,
                damage = mainPlayerUnit != null ? mainPlayerUnit.Damage : 0f,
                fireRate = mainPlayerUnit != null ? mainPlayerUnit.FireRate : 0f,
                projectileCount = mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null
                    ? mainPlayerUnit.BulletSpawner.ProjectileCount
                    : 0,
                activeEnemies = enemySpawnerSystem != null
                    ? enemySpawnerSystem.ActiveEnemyCount
                    : 0,
                visibleEnemies = enemySpawnerSystem != null
                    ? enemySpawnerSystem.VisibleEnemyCount
                    : 0,
                activeThreat = enemySpawnerSystem != null
                    ? enemySpawnerSystem.CurrentActiveThreat
                    : 0f,
                gateSetCount = gateSystem != null ? gateSystem.GateSetCount : 0
            });

            _snapshotCount++;
            RecordEvent("snapshot_15s");
        }

        private void RecordGateEvent(
            string eventName,
            int gateSet,
            int laneIndex,
            GateConfig gate)
        {
            RecordEvent(
                eventName,
                gate != null ? gate.GateId : string.Empty,
                gate != null ? gate.Category.ToString() : string.Empty,
                gate != null ? gate.GetDisplayText() : string.Empty,
                gateSet,
                laneIndex,
                gate != null ? gate.Amount : 0f);
        }

        private void RecordEvent(
            string eventName,
            string gateId = "",
            string gateCategory = "",
            string gateLabel = "",
            int gateSet = 0,
            int laneIndex = -1,
            float value = 0f)
        {
            _writer.BufferEvent(new BalanceTelemetryEvent
            {
                eventName = eventName,
                runId = _runId,
                utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                buildVersion = Application.version,
                balanceVersion = BalanceVersion,
                elapsedSeconds = ElapsedSeconds,
                gateId = gateId,
                gateCategory = gateCategory,
                gateLabel = gateLabel,
                gateSet = gateSet,
                laneIndex = laneIndex,
                value = value,
                enemyKills = runStatsTracker != null ? runStatsTracker.EnemyKills : 0,
                squadCount = playerController != null ? playerController.CurrentSquadCount : 0
            });
        }

        private void EnsureWriter()
        {
            _writer ??= new BalanceTelemetryWriter(
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
            "balance_version", "survival_seconds", "enemy_kills",
            "coin_reward_points", "coins_earned", "score", "wallet_coins",
            "starting_squad", "ending_squad", "gates_shown", "gates_selected",
            "first_hit_seconds", "follower_deaths", "promotions", "snapshot_count"
        };

        private static readonly string[] SnapshotHeader =
        {
            "run_id", "elapsed_seconds", "enemy_kills", "coin_reward_points",
            "rounded_run_coins", "score", "squad_count", "current_hp", "max_hp",
            "damage", "fire_rate", "projectile_count", "active_enemies",
            "visible_enemies", "active_threat", "gate_set_count"
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
        public float elapsedSeconds;
        public string gateId;
        public string gateCategory;
        public string gateLabel;
        public int gateSet;
        public int laneIndex;
        public float value;
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
        public float survivalSeconds;
        public int enemyKills;
        public float coinRewardPoints;
        public int coinsEarned;
        public int score;
        public int walletCoins;
        public int startingSquadCount;
        public int endingSquadCount;
        public int gateShownCount;
        public int gateSelectedCount;
        public float firstHitSeconds;
        public int followerDeaths;
        public int promotions;
        public int snapshotCount;

        public string ToCsv()
        {
            return string.Join(",",
                BalanceTelemetryWriter.EscapeCsv(runId),
                BalanceTelemetryWriter.EscapeCsv(runStartedUtc),
                BalanceTelemetryWriter.EscapeCsv(runEndedUtc),
                BalanceTelemetryWriter.EscapeCsv(buildVersion),
                BalanceTelemetryWriter.EscapeCsv(balanceVersion),
                F(survivalSeconds),
                enemyKills,
                F(coinRewardPoints),
                coinsEarned,
                score,
                walletCoins,
                startingSquadCount,
                endingSquadCount,
                gateShownCount,
                gateSelectedCount,
                F(firstHitSeconds),
                followerDeaths,
                promotions,
                snapshotCount);
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
        public float damage;
        public float fireRate;
        public int projectileCount;
        public int activeEnemies;
        public int visibleEnemies;
        public float activeThreat;
        public int gateSetCount;

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
                F(damage),
                F(fireRate),
                projectileCount,
                activeEnemies,
                visibleEnemies,
                F(activeThreat),
                gateSetCount);
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
